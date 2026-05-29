namespace PointerStar.Shared;

/// <summary>
/// Voting mode for a pointing poker room.
/// </summary>
public enum VotingMode
{
    /// <summary>Standard numeric voting (Fibonacci, T-shirt sizes, etc.)</summary>
    Standard = 0,

    /// <summary>Giphy image voting (cosmetic/icebreaker mode).</summary>
    Giphy = 1
}
