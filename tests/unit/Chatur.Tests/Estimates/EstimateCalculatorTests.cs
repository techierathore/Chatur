using System.Text.Json;
using Chatur.Core.Actions;
using Chatur.Core.Estimates;
using Xunit;
using static Chatur.Tests.Estimates.EstimateFixtures;

namespace Chatur.Tests.Estimates;

/// <summary>
/// The estimate's arithmetic against fixtures F1 to F5 (docs/Chatur-P3-BRD.md §3), whose expected figures were
/// checked against <c>tf-estimate.sh</c> on the same records on 2026-11-10.
/// </summary>
public sealed class EstimateCalculatorTests
{
    /// <summary>
    /// REQ-FN-107 (BRD-195): when fixture F1 is estimated, then each REQ-FN row's build reads 2,000/3,000/4,000 tokens
    /// and 240/300/360 s over 5 runs, basis repo, and the three-row total 6,000/9,000/12,000 tokens and 720/900/1,080 s.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-107 F1 gives each REQ-FN row 2,000/3,000/4,000 tokens and 240/300/360 s over 5 runs, total 6,000/9,000/12,000")]
    public void F1RowsAndPhaseTotal()
    {
        var vEstimate = Estimate(F1().Lines);

        foreach (var vReqId in PhaseRows)
        {
            var vRow = vEstimate.Row(vReqId, EstimateSteps.Build);
            Assert.Equal(new EstimateFigure(2000, 3000, 4000), vRow.TokensOut);
            Assert.Equal(new EstimateFigure(240, 300, 360), vRow.DurationS);
            Assert.Equal(EstimateBasis.Repo, vRow.Basis);
            Assert.Equal(5, vRow.BasisRuns);
            Assert.False(vRow.FromAllModels);
            Assert.Empty(vRow.SisterProjects);
        }

        var vTotal = vEstimate.Total(EstimateSteps.Build);
        Assert.Equal(new EstimateFigure(6000, 9000, 12000), vTotal.TokensOut);
        Assert.Equal(new EstimateFigure(720, 900, 1080), vTotal.DurationS);
        Assert.Equal(3, vTotal.RowsCovered);
        Assert.Equal(5, vTotal.BasisRuns);
        Assert.False(vEstimate.Retro);
    }

    /// <summary>
    /// REQ-FN-107: Chatur's own all-string records (lists caller-joined, numbers as strings, no tokens_scope) give
    /// exactly the same F1 figures as the framework's native records.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-107 Chatur's all-string run records give the same F1 figures as native ones")]
    public void F1FromAllStringRecords()
    {
        var vNative = Estimate(F1().Lines);
        var vStrings = Estimate(F1(aAllStrings: true).Lines);

        Assert.Equal(JsonSerializer.Serialize(vNative), JsonSerializer.Serialize(vStrings));
    }

    /// <summary>
    /// The quartiles are interpolated between neighbours and rounded half to even, as the framework's Python
    /// <c>round</c> does: 1.5 becomes 2 and 2.5 becomes 2.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-107 quartiles interpolate between neighbours and round half to even")]
    public void QuartilesRoundHalfToEven()
    {
        Assert.Equal(new EstimateFigure(1, 2, 2), EstimateCalculator.Quartiles([1, 2]));
        Assert.Equal(new EstimateFigure(2, 2, 3), EstimateCalculator.Quartiles([2, 3]));
        Assert.Equal(new EstimateFigure(1750, 2500, 3750), EstimateCalculator.Quartiles([1000, 3000, 2000, 6000]));
    }

