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
public sealed record AgentReplyChunk(string TextDelta, bool IsFinal, string? ModelName, int? TokensUsed);

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
public sealed record SessionEventRecord(int Seq, string Kind, string Payload, string? Model, int? Tokens);

/// <summary>A session with its full recorded history (REQ-FN-031, REQ-FN-032).</summary>
/// <param name="Summary">The session's summary row.</param>
/// <param name="Events">Every message, step and refusal, in order.</param>
public sealed record SessionDetail(SessionSummary Summary, IReadOnlyList<SessionEventRecord> Events);

/// <summary>One step the agent took, for the activity panel (REQ-UI-022).</summary>
/// <param name="Kind">The kind of step, e.g. <c>"read"</c>, <c>"edit"</c>, <c>"run"</c>, <c>"refused"</c>.</param>
/// <param name="Description">A short description of the step.</param>
/// <param name="Succeeded">Whether the step succeeded.</param>
public sealed record ActivityEvent(string Kind, string Description, bool Succeeded);
