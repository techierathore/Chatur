using System.Globalization;

namespace Chatur.Core.ProcessRuns;

/// <summary>Small wording helpers shared by the steps of a verifying run.</summary>
public static class RunText
{
    /// <summary>A limit or duration in words: seconds under a minute, minutes otherwise.</summary>
    /// <param name="aMilliseconds">The length of time.</param>
    public static string Span(long aMilliseconds) => aMilliseconds < 60_000
        ? string.Create(CultureInfo.InvariantCulture, $"{aMilliseconds / 1000.0:0.#} s")
        : string.Create(CultureInfo.InvariantCulture, $"{aMilliseconds / 60000.0:0.#} min");

    /// <summary>The last non-empty lines of some output, capped in characters.</summary>
    /// <param name="aOutput">The output.</param>
    /// <param name="aLines">How many lines to keep.</param>
    /// <param name="aChars">The most characters to keep.</param>
    public static string Tail(string aOutput, int aLines, int aChars)
    {
        var vTail = string.Join('\n', aOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).TakeLast(aLines));
        return vTail.Length > aChars ? vTail[^aChars..] : vTail;
    }
}