    /// <summary>
    /// REQ-FN-108 (BRD-196): when fixture F2 is estimated, two REQ-UI runs here and two in sister project S1, then the
    /// REQ-UI row's build reads 1,750/2,500/3,750 tokens and 175/250/350 s, basis family over 4 runs, naming S1.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-108 F2 REQ-UI row reads 1,750/2,500/3,750 and 175/250/350 s, basis family over 4 runs naming S1")]
    public void F2SisterProjectsFillIn()
    {
        var vHere = new Runs();
        vHere.Add("build-phase", ["REQ-UI-201"], 1000, 100);
        vHere.Add("build-phase", ["REQ-UI-202"], 3000, 300);
        var vSister = new Runs();
        vSister.Add("build-phase", ["REQ-UI-801"], 2000, 200);
        vSister.Add("build-phase", ["REQ-UI-802"], 6000, 500);

        var vEstimate = Estimate(vHere.Lines, ["REQ-FN-301", "REQ-FN-302", "REQ-FN-303", "REQ-UI-301"], null, ("S1", vSister.Lines));

        var vRow = vEstimate.Row("REQ-UI-301", EstimateSteps.Build);
        Assert.Equal(new EstimateFigure(1750, 2500, 3750), vRow.TokensOut);
        Assert.Equal(new EstimateFigure(175, 250, 350), vRow.DurationS);
        Assert.Equal(EstimateBasis.Family, vRow.Basis);
        Assert.Equal(4, vRow.BasisRuns);
        Assert.Equal(["S1"], vRow.SisterProjects);
        Assert.Equal(EstimateBasis.Family, vEstimate.Total(EstimateSteps.Build).Basis);
        Assert.Equal(["S1"], vEstimate.Total(EstimateSteps.Build).SisterProjects);
    }

    /// <summary>
    /// Owner's answer 5 (BRD-208): a project with no runs of its own, whose sister project has three, is estimated
    /// from the sister's runs and names it; "No estimate yet" does not show.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-108 REQ-UI-090 a project with no runs of its own is estimated from its sister projects")]
    public void NoOwnRunsUsesSisterProjects()
    {
        var vSister = F1();

        var vEstimate = Estimate([], null, null, ("S1", vSister.Lines));

        var vRow = vEstimate.Row("REQ-FN-301", EstimateSteps.Build);
        Assert.Equal(new EstimateFigure(2000, 3000, 4000), vRow.TokensOut);
        Assert.Equal(EstimateBasis.Family, vRow.Basis);
        Assert.Equal(["S1"], vRow.SisterProjects);
        Assert.False(vEstimate.NoEstimateYet);
    }

    /// <summary>
    /// REQ-FN-109 (BRD-198): when fixture F3 is estimated, one triage-issues run here and one in S1, then after handoff
    /// reads no precedent, basis none over 2 runs, with no figure, and the records written carry no figure either.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-109 F3 after handoff reads no precedent, basis none over 2 runs, no figure shown or written")]
    public void F3NoPrecedentNeverANumber()
    {
        var vHere = new Runs();
        vHere.Add("triage-issues", ["REQ-FN-201"], 1000, 100);
        var vSister = new Runs();
        vSister.Add("triage-issues", ["REQ-FN-801"], 2000, 200);

        var vEstimate = Estimate(vHere.Lines, null, null, ("S1", vSister.Lines));

        var vRow = vEstimate.Row("REQ-FN-301", EstimateSteps.Triage);
        Assert.Equal(EstimateBasis.None, vRow.Basis);
        Assert.Equal(2, vRow.BasisRuns);
        Assert.Null(vRow.TokensOut);
        Assert.Null(vRow.DurationS);
        Assert.Equal(EstimateBasis.None, vEstimate.Total(EstimateSteps.Triage).Basis);
        Assert.Equal(2, vEstimate.Total(EstimateSteps.Triage).BasisRuns);
        Assert.Null(vEstimate.PhaseColumn("after-handoff", true).Figure);

        foreach (var vLine in EstimateRecords.ToLines(vEstimate, "2026-11-10T11:16:00Z"))
        {
            using var vDoc = JsonDocument.Parse(vLine);
            if (vDoc.RootElement.GetProperty("cmd").GetString() == EstimateSteps.Triage)
            {
                Assert.Equal(JsonValueKind.Null, vDoc.RootElement.GetProperty("tokens_out").ValueKind);
                Assert.Equal(JsonValueKind.Null, vDoc.RootElement.GetProperty("duration_s").ValueKind);
                Assert.Equal("none", vDoc.RootElement.GetProperty("basis").GetString());
            }
        }
    }

