using System.Globalization;
using Chatur.Core.Actions;
using Chatur.Core.Platform;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Components.Estimates;

/// <summary>
/// The estimate as the owner reads it before a run (mockups/processes.html <c>estimate-panel</c>): build, machine QA,
/// after handoff, and "Your own testing", which always reads "not measured" (REQ-UI-089). Shows "No estimate yet"
/// (REQ-UI-090) or "No checklist yet" instead of figures when the <see cref="PhaseEstimate"/> says so. Shared by
/// Processes, End-to-end run, Run queue, Process run and Run report; each decides what to show with the parameters.
/// </summary>
public partial class EstimateLines
{
    private static readonly string[] QaSteps = [EstimateSteps.Verify, EstimateSteps.Fix];

    private IReadOnlyList<EstimateLine> objLines = [];
    private IReadOnlyList<EstimateRowLine> objRowLines = [];

    [Inject]
    private ISecondaryWindow SecondaryWindow { get; set; } = default!;

    /// <summary>The estimate to show, from <c>IEstimateActions.EstimateAsync</c> or a kept estimate.</summary>
    [Parameter, EditorRequired]
    public PhaseEstimate Estimate { get; set; } = default!;

    /// <summary>The panel's heading. Default: "What it should cost".</summary>
    [Parameter]
    public string Title { get; set; } = "What it should cost";

    /// <summary>Shows the "Each of the N rows" table, closed until opened. Default <see langword="true"/>.</summary>
    [Parameter]
    public bool ShowPerRow { get; set; } = true;

    /// <summary>
    /// Shows the "Made for" line (build step's model and tier, with the link to Settings ▸ Routing) and the "how it
    /// is worked out" sentence. Default <see langword="true"/>.
    /// </summary>
    [Parameter]
    public bool ShowMadeFor { get; set; } = true;

