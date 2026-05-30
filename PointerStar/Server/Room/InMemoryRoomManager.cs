using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PointerStar.Server.Hubs;
using PointerStar.Shared;

namespace PointerStar.Server.Room;
public class InMemoryRoomManager : IRoomManager
{
    private ConcurrentDictionary<string, SemaphoreSlim> RoomLocks { get; } = new();
    private ConcurrentDictionary<string, RoomState> Rooms { get; } = new();
    private ConcurrentDictionary<string, string> ConnectionsToRoom { get; } = new();
    private ConcurrentDictionary<string, Guid> ConnectionsToUserId { get; } = new();
    private ConcurrentDictionary<Guid, CancellationTokenSource> PendingDisconnects { get; } = new();
    private TelemetryClient TelemetryClient { get; }
    private IHubContext<RoomHub> HubContext { get; }
    private ILogger<InMemoryRoomManager> Logger { get; }
    private TimeProvider TimeProvider { get; }

    public InMemoryRoomManager(TelemetryClient telemetryClient, IHubContext<RoomHub> hubContext, ILogger<InMemoryRoomManager> logger, TimeProvider timeProvider)
    {
        TelemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
        HubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public Task<RoomState> AddUserToRoomAsync(string roomId, User user, string connectionId)
    {
        string normalizedRoomId = NormalizeRoomId(roomId);
        ConnectionsToRoom.AddOrUpdate(connectionId, normalizedRoomId, (_, _) => normalizedRoomId);
        ConnectionsToUserId.AddOrUpdate(connectionId, user.Id, (_, _) => user.Id);

        if (user.Name.Length > User.MaxNameLength)
        {
            user = user with { Name = user.Name[..User.MaxNameLength] };
        }

        return WithRoomLock(roomId, () =>
        {
            bool isNewRoom = !Rooms.ContainsKey(normalizedRoomId);
            
            RoomState rv = Rooms.AddOrUpdate(normalizedRoomId,
                // For new rooms, use the original roomId casing
                new RoomState(roomId, [user]),
            (_, roomState) =>
            {
                return roomState with
                {
                    Users = [..roomState.Users
                        .Where(u => u.Id != user.Id)
                        .Append(user)]
                };
            });

            // Track room creation
            if (isNewRoom)
            {
                TelemetryClient?.TrackEvent("RoomCreated", new Dictionary<string, string>
                {
                    { "RoomId", rv.RoomId },
                    { "VotingMode", rv.VotingMode.ToString() }
                });
            }

            // Track user connection
            TelemetryClient?.TrackEvent("UserConnected", new Dictionary<string, string>
            {
                { "RoomId", rv.RoomId },
                { "UserId", user.Id.ToString() },
                { "UserRole", user.Role.Name }
            });

            // Track room user count metric
            TelemetryClient?.GetMetric("RoomUserCount", "RoomId").TrackValue(rv.Users.Length, rv.RoomId);

            return Task.FromResult(rv);
        });

    }

    public Task<RoomState?> DisconnectAsync(string connectionId)
    {
        return WithConnection(connectionId, (room, currentUser) =>
        {
            User[] users = [..room.Users.Where(x => x.Id != currentUser.Id)];
            
            // Track user disconnection
            TelemetryClient?.TrackEvent("UserDisconnected", new Dictionary<string, string>
            {
                { "RoomId", room.RoomId },
                { "UserId", currentUser.Id.ToString() },
                { "UserRole", currentUser.Role.Name }
            });

            if (users.Length != 0)
            {
                // Track remaining user count
                TelemetryClient?.GetMetric("RoomUserCount", "RoomId").TrackValue(users.Length, room.RoomId);
                return room with { Users = users };
            }
            return null;
        });
    }

    public bool TryReleaseConnection(string connectionId, out string? roomId, out Guid userId)
    {
        bool hadRoom = ConnectionsToRoom.TryRemove(connectionId, out roomId);
        bool hadUser = ConnectionsToUserId.TryRemove(connectionId, out userId);
        return hadRoom && hadUser;
    }

    public Task ScheduleDisconnectAsync(Guid userId, string roomId, TimeSpan delay)
    {
        CancelPendingDisconnect(userId);

        var cts = new CancellationTokenSource();
        PendingDisconnects[userId] = cts;

        // Start the deferred disconnect. The method executes synchronously until the first
        // await, so the timer is registered with TimeProvider before this method returns.
        _ = RunDeferredDisconnectAsync(userId, roomId, delay, cts);

        return Task.CompletedTask;
    }

    private async Task RunDeferredDisconnectAsync(Guid userId, string roomId, TimeSpan delay, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(delay, TimeProvider, cts.Token);
            RoomState? room = await RemoveUserFromRoomAsync(NormalizeRoomId(roomId), userId);
            if (room is not null)
            {
                await HubContext.Clients.Group(NormalizeRoomId(room.RoomId))
                    .SendAsync(RoomHubConnection.RoomUpdatedMethodName, room);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during deferred disconnect for user {UserId} in room {RoomId}", userId, roomId);
        }
        finally
        {
            PendingDisconnects.TryRemove(userId, out _);
        }
    }

    public void CancelPendingDisconnect(Guid userId)
    {
        if (PendingDisconnects.TryRemove(userId, out CancellationTokenSource? cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    public Task<RoomState?> UpdateRoomAsync(RoomOptions roomOptions, string connectionId)
    {
        return WithConnection(connectionId, (room, currentUser) =>
        {
            //Only allow facilitators to change the room options
            if (currentUser.Role == Role.Facilitator)
            {
                if (roomOptions.AutoShowVotes is { } autoShowVotes)
                {
                    room = room with { AutoShowVotes = autoShowVotes };
                }

                if (roomOptions.FacilitatorCanVote is { } facilitatorCanVote)
                {
                    room = room with { FacilitatorCanVote = facilitatorCanVote };
                }

                if (roomOptions.VotesShown is { } votesShown)
                {
                    room = room with { VotesShown = votesShown };
                    if (!votesShown && room.AutoShowVotes)
                    {
                        room = room with { AutoShowVotes = false };
                    }
                }
                else if (ShouldShowVotes(room))
                {
                    room = room with { VotesShown = true };
                }

                if (roomOptions.VoteOptions is { Length: > 0 } voteOptions)
                {
                    room = room with { VoteOptions = voteOptions };
                }

                if (roomOptions.VotingMode.HasValue && roomOptions.VotingMode != room.VotingMode)
                {
                    room = room with
                    {
                        VotingMode = roomOptions.VotingMode.Value,
                        Users = [.. room.Users.Select(u => u with { Vote = null, OriginalVote = null })],
                        VotesShown = false,
                        VoteStartTime = DateTime.UtcNow,
                        ResetVotesRequestedAt = null,
                        ResetVotesRequestedBy = null,
                    };
                }

                return room;
            }
            return room;
        });
    }

    public Task<RoomState?> UpdateUserAsync(UserOptions userOptions, string connectionId)
    {
        return WithConnection(connectionId, (room, currentUser) =>
        {
            User[] users = [..room.Users.Select(x =>
            {
                if (x.Id == currentUser.Id)
                {
                    if (userOptions.Name is { } name)
                    {
                        if (name.Length > User.MaxNameLength)
                        {
                            name = name[..User.MaxNameLength];
                        }
                        x = x with { Name = name };
                    }
                    if (userOptions.Role is { } role)
                    {
                        x = x with { Role = role };
                    }
                }
                return x;
            })];
            return room with { Users = users };
        });
    }

    public Task<RoomState?> SubmitVoteAsync(string vote, string connectionId)
    {
        return WithConnection(connectionId, (room, currentUser) =>
        {
            // Validate vote based on voting mode
            if (!IsValidVote(vote, room.VotingMode, room.VoteOptions))
            {
                return room;
            }

            var roomState = room with
            {
                Users = [..room.Users.Select(u => u.Id == currentUser.Id ? u with
                {
                    OriginalVote = room.VotesShown ? u.OriginalVote : vote,
                    Vote = vote
                } : u)]
            };

            if (ShouldShowVotes(roomState))
            {
                roomState = roomState with
                {
                    VotesShown = true
                };
            }
            return roomState;
        });
    }

    public Task<RoomState?> ResetVotesAsync(string connectionId)
    {
        return WithConnection(connectionId, (room, currentUser) =>
        {
            if (currentUser.Role == Role.Facilitator)
            {
                User[] users = [.. room.Users.Select(u => u with { Vote = null })];
                
                // Track vote reset (indicates active pointing session)
                TelemetryClient?.TrackEvent("VotesReset", new Dictionary<string, string>
                {
                    { "RoomId", room.RoomId },
                    { "FacilitatorId", currentUser.Id.ToString() }
                });

                return room with
                {
                    Users = users,
                    VotesShown = false,
                    VoteStartTime = DateTime.UtcNow,
                    ResetVotesRequestedAt = null,
                    ResetVotesRequestedBy = null,
                };
            }
            return room;
        });
    }

    public Task<RoomState?> RequestResetVotesAsync(string connectionId)
    {
        return WithConnection(connectionId, (room, currentUser) =>
        {
            // Any user can request a reset
            // Track reset request
            TelemetryClient?.TrackEvent("VoteResetRequested", new Dictionary<string, string>
            {
                { "RoomId", room.RoomId },
                { "UserId", currentUser.Id.ToString() },
                { "UserRole", currentUser.Role.Name }
            });

            return room with
            {
                ResetVotesRequestedAt = DateTime.UtcNow.AddSeconds(10),
                ResetVotesRequestedBy = currentUser.Id
            };
        });
    }

    public Task<RoomState?> CancelResetVotesAsync(string connectionId)
    {
        return WithConnection(connectionId, (room, currentUser) =>
        {
            // Only facilitators can cancel a reset request
            if (currentUser.Role == Role.Facilitator)
            {
                // Track reset cancellation
                TelemetryClient?.TrackEvent("VoteResetCancelled", new Dictionary<string, string>
                {
                    { "RoomId", room.RoomId },
                    { "FacilitatorId", currentUser.Id.ToString() }
                });

                return room with
                {
                    ResetVotesRequestedAt = null,
                    ResetVotesRequestedBy = null
                };
            }
            return room;
        });
    }

    public Task<Role> GetNewUserRoleAsync(string roomId)
    {
        return WithRoomLock(roomId, () =>
        {
            string normalizedRoomId = NormalizeRoomId(roomId);
            if (Rooms.TryGetValue(normalizedRoomId, out RoomState? room) &&
                room.Users.Any(x => x.Role == Role.Facilitator))
            {
                return Task.FromResult(Role.TeamMember);
            }
            return Task.FromResult(Role.Facilitator);
        });
    }

    public Task<RoomState?> RemoveUserAsync(Guid userId, string connectionId)
    {
        return WithConnection(connectionId, (room, currentUser) =>
        {
            if (currentUser.Role != Role.Facilitator)
            {
                return room;
            }
            // Facilitators cannot be demoted to observer
            if (room.Users.SingleOrDefault(u => u.Id == userId)?.Role == Role.Facilitator)
            {
                return room;
            }
            return room with
            {
                Users = [.. room.Users.Select(u => u.Id == userId ? u with
                {
                    Role = Role.Observer
                } : u)]
            };
        });
    }


    private Task<RoomState?> RemoveUserFromRoomAsync(string roomId, Guid userId)
    {
        return WithExistingRoom(roomId, userId, (room, currentUser) =>
        {
            User[] users = [..room.Users.Where(x => x.Id != currentUser.Id)];

            TelemetryClient?.TrackEvent("UserDisconnected", new Dictionary<string, string>
            {
                { "RoomId", room.RoomId },
                { "UserId", currentUser.Id.ToString() },
                { "UserRole", currentUser.Role.Name }
            });

            if (users.Length != 0)
            {
                TelemetryClient?.GetMetric("RoomUserCount", "RoomId").TrackValue(users.Length, room.RoomId);
                return room with { Users = users };
            }
            return null;
        });
    }

    private Task<RoomState?> WithConnection(string connectionId, Func<RoomState, User, RoomState?> updateRoom)
    {
        if (ConnectionsToRoom.TryGetValue(connectionId, out string? roomId) &&
            ConnectionsToUserId.TryGetValue(connectionId, out Guid userId))
        {
            return WithExistingRoom(roomId, userId, (room, user) => updateRoom(room, user));
        }
        return Task.FromResult<RoomState?>(null);
    }

    private Task<RoomState?> WithExistingRoom(string roomId, Guid userId, Func<RoomState, User, RoomState?> updateRoom)
    {
        return WithRoomLock(roomId, () =>
        {
            string normalizedRoomId = NormalizeRoomId(roomId);
            if (Rooms.TryGetValue(normalizedRoomId, out RoomState? existingRoom) &&
                existingRoom.Users.SingleOrDefault(x => x.Id == userId) is { } user)
            {
                RoomState? updatedRoom = updateRoom(existingRoom, user);
                if (updatedRoom is not null)
                {
                    if (Rooms.TryUpdate(normalizedRoomId, updatedRoom, existingRoom))
                    {
                        return Task.FromResult<RoomState?>(updatedRoom);
                    }
                    else
                    {
                        return Task.FromResult<RoomState?>(existingRoom);
                    }
                }
                else
                {
                    Rooms.TryRemove(normalizedRoomId, out _);
                }
            }
            return Task.FromResult<RoomState?>(null);
        });
    }

    private async Task<T> WithRoomLock<T>(string roomId, Func<Task<T>> action)
    {
        string normalizedRoomId = NormalizeRoomId(roomId);
        SemaphoreSlim roomLock = RoomLocks.AddOrUpdate(normalizedRoomId, new SemaphoreSlim(1, 1), (_, existing) => existing);

        await roomLock.WaitAsync();

        try
        {
            return await action();
        }
        finally
        {
            roomLock.Release();
        }
    }

    private static bool ShouldShowVotes(RoomState roomState)
    {
        if (roomState.AutoShowVotes)
        {
            IReadOnlyList<User> votingMembers = roomState.FacilitatorCanVote
                ? [.. roomState.TeamMembers, .. roomState.Facilitators]
                : roomState.TeamMembers;
            if (votingMembers.Any() && votingMembers.All(x => x.Vote is not null))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Validates a vote based on the current voting mode.
    /// </summary>
    private static bool IsValidVote(string vote, VotingMode votingMode, string[] voteOptions)
    {
        return votingMode switch
        {
            VotingMode.Standard => voteOptions.Contains(vote),
            VotingMode.Giphy => IsValidGiphyId(vote),
            _ => false
        };
    }

    /// <summary>
    /// Validates that a vote is a reasonably formatted Giphy ID.
    /// Giphy IDs returned by the API vary in length, so avoid strict length assumptions.
    /// </summary>
    private static bool IsValidGiphyId(string giphyId)
    {
        if (string.IsNullOrWhiteSpace(giphyId))
        {
            return false;
        }

        string normalizedId = giphyId.Trim();

        // Accept a practical max length and common identifier characters from API responses.
        return normalizedId.Length <= 128 &&
               System.Text.RegularExpressions.Regex.IsMatch(normalizedId, "^[a-zA-Z0-9_-]+$");
    }

    private static string NormalizeRoomId(string roomId) => roomId.ToUpperInvariant();

}
