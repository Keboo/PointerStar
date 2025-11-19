namespace PointerStar.Shared;

/// <summary>
/// Pre-defined voting option presets for common pointing poker scales.
/// </summary>
public static class VotingPresets
{
    public static string[] Fibonacci { get; } =
    [
        "1",
        "2",
        "3",
        "5",
        "8",
        "13",
        "21",
        "Abstain",
        "?"
    ];

    public static string[] LinearOneToFive { get; } =
    [
        "1",
        "2",
        "3",
        "4",
        "5",
        "Abstain",
        "?"
    ];

    public static string[] LinearOneToTen { get; } =
    [
        "1",
        "2",
        "3",
        "4",
        "5",
        "6",
        "7",
        "8",
        "9",
        "10",
        "Abstain",
        "?"
    ];

    public static string[] TShirtSizes { get; } =
    [
        "XS",
        "S",
        "M",
        "L",
        "XL",
        "XXL",
        "Abstain",
        "?"
    ];

    public static IReadOnlyList<(string Name, string[] Options)> AllPresets { get; } =
    [
        ("Fibonacci (1-21)", Fibonacci),
        ("Linear (1-5)", LinearOneToFive),
        ("Linear (1-10)", LinearOneToTen),
        ("T-Shirt Sizes", TShirtSizes)
    ];
}
