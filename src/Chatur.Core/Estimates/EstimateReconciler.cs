using Chatur.Core.Actions;

namespace Chatur.Core.Estimates;

/// <summary>
/// Sets a written estimate beside what the runs measured (BRD-203, BRD-204), by the framework's
/// reconciliation (<c>tf-metrics.sh --estimates</c>): per step, the live runs that touched the rows the
/// estimate had a figure for, each counting the share of its tokens and time its rows in the phase make up
/// (apportioned when below one). Fewer than three runs: insufficient data, never a number. Runs that named no
/// row are in no actual; their count, tokens and time stand apart. Nothing here divides one column by the other.
/// </summary>
public static class EstimateReconciler
{
    /// <summary>The batch beside the actuals.</summary>
    /// <param name="aBatch">The estimate batch.</param>
    /// <param name="aRunsLines">The lines of the project's <c>runs.jsonl</c>.</param>
    public static EstimateAgainstActual Reconcile(EstimateBatch aBatch, IEnumerable<string> aRunsLines)
    {
        var vRecords = EstimateJson.Records(aRunsLines).ToList();
        var vVoided = vRecords
            .Where(aRecord => EstimateJson.Text(aRecord, "kind") == "run-void")
            .Select(aRecord => (EstimateJson.Text(aRecord, "cmd"), EstimateJson.Text(aRecord, "started")))
            .ToHashSet();
        var vRuns = vRecords
            .Where(aRecord => EstimateJson.Text(aRecord, "kind") == "run"
                && !EstimateJson.Flag(aRecord, "backfilled")
                && !vVoided.Contains((EstimateJson.Text(aRecord, "cmd"), EstimateJson.Text(aRecord, "started"))))
            .ToList();

        var vLines = new List<EstimateActual>();
        foreach (var vStep in aBatch.Steps)
        {
            int vRunsN = 0, vApportioned = 0, vTokensN = 0, vDurationN = 0, vRowless = 0;
            double vTokens = 0, vSeconds = 0, vRowlessTokens = 0, vRowlessSeconds = 0;
            foreach (var vRun in vRuns.Where(aRun => EstimateSteps.StepOf(EstimateJson.Text(aRun, "cmd")) == vStep.Step))
            {
                var vReqs = EstimateJson.List(vRun, "reqs_touched");
                if (vReqs.Count == 0)
                {
                    vRowless++;
                    vRowlessTokens += EstimateJson.MeasuredTokensOut(vRun) ?? 0;
                    vRowlessSeconds += EstimateJson.Duration(vRun) ?? 0;
                    continue;
                }

                var vCount = EstimateJson.RowCount(vRun, vReqs);
                var vHit = vReqs.Where(vStep.Covered.Contains).Distinct(StringComparer.Ordinal).Count();
                if (vHit == 0 || vCount <= 0)
                {
                    continue;
                }

                var vShare = Math.Min(1.0, (double)vHit / vCount);
                vRunsN++;
                if (vShare < 1.0)
                {
                    vApportioned++;
                }

                if (EstimateJson.MeasuredTokensOut(vRun) is { } vRunTokens)
                {
                    vTokens += vRunTokens * vShare;
                    vTokensN++;
                }

                if (EstimateJson.Duration(vRun) is { } vRunSeconds)
                {
                    vSeconds += vRunSeconds * vShare;
                    vDurationN++;
                }
            }

            vLines.Add(new EstimateActual(
                vStep.Step,
                vStep.TokensOut,
                vStep.DurationS,
                vStep.Covered.Count,
                vRunsN,
                vApportioned,
                vTokensN >= EstimateCalculator.MinRuns ? (long)Math.Round(vTokens, MidpointRounding.ToEven) : null,
                vTokensN,
                vDurationN >= EstimateCalculator.MinRuns ? (long)Math.Round(vSeconds, MidpointRounding.ToEven) : null,
                vDurationN,
                vRowless,
                (long)Math.Round(vRowlessTokens, MidpointRounding.ToEven),
                (long)Math.Round(vRowlessSeconds, MidpointRounding.ToEven)));
        }

        return new EstimateAgainstActual(aBatch.App, aBatch.Phase, aBatch.Ts, aBatch.Retro, vLines);
    }
}
