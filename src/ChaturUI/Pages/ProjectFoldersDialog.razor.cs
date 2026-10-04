using Chatur.Core.Actions;
using Chatur.Core.Platform;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Pages;

/// <summary>
/// Code-behind for <c>ProjectFoldersDialog.razor</c> (REQ-UI-005, REQ-UI-009). Loads the named
/// folders whenever the dialog opens, adds or removes one, and tells <see cref="Start"/> to re-scan
/// afterwards via <see cref="FoldersChanged"/>.
/// </summary>
public partial class ProjectFoldersDialog
{
    [Inject]
    private IProjectActions Projects { get; set; } = null!;

    [Inject]
    private IFolderPicker Picker { get; set; } = null!;

    /// <summary>Whether the dialog is open. Two-way bindable from the caller with <c>@bind-Open</c>.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised when <see cref="Open"/> changes, including when the dialog closes itself.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Raised after a folder is added or removed, so the caller can re-scan (REQ-UI-005, REQ-UI-009).</summary>
    [Parameter]
    public EventCallback FoldersChanged { get; set; }

    private IReadOnlyList<ProjectFolder> objFolders = Array.Empty<ProjectFolder>();
    private string objNewPath = string.Empty;
    private string? objErrorMessage;
    private bool objIsBusy;
    private bool objWasOpen;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (Open && !objWasOpen)
        {
            objErrorMessage = null;
            objNewPath = string.Empty;
            objFolders = await Projects.ListFoldersAsync();
        }

        objWasOpen = Open;
    }

    /// <summary>Opens the machine's own folder picker and fills the typed-path field with what it returns (REQ-UI-005).</summary>
    private async Task BrowseAsync()
    {
        var vPicked = await Picker.PickFolderAsync("Choose a folder for Chatur to search");
        if (vPicked is not null)
        {
            objNewPath = vPicked;
        }
    }

    /// <summary>Names the typed path as a new folder to search (REQ-UI-005).</summary>
    private async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(objNewPath))
        {
            return;
        }

        objIsBusy = true;
        objErrorMessage = null;
        try
        {
            await Projects.AddFolderAsync(objNewPath);
            objNewPath = string.Empty;
            objFolders = await Projects.ListFoldersAsync();
            await FoldersChanged.InvokeAsync();
        }
        catch (Exception aException)
        {
            objErrorMessage = aException.Message;
        }
        finally
        {
            objIsBusy = false;
        }
    }

    /// <summary>Removes a named folder; its projects leave the list with it (REQ-UI-009).</summary>
    private async Task RemoveAsync(int aFolderId)
    {
        objIsBusy = true;
        objErrorMessage = null;
        try
        {
            await Projects.RemoveFolderAsync(aFolderId);
            objFolders = await Projects.ListFoldersAsync();
            await FoldersChanged.InvokeAsync();
        }
        catch (Exception aException)
        {
            objErrorMessage = aException.Message;
        }
        finally
        {
            objIsBusy = false;
        }
    }
}
