using System.Text.Json;
using System.Text.Json.Nodes;
using Chatur.Core.Actions;
using Chatur.Core.Agent;
using Chatur.Core.Estimates;
using Chatur.Core.Models;
using Chatur.Core.Platform;
using Chatur.Core.ProcessRuns;
using Chatur.Core.Routing;
using Chatur.Tests.Support;
using Microsoft.Data.Sqlite;
using Xunit;
using static Chatur.Tests.Estimates.EstimateFixtures;

namespace Chatur.Tests.Estimates;

/// <summary>
/// The Estimator against a real, migrated database and project folders on disk: routing names the step's model and
/// tier, a confirmed start writes the batch and keeps it, sister projects are named and refused.
/// </summary>
public sealed class EstimateActionsTests : MigratedDatabaseFixture
{
    private static readonly DateTime ConfirmedAt = new(2026, 11, 10, 11, 16, 0, DateTimeKind.Utc);

    private readonly TempProject objFolders = new();

    /// <summary>Removes the project folders as well as the database.</summary>
    public override void Dispose()
    {
        objFolders.Dispose();
        base.Dispose();
    }

    /// <summary>
    /// REQ-FN-114 (BRD-205): when the owner confirms a start for fixture F1, then REQ-FN-301's build record in
    /// estimates.jsonl equals the example in docs/Chatur-Metrics-Schema.md, ts aside.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-114 confirming F1 writes REQ-FN-301's build record equal to the Metrics Schema example")]
    public async Task ConfirmWritesTheSchemaExample()
    {
        var (vActions, vProjectId, vFolder) = await ArrangeAsync(F1().Lines);

        var vEstimate = (await vActions.EstimateAsync(vProjectId))!;
        var vKept = await vActions.ConfirmAsync(vProjectId, vEstimate, EstimateRunKinds.Process, 1);

        Assert.Equal("2026-11-10T11:16:00Z", vKept.BatchTs);
        var vWritten = File.ReadAllLines(Path.Combine(vFolder, "docs", "metrics", "estimates.jsonl"))
            .Select(aLine => JsonNode.Parse(aLine)!.AsObject())
            .Single(aRecord => (string?)aRecord["req_id"] == "REQ-FN-301" && (string?)aRecord["cmd"] == "build-phase");
        var vExample = SchemaExample();
        vWritten.Remove("ts");
        vExample.Remove("ts");
        Assert.True(JsonNode.DeepEquals(vExample, vWritten), $"written: {vWritten.ToJsonString()}\nexample: {vExample.ToJsonString()}");
    }

    /// <summary>
    /// REQ-FN-110 (BRD-199): when fixture F1 is estimated twice with no model provider reachable (the only provider is
    /// not connected), both results match to the last digit; the Estimator takes nothing that can call a model.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-110 F1 estimated twice with no provider reachable matches and the Estimator cannot call a model")]
    public async Task EstimateMakesNoModelCall()
    {
        var (vActions, vProjectId, _) = await ArrangeAsync(F1().Lines, aConnected: false);

        var vFirst = await vActions.EstimateAsync(vProjectId);
        var vSecond = await vActions.EstimateAsync(vProjectId);

        Assert.Equal(JsonSerializer.Serialize(vFirst), JsonSerializer.Serialize(vSecond));
        Assert.Equal(new EstimateFigure(2000, 3000, 4000), vFirst!.Row("REQ-FN-301", EstimateSteps.Build).TokensOut);
        var vModelCallers = new[] { typeof(IAgentActions), typeof(IAgentLlmProviderFactory), typeof(IAutoRouter), typeof(IModelChooserFactory), typeof(ILlmConnectionProbe), typeof(IProviderActions), typeof(HttpClient) };
        var vParameters = typeof(EstimateActions).GetConstructors().SelectMany(aCtor => aCtor.GetParameters()).Select(aParameter => aParameter.ParameterType).ToList();
        Assert.DoesNotContain(vParameters, aType => vModelCallers.Any(aCaller => aCaller.IsAssignableFrom(aType)));
    }