    /// <summary>
    /// REQ-FN-110 (BRD-199): when fixture F1 is estimated twice, the results match to the last digit; the calculator
    /// takes no model, provider or network input at all.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-110 the same records estimated twice match to the last digit")]
    public void SameRecordsSameFigures()
    {
        var vFirst = EstimateRecords.ToLines(Estimate(F1().Lines), "2026-11-10T11:16:00Z");
        var vSecond = EstimateRecords.ToLines(Estimate(F1().Lines), "2026-11-10T11:16:00Z");

        Assert.Equal(vFirst, vSecond);
    }

    /// <summary>
    /// REQ-FN-111 (BRD-200): when fixture F5 (runs B and D on claude-sonnet-5-5) is estimated with build on tier 1,
    /// then build reads 3,000/4,000/4,500 tokens and 330/360/405 s over 3 runs, naming claude-opus-5-5 and tier 1.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-111 F5 on tier 1 reads 3,000/4,000/4,500 and 330/360/405 s over 3 runs for claude-opus-5-5")]
    public void F5MadeForTheStepsModel()
    {
        var vRuns = F1([Opus, Sonnet, Opus, Sonnet, Opus]);

        var vEstimate = Estimate(vRuns.Lines, null, Routes(Opus, 1));

        var vRow = vEstimate.Row("REQ-FN-301", EstimateSteps.Build);
        Assert.Equal(new EstimateFigure(3000, 4000, 4500), vRow.TokensOut);
        Assert.Equal(new EstimateFigure(330, 360, 405), vRow.DurationS);
        Assert.Equal(3, vRow.BasisRuns);
        Assert.False(vRow.FromAllModels);
        var vTotal = vEstimate.Total(EstimateSteps.Build);
        Assert.Equal(Opus, vTotal.Model);
        Assert.Equal(1, vTotal.Tier);
    }

    /// <summary>
    /// REQ-FN-112 (BRD-201, owner's answer 2): with build on tier 2, claude-sonnet-5-5 has only 2 runs, so the runs on
    /// all models count: 2,000/3,000/4,000 tokens and 240/300/360 s over 5 runs, labelled from all models.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-112 F5 on tier 2 has 2 sonnet runs so reads 2,000/3,000/4,000 over 5 runs from all models")]
    public void F5TierTwoFallsBackToAllModels()
    {
        var vRuns = F1([Opus, Sonnet, Opus, Sonnet, Opus]);

        var vEstimate = Estimate(vRuns.Lines, null, Routes(Sonnet, 2));

        var vRow = vEstimate.Row("REQ-FN-301", EstimateSteps.Build);
        Assert.Equal(new EstimateFigure(2000, 3000, 4000), vRow.TokensOut);
        Assert.Equal(new EstimateFigure(240, 300, 360), vRow.DurationS);
        Assert.Equal(EstimateBasis.Repo, vRow.Basis);
        Assert.Equal(5, vRow.BasisRuns);
        Assert.True(vRow.FromAllModels);
        Assert.True(vEstimate.Total(EstimateSteps.Build).FromAllModels);
        Assert.Equal(Sonnet, vEstimate.Total(EstimateSteps.Build).Model);
        Assert.Equal(2, vEstimate.Total(EstimateSteps.Build).Tier);
    }

    /// <summary>
    /// REQ-FN-116 (BRD-209): when fixture F4 is estimated, the run naming no row, the one with no measured tokens, the
    /// backfilled one, the voided one and the one on REQ-FN-301 are left out; the figures equal F1's over the same 5
    /// runs, and every record of the batch carries basis_note "retro".
    /// </summary>
    [Fact(DisplayName = "REQ-FN-116 F4 leaves out undividable runs, equals F1 over 5 runs, batch carries basis_note retro")]
    public void F4LeavesOutRunsThatCannotBeDivided()
    {
        var vEstimate = Estimate(F4().Lines);

        Assert.Equal(new EstimateSkipped(1, 1, 1, 1, 1), vEstimate.Skipped);
        Assert.Equal(new EstimateFigure(2000, 3000, 4000), vEstimate.Row("REQ-FN-302", EstimateSteps.Build).TokensOut);
        Assert.Equal(5, vEstimate.Total(EstimateSteps.Build).BasisRuns);
        Assert.Equal(new EstimateFigure(6000, 9000, 12000), vEstimate.Total(EstimateSteps.Build).TokensOut);
        Assert.True(vEstimate.Retro);
        Assert.All(EstimateRecords.ToLines(vEstimate, "2026-11-10T11:16:00Z"), aLine =>
        {
            using var vDoc = JsonDocument.Parse(aLine);
            Assert.Equal("retro", vDoc.RootElement.GetProperty("basis_note").GetString());
        });
    }

