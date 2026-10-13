using Chatur.Core;
using Chatur.Core.Actions;
using Chatur.Core.Platform;
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
    /// <param name="Seq">The message's order within the session; <see langword="null"/> for one added in this view until the session is read again.</param>
    private sealed record DisplayMessage(string Role, string Text, string? ModelName, int? Tokens, bool IsFinal, DateTime? At = null, int? Seq = null);

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
    private HandedSession? objFollowed;
    private SessionSummary? objResumeAfterFollow;
    private CancellationTokenSource? objFollowCts;
    private bool objShown = true;

    /// <summary>Opens Settings, Prerequisites or Repository in the secondary window (REQ-FN-106).</summary>
    [Inject]
    private ISecondaryWindow SecondaryWindow { get; set; } = default!;

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
        Workbench.ConversationStartRequested += OnConversationStartRequested;
        Workbench.HandedSessionChanged += OnHandedSessionChanged;
        Workbench.Changed += OnWorkbenchChanged;
        objShown = Workbench.ConversationVisible;
        await EnsureLoadedAsync();
        if (Workbench.Handed is not null)
        {
            await ShowHandedAsync();
        }
    }

    /// <summary>Repaints when View ▸ Chat or the close button shows or hides the conversation (REQ-UI-074); other tab changes do not touch it.</summary>
    private void OnWorkbenchChanged()
    {
        if (objShown == Workbench.ConversationVisible)
        {
            return;
        }

        objShown = Workbench.ConversationVisible;
        _ = InvokeAsync(StateHasChanged);
    }

    private void OnHandedSessionChanged() => _ = InvokeAsync(ShowHandedAsync);

    /// <summary>
    /// Follows the agent session a process run handed a step to (REQ-UI-071): the thread switches to that session
    /// under the agent's name and re-reads it every second, so its messages and tool calls appear as they happen.
    /// When the run ends the step, or the Process run view is left, the thread returns to the owner's own session.
    /// </summary>
    private async Task ShowHandedAsync()
    {
        var vHanded = Workbench.Handed;
        if (vHanded is null)
        {
            objFollowCts?.Cancel();
            if (objFollowed is not null)
            {
                await ReturnToOwnerSessionAsync();
            }

            return;
        }

        // The owner's own session is remembered so it is back when the view that showed the step ends.
        if (objFollowed is null)
        {
            objResumeAfterFollow = objCurrentSession;
        }

        try
        {
            var vDetail = await Agent.ContinueSessionAsync(vHanded.SessionId);
            objFollowed = vHanded;
            objCurrentSession = vDetail.Summary;
            objSelectedRoleId = objRoles.FirstOrDefault(r => r.Name == vDetail.Summary.RoleName)?.RoleId;
            objHistoryOpen = false;
            await LoadMessagesAsync(vHanded.SessionId);
            await LoadProposedChangesAsync(vHanded.SessionId);
            await LoadTotalTokensAsync(vHanded.SessionId);
            WatchActivity(vHanded.SessionId);
            objFollowCts?.Cancel();
            objFollowCts = new CancellationTokenSource();
            _ = FollowHandedAsync(vHanded.SessionId, objFollowCts.Token);
            await InvokeAsync(StateHasChanged);
        }
        catch (InvalidOperationException)
        {
            // The session is not readable yet; the next change of the handed step announces it again.
        }
    }

    /// <summary>Leaves the handed step's session and shows the session the owner had before it (or the newest of theirs).</summary>
    private async Task ReturnToOwnerSessionAsync()
    {
        var vResume = objResumeAfterFollow;
        objFollowed = null;
        objResumeAfterFollow = null;
        if (ProjectId is not { } vProjectId)
        {
            return;
        }

        if (vResume is not null && vResume.ProjectId == vProjectId)
        {
            objSessions = await Agent.SessionsAsync(vProjectId);
            objCurrentSession = objSessions.FirstOrDefault(s => s.SessionId == vResume.SessionId) ?? vResume;
            objSelectedRoleId = objRoles.FirstOrDefault(r => r.Name == objCurrentSession.RoleName)?.RoleId;
            await LoadMessagesAsync(objCurrentSession.SessionId);
            await LoadProposedChangesAsync(objCurrentSession.SessionId);
            await LoadTotalTokensAsync(objCurrentSession.SessionId);
            WatchActivity(objCurrentSession.SessionId);
        }
        else
        {
            await ReloadSessionsAsync(vProjectId);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task FollowHandedAsync(int aSessionId, CancellationToken aCt)
    {
        try
        {
            while (!aCt.IsCancellationRequested && objCurrentSession?.SessionId == aSessionId)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), aCt);
                if (objSending)
                {
                    continue;
                }

                await LoadMessagesAsync(aSessionId);
                await LoadTotalTokensAsync(aSessionId);
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
            // Following ended with the step, or the conversation moved to another session.
        }
        catch (InvalidOperationException)
        {
            // A read that races the agent's write is simply not repeated; the step's end reads once more.
        }
    }

    private void OnConversationStartRequested(ConversationStart aStart) => _ = InvokeAsync(() => OpenAndSendAsync(aStart));

    /// <summary>
    /// Opens a session another screen asked for — the Agent workspace's Start, a command button or a
    /// session it still has open — and, when it came with a first message, sends it here, so the reply
    /// streams into this conversation (REQ-UI-053, REQ-UI-054, REQ-FN-059, REQ-FN-060).
    /// </summary>
    /// <param name="aStart">The session to open and what to send first.</param>
    private async Task OpenAndSendAsync(ConversationStart aStart)
    {
        if (ProjectId is not { } vProjectId)
        {
            return;
        }

        objSessions = await Agent.SessionsAsync(vProjectId);
        if (objSessions.FirstOrDefault(s => s.SessionId == aStart.SessionId) is not { } vSession)
        {
            return;
        }

        // A reply still streaming in the session on screen finishes first; the new message is not dropped.
        for (var vWait = 0; objSending && aStart.Message is not null && vWait < 480; vWait++)
        {
            await Task.Delay(250);
        }

        objCurrentSession = vSession;
        objHistoryOpen = false;
        objSelectedRoleId = aStart.RoleId ?? objRoles.FirstOrDefault(r => r.Name == vSession.RoleName)?.RoleId;
        await LoadMessagesAsync(vSession.SessionId);
        await LoadProposedChangesAsync(vSession.SessionId);
        await LoadTotalTokensAsync(vSession.SessionId);
        WatchActivity(vSession.SessionId);
        StateHasChanged();

        if (aStart.Message is { } vMessage)
        {
            objMessageText = vMessage;
            await SendMessageAsync();
        }
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
        objFollowed = null;
        objResumeAfterFollow = null;
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
            .Select(e => new DisplayMessage(e.Kind, e.Payload, e.Model, e.Tokens, true, null, e.Seq))
            .ToList();

        // A handed step's agent is still writing while the run holds the step: its last line keeps the spinner.
        if (Workbench.Handed?.SessionId == aSessionId && objMessages.FindLastIndex(m => m.Role != "user") is var vLast and >= 0)
        {
            objMessages[vLast] = objMessages[vLast] with { IsFinal = false };
        }
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

    /// <summary>
    /// Works out which message each activity step is drawn under, so a step shows where it happened and
    /// not under whichever reply is newest (REQ-UI-022, REQ-UI-023). A step the agent took (a read, an
    /// edit, a refusal) belongs to the reply that followed it; a stop belongs to the message it came
    /// after, so it never moves to a later reply. A step with no such message waits at the thread's end.
    /// </summary>
    /// <returns>The steps for each message index, and those that belong to no message.</returns>
    private (Dictionary<int, List<ActivityEvent>> ByMessage, List<ActivityEvent> Unplaced) PlaceActivity()
    {
        var vByMessage = new Dictionary<int, List<ActivityEvent>>();
        var vUnplaced = new List<ActivityEvent>();
        var vHasUnsequenced = objMessages.Any(m => m.Seq is null);

        foreach (var vEvent in objActivity)
        {
            var vIndex = -1;
            if (vHasUnsequenced || vEvent.Seq == 0)
            {
                // A reply still being written (or an event with no order): the newest reply is where the work is.
                vIndex = objMessages.FindLastIndex(m => m.Role != "user");
            }
            else if (vEvent.Kind == "stopped")
            {
                vIndex = objMessages.FindLastIndex(m => m.Seq < vEvent.Seq);
            }
            else
            {
                vIndex = objMessages.FindIndex(m => m.Role != "user" && m.Seq > vEvent.Seq);
                if (vIndex < 0)
                {
                    vIndex = objMessages.FindLastIndex(m => m.Seq < vEvent.Seq);
                }
            }

            if (vIndex < 0)
            {
                vUnplaced.Add(vEvent);
            }
            else
            {
                if (!vByMessage.TryGetValue(vIndex, out var vList))
                {
                    vByMessage[vIndex] = vList = [];
                }

                vList.Add(vEvent);
            }
        }

        return (vByMessage, vUnplaced);
    }

    /// <summary>
    /// Gives the messages added in this view their place in the session once they are stored, so the
    /// steps that follow are drawn in order (REQ-UI-023). The stored messages are the same ones, in
    /// the same order, so the newest of them take the numbers.
    /// </summary>
    private async Task ResolveLiveSeqsAsync(int aSessionId)
    {
        var vLive = objMessages.Count(m => m.Seq is null);
        if (vLive == 0 || objSending)
        {
            return;
        }

        var vDetail = await Agent.ContinueSessionAsync(aSessionId);
        var vStored = vDetail.Events.Where(e => e.Kind is "user" or "assistant").ToList();
        if (vStored.Count < objMessages.Count)
        {
            return;
        }

        var vOffset = vStored.Count - objMessages.Count;
        for (var vIndex = 0; vIndex < objMessages.Count; vIndex++)
        {
            if (objMessages[vIndex].Seq is null && vStored[vIndex + vOffset].Kind == objMessages[vIndex].Role)
            {
                objMessages[vIndex] = objMessages[vIndex] with { Seq = vStored[vIndex + vOffset].Seq };
            }
        }
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
                await ResolveLiveSeqsAsync(aSessionId);

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
        await ResolveLiveSeqsAsync(vSession.SessionId);

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
        Workbench.ConversationStartRequested -= OnConversationStartRequested;
        Workbench.HandedSessionChanged -= OnHandedSessionChanged;
        Workbench.Changed -= OnWorkbenchChanged;
        objFollowCts?.Cancel();
        objActivityCts?.Cancel();
    }
}
