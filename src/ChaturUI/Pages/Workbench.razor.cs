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
    private string? objUserId;

    [Inject]
    private AppState AppStateService { get; set; } = default!;

    [Inject]
    private WorkbenchState WorkbenchTabs { get; set; } = default!;

    [Inject]
    private IProjectActions Projects { get; set; } = default!;

    [Inject]
    private IFileActions Files { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        objProjectId = AppStateService.SelectedProject?.ProjectId;
        objUserId = AppStateService.CurrentUser?.UserId;
        AppStateService.Changed += OnAppStateChanged;
        WorkbenchTabs.Changed += OnWorkbenchChanged;
        WorkbenchTabs.OpenFileRequested += OnOpenFileRequested;

        if (AppStateService.SelectedProject is null)
        {
            objProjectId = (await Projects.SelectedProjectAsync())?.ProjectId;
        }

        await OpenPendingFileAsync();
    }

    private void OnOpenFileRequested() => _ = InvokeAsync(OpenPendingFileAsync);

    /// <summary>
    /// Opens the file Start's "Open a file" chose, once its project is the one showing (BRD-215). A file
    /// that cannot be read leaves the Workbench on its project with nothing open, rather than failing.
    /// </summary>
    private async Task OpenPendingFileAsync()
    {
        if (AppStateService.SelectedProject is not { } vProject || WorkbenchTabs.TakePendingFile() is not { } vPath)
        {
            return;
        }

        try
        {
            var vContent = await Files.ReadAsync(vProject.ProjectId, vPath);
            WorkbenchTabs.Open(vPath, vContent);
        }
        catch (Exception aException) when (aException is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            // Nothing to show: the project is open, the file is not.
        }
    }

    /// <summary>View ▸ Files shows or hides the files panel, which this page hosts (REQ-UI-073).</summary>
    private void OnWorkbenchChanged() => InvokeAsync(StateHasChanged);

    private void OnAppStateChanged()
    {
        // Another account signing in shows nothing the last one had open (BRD-216). Two accounts can
        // both have a project 1, so the account is compared as well as the project.
        var vUserId = AppStateService.CurrentUser?.UserId;
        if (!string.Equals(vUserId, objUserId, StringComparison.Ordinal))
        {
            objUserId = vUserId;
            objProjectId = AppStateService.SelectedProject?.ProjectId;
            WorkbenchTabs.ResetForAccount();
            return;
        }

        var vProjectId = AppStateService.SelectedProject?.ProjectId;
        if (vProjectId == objProjectId)
        {
            return;
        }

        objProjectId = vProjectId;
        var vPending = WorkbenchTabs.TakePendingFile();
        WorkbenchTabs.Reset();
        if (vPending is not null)
        {
            WorkbenchTabs.RequestOpenFile(vPending);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        AppStateService.Changed -= OnAppStateChanged;
        WorkbenchTabs.Changed -= OnWorkbenchChanged;
        WorkbenchTabs.OpenFileRequested -= OnOpenFileRequested;
    }
}
