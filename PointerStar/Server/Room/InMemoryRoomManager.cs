using PointerStar.Shared;

namespace PointerStar.Server.Room;
public class InMemoryRoomManager : IRoomManager
{
    private ConcurrentDictionary<string, RoomState> Rooms { get; } = new();
    private ConcurrentDictionary<string, string> ConnectionsToRoom { get; } = new();
    private ConcurrentDictionary<string, Guid> ConnectionsToUserId { get; } = new();

    public Task<RoomState> AddUserToRoomAsync(string roomId, User user, string connectionId)
    {
        ConnectionsToRoom.AddOrUpdate(connectionId, roomId, (_, _) => roomId);
        ConnectionsToUserId.AddOrUpdate(connectionId, user.Id, (_, _) => user.Id);

        RoomState rv = Rooms.AddOrUpdate(
            roomId,
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
    }

    public Task<RoomState?> DisconnectAsync(string connectionId)
    {
        if (ConnectionsToRoom.TryRemove(connectionId, out string? roomId) &&
            ConnectionsToUserId.TryRemove(connectionId, out Guid userId))
        {
            RoomState rv = Rooms.AddOrUpdate(
                roomId,
                new RoomState(roomId, Array.Empty<User>()),
                (_, roomState) =>
                {
                    return roomState with { Users = roomState.Users.Where(x => x.Id != userId).ToArray() };
                });
            if (rv.Users.Length == 0)
            {
                Rooms.TryRemove(roomId, out _);
                return Task.FromResult<RoomState?>(null);
            }
            return Task.FromResult<RoomState?>(rv);
        }
        return Task.FromResult<RoomState?>(null);
    }

    public Task<RoomState?> SubmitVoteAsync(string vote, string connectionId)
    {
        if (ConnectionsToRoom.TryGetValue(connectionId, out string? roomId) &&
            ConnectionsToUserId.TryGetValue(connectionId, out Guid userId))
        {
            RoomState? rv = Rooms.AddOrUpdate(
                roomId,
                new RoomState(roomId, new[] { new User(userId, "", vote) }),
                (_, roomState) =>
                {
                    User[] users = roomState.Users.Select(u => u.Id == userId ? u with { Vote = vote } : u).ToArray();
                    return roomState with { Users = users };
                });
            return Task.FromResult<RoomState?>(rv);
        }
        return Task.FromResult<RoomState?>(null);
    }
}
