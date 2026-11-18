using System.Globalization;
using System.Text.Json;
using Chatur.Core.Actions;
using Chatur.Core.Estimates;

namespace Chatur.Tests.Estimates;

/// <summary>
/// The run records of fixtures F1 to F6 in docs/Chatur-P3-BRD.md §3 "Estimate before, actual after", as
/// <c>runs.jsonl</c> lines. Every run is live, measured and on <c>claude-opus-5-5</c> unless a fixture says
/// otherwise. The same records were run through <c>tf-estimate.sh</c> and <c>tf-metrics.sh --estimates</c> on
/// 2026-11-10 and gave the figures the tests expect.
/// </summary>
internal static class EstimateFixtures
{
    public const string Opus = "claude-opus-5-5";
    public const string Sonnet = "claude-sonnet-5-5";

    /// <summary>The phase-2 checklist rows of the test project <c>Fixture</c>.</summary>
    public static readonly string[] PhaseRows = ["REQ-FN-301", "REQ-FN-302", "REQ-FN-303"];

    /// <summary>A phase-2 checklist holding <paramref name="aRows"/>, in the shape Chatur's checklists have.</summary>
    public static string Checklist(params string[] aRows) =>
        "# Fixture — Checklist (phase 2)\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|-------------|--------|---|---------|---------|\n"
        + string.Concat(aRows.Select(aRow => $"| {aRow} | A row | Not Started | 0% | | |\n"))
        + "\n## Page: Fixture\n";

    /// <summary>One run record line. Each call is three hours after the one before, so start times never collide.</summary>
    public sealed class Runs
    {
        private readonly DateTime objBase = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly List<string> objLines = [];
        private int objCount;

        /// <summary>Every line so far.</summary>
        public IReadOnlyList<string> Lines => objLines;

        /// <summary>Adds a run record and returns its start time as written.</summary>
        public string Add(
            string aCmd,
            string[] aReqs,
            long? aTokensOut,
            long aSeconds,
            string aModel = Opus,
            bool aBackfilled = false,
            string aTokensScope = "tree",
            bool aAllStrings = false)
        {
            objCount++;
            var vStarted = objBase.AddHours(objCount * 3);
            var vEnded = vStarted.AddSeconds(aSeconds);
            var vRecord = new Dictionary<string, object?>
            {
                ["v"] = 1,
                ["ts"] = Stamp(vEnded),
                ["kind"] = "run",
                ["app"] = "Fixture",
                ["cmd"] = aCmd,
                ["mode"] = "build",
                ["started"] = Stamp(vStarted),
                ["ended"] = Stamp(vEnded),
                ["duration_s"] = aSeconds,
                ["reqs_touched"] = aReqs,
                ["reqs_count"] = aReqs.Length,
                ["model"] = aModel
            };
            if (aTokensOut is { } vTokens)
            {
                vRecord["tokens_out"] = vTokens;
            }

            if (!aAllStrings)
            {
                vRecord["tokens_scope"] = aTokensScope;
            }

            if (aBackfilled)
            {
                vRecord["backfilled"] = true;
            }

            if (aAllStrings)
            {
                // Chatur's own streams: every value a string, lists caller-joined, "" for not measured.
                vRecord = vRecord.ToDictionary(
                    aPair => aPair.Key,
                    aPair => (object?)(aPair.Value switch
                    {
                        string[] vList => string.Join(",", vList),
                        null => string.Empty,
                        bool vFlag => vFlag ? "true" : "false",
                        _ => Convert.ToString(aPair.Value, CultureInfo.InvariantCulture)
                    }));
            }

            objLines.Add(JsonSerializer.Serialize(vRecord));
            return Stamp(vStarted);
        }

        /// <summary>Adds a <c>run-void</c> record taking out the run that started at <paramref name="aStarted"/>.</summary>
        public void Void(string aCmd, string aStarted) =>
            objLines.Add(JsonSerializer.Serialize(new { v = 1, kind = "run-void", cmd = aCmd, started = aStarted, reason = "fixture" }));
    }

