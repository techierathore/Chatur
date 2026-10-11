using System.Text.Json;
using Chatur.Core.Actions;
using Chatur.Core.ProcessRuns;
using Chatur.Tests.Support;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Chatur.Tests.ProcessRuns;

/// <summary>
/// Tests for a whole process run whose work steps are handed to the process's agent and whose checks judge the project's
/// files on disk, on refresh-status (shape SsCs): the real engine, executor, agent path and guards, with a scripted model
/// (REQ-FN-084, REQ-FN-087, REQ-FN-088, REQ-FN-090, REQ-FN-096).
/// </summary>
public sealed class HandedRunTests : MigratedDatabaseFixture
{
    private const string Checklist =
        "# X — Checklist\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|-------------|--------|---|---------|---------|\n"
        + "| REQ-FN-001 | One | Implemented | 100% | | |\n| REQ-FN-002 | Two | Not Started | 0% | | |\n";

    private readonly string objProjectPath = Path.Combine(Path.GetTempPath(), $"chatur-handed-{Guid.NewGuid():N}");
    private readonly string objAppData = Path.Combine(Path.GetTempPath(), $"chatur-handed-app-{Guid.NewGuid():N}");

    private static UnattendedAdvance Policy => new(TimeSpan.FromMinutes(5));

    /// <summary>
    /// When every step of the run is handed to the agent and the status the agent wrote counts each status as the checklists
    /// do, then the run finishes, each handed step records what the agent did, and the checks passed on the files on disk
    /// (REQ-FN-084, REQ-FN-087, REQ-FN-088, REQ-FN-096).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-088 a run finishes when the checks find the agent's files right on disk")]
    public async Task RunFinishesWhenTheDiskIsRight()
    {
        var vRunId = Prepare(Checklist);
        var vModel = new ScriptedModel(
            ScriptedModel.Say("I read every checklist."),
            ScriptedModel.Say("Counted: 1 implemented, 1 not started."),
            ScriptedModel.Call("edit-file", StatusEdit("| Status | Count |\n|---|---|\n| Implemented | 1 |\n| Not Started | 1 |\n")),
            ScriptedModel.Say("Wrote the status document."));
        var vEngine = StepAgentWiring.Engine(CreateFactory(), objAppData, vModel, objProjectPath);

        var vRun = await vEngine.AdvanceUnattendedAsync(vRunId, Policy);

        Assert.Equal(ProcessRunState.Finished, vRun!.State);
        Assert.All(vRun.Steps.Where(aStep => aStep.Kind == ProcessStepKind.Step), aStep => Assert.Equal("Flow master", aStep.Work!.AgentName));
        Assert.Null(vRun.Steps[2].Work);
        Assert.Equal(["PROJECT-STATUS.md"], vRun.Steps[3].Work!.FilesWritten);
        Assert.Equal("Wrote the status document.", vRun.Steps[3].Work!.FinalMessage);
        Assert.StartsWith("The Flow master (scripted-model) wrote 1 file", vRun.Steps[3].Result, StringComparison.Ordinal);
        Assert.Contains("add up", vRun.Steps[2].Result, StringComparison.Ordinal);
    }

    /// <summary>
    /// When the agent's status document counts a status differently from the checklists, then the check at that step fails
    /// from the file on disk, the run stops there with the reason, and the agent's own message does not change that
    /// (REQ-FN-088, REQ-FN-096).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-088 a failed check stops the run at that step with the reason")]
    public async Task FailedCheckStopsTheRunWithTheReason()
    {
        var vRunId = Prepare(Checklist);
        var vModel = new ScriptedModel(
            ScriptedModel.Say("Read."),
            ScriptedModel.Say("Counted."),
            ScriptedModel.Call("edit-file", StatusEdit("| Status | Count |\n|---|---|\n| Implemented | 5 |\n| Not Started | 1 |\n")),
            ScriptedModel.Say("Everything matches, I am sure."));
        var vEngine = StepAgentWiring.Engine(CreateFactory(), objAppData, vModel, objProjectPath);

        var vRun = await vEngine.AdvanceUnattendedAsync(vRunId, Policy);

        Assert.Equal(ProcessRunState.Stopped, vRun!.State);
        Assert.Equal(4, vRun.CurrentPosition);
        Assert.Equal(ProcessStepState.Failed, vRun.Steps[3].State);
        Assert.Contains("counts 5 implemented but the checklists have 1", vRun.Steps[3].Result, StringComparison.Ordinal);
    }

