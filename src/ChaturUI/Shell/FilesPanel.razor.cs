using Chatur.Core;
using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;
using TrBlazeUI.Components.Toast;

namespace ChaturUI.Shell;

/// <summary>
/// The Workbench's files panel: the project's own folders, read one folder at a time as each opens
/// (REQ-UI-039, BRD-217), opening a file from the tree into a tab (REQ-UI-040), setting the project to
/// run from a right-click (BRD-228), and the branch picker at its foot (BRD-222, BRD-223).
/// </summary>
/// <remarks>
/// Owning cluster: H (REQ-UI-039, REQ-UI-040; REQ-FN-007 for the branch row). Nothing here may throw
/// out of an event handler: on 16 Nov a tree read failing inside <see cref="OnAppStateChanged"/> stopped
/// the whole app, and every start after it (owner's run on Xpenser).
/// </remarks>
public partial class FilesPanel : IDisposable
{
    private readonly Dictionary<string, IReadOnlyList<FileTreeNode>> objChildren = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FileTreeNode> objNodes = new(StringComparer.Ordinal);
    private readonly HashSet<string> objLoading = new(StringComparer.Ordinal);
    private IReadOnlyList<FileTreeNode> objTree = [];
    private int? objLoadedForProjectId;
    private string? objSelectedPath;
    private string? objBranch;
    private BranchList? objBranches;
    private string? objTreeProblem;
    private bool objIsRefreshing;
    private bool objDirtyOpen;
    private bool objSwitching;
    private string? objPendingBranch;
    private string? objSwitchProblem;

    [Inject]
    private IFileActions Files { get; set; } = default!;

    [Inject]
    private ISourceControlActions SourceControl { get; set; } = default!;

    [Inject]
    private IBuildRunActions BuildRun { get; set; } = default!;

    [Inject]
    private AppState AppStateService { get; set; } = default!;

    [Inject]
    private WorkbenchState Workbench { get; set; } = default!;

    [Inject]
    private ToastService Toast { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        AppStateService.Changed += OnAppStateChanged;
        Workbench.BranchChanged += OnBranchChanged;
        Workbench.FilesViewChanged += OnFilesViewChanged;
        Workbench.Changed += OnWorkbenchChanged;
        await EnsureTreeLoadedAsync();
    }

    /// <summary>The children of a folder read so far, for <see cref="FileTreeBranch"/>; empty until it is opened.</summary>
    /// <param name="aPath">The folder's path relative to the project root.</param>
    public IReadOnlyList<FileTreeNode> ChildrenOf(string aPath) => objChildren.TryGetValue(aPath, out var vChildren) ? vChildren : [];

    /// <summary>Whether a folder's children are being read now, for the tree's loading row.</summary>
    /// <param name="aPath">The folder's path relative to the project root.</param>
    public bool IsLoading(string aPath) => objLoading.Contains(aPath);

    /// <summary>
    /// The run target a tree row stands for, when it is a runnable project: its <c>.csproj</c>, or the
    /// folder that holds it (BRD-228). Null for every other row.
    /// </summary>
    /// <param name="aNode">The tree row.</param>
    public RunTarget? RunTargetFor(FileTreeNode aNode) =>
        Workbench.RunTargets.FirstOrDefault(t => t.IsAvailableOnThisMachine && t.ProjectFile.Length > 0
            && (aNode.IsFolder
                ? string.Equals(DirectoryOf(t.ProjectFile), aNode.RelativePath, StringComparison.OrdinalIgnoreCase)
                : string.Equals(t.ProjectFile, aNode.RelativePath, StringComparison.OrdinalIgnoreCase)));

    /// <summary>Whether a row's project is the one set to run now, for its tick in the right-click menu.</summary>
    /// <param name="aTarget">The row's run target.</param>
    public bool IsRunTarget(RunTarget aTarget) => Workbench.SelectedRunTarget?.RunTargetId == aTarget.RunTargetId;

    /// <summary>Right-click ▸ Set as the project to run (BRD-228): chosen now and remembered for this solution.</summary>
    /// <param name="aTarget">The row's run target.</param>
    public async Task SetRunTargetAsync(RunTarget aTarget)
    {
        if (AppStateService.SelectedProject is not { } vProject)
        {
            return;
        }

        Workbench.SelectRunTarget(aTarget);
        await BuildRun.SetRunTargetAsync(vProject.ProjectId, aTarget.RunTargetId);
    }

