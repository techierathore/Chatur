using Chatur.Core;
using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Shell;

/// <summary>
/// The main window's menu bar region. Owns the project switcher (REQ-UI-010): every other screen
/// reads <see cref="Chatur.Core.AppState.SelectedProject"/>, so picking a different project here and
/// calling <see cref="IProjectActions.SelectProjectAsync"/> is enough to make every one of them show
/// it, with no navigation of its own.
/// </summary>
/// <remarks>Owning cluster: H (REQ-UI-010, REQ-UI-018). Sign out (REQ-FN-009) lives in <see cref="AccountMenu"/>.</remarks>
public partial class MenuBar : IDisposable
{
    private IReadOnlyList<ProjectSummary> objOtherProjects = [];
    private bool objProjectsLoaded;

    [Inject]
    private IProjectActions Projects { get; set; } = default!;

    [Inject]
    private AppState AppStateService { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        AppStateService.Changed += OnAppStateChanged;
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

    private void OnAppStateChanged() => InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    public void Dispose() => AppStateService.Changed -= OnAppStateChanged;
}
