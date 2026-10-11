using Chatur.Core.Actions;
using Chatur.Core.Platform;
using Chatur.Core.ProcessRuns;
using Chatur.Core.RunQueue;
using Chatur.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chatur.Tests.RunQueue;

/// <summary>
/// Tests for <see cref="RunQueueActions"/> against a real, migrated, temporary database and a fake
/// end-to-end run engine (REQ-FN-067 to REQ-FN-070).
/// </summary>
public sealed class RunQueueActionsTests : MigratedDatabaseFixture
{
    private const string Brief = "Build the coverage panel and verify every requirement of it.";

    private readonly string objFolder = Path.Combine(Path.GetTempPath(), $"chatur-queue-{Guid.NewGuid():N}");
    private readonly FakeAutoRuns objRuns = new();

    /// <summary>
    /// When the owner queues a run, then it is listed with its project and its position, and a second
    /// run is listed behind it at position 2 (REQ-FN-067).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-067 a queued run is listed with its project and its position")]
    public async Task QueuedRunIsListedWithProjectAndPosition()
    {
        var vActions = Create();
        var vFirst = CreateProject("TfLens", Path.Combine(objFolder, "TfLens"));
        var vSecond = CreateProject("MyDiary", Path.Combine(objFolder, "MyDiary"));

        await vActions.QueueAsync(vFirst, Brief);
        await vActions.QueueAsync(vSecond, Brief);
        var vView = await vActions.ListAsync();

        Assert.Equal(["TfLens", "MyDiary"], vView.Items.Select(aItem => aItem.ProjectName));
        Assert.Equal([1, 2], vView.Items.Select(aItem => aItem.Position));
    }

    /// <summary>When a project is already queued, then queueing it again warns and still queues it (REQ-FN-067).</summary>
    [Fact(DisplayName = "REQ-FN-067 a second brief on a queued project warns and is not refused")]
    public async Task SecondBriefOnQueuedProjectWarns()
    {
        var vActions = Create();
        var vProject = CreateProject("TfLens", Path.Combine(objFolder, "TfLens"));
        await vActions.QueueAsync(vProject, Brief);

        var vAdded = await vActions.QueueAsync(vProject, Brief + " Again.");

        Assert.True(vAdded.ProjectAlreadyQueued);
    }

    /// <summary>When the brief is too short, then queueing is refused (REQ-FN-067).</summary>
    [Fact(DisplayName = "REQ-FN-067 a brief shorter than the minimum is refused")]
    public async Task ShortBriefIsRefused()
    {
        var vActions = Create();
        var vProject = CreateProject("TfLens", Path.Combine(objFolder, "TfLens"));

        await Assert.ThrowsAsync<ArgumentException>(() => vActions.QueueAsync(vProject, "too short"));
    }

    /// <summary>When a run is going, then the next one does not start until it has ended (REQ-UI-057 rule the queue keeps).</summary>
    [Fact(DisplayName = "REQ-FN-067 only one queued run goes at a time")]
    public async Task OnlyOneRunGoesAtATime()
    {
        var vActions = Create();
        await vActions.QueueAsync(CreateProject("A", Path.Combine(objFolder, "A")), Brief);
        await vActions.QueueAsync(CreateProject("B", Path.Combine(objFolder, "B")), Brief);

        var vView = await vActions.ListAsync();
        Assert.Equal([RunQueueState.Running, RunQueueState.Waiting], vView.Items.Select(aItem => aItem.State));

        objRuns.Finish(1);
        var vAfter = await vActions.ListAsync();
        Assert.Equal([RunQueueState.Running, RunQueueState.Finished], vAfter.Items.Select(aItem => aItem.State));
        Assert.Null(vAfter.Items[1].Position);
    }

    /// <summary>When the owner pauses a run and resumes it, then the same run carries on, not a new one (REQ-FN-068).</summary>
    [Fact(DisplayName = "REQ-FN-068 a paused run resumes the same run it was paused on")]
    public async Task PausedRunResumesTheSameRun()
    {
        var vActions = Create();
        await vActions.QueueAsync(CreateProject("A", Path.Combine(objFolder, "A")), Brief);
        var vId = (await vActions.ListAsync()).Items[0].QueueItemId;

        var vPaused = await vActions.PauseAsync(vId);
        var vResumed = await vActions.ResumeAsync(vId);

        Assert.Equal(RunQueueState.Paused, vPaused!.State);
        Assert.Equal(RunQueueState.Running, vResumed!.State);
        Assert.Equal(["start 1", "pause 1", "resume 1"], objRuns.Calls);
    }

    /// <summary>When another run is going, then a resume waits its turn instead of running two at once (REQ-FN-068).</summary>
    [Fact(DisplayName = "REQ-FN-068 a resume while another run goes waits for its turn")]
    public async Task ResumeWhileAnotherRunGoesWaits()
    {
        var vActions = Create();
        await vActions.QueueAsync(CreateProject("A", Path.Combine(objFolder, "A")), Brief);
        await vActions.QueueAsync(CreateProject("B", Path.Combine(objFolder, "B")), Brief);
        var vFirst = (await vActions.ListAsync()).Items[0].QueueItemId;
        await vActions.PauseAsync(vFirst);
        var vView = await vActions.ListAsync();
        Assert.Equal(RunQueueState.Running, vView.Items.Single(aItem => aItem.ProjectName == "B").State);

        var vResumed = await vActions.ResumeAsync(vFirst);

        Assert.Equal(RunQueueState.Waiting, vResumed!.State);
        Assert.DoesNotContain("resume 1", objRuns.Calls);
    }