    /// <summary>
    /// F1: five build-phase runs on REQ-FN rows outside phase 2 — A 2 rows 4,000 tokens 600 s; B 1, 3,000, 240;
    /// C 2, 10,000, 900; D 4, 4,000, 480; E 4, 16,000, 1,440. <paramref name="aModels"/> gives A to E's models.
    /// </summary>
    public static Runs F1(string[]? aModels = null, bool aAllStrings = false)
    {
        var vModels = aModels ?? [Opus, Opus, Opus, Opus, Opus];
        var vRuns = new Runs();
        vRuns.Add("build-phase", ["REQ-FN-201", "REQ-FN-202"], 4000, 600, vModels[0], aAllStrings: aAllStrings);
        vRuns.Add("build-phase", ["REQ-FN-203"], 3000, 240, vModels[1], aAllStrings: aAllStrings);
        vRuns.Add("build-phase", ["REQ-FN-204", "REQ-FN-205"], 10000, 900, vModels[2], aAllStrings: aAllStrings);
        vRuns.Add("build-phase", ["REQ-FN-206", "REQ-FN-207", "REQ-FN-208", "REQ-FN-209"], 4000, 480, vModels[3], aAllStrings: aAllStrings);
        vRuns.Add("build-phase", ["REQ-FN-210", "REQ-FN-211", "REQ-FN-212", "REQ-FN-213"], 16000, 1440, vModels[4], aAllStrings: aAllStrings);
        return vRuns;
    }

    /// <summary>F4: F1, plus a run naming no row, one with no measured tokens, one backfilled, one voided and one that touched REQ-FN-301.</summary>
    public static Runs F4()
    {
        var vRuns = F1();
        vRuns.Add("build-phase", [], 5000, 500);
        vRuns.Add("build-phase", ["REQ-FN-220"], null, 500, aTokensScope: "none");
        vRuns.Add("build-phase", ["REQ-FN-221"], 5000, 500, aBackfilled: true);
        var vVoided = vRuns.Add("build-phase", ["REQ-FN-222"], 5000, 500);
        vRuns.Add("build-phase", ["REQ-FN-301"], 5000, 500);
        vRuns.Void("build-phase", vVoided);
        return vRuns;
    }

    /// <summary>
    /// F6's actuals after F1: R1 (REQ-FN-301, REQ-FN-302 and REQ-FN-999; 9,000 tokens, 900 s), R2 (REQ-FN-303; 3,000,
    /// 300 s), R3 (REQ-FN-302; 1,500, 150 s) and a verify-phase run R4 that named no row (2,000 tokens, 120 s).
    /// </summary>
    public static Runs F6(bool aOnlyR2AndR3 = false)
    {
        var vRuns = F1();
        if (!aOnlyR2AndR3)
        {
            vRuns.Add("build-phase", ["REQ-FN-301", "REQ-FN-302", "REQ-FN-999"], 9000, 900);
        }

        vRuns.Add("build-phase", ["REQ-FN-303"], 3000, 300);
        vRuns.Add("build-phase", ["REQ-FN-302"], 1500, 150);
        if (!aOnlyR2AndR3)
        {
            vRuns.Add("verify-phase", [], 2000, 120);
        }

        return vRuns;
    }

    /// <summary>The routes every step gets in a fixture: <paramref name="aModel"/> on <paramref name="aTier"/>.</summary>
    public static IReadOnlyDictionary<string, StepRoute> Routes(string aModel = Opus, int aTier = 1) =>
        EstimateSteps.All.ToDictionary(aStep => aStep, _ => new StepRoute(aModel, aTier));

    /// <summary>Estimates the phase rows from fixture runs, as the Estimator does once it has read the files.</summary>
    public static PhaseEstimate Estimate(
        IEnumerable<string> aOwnLines,
        string[]? aRows = null,
        IReadOnlyDictionary<string, StepRoute>? aRoutes = null,
        params (string Name, IEnumerable<string> Lines)[] aSisters)
    {
        var vRows = aRows ?? PhaseRows;
        var (vOwn, vSkipped) = RunHistory.Read("Fixture", aOwnLines, vRows.ToHashSet(StringComparer.Ordinal));
        var vSisters = aSisters
            .Select(aSister => new SisterHistory(aSister.Name, RunHistory.Read(aSister.Name, aSister.Lines, new HashSet<string>()).Samples))
            .ToList();
        return EstimateCalculator.Estimate(new EstimateInput("Fixture", 2, vRows, vOwn, vSkipped, vSisters, aRoutes ?? Routes(), false));
    }

    private static string Stamp(DateTime aUtc) => aUtc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
}
