using System.Text.RegularExpressions;

namespace Chatur.Core.ProcessRuns;

/// <summary>
/// Finds the requirement ids a test carries in its name, display name or title: every
/// <c>REQ-XX-123</c>, and the shorthand <c>REQ-FN-053/055</c> for two ids of one type (the way this
/// project's own tests are named).
/// </summary>
public static partial class RequirementIdScanner
{
    [GeneratedRegex(@"REQ-(?<type>[A-Z]+)-(?<first>\d+)(?<more>(?:/\d+)*)")]
    private static partial Regex Ids();

    /// <summary>Every distinct requirement id in the text, in the order written.</summary>
    /// <param name="aText">A test's name, display name or title.</param>
    public static IReadOnlyList<string> Scan(string aText)
    {
        var vFound = new List<string>();
        foreach (Match vMatch in Ids().Matches(aText))
        {
            var vType = vMatch.Groups["type"].Value;
            vFound.Add($"REQ-{vType}-{vMatch.Groups["first"].Value}");
            foreach (var vNumber in vMatch.Groups["more"].Value.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                vFound.Add($"REQ-{vType}-{vNumber}");
            }
        }

        return vFound.Distinct(StringComparer.Ordinal).ToList();
    }
}
