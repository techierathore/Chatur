namespace Chatur.Core.Actions;

/// <summary>A proposed edit, waiting for the owner to approve or reject it (REQ-UI-032).</summary>
/// <param name="ChangeId">The row's identity.</param>
/// <param name="SessionId">The session that proposed the change.</param>
/// <param name="FilePath">The file the change applies to, relative to the project root.</param>
/// <param name="Before">The file's text on disk.</param>
/// <param name="After">The file's proposed new text.</param>
/// <param name="Status">The change's status: <c>"Proposed"</c>, <c>"Approved"</c> or <c>"Rejected"</c>.</param>
public sealed record ProposedChange(int ChangeId, int SessionId, string FilePath, string Before, string After, string Status);
