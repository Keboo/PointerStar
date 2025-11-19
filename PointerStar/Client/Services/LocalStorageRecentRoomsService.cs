using System.Text.Json;
using Microsoft.JSInterop;

namespace PointerStar.Client.Services;

public class LocalStorageRecentRoomsService : IRecentRoomsService
{
    private const string StorageKey = "RecentRooms";
    private const int MaxDaysToKeep = 30;
    
    private readonly IJSRuntime _jsRuntime;

    public LocalStorageRecentRoomsService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }

    public async ValueTask<IReadOnlyList<RecentRoom>> GetRecentRoomsAsync()
    {
        try
        {
            string? json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<RecentRoom>();
            }

            var rooms = JsonSerializer.Deserialize<List<RecentRoom>>(json) ?? new List<RecentRoom>();
            
            // Filter out rooms older than 30 days
            DateTime cutoffDate = DateTime.UtcNow.AddDays(-MaxDaysToKeep);
            var recentRooms = rooms.Where(r => r.LastAccessed >= cutoffDate).ToList();
            
            // If we filtered any rooms, save the updated list
            if (recentRooms.Count != rooms.Count)
            {
                await SaveRoomsAsync(recentRooms);
            }
            
            // Return rooms sorted by most recently accessed first
            return recentRooms.OrderByDescending(r => r.LastAccessed).ToList();
        }
        catch
        {
            return Array.Empty<RecentRoom>();
        }
    }

    public async ValueTask AddRoomAsync(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return;
        }

        var rooms = (await GetRecentRoomsAsync()).ToList();
        
        // Remove existing entry for this room if it exists
        rooms.RemoveAll(r => r.RoomId == roomId);
        
        // Add the room with current timestamp
        rooms.Insert(0, new RecentRoom(roomId, DateTime.UtcNow));
        
        await SaveRoomsAsync(rooms);
    }

    public async ValueTask RemoveRoomAsync(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return;
        }

        var rooms = (await GetRecentRoomsAsync()).ToList();
        rooms.RemoveAll(r => r.RoomId == roomId);
        
        await SaveRoomsAsync(rooms);
    }

    private async ValueTask SaveRoomsAsync(List<RecentRoom> rooms)
    {
        string json = JsonSerializer.Serialize(rooms);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }
}
