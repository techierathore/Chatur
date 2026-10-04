namespace Chatur.Core.Actions;

/// <summary>
/// Running a session with an agent: choosing who answers, sending and streaming replies, watching
/// activity, and continuing an earlier session (Architecture §7 "Agent loop"; page Workbench's
/// conversation).
/// </summary>
public interface IAgentActions
{
    /// <summary>
    /// The agent Chatur pre-selects for a project's current state, and why (REQ-FN-024).
    /// </summary>
    /// <param name="aProjectId">The project to suggest an agent for.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<AgentSuggestion> SuggestAgentAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// The conversation Workbench opens on for a project, with no further step to reach it
    /// (REQ-FN-008): the project's own open session when it has one, or a new one started with
    /// <see cref="SuggestAgentAsync"/>'s own choice of role otherwise.
    /// </summary>
    /// <param name="aProjectId">The project a Workbench window just opened on.</param>
    /// <param name="aCt">A token that cancels the read or the creation.</param>
    Task<SessionSummary> EnsureActiveSessionAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// Starts a new conversation on a project; the one before stays in the history (mockups/main.html
    /// <c>new-chat</c>: "Starts a new conversation. This one is kept in the history.", REQ-UI-031).
    /// </summary>
    /// <param name="aProjectId">The project the conversation is about.</param>
    /// <param name="aRoleId">The role to talk to, or <see langword="null"/> for <see cref="SuggestAgentAsync"/>'s choice.</param>
    /// <param name="aCt">A token that cancels the creation.</param>
    Task<SessionSummary> StartSessionAsync(int aProjectId, int? aRoleId, CancellationToken aCt = default);

    /// <summary>
    /// Sends a message and streams the reply as it is written (REQ-FN-025, REQ-FN-026, REQ-FN-027).
    /// </summary>
    /// <param name="aSessionId">The session to send the message in.</param>
    /// <param name="aMessage">The message text.</param>
    /// <param name="aAgentRoleId">The chosen agent, or <see langword="null"/> for the model alone.</param>
    /// <param name="aModelId">The chosen model, or <see langword="null"/> to use routing.</param>
    /// <param name="aMode">Ask first, or go ahead.</param>
    /// <param name="aCt">A token that stops the reply early (REQ-UI-023).</param>
    IAsyncEnumerable<AgentReplyChunk> SendAsync(
        int aSessionId,
        string aMessage,
        int? aAgentRoleId,
        int? aModelId,
        SessionMode aMode,
        CancellationToken aCt = default);

    /// <summary>
    /// Stops the agent's current work; no further step is taken (REQ-UI-023).
    /// </summary>
    /// <param name="aSessionId">The session to stop.</param>
    /// <param name="aCt">A token that cancels the stop request itself.</param>
    Task StopAsync(int aSessionId, CancellationToken aCt = default);

    /// <summary>Every session worked on for a project (REQ-UI-031).</summary>
    /// <param name="aProjectId">The project to list sessions for.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<SessionSummary>> SessionsAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// Reopens a session with its full recorded history (REQ-FN-031, REQ-FN-032).
    /// </summary>
    /// <param name="aSessionId">The session to continue.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<SessionDetail> ContinueSessionAsync(int aSessionId, CancellationToken aCt = default);

    /// <summary>
    /// The agent's steps as they happen, for the activity panel (REQ-UI-022).
    /// </summary>
    /// <param name="aSessionId">The session to watch.</param>
    /// <param name="aCt">A token that stops the stream.</param>
    IAsyncEnumerable<ActivityEvent> ActivityAsync(int aSessionId, CancellationToken aCt = default);

    /// <summary>
    /// Sets a session's mode (REQ-UI-024).
    /// </summary>
    /// <param name="aSessionId">The session to set the mode of.</param>
    /// <param name="aMode">Ask first, or go ahead.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    Task SetModeAsync(int aSessionId, SessionMode aMode, CancellationToken aCt = default);
}
