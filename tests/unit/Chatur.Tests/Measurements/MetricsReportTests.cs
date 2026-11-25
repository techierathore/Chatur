using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using Chatur.Core.Actions;
using Chatur.Core.Measurements;
using Chatur.Core.Platform;
using Chatur.Tests.Support;
using Xunit;

namespace Chatur.Tests.Measurements;

/// <summary>
/// Tests for Chatur's own measurements (REQ-UI-115 to REQ-UI-118, REQ-UI-120, REQ-FN-313, REQ-FN-314, REQ-NFR-014) over the
/// hand-made files in <c>Fixtures/metrics</c>. The clock is fixed at 2026-11-20 10:00 UTC and the zone is UTC+05:30, so
/// "today" there starts at 2026-11-19 18:30 UTC.
/// </summary>
public sealed class MetricsReportTests : MigratedDatabaseFixture
{
    private static readonly DateTime NowUtc = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);

    private static readonly TimeZoneInfo Zone = TimeZoneInfo.CreateCustomTimeZone("Test+0530", TimeSpan.FromHours(5.5), "Test", "Test");

    private readonly List<string> objFolders = [];

    private static string FixtureFolder => Path.Combine(AppContext.BaseDirectory, "Measurements", "Fixtures", "metrics");

    private static MetricsComputation Calc(MetricsPeriod aPeriod, string? aRuns = null, string? aGates = null, string? aMisses = null) =>
        MetricsCalculator.Compute(
            aRuns ?? File.ReadAllText(Path.Combine(FixtureFolder, "runs.jsonl")),
            aGates ?? File.ReadAllText(Path.Combine(FixtureFolder, "gates.jsonl")),
            aMisses ?? File.ReadAllText(Path.Combine(FixtureFolder, "misses.jsonl")),
            aPeriod,
            NowUtc,
            Zone);

    /// <summary>
    /// When the fixture is added up for all history and for the last 30 days, then every figure (runs, the four token sums, the cost,
    /// the wall clock, each model and command line, the misses and the first pass) equals a plain JsonDocument re-addition of the same
    /// files, and each split adds up to the totals (REQ-NFR-014).
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-014 every figure equals an independent re-addition of the files")]
    public void EveryFigureEqualsAnIndependentReAddition()
    {
        foreach (var (vPeriod, vCutoff) in new[] { (MetricsPeriod.AllHistory, (DateTime?)null), (new MetricsPeriod(MetricsPeriodKind.Last30Days), new DateTime(2026, 10, 21, 18, 30, 0, DateTimeKind.Utc)) })
        {
            var vResult = Calc(vPeriod);
            var vRuns = IndependentRuns(vCutoff);

            Assert.Equal(vRuns.Count, vResult.Totals.Runs);
            Assert.Equal(vRuns.Count, vResult.Runs.Count);
            AssertSum(vRuns.Select(r => r.In).ToList(), vResult.Totals.TokensIn);
            AssertSum(vRuns.Select(r => r.Out).ToList(), vResult.Totals.TokensOut);
            AssertSum(vRuns.Select(r => r.CacheRead).ToList(), vResult.Totals.CacheRead);
            AssertSum(vRuns.Select(r => r.CacheWrite).ToList(), vResult.Totals.CacheWrite);
            AssertSum(vRuns.Select(r => r.Wall).ToList(), vResult.Totals.WallClockS);
            var vPriced = vRuns.Where(r => r.Cost is not null).ToList();
            Assert.Equal(vPriced.Count, vResult.Totals.Cost.Priced);
            Assert.Equal(vRuns.Count - vPriced.Count, vResult.Totals.Cost.NoPrice);
            Assert.Equal(vPriced.Count == 0 ? null : vPriced.Sum(r => r.Cost), vResult.Totals.Cost.Usd);

            foreach (var vSplit in new[] { vResult.ByModel, vResult.ByAgent, vResult.ByCommand })
            {
                Assert.Equal(vResult.Totals.Runs, vSplit.Sum(l => l.Totals.Runs));
                Assert.Equal(vResult.Totals.TokensOut.Value, SumOrNull(vSplit.Select(l => l.Totals.TokensOut.Value)));
                Assert.Equal(vResult.Totals.TokensIn.Value, SumOrNull(vSplit.Select(l => l.Totals.TokensIn.Value)));
                Assert.Equal(vResult.Totals.TokensOut.NotMeasured, vSplit.Sum(l => l.Totals.TokensOut.NotMeasured));
                Assert.Equal(vResult.Totals.Cost.Usd, vSplit.Select(l => l.Totals.Cost.Usd).Where(u => u is not null).Sum());
            }

            foreach (var vLine in vResult.ByModel)
            {
                var vOwn = vRuns.Where(r => r.Model == vLine.Key).ToList();
                Assert.Equal(vOwn.Count, vLine.Totals.Runs);
                AssertSum(vOwn.Select(r => r.Out).ToList(), vLine.Totals.TokensOut);
                AssertSum(vOwn.Select(r => r.Wall).ToList(), vLine.Totals.WallClockS);
            }

            foreach (var vLine in vResult.ByCommand)
            {
                var vOwn = vRuns.Where(r => r.Cmd == vLine.Key).ToList();
                Assert.Equal(vOwn.Count, vLine.Totals.Runs);
                AssertSum(vOwn.Select(r => r.In).ToList(), vLine.Totals.TokensIn);
                AssertSum(vOwn.Select(r => r.CacheWrite).ToList(), vLine.Totals.CacheWrite);
            }

            var vMisses = IndependentMissIds(vCutoff);
            Assert.Equal(vMisses.OrderBy(i => i), vResult.Misses.Misses.Select(m => m.MissId).OrderBy(i => i));
            Assert.Equal(vMisses.Count, vResult.Misses.ByFoundBy.Sum(p => p.Value));

            var (vGraded, vFirstPass) = IndependentFirstPass(vCutoff);
            Assert.Equal(vGraded, vResult.Misses.RowsGraded);
            Assert.Equal(vFirstPass, vResult.Misses.RowsFirstPass);
        }
    }

    /// <summary>
    /// When a run has tokens_scope none, or no priced cost, then its tokens count as not measured and its cost as no price, and the
    /// totals are null rather than 0 (REQ-NFR-014, BRD-261).
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-014 a run without measured tokens or price gives null, never zero")]
    public void NotMeasuredIsNullNeverZero()
    {
        var vText = "{\"ts\":\"2026-11-20T01:00:00Z\",\"kind\":\"run\",\"cmd\":\"build-phase\",\"tokens_scope\":\"none\",\"tokens_in\":0,\"tokens_out\":0,\"cost_usd\":null}\n"
            + "{\"ts\":\"2026-11-20T02:00:00Z\",\"kind\":\"run\",\"cmd\":\"build-phase\",\"tokens_in\":\"5\",\"cost_usd\":\"\"}";

        var vResult = Calc(MetricsPeriod.AllHistory, vText, string.Empty, string.Empty);

        Assert.Equal(2, vResult.Totals.Runs);
        Assert.Equal(new MeasuredSum(null, 0, 2), vResult.Totals.TokensIn);
        Assert.Equal(new MeasuredSum(null, 0, 2), vResult.Totals.TokensOut);
        Assert.Equal(new MeasuredSum(null, 0, 2), vResult.Totals.CacheRead);
        Assert.Equal(new MeasuredSum(null, 0, 2), vResult.Totals.CacheWrite);
        Assert.Equal(new MeasuredSum(null, 0, 2), vResult.Totals.WallClockS);
        Assert.Equal(new MeasuredCost(null, 0, 2), vResult.Totals.Cost);
        Assert.All(vResult.Runs, r => Assert.Null(r.TokensIn));
        Assert.Equal("not measured", vResult.ByModel.Single().Key);
    }

    /// <summary>
    /// When each period is asked with a fixed clock and zone, then Today, Last 7 days, Last 30 days and Between count the runs, misses
    /// and graded rows dated inside it, to the second at each day boundary, and only All history counts the undated (REQ-UI-116).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-116 each period counts only the records dated inside it")]
    public void EachPeriodFiltersByDate()
    {
        var vAll = Calc(MetricsPeriod.AllHistory);
        Assert.Equal(11, vAll.Totals.Runs);
        Assert.Equal(6, vAll.Misses.Misses.Count);
        Assert.Equal(8, vAll.Misses.RowsGraded);

        var vToday = Calc(new MetricsPeriod(MetricsPeriodKind.Today));
        Assert.Equal([1, 2, 12], vToday.Runs.Select(r => r.Line).Order());
        Assert.Equal(["m6"], vToday.Misses.Misses.Select(m => m.MissId));
        Assert.Equal((1, 1), (vToday.Misses.RowsGraded, vToday.Misses.RowsFirstPass));

        var vWeek = Calc(new MetricsPeriod(MetricsPeriodKind.Last7Days));
        Assert.Equal([1, 2, 3, 4, 10, 12, 13, 14], vWeek.Runs.Select(r => r.Line).Order());

        var vMonth = Calc(new MetricsPeriod(MetricsPeriodKind.Last30Days));
        Assert.Equal(9, vMonth.Totals.Runs);
        Assert.DoesNotContain(vMonth.Runs, r => r.AtUtc is null);
        Assert.DoesNotContain(vMonth.Misses.Misses, m => m.MissId == "m5");
        Assert.Equal((6, 3), (vMonth.Misses.RowsGraded, vMonth.Misses.RowsFirstPass));

        var vBetween = Calc(new MetricsPeriod(MetricsPeriodKind.Between, new DateOnly(2026, 11, 14), new DateOnly(2026, 11, 15)));
        Assert.Equal([4, 10, 13], vBetween.Runs.Select(r => r.Line).Order());
    }

    /// <summary>
    /// When the runs are split, then each command maps to its agent by Chatur's command list (verify-phase to Verifier, amend-docs to
    /// Analyst, build-phase to Flow master, an unknown command to not measured), and the model and agent splits come out most tokens
    /// out first (REQ-UI-117).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-117 splits by model, agent and command, with the agent from Chatur's command list")]
    public void SplitsAndAgentMapping()
    {
        Assert.Equal("Verifier", MetricsCalculator.AgentOf("verify-phase"));
        Assert.Equal("Analyst", MetricsCalculator.AgentOf("amend-docs"));
        Assert.Equal("Analyst", MetricsCalculator.AgentOf("day1-greenfield"));
        Assert.Equal("Flow master", MetricsCalculator.AgentOf("build-phase"));
        Assert.Equal("Flow master", MetricsCalculator.AgentOf("metrics-report"));
        Assert.Equal("Flow master", MetricsCalculator.AgentOf("triage-issues"));
        Assert.Equal("Flow master", MetricsCalculator.AgentOf("log-miss"));
        Assert.Equal("Flow master", MetricsCalculator.AgentOf("handoff-phase"));
        Assert.Equal("not measured", MetricsCalculator.AgentOf("mystery-cmd"));
        Assert.Equal("not measured", MetricsCalculator.AgentOf(null));

        var vAll = Calc(MetricsPeriod.AllHistory);
        Assert.Equal(["claude-opus", "claude-sonnet", "not measured", "claude-haiku"], vAll.ByModel.Select(l => l.Key));
        Assert.Equal([5, 3, 1, 2], vAll.ByModel.Select(l => l.Totals.Runs));
        Assert.Equal(742L, vAll.ByModel[0].Totals.TokensOut.Value);
        Assert.Equal(1, vAll.ByModel[0].Totals.TokensOut.NotMeasured);

        Assert.Equal(6, vAll.ByAgent.Single(l => l.Key == "Flow master").Totals.Runs);
        Assert.Equal(2, vAll.ByAgent.Single(l => l.Key == "Verifier").Totals.Runs);
        Assert.Equal(2, vAll.ByAgent.Single(l => l.Key == "Analyst").Totals.Runs);
        Assert.Equal(1, vAll.ByAgent.Single(l => l.Key == "not measured").Totals.Runs);

        Assert.Equal(3, vAll.ByCommand.Single(l => l.Key == "build-phase").Totals.Runs);
        Assert.DoesNotContain(vAll.ByCommand, l => l.Key == "refresh-status");
    }

    /// <summary>
    /// When misses are read, then amends fold into their miss, a voided miss and its fixes disappear, a repeated miss_id keeps its
    /// earliest record, the status comes from the latest fix, found_by and rows are counted raw, and a miss with no found_by is counted
    /// as not measured (REQ-UI-118).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-118 misses fold amends and voids, take status from the latest fix, and count found-by and rows")]
    public void MissesAndFirstPass()
    {
        var vAll = Calc(MetricsPeriod.AllHistory);
        var vMisses = vAll.Misses;

        Assert.Equal(["m6", "m2", "m1", "m4", "m7", "m5"], vMisses.Misses.Select(m => m.MissId));
        var vFirst = vMisses.Misses.Single(m => m.MissId == "m1");
        Assert.Equal("gate", vFirst.FoundBy);
        Assert.Equal("G2", vFirst.FoundGate);
        Assert.Equal("Late amend text", vFirst.What);
        Assert.Equal("Verified", vFirst.Status);
        Assert.Equal("open", vMisses.Misses.Single(m => m.MissId == "m2").Status);
        Assert.Equal("wont-fix", vMisses.Misses.Single(m => m.MissId == "m4").Status);
        Assert.Equal("open", vMisses.Misses.Single(m => m.MissId == "m6").Status);

        Assert.Equal(new[] { "agent-review", "gate", "library-feedback", "owner", "production", "self-smoke" }, vMisses.ByFoundBy.Select(p => p.Key).Order());
        Assert.All(vMisses.ByFoundBy, p => Assert.Equal(1, p.Value));
        Assert.Equal(["REQ-FN-001", "REQ-UI-001", "REQ-FN-002"], vMisses.ByRow.Select(p => p.Key));
        Assert.Equal([2, 2, 1], vMisses.ByRow.Select(p => p.Value));

        Assert.Equal((8, 5), (vMisses.RowsGraded, vMisses.RowsFirstPass));
        Assert.Equal(3, vAll.UnreadableLines);

        var vNoFoundBy = Calc(MetricsPeriod.AllHistory, string.Empty, string.Empty, "{\"ts\":\"2026-11-01T00:00:00Z\",\"kind\":\"miss\",\"miss_id\":\"x1\",\"found_by\":\"\"}");
        Assert.Equal(new KeyValuePair<string, int>("not measured", 1), Assert.Single(vNoFoundBy.Misses.ByFoundBy));
    }

    /// <summary>
    /// When the runs list is read, then each line carries its line number in runs.jsonl, the rows it touched (a JSON array or Chatur's
    /// comma-separated string), the raw record laid out for reading, and null for what was not measured (REQ-UI-120).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-120 run lines carry rows touched, the raw record and null for not measured")]
    public void RunLines()
    {
        var vRuns = Calc(MetricsPeriod.AllHistory).Runs;

        Assert.Equal(2, vRuns[0].Line);
        var vOne = vRuns.Single(r => r.Line == 1);
        Assert.Equal(["REQ-FN-001", "REQ-FN-002"], vOne.RowsTouched);
        Assert.Equal("pass", vOne.Result);
        Assert.Equal(1.25m, vOne.CostUsd);
        Assert.Equal(600L, vOne.WallClockS);
        Assert.Equal("auto", vOne.Mode);
        Assert.Equal("Flow master", vOne.Agent);
        Assert.Contains("\n  \"cmd\": \"build-phase\"", vOne.Raw);

        var vTwo = vRuns.Single(r => r.Line == 2);
        Assert.Equal(["REQ-UI-001", "REQ-UI-002"], vTwo.RowsTouched);
        Assert.Equal(200L, vTwo.TokensIn);
        Assert.Null(vTwo.CacheRead);
        Assert.Equal(50L, vTwo.CacheWrite);
        Assert.Null(vTwo.CostUsd);
        Assert.Equal(120L, vTwo.WallClockS);
        Assert.Equal(new DateTime(2026, 11, 20, 6, 0, 0, DateTimeKind.Utc), vTwo.AtUtc);

        var vThree = vRuns.Single(r => r.Line == 3);
        Assert.Null(vThree.TokensOut);
        Assert.Null(vThree.CostUsd);
        Assert.Equal(30L, vThree.WallClockS);

        var vUndated = vRuns.Single(r => r.Line == 9);
        Assert.Null(vUndated.AtUtc);
        Assert.Empty(vUndated.RowsTouched);
        Assert.Equal("not measured", vRuns.Single(r => r.Line == 4).Model);
        Assert.Equal(1200L, vRuns.Single(r => r.Line == 4).WallClockS);
        Assert.DoesNotContain(vRuns, r => r.Line == 5);
    }

    /// <summary>
    /// When a project's report is asked for, then it is read from docs/metrics alone: the actions take no HttpClient, the project's
    /// figures come from its files, a project with no docs/metrics gives FolderFound false and not measured everywhere, and a
    /// missing project gives null (REQ-UI-115).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-115 the report is read from the project's own files and makes no network call")]
    public async Task ReportReadsOnlyTheProjectsFiles()
    {
        var vConstructor = Assert.Single(typeof(MetricsReportActions).GetConstructors());
        Assert.DoesNotContain(vConstructor.GetParameters(), p => p.ParameterType.Name.Contains("Http", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(vConstructor.GetParameters(), p => p.ParameterType.Name.Contains("AppManager", StringComparison.OrdinalIgnoreCase));

        var vWith = NewProject("With", withMetrics: true);
        var vWithout = NewProject("Without", withMetrics: false);
        var vActions = NewActions();

        var vReport = await vActions.ReportAsync(vWith.Id, MetricsPeriod.AllHistory);
        Assert.NotNull(vReport);
        Assert.True(vReport.FolderFound);
        Assert.Equal("With", vReport.ProjectName);
        Assert.Equal(Path.Combine(vWith.Folder, "docs", "metrics"), vReport.MetricsFolder);
        Assert.Equal(11, vReport.Totals.Runs);
        Assert.Equal(892L, vReport.Totals.TokensOut.Value);
        Assert.Equal(2.15m, vReport.Totals.Cost.Usd);
        Assert.Equal(3, vReport.UnreadableLines);

        var vEmpty = await vActions.ReportAsync(vWithout.Id, MetricsPeriod.AllHistory);
        Assert.NotNull(vEmpty);
        Assert.False(vEmpty.FolderFound);
        Assert.Equal(0, vEmpty.Totals.Runs);
        Assert.Null(vEmpty.Totals.TokensOut.Value);
        Assert.Null(vEmpty.Totals.Cost.Usd);
        Assert.Null(vEmpty.Totals.WallClockS.Value);
        Assert.Empty(vEmpty.Runs);

        Assert.Null(await vActions.ReportAsync(9999, MetricsPeriod.AllHistory));
    }

    /// <summary>
    /// When the account has two projects and one has no docs/metrics, then each has its line, the project without files is shown as
    /// not found with null figures, the lines are most tokens out first and the totals add the lines (REQ-FN-313).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-313 across projects gives one line per project and adds them up")]
    public async Task AcrossProjectsGivesOneLinePerProject()
    {
        var vWith = NewProject("With", withMetrics: true);
        var vWithout = NewProject("Without", withMetrics: false);
        var vActions = NewActions(new FakeProjects(
        [
            new ProjectSummary(vWithout.Id, "Without", vWithout.Folder, "dotnet", null),
            new ProjectSummary(vWith.Id, "With", vWith.Folder, "dotnet", null)
        ]));

        var vAcross = await vActions.AcrossProjectsAsync(MetricsPeriod.AllHistory);

        Assert.Equal(["With", "Without"], vAcross.Projects.Select(p => p.ProjectName));
        Assert.True(vAcross.Projects[0].FolderFound);
        Assert.Equal(6, vAcross.Projects[0].Misses);
        Assert.False(vAcross.Projects[1].FolderFound);
        Assert.Null(vAcross.Projects[1].Totals.TokensOut.Value);
        Assert.Equal(0, vAcross.Projects[1].Totals.Runs);
        Assert.Equal(11, vAcross.Totals.Runs);
        Assert.Equal(892L, vAcross.Totals.TokensOut.Value);
        Assert.Equal(6, vAcross.Misses);

        var vNone = await NewActions(new FakeProjects([new ProjectSummary(vWithout.Id, "Without", vWithout.Folder, "dotnet", null)])).AcrossProjectsAsync(MetricsPeriod.AllHistory);
        Assert.Null(vNone.Totals.TokensOut.Value);
        Assert.Null(vNone.Totals.Cost.Usd);
    }

    /// <summary>
    /// When a project's files are exported, then the zip holds exactly docs/metrics/&lt;file&gt; for each non-empty stream file, byte-equal,
    /// TfLens's rule (file name only) recognises every entry, a solution path writes beside the solution, and a project with no
    /// metrics file gives null (REQ-FN-314).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-314 export writes a zip of docs/metrics/<file> entries that TfLens recognises")]
    public async Task ExportWritesTheTfLensShape()
    {
        var vWith = NewProject("My App", withMetrics: true);
        File.WriteAllText(Path.Combine(vWith.Folder, "docs", "metrics", "sessions.jsonl"), string.Empty);
        File.WriteAllText(Path.Combine(vWith.Folder, "notes.txt"), "not a stream");
        File.WriteAllText(Path.Combine(vWith.Folder, "docs", "metrics", "other.txt"), "not a stream");
        var vSolution = Path.Combine(vWith.Folder, "My.sln");
        File.WriteAllText(vSolution, string.Empty);
        var vSolutionId = CreateProject("Sln", vSolution);
        var vActions = NewActions();

        var vExport = await vActions.ExportAsync(vWith.Id, null);

        Assert.NotNull(vExport);
        Assert.Equal(vWith.Folder, Path.GetDirectoryName(vExport.ZipPath));
        Assert.Matches(@"^My App-metrics-\d{8}-\d{6}\.zip$", Path.GetFileName(vExport.ZipPath));
        Assert.Equal(["docs/metrics/runs.jsonl", "docs/metrics/gates.jsonl", "docs/metrics/misses.jsonl"], vExport.Entries);
        Assert.Equal(new FileInfo(vExport.ZipPath).Length, vExport.Bytes);

        using (var vZip = ZipFile.OpenRead(vExport.ZipPath))
        {
            Assert.Equal(vExport.Entries.Order(), vZip.Entries.Select(e => e.FullName).Order());
            var vRecognised = new[] { "runs.jsonl", "gates.jsonl", "sessions.jsonl", "commits.jsonl", "misses.jsonl", "estimates.jsonl" };
            foreach (var vEntry in vZip.Entries)
            {
                var vFileName = vEntry.FullName[(vEntry.FullName.LastIndexOf('/') + 1)..];
                Assert.Contains(vRecognised, n => string.Equals(n, vFileName, StringComparison.OrdinalIgnoreCase));
                using var vBytes = new MemoryStream();
                using (var vStream = vEntry.Open())
                {
                    vStream.CopyTo(vBytes);
                }

                Assert.Equal(File.ReadAllBytes(Path.Combine(vWith.Folder, vEntry.FullName.Replace('/', Path.DirectorySeparatorChar))), vBytes.ToArray());
            }
        }

        var vOther = Path.Combine(TestScratch.Root, $"export-{Guid.NewGuid():N}");
        objFolders.Add(vOther);
        var vElsewhere = await vActions.ExportAsync(vSolutionId, vOther);
        Assert.NotNull(vElsewhere);
        Assert.Equal(vOther, Path.GetDirectoryName(vElsewhere.ZipPath));
        Assert.Equal(3, vElsewhere.Entries.Count);

        var vEmpty = NewProject("Bare", withMetrics: false);
        Assert.Null(await vActions.ExportAsync(vEmpty.Id, null));
        Directory.CreateDirectory(Path.Combine(vEmpty.Folder, "docs", "metrics"));
        File.WriteAllText(Path.Combine(vEmpty.Folder, "docs", "metrics", "runs.jsonl"), string.Empty);
        Assert.Null(await vActions.ExportAsync(vEmpty.Id, null));
    }

    /// <summary>Removes the temporary project folders this test made.</summary>
    public override void Dispose()
    {
        foreach (var vFolder in objFolders.Where(Directory.Exists))
        {
            Directory.Delete(vFolder, true);
        }

        base.Dispose();
    }

    private MetricsReportActions NewActions(IProjectActions? aProjects = null) =>
        new(CreateFactory(), aProjects ?? new FakeProjects([]), new FixedClock(), Zone);

    private (int Id, string Folder) NewProject(string aName, bool withMetrics)
    {
        var vFolder = Path.Combine(TestScratch.Root, $"metrics-project-{Guid.NewGuid():N}");
        objFolders.Add(vFolder);
        Directory.CreateDirectory(vFolder);
        if (withMetrics)
        {
            var vMetrics = Path.Combine(vFolder, "docs", "metrics");
            Directory.CreateDirectory(vMetrics);
            foreach (var vFile in Directory.EnumerateFiles(FixtureFolder, "*.jsonl"))
            {
                File.Copy(vFile, Path.Combine(vMetrics, Path.GetFileName(vFile)));
            }
        }

        return (CreateProject(aName, vFolder), vFolder);
    }

    // ---- the independent re-addition: plain JsonDocument code, never the calculator ------------------------------------

    private sealed record Indep(string Cmd, string Model, long? In, long? Out, long? CacheRead, long? CacheWrite, decimal? Cost, long? Wall);

    private static IEnumerable<JsonElement> Lines(string aFile)
    {
        foreach (var vLine in File.ReadAllLines(Path.Combine(FixtureFolder, aFile)))
        {
            JsonElement? vElement = null;
            try
            {
                using var vDoc = JsonDocument.Parse(vLine);
                if (vDoc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    vElement = vDoc.RootElement.Clone();
                }
            }
            catch (JsonException)
            {
            }

            if (vElement is { } vOk)
            {
                yield return vOk;
            }
        }
    }

    private static string? Text(JsonElement aElement, string aName)
    {
        if (!aElement.TryGetProperty(aName, out var vValue))
        {
            return null;
        }

        string? vText = vValue.ValueKind switch
        {
            JsonValueKind.String => vValue.GetString(),
            JsonValueKind.Number => vValue.GetRawText(),
            _ => null
        };
        return string.IsNullOrEmpty(vText) ? null : vText;
    }

    private static decimal? Number(JsonElement aElement, string aName) =>
        Text(aElement, aName) is { } vText ? decimal.Parse(vText, CultureInfo.InvariantCulture) : null;

    private static DateTime? When(JsonElement aElement) =>
        (Text(aElement, "ts") ?? Text(aElement, "at")) is { } vText
            ? DateTimeOffset.Parse(vText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).UtcDateTime
            : null;

    private static bool Inside(DateTime? aWhen, DateTime? aCutoff) => aCutoff is null || (aWhen is { } vWhen && vWhen >= aCutoff);

    private static List<Indep> IndependentRuns(DateTime? aCutoff)
    {
        var vAll = Lines("runs.jsonl").ToList();
        var vVoids = vAll.Where(e => Text(e, "kind") == "run-void").Select(e => (Text(e, "cmd"), Text(e, "started"))).ToHashSet();
        var vResult = new List<Indep>();
        foreach (var vRun in vAll.Where(e => Text(e, "kind") == "run"))
        {
            if (vVoids.Contains((Text(vRun, "cmd"), Text(vRun, "started"))) || !Inside(When(vRun), aCutoff))
            {
                continue;
            }

            var vMeasured = Text(vRun, "tokens_scope") != "none" && Number(vRun, "tokens_out") is not null;
            long? Tok(string aName) => vMeasured && Number(vRun, aName) is { } v ? (long)v : null;
            long? vWall = null;
            if (Text(vRun, "started") is { } vS && Text(vRun, "ended") is { } vE
                && (DateTimeOffset.Parse(vE, CultureInfo.InvariantCulture) - DateTimeOffset.Parse(vS, CultureInfo.InvariantCulture)).TotalSeconds > 0)
            {
                vWall = (long)(DateTimeOffset.Parse(vE, CultureInfo.InvariantCulture) - DateTimeOffset.Parse(vS, CultureInfo.InvariantCulture)).TotalSeconds;
            }
            else if (Number(vRun, "duration_s") is > 0 and { } vD)
            {
                vWall = (long)vD;
            }

            vResult.Add(new Indep(Text(vRun, "cmd") ?? "not measured", Text(vRun, "model") ?? "not measured", Tok("tokens_in"), Tok("tokens_out"), Tok("tokens_cache_read"), Tok("tokens_cache_write"), Number(vRun, "cost_usd"), vWall));
        }

        return vResult;
    }

    private static List<string> IndependentMissIds(DateTime? aCutoff)
    {
        var vAll = Lines("misses.jsonl").ToList();
        var vVoided = vAll.Where(e => Text(e, "kind") == "miss-void").Select(e => Text(e, "miss_id")).ToHashSet();
        return vAll.Where(e => Text(e, "kind") == "miss" && !vVoided.Contains(Text(e, "miss_id")))
            .GroupBy(e => Text(e, "miss_id"))
            .Select(g => g.OrderBy(e => When(e)).First())
            .Where(e => Inside(When(e), aCutoff))
            .Select(e => Text(e, "miss_id")!).ToList();
    }

    private static (int Graded, int FirstPass) IndependentFirstPass(DateTime? aCutoff)
    {
        var vGraded = new HashSet<string>();
        var vFirstPass = new HashSet<string>();
        var vCounts = new Dictionary<string, int>();
        foreach (var vGate in Lines("gates.jsonl").Where(e => Text(e, "kind") == "gate").OrderBy(e => Text(e, "ts") ?? Text(e, "at") ?? string.Empty, StringComparer.Ordinal).ThenBy(e => Text(e, "run_id"), StringComparer.Ordinal))
        {
            var vKey = Text(vGate, "app") + "|" + Text(vGate, "req_id");
            vCounts[vKey] = vCounts.GetValueOrDefault(vKey) + 1;
            if (!Inside(When(vGate), aCutoff))
            {
                continue;
            }

            vGraded.Add(vKey);
            var vAttempt = Number(vGate, "attempt") ?? vCounts[vKey];
            if (vAttempt == 1 && Text(vGate, "verdict") == "Verified")
            {
                vFirstPass.Add(vKey);
            }
        }

        return (vGraded.Count, vFirstPass.Count);
    }

    private static long? SumOrNull(IEnumerable<long?> aValues)
    {
        var vPresent = aValues.Where(v => v is not null).ToList();
        return vPresent.Count == 0 ? null : vPresent.Sum();
    }

    private static void AssertSum(IReadOnlyList<long?> aExpected, MeasuredSum aActual)
    {
        var vPresent = aExpected.Where(v => v is not null).Select(v => v!.Value).ToList();
        Assert.Equal(vPresent.Count, aActual.Measured);
        Assert.Equal(aExpected.Count - vPresent.Count, aActual.NotMeasured);
        Assert.Equal(vPresent.Count == 0 ? null : vPresent.Sum(), aActual.Value);
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => NowUtc;
    }

    private sealed class FakeProjects(IReadOnlyList<ProjectSummary> aList) : IProjectActions
    {
        public Task<IReadOnlyList<ProjectSummary>> ScanAsync(CancellationToken aCt = default) => Task.FromResult(aList);

        public Task SelectProjectAsync(int aProjectId, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task<ProjectSummary?> SelectedProjectAsync(CancellationToken aCt = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<ProjectFolder>> ListFoldersAsync(CancellationToken aCt = default) => throw new NotSupportedException();

        public Task<ProjectFolder> AddFolderAsync(string aPath, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task RemoveFolderAsync(int aFolderId, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task ForgetProjectAsync(int aProjectId, CancellationToken aCt = default) => throw new NotSupportedException();
    }
}
