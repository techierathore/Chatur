using Chatur.Core.Actions;

namespace Chatur.Core.ProcessRuns;

/// <summary>One test a test tool reported, with the requirement ids its name carries.</summary>
/// <param name="Source">The tool that reported it, <c>dotnet</c> or <c>playwright</c>.</param>
/// <param name="Name">Its name, display name or title.</param>
/// <param name="Outcome">How it came out.</param>
/// <param name="Requirements">The requirement ids found in the name.</param>
/// <param name="Message">The failure message, or <see langword="null"/>.</param>
/// <param name="DurationMs">How long it took, in milliseconds.</param>
public sealed record ReportedTest(
    string Source,
    string Name,
    ProcessTestOutcome Outcome,
    IReadOnlyList<string> Requirements,
    string? Message,
    long DurationMs);
