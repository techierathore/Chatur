using System.Text.Json;
using Chatur.Core.Actions;
using Chatur.Core.Measurements;
using Chatur.Core.Platform;
using Chatur.Core.ProcessRuns;
using Chatur.Core.Processes;
using Chatur.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chatur.Tests.ProcessRuns;

/// <summary>
/// Tests for <see cref="ProcessRunActions"/> against a real, migrated, temporary database and a real
/// temp project folder, on <c>verify-phase</c> (shape SsCsCPS: seven rows, an owner stop at row 6)
/// (REQ-FN-052..055).
/// </summary>
public sealed class ProcessRunActionsTests : MigratedDatabaseFixture
{
    private const string Process = "verify-phase";

    private readonly string objProjectPath = Path.Combine(Path.GetTempPath(), $"chatur-run-{Guid.NewGuid():N}");

    private string RunsFile => Path.Combine(objProjectPath, "docs", "metrics", "runs.jsonl");

    /// <summary>
    /// When a run is carried on, then it pauses at the owner stop with every step before it done and
    /// carrying its result, the stop marked as current and the rest still to come (REQ-FN-052,
    /// REQ-FN-053).
    /// </summary>
    [Fact]
    public async Task RunPausesAtTheOwnerStopWithEarlierStepsDone()
    {
        var vActions = CreateActions(new RecordingExecutor());
        var vRunId = StartRun(CreateProject("P", objProjectPath));

        var vAfter = await vActions.AdvanceAsync(vRunId);

        Assert.Equal(ProcessRunState.Waiting, vAfter!.State);
        Assert.Equal(6, vAfter.CurrentPosition);
        Assert.All(vAfter.Steps.Take(5), aStep => Assert.Equal(ProcessStepState.Done, aStep.State));
        Assert.Equal("Done", vAfter.Steps[0].Result);
        Assert.Equal("Read the phase checklist and collect every requirement", vAfter.Steps[0].Wording);
        Assert.Equal(ProcessStepState.Waiting, vAfter.Steps[5].State);
        Assert.Equal(ProcessStepState.ToCome, vAfter.Steps[6].State);
    }

    /// <summary>
    /// When a run is opened at its first step, then that step is the one marked as running and the
    /// others are still to come (REQ-FN-052).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-052 an opened run marks the current step and finished steps show their result")]
    public async Task OpenedRunMarksTheCurrentStep()
    {
        var vActions = CreateActions(new RecordingExecutor());
        var vRunId = StartRun(CreateProject("P", objProjectPath));

        var vRun = await vActions.GetAsync(vRunId);

        Assert.Equal(ProcessRunState.Running, vRun!.State);
        Assert.Equal(ProcessStepState.Running, vRun.Steps[0].State);
        Assert.All(vRun.Steps.Skip(1), aStep => Assert.Equal(ProcessStepState.ToCome, aStep.State));
    }

    /// <summary>
    /// When a run is waiting at an owner stop and is advanced again or answered "later", then no step
    /// runs and nothing changes until the owner answers (REQ-FN-053).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-053 a run at an owner stop does nothing further until answered")]
    public async Task WaitingRunDoesNothingFurtherUntilAnswered()
    {
        var vExecutor = new RecordingExecutor();
        var vActions = CreateActions(vExecutor);
        var vRunId = StartRun(CreateProject("P", objProjectPath));
        await vActions.AdvanceAsync(vRunId);
        var vCalls = vExecutor.Calls.Count;

        var vAgain = await vActions.AdvanceAsync(vRunId);
        var vLater = await vActions.AnswerAsync(vRunId, ProcessAnswer.Later);

        Assert.Equal(vCalls, vExecutor.Calls.Count);
        Assert.Equal(ProcessRunState.Waiting, vAgain!.State);
        Assert.Equal(ProcessRunState.Waiting, vLater!.State);
        Assert.Equal(ProcessStepState.ToCome, vLater.Steps[6].State);
    }