    /// <summary>
    /// REQ-FN-111 (BRD-200): with build on tier 1, fixture F5's build is made for claude-opus-5-5 on tier 1 from its 3
    /// runs: 3,000/4,000/4,500 tokens and 330/360/405 s.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-111 the estimate names the routed model claude-opus-5-5 and tier 1")]
    public async Task EstimateNamesTheRoutedModelAndTier()
    {
        var (vActions, vProjectId, _) = await ArrangeAsync(F1([Opus, Sonnet, Opus, Sonnet, Opus]).Lines);

        var vEstimate = (await vActions.EstimateAsync(vProjectId))!;

        var vBuild = vEstimate.Total(EstimateSteps.Build);
        Assert.Equal(Opus, vBuild.Model);
        Assert.Equal(1, vBuild.Tier);
        Assert.Equal(new EstimateFigure(3000, 4000, 4500), vEstimate.Row("REQ-FN-301", EstimateSteps.Build).TokensOut);
        Assert.Equal(new EstimateFigure(330, 360, 405), vEstimate.Row("REQ-FN-301", EstimateSteps.Build).DurationS);
    }

    /// <summary>
    /// REQ-FN-112 (BRD-201): when the owner moves build to tier 2, the very next estimate (same instance, no restart)
    /// reads 2,000/3,000/4,000 tokens over 5 runs from all models for claude-sonnet-5-5, and a run started before the
    /// move keeps the estimate it started with.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-112 moving build to tier 2 changes the next estimate without a restart and a started run keeps its own")]
    public async Task TierChangeMovesTheNextEstimateOnly()
    {
        var (vActions, vProjectId, _) = await ArrangeAsync(F1([Opus, Sonnet, Opus, Sonnet, Opus]).Lines);
        var vBefore = (await vActions.EstimateAsync(vProjectId))!;
        await vActions.ConfirmAsync(vProjectId, vBefore, EstimateRunKinds.Process, 7);

        await new RoutingActions(CreateFactory()).SetWorkTierAsync("write-code", 2);
        var vAfter = (await vActions.EstimateAsync(vProjectId))!;

        var vBuild = vAfter.Total(EstimateSteps.Build);
        Assert.Equal(Sonnet, vBuild.Model);
        Assert.Equal(2, vBuild.Tier);
        Assert.True(vBuild.FromAllModels);
        Assert.Equal(5, vBuild.BasisRuns);
        Assert.Equal(new EstimateFigure(2000, 3000, 4000), vAfter.Row("REQ-FN-301", EstimateSteps.Build).TokensOut);

        var vKept = (await vActions.KeptAsync(EstimateRunKinds.Process, 7))!;
        Assert.Equal(Opus, vKept.Estimate.Total(EstimateSteps.Build).Model);
        Assert.Equal(new EstimateFigure(3000, 4000, 4500), vKept.Estimate.Row("REQ-FN-301", EstimateSteps.Build).TokensOut);
    }

    /// <summary>
    /// REQ-UI-085 (BRD-194): a queued run keeps the estimate it was queued with, and the end-to-end run it becomes
    /// carries the same batch without writing another.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-085 a queued run keeps its estimate and the run it becomes carries the same batch")]
    public async Task QueuedRunKeepsItsEstimate()
    {
        var (vActions, vProjectId, vFolder) = await ArrangeAsync(F1().Lines);
        var vQueued = await vActions.ConfirmAsync(vProjectId, (await vActions.EstimateAsync(vProjectId))!, EstimateRunKinds.Queue, 3);
        var vLinesAfterQueue = File.ReadAllLines(Path.Combine(vFolder, "docs", "metrics", "estimates.jsonl")).Length;

        await vActions.KeepAsync(vProjectId, vQueued, EstimateRunKinds.Auto, 11);

        var vCarried = (await vActions.KeptAsync(EstimateRunKinds.Auto, 11))!;
        Assert.Equal(vQueued.BatchTs, vCarried.BatchTs);
        Assert.Equal(JsonSerializer.Serialize(vQueued.Estimate), JsonSerializer.Serialize(vCarried.Estimate));
        Assert.Equal(vLinesAfterQueue, File.ReadAllLines(Path.Combine(vFolder, "docs", "metrics", "estimates.jsonl")).Length);
    }

    /// <summary>
    /// REQ-FN-115 (BRD-206): confirming an estimate writes estimates.jsonl only: runs.jsonl, the checklist and
    /// PROJECT-STATUS.md are byte for byte as they were.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-115 confirming an estimate leaves runs.jsonl, the checklist and PROJECT-STATUS untouched")]
    public async Task EstimateNeverTouchesMeasuredFiles()
    {
        var (vActions, vProjectId, vFolder) = await ArrangeAsync(F1().Lines);
        File.WriteAllText(Path.Combine(vFolder, "PROJECT-STATUS.md"), "# Fixture status\n");
        var vWatched = new[] { "docs/metrics/runs.jsonl", "docs/Fixture-P2-Checklist.md", "PROJECT-STATUS.md" };
        var vBefore = vWatched.ToDictionary(aPath => aPath, aPath => File.ReadAllBytes(Path.Combine(vFolder, aPath)));

        await vActions.ConfirmAsync(vProjectId, (await vActions.EstimateAsync(vProjectId))!, EstimateRunKinds.Process, 1);
        await vActions.AgainstActualAsync(vProjectId, "2026-11-10T11:16:00Z");

        Assert.All(vWatched, aPath => Assert.Equal(vBefore[aPath], File.ReadAllBytes(Path.Combine(vFolder, aPath))));
        Assert.All(Directory.GetFiles(Path.Combine(vFolder, "docs", "metrics")), aPath =>
            Assert.Contains(Path.GetFileName(aPath), new[] { "runs.jsonl", "estimates.jsonl" }));
    }

