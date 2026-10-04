namespace Chatur.Core.Agent;

/// <summary>
/// The tool-name vocabulary the agent loop asks the guards about (Architecture §3 "Guards --&gt;
/// Tools: read, search, edit, run, build"). These are exactly the <c>RoleRight.Action</c> values
/// <c>0002-SeedRoles.sql</c> seeds, so <see cref="Guards.RoleRightsGuard"/> (cluster K/L) matches a
/// request's <see cref="Guards.ToolRequest.ToolName"/> against a role's rights without either side
/// inventing its own spelling.
/// </summary>
public static class AgentToolNames
{
    /// <summary>Reads a file's text (always allowed; every role's <c>read-file</c> right is seeded <c>1</c>).</summary>
    public const string ReadFile = "read-file";

    /// <summary>Proposes or applies a file's new text (REQ-FN-033, REQ-FN-034, REQ-FN-035).</summary>
    public const string EditFile = "edit-file";

    /// <summary>
    /// Corrects the wording of a role, a rule or a process step in Chatur's own database and logs the
    /// correction for the owner to keep or undo (REQ-UI-034). Seeded as a <c>RoleRight</c> by
    /// <c>0026-M-CorrectWordingRight.sql</c>: every role holds it, because a role that finds its own
    /// instructions wrong mid-work must be able to say so on the spot, and the safety is not the right
    /// but the log — every use is a <c>Correction</c> row the owner can undo.
    /// </summary>
    public const string CorrectWording = "correct-wording";

    /// <summary>Runs the project's own build.</summary>
    public const string RunBuild = "run-build";

    /// <summary>
    /// Any source-control command (status, commit, push, pull, reset, checkout, …). Always refused by
    /// <see cref="Guards.SourceControlRefusalGuard"/> (REQ-UI-033, Coding Standards "A model never
    /// runs a source-control command").
    /// </summary>
    public const string RunSourceControl = "run-source-control";
}