    /// <summary>When the project has no checklist, then the check after the first handed steps fails instead of passing on "not carried out" (REQ-FN-088).</summary>
    [Fact(DisplayName = "REQ-FN-088 a check never passes on a step that left nothing on disk")]
    public async Task CheckNeverPassesWithNothingOnDisk()
    {
        var vRunId = Prepare(null);
        var vModel = new ScriptedModel(ScriptedModel.Say("Nothing to read."), ScriptedModel.Say("Nothing to count."));
        var vEngine = StepAgentWiring.Engine(CreateFactory(), objAppData, vModel, objProjectPath);

        var vRun = await vEngine.AdvanceUnattendedAsync(vRunId, Policy);

        Assert.Equal(ProcessRunState.Stopped, vRun!.State);
        Assert.Equal(2, vRun.CurrentPosition);
        Assert.Contains("no checklist", vRun.Steps[1].Result, StringComparison.Ordinal);
        Assert.DoesNotContain("Not carried out", vRun.Steps[1].Result, StringComparison.Ordinal);
    }

    /// <summary>
    /// When no model is connected, then the agent cannot work the step, the step does not read as done, and the run stops
    /// there (REQ-FN-084).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-084 a handed step the agent could not work stops the run")]
    public async Task StepTheAgentCouldNotWorkStopsTheRun()
    {
        var vRunId = Prepare(Checklist, connectModel: false);
        var vEngine = StepAgentWiring.Engine(CreateFactory(), objAppData, new ScriptedModel(), objProjectPath);

        var vRun = await vEngine.AdvanceUnattendedAsync(vRunId, Policy);

        Assert.Equal(ProcessRunState.Stopped, vRun!.State);
        Assert.Equal(1, vRun.CurrentPosition);
        Assert.Contains("could not work this step", vRun.Steps[0].Result, StringComparison.Ordinal);
    }

    /// <summary>
    /// When the owner presses Stop during a handed step, then the agent's session ends, the step reads stopped by the owner
    /// and the run ends there; what the agent had written stays (REQ-FN-090).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-090 stop during a handed step ends the agent's work and the step reads stopped by the owner")]
    public async Task StopDuringAHandedStepEndsTheRun()
    {
        var vRunId = Prepare(Checklist);
        var vGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var vModel = new ScriptedModel(ScriptedModel.Say("Reading the checklists now, one by one.")) { Gate = vGate };
        var vEngine = StepAgentWiring.Engine(CreateFactory(), objAppData, vModel, objProjectPath);

        var vAdvancing = vEngine.AdvanceAsync(vRunId);
        await WaitUntilAsync(async () => (await vEngine.GetAsync(vRunId))!.Steps[0].Work is not null);
        var vStopped = await vEngine.StopAsync(vRunId).WaitAsync(TimeSpan.FromSeconds(20));
        var vAfter = await vAdvancing.WaitAsync(TimeSpan.FromSeconds(20));

        Assert.Equal(ProcessRunState.Stopped, vStopped!.State);
        Assert.Equal(ProcessRunState.Stopped, vAfter!.State);
        Assert.Equal("Stopped by the owner.", vAfter.Steps[0].Result);
        Assert.True(vAfter.Steps[0].Work!.StoppedByOwner);
        Assert.Equal(ProcessStepState.ToCome, vAfter.Steps[1].State);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        foreach (var vFolder in new[] { objProjectPath, objAppData }.Where(Directory.Exists))
        {
            Directory.Delete(vFolder, true);
        }
    }

    private static string StatusEdit(string aContent) => JsonSerializer.Serialize(new { path = "PROJECT-STATUS.md", content = "# Status\n\n" + aContent });

    private int Prepare(string? aChecklist, bool connectModel = true)
    {
        Directory.CreateDirectory(Path.Combine(objProjectPath, "docs"));
        Directory.CreateDirectory(objAppData);
        if (aChecklist is not null)
        {
            File.WriteAllText(Path.Combine(objProjectPath, "docs", "X-Checklist.md"), aChecklist);
        }

        var vProjectId = CreateProject("Handed", objProjectPath);
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        if (connectModel)
        {
            var vProviderId = vConnection.ExecuteScalar<long>("INSERT INTO Provider (Name, Connector, SignInMethod, BaseUrl, SecretName, State) VALUES ('P', 'Fake', 'Key', 'http://localhost', NULL, 'Connected'); SELECT last_insert_rowid();");
            vConnection.Execute("INSERT INTO Model (ProviderId, Tier, Identifier) VALUES (@vProviderId, 1, 'scripted-model')", new { vProviderId });
        }

        return (int)vConnection.ExecuteScalar<long>(
            "INSERT INTO ProcessRun (ProjectId, ProcessCode, State, CurrentStep, StartedUtc) VALUES (@vProjectId, 'refresh-status', 'running', 1, @Started); SELECT last_insert_rowid();",
            new { vProjectId, Started = DateTime.UtcNow.AddSeconds(-1).ToString("O") });
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