    /// <summary>Reads a folder's entries the first time it opens (BRD-217); a failure leaves it empty, never stops the app.</summary>
    /// <param name="aPath">The folder's path relative to the project root.</param>
    public async Task LoadChildrenAsync(string aPath)
    {
        if (objChildren.ContainsKey(aPath) || AppStateService.SelectedProject is not { } vProject)
        {
            return;
        }

        objLoading.Add(aPath);
        StateHasChanged();
        try
        {
            var vChildren = await Files.ChildrenAsync(vProject.ProjectId, aPath);
            objChildren[aPath] = vChildren;
            Remember(vChildren);
        }
        catch (Exception aException) when (aException is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            objChildren[aPath] = [];
        }
        finally
        {
            objLoading.Remove(aPath);
        }

        StateHasChanged();
    }

    private static string DirectoryOf(string aRelativeFile)
    {
        var vSlash = aRelativeFile.LastIndexOf('/');
        return vSlash < 0 ? string.Empty : aRelativeFile[..vSlash];
    }

    private void Remember(IEnumerable<FileTreeNode> aNodes)
    {
        foreach (var vNode in aNodes)
        {
            objNodes[vNode.RelativePath] = vNode;
        }
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

    /// <summary>View ▸ Show hidden files changed: the tree is read again with the new choice (BRD-225).</summary>
    private async void OnFilesViewChanged()
    {
        await InvokeAsync(async () =>
        {
            objLoadedForProjectId = null;
            await EnsureTreeLoadedAsync();
            StateHasChanged();
        });
    }

    /// <summary>The run targets arrived or changed: the right-click menu follows them.</summary>
    private void OnWorkbenchChanged() => InvokeAsync(StateHasChanged);

    /// <summary>
    /// The header's Refresh (REQ-UI-039): reads the project's tree and branch from disk again, so a
    /// file made or removed outside Chatur shows without reopening the project.
    /// </summary>
    private async Task RefreshAsync()
    {
        objIsRefreshing = true;
        try
        {
            objLoadedForProjectId = null;
            await EnsureTreeLoadedAsync();
        }
        finally
        {
            objIsRefreshing = false;
        }
    }

    private async Task EnsureTreeLoadedAsync()
    {
        var vProjectId = AppStateService.SelectedProject?.ProjectId;
        if (vProjectId == objLoadedForProjectId)
        {
            return;
        }

        // A reload of the same project (Refresh, a branch switch) arrives with no project loaded and keeps the
        // branch list until it is read again; another project's list is never offered.
        if (vProjectId is null || objLoadedForProjectId is not null)
        {
            objBranches = null;
        }

        objLoadedForProjectId = vProjectId;
        objChildren.Clear();
        objNodes.Clear();
        objTreeProblem = null;
        objBranch = null;
        try
        {
            objTree = vProjectId is { } vId ? await Files.TreeAsync(vId) : [];
            Remember(objTree);
        }
        catch (Exception aException)
        {
            // BRD-217: whatever the disk says, the panel says it and the app goes on.
            objTree = [];
            objTreeProblem = aException.Message;
        }

        // The files panel names the project's own branch (REQ-FN-007); a project with no repository
        // under it shows nothing rather than a wrong guess.
        if (vProjectId is { } vBranchProjectId)
        {
            try
            {
                var vStatus = await SourceControl.StatusAsync(vBranchProjectId);
                objBranch = string.IsNullOrEmpty(vStatus.Branch) ? null : vStatus.Branch;
                if (objBranch is not null)
                {
                    StartBranchListRefresh();
                }
            }
            catch (Exception)
            {
                objBranch = null;
            }
        }
    }

    /// <summary>Reads the local and remote branches in the background, so the picker never lags what is on disk and never waits for git (BRD-222).</summary>
    /// <remarks>
    /// Owner, 20 Nov 2026: every item in the branch menu was greyed out on 6 of 10 phase 3 screens. The list was read
    /// with git only on the click that opened the menu, which on a project under /mnt/c took over a second, so a menu
    /// read at once held only "Reading the branches" and "No remote branches", both disabled. The list is now read as
    /// soon as the branch is known, and read again in the background each time the menu opens, never holding it back.
    /// </remarks>
    private void StartBranchListRefresh()
    {
        if (AppStateService.SelectedProject is not { } vProject || objBranchesLoading)
        {
            return;
        }

        objBranchesLoading = true;
        _ = InvokeAsync(async () =>
        {
            try
            {
                var vList = await SourceControl.BranchListAsync(vProject.ProjectId);
                if (AppStateService.SelectedProject?.ProjectId == vProject.ProjectId)
                {
                    objBranches = vList;
                }
            }
            catch (Exception aException) when (aException is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
            {
                objBranches ??= new BranchList(objBranch ?? string.Empty, [], []);
            }
            finally
            {
                objBranchesLoading = false;
            }

            StateHasChanged();
        });
    }

    private bool objBranchesLoading;

    /// <summary>Whether a chosen branch is waiting on the check for uncommitted changes.</summary>
    private bool objCheckingChanges;

    /// <summary>The branch a pick is switching to, while the check for changes or the switch is under way; otherwise none.</summary>
    private string? SwitchingTo => (objCheckingChanges || objSwitching) && objPendingBranch is { } vTo && vTo != objBranch ? vTo : null;

    /// <summary>
    /// A branch was chosen in the picker (BRD-222): switched at once when the working copy is clean,
    /// otherwise the person is asked first what happens to the changes (BRD-223).
    /// </summary>
    private async Task ChooseBranchAsync(string aBranch)
    {
        if (AppStateService.SelectedProject is not { } vProject || aBranch == objBranch)
        {
            return;
        }

        objPendingBranch = aBranch;
        objSwitchProblem = null;
        // The check for changes runs git and can take seconds on a slow disk: the label says at once which
        // branch is being switched to, so the click is never met with nothing (verify of 20 Nov 2026).
        objCheckingChanges = true;
        StateHasChanged();
        bool vDirty;
        try
        {
            vDirty = await SourceControl.HasUncommittedChangesAsync(vProject.ProjectId);
        }
        catch (Exception aException) when (aException is InvalidOperationException or IOException)
        {
            Toast.Error(aException.Message, "Could not switch branch");
            return;
        }
        finally
        {
            objCheckingChanges = false;
        }

        if (vDirty)
        {
            objDirtyOpen = true;
            return;
        }

        await SwitchAsync(BranchSwitchChanges.BringAlong);
    }

    private async Task SwitchAsync(BranchSwitchChanges aChanges)
    {
        if (AppStateService.SelectedProject is not { } vProject || objPendingBranch is not { } vBranch)
        {
            return;
        }

        objSwitching = true;
        try
        {
            await SourceControl.SwitchBranchAsync(vProject.ProjectId, vBranch, aChanges);
            objDirtyOpen = false;
            Toast.Success(aChanges == BranchSwitchChanges.PutAside ? $"Switched to {vBranch}. Your changes are in the stash." : $"Switched to {vBranch}.", "Branch");
            Workbench.NotifyBranchChanged();
        }
        catch (InvalidOperationException aException)
        {
            if (objDirtyOpen)
            {
                objSwitchProblem = aException.Message;
            }
            else
            {
                Toast.Error(aException.Message, "Could not switch branch");
            }
        }
        finally
        {
            objSwitching = false;
        }
    }

    private Task StayAsync()
    {
        objDirtyOpen = false;
        objPendingBranch = null;
        return Task.CompletedTask;
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

        if (!objNodes.TryGetValue(aPath, out var vNode) || vNode.IsFolder)
        {
            return;
        }

        try
        {
            var vContent = await Files.ReadAsync(vProject.ProjectId, aPath);
            Workbench.Open(aPath, vContent);
        }
        catch (Exception aException) when (aException is IOException or UnauthorizedAccessException)
        {
            Toast.Error(aException.Message, "Could not open the file");
        }
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
        Workbench.FilesViewChanged -= OnFilesViewChanged;
        Workbench.Changed -= OnWorkbenchChanged;
    }
}