    /// <summary>
    /// REQ-FN-113 (BRD-204): at handoff, the phase's latest confirmed batch is set beside the runs measured since:
    /// fixture F6's build reads 10,500 tokens, apportioned, within 6,000/9,000/12,000.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-113 handoff sets the phase's batch beside F6's runs: 10,500 tokens apportioned within")]
    public async Task HandoffShowsThePhaseAgainstItsEstimate()
    {
        var (vActions, vProjectId, vFolder) = await ArrangeAsync(F1().Lines);
        await vActions.ConfirmAsync(vProjectId, (await vActions.EstimateAsync(vProjectId))!, EstimateRunKinds.Process, 1);
        File.WriteAllLines(Path.Combine(vFolder, "docs", "metrics", "runs.jsonl"), F6().Lines);

        var vResult = (await vActions.PhaseAgainstActualAsync(vProjectId))!;

        var vBuild = vResult.Step(EstimateSteps.Build);
        Assert.Equal(10500, vBuild.TokensOut);
        Assert.True(vBuild.Apportioned);
        Assert.Equal("within", vBuild.TokensPosition);
        Assert.Equal(1, vResult.Step(EstimateSteps.Verify).RowlessRunsN);
    }

    /// <summary>
    /// REQ-UI-090 (BRD-208): a project with no run records and no sister projects gets an estimate that reads "No
    /// estimate yet", without an exception, and confirming it still works.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-090 a project with no run records reads no estimate yet, starts, and throws nothing")]
    public async Task NoRunRecordsIsNoEstimateYet()
    {
        var (vActions, vProjectId, _) = await ArrangeAsync(aRunsLines: null);

        var vEstimate = (await vActions.EstimateAsync(vProjectId))!;
        var vKept = await vActions.ConfirmAsync(vProjectId, vEstimate, EstimateRunKinds.Process, 2);

        Assert.True(vEstimate.NoEstimateYet);
        Assert.False(vEstimate.NoChecklist);
        Assert.NotNull(vKept);
    }

    /// <summary>
    /// Owner's answer 4 (BRD-193): a brief with no checklist yet has nothing to estimate, writes nothing, and keeps an
    /// empty estimate that the build step later replaces with one that has rows.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-084 a brief with no checklist writes nothing until its build step")]
    public async Task BriefWithoutChecklistWaitsForTheBuildStep()
    {
        var (vActions, vProjectId, vFolder) = await ArrangeAsync(F1().Lines, aChecklist: false);

        var vEarly = (await vActions.EstimateAsync(vProjectId))!;
        var vKeptEarly = await vActions.ConfirmAsync(vProjectId, vEarly, EstimateRunKinds.Auto, 5);

        Assert.True(vEarly.NoChecklist);
        Assert.Equal(string.Empty, vKeptEarly.BatchTs);
        Assert.False(File.Exists(Path.Combine(vFolder, "docs", "metrics", "estimates.jsonl")));

        File.WriteAllText(Path.Combine(vFolder, "docs", "Fixture-P2-Checklist.md"), Checklist(PhaseRows));
        var vLater = await vActions.ConfirmAsync(vProjectId, (await vActions.EstimateAsync(vProjectId))!, EstimateRunKinds.Auto, 5);
        Assert.Equal("2026-11-10T11:16:00Z", vLater.BatchTs);
        Assert.Equal(3, (await vActions.KeptAsync(EstimateRunKinds.Auto, 5))!.Estimate.Rows.Count);
    }

