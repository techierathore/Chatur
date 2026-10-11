using Chatur.Core.Actions;
using TrBlazeUI.Components.Badge;

namespace ChaturUI.Pages;

/// <summary>
/// Run queue (mockups/run-queue.html). Lists the end-to-end runs waiting their turn, one at a time
/// (REQ-UI-057), and holds the controls to queue (REQ-FN-067), pause and resume (REQ-FN-068) and
/// remove (REQ-FN-069) a run. Every fact is read from <see cref="IRunQueueActions"/>; the page asks
/// again every few seconds so the next run's start shows without a click.
/// </summary>
public partial class RunQueue : IDisposable
{
    private static readonly TimeSpan cRefreshEvery = TimeSpan.FromSeconds(4);

    // The mockup's hook on the table's own header row (TrBlazeUI HeaderRowAttributes).
    private static readonly IReadOnlyDictionary<string, object> QueueHeadAttributes =
        new Dictionary<string, object> { ["data-testid"] = "queue-head" };

    private IReadOnlyList<RunQueueItem> objItems = Array.Empty<RunQueueItem>();
    private RunQueueView? objView;
    private RunQueueItem? objRemoving;
    private string? objError;
    private bool objIsLoading;
    private bool objIsBusy;
    private bool objIsQueueDialogOpen;
    private bool objIsReading;
    private Timer? objTimer;

    // The mockup counts a paused run among the waiting: it is not going, and it takes its turn again on resume.
    private string CountText
    {
        get
        {
            if (objView is null)
            {
                return "···";
            }

            var vRunning = objItems.Count(aItem => aItem.State == RunQueueState.Running);
            var vWaiting = objItems.Count(aItem => aItem.State is RunQueueState.Waiting or RunQueueState.Paused);
            return vRunning == 0 && vWaiting == 0 ? "nothing running" : $"{vRunning} running · {vWaiting} waiting";
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        objIsLoading = true;
        await LoadAsync();
        objIsLoading = false;
        objTimer = new Timer(_ => _ = InvokeAsync(RefreshAsync), null, cRefreshEvery, cRefreshEvery);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        objTimer?.Dispose();
        objTimer = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>Reads the queue; the action starts the next waiting run when none is going.</summary>
    private async Task LoadAsync()
    {
        if (objIsReading)
        {
            return;
        }

        objIsReading = true;
        try
        {
            objView = await RunQueueActions.ListAsync();
            objItems = objView.Items;
            objError = null;
        }
        catch (Exception aEx)
        {
            objError = aEx.Message;
        }
        finally
        {
            objIsReading = false;
        }
    }

    private async Task RefreshAsync()
    {
        await LoadAsync();
        StateHasChanged();
    }

    private async Task ReloadAsync()
    {
        await LoadAsync();
    }

    private void OpenAutoRun() => WorkbenchTabs.OpenAutoRun();

    private void OpenQueueDialog() => objIsQueueDialogOpen = true;

    private void OnQueueDialogOpenChanged(bool aOpen) => objIsQueueDialogOpen = aOpen;

    private async Task OnQueuedAsync(RunQueueAdded aAdded)
    {
        objIsQueueDialogOpen = false;
        if (aAdded.ProjectAlreadyQueued)
        {
            ToastService.Success($"{aAdded.Item.ProjectName} already had a run in the queue. This one waits behind it.", "Queued, with a warning");
        }
        else
        {
            ToastService.Success($"{aAdded.Item.ProjectName} is queued.", "Queued");
        }

        await LoadAsync();
    }

    private async Task PauseAsync(RunQueueItem aItem)
    {
        await ActAsync(() => RunQueueActions.PauseAsync(aItem.QueueItemId), "Could not pause the run");
    }

    private async Task ResumeAsync(RunQueueItem aItem)
    {
        await ActAsync(() => RunQueueActions.ResumeAsync(aItem.QueueItemId), "Could not resume the run");
    }

    private void AskRemove(RunQueueItem aItem) => objRemoving = aItem;

    private void CancelRemove() => objRemoving = null;

    private void OnRemoveDialogOpenChanged(bool aOpen)
    {
        if (!aOpen)
        {
            objRemoving = null;
        }
    }

    private async Task ConfirmRemoveAsync()
    {
        var vItem = objRemoving;
        objRemoving = null;
        if (vItem is null)
        {
            return;
        }

        await ActAsync(async () => await RunQueueActions.RemoveAsync(vItem.QueueItemId), "Could not remove the run");
    }

    private async Task ActAsync(Func<Task> aAct, string aFailure)
    {
        objIsBusy = true;
        try
        {
            await aAct();
        }
        catch (Exception aEx)
        {
            ToastService.Error(aEx.Message, aFailure);
        }
        finally
        {
            objIsBusy = false;
        }

        await LoadAsync();
    }

    /// <summary>The 1-based row number the mockup's hooks use (queue-row-1 ...).</summary>
    /// <param name="aItem">The row.</param>
    private int RowNumber(RunQueueItem aItem)
    {
        for (var vIndex = 0; vIndex < objItems.Count; vIndex++)
        {
            if (objItems[vIndex].QueueItemId == aItem.QueueItemId)
            {
                return vIndex + 1;
            }
        }

        return 0;
    }

    private static string RowClass(RunQueueItem aItem) =>
        aItem.State == RunQueueState.Running ? "border-t bg-accent" : "border-t";

    private static string StateWord(RunQueueState aState) => aState switch
    {
        RunQueueState.Waiting => "waiting",
        RunQueueState.Running => "running",
        RunQueueState.Paused => "paused",
        RunQueueState.Finished => "finished",
        _ => "lost"
    };

    private static BadgeVariant StateVariant(RunQueueState aState) => aState switch
    {
        RunQueueState.Paused => BadgeVariant.Warning,
        RunQueueState.Finished => BadgeVariant.Success,
        RunQueueState.Lost => BadgeVariant.Danger,
        _ => BadgeVariant.Secondary
    };

    private static string StateClass(RunQueueState aState) =>
        aState == RunQueueState.Running ? "gap-1 rounded-full border-transparent px-1.5 text-primary" : "gap-1 rounded-full border-transparent px-1.5";

    private static string ElapsedText(TimeSpan? aElapsed)
    {
        if (aElapsed is not { } vElapsed)
        {
            return "—";
        }

        return vElapsed.TotalHours >= 1
            ? $"{(int)vElapsed.TotalHours} h {vElapsed.Minutes} m"
            : $"{Math.Max(0, (int)vElapsed.TotalMinutes)} m";
    }

    private static string PauseTitle(RunQueueItem aItem) => aItem.CanPause
        ? "Pause"
        : aItem.State switch
        {
            RunQueueState.Paused => "Pause — already paused",
            RunQueueState.Finished => "Pause — this run has finished",
            _ => "Pause — this run has not started"
        };

    private static string ResumeTitle(RunQueueItem aItem) => aItem.CanResume
        ? "Resume"
        : aItem.State switch
        {
            RunQueueState.Finished => "Resume — this run has finished",
            RunQueueState.Waiting => "Resume — this run has not started",
            _ => "Resume — this run is not paused"
        };

    private static string RemoveTitle(RunQueueItem aItem) => aItem.CanRemove
        ? "Remove"
        : aItem.State == RunQueueState.Finished
            ? "Remove — a finished run is kept for its report"
            : "Remove — a running run must be paused or stopped first";
}
