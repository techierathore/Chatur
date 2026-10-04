using Chatur.Core;
using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;
using TrBlazeUI.Components.Toast;

namespace ChaturUI.Shell;

/// <summary>
/// The main window's toolbar region: the run target picker, build, run and stop
/// (REQ-UI-011 through REQ-UI-013, REQ-UI-015). Every control stays disabled until a project is
/// selected, and the target list only ever offers what <see cref="IBuildRunActions.TargetsAsync"/>
/// says this machine can build (REQ-UI-011).
/// </summary>
/// <remarks>Owning cluster: H (REQ-UI-011 through REQ-UI-016).</remarks>
public partial class AppToolbar : IDisposable
{
    // Lines BuildRunActions itself writes when a build or run ends, one way or another — watched here
    // only to know when Build/Run/Stop should be enabled again, never displayed twice.
    private static readonly string[] TerminalMarkers =
    [
        "Build succeeded", "Build FAILED", "Build stopped.", "Process exited", "Run stopped.", "Failed to run:"
    ];

    private IReadOnlyList<RunTarget> objTargets = [];
    private RunTarget? objSelectedTarget;
    private int? objLoadedForProjectId;
    private int? objWatchingProjectId;
    private int? objBranchLoadedForProjectId;
    private SourceControlStatus? objBranchStatus;
    private IReadOnlyList<string> objBranches = [];
    private CancellationTokenSource? objWatchCts;

    /// <summary>Whether a build or run is in progress for the selected project — Build, Run and Stop share this one flag since they share one output slot per project.</summary>
    private bool IsBusy { get; set; }

    [Inject]
    private IBuildRunActions BuildRun { get; set; } = default!;

    [Inject]
    private AppState AppStateService { get; set; } = default!;

    [Inject]
    private WorkbenchState Workbench { get; set; } = default!;

    [Inject]
    private ISourceControlActions SourceControl { get; set; } = default!;

    [Inject]
    private ToastService Toast { get; set; } = default!;

