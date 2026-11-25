using System.Text.RegularExpressions;
using Chatur.Core.Actions;

namespace Chatur.Core.Projects;

/// <summary>
/// Reads what <c>dotnet new list</c> and <c>dotnet new &lt;template&gt; --help</c> print, and builds the
/// command lines that make a project and add it to a solution (BRD-244, REQ-UI-112). Pure text in, text
/// out, so it is checked without a machine's templates.
/// </summary>
public static partial class DotnetTemplateOutput
{
    /// <summary>The arguments that list project templates only.</summary>
    public const string ListArguments = "new list --type project";

    /// <summary>The arguments that print a template's own help.</summary>
    /// <param name="aShortName">The template's short name.</param>
    public static string HelpArguments(string aShortName) => $"new {SafeToken(aShortName, "template")} --help";

    /// <summary>The arguments that make a project from a template.</summary>
    /// <param name="aShortName">The template's short name.</param>
    /// <param name="aName">The project's name.</param>
    /// <param name="aLocation">The folder to make it in.</param>
    /// <param name="aFramework">The framework, or <see langword="null"/> for the template's own.</param>
    public static string NewArguments(string aShortName, string aName, string aLocation, string? aFramework)
    {
        var vArguments = $"new {SafeToken(aShortName, "template")} -n \"{aName}\" -o \"{aLocation}\"";
        return string.IsNullOrWhiteSpace(aFramework) ? vArguments : $"{vArguments} -f {SafeToken(aFramework, "framework")}";
    }

