using Microsoft.AspNetCore.SignalR.Client;
using Polly;

namespace PointerStar.Shared;

public class RoomHubConnection
{
    public const string JoinRoomMethodName = "JoinRoom";
    public const string SubmitVoteMethodName = "SubmitVote";
    public const string RoomUpdatedMethodName = "RoomUpdated";

    public event EventHandler<RoomState>? RoomStateUpdated;

    private HubConnection HubConnection { get; }
    private string HubUrl { get; }

    public bool IsConnected => HubConnection.State == HubConnectionState.Connected;

    public RoomHubConnection(string url)
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
    {
        return HubConnection.InvokeAsync(JoinRoomMethodName, roomId, user);
    }

    public Task SubmitVoteAsync(string vote)
    {
        return HubConnection.InvokeAsync(SubmitVoteMethodName, vote);
    }
}
