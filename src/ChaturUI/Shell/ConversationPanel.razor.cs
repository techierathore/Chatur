using Chatur.Core;
using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Shell;

/// <summary>
/// The Workbench's conversation region: the sessions list (REQ-UI-031), the running activity panel
/// (REQ-UI-022, including a guard's refusal, REQ-UI-033), stopping the work (REQ-UI-023), the
/// session's mode (REQ-UI-024), the running token cost (REQ-UI-021) and the proposed change's diff
/// (REQ-UI-032). Sending a message and rendering the reply itself are cluster G's rows.
/// </summary>
/// <remarks>Owning cluster: G (REQ-FN-024 through REQ-FN-027, REQ-FN-031 through REQ-FN-036); H for the surrounding chrome.</remarks>
public partial class ConversationPanel : IDisposable
{
    /// <summary>One line of the conversation thread, growing in place while a reply streams in (REQ-FN-025, REQ-FN-026, REQ-FN-027).</summary>
    /// <param name="Role">Either <c>"user"</c> or <c>"assistant"</c>.</param>
    /// <param name="Text">The message's text so far.</param>
    /// <param name="ModelName">The model that produced it, once known.</param>
    /// <param name="Tokens">The tokens the finished reply used.</param>
    /// <param name="IsFinal">Whether the reply is done streaming.</param>
    /// <param name="At">When the owner sent it in this view; <see langword="null"/> for a message loaded from history, which records no time.</param>
    private sealed record DisplayMessage(string Role, string Text, string? ModelName, int? Tokens, bool IsFinal, DateTime? At = null);

    private IReadOnlyList<SessionSummary> objSessions = [];
    private IReadOnlyList<ActivityEvent> objActivity = [];
    private IReadOnlyList<ProposedChange> objProposedChanges = [];
    private IReadOnlyList<RoleSummary> objRoles = [];
    private IReadOnlyList<ModelSummary> objModels = [];
    private List<DisplayMessage> objMessages = [];
    private SessionSummary? objCurrentSession;
    private AgentSuggestion? objSuggestion;
    private int? objSelectedRoleId;
    private int? objSelectedModelId;
    private int? objLoadedForProjectId;
    private bool objHistoryOpen;
    private bool objAgentPickOpen;
    private bool objModelPickOpen;
    private bool objSending;
    private string objMessageText = string.Empty;
    private CancellationTokenSource? objActivityCts;
    private int? objTotalTokens;

    [Inject]
    private IAgentActions Agent { get; set; } = default!;

    [Inject]
    private IChangeActions Changes { get; set; } = default!;

    [Inject]
    private IRoleActions Roles { get; set; } = default!;

    [Inject]
    private IProviderActions Providers { get; set; } = default!;

    [Inject]
    private IFileActions Files { get; set; } = default!;

    [Inject]
    private WorkbenchState Workbench { get; set; } = default!;

    [Inject]
    private AppState AppStateService { get; set; } = default!;

