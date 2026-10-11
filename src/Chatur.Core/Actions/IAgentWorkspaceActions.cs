namespace Chatur.Core.Actions;

/// <summary>
/// One agent's workspace: what it may do, the commands it answers to, and the sessions it still has
/// open (page Agent workspace, <c>/workspace/{agent}</c>).
/// </summary>
public interface IAgentWorkspaceActions
{
    /// <summary>
    /// The workspace of one agent on a project. A right the agent lacks removes the action needing it
    /// from <see cref="AgentWorkspaceView.Commands"/> altogether; the right is listed once under
    /// <see cref="AgentWorkspaceView.Denied"/> (REQ-FN-058). Reading it changes no session, so the
    /// sessions an agent was left with are still open when the owner comes back (REQ-FN-060).
    /// </summary>
    /// <param name="aAgentCode">The role's code from the route; an unknown code falls back to the first agent.</param>
    /// <param name="aProjectId">The selected project.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<AgentWorkspaceView> WorkspaceAsync(string aAgentCode, int aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// Starts one of the agent's own commands: opens a session as that agent on the selected project
    /// and returns the message to send first (REQ-FN-059).
    /// </summary>
    /// <param name="aAgentCode">The agent whose command it is.</param>
    /// <param name="aProjectId">The project the command runs on.</param>
    /// <param name="aCommand">The command, with or without its leading asterisk.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <exception cref="InvalidOperationException">The agent has no such command, or lacks the right it needs.</exception>
    Task<WorkspaceCommandStart> StartCommandAsync(string aAgentCode, int aProjectId, string aCommand, CancellationToken aCt = default);
}
