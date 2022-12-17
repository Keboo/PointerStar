using Microsoft.AspNetCore.SignalR;
using PointerStar.Server.Room;
using PointerStar.Shared;

namespace PointerStar.Server.Hubs;

public class RoomHub : Hub
{
    private IRoomManager RoomManager { get; }

    public RoomHub(IRoomManager roomManager)
    {
        RoomManager = roomManager ?? throw new ArgumentNullException(nameof(roomManager));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        RoomState? room = await RoomManager.DisconnectAsync(Context.ConnectionId);
        if (room?.RoomId is { } roomId)
        {
            await Clients.Group(roomId).SendAsync(RoomHubConnection.RoomUpdatedMethodName, room);
        }
        await base.OnDisconnectedAsync(exception);
    }

    //public override Task OnConnectedAsync()
    //{
    //    return base.OnConnectedAsync();
    //}

    [HubMethodName(RoomHubConnection.JoinRoomMethodName)]
    public async Task JoinRoomAsync(string roomId, User user)
    {
        RoomState roomState = await RoomManager.AddUserToRoomAsync(roomId, user, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await Clients.Groups(roomId).SendAsync(RoomHubConnection.RoomUpdatedMethodName, roomState);
    }

    [HubMethodName(RoomHubConnection.SubmitVoteMethodName)]
    public async Task SubmitVoteAsync(string vote)
    {
        RoomState? roomState = await RoomManager.SubmitVoteAsync(vote, Context.ConnectionId);
        if (roomState?.RoomId is { } roomId)
        {
            await Clients.Groups(roomId).SendAsync(RoomHubConnection.RoomUpdatedMethodName, roomState);
        }
    }
}