    /// <summary>
    /// When the owner answers after a restart (a new action object over the same database), then the
    /// run continues from the step after the stop and the earlier steps are not run again
    /// (REQ-FN-055).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-055 answering a paused run after a restart continues from that step")]
    public async Task AnswerAfterRestartContinuesFromTheStepNotTheBeginning()
    {
        var vRunId = StartRun(CreateProject("P", objProjectPath));
        await CreateActions(new RecordingExecutor()).AdvanceAsync(vRunId);

        var vExecutorAfterRestart = new RecordingExecutor();
        var vRestarted = CreateActions(vExecutorAfterRestart);
        var vOpened = await vRestarted.GetAsync(vRunId);
        var vAnswered = await vRestarted.AnswerAsync(vRunId, ProcessAnswer.Yes);

        Assert.Equal(6, vOpened!.CurrentPosition);
        Assert.Equal(["s7"], vExecutorAfterRestart.Calls);
        Assert.Equal(ProcessRunState.Finished, vAnswered!.State);
        Assert.Equal("Yes", vAnswered.Steps[5].Answer);
        Assert.All(vAnswered.Steps, aStep => Assert.Equal(ProcessStepState.Done, aStep.State));
    }

    /// <summary>
    /// When the app is closed mid-run and opened again, then advancing carries on from the saved step,
    /// not from the first (REQ-FN-055).
    /// </summary>
    [Fact]
    public async Task AdvanceAfterRestartResumesTheSavedStep()
    {
        var vRunId = StartRun(CreateProject("P", objProjectPath), currentStep: 3);

        var vExecutor = new RecordingExecutor();
        var vAfter = await CreateActions(vExecutor).AdvanceAsync(vRunId);

        Assert.Equal(["s3", "s4", "s5"], vExecutor.Calls);
        Assert.Equal(ProcessRunState.Waiting, vAfter!.State);
    }

    /// <summary>
    /// When a check fails, then the run stops on that step with the reason as its result and later
    /// steps never run (REQ-FN-052).
    /// </summary>
    [Fact]
    public async Task FailedCheckStopsTheRunOnThatStep()
    {
        var vExecutor = new RecordingExecutor { FailOn = "s3", Reason = "The build failed with 2 errors." };
        var vActions = CreateActions(vExecutor);
        var vRunId = StartRun(CreateProject("P", objProjectPath));

        var vAfter = await vActions.AdvanceAsync(vRunId);

        Assert.Equal(ProcessRunState.Stopped, vAfter!.State);
        Assert.Equal(3, vAfter.CurrentPosition);
        Assert.Equal(ProcessStepState.Failed, vAfter.Steps[2].State);
        Assert.Equal("The build failed with 2 errors.", vAfter.Steps[2].Result);
        Assert.Equal(["s1", "s2", "s3"], vExecutor.Calls);
    }

    /// <summary>
    /// When a run ends, then one record for it is in the project's runs file with the tool named as
    /// <c>chatur</c> and the process as the command, and none was written before it ended (REQ-FN-054).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-054 an ended run writes one record to the runs file signed chatur")]
    public async Task EndedRunWritesOneRecordSignedChatur()
    {
        var vActions = CreateActions(new RecordingExecutor());
        var vRunId = StartRun(CreateProject("P", objProjectPath));
        await vActions.AdvanceAsync(vRunId);
        Assert.False(File.Exists(RunsFile) && new FileInfo(RunsFile).Length > 0);

        await vActions.AnswerAsync(vRunId, ProcessAnswer.No);

        var vRecord = Assert.Single(File.ReadAllLines(RunsFile), aLine => aLine.Length > 0);
        using var vDoc = JsonDocument.Parse(vRecord);
        Assert.Equal("chatur", vDoc.RootElement.GetProperty("tool").GetString());
        Assert.Equal("run", vDoc.RootElement.GetProperty("kind").GetString());
        Assert.Equal(Process, vDoc.RootElement.GetProperty("cmd").GetString());
    }

