namespace Chatur.Core.Actions;

/// <summary>Whether a session waits for approval before writing a file (REQ-UI-024).</summary>
public enum SessionMode
{
    /// <summary>Every proposed change waits for the owner to approve or reject it.</summary>
    AskFirst,

    /// <summary>The agent writes without stopping.</summary>
    GoAhead
}

/// <summary>The agent Chatur pre-selected for a project, and why (REQ-FN-024).</summary>
/// <param name="RoleId">The suggested role, or <see langword="null"/> to suggest the model alone.</param>
/// <param name="Why">One line explaining the suggestion, shown beside the picker.</param>
public sealed record AgentSuggestion(int? RoleId, string Why);

/// <summary>One piece of a streamed reply (REQ-FN-025, REQ-FN-026, REQ-FN-027).</summary>
/// <param name="TextDelta">The text to append to the reply so far.</param>
/// <param name="IsFinal">Whether this is the last piece of the reply.</param>
/// <param name="ModelName">The model that produced the reply; set once known.</param>
/// <param name="TokensUsed">The tokens the finished reply used; set only on the final piece.</param>
/// <param name="RoutingReason">In a few words, why Automatic routing picked that model (REQ-UI-111); <see langword="null"/> when routing did not choose; set only on the final piece.</param>
public sealed record AgentReplyChunk(string TextDelta, bool IsFinal, string? ModelName, int? TokensUsed, string? RoutingReason = null)
{
    /// <summary>The conversation's mode after this reply, when a command changed it (<c>*yolo</c>, BRD-249); <see langword="null"/> otherwise.</summary>
    public SessionMode? ModeNow { get; init; }

    /// <summary>Whether this reply closed the conversation (<c>*exit</c>, BRD-249), so the next message starts a new one.</summary>
    public bool EndsConversation { get; init; }
}

/// <summary>One past or current session, as Sessions lists it (REQ-UI-031).</summary>
/// <param name="SessionId">The row's identity.</param>
/// <param name="ProjectId">The project the session belongs to.</param>
/// <param name="RoleName">The role that acted in the session.</param>
/// <param name="Mode">Ask first, or go ahead.</param>
/// <param name="StartedUtc">When the session started.</param>
/// <param name="State">The session's current state.</param>
public sealed record SessionSummary(int SessionId, int ProjectId, string RoleName, SessionMode Mode, DateTime StartedUtc, string State);

/// <summary>One recorded step of a session (REQ-FN-032).</summary>
/// <param name="Seq">The step's order within the session.</param>
/// <param name="Kind">The kind of step: message, tool request, refusal, or result.</param>
/// <param name="Payload">The step's own content.</param>
/// <param name="Model">The model that produced this step, when one did.</param>
/// <param name="Tokens">The tokens this step used, when known.</param>
/// <param name="RoleName">The agent that wrote this reply (REQ-FN-024); <see langword="null"/> for a step that is not a reply, and for a reply recorded before replies kept their agent.</param>
/// <param name="RoutingReason">Why Automatic routing picked the model that wrote this reply (REQ-UI-111); <see langword="null"/> when routing did not choose it, and for a reply recorded before the reason was kept.</param>
public sealed record SessionEventRecord(int Seq, string Kind, string Payload, string? Model, int? Tokens, string? RoleName = null, string? RoutingReason = null);

/// <summary>One command an agent has (REQ-FN-308).</summary>
/// <param name="Command">The command as typed, e.g. <c>*build-phase {App}</c>.</param>
/// <param name="Description">What it does, in one line; empty when the agent's definition says nothing.</param>
public sealed record PersonaCommand(string Command, string Description);

/// <summary>What an agent says about itself when the person chooses it (REQ-FN-308).</summary>
/// <param name="RoleName">The role's name in the picker, e.g. <c>Analyst</c>.</param>
/// <param name="Name">The agent's own name, e.g. <c>Chanakya</c>; the role's name when it has none.</param>
/// <param name="Title">The agent's title, e.g. <c>Business Analyst</c>.</param>
/// <param name="Purpose">What the agent is for, in a sentence or two.</param>
/// <param name="Commands">The commands the agent has, as its own definition lists them.</param>
public sealed record AgentGreeting(string RoleName, string Name, string Title, string Purpose, IReadOnlyList<PersonaCommand> Commands);

/// <summary>A session with its full recorded history (REQ-FN-031, REQ-FN-032).</summary>
/// <param name="Summary">The session's summary row.</param>
/// <param name="Events">Every message, step and refusal, in order.</param>
public sealed record SessionDetail(SessionSummary Summary, IReadOnlyList<SessionEventRecord> Events);

/// <summary>One step the agent took, for the activity panel (REQ-UI-022).</summary>
/// <param name="Kind">The kind of step, e.g. <c>"read"</c>, <c>"edit"</c>, <c>"run"</c>, <c>"refused"</c>.</param>
/// <param name="Description">A short description of the step.</param>
/// <param name="Succeeded">Whether the step succeeded.</param>
/// <param name="Seq">The step's order within the session, the same numbering the messages carry, so the conversation can draw it where it happened (REQ-UI-023); 0 when not known.</param>
public sealed record ActivityEvent(string Kind, string Description, bool Succeeded, int Seq = 0);
