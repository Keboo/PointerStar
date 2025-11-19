namespace PointerStar.Client.Services;

public interface IRecentRoomsService
{
    ValueTask<IReadOnlyList<RecentRoom>> GetRecentRoomsAsync();
    ValueTask AddRoomAsync(string roomId);
    ValueTask RemoveRoomAsync(string roomId);
}
