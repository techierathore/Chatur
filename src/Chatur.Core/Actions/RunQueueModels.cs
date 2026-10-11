namespace Chatur.Core.Actions;

/// <summary>Where a queued run is (page Run queue, <c>/auto/queue</c>; REQ-FN-067, REQ-FN-068, REQ-FN-069).</summary>
public enum RunQueueState
{
    /// <summary>Queued and not started; it takes its turn when the run above it ends.</summary>
    Waiting,

    /// <summary>Its end-to-end run is going now (running, or waiting out a usage limit).</summary>
    Running,

    /// <summary>Started and then paused by the owner; resuming carries it on from its step.</summary>
    Paused,

    /// <summary>Its end-to-end run has ended; the row is kept for its report.</summary>
    Finished,

    /// <summary>Its end-to-end run cannot be found any more; the row can still be removed.</summary>
    Lost,
}

/// <summary>One row of the queue.</summary>
/// <param name="QueueItemId">The row's identity.</param>
/// <param name="Position">1-based place among the runs not yet finished, or <see langword="null"/> for a finished run.</param>
/// <param name="ProjectId">The project the run is on.</param>
/// <param name="ProjectName">The project's display name.</param>
/// <param name="Brief">What the run should achieve, as typed.</param>
/// <param name="BriefShort">The brief cut to one line for the table.</param>
/// <param name="State">Where the run is.</param>
/// <param name="StartedUtc">When it started, or <see langword="null"/> while it waits.</param>
/// <param name="Elapsed">How long it has taken (to now, or to its end), or <see langword="null"/> while it waits.</param>
/// <param name="AutoRunId">The end-to-end run behind it, or <see langword="null"/> while it waits.</param>
/// <param name="ResumeFocus">The id of the open requirement the last resume took first (REQ-FN-070), or <see langword="null"/>.</param>
/// <param name="CanPause">Whether Pause applies to this row.</param>
/// <param name="CanResume">Whether Resume applies to this row.</param>
/// <param name="CanRemove">Whether Remove applies to this row.</param>
public sealed record RunQueueItem(
    int QueueItemId,
    int? Position,
    int ProjectId,
    string ProjectName,
    string Brief,
    string BriefShort,
    RunQueueState State,
    DateTime? StartedUtc,
    TimeSpan? Elapsed,
    int? AutoRunId,
    string? ResumeFocus,
    bool CanPause,
    bool CanResume,
    bool CanRemove);

/// <summary>The whole queue: every row, and the counts the heading shows.</summary>
/// <param name="Items">Unfinished runs in queue order, then finished runs newest first.</param>
/// <param name="RunningCount">How many runs are going now (at most one).</param>
/// <param name="WaitingCount">How many are queued and not started.</param>
public sealed record RunQueueView(IReadOnlyList<RunQueueItem> Items, int RunningCount, int WaitingCount);

/// <summary>The result of queueing a run.</summary>
/// <param name="Item">The new row, at the end of the queue.</param>
/// <param name="ProjectAlreadyQueued">Whether the project already had an unfinished run in the queue; a warning, not a refusal.</param>
public sealed record RunQueueAdded(RunQueueItem Item, bool ProjectAlreadyQueued);

/// <summary>The most a queued brief may be, and how it is shortened for the table.</summary>
public static class RunQueueBrief
{
    /// <summary>The longest brief the queue keeps.</summary>
    public const int MaxLength = 4000;

    /// <summary>The longest brief the table shows on one line.</summary>
    public const int ShortLength = 60;
}
