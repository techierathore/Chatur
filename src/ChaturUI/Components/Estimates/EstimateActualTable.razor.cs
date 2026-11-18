using System.Globalization;
using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;
using TrBlazeUI.Components.Badge;

namespace ChaturUI.Components.Estimates;

/// <summary>
/// A written estimate batch beside what the runs measured (BRD-203, BRD-204, BRD-207; mockups/process-run.html
/// <c>handoff-estimate</c> and mockups/run-report.html <c>estimate-actual-panel</c>): build, machine QA (verify plus
/// fix), after handoff and "Your own testing", which always reads "not measured". Tokens and time are two columns,
/// each judged on its own against its range; no ratio, no percent, nothing that divides one figure by another
/// (REQ-FN-115). Runs that named no row are shown apart, never inside a figure. Shared by Process run (handoff) and
/// Run report; every figure comes from <see cref="IEstimateActions"/>, only how a number is written is decided here.
/// </summary>
public partial class EstimateActualTable
{
    private IReadOnlyList<ActualLine> objLines = [];
    private IReadOnlyList<string> objRowlessNotes = [];

    /// <summary>The reconciliation, from <c>AgainstActualAsync</c> or <c>PhaseAgainstActualAsync</c>.</summary>
    [Parameter, EditorRequired]
    public EstimateAgainstActual Actual { get; set; } = default!;

    /// <summary>The panel's heading.</summary>
    [Parameter]
    public string Title { get; set; } = "Estimate and actual";

    /// <summary>The prefix of every data-testid: <c>estimate-actual</c> on Run report, <c>handoff</c> on Process run.</summary>
    [Parameter]
    public string TestId { get; set; } = "estimate-actual";

    /// <summary>When the estimate was made, for the heading's pill (mockups/run-report.html <c>estimate-made</c>); none when not given.</summary>
    [Parameter]
    public string? MadeText { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var vBuild = Actual.Step(EstimateSteps.Build);
        var vVerify = Actual.Step(EstimateSteps.Verify);
        var vFix = Actual.Step(EstimateSteps.Fix);
        var vTriage = Actual.Step(EstimateSteps.Triage);

        objLines =
        [
            LineOf("build", "Build", vBuild.EstimateTokens, vBuild.EstimateDuration, vBuild.TokensOut, vBuild.TokensN, vBuild.DurationS, vBuild.DurationN, vBuild.Apportioned),
            QaLine(vVerify, vFix),
            LineOf("triage", "After handoff (triage)", vTriage.EstimateTokens, vTriage.EstimateDuration, vTriage.TokensOut, vTriage.TokensN, vTriage.DurationS, vTriage.DurationN, vTriage.Apportioned),
            new ActualLine("owner", "Your own testing", null, null, EstimateSteps.OwnerTestingText, EstimateSteps.OwnerTestingText, null, null, false, true, false)
        ];
        objRowlessNotes = Actual.Steps
            .Where(aStep => aStep.RowlessRunsN > 0)
            .Select(aStep => $"{StepLabel(aStep.Step)}: {aStep.RowlessRunsN} run{(aStep.RowlessRunsN == 1 ? "" : "s")} named no row — {aStep.RowlessTokensOut.ToString("N0", CultureInfo.InvariantCulture)} tokens out, {EstimateLines.Duration(aStep.RowlessSeconds)}. Shown here, never shared out among the rows.")
            .ToList();
    }

    /// <summary>"Machine QA" is verify plus fix, added for display only; a part with no run at all adds nothing.</summary>
    private static ActualLine QaLine(EstimateActual aVerify, EstimateActual aFix)
    {
        var vTokens = SumOrNull(aVerify.TokensOut, aVerify.RunsN, aFix.TokensOut, aFix.RunsN);
        var vTime = SumOrNull(aVerify.DurationS, aVerify.RunsN, aFix.DurationS, aFix.RunsN);
        return LineOf(
            "qa",
            "Machine QA (verify + fix)",
            EstimateFigure.Add(aVerify.EstimateTokens, aFix.EstimateTokens),
            EstimateFigure.Add(aVerify.EstimateDuration, aFix.EstimateDuration),
            vTokens,
            aVerify.TokensN + aFix.TokensN,
            vTime,
            aVerify.DurationN + aFix.DurationN,
            aVerify.Apportioned || aFix.Apportioned);
    }

    private static long? SumOrNull(long? aLeft, int aLeftRuns, long? aRight, int aRightRuns)
    {
        if (aLeftRuns == 0 && aRightRuns == 0)
        {
            return null;
        }

        var vLeft = aLeftRuns == 0 ? 0 : aLeft;
        var vRight = aRightRuns == 0 ? 0 : aRight;
        return vLeft is null || vRight is null ? null : vLeft + vRight;
    }

    private static ActualLine LineOf(string aKey, string aLabel, EstimateFigure? aEstimateTokens, EstimateFigure? aEstimateTime,
        long? aTokens, int aTokensN, long? aSeconds, int aSecondsN, bool aApportioned)
    {
        var vTokensActual = aTokens is { } vTokens
            ? $"{vTokens.ToString("N0", CultureInfo.InvariantCulture)} tokens"
            :$"insufficient data (n={aTokensN})";
        var vTimeActual = aSeconds is { } vSeconds ? EstimateLines.Duration(vSeconds) : $"insufficient data (n={aSecondsN})";
        return new ActualLine(
            aKey,
            aLabel,
            EstimateLines.TokensText(aEstimateTokens),
            EstimateLines.DurationText(aEstimateTime),
            vTokensActual,
            vTimeActual,
            aTokens is { } vT && aEstimateTokens is not null ? aEstimateTokens.PositionOf(vT) : null,
            aSeconds is { } vS && aEstimateTime is not null ? aEstimateTime.PositionOf(vS) : null,
            aApportioned,
            false,
            aEstimateTokens is null && aEstimateTime is null);
    }

    private static string StepLabel(string aStep) => aStep switch
    {
        EstimateSteps.Build => "Build",
        EstimateSteps.Verify => "Verify",
        EstimateSteps.Fix => "Fix",
        _ => "After handoff"
    };

    private static BadgeVariant PositionVariant(string aPosition) => aPosition == "within" ? BadgeVariant.Success : BadgeVariant.Warning;

    /// <summary>One line of the table, already worded.</summary>
    private sealed record ActualLine(
        string Key,
        string Label,
        string? EstimateTokens,
        string? EstimateTime,
        string ActualTokens,
        string ActualTime,
        string? TokensWhere,
        string? TimeWhere,
        bool Apportioned,
        bool NotMeasured,
        bool NoPrecedent);
}
