using Chatur.Core;
using Chatur.Core.Actions;
using Chatur.Core.Platform;
using ChaturUI.Layouts;
using ChaturUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ChaturUI.Shell;

/// <summary>
/// The main window's menu bar region. Owns the project switcher (REQ-UI-010): every other screen
/// reads <see cref="Chatur.Core.AppState.SelectedProject"/>, so picking a different project here and
/// calling <see cref="IProjectActions.SelectProjectAsync"/> is enough to make every one of them show
/// it, with no navigation of its own.
/// </summary>
/// <remarks>Owning cluster: H (REQ-UI-010, REQ-UI-018, REQ-UI-073). Sign out (REQ-FN-009) lives in <see cref="AccountMenu"/>.</remarks>
public partial class MenuBar : IAsyncDisposable
{
    /// <summary>The page's script runtime, so a clean-up for a page that has gone is skipped (REQ-NFR-011).</summary>
    [Inject]
    private IJSRuntime PageJs { get; set; } = default!;

    private DotNetObjectReference<MenuBar>? objSelfRef;
    private IJSObjectReference? objShortcuts;
    private IReadOnlyList<ProjectSummary> objAllProjects = [];
    private IReadOnlyList<ProjectSummary> objRecent = [];
    private ChaturBuildInfo? objBuildInfo;
    private ProjectOpenMode objOpenProjectMode;
    private FileSearchMode objSearchMode;
    private bool objOpenProjectOpen;
    private bool objSearchOpen;
    private bool objAboutOpen;
    private bool objCanRedo;

    [Inject]
    private IProjectActions Projects { get; set; } = default!;

    [Inject]
    private AppState AppStateService { get; set; } = default!;

    [Inject]
    private WorkbenchState Workbench { get; set; } = default!;

    [Inject]
    private ISecondaryWindow SecondaryWindow { get; set; } = default!;

    [Inject]
    private IAppShell AppShell { get; set; } = default!;

    [Inject]
    private IProjectOpenActions Opener { get; set; } = default!;

    [Inject]
    private IFileActions Files { get; set; } = default!;

    [Inject]
    private IPrerequisiteActions PrerequisiteActions { get; set; } = default!;

    [Inject]
    private IBuildRunActions BuildRun { get; set; } = default!;

