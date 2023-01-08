using Microsoft.AspNetCore.SignalR.Client;
using Polly;

namespace PointerStar.Shared;

public class RoomHubConnection : IRoomHubConnection
{
    public const string JoinRoomMethodName = "JoinRoom";
    public const string SubmitVoteMethodName = "SubmitVote";
    public const string UpdateRoomMethodName = "UpdateRoom";
    public const string UpdateUserMethodName = "UpdateUser";
    public const string ResetVotesMethodName = "ResetVotes";
    public const string RoomUpdatedMethodName = "RoomUpdated";
    public const string StartVotingMethodName = "StartVoting";
    public const string StopVotingMethodName = "StopVoting";

    public event EventHandler<RoomState>? RoomStateUpdated;

    private HubConnection HubConnection { get; }
    private Uri HubUrl { get; }

    public bool IsConnected => HubConnection.State == HubConnectionState.Connected;

    public RoomHubConnection(Uri url)
    {
        HubUrl = url;
        HubConnection = new HubConnectionBuilder()
            .WithUrl(HubUrl, options => { })
            .WithAutomaticReconnect()
            .Build();

        HubConnection.On<RoomState>(RoomUpdatedMethodName,
           (roomState) => RoomStateUpdated?.Invoke(this, roomState)
        );
    }

    public async Task OpenAsync()
    {
        var pauseBetweenFailures = TimeSpan.FromSeconds(20);
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryForeverAsync(
                i => pauseBetweenFailures,
                (exception, timeSpan) =>
                {
                    //Console.Error.WriteLine(
                    //    $"Error connecting to SignalR hub {HubUrl} - {exception.Message}"
                    //);
                }
            );
        await retryPolicy.ExecuteAsync(
            async () =>
            {
                await HubConnection.StartAsync();
            }
        );
    }

    public Task JoinRoomAsync(string roomId, User user)
        => HubConnection.InvokeAsync(JoinRoomMethodName, roomId, user);

    public Task SubmitVoteAsync(string vote)
        => HubConnection.InvokeAsync(SubmitVoteMethodName, vote);

    public Task UpdateRoomAsync(RoomOptions roomOptions)
        => HubConnection.InvokeAsync(UpdateRoomMethodName, roomOptions);

    public Task UpdateUserAsync(UserOptions userOptions)
        => HubConnection.InvokeAsync(UpdateUserMethodName, userOptions);

    public Task ResetVotesAsync()
        => HubConnection.InvokeAsync(ResetVotesMethodName);

    public Task StartVotingAsync() => HubConnection.InvokeAsync(StartVotingMethodName);

    public Task StopVotingAsync() => HubConnection.InvokeAsync(StopVotingMethodName);
}
