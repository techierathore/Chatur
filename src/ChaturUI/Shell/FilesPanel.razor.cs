using Chatur.Core;
using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Shell;

/// <summary>
/// The Workbench's files panel: the project's own solution and folders, opening and closing
/// (REQ-UI-039), and opening a file from the tree into a tab (REQ-UI-040).
/// </summary>
/// <remarks>Owning cluster: H (REQ-UI-039, REQ-UI-040; REQ-FN-007 for the branch row).</remarks>
public partial class FilesPanel : IDisposable
{
    private IReadOnlyList<FileTreeNode> objTree = [];
    private int? objLoadedForProjectId;
    private string? objSelectedPath;
    private string? objBranchName;

    [Inject]
    private IFileActions Files { get; set; } = default!;

    [Inject]
    private ISourceControlActions SourceControl { get; set; } = default!;

    [Inject]
    private AppState AppStateService { get; set; } = default!;

    [Inject]
    private WorkbenchState Workbench { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        AppStateService.Changed += OnAppStateChanged;
        Workbench.BranchChanged += OnBranchChanged;
        await EnsureTreeLoadedAsync();
    }

    /// <summary>A branch switch changes the files on disk and the branch named beneath them, so both are read again.</summary>
    private async void OnBranchChanged()
    {
        await InvokeAsync(async () =>
        {
            objLoadedForProjectId = null;
            await EnsureTreeLoadedAsync();
            StateHasChanged();
        });
    }

    private async Task EnsureTreeLoadedAsync()
    {
        var vProjectId = AppStateService.SelectedProject?.ProjectId;
        if (vProjectId == objLoadedForProjectId)
        {
            return;
        }

        objLoadedForProjectId = vProjectId;
        objTree = vProjectId is { } vId ? await Files.TreeAsync(vId) : [];
        objBranchName = null;

        // The toolbar/files panel names the project's own branch (REQ-FN-007); a project with no
        // repository under it (or one M/N's source-control module cannot read yet) shows nothing
        // rather than a wrong guess.
        if (vProjectId is { } vBranchProjectId)
        {
            try
            {
                var vStatus = await SourceControl.StatusAsync(vBranchProjectId);
                objBranchName = vStatus.Branch;
            }
            catch (Exception)
            {
                objBranchName = null;
            }
        }
    }

    /// <summary>
    /// Reacts to the tree's own selection (REQ-UI-040): a folder just opens or closes on its own, a
    /// file's text is read and opened into a tab.
    /// </summary>
    private async Task OnSelectedPathChangedAsync(string? aPath)
    {
        objSelectedPath = aPath;
        if (aPath is null || AppStateService.SelectedProject is not { } vProject)
        {
            return;
        }

        var vNode = FindNode(objTree, aPath);
        if (vNode is null || vNode.IsFolder)
        {
            return;
        }

        var vContent = await Files.ReadAsync(vProject.ProjectId, aPath);
        Workbench.Open(aPath, vContent);
    }

    private static FileTreeNode? FindNode(IReadOnlyList<FileTreeNode> aNodes, string aPath)
    {
        foreach (var vNode in aNodes)
        {
            if (vNode.RelativePath == aPath)
            {
                return vNode;
            }

            var vFound = FindNode(vNode.Children, aPath);
            if (vFound is not null)
            {
                return vFound;
            }
        }

        return null;
    }

    private async void OnAppStateChanged()
    {
        await InvokeAsync(async () =>
        {
            await EnsureTreeLoadedAsync();
            StateHasChanged();
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        AppStateService.Changed -= OnAppStateChanged;
        Workbench.BranchChanged -= OnBranchChanged;
    }
}
