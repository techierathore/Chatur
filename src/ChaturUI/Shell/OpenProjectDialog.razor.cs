using Chatur.Core.Actions;
using Chatur.Core.Platform;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Shell;

/// <summary>Which way into a project the <see cref="OpenProjectDialog"/> is asking for.</summary>
public enum ProjectOpenMode
{
    /// <summary>File ▸ New project</summary>
    NewProject,

    /// <summary>File ▸ Open a solution</summary>
    Solution,

    /// <summary>File ▸ Open a folder</summary>
    Folder,

    /// <summary>File ▸ Clone a repository</summary>
    Clone,

    /// <summary>Start ▸ Open a file: the folder that holds it opens, with the file open (BRD-215)</summary>
    File
}

/// <summary>
/// Code-behind for <c>OpenProjectDialog.razor</c> (REQ-UI-073, REQ-UI-094): one dialog for the File-menu and Start ways
/// into a project, each ending in <see cref="IProjectOpenActions"/> and the Workbench showing the
/// project it made or found.
/// </summary>
public partial class OpenProjectDialog
{
    private string objPath = string.Empty;
    private string objName = string.Empty;
    private string objAddress = string.Empty;
    private string? objErrorMessage;
    private bool objIsBusy;
    private bool objWasOpen;

    [Inject]
    private IProjectOpenActions Opener { get; set; } = default!;

    [Inject]
    private IFolderPicker Picker { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    [Inject]
    private WorkbenchState Workbench { get; set; } = default!;

    /// <summary>Whether the dialog is open. Two-way bindable from the caller with <c>@bind-Open</c>.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised when <see cref="Open"/> changes, including when the dialog closes itself.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Which way into a project the dialog asks for.</summary>
    [Parameter]
    public ProjectOpenMode Mode { get; set; }

    private string Title => Mode switch
    {
        ProjectOpenMode.NewProject => "New project",
        ProjectOpenMode.Solution => "Open a solution",
        ProjectOpenMode.Folder => "Open a folder",
        ProjectOpenMode.File => "Open a file",
        _ => "Clone a repository"
    };

    private string Description => Mode switch
    {
        ProjectOpenMode.NewProject => "Makes a folder with a new, empty solution in it, and opens it.",
        ProjectOpenMode.Solution => "A .sln file, or the folder that holds one. Its projects are read.",
        ProjectOpenMode.Folder => "Anything on disk, project or not.",
        ProjectOpenMode.File => "Any file. The folder that holds it opens, with the file open in a tab.",
        _ => "Copies a repository from GitHub, or any address you paste, and opens it."
    };

    private string PathLabel => Mode switch
    {
        ProjectOpenMode.NewProject => "Make it in this folder",
        ProjectOpenMode.Solution => "The solution file or its folder",
        ProjectOpenMode.Folder => "The folder",
        ProjectOpenMode.File => "The file",
        _ => "Clone into this folder"
    };

    private string PathPlaceholder => Mode switch
    {
        ProjectOpenMode.Solution => "The full path of a .sln file or its folder",
        ProjectOpenMode.File => "The file's full path",
        _ => "The folder's full path"
    };

    private string ActionLabel => Mode switch
    {
        ProjectOpenMode.NewProject => "Create project",
        ProjectOpenMode.Clone => "Clone",
        _ => "Open"
    };

    private bool CanSubmit =>
        !string.IsNullOrWhiteSpace(objPath)
        && (Mode != ProjectOpenMode.NewProject || !string.IsNullOrWhiteSpace(objName))
        && (Mode != ProjectOpenMode.Clone || !string.IsNullOrWhiteSpace(objAddress));

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Open && !objWasOpen)
        {
            objPath = string.Empty;
            objName = string.Empty;
            objAddress = string.Empty;
            objErrorMessage = null;
        }

        objWasOpen = Open;
    }

    /// <summary>Opens the machine's own folder picker and fills the path with what it returns.</summary>
    private async Task BrowseAsync()
    {
        var vPicked = Mode == ProjectOpenMode.File
            ? await Picker.PickFileAsync(Title)
            : await Picker.PickFolderAsync(Title);
        if (vPicked is not null)
        {
            objPath = vPicked;
        }
    }

    /// <summary>Does what the File menu item said, then shows the project on the Workbench.</summary>
    private async Task SubmitAsync()
    {
        objIsBusy = true;
        objErrorMessage = null;
        try
        {
            string? vFileToOpen = null;
            switch (Mode)
            {
                case ProjectOpenMode.NewProject:
                    await Opener.CreateSolutionAsync(objPath, objName);
                    break;
                case ProjectOpenMode.Solution:
                    await Opener.OpenSolutionAsync(objPath);
                    break;
                case ProjectOpenMode.Folder:
                    await Opener.OpenFolderAsync(objPath);
                    break;
                case ProjectOpenMode.File:
                    vFileToOpen = (await Opener.OpenFileAsync(objPath)).RelativePath;
                    break;
                default:
                    await Opener.CloneAsync(objAddress, objPath);
                    break;
            }

            await OpenChanged.InvokeAsync(false);
            Nav.NavigateTo("/");
            if (vFileToOpen is not null)
            {
                // The Workbench opens it as soon as the project that holds it is showing (BRD-215).
                Workbench.RequestOpenFile(vFileToOpen);
            }
        }
        catch (Exception aException) when (aException is InvalidOperationException or ArgumentException or IOException or UnauthorizedAccessException)
        {
            objErrorMessage = aException.Message;
        }
        finally
        {
            objIsBusy = false;
        }
    }
}