    /// <summary>When the owner removes a waiting run, then it leaves the queue and is never started (REQ-FN-069).</summary>
    [Fact(DisplayName = "REQ-FN-069 a removed waiting run leaves the queue and is never started")]
    public async Task RemovedWaitingRunIsNeverStarted()
    {
        var vActions = Create();
        await vActions.QueueAsync(CreateProject("A", Path.Combine(objFolder, "A")), Brief);
        await vActions.QueueAsync(CreateProject("B", Path.Combine(objFolder, "B")), Brief);
        var vWaiting = (await vActions.ListAsync()).Items.Single(aItem => aItem.ProjectName == "B");

        var vRemoved = await vActions.RemoveAsync(vWaiting.QueueItemId);
        objRuns.Finish(1);
        var vView = await vActions.ListAsync();

        Assert.True(vRemoved);
        Assert.DoesNotContain(vView.Items, aItem => aItem.ProjectName == "B");
        Assert.Equal(["start 1"], objRuns.Calls);
    }

    /// <summary>When a run is going, then it cannot be removed until it is paused or stopped (REQ-FN-069).</summary>
    [Fact(DisplayName = "REQ-FN-069 a running run cannot be removed")]
    public async Task RunningRunCannotBeRemoved()
    {
        var vActions = Create();
        await vActions.QueueAsync(CreateProject("A", Path.Combine(objFolder, "A")), Brief);
        var vRunning = (await vActions.ListAsync()).Items[0];

        Assert.False(await vActions.RemoveAsync(vRunning.QueueItemId));
    }

    /// <summary>When a resumed run reads the checklist, then the blocked requirement comes before partial and untouched ones (REQ-FN-070).</summary>
    [Fact(DisplayName = "REQ-FN-070 a resume takes the blocked requirement before partial and untouched")]
    public async Task ResumeTakesBlockedFirst()
    {
        Directory.CreateDirectory(Path.Combine(objFolder, "A", "docs"));
        File.WriteAllText(Path.Combine(objFolder, "A", "docs", "X-Checklist.md"), Table(
            ("REQ-FN-001", "Not Started", 0), ("REQ-FN-002", "PARTIAL", 50), ("REQ-FN-003", "Blocked", 25), ("REQ-FN-004", "Verified", 100)));
        var vActions = Create();
        await vActions.QueueAsync(CreateProject("A", Path.Combine(objFolder, "A")), Brief);
        var vId = (await vActions.ListAsync()).Items[0].QueueItemId;
        await vActions.PauseAsync(vId);

        var vResumed = await vActions.ResumeAsync(vId);

        Assert.Equal("REQ-FN-003", vResumed!.ResumeFocus);
    }

    /// <summary>The order is blocked, then partial or in progress, then untouched, then built and unverified; verified is not open (REQ-FN-070).</summary>
    [Fact(DisplayName = "REQ-FN-070 open requirements are ordered furthest from verified first")]
    public void OpenRequirementsAreOrdered()
    {
        string[] vStatuses = ["Implemented", "Not Started", "PARTIAL", "Verified", "Blocked", "FAIL"];
        var vItems = vStatuses.Select((aStatus, aIndex) => Item($"REQ-FN-00{aIndex}", aStatus)).ToList();

        var vOrder = RequirementOrder.OpenFurthestFirst(vItems).Select(aItem => aItem.FileStatus);

        Assert.Equal(["Blocked", "FAIL", "PARTIAL", "Not Started", "Implemented"], vOrder);
    }

    /// <summary>Deletes the temporary project folder as well as the database.</summary>
    public override void Dispose()
    {
        base.Dispose();
        if (Directory.Exists(objFolder))
        {
            Directory.Delete(objFolder, true);
        }
    }

    private RunQueueActions Create()
    {
        Directory.CreateDirectory(objFolder);
        var vFactory = CreateFactory();
        return new RunQueueActions(vFactory, objRuns, new ProjectFolders(vFactory), new SystemClock(), NullLogger<RunQueueActions>.Instance);
    }

    private static RequirementItem Item(string aId, string aStatus) =>
        new(aId, "REQ-FN", "T", aStatus, aStatus, 0, string.Empty, "flow-master", "Flow master", 3, "docs/X-Checklist.md", string.Empty, string.Empty, null, null);

    private static string Table(params (string Id, string Status, int Percent)[] aRows) =>
        "## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|-------------|--------|---|---------|---------|\n"
        + string.Concat(aRows.Select(aRow => $"| {aRow.Id} | Title {aRow.Id} | {aRow.Status} | {aRow.Percent}% | — | — |\n"));
}
