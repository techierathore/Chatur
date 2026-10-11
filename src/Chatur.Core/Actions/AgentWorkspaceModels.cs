namespace Chatur.Core.Actions;

/// <summary>One agent in the workspace's picker, with its open-session count (REQ-FN-060).</summary>
/// <param name="RoleId">The role's identity.</param>
/// <param name="Code">The role's short code, the <c>{agent}</c> in the route, e.g. <c>"verifier"</c>.</param>
/// <param name="Name">The role's display name.</param>
/// <param name="OpenSessions">How many sessions this agent has open on the project.</param>
public sealed record WorkspaceAgent(int RoleId, string Code, string Name, int OpenSessions);

/// <summary>One thing an agent may do (REQ-FN-058).</summary>
/// <param name="Action">The right's action name, e.g. <c>"run-build"</c>.</param>
/// <param name="Label">What it reads as on the screen.</param>
public sealed record WorkspaceRight(string Action, string Label);

/// <summary>One thing an agent may not do, shown once so the owner knows where it went (REQ-FN-058).</summary>
/// <param name="Action">The right's action name.</param>
/// <param name="Label">What it reads as on the screen, with where it belongs instead.</param>
public sealed record WorkspaceDeniedRight(string Action, string Label);

/// <summary>One command an agent answers to, offered only when its right is held (REQ-FN-058, REQ-FN-059).</summary>
/// <param name="Command">The command's name without its leading asterisk, e.g. <c>"verify-phase"</c>.</param>
/// <param name="Description">One line of what the command does.</param>
public sealed record WorkspaceCommand(string Command, string Description);

/// <summary>A session an agent still has open, with the place it was left at (REQ-FN-060).</summary>
/// <param name="SessionId">The session's identity.</param>
/// <param name="Title">The first thing said in it, shortened.</param>
/// <param name="StartedUtc">When it was opened.</param>
/// <param name="MessageCount">How many messages it holds.</param>
/// <param name="TokensUsed">The tokens its replies used.</param>
/// <param name="State">The session's state; any state but <c>Stopped</c> is open.</param>
public sealed record WorkspaceSession(int SessionId, string Title, DateTime StartedUtc, int MessageCount, int TokensUsed, string State);

/// <summary>Everything one agent's workspace shows on one project (REQ-FN-058, REQ-FN-060).</summary>
/// <param name="Agent">The agent the workspace belongs to.</param>
/// <param name="Agents">Every agent, for the picker, each with its open-session count.</param>
/// <param name="Allowed">What the agent may do.</param>
/// <param name="Denied">What it may not do.</param>
/// <param name="Commands">Only the commands whose right the agent holds.</param>
/// <param name="Sessions">The agent's sessions still open on the project.</param>
public sealed record AgentWorkspaceView(
    WorkspaceAgent Agent,
    IReadOnlyList<WorkspaceAgent> Agents,
    IReadOnlyList<WorkspaceRight> Allowed,
    IReadOnlyList<WorkspaceDeniedRight> Denied,
    IReadOnlyList<WorkspaceCommand> Commands,
    IReadOnlyList<WorkspaceSession> Sessions);

/// <summary>A command started from a workspace: the session it runs in and the message that begins it (REQ-FN-059).</summary>
/// <param name="SessionId">The session opened as the agent on the project.</param>
/// <param name="RoleId">The agent the session runs as.</param>
/// <param name="ProjectId">The project it runs on.</param>
/// <param name="Message">The text to send first, e.g. <c>"*verify-phase"</c>.</param>
public sealed record WorkspaceCommandStart(int SessionId, int RoleId, int ProjectId, string Message);
