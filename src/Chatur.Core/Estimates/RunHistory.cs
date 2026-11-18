using System.Text.RegularExpressions;
using Chatur.Core.Actions;

namespace Chatur.Core.Estimates;

/// <summary>One usable earlier run: what it cost per row it touched.</summary>
/// <param name="Step">The line of work it counts towards (<see cref="EstimateSteps"/>).</param>
/// <param name="Prefixes">The row kinds it touched, e.g. <c>REQ-UI</c>, <c>REQ-FN</c>.</param>
/// <param name="TokensPerRow">Its output tokens divided by the rows it touched.</param>
/// <param name="SecondsPerRow">Its wall seconds divided by the rows it touched.</param>
/// <param name="Model">The model that ran it, as the record names it; empty when not recorded.</param>
/// <param name="Key">Identifies the run across pools: source, command and start time.</param>
public sealed record RunSample(string Step, IReadOnlySet<string> Prefixes, double TokensPerRow, double SecondsPerRow, string Model, string Key);

/// <summary>
/// Turns a project's <c>runs.jsonl</c> into the samples an estimate is worked out from, by the framework's
/// rules (schema §2.8 "How the history is chosen"): live runs only, never backfilled, never voided; only runs
/// that named at least one row; never a run that touched a row of the phase being estimated; never a run
/// with no measured output tokens or no measurable time.
/// </summary>
public static partial class RunHistory
{
    [GeneratedRegex(@"^(REQ-[A-Z]+)-\d+$")]
    private static partial Regex PrefixPattern();

    /// <summary>The row kind of a checklist id (<c>REQ-UI-083</c> is <c>REQ-UI</c>), or <see langword="null"/>.</summary>
    /// <param name="aReqId">The id.</param>
    public static string? PrefixOf(string aReqId) =>
        PrefixPattern().Match(aReqId) is { Success: true } vMatch ? vMatch.Groups[1].Value : null;

    /// <summary>The usable runs among the lines and what was left out.</summary>
    /// <param name="aSource">The project the lines came from, for the run keys.</param>
    /// <param name="aLines">The lines of its <c>runs.jsonl</c>.</param>
    /// <param name="aExclude">The rows of the phase being estimated; a run touching any is left out. Empty for a sister project.</param>
    public static (IReadOnlyList<RunSample> Samples, EstimateSkipped Skipped) Read(string aSource, IEnumerable<string> aLines, IReadOnlySet<string> aExclude)
    {
        var vRecords = EstimateJson.Records(aLines).ToList();
        var vVoided = vRecords
            .Where(aRecord => EstimateJson.Text(aRecord, "kind") == "run-void")
            .Select(aRecord => (EstimateJson.Text(aRecord, "cmd"), EstimateJson.Text(aRecord, "started")))
            .ToHashSet();

        var vSamples = new List<RunSample>();
        int vBackfilled = 0, vVoidedN = 0, vNoReqs = 0, vThisPhase = 0, vNoTokens = 0;
        foreach (var vRecord in vRecords)
        {
            var vCmd = EstimateJson.Text(vRecord, "cmd");
            if (EstimateJson.Text(vRecord, "kind") != "run" || EstimateSteps.StepOf(vCmd) is not { } vStep)
            {
                continue;
            }

            var vStarted = EstimateJson.Text(vRecord, "started");
            if (EstimateJson.Flag(vRecord, "backfilled"))
            {
                vBackfilled++;
                continue;
            }

            if (vVoided.Contains((vCmd, vStarted)))
            {
                vVoidedN++;
                continue;
            }

            var vReqs = EstimateJson.List(vRecord, "reqs_touched");
            var vCount = EstimateJson.RowCount(vRecord, vReqs);
            if (vCount <= 0 || vReqs.Count == 0)
            {
                vNoReqs++;
                continue;
            }

            if (aExclude.Count > 0 && vReqs.Any(aExclude.Contains))
            {
                vThisPhase++;
                continue;
            }

            var vTokens = EstimateJson.MeasuredTokensOut(vRecord);
            var vSeconds = EstimateJson.Duration(vRecord);
            if (vTokens is null || vSeconds is null)
            {
                vNoTokens++;
                continue;
            }

            var vPrefixes = vReqs.Select(PrefixOf).OfType<string>().ToHashSet(StringComparer.Ordinal);
            vSamples.Add(new RunSample(
                vStep,
                vPrefixes,
                vTokens.Value / vCount,
                vSeconds.Value / vCount,
                EstimateJson.Text(vRecord, "model"),
                $"{aSource} {vCmd} {vStarted}"));
        }

        return (vSamples, new EstimateSkipped(vBackfilled, vVoidedN, vNoReqs, vThisPhase, vNoTokens));
    }

    /// <summary>How many live run records a <c>runs.jsonl</c> holds (Settings ▸ Measurements' run count, BRD-197).</summary>
    /// <param name="aLines">The file's lines.</param>
    public static int LiveRunCount(IEnumerable<string> aLines)
    {
        var vRecords = EstimateJson.Records(aLines).ToList();
        var vVoided = vRecords
            .Where(aRecord => EstimateJson.Text(aRecord, "kind") == "run-void")
            .Select(aRecord => (EstimateJson.Text(aRecord, "cmd"), EstimateJson.Text(aRecord, "started")))
            .ToHashSet();
        return vRecords.Count(aRecord =>
            EstimateJson.Text(aRecord, "kind") == "run"
            && !EstimateJson.Flag(aRecord, "backfilled")
            && !vVoided.Contains((EstimateJson.Text(aRecord, "cmd"), EstimateJson.Text(aRecord, "started"))));
    }
}