    /// <summary>The arguments that add one or more projects to a solution.</summary>
    /// <param name="aSolutionPath">The solution file's path.</param>
    /// <param name="aProjectPaths">The project files' paths.</param>
    public static string AddToSolutionArguments(string aSolutionPath, params string[] aProjectPaths) =>
        $"sln \"{aSolutionPath}\" add {string.Join(' ', aProjectPaths.Select(p => $"\"{p}\""))}";

    /// <summary>
    /// The extension <c>dotnet new sln</c> gives a solution file, from what <c>dotnet --version</c> printed:
    /// <c>.slnx</c> from the .NET 10 SDK on, <c>.sln</c> before it or when the version cannot be read.
    /// </summary>
    /// <param name="aVersionOutput">The command's output, e.g. <c>10.0.100</c>.</param>
    public static string SolutionExtensionFor(string aVersionOutput)
    {
        foreach (var vLine in (aVersionOutput ?? string.Empty).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var vDot = vLine.IndexOf('.', StringComparison.Ordinal);
            if (vDot > 0 && int.TryParse(vLine[..vDot], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var vMajor))
            {
                return vMajor >= 10 ? ".slnx" : ".sln";
            }
        }

        return ".sln";
    }

    /// <summary>
    /// The C# project templates in <c>dotnet new list</c> output. The columns are found from the line of
    /// dashes under the header, so a wider or narrower table reads the same; the first short name of a
    /// template is kept, a short name listed twice counts once, and a template that offers no C# is left out.
    /// </summary>
    /// <param name="aOutput">The command's output.</param>
    public static IReadOnlyList<ProjectTemplate> ParseTemplates(string aOutput)
    {
        var vLines = (aOutput ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
        var vRuleIndex = -1;
        for (var vIndex = 1; vIndex < vLines.Length; vIndex++)
        {
            if (RuleLine().IsMatch(vLines[vIndex]) && vLines[vIndex - 1].Trim().Length > 0)
            {
                vRuleIndex = vIndex;
                break;
            }
        }

        if (vRuleIndex < 0)
        {
            return [];
        }

        var vColumns = ColumnsOf(vLines[vRuleIndex]);
        var vResult = new List<ProjectTemplate>();
        var vSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var vIndex = vRuleIndex + 1; vIndex < vLines.Length; vIndex++)
        {
            var vLine = vLines[vIndex];
            if (vLine.Trim().Length == 0)
            {
                break;
            }

            var vName = Cell(vLine, vColumns, 0);
            var vShort = Cell(vLine, vColumns, 1).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            var vLanguages = Cell(vLine, vColumns, 2);
            var vTags = Cell(vLine, vColumns, 3);
            if (vShort is null || vName.Length == 0 || !ShortNamePattern().IsMatch(vShort))
            {
                continue;
            }

            if (vLanguages.Length > 0 && !vLanguages.Contains("C#", StringComparison.Ordinal))
            {
                continue;
            }

            if (vSeen.Add(vShort))
            {
                vResult.Add(new ProjectTemplate(vName, vShort, vLanguages, vTags));
            }
        }

        return vResult;
    }

    /// <summary>
    /// The framework choices in a template's help: the <c>-f, --framework &lt;net10.0|net9.0&gt;</c> line,
    /// sorted newest first, and the <c>Default:</c> line under it.
    /// </summary>
    /// <param name="aOutput">The help text.</param>
    public static TemplateFrameworks ParseFrameworks(string aOutput)
    {
        var vLines = (aOutput ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
        for (var vIndex = 0; vIndex < vLines.Length; vIndex++)
        {
            var vMatch = FrameworkOption().Match(vLines[vIndex]);
            if (!vMatch.Success)
            {
                continue;
            }

            // An SDK that prints "<choice>" in the header (the 10.0 SDK on Linux) lists the choices only on the
            // lines under it ("net10.0  Target net10.0"), so both are read.
            var vListed = new List<string>();
            for (var vNext = vIndex + 1; vNext < vLines.Length && vNext <= vIndex + 14; vNext++)
            {
                var vText = vLines[vNext].Trim();
                if (vText.StartsWith('-') || vText.StartsWith("Default:", StringComparison.Ordinal))
                {
                    break;
                }

                var vFirst = vText.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
                if (vFirst.StartsWith("net", StringComparison.OrdinalIgnoreCase) && ChoicePattern().IsMatch(vFirst))
                {
                    vListed.Add(vFirst);
                }
            }

            var vChoices = vMatch.Groups["choices"].Value
                .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Where(c => c.StartsWith("net", StringComparison.OrdinalIgnoreCase) && ChoicePattern().IsMatch(c))
                .Concat(vListed)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(VersionOf)
                .ThenBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();

            string? vDefault = null;
            for (var vNext = vIndex + 1; vNext < vLines.Length && vNext <= vIndex + 14; vNext++)
            {
                var vText = vLines[vNext].Trim();
                if (vText.StartsWith('-'))
                {
                    break;
                }

                if (vText.StartsWith("Default:", StringComparison.Ordinal))
                {
                    vDefault = vText["Default:".Length..].Trim();
                    break;
                }
            }

            return new TemplateFrameworks(vChoices, vDefault is not null && vChoices.Contains(vDefault) ? vDefault : null);
        }

        return new TemplateFrameworks([], null);
    }

    private static Version VersionOf(string aChoice)
    {
        var vMatch = VersionText().Match(aChoice);
        return vMatch.Success && Version.TryParse(vMatch.Value, out var vVersion) ? vVersion : new Version(0, 0);
    }

    private static string SafeToken(string aValue, string aWhat) =>
        aValue is not null && ChoicePattern().IsMatch(aValue)
            ? aValue
            : throw new InvalidOperationException($"The {aWhat} name \"{aValue}\" is not one dotnet accepts.");

    /// <summary>The start and width of each column, from the line of dashes.</summary>
    private static List<(int Start, int Width)> ColumnsOf(string aRule)
    {
        var vColumns = new List<(int, int)>();
        var vIndex = 0;
        while (vIndex < aRule.Length)
        {
            if (aRule[vIndex] == '-')
            {
                var vStart = vIndex;
                while (vIndex < aRule.Length && aRule[vIndex] == '-')
                {
                    vIndex++;
                }

                vColumns.Add((vStart, vIndex - vStart));
            }
            else
            {
                vIndex++;
            }
        }

        return vColumns;
    }

    /// <summary>One cell of a row; the last column runs to the end of the line.</summary>
    private static string Cell(string aLine, List<(int Start, int Width)> aColumns, int aColumn)
    {
        if (aColumn >= aColumns.Count)
        {
            return string.Empty;
        }

        var (vStart, vWidth) = aColumns[aColumn];
        if (vStart >= aLine.Length)
        {
            return string.Empty;
        }

        var vLength = aColumn == aColumns.Count - 1 ? aLine.Length - vStart : Math.Min(vWidth, aLine.Length - vStart);
        return aLine.Substring(vStart, vLength).Trim();
    }

    [GeneratedRegex(@"^\s*-{2,}(\s+-{2,})+\s*$")]
    private static partial Regex RuleLine();

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    private static partial Regex ShortNamePattern();

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    private static partial Regex ChoicePattern();

    [GeneratedRegex(@"--framework\s+<(?<choices>[^>]+)>")]
    private static partial Regex FrameworkOption();

    [GeneratedRegex(@"\d+(\.\d+)*")]
    private static partial Regex VersionText();
}
