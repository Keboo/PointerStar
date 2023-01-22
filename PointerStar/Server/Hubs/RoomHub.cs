using Microsoft.AspNetCore.SignalR;
using PointerStar.Server.Room;
using PointerStar.Shared;
using Timer = System.Timers.Timer;

namespace PointerStar.Server.Hubs;

public class RoomHub : Hub
{
    private IRoomManager RoomManager { get; }
    private Timer VotingTimer { get; }

    public RoomHub(IRoomManager roomManager)
    {
        RoomManager = roomManager ?? throw new ArgumentNullException(nameof(roomManager));
        // TODO: I think we need to unsubscribe and dispose the timer but Hub already implements IDisposable...
        VotingTimer = new();
        VotingTimer.Elapsed += VotingTimer_Elapsed;
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
            VotingTimer.Start();
        }
    }

    [HubMethodName(RoomHubConnection.UpdateRoomMethodName)]
    public async Task UpdateRoomAsync(RoomOptions roomOptions)
    {
        RoomState? roomState = await RoomManager.UpdateRoomAsync(roomOptions, Context.ConnectionId);
        if (roomState?.RoomId is { } roomId)
        {
            await Clients.Groups(roomId).SendAsync(RoomHubConnection.RoomUpdatedMethodName, roomState);
        }
    }

    [HubMethodName(RoomHubConnection.UpdateUserMethodName)]
    public async Task UpdateUserAsync(UserOptions userOptions)
    {
        RoomState? roomState = await RoomManager.UpdateUserAsync(userOptions, Context.ConnectionId);
        if (roomState?.RoomId is { } roomId)
        {
            await Clients.Groups(roomId).SendAsync(RoomHubConnection.RoomUpdatedMethodName, roomState);
        }
    }

    [HubMethodName(RoomHubConnection.ResetVotesMethodName)]
    public async Task ResetVotesAsync()
    {
        RoomState? roomState = await RoomManager.ResetVotesAsync(Context.ConnectionId);
        if (roomState?.RoomId is { } roomId)
        {
            VotingTimer.Stop();
            await Clients.Groups(roomId).SendAsync(RoomHubConnection.RoomUpdatedMethodName, roomState);
        }
    }

    private async void VotingTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e) =>
        await UpdateRoomAsync(new RoomOptions { VotingTimerValue = e.SignalTime });
}
