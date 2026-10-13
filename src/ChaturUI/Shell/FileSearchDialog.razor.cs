using Chatur.Core;
using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ChaturUI.Shell;

/// <summary>Which search the <see cref="FileSearchDialog"/> runs.</summary>
public enum FileSearchMode
{
    /// <summary>Edit ▸ Go to file…: files by name, listed as the owner types.</summary>
    GoToFile,

    /// <summary>Edit ▸ Find in files…: lines of text, searched when asked.</summary>
    FindInFiles
}

/// <summary>
/// Code-behind for <c>FileSearchDialog.razor</c> (REQ-UI-073): searches through
/// <see cref="IFileSearchActions"/> and opens the chosen file in the file view.
/// </summary>
public partial class FileSearchDialog
{
    private IReadOnlyList<FileSearchHit> objHits = [];
    private string objQuery = string.Empty;
    private bool objIsBusy;
    private bool objSearched;
    private bool objWasOpen;

    [Inject]
    private IFileSearchActions Search { get; set; } = default!;

    [Inject]
    private IFileActions Files { get; set; } = default!;

    [Inject]
    private AppState AppStateService { get; set; } = default!;

    [Inject]
    private WorkbenchState Workbench { get; set; } = default!;

    /// <summary>Whether the dialog is open. Two-way bindable from the caller with <c>@bind-Open</c>.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised when <see cref="Open"/> changes, including when the dialog closes itself.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Which search the dialog runs.</summary>
    [Parameter]
    public FileSearchMode Mode { get; set; }

    private int? objProjectId => AppStateService.SelectedProject?.ProjectId;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (Open && !objWasOpen)
        {
            objQuery = string.Empty;
            objHits = [];
            objSearched = false;
            if (Mode == FileSearchMode.GoToFile && objProjectId is { } vProjectId)
            {
                objHits = await Search.FindFilesAsync(vProjectId, string.Empty);
            }
        }

        objWasOpen = Open;
    }

    /// <summary>Go to file lists as the owner types; Find in files waits for Enter or the Find button.</summary>
    private async Task OnQueryChangedAsync(string? aValue)
    {
        objQuery = aValue ?? string.Empty;
        if (Mode == FileSearchMode.GoToFile)
        {
            await SearchAsync();
        }
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs aArgs)
    {
        if (Mode == FileSearchMode.FindInFiles && aArgs.Key == "Enter" && !string.IsNullOrWhiteSpace(objQuery))
        {
            await SearchAsync();
        }
    }

    private async Task SearchAsync()
    {
        if (objProjectId is not { } vProjectId)
        {
            return;
        }

        objIsBusy = Mode == FileSearchMode.FindInFiles;
        try
        {
            objHits = Mode == FileSearchMode.GoToFile
                ? await Search.FindFilesAsync(vProjectId, objQuery)
                : await Search.FindInFilesAsync(vProjectId, objQuery);
            objSearched = true;
        }
        finally
        {
            objIsBusy = false;
        }
    }

    /// <summary>Opens the chosen file in the file view and closes the dialog.</summary>
    private async Task OpenHitAsync(FileSearchHit aHit)
    {
        if (objProjectId is not { } vProjectId)
        {
            return;
        }

        Workbench.Open(aHit.RelativePath, await Files.ReadAsync(vProjectId, aHit.RelativePath));
        await OpenChanged.InvokeAsync(false);
    }
}
