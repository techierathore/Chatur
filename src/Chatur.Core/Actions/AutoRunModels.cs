namespace Chatur.Core.Actions;

/// <summary>Where an end-to-end run stands (BRD-118 to BRD-125).</summary>
public enum AutoRunState
{
    /// <summary>The chain is being carried on.</summary>
    Running,

    /// <summary>A model reported a usage limit; the run waits and carries on by itself at <see cref="AutoRunView.ResumeAtUtc"/> (REQ-FN-064).</summary>
    WaitingForLimit,

    /// <summary>The owner paused the run; it carries on from the same step when resumed.</summary>
    Paused,

    /// <summary>The run ended before the chain did: the owner stopped it, or a step failed past its retries. <see cref="AutoRunView.StopReason"/> says why.</summary>
    Stopped,

    /// <summary>The chain finished. The run reads ready for the owner's acceptance test and nothing further is run (REQ-FN-062).</summary>
    ReadyForAcceptance,

    /// <summary>The owner accepted and no requirement was open (REQ-FN-066).</summary>
    Complete
}

/// <summary>Where one process of the chain stands.</summary>
public enum AutoRunProcessState
{
    /// <summary>Not reached yet.</summary>
    ToCome,

    /// <summary>The process being run.</summary>
    Running,

    /// <summary>The process finished.</summary>
    Finished,

    /// <summary>The process stopped on a failed check, or the run was stopped on it.</summary>
    Stopped
}

/// <summary>What an entry in a run's record is.</summary>
public enum AutoRunEventKind
{
    /// <summary>The run began.</summary>
    Started,

    /// <summary>A process of the chain began; the agent that owns it is started (REQ-UI-055).</summary>
    ProcessStarted,

    /// <summary>A process of the chain finished.</summary>
    ProcessFinished,

    /// <summary>A model reported a usage limit and the run began to wait (REQ-FN-064).</summary>
    LimitWait,

    /// <summary>The wait ended and the run carried on by itself on the same step (REQ-FN-064).</summary>
    LimitResumed,

    /// <summary>A step stalled or the work crashed and was tried again (REQ-FN-065).</summary>
    Retry,

    /// <summary>The owner paused the run.</summary>
    Paused,

    /// <summary>The owner resumed the run.</summary>
    Resumed,

    /// <summary>An owner stop inside the chain was answered by the run itself.</summary>
    OwnerStopAnswered,

    /// <summary>The run ended before the chain did; the message is the reason.</summary>
    Stopped,

    /// <summary>The chain finished and the run is ready for the owner's acceptance test (REQ-FN-062).</summary>
    ReadyForAcceptance,

    /// <summary>The owner marked the run complete (REQ-FN-066).</summary>
    Completed
}

/// <summary>The brief's rule, shared with the screen.</summary>
public static class AutoRunBrief
{
    /// <summary>The fewest characters a brief may have once trimmed ("at least a sentence"; the mockup's <c>minlength</c>).</summary>
    public const int MinLength = 40;
}

/// <summary>One process of the chain with its state (REQ-FN-061, REQ-UI-055).</summary>
/// <param name="Position">The place in the chain, from 1.</param>
/// <param name="ProcessCode">The process, e.g. <c>build-phase</c>.</param>
/// <param name="Agent">The agent that owns it, e.g. <c>Flow master</c>.</param>
/// <param name="State">Where it stands.</param>
/// <param name="ProcessRunId">The process run (<see cref="IProcessRunActions"/>) that carried it out, or <see langword="null"/> before it began.</param>
/// <param name="Retries">How many times a step of it was tried again (REQ-FN-065).</param>
public sealed record AutoRunProcessView(
    int Position,
    string ProcessCode,
    string Agent,
    AutoRunProcessState State,
    int? ProcessRunId,
    int Retries);

/// <summary>One entry in a run's record: what happened, when, and on which process (REQ-FN-064, REQ-FN-065).</summary>
/// <param name="EventId">The entry's id.</param>
/// <param name="AtUtc">When it happened.</param>
/// <param name="Kind">What it is.</param>
/// <param name="ProcessPosition">The place in the chain it concerns, or <see langword="null"/>.</param>
/// <param name="ProcessCode">The process it concerns, or <see langword="null"/>.</param>
/// <param name="Message">One plain sentence saying what happened and why.</param>
/// <param name="UntilUtc">For a <see cref="AutoRunEventKind.LimitWait"/>, when the wait ends; otherwise <see langword="null"/>.</param>
public sealed record AutoRunEvent(
    int EventId,
    DateTime AtUtc,
    AutoRunEventKind Kind,
    int? ProcessPosition,
    string? ProcessCode,
    string Message,
    DateTime? UntilUtc);

