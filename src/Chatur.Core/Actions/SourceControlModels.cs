namespace Chatur.Core.Actions;

/// <summary>One changed file, as the Repository screen lists it (REQ-UI-042).</summary>
/// <param name="Path">The file's path relative to the repository root.</param>
/// <param name="State">The file's state: <c>"modified"</c>, <c>"added"</c> or <c>"deleted"</c>.</param>
/// <param name="LinesAdded">Lines added since the last check-in.</param>
/// <param name="LinesRemoved">Lines removed since the last check-in.</param>
public sealed record ChangedFile(string Path, string State, int LinesAdded, int LinesRemoved);

/// <summary>
/// The branch checked out right now and how far it is from its upstream — shown on Repository and
/// refreshed after a push or a pull (REQ-UI-044).
/// </summary>
/// <param name="Branch">The branch checked out right now.</param>
/// <param name="Ahead">Check-ins on this branch that are not yet on the upstream.</param>
/// <param name="Behind">Check-ins on the upstream that are not yet on this branch.</param>
public sealed record SourceControlStatus(string Branch, int Ahead, int Behind);

/// <summary>One check-in in the Repository's history list (BRD §4 Repository "history").</summary>
/// <param name="Hash">The check-in's short identifier.</param>
/// <param name="Message">The first line of the check-in's message.</param>
/// <param name="Author">Who made the check-in.</param>
/// <param name="WhenUtc">When the check-in was made.</param>
/// <param name="WrittenByProcess">Whether the check-in exists only on a branch a process made, so it is not yet part of any branch the owner works on.</param>
public sealed record CommitEntry(string Hash, string Message, string Author, DateTime WhenUtc, bool WrittenByProcess);

/// <summary>
/// A branch a process checked in to by itself, with what it carries (UI Design "Screen:
/// Repository", <c>agent-checkins</c>; REQ-FN-048).
/// </summary>
/// <param name="Name">The branch's name, e.g. <c>run/REQ-FN-048</c>.</param>
/// <param name="RequirementIds">The requirement ids the branch's check-ins name, in the order first met.</param>
/// <param name="CheckInCount">How many check-ins the branch holds that no other branch does.</param>
/// <param name="LastCheckInUtc">When the branch was last checked in to.</param>
public sealed record ProcessBranch(string Name, IReadOnlyList<string> RequirementIds, int CheckInCount, DateTime LastCheckInUtc);

/// <summary>The branches the picker at the foot of Files offers (BRD-222).</summary>
/// <param name="Current">The branch checked out now; empty when none (a detached head).</param>
/// <param name="Local">The repository's own branches.</param>
/// <param name="Remote">The remotes' branches, as <c>origin/name</c>, without the remotes' <c>HEAD</c> pointers.</param>
public sealed record BranchList(string Current, IReadOnlyList<string> Local, IReadOnlyList<string> Remote);

/// <summary>What to do with uncommitted changes when switching branch (BRD-223).</summary>
public enum BranchSwitchChanges
{
    /// <summary>Bring them along: git carries them to the new branch, and refuses when they would be overwritten.</summary>
    BringAlong,

    /// <summary>Put them aside: they are stashed under a name saying where they came from, then the branch switches.</summary>
    PutAside
}

