namespace Chatur.Core.Guards;

/// <summary>A tool the agent is asking to use, before any guard has decided (REQ-FN-036).</summary>
/// <param name="SessionId">The session making the request.</param>
/// <param name="ToolName">
/// The tool's name — one of <see cref="Agent.AgentToolNames"/>, e.g. <c>"read-file"</c>,
/// <c>"edit-file"</c>, <c>"run-build"</c>, <c>"run-source-control"</c>.
/// </param>
/// <param name="Description">A short description of what the tool would do, for the activity panel.</param>
/// <param name="FilePath">
/// The file the tool acts on, relative to the project root; <see langword="null"/> for a tool that
/// does not touch one file, e.g. <c>run-build</c>.
/// </param>
/// <param name="Before">
/// The file's text on disk before an <c>edit-file</c> request; <see langword="null"/> otherwise.
/// Read by the caller before the guard runs, so <see cref="Guards.AskMeFirstGuard"/> can hold a held
/// write as a proposed <c>Change</c> row without itself depending on <c>IFileActions</c>
/// (Architecture §7 "Guards" depends only on "Roles").
/// </param>
/// <param name="After">
/// The file's proposed new text for an <c>edit-file</c> request; <see langword="null"/> otherwise.
/// </param>
/// <param name="Arguments">
/// The named arguments of a tool that has more than a file path to carry, e.g. <c>correct-wording</c>'s
/// <c>kind</c>, <c>target</c>, <c>wording</c> and <c>reason</c>; <see langword="null"/> otherwise.
/// </param>
public sealed record ToolRequest(
    int SessionId,
    string ToolName,
    string Description,
    string? FilePath = null,
    string? Before = null,
    string? After = null,
    IReadOnlyDictionary<string, string>? Arguments = null);
