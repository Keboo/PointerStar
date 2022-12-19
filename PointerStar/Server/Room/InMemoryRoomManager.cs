﻿using PointerStar.Shared;

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

        return WithRoomLock(roomId, () =>
        {
            RoomState rv = Rooms.AddOrUpdate(roomId,
                new RoomState(roomId, new[] { user with { Role = Role.Facilitator } }),
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
        if (ConnectionsToRoom.TryRemove(connectionId, out string? roomId) &&
            ConnectionsToUser.TryRemove(connectionId, out User? user))
        {
            return WithExistingRoom(roomId, room =>
            {
                User[] users = room.Users.Where(x => x.Id != user.Id).ToArray();
                if (users.Any())
                {
                    return room with { Users = users };
                }
                return null;
            });
        }
        return Task.FromResult<RoomState?>(null);
    }

    public Task<RoomState?> ShowVotesAsync(bool areVotesShown, string connectionId)
    {
        if (ConnectionsToRoom.TryGetValue(connectionId, out string? roomId) &&
            ConnectionsToUser.TryGetValue(connectionId, out User? user))
        {
            return WithExistingRoom(roomId, room =>
            {
                //Only allow facilitators to change the votes
                if (room.Users.FirstOrDefault(x => x.Id == user.Id)?.Role == Role.Facilitator)
                {
                    return room with { VotesShown = areVotesShown };
                }
                return room;
            });
        }
        return Task.FromResult<RoomState?>(null);
    }

    public Task<RoomState?> SubmitVoteAsync(string vote, string connectionId)
    {
        if (ConnectionsToRoom.TryGetValue(connectionId, out string? roomId) &&
            ConnectionsToUser.TryGetValue(connectionId, out User? user))
        {
            return WithExistingRoom(roomId, room =>
            {
                User[] users = room.Users.Select(u => u.Id == user.Id ? u with { Vote = vote } : u).ToArray();
                return room with { Users = users };
            });
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
}