    [Inject]
    private EditorInterop Editor { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    [Inject]
    private ShortcutInterop Shortcuts { get; set; } = default!;

    /// <summary>
    /// Tools ▸ "Go straight to": the Settings pages in the secondary window's own order, from
    /// providers to appearance (mockups/main.html, menu-tools-items). Account is reached from the
    /// account menu, as drawn.
    /// </summary>
    private static IEnumerable<SecondaryNavItem> SettingsPages =>
        SecondaryNavigation.Items.Where(aItem => aItem.Route.StartsWith("/settings/", StringComparison.Ordinal) && aItem.Route != "/settings/account");

    /// <summary>Opens the secondary window on a page (REQ-FN-106).</summary>
    private void OpenSecondary(string aRoute) => SecondaryWindow.Open(aRoute);

    private int? ProjectId => AppStateService.SelectedProject?.ProjectId;

    private bool objIsBusy => Workbench.IsBuildRunBusy;

    private IReadOnlyList<RunTarget> objTargets => Workbench.RunTargets;

    private RunTarget? objSelectedTarget => Workbench.SelectedRunTarget;

    private void OnWorkbenchChanged() => InvokeAsync(StateHasChanged);

    /// <summary>Loads run targets the first time the Project menu opens, matching the toolbar's timing.</summary>
    private async Task EnsureTargetsLoadedAsync()
    {
        if (ProjectId is not { } vProjectId || objTargets.Count > 0)
        {
            return;
        }

        Workbench.SetRunTargets(await BuildRun.TargetsAsync(vProjectId));
        Workbench.SetStartupTargets(await BuildRun.StartupAsync(vProjectId));
    }

    /// <summary>Project ▸ Project to run: chooses it now and remembers it for this solution and account (BRD-228).</summary>
    private async Task SelectTarget(RunTarget aTarget)
    {
        if (ProjectId is not { } vProjectId || !aTarget.IsAvailableOnThisMachine)
        {
            return;
        }

        Workbench.SelectRunTarget(aTarget);
        await BuildRun.SetRunTargetAsync(vProjectId, aTarget.RunTargetId);
    }

    /// <summary>Whether View ▸ Show hidden files is ticked for the signed-in account (BRD-225).</summary>
    private bool objShowHidden;

    /// <summary>File's close item names what is open: a solution or a folder (BRD-229).</summary>
    private string CloseLabel => AppStateService.SelectedProject?.Kind == "Solution" ? "Close solution" : "Close folder";

    /// <summary>View ▸ Show hidden files: keeps the choice for the account and has Files read its tree again (BRD-225).</summary>
    private async Task SetShowHiddenAsync(bool aShow)
    {
        objShowHidden = aShow;
        await Files.SetShowHiddenAsync(aShow);
        Workbench.NotifyFilesViewChanged();
    }

    /// <summary>Builds the project on the chosen target (REQ-UI-012).</summary>
    private async Task BuildAsync()
    {
        if (ProjectId is not { } vProjectId || objSelectedTarget is null || objIsBusy)
        {
            return;
        }

        Workbench.SetBuildRunBusy(true);
        var vBuildTask = BuildRun.BuildAsync(vProjectId, objSelectedTarget.RunTargetId);
        Workbench.NotifyBuildOrRunStarted();
        await vBuildTask;
    }

    /// <summary>Runs the chosen target (REQ-UI-013).</summary>
    private async Task RunAsync()
    {
        if (ProjectId is not { } vProjectId || objSelectedTarget is null || objIsBusy)
        {
            return;
        }

        Workbench.SetBuildRunBusy(true);
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

    /// <summary>Ctrl+, (⌘, on a Mac) does what Tools ▸ Settings does, the shortcut that item shows (REQ-UI-073).</summary>
    [JSInvokable]
    public void OnSettingsShortcut() => OpenSecondary("/settings/providers");

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool aFirstRender)
    {
        if (!aFirstRender)
        {
            return;
        }

        objSelfRef = DotNetObjectReference.Create(this);
        try
        {
            objShortcuts = await Shortcuts.ListenAsync(objSelfRef);
        }
        catch (JSDisconnectedException)
        {
            // the window closed before the listener was set up; nothing to listen for
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        AppStateService.Changed += OnSelectedProjectChanged;
        Workbench.Changed += OnAppStateChanged;
        await EnsureTargetsLoadedAsync();
        objShowHidden = await Files.ShowHiddenAsync();
        RefreshProjectListsInBackground();
    }

    /// <summary>Whether the open file can be saved now: it has changes, and no proposed change holds it read-only.</summary>
    private bool CanSave => Workbench.ActiveFile is { IsDirty: true } vFile && !Workbench.HasPendingChange(vFile.RelativePath);

    private bool CanSaveAll => Workbench.OpenFiles.Any(f => f.IsDirty && !Workbench.HasPendingChange(f.RelativePath));

    /// <summary>Whether the open file's editor can be changed: a file is open and no proposed change holds it read-only.</summary>
    private bool CanEdit => Workbench.ActiveFile is { } vFile && !Workbench.HasPendingChange(vFile.RelativePath);

    /// <summary>Opens the New project / Open a solution / Open a folder / Clone dialog (REQ-UI-073).</summary>
    private void OpenProjectDialog(ProjectOpenMode aMode)
    {
        objOpenProjectMode = aMode;
        objOpenProjectOpen = true;
    }

    /// <summary>Opens Go to file or Find in files (REQ-UI-073).</summary>
    private void OpenSearch(FileSearchMode aMode)
    {
        objSearchMode = aMode;
        objSearchOpen = true;
    }

    /// <summary>
    /// Re-reads the project switcher's list and File ▸ Recent projects off the page's event path (owner, 20 Nov 2026).
    /// Before, File's pointer-down and the switcher's click ran <see cref="IProjectActions.ScanAsync"/>, which walks
    /// every named folder on disk and runs a statement per subfolder synchronously on the circuit; with 80 or more
    /// projects the click that opens the menu queued behind it, and a second click toggled the menu shut.
    /// </summary>
    private void RefreshProjectListsInBackground()
    {
        if (Interlocked.Exchange(ref objRefreshing, 1) == 1)
        {
            objRefreshAgain = true;
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var vChanged = false;
                do
                {
                    objRefreshAgain = false;
                    var vProjects = await Projects.KnownAsync().ConfigureAwait(false);
                    // Decided (owner, 21 Nov 2026, replacing the 20 Nov "20 last opened, then All projects"): the
                    // switcher lists every one of the account's projects, the most recently opened first, in a list
                    // that scrolls and filters (REQ-UI-109).
                    var vOthers = vProjects
                        .OrderByDescending(p => p.LastOpenedUtc ?? DateTime.MinValue)
                        .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    var vRecent = vProjects
                        .Where(p => p.LastOpenedUtc is not null)
                        .OrderByDescending(p => p.LastOpenedUtc)
                        .Take(5)
                        .ToList();
                    if (!SameProjects(vOthers, objAllProjects) || !SameProjects(vRecent, objRecent))
                    {
                        objAllProjects = vOthers;
                        objRecent = vRecent;
                        vChanged = true;
                    }
                }
                while (objRefreshAgain);

                // Only a list that changed redraws the menu bar: a redraw while a submenu is open moved it after
                // the menu library had placed it (TR-024 check at 390 px, 21 Nov 2026).
                if (vChanged)
                {
                    await InvokeAsync(StateHasChanged).ConfigureAwait(false);
                }
            }
            catch (Exception aException) when (aException is InvalidOperationException or IOException or ObjectDisposedException or Microsoft.Data.Sqlite.SqliteException)
            {
                // the lists keep what they last held; the next project change reads them again
            }
            finally
            {
                Interlocked.Exchange(ref objRefreshing, 0);
            }
        });
    }

