using Chatur.Core.Actions;
using Chatur.Core.Estimates;
using Xunit;
using static Chatur.Tests.Estimates.EstimateFixtures;

namespace Chatur.Tests.Estimates;

/// <summary>
/// Estimate beside actual against fixture F6 (docs/Chatur-P3-BRD.md §3), whose expected figures were checked against
/// <c>tf-metrics.sh --estimates</c> on the same records on 2026-11-10.
/// </summary>
public sealed class EstimateReconcilerTests
{
    private const string BatchTs = "2026-11-01T00:00:00Z";

    /// <summary>
    /// REQ-FN-113 (BRD-204): with F1's phase total as the estimate and runs R1 to R4, build shows 6,000/9,000/12,000
    /// beside 10,500 tokens and 1,050 s, apportioned (R1 also touched REQ-FN-999), within; the verify run that named no
    /// row is in no figure and stands apart with its 2,000 tokens and 120 s.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-113 F6 build shows 6,000/9,000/12,000 beside 10,500 tokens apportioned within, 1 run named no row apart")]
    public void F6PhaseAgainstItsEstimate()
    {
        var vBatch = F1Batch();

        var vResult = EstimateReconciler.Reconcile(vBatch, F6().Lines);

        var vBuild = vResult.Step(EstimateSteps.Build);
        Assert.Equal(new EstimateFigure(6000, 9000, 12000), vBuild.EstimateTokens);
        Assert.Equal(10500, vBuild.TokensOut);
        Assert.Equal(1050, vBuild.DurationS);
        Assert.Equal(3, vBuild.RunsN);
        Assert.Equal(1, vBuild.ApportionedN);
        Assert.True(vBuild.Apportioned);
        Assert.Equal("within", vBuild.TokensPosition);
        Assert.Equal("within", vBuild.DurationPosition);
        Assert.Equal(0, vBuild.RowlessRunsN);

        var vVerify = vResult.Step(EstimateSteps.Verify);
        Assert.Equal(1, vVerify.RowlessRunsN);
        Assert.Equal(2000, vVerify.RowlessTokensOut);
        Assert.Equal(120, vVerify.RowlessSeconds);
        Assert.Equal(0, vVerify.RunsN);
        Assert.Null(vVerify.TokensOut);
    }

    /// <summary>REQ-FN-113: with only R2 and R3 the build actual is never a number: "insufficient data (n=2)".</summary>
    [Fact(DisplayName = "REQ-FN-113 F6 with only R2 and R3 reads insufficient data (n=2)")]
    public void F6WithTwoRunsIsInsufficient()
    {
        var vResult = EstimateReconciler.Reconcile(F1Batch(), F6(aOnlyR2AndR3: true).Lines);

        var vBuild = vResult.Step(EstimateSteps.Build);
        Assert.Null(vBuild.TokensOut);
        Assert.Equal("insufficient data (n=2)", vBuild.TokensText);
        Assert.Equal("insufficient data (n=2)", vBuild.DurationText);
        Assert.Null(vBuild.TokensPosition);
    }

    /// <summary>
    /// REQ-FN-115 (BRD-206): estimate and actual are two columns, never one number: neither result type carries a ratio,
    /// a percentage or an accuracy.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-115 no estimate-against-actual figure is a ratio, percentage or accuracy")]
    public void NoRatioAnywhere()
    {
        var vNames = new[] { typeof(EstimateActual), typeof(EstimateAgainstActual), typeof(PhaseEstimate), typeof(StepTotal), typeof(RowEstimate) }
            .SelectMany(aType => aType.GetProperties().Select(aProperty => aProperty.Name))
            .ToList();

        // PascalCase words, compared exactly: "Duration" holds the letters "ratio" but is not a ratio.
        Assert.DoesNotContain(vNames, aName =>
            aName.Contains("Ratio", StringComparison.Ordinal)
            || aName.Contains("Percent", StringComparison.Ordinal)
            || aName.Contains("Accuracy", StringComparison.Ordinal)
            || aName.Contains("Share", StringComparison.Ordinal));
    }

    private static EstimateBatch F1Batch()
    {
        var vLines = EstimateRecords.ToLines(Estimate(F1().Lines), BatchTs);
        return EstimateRecords.Batch(vLines, BatchTs)!;
    }
}