    /// <summary>
    /// The owner's buttons (Start, Cancel, Queue it, and so on), shown in a row under the table. Left out entirely when
    /// <see langword="null"/>, so a read-only screen passes nothing.
    /// </summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        objLines = BuildLines();
        objRowLines = Estimate.Rows
            .Select((aReqId, aIndex) => new EstimateRowLine(
                aIndex + 1,
                aReqId,
                TokensText(Estimate.Row(aReqId, EstimateSteps.Build).TokensOut),
                DurationText(Estimate.Row(aReqId, EstimateSteps.Build).DurationS),
                TokensText(Estimate.MachineQa(aReqId, true)),
                DurationText(Estimate.MachineQa(aReqId, false))))
            .ToList();
    }

    /// <summary>"12 rows · phase 2".</summary>
    private string RowsText => $"{Estimate.Rows.Count} row{(Estimate.Rows.Count == 1 ? "" : "s")} · phase {Estimate.Phase}";

    /// <summary>"claude-opus-5-5 · tier 1", from the build step.</summary>
    private string MadeForText
    {
        get
        {
            var vBuild = Estimate.Total(EstimateSteps.Build);
            var vParts = new List<string>();
            if (vBuild.Model is not null)
            {
                vParts.Add(vBuild.Model);
            }

            if (vBuild.Tier is { } vTier)
            {
                vParts.Add($"tier {vTier}");
            }

            return vParts.Count == 0 ? "routing names no model yet" : string.Join(" · ", vParts);
        }
    }

    /// <summary>The sentence under the table when build left some rows out of its figure.</summary>
    private string? BuildLeftOutText
    {
        get
        {
            var vBuild = Estimate.Total(EstimateSteps.Build);
            return vBuild.RowsNoPrecedentN > 0 && vBuild.RowsCovered > 0
                ? $"{vBuild.RowsNoPrecedentN} row{(vBuild.RowsNoPrecedentN == 1 ? " has" : "s have")} no precedent for build and {(vBuild.RowsNoPrecedentN == 1 ? "is" : "are")} left out of the phase figure: it covers {vBuild.RowsCovered} of {vBuild.RowsN} rows."
                : null;
        }
    }

    private void ChangeTier() => SecondaryWindow.Open("/settings/routing");

    private List<EstimateLine> BuildLines()
    {
        var vBuild = Estimate.Total(EstimateSteps.Build);
        var vTriage = Estimate.Total(EstimateSteps.Triage);
        var vQaTotals = QaSteps.Select(Estimate.Total).ToList();
        var vQaTokens = Estimate.PhaseColumn("machine-qa", true);
        var vQaTime = Estimate.PhaseColumn("machine-qa", false);
        var vQaBasis = vQaTotals.Any(aTotal => aTotal.Basis == EstimateBasis.Family) ? EstimateBasis.Family
            : vQaTotals.Any(aTotal => aTotal.Basis == EstimateBasis.Repo) ? EstimateBasis.Repo
            : EstimateBasis.None;

        return
        [
            StepLine("build", "Build", vBuild.TokensOut, vBuild.DurationS, vBuild.RowsCovered, vBuild.RowsN, vBuild.Basis,
                vBuild.BasisRuns, vBuild.FromAllModels, vBuild.SisterProjects),
            StepLine("qa", "Machine QA (verify + fix)", vQaTokens.Figure, vQaTime.Figure, vQaTokens.RowsCovered, Estimate.Rows.Count,
                vQaBasis, vQaTotals.Sum(aTotal => aTotal.BasisRuns), vQaTotals.Any(aTotal => aTotal.FromAllModels),
                vQaTotals.SelectMany(aTotal => aTotal.SisterProjects).Distinct().ToList()),
            StepLine("triage", "After handoff (triage)", vTriage.TokensOut, vTriage.DurationS, vTriage.RowsCovered, vTriage.RowsN,
                vTriage.Basis, vTriage.BasisRuns, vTriage.FromAllModels, vTriage.SisterProjects),
            new EstimateLine("owner", "Your own testing", Estimate.OwnerTesting, Estimate.OwnerTesting, false, true,
                "Chatur does not time it", null)
        ];
    }

    private static EstimateLine StepLine(string aKey, string aLabel, EstimateFigure? aTokens, EstimateFigure? aTime,
        int aCovered, int aRows, string aBasis, int aRuns, bool aFromAllModels, IReadOnlyList<string> aSisters)
    {
        var vRuns = $"{aRuns} run{(aRuns == 1 ? "" : "s")}";
        if (aTokens is null && aTime is null)
        {
            return new EstimateLine(aKey, aLabel, null, null, true, false,
                $"{vRuns} found, fewer than three, so no figure", null);
        }

        var vBasis = aBasis == EstimateBasis.Family
            ? $"from sister projects {string.Join(", ", aSisters)} · {vRuns}"
            : $"this project · {vRuns}";
        if (aFromAllModels)
        {
            vBasis += " · from all models";
        }

        var vCovers = aCovered < aRows ? $"covers {aCovered} of {aRows} rows" : null;
        return new EstimateLine(aKey, aLabel, TokensText(aTokens), DurationText(aTime), false, false, vBasis, vCovers);
    }

    /// <summary>A token range as the owner reads it: <c>6,000 · 9,000 · 12,000</c>; <see langword="null"/> with no figure.</summary>
    /// <param name="aFigure">The range.</param>
    public static string? TokensText(EstimateFigure? aFigure) => aFigure is null
        ? null
        : string.Join(" · ", new[] { aFigure.Low, aFigure.Mid, aFigure.High }.Select(aValue => aValue.ToString("N0", CultureInfo.InvariantCulture)));

    /// <summary>A time range as the owner reads it: <c>4 m · 5 m · 6 m</c>; <see langword="null"/> with no figure.</summary>
    /// <param name="aFigure">The range, in seconds.</param>
    public static string? DurationText(EstimateFigure? aFigure) => aFigure is null
        ? null
        : string.Join(" · ", new[] { aFigure.Low, aFigure.Mid, aFigure.High }.Select(Duration));

    /// <summary>Seconds as <c>45 s</c> below a minute, <c>38 m</c> below an hour, otherwise <c>1 h 05 m</c> (minutes rounded, half up).</summary>
    /// <param name="aSeconds">The seconds.</param>
    public static string Duration(long aSeconds)
    {
        if (aSeconds < 60)
        {
            return $"{aSeconds} s";
        }

        var vMinutes = (aSeconds + 30) / 60;
        return vMinutes < 60 ? $"{vMinutes} m" : $"{vMinutes / 60} h {vMinutes % 60:00} m";
    }

    /// <summary>One line of the four-line table.</summary>
    private sealed record EstimateLine(string Key, string Label, string? Tokens, string? Time, bool NoPrecedent, bool NotMeasured, string Basis, string? Covers);

    /// <summary>One row of the per-row table.</summary>
    private sealed record EstimateRowLine(int Number, string ReqId, string? BuildTokens, string? BuildTime, string? QaTokens, string? QaTime);
}
