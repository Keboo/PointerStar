namespace PointerStar.Shared;

public record class RoomOptions
{
    public bool? VotesShown { get; init; }
    public bool? AutoShowVotes { get; init; }
    public DateTime? VotingTimerValue { get; init; }
}
