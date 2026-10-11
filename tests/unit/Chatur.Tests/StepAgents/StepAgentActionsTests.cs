using Chatur.Core.Actions;
using Chatur.Core.Agent;
using Chatur.Core.ProcessRuns;
using Chatur.Core.StepAgents;
using Chatur.Tests.Support;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Chatur.Tests.StepAgents;

/// <summary>
/// Tests for handing a work step to the agent that owns its process, over the real agent path (agent actions, guard
/// pipeline, routing, session records) with a scripted model (REQ-FN-084, REQ-FN-085, REQ-FN-086, REQ-FN-087, REQ-FN-090).
/// </summary>
public sealed class StepAgentActionsTests : MigratedDatabaseFixture
{
    private readonly string objProjectPath = Path.Combine(Path.GetTempPath(), $"chatur-step-{Guid.NewGuid():N}");

    /// <summary>
    /// When a run reaches a work step, then a session starts with the process's agent, and the agent is given the
    /// step's wording, the run and the project (REQ-FN-084).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-084 a work step starts a session with the process's agent, given the wording, the run and the project")]
    public async Task WorkStepStartsASessionWithTheProcessAgent()
    {
        var vRunId = StartRun("build-phase", 4);
        InsertConnectedModel(1, "tier-one-model");
        var vModel = new ScriptedModel(ScriptedModel.Say("Built it."));

        var vWork = await StepAgentWiring.StepAgents(CreateFactory(), vModel, objProjectPath).HandAsync(vRunId, 4);

        Assert.Equal("Flow master", vWork.AgentName);
        Assert.Equal("Flow master", Scalar<string>("SELECT r.Name FROM Session s JOIN Role r ON r.RoleId = s.RoleId WHERE s.SessionId = @Id", vWork.SessionId));
        var vTask = Scalar<string>("SELECT Payload FROM SessionEvent WHERE SessionId = @Id AND Kind = 'user'", vWork.SessionId);
        Assert.Contains("Build the cluster to the coding standards", vTask, StringComparison.Ordinal);
        Assert.Contains($"run {vRunId}", vTask, StringComparison.Ordinal);
        Assert.Contains("ProjectUnderTest", vTask, StringComparison.Ordinal);
        Assert.Equal(vTask, vWork.Task);
    }

    /// <summary>
    /// When the analyst's step asks for the edit tool its rights withhold, then the tool is refused, nothing is written
    /// and the refusal is recorded on the step (REQ-FN-085).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-085 a tool the agent's rights withhold is refused and the refusal is recorded on the step")]
    public async Task WithheldToolIsRefusedAndRecorded()
    {
        var vRunId = StartRun("day1-greenfield", 3);
        InsertConnectedModel(3, "tier-three-model");
        var vModel = new ScriptedModel(
            ScriptedModel.Call("edit-file", """{"path":"src/Brief.cs","content":"x"}"""),
            ScriptedModel.Call("run-source-control", """{"command":"commit"}"""),
            ScriptedModel.Say("I could not write the brief."));

        var vWork = await StepAgentWiring.StepAgents(CreateFactory(), vModel, objProjectPath).HandAsync(vRunId, 3);

        Assert.Equal(2, vWork.Refusals.Count);
        Assert.Contains("edit-file", vWork.Refusals[0], StringComparison.Ordinal);
        Assert.Empty(vWork.FilesWritten);
        Assert.False(File.Exists(Path.Combine(objProjectPath, "src", "Brief.cs")));
    }