    /// <summary>
    /// When a run is stopped on a failed check or by the owner, then its record is written too, once
    /// (REQ-FN-054).
    /// </summary>
    [Fact]
    public async Task StoppedRunWritesItsRecordOnce()
    {
        var vActions = CreateActions(new RecordingExecutor());
        var vRunId = StartRun(CreateProject("P", objProjectPath));
        await vActions.AdvanceAsync(vRunId);

        var vStopped = await vActions.StopAsync(vRunId);
        await vActions.StopAsync(vRunId);

        Assert.Equal(ProcessRunState.Stopped, vStopped!.State);
        Assert.Single(File.ReadAllLines(RunsFile), aLine => aLine.Length > 0);
    }

    /// <summary>When the run is not at an owner stop, then answering it is refused rather than ignored.</summary>
    [Fact]
    public async Task AnsweringARunThatIsNotWaitingIsRefused()
    {
        var vActions = CreateActions(new RecordingExecutor());
        var vRunId = StartRun(CreateProject("P", objProjectPath));

        await Assert.ThrowsAsync<InvalidOperationException>(() => vActions.AnswerAsync(vRunId, ProcessAnswer.Yes));
    }

    /// <summary>When a run does not exist, then every read and action answers null rather than throwing.</summary>
    [Fact]
    public async Task UnknownRunIsNull()
    {
        var vActions = CreateActions(new RecordingExecutor());

        Assert.Null(await vActions.GetAsync(999));
        Assert.Null(await vActions.AdvanceAsync(999));
        Assert.Null(await vActions.StopAsync(999));
    }

    /// <summary>When a project has runs, then they are listed newest first with the step each is on.</summary>
    [Fact]
    public async Task ListReturnsRunsNewestFirst()
    {
        var vProjectId = CreateProject("P", objProjectPath);
        var vActions = CreateActions(new RecordingExecutor());
        var vOne = StartRun(vProjectId);
        var vTwo = StartRun(vProjectId);
        await vActions.AdvanceAsync(vTwo);

        var vList = await vActions.ListAsync(vProjectId);

        Assert.Equal([vTwo, vOne], vList.Select(aRun => aRun.RunId));
        Assert.Equal(6, vList[0].CurrentPosition);
        Assert.Equal(1, vList[1].CurrentPosition);
    }

    private ProcessRunActions CreateActions(RecordingExecutor aExecutor) =>
        ProcessRunWiring.Actions(CreateFactory(), aExecutor);

    private int StartRun(int aProjectId, int currentStep = 1)
    {
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText =
            """
            INSERT INTO ProcessRun (ProjectId, ProcessCode, State, CurrentStep, StartedUtc)
            VALUES (@ProjectId, @Process, 'running', @CurrentStep, @Started);
            SELECT last_insert_rowid();
            """;
        vCommand.Parameters.AddWithValue("@ProjectId", aProjectId);
        vCommand.Parameters.AddWithValue("@Process", Process);
        vCommand.Parameters.AddWithValue("@CurrentStep", currentStep);
        vCommand.Parameters.AddWithValue("@Started", DateTime.UtcNow.ToString("O"));
        return (int)(long)vCommand.ExecuteScalar()!;
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

    private sealed class RecordingExecutor : IProcessStepExecutor
    {
        public List<string> Calls { get; } = [];

        public string? FailOn { get; init; }

        public string Reason { get; init; } = string.Empty;

        public Task<ProcessStepOutcome> ExecuteAsync(int aRunId, int aProjectId, string aProcessCode, ProcessRunStepView aStep, CancellationToken aCt = default)
        {
            Calls.Add(aStep.StepCode);
            return Task.FromResult(aStep.StepCode == FailOn
                ? new ProcessStepOutcome(false, Reason)
                : new ProcessStepOutcome(true, "Done"));
        }
    }
}
