namespace Chatur.Core.Licences;

/// <summary>One feature a licence can switch, as the Licence screen lists it (mockups/licence.html).</summary>
/// <param name="Code">The code the licence source files it under.</param>
/// <param name="Name">The name shown.</param>
/// <param name="Description">One line about it.</param>
public sealed record ChaturFeature(string Code, string Name, string Description);

/// <summary>The features Chatur has and every surface that offers them is gated by (REQ-FN-075).</summary>
public static class ChaturFeatures
{
    /// <summary>Auto run.</summary>
    public const string AutoRun = "auto-run";

    /// <summary>Working copies.</summary>
    public const string Worktrees = "worktrees";

    /// <summary>The verifier on real windows.</summary>
    public const string Verifier = "verifier";

    /// <summary>The board.</summary>
    public const string Board = "board";

    /// <summary>Measurements.</summary>
    public const string Measurements = "measurements";

    /// <summary>Actions for other tools.</summary>
    public const string Integrations = "integrations";

    /// <summary>Every feature, in the order the Licence screen lists them.</summary>
    public static IReadOnlyList<ChaturFeature> All { get; } =
    [
        new(AutoRun, "Auto run", "A process runs to its end without being asked each time"),
        new(Worktrees, "Working copies", "Each run gets its own copy of the repository"),
        new(Verifier, "The verifier on real windows", "Checks the built application by driving its windows"),
        new(Board, "The board", "Requirements as cards, moved between columns"),
        new(Measurements, "Measurements", "Runs, misses and token counts kept over time"),
        new(Integrations, "Actions for other tools", "Chatur's actions offered over an address")
    ];
}
