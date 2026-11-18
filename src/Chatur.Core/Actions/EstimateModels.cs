namespace Chatur.Core.Actions;

/// <summary>
/// The four lines of work an estimate is made for, by the names the framework's estimates stream uses
/// (framework schema §2.8). Machine QA is <see cref="Verify"/> plus <see cref="Fix"/>, added for display only.
/// </summary>
public static class EstimateSteps
{
    /// <summary>Build: <c>build-phase</c> runs.</summary>
    public const string Build = "build-phase";

    /// <summary>The verify half of machine QA: <c>verify-phase</c> runs.</summary>
    public const string Verify = "verify-phase";

    /// <summary>The fix half of machine QA: <c>fix-issues</c> runs.</summary>
    public const string Fix = "fix-issues";

    /// <summary>After handoff: <c>triage-issues</c> and <c>triage-and-fix</c> runs, pooled.</summary>
    public const string Triage = "triage";

    /// <summary>The four, in the order the framework writes them.</summary>
    public static IReadOnlyList<string> All { get; } = [Build, Verify, Fix, Triage];

    /// <summary>What the owner's own testing reads on every estimate table (BRD-207). It is never estimated.</summary>
    public const string OwnerTestingText = "not measured";

    /// <summary>The step a run record's <c>cmd</c> counts towards, or <see langword="null"/> when it counts towards none.</summary>
    /// <param name="aCmd">The run record's <c>cmd</c>.</param>
    public static string? StepOf(string aCmd) => aCmd switch
    {
        Build => Build,
        Verify => Verify,
        Fix => Fix,
        "triage-issues" or "triage-and-fix" => Triage,
        _ => null
    };
}

/// <summary>Where an estimate's runs came from (framework schema §2.8 <c>basis</c>).</summary>
public static class EstimateBasis
{
    /// <summary>At least three of this project's own runs.</summary>
    public const string Repo = "repo";

    /// <summary>The sister projects' runs were added.</summary>
    public const string Family = "family";

    /// <summary>Fewer than three runs anywhere it may look: no precedent, never a number.</summary>
    public const string None = "none";
}

/// <summary>Low, middle and high: the first quartile, the median and the third quartile, whole numbers.</summary>
/// <param name="Low">The first quartile.</param>
/// <param name="Mid">The median.</param>
/// <param name="High">The third quartile.</param>
public sealed record EstimateFigure(long Low, long Mid, long High)
{
    /// <summary>Two figures added part by part, or <see langword="null"/> when either is missing: never a sum with a part left out.</summary>
    /// <param name="aLeft">One figure.</param>
    /// <param name="aRight">The other.</param>
    public static EstimateFigure? Add(EstimateFigure? aLeft, EstimateFigure? aRight) =>
        aLeft is null || aRight is null ? null : new EstimateFigure(aLeft.Low + aRight.Low, aLeft.Mid + aRight.Mid, aLeft.High + aRight.High);

    /// <summary>
    /// Where an actual stands against this range (BRD-203): <c>below</c> under the low, <c>above</c> over the high,
    /// otherwise <c>within</c>. Both ends belong to the range.
    /// </summary>
    /// <param name="aActual">The measured figure.</param>
    public string PositionOf(long aActual) => aActual < Low ? "below" : aActual > High ? "above" : "within";
}

/// <summary>One row's estimate for one line of work.</summary>
/// <param name="ReqId">The checklist row.</param>
/// <param name="Step">One of <see cref="EstimateSteps.All"/>.</param>
/// <param name="TokensOut">Output tokens for the row, or <see langword="null"/> with no precedent.</param>
/// <param name="DurationS">Seconds for the row, or <see langword="null"/> with no precedent.</param>
/// <param name="Basis">One of <see cref="EstimateBasis"/>.</param>
/// <param name="BasisRuns">How many runs the figure rests on; with no precedent, how many the widest pool held.</param>
/// <param name="FromAllModels">The step's model had fewer than three runs, so runs on every model were used.</param>
/// <param name="SisterProjects">The sister projects whose runs were added, by folder name; empty when none were.</param>
public sealed record RowEstimate(
    string ReqId,
    string Step,
    EstimateFigure? TokensOut,
    EstimateFigure? DurationS,
    string Basis,
    int BasisRuns,
    bool FromAllModels,
    IReadOnlyList<string> SisterProjects);

