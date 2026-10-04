using Chatur.Core.Actions;
using TrBlazeUI.Components.Badge;

namespace ChaturUI.Pages;

/// <summary>
/// Repository (mockups/repository.html). Shows every changed file in the selected project's working
/// copy, the old and new text of the one the owner is on, checks the chosen files in, and pushes or
/// pulls (REQ-UI-042, REQ-UI-043, REQ-UI-044); lists the repository's branches and switches to the
/// one the owner picks (REQ-FN-047, cluster N). The run's own automatic check-in (REQ-FN-048) has no
/// control here — it runs by itself when an agent run finishes.
/// </summary>
public partial class Repository
{
    /// <summary>How many of the newest check-ins the history list shows.</summary>
    private const int HistoryCount = 20;

    // The mockup's hooks on the tables' own header rows and choose-all box (TrBlazeUI 2.1.0
    // HeaderRowAttributes / SelectAllAttributes, TR-011).
    private static readonly IReadOnlyDictionary<string, object> ChangesHeadAttributes = TestId("changes-head");
    private static readonly IReadOnlyDictionary<string, object> ChooseAllAttributes = TestId("choose-all");
    private static readonly IReadOnlyDictionary<string, object> HistoryHeadAttributes = TestId("history-head");
    private static readonly IReadOnlyDictionary<string, object> ProcessBranchesHeadAttributes = TestId("process-branches-head");

    private static IReadOnlyDictionary<string, object> TestId(string aId) =>
        new Dictionary<string, object> { ["data-testid"] = aId };

    private ProjectSummary? objProject;
    private IReadOnlyList<ChangedFile> objChanges = Array.Empty<ChangedFile>();
    private IReadOnlyCollection<ChangedFile> objChosen = Array.Empty<ChangedFile>();
    private IReadOnlyList<string> objBranches = Array.Empty<string>();
    private IReadOnlyList<CommitEntry> objHistory = Array.Empty<CommitEntry>();
    private IReadOnlyList<ProcessBranch> objProcessBranches = Array.Empty<ProcessBranch>();
    private SourceControlStatus? objStatus;
    private string objMainBranch = "main";
    private string? objCurrentDiffFile;
    private int objDiffRequest;
    private string objCurrentDiff = string.Empty;
    private string objCheckInMessage = string.Empty;
    private string objCheckInAuthorLine = string.Empty;
    private bool objIsLoading;
    private bool objIsBusy;

    /// <summary>Whether the check-in button may be pressed right now.</summary>
    private bool CanCheckIn =>
        objProject is not null && !objIsBusy && objChosen.Count > 0 && !string.IsNullOrWhiteSpace(objCheckInMessage);

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        objCheckInAuthorLine = await AuthorLineAsync();

        try
        {
            objProject = await ProjectActions.SelectedProjectAsync();
        }
        catch (NotImplementedException)
        {
            // Cluster F has not wired the selected project yet — show the honest empty state.
            objProject = null;
        }