/// <summary>What is happening now in a run that is going (REQ-UI-056).</summary>
/// <param name="Agent">The agent working, e.g. <c>Flow master</c>.</param>
/// <param name="ProcessCode">The process being run.</param>
/// <param name="ProcessPosition">Its place in the chain, from 1.</param>
/// <param name="ProcessCount">The number of processes in the chain.</param>
/// <param name="StepPosition">The step the process is on, from 1; 0 before the process has begun.</param>
/// <param name="StepWording">The step's wording, or empty.</param>
/// <param name="RequirementId">The open requirement furthest from verified (lowest percent first), or <see langword="null"/> when none is open.</param>
/// <param name="RequirementTitle">That requirement's title, or empty.</param>
/// <param name="Model">The model the working agent uses, or <see langword="null"/> when nothing the run records names one (no step of a run calls a model yet, so it is null today; never guessed).</param>
public sealed record AutoRunNow(
    string Agent,
    string ProcessCode,
    int ProcessPosition,
    int ProcessCount,
    int StepPosition,
    string StepWording,
    string? RequirementId,
    string RequirementTitle,
    string? Model = null);

/// <summary>One process of the chain a run would go through, before any run exists (REQ-UI-055).</summary>
/// <param name="Position">The place in the chain, from 1.</param>
/// <param name="ProcessCode">The process, e.g. <c>build-phase</c>.</param>
/// <param name="Agent">The agent that runs it, e.g. <c>Flow master</c>.</param>
public sealed record AutoRunChainStep(int Position, string ProcessCode, string Agent);

/// <summary>The project's requirements that are not verified, by where they stand (REQ-FN-066).</summary>
/// <param name="Total">All open requirements: the three below added up.</param>
/// <param name="NotStarted">Requirements still <c>Not Started</c>.</param>
/// <param name="InHand">Requirements in progress, implemented, partial or needing re-verify.</param>
/// <param name="SentBack">Requirements blocked or failed.</param>
public sealed record AutoRunOpenCounts(int Total, int NotStarted, int InHand, int SentBack);

/// <summary>An end-to-end run as the screen shows it.</summary>
/// <param name="AutoRunId">The run's id.</param>
/// <param name="ProjectId">The project it works on.</param>
/// <param name="ProjectName">The project's display name.</param>
/// <param name="Brief">The brief the owner gave.</param>
/// <param name="State">Where the run stands.</param>
/// <param name="StateText">The state in the owner's words, e.g. <c>ready for your acceptance test</c>.</param>
/// <param name="StartedUtc">When the run began.</param>
/// <param name="EndedUtc">When the run stopped, became ready for acceptance or completed; <see langword="null"/> while it has not.</param>
/// <param name="StopReason">Why a <see cref="AutoRunState.Stopped"/> run stopped; otherwise <see langword="null"/>.</param>
/// <param name="ResumeAtUtc">For a <see cref="AutoRunState.WaitingForLimit"/> run, when it carries on; otherwise <see langword="null"/>.</param>
/// <param name="ProgressPercent">How far through the chain, 0 to 100: finished processes plus the share of the current one.</param>
/// <param name="Chain">Every process of the chain, in order.</param>
/// <param name="Now">What is happening now, or <see langword="null"/> when the run is not going.</param>
/// <param name="Open">The project's open requirements, read from its checklists just now.</param>
/// <param name="CanBeCompleted"><see langword="true"/> only when the run is ready for acceptance and no requirement is open.</param>
/// <param name="Events">The run's record, oldest first: every wait, every retry, every process started.</param>
public sealed record AutoRunView(
    int AutoRunId,
    int ProjectId,
    string ProjectName,
    string Brief,
    AutoRunState State,
    string StateText,
    DateTime StartedUtc,
    DateTime? EndedUtc,
    string? StopReason,
    DateTime? ResumeAtUtc,
    int ProgressPercent,
    IReadOnlyList<AutoRunProcessView> Chain,
    AutoRunNow? Now,
    AutoRunOpenCounts Open,
    bool CanBeCompleted,
    IReadOnlyList<AutoRunEvent> Events);

/// <summary>One line of a project's end-to-end run history.</summary>
/// <param name="AutoRunId">The run's id.</param>
/// <param name="Brief">The brief, as given.</param>
/// <param name="State">Where the run stands.</param>
/// <param name="StartedUtc">When it began.</param>
/// <param name="EndedUtc">When it ended, or <see langword="null"/>.</param>
public sealed record AutoRunSummary(
    int AutoRunId,
    string Brief,
    AutoRunState State,
    DateTime StartedUtc,
    DateTime? EndedUtc);
