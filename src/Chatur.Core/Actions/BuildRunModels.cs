namespace Chatur.Core.Actions;

/// <summary>A target this machine can build and run (REQ-UI-011).</summary>
/// <param name="RunTargetId">The row's identity.</param>
/// <param name="Name">The target's display name, e.g. <c>"Mac Catalyst · Debug"</c>.</param>
/// <param name="Platform">The platform the target builds for.</param>
/// <param name="IsAvailableOnThisMachine">Whether this machine can build the target.</param>
public sealed record RunTarget(int RunTargetId, string Name, string Platform, bool IsAvailableOnThisMachine);

/// <summary>One line of a build or run's output (REQ-UI-014).</summary>
/// <param name="Text">The line's text.</param>
/// <param name="IsError">Whether the line came from standard error.</param>
public sealed record OutputLine(string Text, bool IsError);

/// <summary>One error a failed build reported (REQ-UI-016).</summary>
/// <param name="FilePath">The file the error is in, relative to the project root.</param>
/// <param name="Line">The line number the error is at.</param>
/// <param name="Message">The compiler's own message.</param>
public sealed record BuildError(string FilePath, int Line, string Message);

/// <summary>The result of a build (REQ-UI-012).</summary>
/// <param name="Succeeded">Whether the build succeeded.</param>
/// <param name="Duration">How long the build took.</param>
/// <param name="Errors">Every error the build reported; empty when it succeeded.</param>
public sealed record BuildResult(bool Succeeded, TimeSpan Duration, IReadOnlyList<BuildError> Errors);