/// <summary>A phase total for one line of work: the sum of the row figures that had one.</summary>
/// <param name="Step">One of <see cref="EstimateSteps.All"/>.</param>
/// <param name="Model">The model that will run the step, or <see langword="null"/> when routing names none.</param>
/// <param name="Tier">The tier routing gives the step, 1 to 3, or <see langword="null"/> when not known.</param>
/// <param name="TokensOut">The sum over the rows that had a figure, or <see langword="null"/> when none had.</param>
/// <param name="DurationS">The same for seconds.</param>
/// <param name="Basis"><see cref="EstimateBasis.Family"/> when any row it sums was, else <see cref="EstimateBasis.Repo"/>, or <see cref="EstimateBasis.None"/>.</param>
/// <param name="BasisRuns">Distinct runs across its rows.</param>
/// <param name="RowsN">Rows in the phase.</param>
/// <param name="RowsNoPrecedentN">Rows with no figure for this step; the total covers the others.</param>
/// <param name="FromAllModels">Any row it sums was worked out from every model's runs.</param>
/// <param name="SisterProjects">The sister projects any of its rows drew on.</param>
public sealed record StepTotal(
    string Step,
    string? Model,
    int? Tier,
    EstimateFigure? TokensOut,
    EstimateFigure? DurationS,
    string Basis,
    int BasisRuns,
    int RowsN,
    int RowsNoPrecedentN,
    bool FromAllModels,
    IReadOnlyList<string> SisterProjects)
{
    /// <summary>The rows the total covers.</summary>
    public int RowsCovered => RowsN - RowsNoPrecedentN;
}

/// <summary>What the history left out, and why (framework <c>history_skipped</c>).</summary>
/// <param name="Backfilled">Runs written after the fact.</param>
/// <param name="Voided">Runs a <c>run-void</c> record took out.</param>
/// <param name="NoReqs">Runs that named no row, so cannot be divided among rows.</param>
/// <param name="ThisPhase">Runs that touched a row of the phase being estimated.</param>
/// <param name="NoTokens">Runs with no measured output tokens or no measurable time.</param>
public sealed record EstimateSkipped(int Backfilled, int Voided, int NoReqs, int ThisPhase, int NoTokens);

