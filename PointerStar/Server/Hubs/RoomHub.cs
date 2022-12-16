using Microsoft.AspNetCore.SignalR;

namespace PointerStar.Server.Hubs;

public class RoomHub : Hub
{
    public override Task OnDisconnectedAsync(Exception? exception) => base.OnDisconnectedAsync(exception);

    public override Task OnConnectedAsync() => base.OnConnectedAsync();

    [HubMethodName(nameof(JoinRoom))]
    public async Task JoinRoom(string roomId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
}
