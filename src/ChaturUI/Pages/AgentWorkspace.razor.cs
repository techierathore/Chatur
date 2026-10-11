using Chatur.Core;
using Chatur.Core.Actions;
using ChaturUI.Shell;
using Microsoft.AspNetCore.Components;
using TrBlazeUI.Components.Badge;

namespace ChaturUI.Pages;

/// <summary>
/// Agent workspace (a tab in the main window, mockups/agent-workspace.html): one agent's own view of
/// the selected project — what it may and may not do, the requirements waiting on it, its commands
/// and the sessions it still has open. An action the agent has no right to is not offered at all
/// (REQ-FN-058); moving to another agent reads a different workspace and closes nothing (REQ-FN-060).
/// </summary>
/// <remarks>Owning cluster: G (REQ-UI-053, REQ-UI-054); the backing rows are cluster H's.</remarks>
public partial class AgentWorkspace : IDisposable
{
    /// <summary>The right each action name is anchored by in the mockup (<c>right-read</c>, <c>denied-source</c> …).</summary>
    private static readonly Dictionary<string, string> RightAnchors = new()
    {
        ["read-file"] = "read",
        ["run-build"] = "build",
        ["run-tests"] = "tests",
        ["drive-screens"] = "screens",
        ["mark-verified"] = "verdicts",
        ["open-feedback"] = "feedback",
        ["log-miss"] = "miss",
        ["edit-file"] = "source",
        ["write-docs"] = "docs",
        ["run-source-control"] = "git",
        ["close-phase"] = "close",
        ["correct-wording"] = "wording"
    };

    private AgentWorkspaceView? objView;
    private IReadOnlyList<RequirementItem> objOpenWork = [];
    private string? objError;
    private bool objPickerOpen;
    private string? objBusyKey;
    private string? objLoadedFor;

    /// <summary>The agent code in the route; an unknown one falls back to the first agent.</summary>
    [Parameter]
    public string? Agent { get; set; }

    [Inject]
    private IAgentWorkspaceActions Workspace { get; set; } = default!;

    [Inject]
    private IRequirementBoardActions Board { get; set; } = default!;

    [Inject]
    private IAgentActions Sessions { get; set; } = default!;

    [Inject]
    private AppState AppStateService { get; set; } = default!;

    [Inject]
    private WorkbenchState WorkbenchTabs { get; set; } = default!;

    private int? ProjectId => AppStateService.SelectedProject?.ProjectId;

    private string ProjectName => AppStateService.SelectedProject?.Name ?? "the project";

    /// <inheritdoc />
    protected override void OnInitialized() => AppStateService.Changed += OnAppStateChanged;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync() => await LoadAsync();

    private void OnAppStateChanged() => InvokeAsync(async () =>
    {
        await LoadAsync();
        StateHasChanged();
    });

    /// <summary>Reads the workspace, then the work waiting for that agent, when the agent or the project has changed.</summary>
    private async Task LoadAsync()
    {
        var vKey = $"{Agent}|{ProjectId}";
        if (vKey == objLoadedFor)
        {
            return;
        }

        objLoadedFor = vKey;
        objError = null;
        objPickerOpen = false;
        if (ProjectId is not { } vProjectId)
        {
            objView = null;
            objOpenWork = [];
            return;
        }

        try
        {
            var vView = await Workspace.WorkspaceAsync(Agent ?? string.Empty, vProjectId);
            objOpenWork = await OpenWorkAsync(vView.Agent.Code);
            objView = vView;
        }
        catch (Exception aEx) when (aEx is InvalidOperationException or IOException or NotImplementedException or ArgumentException)
        {
            objView = null;
            objOpenWork = [];
            objError = $"The definition of the agent \"{Agent}\" cannot be read: {aEx.Message}";
        }
    }

    /// <summary>The requirements this agent owns that are not yet verified, from the project's own checklists (REQ-UI-054).</summary>
    private async Task<IReadOnlyList<RequirementItem>> OpenWorkAsync(string aAgentCode)
    {
        var vBoard = await Board.LoadAsync(new RequirementFilter(AgentCode: aAgentCode));
        return vBoard.Items
            .Where(i => i.BoardStatus is BoardStatuses.Planned or BoardStatuses.InProgress or BoardStatuses.Blocked)
            .ToList();
    }

