using Chatur.Core;
using Chatur.Core.Actions;
using Chatur.Core.Platform;
using ChaturUI.Layouts;
using ChaturUI.Services;
using Microsoft.AspNetCore.Components;
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
    private DotNetObjectReference<MenuBar>? objSelfRef;
    private IJSObjectReference? objShortcuts;
    private IReadOnlyList<ProjectSummary> objOtherProjects = [];
    private IReadOnlyList<ProjectSummary> objRecent = [];
    private ChaturBuildInfo? objBuildInfo;
    private ProjectOpenMode objOpenProjectMode;
    private FileSearchMode objSearchMode;
    private bool objProjectsLoaded;
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
    }

    private void SelectTarget(RunTarget aTarget) => Workbench.SelectRunTarget(aTarget);

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
        AppStateService.Changed += OnAppStateChanged;
        Workbench.Changed += OnAppStateChanged;
        await EnsureTargetsLoadedAsync();
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

    /// <summary>Re-reads the projects opened most recently each time File opens, so the list never lags what was just opened.</summary>
    private async Task LoadRecentAsync()
    {
        objRecent = (await Projects.ScanAsync())
            .Where(p => p.LastOpenedUtc is not null)
            .OrderByDescending(p => p.LastOpenedUtc)
            .Take(5)
            .ToList();
    }

    /// <summary>Asks the editor what it can undo and redo each time Edit opens, so Redo is off when there is nothing to redo.</summary>
    private async Task RefreshEditStateAsync()
    {
        if (Workbench.ActiveFile is null)
        {
            objCanRedo = false;
            return;
        }

        objCanRedo = (await Editor.StateAsync()).CanRedo;
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

    /// <summary>File ▸ Close project: nothing stays selected, and the project list is shown.</summary>
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
    private async Task EnsureProjectsLoadedAsync()
    {
        if (objProjectsLoaded)
        {
            return;
        }

        objOtherProjects = await Projects.ScanAsync();
        objProjectsLoaded = true;
    }

    /// <summary>Selects another project without leaving the Workbench (REQ-UI-010).</summary>
    private async Task SwitchToAsync(int aProjectId)
    {
        await Projects.SelectProjectAsync(aProjectId);
        objProjectsLoaded = false;
    }

    /// <summary>Opens the Agent workspace in a tab, on the agent already shown there or the first one.</summary>
    private void OpenAgentWorkspace() => Workbench.OpenAgentWorkspace();

    private void OnAppStateChanged() => InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        AppStateService.Changed -= OnAppStateChanged;
        Workbench.Changed -= OnAppStateChanged;
        if (objShortcuts is not null)
        {
            try
            {
                await objShortcuts.InvokeVoidAsync("dispose");
                await objShortcuts.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // the window is already gone, and its listener with it
            }
        }

        objSelfRef?.Dispose();
    }
}
