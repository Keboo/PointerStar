namespace PointerStar.Shared;

public interface IRoomHubConnection
{
    event EventHandler<RoomState>? RoomStateUpdated;

    bool IsConnected { get; }

    Task OpenAsync();
    Task JoinRoomAsync(string roomId, User user);
    Task SubmitVoteAsync(string vote);
    Task ShowVotesAsync(bool areVotesShown);
    Task ResetVotesAsync();
}