    /// <summary>Moves the tab to another agent; the agent it leaves keeps every session open (REQ-FN-060).</summary>
    private void ChooseAgent(WorkspaceAgent aAgent)
    {
        objPickerOpen = false;
        WorkbenchTabs.SetWorkspaceAgent(aAgent.Code);
    }

    /// <summary>Brings a session this agent still has open into the conversation, where it was left.</summary>
    private void OpenSession(WorkspaceSession aSession) =>
        WorkbenchTabs.RequestConversation(new ConversationStart(aSession.SessionId, objView?.Agent.RoleId, null));

    /// <summary>
    /// Starts this agent on one requirement (mockup <c>start-REQ-…</c>): opens a session as the agent on
    /// the project and sends the first message in the conversation, so the reply streams there
    /// (REQ-UI-054).
    /// </summary>
    private async Task StartRequirementAsync(RequirementItem aItem)
    {
        if (objView is null || ProjectId is not { } vProjectId)
        {
            return;
        }

        objBusyKey = aItem.Id;
        try
        {
            var vSession = await Sessions.StartSessionAsync(vProjectId, objView.Agent.RoleId);
            WorkbenchTabs.RequestConversation(new ConversationStart(
                vSession.SessionId,
                objView.Agent.RoleId,
                $"Start on {aItem.Id}: {aItem.Title}. Read its acceptance line in the checklist and tell me what you will do first."));
            await ReloadSessionsAsync();
        }
        catch (Exception aEx) when (aEx is InvalidOperationException or NotImplementedException)
        {
            Toast.Error(aEx.Message);
        }
        finally
        {
            objBusyKey = null;
        }
    }

    /// <summary>
    /// Starts one of the agent's own commands on the selected project (mockup <c>run-…</c>): the session
    /// the action layer opens is shown in the conversation and the command is sent there as the first
    /// message (REQ-UI-053, REQ-FN-059).
    /// </summary>
    private async Task StartCommandAsync(WorkspaceCommand aCommand)
    {
        if (objView is null || ProjectId is not { } vProjectId)
        {
            return;
        }

        objBusyKey = aCommand.Command;
        try
        {
            var vStart = await Workspace.StartCommandAsync(objView.Agent.Code, vProjectId, aCommand.Command);
            WorkbenchTabs.RequestConversation(new ConversationStart(vStart.SessionId, vStart.RoleId, vStart.Message));
            await ReloadSessionsAsync();
        }
        catch (Exception aEx) when (aEx is InvalidOperationException or NotImplementedException)
        {
            Toast.Error(aEx.Message);
        }
        finally
        {
            objBusyKey = null;
        }
    }

    private async Task ReloadSessionsAsync()
    {
        if (objView is null || ProjectId is not { } vProjectId)
        {
            return;
        }

        objView = await Workspace.WorkspaceAsync(objView.Agent.Code, vProjectId);
    }

    private static string RightAnchor(string aAction) => RightAnchors.GetValueOrDefault(aAction, aAction);

    /// <summary>The status as the mockup words it: the board's word in lower case, or "re-check" for work awaiting a fresh verification.</summary>
    private static string StatusText(RequirementItem aItem) =>
        aItem.FileStatus.Contains("re-verify", StringComparison.OrdinalIgnoreCase)
            ? "re-check"
            : aItem.BoardStatus.ToLowerInvariant();

    private static BadgeVariant StatusVariant(RequirementItem aItem) =>
        aItem.BoardStatus switch
        {
            BoardStatuses.Blocked => BadgeVariant.Destructive,
            BoardStatuses.InProgress => BadgeVariant.Warning,
            _ => BadgeVariant.Secondary
        };

    private static string OpenLine(int aCount) => aCount == 1 ? "1 requirement" : $"{aCount} requirements";

    private IReadOnlyDictionary<string, object>? WorkRow(RequirementItem aItem) =>
        new Dictionary<string, object> { ["data-testid"] = $"work-{aItem.Id}" };

    /// <inheritdoc />
    public void Dispose() => AppStateService.Changed -= OnAppStateChanged;
}