    /// <summary>The model the owner chose, or <see langword="null"/> when the routing chooses.</summary>
    private ModelSummary? SelectedModel => objModels.FirstOrDefault(m => m.ModelId == objSelectedModelId);

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        AppStateService.Changed += OnAppStateChanged;
        await EnsureLoadedAsync();
    }

    /// <summary>The owner's initials for the message avatar, from the signed-in account's name (the mockup's "TR").</summary>
    private string OwnerInitials
    {
        get
        {
            var vParts = (AppStateService.CurrentUser?.DisplayName ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return vParts.Length switch
            {
                0 => "?",
                1 => vParts[0][..1].ToUpperInvariant(),
                _ => (vParts[0][..1] + vParts[^1][..1]).ToUpperInvariant()
            };
        }
    }

    private int? ProjectId => AppStateService.SelectedProject?.ProjectId;

    private async Task EnsureLoadedAsync()
    {
        if (ProjectId == objLoadedForProjectId)
        {
            return;
        }

        objLoadedForProjectId = ProjectId;
        objActivityCts?.Cancel();
        objActivity = [];
        objProposedChanges = [];
        objMessages = [];
        objCurrentSession = null;
        objSessions = [];
        objSuggestion = null;
        Workbench.SetPendingChanges([]);

        if (ProjectId is not { } vProjectId)
        {
            return;
        }

        objRoles = await Roles.RolesAsync();
        await LoadModelsAsync();
        await ReloadSessionsAsync(vProjectId);
    }

    /// <summary>
    /// Reloads the session list for a project; when the project has never had a session, opens one
    /// straight away with the suggested agent, so the conversation is already there with no further
    /// step to reach it (REQ-FN-008), then watches its activity (REQ-UI-031, REQ-UI-022).
    /// </summary>
    private async Task ReloadSessionsAsync(int aProjectId)
    {
        objSessions = await Agent.SessionsAsync(aProjectId);
        var vCurrentSession = objSessions.FirstOrDefault();

        var vSuggestion = await Agent.SuggestAgentAsync(aProjectId);
        objSuggestion = vSuggestion;

        if (vCurrentSession is null)
        {
            vCurrentSession = await Agent.EnsureActiveSessionAsync(aProjectId);
            objSessions = await Agent.SessionsAsync(aProjectId);
        }

        objCurrentSession = vCurrentSession;
        objSelectedRoleId = objRoles.FirstOrDefault(r => r.Name == vCurrentSession.RoleName)?.RoleId ?? vSuggestion.RoleId;

        await LoadMessagesAsync(vCurrentSession.SessionId);
        await LoadProposedChangesAsync(vCurrentSession.SessionId);
        await LoadTotalTokensAsync(vCurrentSession.SessionId);
        WatchActivity(vCurrentSession.SessionId);
    }

    /// <summary>Loads every past message of a session so a continued conversation shows exactly what was said (REQ-FN-031, REQ-FN-032).</summary>
    private async Task LoadMessagesAsync(int aSessionId)
    {
        var vDetail = await Agent.ContinueSessionAsync(aSessionId);
        objMessages = vDetail.Events
            .Where(e => e.Kind is "user" or "assistant")
            .Select(e => new DisplayMessage(e.Kind, e.Payload, e.Model, e.Tokens, true))
            .ToList();
    }

    /// <summary>
    /// Sums the tokens every recorded step of the session used so far (REQ-UI-021). Reads the full
    /// session detail rather than the activity stream, since only the recorded events carry a token
    /// count; shows nothing rather than a guess while that read is not built yet.
    /// </summary>
    private async Task LoadTotalTokensAsync(int aSessionId)
    {
        try
        {
            var vDetail = await Agent.ContinueSessionAsync(aSessionId);
            objTotalTokens = vDetail.Events.Sum(e => e.Tokens ?? 0);
        }
        catch (NotImplementedException)
        {
            objTotalTokens = null;
        }
    }

    private async Task LoadProposedChangesAsync(int aSessionId)
    {
        var vAll = await Changes.ProposedAsync(aSessionId);
        objProposedChanges = vAll.Where(c => c.Status == "Proposed").ToList();

        // A file with a change waiting on it is read-only in the file view until it is settled.
        Workbench.SetPendingChanges(objProposedChanges.Select(c => c.FilePath));
    }

    /// <summary>Reads the models the connected providers serve, for the model picker; none is offered while providers are not wired.</summary>
    private async Task LoadModelsAsync()
    {
        try
        {
            objModels = await Providers.ListModelsAsync();
        }
        catch (NotImplementedException)
        {
            objModels = [];
        }

        // A model removed since it was chosen falls back to the routing's own choice.
        if (objSelectedModelId is { } vChosen && objModels.All(m => m.ModelId != vChosen))
        {
            objSelectedModelId = null;
        }
    }

    /// <summary>Re-reads the models each time the picker opens, so a provider added a moment ago is offered at once.</summary>
    private async Task OnModelPickOpenChangedAsync(bool aOpen)
    {
        objModelPickOpen = aOpen;
        if (aOpen)
        {
            await LoadModelsAsync();
        }
    }

    /// <summary>Chooses the model that answers the next message, or hands the choice back to the routing (BRD-46).</summary>
    private void SelectModel(int? aModelId)
    {
        objSelectedModelId = aModelId;
        objModelPickOpen = false;
    }

    /// <summary>
    /// Opens the file a proposed change is for in the file view beside the conversation. A file the
    /// change is about to create has nothing on disk yet, so its tab opens empty.
    /// </summary>
    private async Task OpenChangeAsync(ProposedChange aChange)
    {
        if (ProjectId is not { } vProjectId)
        {
            return;
        }

        string vContent;
        try
        {
            vContent = await Files.ReadAsync(vProjectId, aChange.FilePath);
        }
        catch (IOException)
        {
            vContent = aChange.Before;
        }

        Workbench.Open(aChange.FilePath, vContent);
    }

    private void WatchActivity(int aSessionId)
    {
        objActivityCts?.Cancel();
        objActivityCts = new CancellationTokenSource();
        objActivity = [];
        _ = ConsumeActivityAsync(aSessionId, objActivityCts.Token);
    }

    private async Task ConsumeActivityAsync(int aSessionId, CancellationToken aCt)
    {
        var vEvents = new List<ActivityEvent>();
        try
        {
            await foreach (var vEvent in Agent.ActivityAsync(aSessionId, aCt))
            {
                vEvents.Add(vEvent);
                objActivity = vEvents.ToList();

                // A proposed change and a token count usually land in the same turn as its activity
                // step, so the change card, the running total and the activity line update together
                // (REQ-UI-021, REQ-UI-022, REQ-UI-032).
                await LoadProposedChangesAsync(aSessionId);
                await LoadTotalTokensAsync(aSessionId);
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the project or session changes.
        }
    }

    /// <summary>
    /// Starts a new conversation with the role chosen in the composer; the one before stays in the
    /// history (mockups/main.html <c>new-chat</c>, REQ-UI-031).
    /// </summary>
    private async Task NewChatAsync()
    {
        if (ProjectId is not { } vProjectId || objSending)
        {
            return;
        }

        await Agent.StartSessionAsync(vProjectId, objSelectedRoleId);
        await ReloadSessionsAsync(vProjectId);
    }

    /// <summary>Re-reads the session list each time the history popover opens, so it never shows a stale list (REQ-UI-031).</summary>
    private async Task OnHistoryOpenChangedAsync(bool aOpen)
    {
        objHistoryOpen = aOpen;
        if (aOpen && ProjectId is { } vProjectId)
        {
            objSessions = await Agent.SessionsAsync(vProjectId);
        }
    }

    /// <summary>Opens a session from the history list (REQ-UI-031's own list of what has been worked on).</summary>
    private void SelectSession(SessionSummary aSession)
    {
        objCurrentSession = aSession;
        objHistoryOpen = false;
        objSelectedRoleId = objRoles.FirstOrDefault(r => r.Name == aSession.RoleName)?.RoleId;
        _ = LoadMessagesAsync(aSession.SessionId);
        _ = LoadProposedChangesAsync(aSession.SessionId);
        _ = LoadTotalTokensAsync(aSession.SessionId);
        WatchActivity(aSession.SessionId);
    }

    /// <summary>Picks a role to talk to instead of the one Chatur suggested (REQ-FN-024).</summary>
    private void SelectRole(int? aRoleId)
    {
        objSelectedRoleId = aRoleId;
        objAgentPickOpen = false;
    }

    /// <summary>
    /// Sends the typed message and streams the reply into the thread as it arrives (REQ-FN-025,
    /// REQ-FN-026, REQ-FN-027): a placeholder assistant line is added first and its text grows with
    /// every <see cref="AgentReplyChunk"/>, so the conversation visibly grows before the reply is
    /// finished.
    /// </summary>
    private async Task SendMessageAsync()
    {
        if (objCurrentSession is not { } vSession || objSending || string.IsNullOrWhiteSpace(objMessageText))
        {
            return;
        }

        var vText = objMessageText.Trim();
        objMessageText = string.Empty;
        objSending = true;

        objMessages.Add(new DisplayMessage("user", vText, null, null, true, DateTime.Now));
        var vAssistantIndex = objMessages.Count;
        objMessages.Add(new DisplayMessage("assistant", string.Empty, null, null, false));
        StateHasChanged();

        try
        {
            await foreach (var vChunk in Agent.SendAsync(vSession.SessionId, vText, objSelectedRoleId, objSelectedModelId, vSession.Mode))
            {
                var vSoFar = objMessages[vAssistantIndex];
                objMessages[vAssistantIndex] = vSoFar with
                {
                    Text = vSoFar.Text + vChunk.TextDelta,
                    ModelName = vChunk.ModelName ?? vSoFar.ModelName,
                    Tokens = vChunk.TokensUsed ?? vSoFar.Tokens,
                    IsFinal = vChunk.IsFinal
                };
                await InvokeAsync(StateHasChanged);
            }
        }
        finally
        {
            objSending = false;
        }

        await LoadProposedChangesAsync(vSession.SessionId);
        await LoadTotalTokensAsync(vSession.SessionId);

        // A role picked in the composer is now the session's role (AgentActions.SendAsync), so the
        // replies are labelled with it.
        if (objSelectedRoleId is { } vPickedRoleId
            && objRoles.FirstOrDefault(r => r.RoleId == vPickedRoleId) is { } vPickedRole
            && objCurrentSession is { } vCurrent
            && vCurrent.RoleName != vPickedRole.Name)
        {
            objCurrentSession = vCurrent with { RoleName = vPickedRole.Name };
        }

        // The reason beside the box describes the conversation as it now stands (REQ-FN-024): after the
        // first turn it is no longer "no session has been started".
        if (ProjectId is { } vProjectId)
        {
            objSuggestion = await Agent.SuggestAgentAsync(vProjectId);
        }
    }

    /// <summary>Stops the agent's current work (REQ-UI-023).</summary>
    private async Task StopAsync()
    {
        if (objCurrentSession is { } vSession)
        {
            await Agent.StopAsync(vSession.SessionId);
        }
    }

    /// <summary>Sets the session's mode — ask first, or go ahead (REQ-UI-024).</summary>
    private async Task SetModeAsync(SessionMode aMode)
    {
        if (objCurrentSession is not { } vSession || vSession.Mode == aMode)
        {
            return;
        }

        await Agent.SetModeAsync(vSession.SessionId, aMode);
        objCurrentSession = vSession with { Mode = aMode };
    }

    /// <summary>Approves a proposed change (REQ-FN-033, wired here since the card lives in this region).</summary>
    private async Task ApproveAsync(ProposedChange aChange)
    {
        await Changes.ApproveAsync(aChange.ChangeId);

        // The file on disk now holds the new text; a tab already showing it must show that too.
        if (ProjectId is { } vProjectId)
        {
            try
            {
                Workbench.Reload(aChange.FilePath, await Files.ReadAsync(vProjectId, aChange.FilePath));
            }
            catch (IOException)
            {
                // Nothing on disk to show; the tab keeps what it had.
            }
        }

        if (objCurrentSession is { } vSession)
        {
            await LoadProposedChangesAsync(vSession.SessionId);
        }
    }

    /// <summary>Rejects a proposed change (REQ-FN-034, wired here since the card lives in this region).</summary>
    private async Task RejectAsync(ProposedChange aChange)
    {
        await Changes.RejectAsync(aChange.ChangeId);
        if (objCurrentSession is { } vSession)
        {
            await LoadProposedChangesAsync(vSession.SessionId);
        }
    }

    private async void OnAppStateChanged()
    {
        await InvokeAsync(async () =>
        {
            await EnsureLoadedAsync();
            StateHasChanged();
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        AppStateService.Changed -= OnAppStateChanged;
        objActivityCts?.Cancel();
    }
}
