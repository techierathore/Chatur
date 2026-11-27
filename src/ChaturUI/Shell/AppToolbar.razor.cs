using Chatur.Core;
using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;

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
    // Since Run shows the build (REQ-UI-107), dotnet's own "Build succeeded." and "Build FAILED." lines appear in the
    // output too, so only the lines Chatur writes itself, with their time or code, end a build or run.
    private static readonly System.Text.RegularExpressions.Regex TerminalLine = new(
        @"^(Build succeeded in \d|Build FAILED in \d|All \d+ projects ended\.|Build stopped\.|Process exited with code |Run stopped\.|Failed to run:|A solution builds but does not run)",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    private string? objLoadedForProjectKey;
    private int? objWatchingProjectId;
    private CancellationTokenSource? objWatchCts;

    /// <summary>Whether the toolbar's Find in files dialog is open (REQ-UI-073).</summary>
    private bool objFindOpen;

    /// <summary>Whether a build or run is in progress for the selected project — Build, Run and Stop share this one flag since they share one output slot per project.</summary>
    private bool IsBusy => Workbench.IsBuildRunBusy;

    /// <summary>Whether the Multiple startup projects dialog is open (REQ-FN-324).</summary>
    private bool objStartupOpen;

    private IReadOnlyList<RunTarget> objTargets => Workbench.RunTargets;
    private RunTarget? objSelectedTarget => Workbench.SelectedRunTarget;

    [Inject]
    private IBuildRunActions BuildRun { get; set; } = default!;

    [Inject]
    private AppState AppStateService { get; set; } = default!;

    [Inject]
    private WorkbenchState Workbench { get; set; } = default!;

    /// <summary>What the picker shows: the project chosen to run, or how many start together (REQ-FN-324).</summary>
    private string TargetLabel => Workbench.HasMultipleStartup
        ? $"{Workbench.StartupTargets.Count} startup projects"
        : objSelectedTarget?.Name ?? (ProjectId is null ? "No project open" : "No target found");

    /// <summary>Whether a target is ticked in the picker: the single project chosen, or one of the startup projects.</summary>
    private bool IsChosen(RunTarget aTarget) =>
        Workbench.HasMultipleStartup ? Workbench.StartupTargets.Any(t => t.RunTargetId == aTarget.RunTargetId) : aTarget == objSelectedTarget;

    private int? ProjectId => AppStateService.SelectedProject?.ProjectId;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        AppStateService.Changed += OnAppStateChanged;
        Workbench.Changed += OnWorkbenchChanged;
        Workbench.BranchChanged += OnBranchChanged;
        Workbench.StartupDialogRequested += OnStartupDialogRequested;
        await EnsureTargetsLoadedAsync();
        EnsureWatching();
    }

    private async Task EnsureTargetsLoadedAsync()
    {
        // Keyed by the project's id and path: a project removed and another opened can be given the same id, and the
        // toolbar then kept the removed one's targets, whose Run could not start (21 Nov 2026).
        var vKey = AppStateService.SelectedProject is { } vProject ? $"{vProject.ProjectId}|{vProject.Path}" : null;
        if (ProjectId is not { } vProjectId || objLoadedForProjectKey == vKey)
        {
            return;
        }

        Workbench.SetRunTargets(await BuildRun.TargetsAsync(vProjectId));
        Workbench.SetStartupTargets(await BuildRun.StartupAsync(vProjectId));
        objLoadedForProjectKey = vKey;
        if (Workbench.RunTargets.Count == 0)
        {
            StartLookingForTargets(vProjectId, vKey!);
        }
    }

    /// <summary>
    /// BRD-248, REQ-FN-309 (22 Nov 2026): "No target found" never stays once a project exists. While the open project
    /// has no target, its targets are read again every few seconds, so a project made by New project, by an agent or
    /// outside Chatur is offered without anything more; the looking stops at the first target or when the project changes.
    /// </summary>
    private void StartLookingForTargets(int aProjectId, string aKey)
    {
        if (objLookingForKey == aKey)
        {
            return;
        }

        objLookCts?.Cancel();
        objLookCts = new CancellationTokenSource();
        objLookingForKey = aKey;
        _ = LookForTargetsAsync(aProjectId, aKey, objLookCts.Token);
    }

    private async Task LookForTargetsAsync(int aProjectId, string aKey, CancellationToken aCt)
    {
        try
        {
            using var vTimer = new PeriodicTimer(TimeSpan.FromSeconds(4));
            while (await vTimer.WaitForNextTickAsync(aCt))
            {
                if (objLoadedForProjectKey != aKey || Workbench.RunTargets.Count > 0)
                {
                    break;
                }

                IReadOnlyList<RunTarget> vTargets;
                try
                {
                    vTargets = await BuildRun.TargetsAsync(aProjectId, aCt);
                }
                catch (Exception aException) when (aException is IOException or InvalidOperationException or UnauthorizedAccessException)
                {
                    continue;
                }

                if (vTargets.Count > 0 && objLoadedForProjectKey == aKey)
                {
                    await InvokeAsync(() =>
                    {
                        Workbench.SetRunTargets(vTargets);
                        StateHasChanged();
                    });
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The project changed or the toolbar went away.
        }
        finally
        {
            if (objLookingForKey == aKey)
            {
                objLookingForKey = null;
            }
        }
    }

    private string? objLookingForKey;
    private CancellationTokenSource? objLookCts;

    /// <summary>Chooses the target to build and run, and remembers it for this project and account (BRD-228).</summary>
    private async Task SelectTarget(RunTarget aTarget)
    {
        if (ProjectId is not { } vProjectId || !aTarget.IsAvailableOnThisMachine)
        {
            return;
        }

        Workbench.SelectRunTarget(aTarget);
        await BuildRun.SetRunTargetAsync(vProjectId, aTarget.RunTargetId);
    }

    /// <summary>The target picker's last item: Multiple startup projects opens the dialog that chooses which projects Run starts together (REQ-FN-324).</summary>
    private void OpenStartupProjects() => Workbench.RequestStartupDialog();

    /// <summary>Builds the project on the chosen target (REQ-UI-012).</summary>
    private async Task BuildAsync()
    {
        if (ProjectId is not { } vProjectId || objSelectedTarget is null || IsBusy)
        {
            return;
        }

        Workbench.SetBuildRunBusy(true);

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

        Workbench.SetBuildRunBusy(true);

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
        Workbench.SetBuildRunBusy(false);

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
                if (TerminalLine.IsMatch(vLine.Text))
                {
                    Workbench.SetBuildRunBusy(false);
                }

                // A run that could not start because its target was read again under a new id: the toolbar reads the
                // project's targets again, so the next Run uses one that exists.
                if (vLine.IsError && (vLine.Text.StartsWith("Failed to run:", StringComparison.Ordinal) || vLine.Text.StartsWith("Build FAILED in 0.0s", StringComparison.Ordinal)))
                {
                    _ = InvokeAsync(async () =>
                    {
                        objLoadedForProjectKey = null;
                        try
                        {
                            await EnsureTargetsLoadedAsync();
                        }
                        catch (Exception aException) when (aException is IOException or InvalidOperationException)
                        {
                            Workbench.SetRunTargets([]);
                        }

                        StateHasChanged();
                    });
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

            StateHasChanged();
        });
    }

    private void OnWorkbenchChanged() => InvokeAsync(StateHasChanged);

    private void OnStartupDialogRequested() => InvokeAsync(() =>
    {
        objStartupOpen = true;
        StateHasChanged();
    });

    /// <summary>A branch switch can move or remove projects, so the targets are read again (BRD-227).</summary>
    private async void OnBranchChanged()
    {
        await InvokeAsync(async () =>
        {
            objLoadedForProjectKey = null;
            try
            {
                await EnsureTargetsLoadedAsync();
            }
            catch (Exception aException) when (aException is IOException or InvalidOperationException)
            {
                Workbench.SetRunTargets([]);
            }

            StateHasChanged();
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        AppStateService.Changed -= OnAppStateChanged;
        Workbench.Changed -= OnWorkbenchChanged;
        Workbench.BranchChanged -= OnBranchChanged;
        Workbench.StartupDialogRequested -= OnStartupDialogRequested;
        objWatchCts?.Cancel();
        objLookCts?.Cancel();
    }
}