/// <summary>A phase's estimate, worked out from run records with no model asked (BRD-192 to BRD-209).</summary>
/// <param name="App">The project's name, as the records carry it.</param>
/// <param name="Phase">The phase whose checklist was estimated; 0 when there is no checklist yet.</param>
/// <param name="Rows">The phase's checklist rows, in order; empty when there is no checklist yet.</param>
/// <param name="RowFigures">One entry per row per step.</param>
/// <param name="Totals">One entry per step, in <see cref="EstimateSteps.All"/> order.</param>
/// <param name="Retro">Runs on the phase's own rows already existed: estimated after the fact.</param>
/// <param name="AutoRouting">Automatic routing is on, so the run may use another model than the one named.</param>
/// <param name="Skipped">What this project's history left out.</param>
public sealed record PhaseEstimate(
    string App,
    int Phase,
    IReadOnlyList<string> Rows,
    IReadOnlyList<RowEstimate> RowFigures,
    IReadOnlyList<StepTotal> Totals,
    bool Retro,
    bool AutoRouting,
    EstimateSkipped Skipped)
{
    /// <summary>There is no checklist yet (a brief before day-1): nothing to estimate before the build step.</summary>
    public bool NoChecklist => Rows.Count == 0;

    /// <summary>No step has a figure from any pool: the screen reads "No estimate yet" (BRD-208).</summary>
    public bool NoEstimateYet => Totals.All(aTotal => aTotal.Basis == EstimateBasis.None);

    /// <summary>The owner's own testing: always <see cref="EstimateSteps.OwnerTestingText"/> (BRD-207).</summary>
    public string OwnerTesting => EstimateSteps.OwnerTestingText;

    /// <summary>One step's total.</summary>
    /// <param name="aStep">One of <see cref="EstimateSteps.All"/>.</param>
    public StepTotal Total(string aStep) => Totals.Single(aTotal => aTotal.Step == aStep);

    /// <summary>One row's figure for one step.</summary>
    /// <param name="aReqId">The row.</param>
    /// <param name="aStep">One of <see cref="EstimateSteps.All"/>.</param>
    public RowEstimate Row(string aReqId, string aStep) =>
        RowFigures.Single(aRow => aRow.ReqId == aReqId && aRow.Step == aStep);

    /// <summary>
    /// Machine QA for one row: verify plus fix, or <see langword="null"/> when either has no precedent.
    /// Shown, never stored (framework schema §2.8).
    /// </summary>
    /// <param name="aReqId">The row.</param>
    /// <param name="aTokens"><see langword="true"/> for output tokens, <see langword="false"/> for seconds.</param>
    public EstimateFigure? MachineQa(string aReqId, bool aTokens) => aTokens
        ? EstimateFigure.Add(Row(aReqId, EstimateSteps.Verify).TokensOut, Row(aReqId, EstimateSteps.Fix).TokensOut)
        : EstimateFigure.Add(Row(aReqId, EstimateSteps.Verify).DurationS, Row(aReqId, EstimateSteps.Fix).DurationS);

    /// <summary>
    /// A phase column as the framework prints it: the sum, over the rows where the column has a number, of
    /// build, machine QA, after handoff or build plus machine QA; and how many rows that is.
    /// </summary>
    /// <param name="aPart"><c>build</c>, <c>machine-qa</c>, <c>after-handoff</c> or <c>total</c>.</param>
    /// <param name="aTokens"><see langword="true"/> for output tokens, <see langword="false"/> for seconds.</param>
    public (EstimateFigure? Figure, int RowsCovered) PhaseColumn(string aPart, bool aTokens)
    {
        EstimateFigure? Pick(string aReqId, string aStep) =>
            aTokens ? Row(aReqId, aStep).TokensOut : Row(aReqId, aStep).DurationS;

        var vValues = Rows
            .Select(aReqId => aPart switch
            {
                "build" => Pick(aReqId, EstimateSteps.Build),
                "machine-qa" => MachineQa(aReqId, aTokens),
                "after-handoff" => Pick(aReqId, EstimateSteps.Triage),
                "total" => EstimateFigure.Add(Pick(aReqId, EstimateSteps.Build), MachineQa(aReqId, aTokens)),
                _ => throw new ArgumentException($"\"{aPart}\" is not a phase column.", nameof(aPart))
            })
            .OfType<EstimateFigure>()
            .ToList();
        return vValues.Count == 0
            ? (null, 0)
            : (vValues.Aggregate((aSum, aNext) => EstimateFigure.Add(aSum, aNext)!), vValues.Count);
    }
}

