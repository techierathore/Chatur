using Chatur.Core.Actions;
using Chatur.Core.AutoRun;
using Chatur.Core.Estimates;
using Chatur.Core.ProcessRuns;
using Chatur.Core.Routing;
using Chatur.Tests.AutoRun;
using Chatur.Tests.Support;
using Xunit;
using static Chatur.Tests.Estimates.EstimateFixtures;

namespace Chatur.Tests.Estimates;

/// <summary>An end-to-end run writes its estimate when it reaches the build step (owner's answer 4, BRD-193).</summary>
public sealed class AutoRunEstimateTests : MigratedDatabaseFixture
{
    private const string Brief = "Build the fixture phase and verify it before you stop, tagging every REQ id.";

    private readonly string objFolder = Path.Combine(Chatur.Tests.Support.TestScratch.Root, $"chatur-estimate-auto-{Guid.NewGuid():N}");

    /// <summary>Creates the project folder with phase 2's checklist and F1's runs.</summary>
    public AutoRunEstimateTests()
    {
        Directory.CreateDirectory(Path.Combine(objFolder, "docs", "metrics"));
        File.WriteAllText(Path.Combine(objFolder, "docs", "Fixture-P2-Checklist.md"), Checklist(PhaseRows));
        File.WriteAllLines(Path.Combine(objFolder, "docs", "metrics", "runs.jsonl"), F1().Lines);
    }

    /// <summary>Removes the project folder as well as the database.</summary>
    public override void Dispose()
    {
        if (Directory.Exists(objFolder))
        {
            Directory.Delete(objFolder, true);
        }

        base.Dispose();
    }

    /// <summary>
    /// REQ-UI-084 (BRD-193): a run started from a brief with no estimate kept writes one when it reaches build-phase,
    /// and the chain carries on to the end without stopping for it.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-084 an end-to-end run writes its estimate at the build step and does not stop")]
    public async Task RunWritesItsEstimateAtTheBuildStep()
    {
        var vEstimates = new EstimateActions(CreateFactory(), new RoutingActions(CreateFactory()), new ProjectFolders(CreateFactory()), new ManualClock());
        var vHarness = new AutoRunHarness(CreateFactory(), aEstimates: vEstimates);
        var vProject = CreateProject("Fixture", objFolder);

        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);
        var vAfter = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);

        Assert.All(vAfter!.Chain, aProcess => Assert.Equal(AutoRunProcessState.Finished, aProcess.State));
        var vKept = await vEstimates.KeptAsync(EstimateRunKinds.Auto, vStarted.AutoRunId);
        Assert.NotNull(vKept);
        Assert.Equal(PhaseRows, vKept!.Estimate.Rows);
        Assert.NotEqual(string.Empty, vKept.BatchTs);
        Assert.True(File.Exists(Path.Combine(objFolder, "docs", "metrics", "estimates.jsonl")));
    }
}