    /// <summary>
    /// REQ-UI-089 (BRD-207): the owner's own testing is never estimated: every estimate reads "not measured", whatever
    /// the runs, and no record is written for it.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-089 the owner's own testing reads not measured and is never written")]
    public void OwnerTestingIsNotMeasured()
    {
        var vEstimate = Estimate(F1().Lines);

        Assert.Equal("not measured", vEstimate.OwnerTesting);
        Assert.Equal("not measured", Estimate([]).OwnerTesting);
        Assert.DoesNotContain(EstimateRecords.ToLines(vEstimate, "2026-11-10T11:16:00Z"), aLine => aLine.Contains("owner", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>REQ-UI-090 (BRD-208): with no usable run anywhere, every step has no precedent and the estimate reads "No estimate yet".</summary>
    [Fact(DisplayName = "REQ-UI-090 with no usable run anywhere the estimate reads no estimate yet")]
    public void NoRunsAnywhereIsNoEstimateYet()
    {
        var vEstimate = Estimate([]);

        Assert.True(vEstimate.NoEstimateYet);
        Assert.All(vEstimate.Totals, aTotal => Assert.Null(aTotal.TokensOut));
    }

    /// <summary>
    /// REQ-FN-114 (BRD-205): records are in the framework's shape, with Chatur's tier: one per row per step and one total
    /// per step, the totals carrying rows_n and rows_no_precedent_n, machine QA never stored added.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-114 records keep the framework's shape with tier, totals carry rows_n")]
    public void RecordsKeepTheFrameworkShape()
    {
        var vLines = EstimateRecords.ToLines(Estimate(F1().Lines), "2026-11-10T11:16:00Z");

        Assert.Equal((3 * 4) + 4, vLines.Count);
        using var vTotal = JsonDocument.Parse(vLines[^4]);
        Assert.Equal(JsonValueKind.Null, vTotal.RootElement.GetProperty("req_id").ValueKind);
        Assert.Equal("build-phase", vTotal.RootElement.GetProperty("cmd").GetString());
        Assert.Equal(3, vTotal.RootElement.GetProperty("rows_n").GetInt32());
        Assert.Equal(0, vTotal.RootElement.GetProperty("rows_no_precedent_n").GetInt32());
        Assert.Equal(1, vTotal.RootElement.GetProperty("tier").GetInt32());
        Assert.DoesNotContain(vLines, aLine => aLine.Contains("machine", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>REQ-FN-114: a figure with basis none, or no figure without it, is refused, as the framework refuses it.</summary>
    [Fact(DisplayName = "REQ-FN-114 a record with a figure and basis none is refused")]
    public void ARecordBreakingTheRulesIsRefused()
    {
        var vEstimate = Estimate(F1().Lines);
        var vBroken = vEstimate with
        {
            RowFigures = vEstimate.RowFigures.Select(aRow => aRow.Step == EstimateSteps.Build ? aRow with { Basis = EstimateBasis.None } : aRow).ToList()
        };

        Assert.Throws<InvalidOperationException>(() => EstimateRecords.ToLines(vBroken, "2026-11-10T11:16:00Z"));
    }

    /// <summary>
    /// REQ-UI-088 (BRD-203): a run estimated at 6,000/9,000/12,000 tokens reads within at 6,000 and 12,000, above at
    /// 12,001 and below at 5,999.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-088 6,000 and 12,000 read within, 12,001 above, 5,999 below")]
    public void ActualAgainstTheRange()
    {
        var vRange = new EstimateFigure(6000, 9000, 12000);

        Assert.Equal("within", vRange.PositionOf(6000));
        Assert.Equal("within", vRange.PositionOf(12000));
        Assert.Equal("above", vRange.PositionOf(12001));
        Assert.Equal("below", vRange.PositionOf(5999));
    }
}
