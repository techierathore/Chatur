namespace Chatur.Core.Actions;

/// <summary>One run on record, as the Run report picker lists it (REQ-UI-061).</summary>
/// <param name="Key">The run's identity: the time it started, exactly as its record in <c>runs.jsonl</c> names it.</param>
/// <param name="ProjectName">The project the run belongs to.</param>
/// <param name="Command">What the run was, e.g. <c>build-phase</c> or <c>auto</c>.</param>
/// <param name="StartedUtc">When the run started.</param>
/// <param name="EndedUtc">When it ended, or <see langword="null"/> when its record was never closed.</param>
public sealed record RunReportEntry(string Key, string ProjectName, string Command, DateTime StartedUtc, DateTime? EndedUtc)
{
    /// <summary>Whether the run's record was closed (it has an end time).</summary>
    public bool IsClosed => EndedUtc is not null;
}

/// <summary>A requirement the run touched and the verdict it was left at (REQ-UI-058).</summary>
/// <param name="Id">The requirement id, e.g. <c>REQ-UI-014</c>.</param>
/// <param name="Title">Its title from the project's checklist, or empty when no checklist names it.</param>
/// <param name="AgentName">The agent that owns it.</param>
/// <param name="Verdict">The verdict recorded for it in this run, or empty when none was recorded.</param>
/// <param name="AtUtc">When that verdict was recorded, or <see langword="null"/>.</param>
public sealed record RunReportRequirement(string Id, string Title, string AgentName, string Verdict, DateTime? AtUtc);

/// <summary>Whose gap a miss was (REQ-UI-059).</summary>
public enum MissOwner
{
    /// <summary>The owner's brief or the documents written from it left something unsaid.</summary>
    OwnerBrief,

    /// <summary>TrBlazeUI lacked something.</summary>
    TrBlazeUI,

    /// <summary>TechieRag lacked something.</summary>
    TechieRag,

    /// <summary>Chatur itself.</summary>
    Chatur
}

/// <summary>A miss the run recorded (REQ-UI-059).</summary>
/// <param name="MissId">The miss's id.</param>
/// <param name="What">What it was, in words.</param>
/// <param name="Note">A second line about it, or empty.</param>
/// <param name="Owner">Whose gap it was.</param>
/// <param name="RequirementId">The requirement it happened on, or empty.</param>
/// <param name="IsFixed">Whether a later record fixed it.</param>
public sealed record RunReportMiss(string MissId, string What, string Note, MissOwner Owner, string RequirementId, bool IsFixed);

/// <summary>One model's share of a run's cost (REQ-UI-060). A figure the record does not carry is <see langword="null"/>, never guessed.</summary>
/// <param name="Model">The model's name.</param>
/// <param name="Tier">The routing tier it served, or empty.</param>
/// <param name="Calls">How many calls it took, or <see langword="null"/> when not recorded.</param>
/// <param name="TokensIn">Tokens in, or <see langword="null"/>.</param>
/// <param name="TokensOut">Tokens out, or <see langword="null"/>.</param>
/// <param name="Seconds">Time it worked, or <see langword="null"/>.</param>
public sealed record RunReportCost(string Model, string Tier, long? Calls, long? TokensIn, long? TokensOut, long? Seconds);

/// <summary>One run's report: what it touched, what it missed and what it cost (BRD-131 to BRD-134).</summary>
/// <param name="Run">The run.</param>
/// <param name="Requirements">Every requirement it touched with its verdict.</param>
/// <param name="Misses">Every miss it recorded.</param>
/// <param name="Cost">The models it used.</param>
/// <param name="TotalTokens">Tokens in plus out, or <see langword="null"/> when none were recorded.</param>
/// <param name="DurationSeconds">How long the run took, or <see langword="null"/> when its record was never closed.</param>
/// <param name="WaitedSeconds">Time spent waiting on a usage limit (not counted in the duration), or <see langword="null"/> when none is recorded.</param>
/// <param name="TotalTokensIn">Tokens in across the run: the sum of the cost rows' tokens in, so the Total row and <paramref name="TotalTokens"/> cannot disagree; <see langword="null"/> when none were recorded.</param>
/// <param name="TotalTokensOut">Tokens out across the run (same rule as <paramref name="TotalTokensIn"/>), or <see langword="null"/>.</param>
/// <param name="WaitCount">How many usage-limit waits the run recorded, or <see langword="null"/> for a run whose record does not say (a run read from <c>runs.jsonl</c> only).</param>
/// <param name="RetryCount">How many times a step was tried again, or <see langword="null"/> when the record does not say.</param>
public sealed record RunReportView(
    RunReportEntry Run,
    IReadOnlyList<RunReportRequirement> Requirements,
    IReadOnlyList<RunReportMiss> Misses,
    IReadOnlyList<RunReportCost> Cost,
    long? TotalTokens,
    long? DurationSeconds,
    long? WaitedSeconds,
    long? TotalTokensIn = null,
    long? TotalTokensOut = null,
    int? WaitCount = null,
    int? RetryCount = null)
{
    /// <summary>Whether the cost figures are partial because the run's record was never closed.</summary>
    public bool IsPartial => !Run.IsClosed;
}
