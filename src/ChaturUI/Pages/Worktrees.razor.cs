using System.Globalization;
using Chatur.Core.Actions;
using TrBlazeUI.Components.Badge;

namespace ChaturUI.Pages;

/// <summary>
/// Working copies (mockups/worktrees.html). Lists the project's working copies, adds one for a branch (REQ-FN-079),
/// starts a process in a chosen copy so a run's files land there and not in the owner's folder (REQ-FN-080), and
/// removes a copy with its folder (REQ-UI-068).
/// </summary>
public partial class Worktrees
{
    /// <summary>The Select's value for "make a new branch"; never a real branch name.</summary>
    private const string NewBranchChoice = "\u0001new-branch";

    private ProjectSummary? objProject;
    private IReadOnlyList<WorkingCopy> objCopies = Array.Empty<WorkingCopy>();
    private IReadOnlyList<ProcessSummary> objProcesses = Array.Empty<ProcessSummary>();
    private WorkingCopyChoices? objChoices;
    private long? objDiskBytes;
    private bool objIsLoading = true;
    private bool objIsBusy;
    private bool objAddOpen;
    private bool objRemoveOpen;
    private WorkingCopy? objRemoving;
    private string objBranch = string.Empty;
    private string objNewBranchName = string.Empty;
    private string objFolder = string.Empty;
    private bool objFolderEdited;
    private string? objAddError;
    private CancellationTokenSource objCts = new();

    private string Lead => objProject is null
        ? "No project is open."
        : $"{objCopies.Count} {(objCopies.Count == 1 ? "copy" : "copies")} of {objProject.Name} on this machine. One is yours; a run works in one of its own.";

    private string OwnerName => AppStateService.CurrentUser?.DisplayName is { Length: > 0 } vName ? vName : "You";

    private string OwnerInitials
    {
        get
        {
            var vParts = OwnerName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return vParts.Length switch
            {
                0 => "?",
                1 => vParts[0][..1].ToUpperInvariant(),
                _ => $"{vParts[0][0]}{vParts[^1][0]}".ToUpperInvariant()
            };
        }
    }

    private string DiskText => objDiskBytes is { } vBytes ? FormatBytes(vBytes) : "not read yet";

    private string OldestText => objCopies.Count == 0 ? "—" : objCopies.Min(c => c.MadeUtc).ToLocalTime().ToString("d MMM yyyy", CultureInfo.CurrentCulture);

