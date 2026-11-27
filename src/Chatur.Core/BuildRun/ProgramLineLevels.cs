using System.Text.RegularExpressions;

namespace Chatur.Core.BuildRun;

/// <summary>How Output marks one line of a running program's output (REQ-UI-107, BRD-235).</summary>
public enum ProgramLineLevel
{
    /// <summary>An ordinary line: information, debug or trace, or plain text.</summary>
    Ordinary,

    /// <summary>A warning: shown as one, never as an error.</summary>
    Warning,

    /// <summary>An error: a failure the program reported.</summary>
    Error,
}

/// <summary>
/// Reads the level of each line a running program writes (REQ-UI-107, BRD-235). A line is an error by its own level,
/// never because it came on standard error. The .NET console logger writes a header line such as
/// <c>warn: Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware[3]</c> and then the message on indented lines;
/// those lines carry the header's level, so "Failed to determine the https port for redirect." under a <c>warn:</c>
/// header is a warning. <c>fail:</c> and <c>crit:</c> are errors, <c>warn:</c> a warning, <c>info:</c>, <c>dbug:</c> and
/// <c>trce:</c> ordinary. A line that is not a logger line keeps the earlier rule: an unhandled exception, a failed
/// test or a compiler-style error is an error. One reader serves one program's output, in the order it is written.
/// </summary>
public sealed class ProgramLineLevels
{
    private static readonly Regex HeaderRegex = new(@"^(?<level>trce|dbug|info|warn|fail|crit): ", RegexOptions.Compiled);

    private static readonly Regex AnsiRegex = new(@"\x1B\[[0-9;]*[A-Za-z]", RegexOptions.Compiled);

    private static readonly Regex CompilerErrorRegex = new(
        @"^\s*[^()\r\n]+?\(\d+(,\d+)?\)\s*:\s*error\s+\S+\s*:",
        RegexOptions.Compiled);

    private readonly Lock objLock = new();

    private ProgramLineLevel? objCarried;

    /// <summary>Reads one line's level; a logger header's level is kept for the indented lines that follow it.</summary>
    /// <param name="aLine">The line as the program wrote it.</param>
    /// <returns>The line's level.</returns>
    public ProgramLineLevel Read(string aLine)
    {
        var vLine = AnsiRegex.Replace(aLine ?? string.Empty, string.Empty);
        lock (objLock)
        {
            var vHeader = HeaderRegex.Match(vLine);
            if (vHeader.Success)
            {
                objCarried = LevelOf(vHeader.Groups["level"].Value);
                return objCarried.Value;
            }

            if (vLine.Trim().Length == 0)
            {
                return ProgramLineLevel.Ordinary;
            }

            if (objCarried is { } vCarried && char.IsWhiteSpace(vLine[0]))
            {
                return vCarried;
            }

            objCarried = null;
            return IsPlainErrorLine(vLine) ? ProgramLineLevel.Error : ProgramLineLevel.Ordinary;
        }
    }

    private static ProgramLineLevel LevelOf(string aLevel) => aLevel switch
    {
        "fail" or "crit" => ProgramLineLevel.Error,
        "warn" => ProgramLineLevel.Warning,
        _ => ProgramLineLevel.Ordinary,
    };

    /// <summary>A line that is not a logger line, reporting a failure: an unhandled exception, a failed test or a compiler-style error.</summary>
    private static bool IsPlainErrorLine(string aLine)
    {
        var vText = aLine.TrimStart();
        return vText.StartsWith("Unhandled exception", StringComparison.Ordinal)
            || vText.StartsWith("[FAIL]", StringComparison.Ordinal)
            || vText.StartsWith("Failed ", StringComparison.Ordinal)
            || CompilerErrorRegex.IsMatch(aLine);
    }
}