    /// <summary>Whether two project lists name the same projects, by the same names, in the same order.</summary>
    private static bool SameProjects(IReadOnlyList<ProjectSummary> aLeft, IReadOnlyList<ProjectSummary> aRight) =>
        aLeft.Select(p => (p.ProjectId, p.Name)).SequenceEqual(aRight.Select(p => (p.ProjectId, p.Name)));

    /// <summary>From how many projects on the switcher shows its filter box (REQ-UI-109).</summary>
    private const int SwitcherFilterFrom = 8;

    /// <summary>Whether the project switcher's list is open.</summary>
    private bool objSwitcherOpen;

    /// <summary>What the switcher's filter box holds.</summary>
    private string objSwitcherFilter = string.Empty;

    /// <summary>The projects the switcher shows: all of them, or those whose name or path holds the filter's text.</summary>
    private List<ProjectSummary> SwitcherProjects => string.IsNullOrWhiteSpace(objSwitcherFilter)
        ? objAllProjects.ToList()
        : objAllProjects.Where(p => p.Name.Contains(objSwitcherFilter.Trim(), StringComparison.OrdinalIgnoreCase)
            || p.Path.Contains(objSwitcherFilter.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

    private void OnSwitcherFilterChanged(string? aText) => objSwitcherFilter = aText ?? string.Empty;

    /// <summary>Escape closes the switcher, from its button, its filter box or its list, as it closes every menu.</summary>
    private void OnSwitcherKeyDown(KeyboardEventArgs aArgs)
    {
        if (aArgs.Key == "Escape" && objSwitcherOpen)
        {
            objSwitcherOpen = false;
            objSwitcherFilter = string.Empty;
        }
    }

    private int objRefreshing;
    private volatile bool objRefreshAgain;

    /// <summary>
    /// Asks the editor what it can redo as Edit opens, without holding the click back: the menu opens at once and
    /// Redo's state follows when the editor answers.
    /// </summary>
    private void StartEditStateRefresh()
    {
        if (Workbench.ActiveFile is null)
        {
            objCanRedo = false;
            return;
        }

        _ = InvokeAsync(async () =>
        {
            try
            {
                objCanRedo = (await Editor.StateAsync()).CanRedo;
                StateHasChanged();
            }
            catch (Exception aException) when (aException is JSException or JSDisconnectedException or TaskCanceledException or InvalidOperationException)
            {
                objCanRedo = false;
            }
        });
    }

    /// <summary>Runs undo, redo, cut or copy on the open file's editor.</summary>
    private async Task EditAsync(string aCommand) => await Editor.RunAsync(aCommand);

    private async Task PasteAsync() => await Editor.PasteAsync();

    /// <summary>File ▸ Save: writes the open file to disk (REQ-FN-044).</summary>
    private async Task SaveAsync()
    {
        if (Workbench.ActiveFile is { } vFile)
        {
            await SaveFileAsync(vFile);
        }
    }

    /// <summary>File ▸ Save all: writes every open file that has changes.</summary>
    private async Task SaveAllAsync()
    {
        foreach (var vFile in Workbench.OpenFiles.Where(f => f.IsDirty && !Workbench.HasPendingChange(f.RelativePath)).ToList())
        {
            await SaveFileAsync(vFile);
        }
    }

    private async Task SaveFileAsync(OpenFile aFile)
    {
        if (AppStateService.SelectedProject is not { } vProject)
        {
            return;
        }

        await Files.SaveAsync(vProject.ProjectId, aFile.RelativePath, aFile.CurrentContent);
        Workbench.MarkSaved(aFile.Id);
    }

    /// <summary>File ▸ Recent projects ▸ Project list: the Start window with every project, the open one left open (<c>/start?all=1</c>).</summary>
    private void ShowProjectList() => Nav.NavigateTo("/start?all=1");

    /// <summary>File ▸ Close solution or Close folder (BRD-229): nothing stays selected, and the project list is shown.</summary>
    private async Task CloseProjectAsync()
    {
        await Opener.CloseProjectAsync();
        Nav.NavigateTo("/start?all=1");
    }

    /// <summary>Help ▸ About Chatur: Chatur's own version and commit, as Prerequisites shows them (REQ-UI-018).</summary>
    private async Task ShowAboutAsync()
    {
        try
        {
            objBuildInfo = await PrerequisiteActions.OwnVersionAsync();
        }
        catch (Exception aException) when (aException is InvalidOperationException or NotImplementedException)
        {
            objBuildInfo = null;
        }

        objAboutOpen = true;
    }

    /// <summary>
    /// Loads the other known projects the first time the switcher opens, so a project with nothing
    /// selected yet still shows a normal, empty menu bar rather than an eager background fetch.
    /// </summary>
    /// <summary>Selects another project without leaving the Workbench (REQ-UI-010).</summary>
    private async Task SwitchToAsync(int aProjectId)
    {
        objSwitcherOpen = false;
        objSwitcherFilter = string.Empty;
        await Projects.SelectProjectAsync(aProjectId);
        RefreshProjectListsInBackground();
    }

    /// <summary>Opens the Agent workspace in a tab, on the agent already shown there or the first one.</summary>
    private void OpenAgentWorkspace() => Workbench.OpenAgentWorkspace();

    private void OnAppStateChanged() => InvokeAsync(StateHasChanged);

    /// <summary>Another project was opened somewhere: the switcher and Recent projects read the list again, in the background.</summary>
    private void OnSelectedProjectChanged()
    {
        RefreshProjectListsInBackground();
        OnAppStateChanged();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        AppStateService.Changed -= OnSelectedProjectChanged;
        Workbench.Changed -= OnAppStateChanged;
        if (objShortcuts is { } vShortcuts)
        {
            // Never waits on a page that has gone: Sign out reloads the page, and waiting here for the old page's
            // script left the window on "Loading Chatur" (REQ-FN-009, miss of 2026-11-16).
            objShortcuts = null;
            await JsCleanUp.RunAsync(PageJs, async () =>
            {
                await vShortcuts.InvokeVoidAsync("dispose");
                await vShortcuts.DisposeAsync();
            });
        }

        objSelfRef?.Dispose();
    }
}
