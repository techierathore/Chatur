using Chatur.Core;
using Chatur.Core.Actions;
using ChaturUI.Shell;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Pages;

/// <summary>
/// Workbench (mockups/main.html), the main window's own content. Composes the region components in
/// <c>ChaturUI.Shell</c>. Its own logic: hydrating <see cref="AppState.SelectedProject"/> when the
/// Workbench is the very first page a circuit renders (REQ-FN-007) — every region below reads
/// <see cref="AppState"/> and nothing else populates it on a direct load, only Start's own restart
/// path does — and resetting <see cref="WorkbenchState"/>'s open tabs when the selected project
/// changes, since no single region owns that cross-cutting reset on its own.
/// </summary>
public partial class Workbench : IDisposable
{
    private int? objProjectId;

    [Inject]
    private AppState AppStateService { get; set; } = default!;

    [Inject]
    private WorkbenchState WorkbenchTabs { get; set; } = default!;

    [Inject]
    private IProjectActions Projects { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        objProjectId = AppStateService.SelectedProject?.ProjectId;
        AppStateService.Changed += OnAppStateChanged;

        if (AppStateService.SelectedProject is null)
        {
            objProjectId = (await Projects.SelectedProjectAsync())?.ProjectId;
        }
    }

    private void OnAppStateChanged()
    {
        var vProjectId = AppStateService.SelectedProject?.ProjectId;
        if (vProjectId == objProjectId)
        {
            return;
        }

        objProjectId = vProjectId;
        WorkbenchTabs.Reset();
    }

    /// <inheritdoc />
    public void Dispose() => AppStateService.Changed -= OnAppStateChanged;
}