    private int? ProjectId => AppStateService.SelectedProject?.ProjectId;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        AppStateService.Changed += OnAppStateChanged;
        Workbench.BranchChanged += OnBranchChanged;
        await EnsureTargetsLoadedAsync();
        EnsureWatching();
        _ = LoadBranchAsync();
    }

    /// <summary>
    /// Reads the project's checked-out branch, what is to push and the branches it has, for the
    /// branch picker (BRD-16). Runs beside the first render rather than before it, since reading the
    /// status asks the server what is new. A project with no repository under it shows no branch
    /// rather than a wrong guess.
    /// </summary>
    private async Task LoadBranchAsync()
    {
        var vProjectId = ProjectId;
        objBranchLoadedForProjectId = vProjectId;
        if (vProjectId is not { } vId)
        {
            objBranchStatus = null;
            objBranches = [];
            return;
        }

        try
        {
            var vStatus = await SourceControl.StatusAsync(vId);
            var vBranches = await SourceControl.BranchesAsync(vId);
            if (vProjectId == ProjectId)
            {
                objBranchStatus = vStatus;
                objBranches = vBranches;
            }
        }
        catch (Exception aException) when (aException is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            objBranchStatus = null;
            objBranches = [];
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Switches the project to another branch from the toolbar, then tells the other regions to re-read it (REQ-FN-047).</summary>
    private async Task SwitchBranchAsync(string aBranchName)
    {
        if (ProjectId is not { } vProjectId || aBranchName == objBranchStatus?.Branch)
        {
            return;
        }

        try
        {
            await SourceControl.SwitchBranchAsync(vProjectId, aBranchName);
            Toast.Success($"Switched to {aBranchName}.", "Branch");
        }
        catch (InvalidOperationException aException)
        {
            Toast.Error(aException.Message, "Could not switch branch");
            return;
        }

        // The toolbar's own BranchChanged listener re-reads the branch, along with the files panel's.
        Workbench.NotifyBranchChanged();
    }

    /// <summary>Re-reads the branch when another region switched it.</summary>
    private void OnBranchChanged() => _ = LoadBranchAsync();

    private async Task EnsureTargetsLoadedAsync()
    {
        if (ProjectId is not { } vProjectId || objLoadedForProjectId == vProjectId)
        {
            return;
        }

        objTargets = await BuildRun.TargetsAsync(vProjectId);
        objSelectedTarget = objTargets.FirstOrDefault(t => t.IsAvailableOnThisMachine) ?? objTargets.FirstOrDefault();
        objLoadedForProjectId = vProjectId;
    }

    private void SelectTarget(RunTarget aTarget)
    {
        if (!aTarget.IsAvailableOnThisMachine)
        {
            return;
        }

        objSelectedTarget = aTarget;
    }

    /// <summary>Builds the project on the chosen target (REQ-UI-012).</summary>
    private async Task BuildAsync()
    {
        if (ProjectId is not { } vProjectId || objSelectedTarget is null || IsBusy)
        {
            return;
        }

        IsBusy = true;

        // Call first, notify second: BuildRunActions.BuildAsync clears its output buffer as the very
        // first thing it does, before its own first await — calling it (without awaiting yet) already
        // runs that synchronous prefix, so the buffer is provably clear by the time
        // NotifyBuildOrRunStarted tells OutputStrip to open a fresh subscription. Notifying first
        // would let that fresh subscription's initial read land on the still-stale buffer from the
        // project's last build or run and replay it as if it were new (REQ-UI-014).
        var vBuildTask = BuildRun.BuildAsync(vProjectId, objSelectedTarget.RunTargetId);
        Workbench.NotifyBuildOrRunStarted();
        await vBuildTask;
    }

    /// <summary>Runs the chosen target (REQ-UI-013).</summary>
    private async Task RunAsync()
    {
        if (ProjectId is not { } vProjectId || objSelectedTarget is null || IsBusy)
        {
            return;
        }

        IsBusy = true;

        // Same ordering as BuildAsync above: BuildRunActions.RunAsync clears the buffer synchronously
        // before it returns (even though the run itself keeps going in the background), so calling it
        // first and notifying second means the fresh subscription OutputStrip opens never lands on a
        // stale buffer.
        var vRunTask = BuildRun.RunAsync(vProjectId, objSelectedTarget.RunTargetId);
        Workbench.NotifyBuildOrRunStarted();
        await vRunTask;
    }

    /// <summary>Stops the run and every program it started (REQ-UI-015).</summary>
    private async Task StopAsync()
    {
        if (ProjectId is not { } vProjectId)
        {
            return;
        }

        await BuildRun.StopAsync(vProjectId);
    }

    /// <summary>
    /// Watches the selected project's own output for the line BuildRunActions writes when a build or
    /// run ends, so Build/Run/Stop re-enable themselves the moment the process actually stops, rather
    /// than staying stuck on whichever button was pressed last.
    /// </summary>
    private void EnsureWatching()
    {
        if (ProjectId == objWatchingProjectId)
        {
            return;
        }

        objWatchCts?.Cancel();
        objWatchingProjectId = ProjectId;
        IsBusy = false;

        if (ProjectId is { } vProjectId)
        {
            objWatchCts = new CancellationTokenSource();
            _ = WatchOutputAsync(vProjectId, objWatchCts.Token);
        }
    }

    private async Task WatchOutputAsync(int aProjectId, CancellationToken aCt)
    {
        try
        {
            await foreach (var vLine in BuildRun.StreamOutputAsync(aProjectId, aCt))
            {
                if (Array.Exists(TerminalMarkers, m => vLine.Text.Contains(m, StringComparison.Ordinal)))
                {
                    IsBusy = false;
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the project changes or the component is disposed.
        }
    }

    private async void OnAppStateChanged()
    {
        await InvokeAsync(async () =>
        {
            await EnsureTargetsLoadedAsync();
            EnsureWatching();
            if (ProjectId != objBranchLoadedForProjectId)
            {
                _ = LoadBranchAsync();
            }

            StateHasChanged();
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        AppStateService.Changed -= OnAppStateChanged;
        Workbench.BranchChanged -= OnBranchChanged;
        objWatchCts?.Cancel();
    }
}
