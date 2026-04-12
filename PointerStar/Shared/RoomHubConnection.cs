namespace PointerStar.Shared;

public static class RoomHubConnection
{
    public const string JoinRoomMethodName = "JoinRoom";
    public const string SubmitVoteMethodName = "SubmitVote";
    public const string UpdateRoomMethodName = "UpdateRoom";
    public const string UpdateUserMethodName = "UpdateUser";
    public const string ResetVotesMethodName = "ResetVotes";
    public const string RequestResetVotesMethodName = "RequestResetVotes";
    public const string CancelResetVotesMethodName = "CancelResetVotes";
    public const string RemoveUserMethodName = "RemoveUser";
    public const string GetServerTimeMethodName = "GetServerTime";
    public const string RoomUpdatedMethodName = "RoomUpdated";
}
