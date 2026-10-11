using Chatur.Core.Actions;
using TrBlazeUI.Components.Badge;

namespace ChaturUI.Pages;

/// <summary>
/// Requirements board (mockups/board.html). Lists every requirement of the selected project with
/// its status and owning agent, opens one in a detail card, and reads the checklist files again
/// every time the page opens (REQ-UI-049, REQ-UI-050, REQ-UI-051, REQ-UI-052).
/// </summary>
public partial class Board
{
    /// <summary>The select value that stands for "do not narrow".</summary>
    private const string AllValue = "all";

    private RequirementBoard? objBoard;
    private RequirementItem? objOpened;
    private bool objIsLoading;

    private string PhaseFilter { get; set; } = AllValue;

    private string StatusFilter { get; set; } = AllValue;

    private string AgentFilter { get; set; } = AllValue;

    private string Search { get; set; } = string.Empty;

    /// <summary>The requirement shown in the detail card: the one opened, else the first listed.</summary>
    private RequirementItem? CurrentDetail
    {
        get
        {
            if (objBoard is null || objBoard.Items.Count == 0)
            {
                return null;
            }

            return objOpened is not null
                && objBoard.Items.FirstOrDefault(aItem => aItem.Id == objOpened.Id && aItem.ChecklistFile == objOpened.ChecklistFile) is { } vSame
                    ? vSame
                    : objBoard.Items[0];
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync() => await LoadAsync();

    private RequirementFilter BuildFilter() => new(
        Phase: int.TryParse(PhaseFilter, out var vPhase) ? vPhase : null,
        Status: StatusFilter == AllValue ? null : StatusFilter,
        AgentCode: AgentFilter == AllValue ? null : AgentFilter,
        Search: Search);

    private async Task LoadAsync()
    {
        objIsLoading = true;
        try
        {
            objBoard = await BoardActions.LoadAsync(BuildFilter());
        }
        finally
        {
            objIsLoading = false;
        }
    }

    /// <summary>Reads the checklist files again from disk (REQ-UI-052).</summary>
    private async Task ReloadAsync() => await LoadAsync();

    private async Task OnPhaseChangedAsync(string aValue)
    {
        PhaseFilter = aValue;
        await LoadAsync();
    }

    private async Task OnStatusChangedAsync(string aValue)
    {
        StatusFilter = aValue;
        await LoadAsync();
    }

    private async Task OnAgentChangedAsync(string aValue)
    {
        AgentFilter = aValue;
        await LoadAsync();
    }

    private async Task OnSearchChangedAsync(string? aValue)
    {
        Search = aValue ?? string.Empty;
        await LoadAsync();
    }

    private async Task ResetAsync()
    {
        PhaseFilter = AllValue;
        StatusFilter = AllValue;
        AgentFilter = AllValue;
        Search = string.Empty;
        await LoadAsync();
    }

    private void Open(RequirementItem aItem) => objOpened = aItem;

    /// <summary>Whether a screen's route is one of the phase-2 screens, which open as a tab rather than in the second window.</summary>
    /// <param name="aRoute">The route the checklist names for the screen.</param>
    private static bool IsTabScreen(string aRoute) =>
        aRoute is "/processes" or "/board" || aRoute.StartsWith("/workspace/", StringComparison.Ordinal);

    /// <summary>Opens the screen a requirement belongs to: a tab for a phase-2 screen.</summary>
    /// <param name="aRoute">The route the checklist names for the screen.</param>
    private void OpenScreen(string aRoute)
    {
        if (aRoute == "/processes")
        {
            WorkbenchTabs.OpenProcesses();
        }
        else if (aRoute == "/board")
        {
            WorkbenchTabs.OpenBoard();
        }
        else
        {
            var vAgent = aRoute["/workspace/".Length..];
            WorkbenchTabs.OpenAgentWorkspace(vAgent.Contains('{') ? null : vAgent);
        }
    }

    private async Task SetStatusAsync(RequirementItem aItem, string aStatus)
    {
        if (aStatus == aItem.BoardStatus || !BoardStatuses.All.Contains(aStatus))
        {
            return;
        }

        try
        {
            await BoardActions.SetStatusAsync(aItem.ChecklistFile, aItem.Id, aStatus);
            Toast.Success($"{aItem.Id} is now {aStatus}.");
        }
        catch (Exception vError) when (vError is ArgumentException or InvalidOperationException or IOException)
        {
            Toast.Error(vError.Message);
        }

        await LoadAsync();
    }

    private async Task CopyAsync(RequirementItem aItem)
    {
        var vText = string.IsNullOrWhiteSpace(aItem.Acceptance) ? aItem.Id : $"{aItem.Id} — {aItem.Acceptance}";
        if (await Browser.CopyTextAsync(vText))
        {
            Toast.Success($"Copied {aItem.Id}.");
        }
        else
        {
            Toast.Error("Could not copy to the clipboard.");
        }
    }

    /// <summary>The four statuses, plus the file's own word when it fits none (shown, not selectable).</summary>
    private static IEnumerable<string> StatusChoices(RequirementItem aItem) =>
        BoardStatuses.All.Contains(aItem.BoardStatus) ? BoardStatuses.All : BoardStatuses.All.Append(aItem.BoardStatus);

    private static BadgeVariant StatusVariant(string aStatus) => aStatus switch
    {
        BoardStatuses.Verified => BadgeVariant.Success,
        BoardStatuses.InProgress => BadgeVariant.Warning,
        BoardStatuses.Blocked => BadgeVariant.Destructive,
        _ => BadgeVariant.Secondary
    };

    private static string PhaseLabel(string aValue) => aValue == AllValue ? "Every phase" : $"Phase {aValue}";

    private string AgentLabel(string aValue) =>
        aValue == AllValue ? "Every agent" : BoardActions.Agents.FirstOrDefault(aAgent => aAgent.Code == aValue)?.Name ?? aValue;

    /// <summary>A <c>data-testid</c> for each requirement's row, so a check can find the row by id.</summary>
    private static IReadOnlyDictionary<string, object>? RowHooks(RequirementItem aItem) =>
        new Dictionary<string, object> { ["data-testid"] = $"req-row-{aItem.Id}", ["class"] = "border-t" };
}