    /// <summary>
    /// When the analyst's handed step writes a document under docs/ and then a source file, then the document is
    /// written, the source write is refused with the reason and the refusal is recorded on the step (REQ-FN-105).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-105 a handed step writes its documents but a source write is refused and recorded")]
    public async Task HandedStepWritesDocumentsButNotSource()
    {
        var vRunId = StartRun("day1-greenfield", 3);
        InsertConnectedModel(3, "tier-three-model");
        var vModel = new ScriptedModel(
            ScriptedModel.Call("edit-file", """{"path":"docs/Brief.md","content":"x"}"""),
            ScriptedModel.Call("edit-file", """{"path":"src/Brief.cs","content":"x"}"""),
            ScriptedModel.Say("Wrote the brief."));

        ProcessStepWork vWork;
        using (ModelCallScope.ForRun())
        {
            vWork = await StepAgentWiring.StepAgents(CreateFactory(), vModel, objProjectPath).HandAsync(vRunId, 3);
        }

        Assert.Single(vWork.Refusals);
        Assert.Contains("src/Brief.cs", vWork.Refusals[0], StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(objProjectPath, "docs", "Brief.md")));
        Assert.False(File.Exists(Path.Combine(objProjectPath, "src", "Brief.cs")));
        Assert.Contains("docs/Brief.md", vWork.FilesWritten);
    }

    /// <summary>
    /// When a handed step starts, then the agent's session uses the model of the agent's tier, and the step names that
    /// model before the agent has answered and after (REQ-FN-086).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-086 the handed step names the model routed to its agent's tier")]
    public async Task HandedStepNamesTheModelOfTheAgentsTier()
    {
        var vRunId = StartRun("day1-greenfield", 1);
        InsertConnectedModel(1, "tier-one-model");
        InsertConnectedModel(3, "tier-three-model");
        var vGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var vModel = new ScriptedModel(ScriptedModel.Say("Reading the idea now.")) { Gate = vGate, ModelNameOverride = "tier-three-model" };
        var vSteps = StepAgentWiring.StepAgents(CreateFactory(), vModel, objProjectPath);

        var vHanding = vSteps.HandAsync(vRunId, 1);
        var vWorking = await WaitForWorkAsync(vSteps, vRunId, 1);
        vGate.SetResult();
        var vDone = await vHanding;

        Assert.Equal(ProcessStepWorkState.Working, vWorking.State);
        Assert.Equal("tier-three-model", vWorking.ModelName);
        Assert.Equal(3, vWorking.Tier);
        Assert.Equal("tier-three-model", vDone.ModelName);
        Assert.Equal(3, vDone.Tier);
    }

    /// <summary>
    /// When a handed step's agent has not finished, then the step is still working; when it finishes, then the step ends
    /// recording the files it wrote, the commands it ran and its final message (REQ-FN-087).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-087 the step ends only when the agent finishes, recording files, commands and the final message")]
    public async Task StepEndsOnlyWhenTheAgentFinishes()
    {
        var vRunId = StartRun("build-phase", 4);
        InsertConnectedModel(1, "tier-one-model");
        var vGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var vModel = new ScriptedModel(
            ScriptedModel.Call("edit-file", """{"path":"src/Feature.cs","content":"class Feature {}"}"""),
            ScriptedModel.Call("run-build", "{}"),
            ScriptedModel.Say("All done, Feature.cs is written and builds.")) { Gate = vGate };
        var vSteps = StepAgentWiring.StepAgents(CreateFactory(), vModel, objProjectPath);

        Task<ProcessStepWork> vHanding;
        using (ModelCallScope.ForRun())
        {
            vHanding = vSteps.HandAsync(vRunId, 4);
        }

        var vWorking = await WaitForWorkAsync(vSteps, vRunId, 4);
        await WaitUntilAsync(async () => (await vSteps.WorkAsync(vRunId, 4))!.CommandsRun.Count == 1);
        Assert.False(vHanding.IsCompleted);
        Assert.Equal(ProcessStepWorkState.Working, (await vSteps.WorkAsync(vRunId, 4))!.State);

        vGate.SetResult();
        var vDone = await vHanding;

        Assert.Equal(ProcessStepWorkState.Finished, vDone.State);
        Assert.Equal(["src/Feature.cs"], vDone.FilesWritten);
        Assert.Equal(["Build the project."], vDone.CommandsRun);
        Assert.Equal("All done, Feature.cs is written and builds.", vDone.FinalMessage);
        Assert.True(File.Exists(Path.Combine(objProjectPath, "src", "Feature.cs")));
        Assert.Equal(vWorking.SessionId, vDone.SessionId);
    }

    /// <summary>
    /// When the step is worked in a session the owner has set to ask first, then the agent's edit waits for approval:
    /// it is listed as proposed and not as written (REQ-FN-087, owner's session mode kept).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-087 an attended step keeps the owner's ask-first mode, so its edit is proposed not written")]
    public async Task AttendedStepKeepsAskFirst()
    {
        var vRunId = StartRun("build-phase", 4);
        InsertConnectedModel(1, "tier-one-model");
        var vModel = new ScriptedModel(
            ScriptedModel.Call("edit-file", """{"path":"src/Feature.cs","content":"class Feature {}"}"""),
            ScriptedModel.Say("Proposed Feature.cs."));

        var vWork = await StepAgentWiring.StepAgents(CreateFactory(), vModel, objProjectPath).HandAsync(vRunId, 4);

        Assert.Equal(["src/Feature.cs"], vWork.FilesProposed);
        Assert.Empty(vWork.FilesWritten);
        Assert.Empty(vWork.Refusals);
        Assert.False(File.Exists(Path.Combine(objProjectPath, "src", "Feature.cs")));
    }

    /// <summary>
    /// When the owner stops while the agent works, then the agent's session ends, the step reads stopped by the owner
    /// and what the agent had already written stays (REQ-FN-090).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-090 stop ends the agent's session and the step reads stopped by the owner")]
    public async Task StopEndsTheSessionAndTheStepReadsStopped()
    {
        var vRunId = StartRun("build-phase", 4);
        InsertConnectedModel(1, "tier-one-model");
        var vGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var vModel = new ScriptedModel(
            ScriptedModel.Call("edit-file", """{"path":"src/Early.cs","content":"class Early {}"}"""),
            ScriptedModel.Say("Still working on the rest.")) { Gate = vGate };
        var vSteps = StepAgentWiring.StepAgents(CreateFactory(), vModel, objProjectPath);

        Task<ProcessStepWork> vHanding;
        using (ModelCallScope.ForRun())
        {
            vHanding = vSteps.HandAsync(vRunId, 4);
        }

        var vWorking = await WaitForWorkAsync(vSteps, vRunId, 4);
        await WaitUntilAsync(async () => (await vSteps.WorkAsync(vRunId, 4))!.FilesWritten.Count == 1);
        var vStopped = await vSteps.StopAsync(vRunId);
        var vDone = await vHanding.WaitAsync(TimeSpan.FromSeconds(20));

        Assert.True(vStopped);
        Assert.True(vDone.StoppedByOwner);
        Assert.Equal("Stopped", Scalar<string>("SELECT State FROM Session WHERE SessionId = @Id", vWorking.SessionId));
        Assert.Equal(["src/Early.cs"], vDone.FilesWritten);
        Assert.True(File.Exists(Path.Combine(objProjectPath, "src", "Early.cs")));
    }

    /// <summary>When no step of the run is being worked, then stopping does nothing (REQ-FN-090).</summary>
    [Fact(DisplayName = "REQ-FN-090 stopping a run with no agent at work does nothing")]
    public async Task StopWithNothingWorkingDoesNothing()
    {
        var vRunId = StartRun("build-phase", 1);

        var vStopped = await StepAgentWiring.StepAgents(CreateFactory(), new ScriptedModel(), objProjectPath).StopAsync(vRunId);

        Assert.False(vStopped);
    }

    /// <summary>
    /// When the model reports a usage limit for an unattended run, then the hand-over lets it through with the step left
    /// handed, and handing the step again carries on in the same session (REQ-FN-084 with REQ-FN-064).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-084 an interrupted handed step is carried on in the same session")]
    public async Task InterruptedStepCarriesOnInTheSameSession()
    {
        var vRunId = StartRun("build-phase", 4);
        InsertConnectedModel(1, "tier-one-model");
        var vLimit = new TechieRag.Models.LlmRateLimitException("limit reached", System.Net.HttpStatusCode.TooManyRequests, TimeSpan.FromMinutes(1));
        var vModel = new ScriptedModel(ScriptedModel.Say("Done after the wait.")) { FailWith = vLimit };
        var vSteps = StepAgentWiring.StepAgents(CreateFactory(), vModel, objProjectPath);

        ModelUsageLimitException? vCaught = null;
        using (ModelCallScope.ForRun())
        {
            try
            {
                await vSteps.HandAsync(vRunId, 4);
            }
            catch (ModelUsageLimitException aEx)
            {
                vCaught = aEx;
            }

            var vInterrupted = await vSteps.WorkAsync(vRunId, 4);
            Assert.NotNull(vCaught);
            Assert.Equal(ProcessStepWorkState.Working, vInterrupted!.State);

            var vDone = await vSteps.HandAsync(vRunId, 4);

            Assert.Equal(vInterrupted.SessionId, vDone.SessionId);
            Assert.Equal(1, Scalar<long>("SELECT COUNT(*) FROM Session WHERE ProjectId = (SELECT ProjectId FROM ProcessRun WHERE RunId = @Id)", vRunId));
            Assert.Equal("Done after the wait.", vDone.FinalMessage);
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        if (Directory.Exists(objProjectPath))
        {
            Directory.Delete(objProjectPath, true);
        }
    }

    private int StartRun(string aProcess, int aStep)
    {
        Directory.CreateDirectory(objProjectPath);
        var vProjectId = CreateProject("ProjectUnderTest", objProjectPath);
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        return (int)vConnection.ExecuteScalar<long>(
            """
            INSERT INTO ProcessRun (ProjectId, ProcessCode, State, CurrentStep, StartedUtc)
            VALUES (@vProjectId, @aProcess, 'running', @aStep, @Started);
            SELECT last_insert_rowid();
            """,
            new { vProjectId, aProcess, aStep, Started = DateTime.UtcNow.ToString("O") });
    }

    private void InsertConnectedModel(int aTier, string aIdentifier)
    {
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        var vProviderId = vConnection.ExecuteScalar<long>(
            "INSERT INTO Provider (Name, Connector, SignInMethod, BaseUrl, SecretName, State) VALUES (@aIdentifier, 'Fake', 'Key', 'http://localhost', NULL, 'Connected'); SELECT last_insert_rowid();",
            new { aIdentifier });
        vConnection.Execute(
            "INSERT INTO Model (ProviderId, Tier, Identifier) VALUES (@vProviderId, @aTier, @aIdentifier)",
            new { vProviderId, aTier, aIdentifier });
    }

    private T Scalar<T>(string aSql, long aId)
    {
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        return vConnection.ExecuteScalar<T>(aSql, new { Id = aId })!;
    }

    private static async Task<ProcessStepWork> WaitForWorkAsync(StepAgentActions aSteps, int aRunId, int aPosition)
    {
        ProcessStepWork? vWork = null;
        await WaitUntilAsync(async () => (vWork = await aSteps.WorkAsync(aRunId, aPosition)) is not null);
        return vWork!;
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> aCondition)
    {
        var vDeadline = DateTime.UtcNow.AddSeconds(20);
        while (!await aCondition())
        {
            Assert.True(DateTime.UtcNow < vDeadline, "The condition was not reached in 20 seconds.");
            await Task.Delay(25);
        }
    }
}
