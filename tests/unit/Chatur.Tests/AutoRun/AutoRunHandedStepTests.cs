using Chatur.Core.Actions;
using Chatur.Core.AutoRun;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Chatur.Core.ProcessRuns;
using Chatur.Core.Processes;
using Chatur.Tests.Support;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using TechieRag.Models;
using Xunit;

namespace Chatur.Tests.AutoRun;

/// <summary>
/// Tests for the end-to-end run carrying a step that is handed to a real agent (REQ-FN-089, REQ-FN-091): the real
/// agent path with a scripted model, under the run engine and the end-to-end run.
/// </summary>
public sealed class AutoRunHandedStepTests : MigratedDatabaseFixture
{
    private const string Brief = "Finish the coverage panel and verify it before you stop, tagging every REQ id.";

    private readonly string objFolder = Path.Combine(Path.GetTempPath(), $"chatur-auto-hand-{Guid.NewGuid():N}");

    /// <summary>
    /// When the model of a handed step reports a usage limit, then the run waits, resumes that same step in the same
    /// session by itself, and the wait is recorded against that step (REQ-FN-089).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-089 a handed step's usage limit is waited out, the step resumed and the wait recorded against it")]
    public async Task HandedStepLimitIsWaitedOut()
    {
        var vLimit = new LlmRateLimitException("limit reached", System.Net.HttpStatusCode.TooManyRequests, TimeSpan.FromMinutes(15));
        var vModel = new ScriptedModel(ScriptedModel.Say("Built it after the wait.")) { FailWith = vLimit };
        var vWiring = Wire(vModel);
        var vProject = CreateProject("App", objFolder);

        var vStarted = await vWiring.Auto.StartAsync(vProject, Brief);
        var vAfter = await vWiring.Auto.AdvanceAsync(vStarted.AutoRunId);

        var vWait = Assert.Single(vAfter!.Events, aEvent => aEvent.Kind == AutoRunEventKind.LimitWait);
        Assert.Contains("step 4 of build-phase", vWait.Message, StringComparison.Ordinal);
        Assert.Single(vAfter.Events, aEvent => aEvent.Kind == AutoRunEventKind.LimitResumed);
        Assert.Equal(AutoRunState.ReadyForAcceptance, vAfter.State);
        Assert.Equal(TimeSpan.FromMinutes(15), vWiring.Delay.Waited.Aggregate(TimeSpan.Zero, (aSum, aSpan) => aSum + aSpan));
        var vRunId = (int)vAfter.Chain.First(aProcess => aProcess.ProcessCode == "build-phase").ProcessRunId!.Value;
        var vWork = await vWiring.Steps.WorkAsync(vRunId, 4);
        Assert.Equal(ProcessStepWorkState.Finished, vWork!.State);
        Assert.Equal("Built it after the wait.", vWork.FinalMessage);
    }

    /// <summary>
    /// When a handed step runs inside an end-to-end run, then the agent's file is written without waiting for approval,
    /// while a source-control command it asks for is still refused (REQ-FN-091).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-091 an unattended handed step writes without approval and the source-control refusal still applies")]
    public async Task UnattendedHandedStepWritesWithoutApproval()
    {
        var vModel = new ScriptedModel(
            ScriptedModel.Call("edit-file", """{"path":"src/Feature.cs","content":"class Feature {}"}"""),
            ScriptedModel.Call("run-source-control", """{"command":"commit"}"""),
            ScriptedModel.Say("Feature.cs written."));
        var vWiring = Wire(vModel);
        var vProject = CreateProject("App", objFolder);

        var vStarted = await vWiring.Auto.StartAsync(vProject, Brief);
        var vAfter = await vWiring.Auto.AdvanceAsync(vStarted.AutoRunId);

        Assert.Equal(AutoRunState.ReadyForAcceptance, vAfter!.State);
        var vRunId = (int)vAfter.Chain.First(aProcess => aProcess.ProcessCode == "build-phase").ProcessRunId!.Value;
        var vWork = await vWiring.Steps.WorkAsync(vRunId, 4);
        Assert.Equal(["src/Feature.cs"], vWork!.FilesWritten);
        Assert.Empty(vWork.FilesProposed);
        Assert.Single(vWork.Refusals);
        Assert.True(File.Exists(Path.Combine(objFolder, "src", "Feature.cs")));
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        if (Directory.Exists(objFolder))
        {
            Directory.Delete(objFolder, true);
        }
    }