        if (objProject is not null)
        {
            await LoadAsync();
        }
    }

    /// <summary>Reads the changes and the branch status for the selected project.</summary>
    private async Task LoadAsync()
    {
        if (objProject is null)
        {
            return;
        }

        objIsLoading = true;
        try
        {
            // The reads are independent and each one starts a git process (slow on a Windows drive),
            // so they run side by side and the page is complete after the slowest one, not the sum.
            var vProjectId = objProject.ProjectId;
            var vChangesTask = SourceControl.ChangesAsync(vProjectId);
            var vStatusTask = ReadStatusAsync(vProjectId);
            var vBranchesTask = ReadOrDefaultAsync(SourceControl.BranchesAsync(vProjectId), Array.Empty<string>());
            var vHistoryTask = LoadHistoryAsync();

            objChanges = await vChangesTask;
            objChosen = Array.Empty<ChangedFile>();
            objStatus = await vStatusTask;
            objBranches = await vBranchesTask;

            objMainBranch = objBranches.FirstOrDefault(aBranch => aBranch is "main" or "master") ?? "main";

            if (objChanges.Count > 0)
            {
                await ShowDiffAsync(objChanges[0].Path);
            }
            else
            {
                objCurrentDiffFile = null;
                objCurrentDiff = string.Empty;
            }

            await vHistoryTask;
        }
        catch (Exception aEx) when (aEx is not NotImplementedException)
        {
            ToastService.Error(aEx.Message, "Could not read the repository");
        }
        finally
        {
            objIsLoading = false;
        }
    }

    /// <summary>Reads the branch status; a status that is not wired yet gives none.</summary>
    private async Task<SourceControlStatus?> ReadStatusAsync(int aProjectId)
    {
        try
        {
            return await SourceControl.StatusAsync(aProjectId);
        }
        catch (NotImplementedException)
        {
            return null;
        }
    }

    /// <summary>Awaits a read that may not be wired yet; a missing implementation gives the default.</summary>
    private static async Task<T> ReadOrDefaultAsync<T>(Task<T> aRead, T aDefault)
    {
        try
        {
            return await aRead;
        }
        catch (NotImplementedException)
        {
            return aDefault;
        }
    }

    /// <summary>
    /// Reads the newest check-ins and the branches a process made (BRD §4 Repository "history";
    /// REQ-FN-048). A repository that cannot be read leaves both lists empty rather than failing the
    /// page — the changes above are what the owner came for.
    /// </summary>
    private async Task LoadHistoryAsync()
    {
        if (objProject is null)
        {
            return;
        }

        try
        {
            var vHistoryTask = SourceControl.HistoryAsync(objProject.ProjectId, HistoryCount);
            var vBranchesTask = SourceControl.ProcessBranchesAsync(objProject.ProjectId);
            objHistory = await vHistoryTask;
            objProcessBranches = await vBranchesTask;
        }
        catch (Exception aEx) when (aEx is InvalidOperationException or NotImplementedException)
        {
            objHistory = Array.Empty<CommitEntry>();
            objProcessBranches = Array.Empty<ProcessBranch>();
        }
    }

    /// <summary>The position of a check-in in the history list, counting from 1, for its row's test ids.</summary>
    /// <param name="aEntry">A check-in from <see cref="objHistory"/>.</param>
    private int NumberOf(CommitEntry aEntry)
    {
        for (var vIndex = 0; vIndex < objHistory.Count; vIndex++)
        {
            if (objHistory[vIndex] == aEntry)
            {
                return vIndex + 1;
            }
        }

        return 0;
    }

    /// <summary>Says that the merge of a process's branch is the owner's to do, never a model's.</summary>
    /// <param name="aBranch">The branch the owner asked about.</param>
    private void ExplainMerge(ProcessBranch aBranch) =>
        ToastService.Show($"Merging {aBranch.Name} stays with you: read it line by line, then merge it yourself.", "Merge");

    /// <summary>The position of a branch in the process branches list, counting from 1, for its row's test ids.</summary>
    /// <param name="aBranch">A branch from <see cref="objProcessBranches"/>.</param>
    private int NumberOf(ProcessBranch aBranch)
    {
        for (var vIndex = 0; vIndex < objProcessBranches.Count; vIndex++)
        {
            if (objProcessBranches[vIndex] == aBranch)
            {
                return vIndex + 1;
            }
        }

        return 0;
    }

    /// <summary>The requirement ids a branch carries: all of them when there are few, else the first and last of the range.</summary>
    /// <param name="aBranch">The branch to describe.</param>
    private static string RequirementRange(ProcessBranch aBranch) => aBranch.RequirementIds.Count switch
    {
        0 => "—",
        <= 3 => string.Join(", ", aBranch.RequirementIds),
        _ => $"{aBranch.RequirementIds[0]} … {aBranch.RequirementIds[^1]}"
    };

    /// <summary>"today, 09:14", "yesterday, 17:40", or the date, for a check-in's time.</summary>
    /// <param name="aWhenUtc">When the check-in was made.</param>
    private string FormatWhen(DateTime aWhenUtc)
    {
        var vWhen = aWhenUtc.ToLocalTime();
        var vToday = Clock.UtcNow.ToLocalTime().Date;
        return vWhen.Date == vToday
            ? $"today, {vWhen:HH:mm}"
            : vWhen.Date == vToday.AddDays(-1)
                ? $"yesterday, {vWhen:HH:mm}"
                : vWhen.ToString("d MMMM");
    }

    /// <summary>Shows the old and new text of one changed file (REQ-UI-042).</summary>
    /// <param name="aPath">The file's path relative to the repository root.</param>
    private async Task ShowDiffAsync(string aPath)
    {
        if (objProject is null)
        {
            return;
        }

        // Only the newest choice may fill the panel: an earlier, slower diff that finishes after a
        // later choice would otherwise leave the old file's text under the new file's name.
        var vRequest = ++objDiffRequest;
        objCurrentDiffFile = aPath;
        try
        {
            var vDiff = await SourceControl.DiffAsync(objProject.ProjectId, aPath);
            if (vRequest == objDiffRequest)
            {
                objCurrentDiff = vDiff;
            }
        }
        catch (Exception aEx)
        {
            if (vRequest == objDiffRequest)
            {
                objCurrentDiff = string.Empty;
                ToastService.Error(aEx.Message, "Could not read the difference");
            }
        }
    }

    /// <summary>Commits the chosen files with the typed message (REQ-UI-043).</summary>
    private async Task CheckInChosenAsync()
    {
        if (objProject is null || !CanCheckIn)
        {
            return;
        }

        objIsBusy = true;
        try
        {
            var vPaths = objChosen.Select(aChange => aChange.Path).ToList();
            await SourceControl.CheckInAsync(objProject.ProjectId, vPaths, objCheckInMessage);
            objCheckInMessage = string.Empty;
            ToastService.Success("The chosen files are checked in.", "Check in");
            await LoadAsync();
        }
        catch (Exception aEx)
        {
            ToastService.Error(aEx.Message, "Could not check in");
        }
        finally
        {
            objIsBusy = false;
        }
    }

    /// <summary>Switches the repository to another branch (REQ-FN-047).</summary>
    /// <param name="aBranchName">The branch to switch to, from <see cref="objBranches"/>.</param>
    private async Task SwitchBranchAsync(string aBranchName)
    {
        if (objProject is null || objIsBusy || aBranchName == objStatus?.Branch)
        {
            return;
        }

        objIsBusy = true;
        try
        {
            await SourceControl.SwitchBranchAsync(objProject.ProjectId, aBranchName);
            ToastService.Success($"Switched to {aBranchName}.", "Branch");
            await LoadAsync();
        }
        catch (Exception aEx)
        {
            ToastService.Error(aEx.Message, "Could not switch branch");
        }
        finally
        {
            objIsBusy = false;
        }
    }

    /// <summary>Pushes the current branch's check-ins (REQ-UI-044).</summary>
    private async Task PushChangesAsync()
    {
        if (objProject is null)
        {
            return;
        }

        objIsBusy = true;
        try
        {
            objStatus = await SourceControl.PushAsync(objProject.ProjectId);
            ToastService.Success("Pushed to the server.", "Push");
        }
        catch (Exception aEx)
        {
            ToastService.Error(aEx.Message, "Could not push");
        }
        finally
        {
            objIsBusy = false;
        }
    }

    /// <summary>Pulls what is new on the server (REQ-UI-044).</summary>
    private async Task PullChangesAsync()
    {
        if (objProject is null)
        {
            return;
        }

        objIsBusy = true;
        try
        {
            objStatus = await SourceControl.PullAsync(objProject.ProjectId);
            ToastService.Success("Pulled from the server.", "Pull");
            await LoadAsync();
        }
        catch (Exception aEx)
        {
            ToastService.Error(aEx.Message, "Could not pull");
        }
        finally
        {
            objIsBusy = false;
        }
    }

    /// <summary>The badge colour for a file's state.</summary>
    /// <param name="aState"><c>"modified"</c>, <c>"added"</c> or <c>"deleted"</c>.</param>
    private static BadgeVariant StateVariant(string aState) => aState switch
    {
        "added" => BadgeVariant.Success,
        "deleted" => BadgeVariant.Destructive,
        _ => BadgeVariant.Secondary,
    };

    /// <summary>"Name · today's date", or just the date while nobody is known to be signed in yet.</summary>
    private async Task<string> AuthorLineAsync()
    {
        var vToday = Clock.UtcNow.ToLocalTime().ToString("d MMMM yyyy");
        try
        {
            var vUser = await Accounts.CurrentUserAsync();
            return vUser is null ? vToday : $"{vUser.DisplayName} · {vToday}";
        }
        catch (NotImplementedException)
        {
            return vToday;
        }
    }
}
