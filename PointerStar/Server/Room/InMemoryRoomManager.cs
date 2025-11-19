using PointerStar.Shared;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace PointerStar.Server.Room;
public class InMemoryRoomManager : IRoomManager
{
    private ConcurrentDictionary<string, SemaphoreSlim> RoomLocks { get; } = new();
    private ConcurrentDictionary<string, RoomState> Rooms { get; } = new();
    private ConcurrentDictionary<string, string> ConnectionsToRoom { get; } = new();
    private ConcurrentDictionary<string, Guid> ConnectionsToUserId { get; } = new();
    private TelemetryClient TelemetryClient { get; }

    public InMemoryRoomManager(TelemetryClient telemetryClient)
    {
        TelemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
    }

    public Task<RoomState> AddUserToRoomAsync(string roomId, User user, string connectionId)
    {
        ConnectionsToRoom.AddOrUpdate(connectionId, roomId, (_, _) => roomId);
        ConnectionsToUserId.AddOrUpdate(connectionId, user.Id, (_, _) => user.Id);

        if (user.Name.Length > User.MaxNameLength)
        {
            user = user with { Name = user.Name[..User.MaxNameLength] };
        }

        return WithRoomLock(roomId, () =>
        {
            bool isNewRoom = !Rooms.ContainsKey(roomId);
            
            RoomState rv = Rooms.AddOrUpdate(roomId,
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
                    { "RoomId", roomId }
                });
            }

            // Track user connection
            TelemetryClient?.TrackEvent("UserConnected", new Dictionary<string, string>
            {
                { "RoomId", roomId },
                { "UserId", user.Id.ToString() },
                { "UserRole", user.Role.Name }
            });

            // Track room user count metric
            TelemetryClient?.GetMetric("RoomUserCount", "RoomId").TrackValue(rv.Users.Length, roomId);

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
            if (!room.VoteOptions.Contains(vote))
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
                User[] users = room.Users.Select(u => u with { Vote = null }).ToArray();
                
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
                };
            }
            return room;
        });
    }

    public Task<Role> GetNewUserRoleAsync(string roomId)
    {
        return WithRoomLock(roomId, () =>
        {
            if (Rooms.TryGetValue(roomId, out RoomState? room) &&
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
            return room with
            {
                Users = room.Users.Select(u => u.Id == userId ? u with
                {
                    Role = Role.Observer
                } : u).ToArray()
            };
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
            if (Rooms.TryGetValue(roomId, out RoomState? existingRoom) &&
                existingRoom.Users.SingleOrDefault(x => x.Id == userId) is { } user)
            {
                RoomState? updatedRoom = updateRoom(existingRoom, user);
                if (updatedRoom is not null)
                {
                    if (Rooms.TryUpdate(roomId, updatedRoom, existingRoom))
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
                    Rooms.TryRemove(roomId, out _);
                }
            }
            return Task.FromResult<RoomState?>(null);
        });
    }

    private async Task<T> WithRoomLock<T>(string roomId, Func<Task<T>> action)
    {
        SemaphoreSlim roomLock = RoomLocks.AddOrUpdate(roomId, new SemaphoreSlim(1, 1), (_, existing) => existing);

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
            var teamMembers = roomState.TeamMembers;
            if (teamMembers.Any() && teamMembers.All(x => x.Vote is not null))
            {
                return true;
            }
        }
        return false;
    }

}