/// <summary>One line of work, estimate beside actual (BRD-202 to BRD-204). Two columns, never one number: no ratio.</summary>
/// <param name="Step">One of <see cref="EstimateSteps.All"/>.</param>
/// <param name="EstimateTokens">The phase total's output tokens, or <see langword="null"/> with no precedent.</param>
/// <param name="EstimateDuration">The phase total's seconds.</param>
/// <param name="RowsCovered">The rows the estimate had a figure for; the actual is measured over the same rows.</param>
/// <param name="RunsN">Live runs of this step that touched those rows.</param>
/// <param name="ApportionedN">Of those, runs that also touched other rows and count only their share.</param>
/// <param name="TokensOut">The actual output tokens, or <see langword="null"/> when fewer than three runs measured them.</param>
/// <param name="TokensN">Runs with measured output tokens.</param>
/// <param name="DurationS">The actual seconds, or <see langword="null"/> when fewer than three runs had a time.</param>
/// <param name="DurationN">Runs with a measurable time.</param>
/// <param name="RowlessRunsN">Runs of this step in the project that named no row: in no figure, shown apart.</param>
/// <param name="RowlessTokensOut">Their output tokens, shown apart, never shared out.</param>
/// <param name="RowlessSeconds">Their seconds, shown apart, never shared out.</param>
public sealed record EstimateActual(
    string Step,
    EstimateFigure? EstimateTokens,
    EstimateFigure? EstimateDuration,
    int RowsCovered,
    int RunsN,
    int ApportionedN,
    long? TokensOut,
    int TokensN,
    long? DurationS,
    int DurationN,
    int RowlessRunsN,
    long RowlessTokensOut,
    long RowlessSeconds)
{
    /// <summary>At least one run counted only its share.</summary>
    public bool Apportioned => ApportionedN > 0;

    /// <summary>The actual tokens as words when there is no number: <c>insufficient data (n=2)</c>.</summary>
    public string TokensText => TokensOut is { } vTokens ? vTokens.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) : $"insufficient data (n={TokensN})";

    /// <summary>The actual seconds as words when there is no number.</summary>
    public string DurationText => DurationS is { } vSeconds ? $"{vSeconds} s" : $"insufficient data (n={DurationN})";

    /// <summary>Where the actual tokens stand against the estimate, or <see langword="null"/> when either is missing.</summary>
    public string? TokensPosition => TokensOut is { } vActual && EstimateTokens is not null ? EstimateTokens.PositionOf(vActual) : null;

    /// <summary>Where the actual seconds stand against the estimate, or <see langword="null"/> when either is missing.</summary>
    public string? DurationPosition => DurationS is { } vActual && EstimateDuration is not null ? EstimateDuration.PositionOf(vActual) : null;
}

/// <summary>A confirmed estimate batch beside what the runs measured (the reconciliation, BRD-204).</summary>
/// <param name="App">The project's name.</param>
/// <param name="Phase">The phase.</param>
/// <param name="BatchTs">The batch's <c>ts</c>.</param>
/// <param name="Retro">The batch was made after the fact.</param>
/// <param name="Steps">One entry per step, in <see cref="EstimateSteps.All"/> order.</param>
public sealed record EstimateAgainstActual(string App, int Phase, string BatchTs, bool Retro, IReadOnlyList<EstimateActual> Steps)
{
    /// <summary>One step's line.</summary>
    /// <param name="aStep">One of <see cref="EstimateSteps.All"/>.</param>
    public EstimateActual Step(string aStep) => Steps.Single(aLine => aLine.Step == aStep);
}

/// <summary>An estimate a run was started or queued with, kept so a later tier change does not move it (BRD-194, BRD-201).</summary>
/// <param name="BatchTs">The <c>ts</c> of the batch written to <c>estimates.jsonl</c>, or empty when nothing was written (no checklist yet).</param>
/// <param name="Estimate">The estimate as it was shown.</param>
public sealed record KeptEstimate(string BatchTs, PhaseEstimate Estimate);

/// <summary>The kinds of run an estimate is kept for.</summary>
public static class EstimateRunKinds
{
    /// <summary>A single process started from Processes; the id is the process run's.</summary>
    public const string Process = "process";

    /// <summary>An end-to-end run from a brief; the id is the <c>AutoRun</c>'s.</summary>
    public const string Auto = "auto";

    /// <summary>A queued run; the id is the <c>RunQueueItem</c>'s.</summary>
    public const string Queue = "queue";
}

/// <summary>A sister project the owner named on Settings ▸ Measurements (BRD-197).</summary>
/// <param name="SisterProjectId">Its id.</param>
/// <param name="Path">Its folder.</param>
/// <param name="Name">The folder's name, as the estimate names it.</param>
/// <param name="RunCount">The live run records in its <c>docs/metrics/runs.jsonl</c> now.</param>
public sealed record SisterProject(int SisterProjectId, string Path, string Name, int RunCount);
