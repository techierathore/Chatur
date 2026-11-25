namespace Chatur.Core.Actions;

/// <summary>The periods the Measurements screen offers (BRD-254, REQ-UI-116). Words as TfLens shows them where it has them.</summary>
public enum MetricsPeriodKind
{
    /// <summary>"All history": every record, dated or not.</summary>
    All,

    /// <summary>"Today": records dated today, in this machine's time zone.</summary>
    Today,

    /// <summary>"Last 7 days": records dated from the start of the day six days ago up to now.</summary>
    Last7Days,

    /// <summary>"Last 30 days": records dated from the start of the day 29 days ago up to now.</summary>
    Last30Days,

    /// <summary>"Between two dates": records dated from the start of <see cref="MetricsPeriod.From"/> to the end of <see cref="MetricsPeriod.To"/>, both included.</summary>
    Between
}

/// <summary>A period the figures are counted over (REQ-UI-116). A record is dated by its <c>ts</c>, or by <c>at</c> in a record Chatur wrote itself.</summary>
/// <param name="Kind">Which period.</param>
/// <param name="From">The first day, for <see cref="MetricsPeriodKind.Between"/>.</param>
/// <param name="To">The last day, for <see cref="MetricsPeriodKind.Between"/>.</param>
public sealed record MetricsPeriod(MetricsPeriodKind Kind, DateOnly? From = null, DateOnly? To = null)
{
    /// <summary>Every record.</summary>
    public static MetricsPeriod AllHistory { get; } = new(MetricsPeriodKind.All);
}

/// <summary>
/// One total, kept with how many records could not give it, so "not measured" is shown rather than a zero
/// (BRD-261, REQ-NFR-014). <see cref="Value"/> is <see langword="null"/> when no record measured it.
/// </summary>
/// <param name="Value">The sum over the records that measured it, or <see langword="null"/> when none did.</param>
/// <param name="Measured">How many records gave a value.</param>
/// <param name="NotMeasured">How many records in the period did not.</param>
public sealed record MeasuredSum(long? Value, int Measured, int NotMeasured);

/// <summary>The cost, from <c>cost_usd</c> only: never a rate card, never a guess (BRD-255, REQ-UI-117).</summary>
/// <param name="Usd">The sum of the recorded <c>cost_usd</c>, or <see langword="null"/> when no run in the period carries one ("no price").</param>
/// <param name="Priced">How many runs carry a <c>cost_usd</c>.</param>
/// <param name="NoPrice">How many runs carry none.</param>
public sealed record MeasuredCost(decimal? Usd, int Priced, int NoPrice);

/// <summary>Runs, the four token kinds, the cost and the wall clock of a set of runs (BRD-255).</summary>
/// <param name="Runs">How many runs.</param>
/// <param name="TokensIn">The sum of <c>tokens_in</c> ("Tokens in").</param>
/// <param name="TokensOut">The sum of <c>tokens_out</c> ("Tokens out").</param>
/// <param name="CacheRead">The sum of <c>tokens_cache_read</c> ("Cache read").</param>
/// <param name="CacheWrite">The sum of <c>tokens_cache_write</c> ("Cache write").</param>
/// <param name="Cost">The cost ("Measured USD", or "no price").</param>
/// <param name="WallClockS">The wall clock in seconds: <c>duration_s</c>, else started to ended, else started to ts.</param>
public sealed record RunTotals(
    int Runs,
    MeasuredSum TokensIn,
    MeasuredSum TokensOut,
    MeasuredSum CacheRead,
    MeasuredSum CacheWrite,
    MeasuredCost Cost,
    MeasuredSum WallClockS);

/// <summary>One line of a split: a model, an agent or a command phase, and its totals.</summary>
/// <param name="Key">The model id, the agent's name or the command; <c>"not measured"</c> when the records do not say.</param>
/// <param name="Totals">That line's totals.</param>
public sealed record RunSplitLine(string Key, RunTotals Totals);

/// <summary>One miss in the period (BRD-256, REQ-UI-118).</summary>
/// <param name="MissId">Its <c>miss_id</c>.</param>
/// <param name="ReqId">The row it belongs to (<c>req_id</c>), or <see langword="null"/> when the record names none.</param>
/// <param name="FoundBy">The raw <c>found_by</c> value (gate, self-smoke, agent-review, owner, production, library-feedback), or <see langword="null"/>.</param>
/// <param name="FoundGate">The <c>found_gate</c>, when a gate is named.</param>
/// <param name="MissClass">Its <c>miss_class</c>.</param>
/// <param name="Severity">Its <c>severity</c>.</param>
/// <param name="What">What happened, in the record's words, when it says.</param>
/// <param name="Status">"open", "Verified" or "wont-fix", from the latest fix by ts then fix_attempt.</param>
/// <param name="AtUtc">When it was recorded.</param>
public sealed record MissLine(
    string MissId,
    string? ReqId,
    string? FoundBy,
    string? FoundGate,
    string? MissClass,
    string? Severity,
    string? What,
    string Status,
    DateTime? AtUtc);

