namespace Chatur.Core.Actions;

/// <summary>How All projects orders its list (REQ-FN-076).</summary>
public enum AllProjectsOrder
{
    /// <summary>Most recently worked on first; never opened last.</summary>
    LastWorked,

    /// <summary>By name.</summary>
    Name,

    /// <summary>Most open requirements first.</summary>
    OpenRequirements,
}

/// <summary>One project's row on All projects (REQ-FN-076, REQ-UI-067).</summary>
/// <param name="ProjectId">The project's identity.</param>
/// <param name="Name">The project's display name.</param>
/// <param name="Path">Where the project is on disk.</param>
/// <param name="Phase">The latest phase its checklist files name; <see langword="null"/> when it has none.</param>
/// <param name="Requirements">How many requirements its checklists hold.</param>
/// <param name="Open">How many of them are not verified.</param>
/// <param name="Verified">How many are verified.</param>
/// <param name="LastWorkedUtc">When the project was last worked on; <see langword="null"/> if never opened.</param>
/// <param name="State"><c>active</c> when worked on in the last 14 days, <c>paused</c> when longer ago, <c>not opened</c> when never.</param>
public sealed record ProjectStanding(
    int ProjectId,
    string Name,
    string Path,
    int? Phase,
    int Requirements,
    int Open,
    int Verified,
    DateTime? LastWorkedUtc,
    string State);

/// <summary>Every project Chatur knows, and the totals above them (REQ-FN-076).</summary>
/// <param name="Projects">The projects that match the search, in order.</param>
/// <param name="TotalProjects">How many projects Chatur knows, whatever the search.</param>
/// <param name="OpenRequirements">Open requirements across all projects.</param>
/// <param name="VerifiedRequirements">Verified requirements across all projects.</param>
/// <param name="SessionsThisWeek">Sessions started in the last 7 days across all projects.</param>
public sealed record AllProjectsView(
    IReadOnlyList<ProjectStanding> Projects,
    int TotalProjects,
    int OpenRequirements,
    int VerifiedRequirements,
    int SessionsThisWeek);
