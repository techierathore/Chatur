using Chatur.Core.Processes;

namespace Chatur.Core.AutoRun;

/// <summary>
/// The chain an end-to-end run goes through, in order (mockups/auto-run.html "The chain — eight
/// processes"). Every code is one of the twelve in <see cref="ProcessCatalogue"/>; the mockup's
/// <c>create-project-brief</c> and <c>devguide</c> are not among them, so <c>day1-greenfield</c> (brief,
/// BRD and checklist) and <c>mockups</c> stand in their places.
/// </summary>
public static class AutoRunChain
{
    /// <summary>The process codes, in the order they are run.</summary>
    public static readonly IReadOnlyList<string> Processes =
    [
        "day1-greenfield",
        "split-brd",
        "mockups",
        "deploy-checklist",
        "build-phase",
        "verify-phase",
        "fix-issues",
        "refresh-status"
    ];

    /// <summary>The agent that owns a process, e.g. <c>Flow master</c>.</summary>
    /// <param name="aProcessCode">The process.</param>
    public static string AgentOf(string aProcessCode) =>
        ProcessCatalogue.Find(aProcessCode)?.Agent ?? string.Empty;
}
