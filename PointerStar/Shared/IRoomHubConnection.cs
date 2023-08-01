namespace PointerStar.Shared;

public interface IRoomHubConnection
{
    event EventHandler<RoomState>? RoomStateUpdated;

    bool IsConnected { get; }

    Task OpenAsync();
    Task JoinRoomAsync(string roomId, User user);
    Task SubmitVoteAsync(string vote);
    Task UpdateRoomAsync(RoomOptions roomOptions);
    Task UpdateUserAsync(UserOptions userOptions);
    Task ResetVotesAsync();
    Task RemoveUserAsync(Guid userId);
}