    /// <summary>
    /// REQ-UI-086 / REQ-FN-108 (BRD-196, BRD-197): a sister project is listed with its run count and its runs fill in
    /// the estimate under its folder name; a folder without docs/metrics/runs.jsonl is refused with the reason.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-086 REQ-FN-108 a sister project is listed with its run count and a folder without runs.jsonl is refused")]
    public async Task SisterProjectsAreNamedAndRefused()
    {
        var (vActions, vProjectId, _) = await ArrangeAsync(aRunsLines: null);
        objFolders.Write("S1/docs/metrics/runs.jsonl", string.Join("\n", F1().Lines) + "\n");
        objFolders.Write("Empty/README.md", "no metrics here\n");

        var vSister = await vActions.AddSisterAsync(Path.Combine(objFolders.Root, "S1"));
        var vRefused = await Assert.ThrowsAsync<ArgumentException>(() => vActions.AddSisterAsync(Path.Combine(objFolders.Root, "Empty")));
        var vTwice = await Assert.ThrowsAsync<ArgumentException>(() => vActions.AddSisterAsync(Path.Combine(objFolders.Root, "S1")));

        Assert.Equal("S1", vSister.Name);
        Assert.Equal(5, vSister.RunCount);
        Assert.Contains("has no docs/metrics/runs.jsonl", vRefused.Message);
        Assert.Contains("already a sister project", vTwice.Message);
        Assert.Single(await vActions.SistersAsync());

        var vEstimate = (await vActions.EstimateAsync(vProjectId))!;
        Assert.Equal(["S1"], vEstimate.Total(EstimateSteps.Build).SisterProjects);
        Assert.Equal(EstimateBasis.Family, vEstimate.Total(EstimateSteps.Build).Basis);

        await vActions.RemoveSisterAsync(vSister.SisterProjectId);
        Assert.Empty(await vActions.SistersAsync());
        Assert.True((await vActions.EstimateAsync(vProjectId))!.NoEstimateYet);
    }

    private async Task<(EstimateActions Actions, int ProjectId, string Folder)> ArrangeAsync(
        IReadOnlyList<string>? aRunsLines,
        bool aChecklist = true,
        bool aConnected = true)
    {
        var vFolder = Path.Combine(objFolders.Root, "Fixture");
        Directory.CreateDirectory(Path.Combine(vFolder, "docs"));
        if (aChecklist)
        {
            objFolders.Write("Fixture/docs/Fixture-P2-Checklist.md", Checklist(PhaseRows));
        }

        if (aRunsLines is not null)
        {
            objFolders.Write("Fixture/docs/metrics/runs.jsonl", string.Join("\n", aRunsLines) + "\n");
        }

        SeedModels(aConnected);
        var vRouting = new RoutingActions(CreateFactory());
        await vRouting.SetWorkTierAsync("write-code", 1);
        var vProjectId = CreateProject("Fixture", vFolder);
        var vActions = new EstimateActions(CreateFactory(), vRouting, new ProjectFolders(CreateFactory()), new FixedClock(ConfirmedAt));
        return (vActions, vProjectId, vFolder);
    }

    private void SeedModels(bool aConnected)
    {
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText =
            """
            INSERT INTO Provider (Name, Connector, SignInMethod, BaseUrl, SecretName, State)
            VALUES ('Anthropic', 'anthropic', 'Key', 'http://127.0.0.1:9', NULL, @State);
            INSERT INTO Model (ProviderId, Tier, Identifier) VALUES (last_insert_rowid(), 1, 'claude-opus-5-5');
            INSERT INTO Model (ProviderId, Tier, Identifier) SELECT ProviderId, 2, 'claude-sonnet-5-5' FROM Provider WHERE Name = 'Anthropic';
            """;
        vCommand.Parameters.AddWithValue("@State", aConnected ? "Connected" : "Failed");
        vCommand.ExecuteNonQuery();
    }

    private static JsonObject SchemaExample()
    {
        var vRoot = new DirectoryInfo(AppContext.BaseDirectory);
        while (vRoot is not null && !File.Exists(Path.Combine(vRoot.FullName, "Chatur.sln")))
        {
            vRoot = vRoot.Parent;
        }

        var vText = File.ReadAllText(Path.Combine(vRoot!.FullName, "docs", "Chatur-Metrics-Schema.md"));
        var vSection = vText[vText.IndexOf("## `estimates.jsonl`", StringComparison.Ordinal)..];
        var vStart = vSection.IndexOf("```json", StringComparison.Ordinal) + "```json".Length;
        var vEnd = vSection.IndexOf("```", vStart, StringComparison.Ordinal);
        return JsonNode.Parse(vSection[vStart..vEnd])!.AsObject();
    }

    private sealed class FixedClock(DateTime aUtcNow) : IClock
    {
        public DateTime UtcNow { get; } = aUtcNow;
    }
}
