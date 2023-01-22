namespace PointerStar.Shared;

public record class RoomState(string RoomId, User[] Users)
{
    public IReadOnlyList<User> TeamMemebers
        => Users.Where(u => u.Role == Role.TeamMember).ToList();

    public IReadOnlyList<User> Facilitators
        => Users.Where(u => u.Role == Role.Facilitator).ToList();

    public IReadOnlyList<User> Observers
        => Users.Where(u => u.Role == Role.Observer).ToList();

    public bool VotesShown { get; init; }
    public bool AutoShowVotes { get; init; }
    
    public string[] VoteOptions { get; init; } = new[]
    {
        "1",
        "2",
        "3",
        "5",
        "8",
        "13",
        "21",
        "34",
        "55",
        "89",
        "Abstain",
        "?"
    };
    public DateTime? VoteStartTime { get; init; }
}

public record class User(Guid Id, string Name)
{
    public string? OriginalVote { get; init; }
    public string? Vote { get; init; }
    public Role Role { get; init; } = Role.TeamMember;
}

public record class Role(Guid Id, string Name)
{
    private static readonly Guid FacilitatorId = new("5fea7d71-fb62-405c-823c-09752c684bf0");
    private static readonly Guid TeamMemberId = new("116b133b-b16d-4a92-a3ce-ae53688e973c");
    private static readonly Guid ObserverId = new("a0fec1ad-caee-4fa0-8d93-d0ce970f92d7");
    
    public static Role Facilitator { get; } = new(FacilitatorId, "Facilitator");
    public static Role TeamMember { get; } = new(TeamMemberId, "Team Member");
    public static Role Observer { get; } = new(ObserverId, "Observer");

    public static Role? FromId(Guid roleId)
    {
        if (roleId == FacilitatorId)
        {
            return Facilitator;
        }

        if (roleId == TeamMemberId)
        {
            return TeamMember;
        }

        if (roleId == ObserverId)
        {
            return Observer;
        }

        return null;
    }
}

