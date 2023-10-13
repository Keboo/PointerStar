using PointerStar.Shared;

namespace PointerStar.Server.Room;
public class InMemoryRoomManager : IRoomManager
{
    private ConcurrentDictionary<string, SemaphoreSlim> RoomLocks { get; } = new();
    private ConcurrentDictionary<string, RoomState> Rooms { get; } = new();
    private ConcurrentDictionary<string, string> ConnectionsToRoom { get; } = new();
    private ConcurrentDictionary<string, User> ConnectionsToUser { get; } = new();

    public Task<RoomState> AddUserToRoomAsync(string roomId, User user, string connectionId)
    {
        ConnectionsToRoom.AddOrUpdate(connectionId, roomId, (_, _) => roomId);
        ConnectionsToUser.AddOrUpdate(connectionId, user, (_, _) => user);

        if (user.Name.Length > User.MaxNameLength)
        {
            user = user with { Name = user.Name[..User.MaxNameLength] };
        }

        return WithRoomLock(roomId, () =>
        {
            RoomState rv = Rooms.AddOrUpdate(roomId,
                new RoomState(roomId, new[] { user }),
            (_, roomState) =>
            {
                return roomState with
                {
                    Users = roomState.Users
                        .Where(u => u.Id != user.Id)
                        .Append(user).ToArray()
                };
            });
            return Task.FromResult(rv);
        });

    }

    public Task<RoomState?> DisconnectAsync(string connectionId)
    {
        return WithConnection(connectionId, (room, currentUser) =>
        {
            User[] users = room.Users.Where(x => x.Id != currentUser.Id).ToArray();
            if (users.Any())
            {
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
                return room;
            }
            return room;
        });
    }

    public Task<RoomState?> UpdateUserAsync(UserOptions userOptions, string connectionId)
    {
        return WithConnection(connectionId, (room, currentUser) =>
        {
            User[] users = room.Users.Select(x =>
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
            }).ToArray();
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
                Users = room.Users.Select(u => u.Id == currentUser.Id ? u with
                {
                    OriginalVote = room.VotesShown ? u.OriginalVote : vote,
                    Vote = vote
                } : u).ToArray()
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
            ConnectionsToUser.TryGetValue(connectionId, out User? user))
        {
            return WithExistingRoom(roomId, room => updateRoom(room, user));
        }
        return Task.FromResult<RoomState?>(null);
    }

    private Task<RoomState?> WithExistingRoom(string roomId, Func<RoomState, RoomState?> updateRoom)
    {
        return WithRoomLock(roomId, () =>
        {
            if (Rooms.TryGetValue(roomId, out RoomState? existingRoom))
            {
                RoomState? updatedRoom = updateRoom(existingRoom);
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
            var teamMembers = roomState.TeamMemebers;
            if (teamMembers.Any() && teamMembers.All(x => x.Vote is not null))
            {
                return true;
            }
        }
        return false;
    }

}
