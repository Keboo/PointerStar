using Microsoft.AspNetCore.SignalR.Client;
using Polly;

namespace PointerStar.Shared;

public class RoomHubConnection : IRoomHubConnection
{
    public const string JoinRoomMethodName = "JoinRoom";
    public const string SubmitVoteMethodName = "SubmitVote";
    public const string ShowVotesMethodName = "ShowVotes";
    public const string RoomUpdatedMethodName = "RoomUpdated";

    public event EventHandler<RoomState>? RoomStateUpdated;

    private HubConnection HubConnection { get; }
    private Uri HubUrl { get; }

    public bool IsConnected => HubConnection.State == HubConnectionState.Connected;

    public RoomHubConnection(Uri url)
    {
        HubUrl = url;
        HubConnection = new HubConnectionBuilder()
            .WithUrl(HubUrl, options => { })
            .Build();

        HubConnection.On<RoomState>(RoomUpdatedMethodName,
           (roomState) => RoomStateUpdated?.Invoke(this, roomState)
        );

        HubConnection.Closed += async (error) =>
        {
            await OpenAsync();
        };
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
                await TryOpen();
            }
        );
        async Task<bool> TryOpen()
        {
            await HubConnection.StartAsync();
            return true;
        }
    }

    public Task JoinRoomAsync(string roomId, User user)
        => HubConnection.InvokeAsync(JoinRoomMethodName, roomId, user);

    public Task SubmitVoteAsync(string vote)
        => HubConnection.InvokeAsync(SubmitVoteMethodName, vote);

    public Task ShowVotesAsync(bool areVotesShown)
        => HubConnection.InvokeAsync(ShowVotesMethodName, areVotesShown);
}