/// <summary>Misses and first-pass figures for the period (BRD-256, REQ-UI-118).</summary>
/// <param name="Misses">Every miss in the period (kind <c>miss</c>, amends folded in, voided ones left out), newest first.</param>
/// <param name="ByFoundBy">How many misses each <c>found_by</c> value found; a miss with none is counted under <c>"not measured"</c>.</param>
/// <param name="ByRow">How many misses each row (<c>req_id</c>) had, most first.</param>
/// <param name="RowsGraded">How many distinct rows (app, req_id) the gates graded in the period.</param>
/// <param name="RowsFirstPass">How many of them passed the first time: a gate record with attempt 1 and verdict Verified.</param>
public sealed record MissFigures(
    IReadOnlyList<MissLine> Misses,
    IReadOnlyList<KeyValuePair<string, int>> ByFoundBy,
    IReadOnlyList<KeyValuePair<string, int>> ByRow,
    int RowsGraded,
    int RowsFirstPass);

/// <summary>One run record, for the runs list and its detail (BRD-258, REQ-UI-120).</summary>
/// <param name="Line">The record's line number in runs.jsonl, from 1: its key in this list.</param>
/// <param name="AtUtc">Its <c>ts</c> (or <c>at</c>).</param>
/// <param name="Command">Its <c>cmd</c> ("Command phase"), or <c>"not measured"</c>.</param>
/// <param name="Mode">Its <c>mode</c>, when given.</param>
/// <param name="Agent">The agent the command belongs to, from Chatur's command list, or <c>"not measured"</c>.</param>
/// <param name="Model">Its <c>model</c>, or <c>"not measured"</c>.</param>
/// <param name="TokensIn">tokens_in, or <see langword="null"/> when not measured.</param>
/// <param name="TokensOut">tokens_out, or <see langword="null"/>.</param>
/// <param name="CacheRead">tokens_cache_read, or <see langword="null"/>.</param>
/// <param name="CacheWrite">tokens_cache_write, or <see langword="null"/>.</param>
/// <param name="CostUsd">cost_usd, or <see langword="null"/> ("no price").</param>
/// <param name="WallClockS">The wall clock in seconds, or <see langword="null"/>.</param>
/// <param name="Result">Its <c>build_result</c> (pass, fail, not-run), or <see langword="null"/>.</param>
/// <param name="RowsTouched">The rows it touched (<c>reqs_touched</c>), possibly empty.</param>
/// <param name="Raw">The record as written, laid out for reading.</param>
public sealed record RunLine(
    int Line,
    DateTime? AtUtc,
    string Command,
    string? Mode,
    string Agent,
    string Model,
    long? TokensIn,
    long? TokensOut,
    long? CacheRead,
    long? CacheWrite,
    decimal? CostUsd,
    long? WallClockS,
    string? Result,
    IReadOnlyList<string> RowsTouched,
    string Raw);

/// <summary>Everything the Measurements screen shows for one project and one period (BRD-253 to BRD-258).</summary>
/// <param name="ProjectId">The project.</param>
/// <param name="ProjectName">Its name.</param>
/// <param name="MetricsFolder">Its <c>docs/metrics</c> folder.</param>
/// <param name="FolderFound">Whether that folder exists.</param>
/// <param name="Period">The period counted.</param>
/// <param name="Totals">Every run in the period, added up.</param>
/// <param name="ByModel">Split by <c>model</c>, most tokens out first.</param>
/// <param name="ByAgent">Split by the agent owning the command.</param>
/// <param name="ByCommand">Split by <c>cmd</c>.</param>
/// <param name="Misses">Misses and first-pass figures.</param>
/// <param name="Runs">Every run in the period, newest first.</param>
/// <param name="UnreadableLines">Lines no stream could read (not JSON); counted, never guessed at.</param>
public sealed record MetricsReport(
    int ProjectId,
    string ProjectName,
    string MetricsFolder,
    bool FolderFound,
    MetricsPeriod Period,
    RunTotals Totals,
    IReadOnlyList<RunSplitLine> ByModel,
    IReadOnlyList<RunSplitLine> ByAgent,
    IReadOnlyList<RunSplitLine> ByCommand,
    MissFigures Misses,
    IReadOnlyList<RunLine> Runs,
    int UnreadableLines);

/// <summary>One project's line on Measurements across projects (BRD-259, REQ-FN-313).</summary>
/// <param name="ProjectId">The project.</param>
/// <param name="ProjectName">Its name.</param>
/// <param name="Path">Its folder or solution file.</param>
/// <param name="FolderFound">Whether its <c>docs/metrics</c> folder exists; a project without one shows "not measured".</param>
/// <param name="Totals">Its runs in the period, added up.</param>
/// <param name="Misses">Its misses in the period.</param>
public sealed record ProjectMetricsLine(int ProjectId, string ProjectName, string Path, bool FolderFound, RunTotals Totals, int Misses);

/// <summary>Every project of the account for one period (BRD-259).</summary>
/// <param name="Period">The period counted.</param>
/// <param name="Projects">One line per project, most tokens out first.</param>
/// <param name="Totals">Every project's runs, added up.</param>
/// <param name="Misses">Every project's misses, added up.</param>
public sealed record MetricsAcrossProjects(MetricsPeriod Period, IReadOnlyList<ProjectMetricsLine> Projects, RunTotals Totals, int Misses);

/// <summary>A zip of a project's measurement files, saved (BRD-260, REQ-FN-314).</summary>
/// <param name="ZipPath">Where it was written.</param>
/// <param name="Entries">The entry names inside it, e.g. <c>docs/metrics/runs.jsonl</c>.</param>
/// <param name="Bytes">Its size.</param>
public sealed record MetricsExport(string ZipPath, IReadOnlyList<string> Entries, long Bytes);