    private Wiring Wire(ScriptedModel aModel)
    {
        Directory.CreateDirectory(Path.Combine(objFolder, "docs"));
        File.WriteAllText(Path.Combine(objFolder, "docs", "App-Checklist.md"), "# App — Checklist (phase 1)\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|---|---|---|---|---|\n| REQ-FN-001 | One | Verified | 100% | — | [view](#d-req-fn-001) |\n");
        using (var vConnection = new SqliteConnection(ConnectionString))
        {
            vConnection.Open();
            foreach (var vTier in new[] { 1, 2, 3 })
            {
                var vProviderId = vConnection.ExecuteScalar<long>(
                    "INSERT INTO Provider (Name, Connector, SignInMethod, BaseUrl, SecretName, State) VALUES (@vName, 'Fake', 'Key', 'http://localhost', NULL, 'Connected'); SELECT last_insert_rowid();",
                    new { vName = $"tier-{vTier}" });
                vConnection.Execute("INSERT INTO Model (ProviderId, Tier, Identifier) VALUES (@vProviderId, @vTier, @vName)", new { vProviderId, vTier, vName = $"tier-{vTier}-model" });
            }
        }

        var vFactory = CreateFactory();
        var vSteps = StepAgentWiring.StepAgents(vFactory, aModel, objFolder);
        var vClock = new ManualClock();
        var vDelay = new ManualDelay(vClock);
        var vEngineClock = new SystemClock();
        var vEngine = new ProcessRunActions(
            vFactory,
            new HandingExecutor(vSteps),
            new Chatur.Core.Measurements.MeasurementActions(vFactory, NullLogger<Chatur.Core.Measurements.MeasurementActions>.Instance),
            new QuietVerdictWriter(),
            new RunEvidenceStore(vFactory, vEngineClock),
            vEngineClock,
            NullLogger<ProcessRunActions>.Instance,
            vSteps);
        var vStore = new AutoRunStore(vFactory, vClock);
        var vViews = new AutoRunViewBuilder(vStore, vEngine, new OpenRequirementReader(new ProjectFolders(vFactory)));
        var vAuto = new AutoRunActions(
            vStore,
            vViews,
            new ProcessActions(vFactory, vEngineClock, NullLogger<ProcessActions>.Instance),
            vEngine,
            vEngine,
            new RecordingLauncher(),
            vDelay,
            new AutoRunOptions { StallTimeout = TimeSpan.FromSeconds(30) },
            vClock,
            NullLogger<AutoRunActions>.Instance);
        return new Wiring(vAuto, vSteps, vDelay);
    }

    private sealed record Wiring(AutoRunActions Auto, IStepAgentActions Steps, ManualDelay Delay);

    /// <summary>Hands step 4 of build-phase to the agent and passes every other step.</summary>
    private sealed class HandingExecutor : IProcessStepExecutor
    {
        private readonly IStepAgentActions objSteps;

        public HandingExecutor(IStepAgentActions aSteps) => objSteps = aSteps;

        public async Task<ProcessStepOutcome> ExecuteAsync(int aRunId, int aProjectId, string aProcessCode, ProcessRunStepView aStep, CancellationToken aCt = default)
        {
            if (aProcessCode != "build-phase" || aStep.Position != 4)
            {
                return new ProcessStepOutcome(true, "Done");
            }

            var vWork = await objSteps.HandAsync(aRunId, aStep.Position, aCt);
            return new ProcessStepOutcome(true, vWork.FinalMessage ?? "Done");
        }
    }
}