    private int StaleCount => objCopies.Count(c => !c.IsMain && c.State != "in use" && c.MadeUtc < DateTime.UtcNow.AddDays(-7));

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        objProject = await ProjectActions.SelectedProjectAsync();
        await LoadAsync();
    }

    /// <summary>Stops the disk count when the screen closes.</summary>
    public void Dispose() => objCts.Cancel();

    private async Task LoadAsync()
    {
        if (objProject is null)
        {
            objIsLoading = false;
            return;
        }

        try
        {
            objCopies = await Worktree.ListWorkingCopiesAsync(objProject.ProjectId, objCts.Token);
            objProcesses = await Processes.ListAsync(objProject.ProjectId, objCts.Token);
        }
        catch (Exception aEx) when (aEx is InvalidOperationException)
        {
            objCopies = Array.Empty<WorkingCopy>();
        }
        finally
        {
            objIsLoading = false;
        }

        _ = CountDiskAsync(objCopies.Select(c => c.Path).ToList());
    }

    private async Task CountDiskAsync(IReadOnlyList<string> aFolders)
    {
        try
        {
            long vTotal = 0;
            foreach (var vFolder in aFolders)
            {
                vTotal += await Worktree.WorkingCopyBytesAsync(vFolder, objCts.Token);
            }

            objDiskBytes = vTotal;
            await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException)
        {
            // The screen closed before the count finished.
        }
    }

    private static string Key(WorkingCopy aCopy)
    {
        var vName = System.IO.Path.GetFileName(aCopy.Path.TrimEnd('/', '\\'));
        return aCopy.IsMain ? "main" : new string(vName.Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '-').ToArray());
    }

    private static BadgeVariant StateVariant(WorkingCopy aCopy) =>
        aCopy.State != "in use" ? BadgeVariant.Secondary : aCopy.IsMain ? BadgeVariant.Success : BadgeVariant.Warning;

    private static string FormatBytes(long aBytes) => aBytes switch
    {
        >= 1L << 30 => (aBytes / (double)(1L << 30)).ToString("0.0", CultureInfo.InvariantCulture) + " GB",
        >= 1L << 20 => (aBytes / (double)(1L << 20)).ToString("0.0", CultureInfo.InvariantCulture) + " MB",
        _ => Math.Max(1, aBytes / 1024).ToString(CultureInfo.InvariantCulture) + " KB"
    };

    private async Task OpenAddAsync()
    {
        if (objProject is null)
        {
            return;
        }

        objAddError = null;
        try
        {
            objChoices = await Worktree.WorkingCopyChoicesAsync(objProject.ProjectId);
        }
        catch (InvalidOperationException aEx)
        {
            ToastService.Error(aEx.Message, "Could not read the branches");
            return;
        }

        objBranch = objChoices.Branches.FirstOrDefault() ?? NewBranchChoice;
        objNewBranchName = string.Empty;
        objFolderEdited = false;
        objFolder = objChoices.FolderFor(objBranch == NewBranchChoice ? "new-copy" : objBranch);
        objAddOpen = true;
    }

    private void AddOpenChanged(bool aOpen) => objAddOpen = aOpen;

    private void FolderChanged(string? aFolder)
    {
        objFolder = aFolder ?? string.Empty;
        objFolderEdited = true;
    }

    private void CloseAdd() => objAddOpen = false;

    private void BranchChanged(string? aBranch)
    {
        objBranch = aBranch ?? string.Empty;
        if (!objFolderEdited && objChoices is not null && objBranch != NewBranchChoice)
        {
            objFolder = objChoices.FolderFor(objBranch);
        }
    }

    private void NewBranchNameChanged(string? aName)
    {
        objNewBranchName = aName ?? string.Empty;
        if (!objFolderEdited && objChoices is not null && objNewBranchName.Length > 0)
        {
            objFolder = objChoices.FolderFor(objNewBranchName);
        }
    }

    private async Task AddAsync()
    {
        if (objProject is null || objIsBusy)
        {
            return;
        }

        objIsBusy = true;
        objAddError = null;
        try
        {
            var vNew = objBranch == NewBranchChoice;
            var vAdded = await Worktree.AddWorkingCopyAsync(objProject.ProjectId, vNew ? objNewBranchName : objBranch, objFolder, vNew);
            objAddOpen = false;
            ToastService.Success($"A copy of {vAdded.Branch} is ready.", "Working copy added");
            await LoadAsync();
        }
        catch (Exception aEx) when (aEx is InvalidOperationException or ArgumentException)
        {
            objAddError = aEx.Message;
        }
        finally
        {
            objIsBusy = false;
        }
    }

    private void AskRemove(WorkingCopy aCopy)
    {
        if (aCopy.IsMain)
        {
            ToastService.Warning("This is the folder you are working in. It cannot be removed here.", "Not removed");
            return;
        }

        if (aCopy.State == "in use")
        {
            ToastService.Warning($"Run #{aCopy.RunId} is working in this copy. Stop the run first, then remove it.", "Not removed");
            return;
        }

        objRemoving = aCopy;
        objRemoveOpen = true;
    }

    private async Task RemoveAsync()
    {
        if (objProject is null || objRemoving is null)
        {
            return;
        }

        objIsBusy = true;
        try
        {
            await Worktree.RemoveWorkingCopyAsync(objProject.ProjectId, objRemoving.Path);
            ToastService.Success($"The copy of {objRemoving.Branch} and its folder are gone.", "Working copy removed");
        }
        catch (InvalidOperationException aEx)
        {
            ToastService.Error(aEx.Message, "Could not remove the copy");
        }
        finally
        {
            objRemoving = null;
            objIsBusy = false;
        }

        await LoadAsync();
    }

    /// <summary>Starts a process on the copy's own project row, so the run reads and writes only inside the copy, and opens the run.</summary>
    private async Task RunHereAsync(WorkingCopy aCopy, string aProcessCode)
    {
        if (objProject is null || objIsBusy)
        {
            return;
        }

        objIsBusy = true;
        try
        {
            var vCopyProject = await Worktree.WorkingCopyProjectAsync(objProject.ProjectId, aCopy.Path);
            var vStarted = await Processes.StartAsync(vCopyProject, aProcessCode);
            Nav.NavigateTo($"/processes/{vStarted.RunId}/run");
        }
        catch (Exception aEx) when (aEx is InvalidOperationException or ArgumentException)
        {
            ToastService.Error(aEx.Message, "Could not start the run");
            objIsBusy = false;
        }
    }

    private void OpenRun(int aRunId) => Nav.NavigateTo($"/processes/{aRunId}/run");
}
