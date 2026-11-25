using System.Text;
using System.Text.RegularExpressions;
using Chatur.Core.Actions;

namespace Chatur.Core.Roles;

/// <summary>
/// Reads an agent's persona file (for a TechieFlow project, <c>.tfcore/agents/&lt;code&gt;.md</c>) into who the agent is,
/// what it is for and the commands it has (REQ-FN-308). Two shapes exist in the framework and both are read: a
/// markdown table whose rows start with a <c>*command</c> (the flow master and the verifier), and a YAML
/// <c>commands:</c> list of <c>- name args: what it does</c> items beside an <c>agent:</c> block (the analyst and
/// the architect). Nothing is invented: a file with no commands gives an empty list, and the caller falls back to
/// the commands stored for the role.
/// </summary>
public static partial class AgentPersonaParser
{
    private const int DescriptionLimit = 140;

    /// <summary>What a persona file says about the agent.</summary>
    /// <param name="Name">The agent's own name, e.g. <c>Chanakya</c>; the title when the file gives none.</param>
    /// <param name="Title">The agent's title, e.g. <c>Business Analyst</c>.</param>
    /// <param name="Purpose">What the agent is for, in one or two sentences; empty when the file says nothing.</param>
    /// <param name="Commands">The commands the file lists, in file order.</param>
    public sealed record ParsedPersona(string Name, string Title, string Purpose, IReadOnlyList<PersonaCommand> Commands);

    /// <summary>Reads a persona file's text.</summary>
    /// <param name="aText">The persona file's whole text.</param>
    /// <param name="aFallbackName">The name to use when the file names none, normally the role's own name.</param>
    public static ParsedPersona Parse(string aText, string aFallbackName)
    {
        var vLines = (aText ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        var vName = YamlValue(vLines, "agent", "name");
        var vTitle = YamlValue(vLines, "agent", "title");
        var vPurpose = YamlValue(vLines, "agent", "whenToUse");
        if (vPurpose.StartsWith("Use for ", StringComparison.OrdinalIgnoreCase))
        {
            vPurpose = "Use me for " + vPurpose["Use for ".Length..];
        }

        if (vPurpose.Length == 0)
        {
            (vName, vTitle, vPurpose) = FromHeading(vLines, vName, vTitle);
        }

        var vCommands = new List<PersonaCommand>();
        AddTableCommands(vLines, vCommands);
        AddYamlCommands(vLines, vCommands);

        var vShownTitle = vTitle.Length > 0 ? vTitle : aFallbackName;
        var vShownName = vName.Length > 0 ? vName : vShownTitle;
        return new ParsedPersona(vShownName, vShownTitle, vPurpose, vCommands);
    }

    /// <summary>The value of <c>aKey:</c> inside the top-level <c>aBlock:</c> of the YAML in the file; empty when there is none.</summary>
    private static string YamlValue(string[] aLines, string aBlock, string aKey)
    {
        var vInBlock = false;
        foreach (var vLine in aLines)
        {
            if (vLine.StartsWith(aBlock + ":", StringComparison.Ordinal))
            {
                vInBlock = true;
                continue;
            }

            if (!vInBlock)
            {
                continue;
            }

            if (vLine.Length > 0 && !char.IsWhiteSpace(vLine[0]))
            {
                return string.Empty;
            }

            var vTrimmed = vLine.Trim();
            if (vTrimmed.StartsWith(aKey + ":", StringComparison.Ordinal))
            {
                var vValue = vTrimmed[(aKey.Length + 1)..].Trim().Trim('"', '\'');
                return string.Equals(vValue, "null", StringComparison.OrdinalIgnoreCase) ? string.Empty : vValue;
            }
        }

        return string.Empty;
    }

    /// <summary>For a persona that is plain markdown: the title from the first heading, and the name and purpose from the paragraph under it.</summary>
    private static (string Name, string Title, string Purpose) FromHeading(string[] aLines, string aName, string aTitle)
    {
        var vHeadingIndex = Array.FindIndex(aLines, aLine => aLine.StartsWith("# ", StringComparison.Ordinal));
        if (vHeadingIndex < 0)
        {
            return (aName, aTitle, string.Empty);
        }

        var vTitle = aTitle.Length > 0 ? aTitle : aLines[vHeadingIndex][2..].Trim();
        var vParagraph = aLines
            .Skip(vHeadingIndex + 1)
            .SkipWhile(string.IsNullOrWhiteSpace)
            .TakeWhile(aLine => !string.IsNullOrWhiteSpace(aLine))
            .Select(aLine => aLine.Trim())
            .ToList();
        if (vParagraph.Count == 0 || vParagraph[0].StartsWith('#') || vParagraph[0].StartsWith('|'))
        {
            return (aName, vTitle, string.Empty);
        }

        var vText = IconNote().Replace(string.Join(' ', vParagraph), string.Empty).Replace("`", string.Empty, StringComparison.Ordinal);
        var vName = aName;
        var vComma = vText.IndexOf(',', StringComparison.Ordinal);
        if (vName.Length == 0 && vComma is > 0 and <= 24)
        {
            vName = vText[..vComma].Trim();
        }

        var vColon = vText.IndexOf(": ", StringComparison.Ordinal);
        var vPurpose = vColon >= 0 ? vText[(vColon + 2)..].Trim() : vText.Trim();
        if (vPurpose.Length > 0)
        {
            vPurpose = char.ToUpperInvariant(vPurpose[0]) + vPurpose[1..];
        }

        return (vName, vTitle, vPurpose);
    }

    /// <summary>Adds each row of a markdown table whose first cell is a <c>*command</c>.</summary>
    private static void AddTableCommands(string[] aLines, List<PersonaCommand> aCommands)
    {
        foreach (var vLine in aLines)
        {
            var vTrimmed = vLine.Trim();
            if (!vTrimmed.StartsWith('|'))
            {
                continue;
            }

            var vCells = SplitRow(vTrimmed);
            if (vCells.Count < 2)
            {
                continue;
            }

            var vCommand = vCells[0].Replace("`", string.Empty, StringComparison.Ordinal).Trim();
            if (!vCommand.StartsWith('*') || vCommand.Length < 2)
            {
                continue;
            }

            AddIfNew(aCommands, vCommand, vCells[1]);
        }
    }

    /// <summary>Splits one table row into its cells, treating <c>\|</c> as a bar inside a cell rather than a cell edge.</summary>
    private static List<string> SplitRow(string aRow)
    {
        var vCells = new List<string>();
        var vCell = new StringBuilder();
        for (var vIndex = 1; vIndex < aRow.Length; vIndex++)
        {
            var vChar = aRow[vIndex];
            if (vChar == '\\' && vIndex + 1 < aRow.Length && aRow[vIndex + 1] == '|')
            {
                vCell.Append('|');
                vIndex++;
            }
            else if (vChar == '|')
            {
                vCells.Add(vCell.ToString().Trim());
                vCell.Clear();
            }
            else
            {
                vCell.Append(vChar);
            }
        }

        if (vCell.ToString().Trim().Length > 0)
        {
            vCells.Add(vCell.ToString().Trim());
        }

        return vCells;
    }

    /// <summary>Adds each item of the YAML <c>commands:</c> list: <c>- name args: what it does</c>.</summary>
    private static void AddYamlCommands(string[] aLines, List<PersonaCommand> aCommands)
    {
        var vInList = false;
        foreach (var vLine in aLines)
        {
            if (vLine.StartsWith("commands:", StringComparison.Ordinal))
            {
                vInList = true;
                continue;
            }

            if (!vInList)
            {
                continue;
            }

            if (vLine.Length > 0 && !char.IsWhiteSpace(vLine[0]))
            {
                return;
            }

            var vItem = vLine.Trim();
            if (!vItem.StartsWith("- ", StringComparison.Ordinal))
            {
                continue;
            }

            vItem = vItem[2..].Trim();
            var vColon = vItem.IndexOf(": ", StringComparison.Ordinal);
            var vName = (vColon > 0 ? vItem[..vColon] : vItem).Trim();
            var vDescription = vColon > 0 ? vItem[(vColon + 2)..] : string.Empty;
            if (vName.Length == 0)
            {
                continue;
            }

            AddIfNew(aCommands, vName.StartsWith('*') ? vName : "*" + vName, vDescription);
        }
    }

    private static void AddIfNew(List<PersonaCommand> aCommands, string aCommand, string aDescription)
    {
        if (aCommands.Any(aExisting => string.Equals(aExisting.Command, aCommand, StringComparison.Ordinal)))
        {
            return;
        }

        aCommands.Add(new PersonaCommand(aCommand, Shorten(aDescription)));
    }

    /// <summary>The first sentence of a description with its code quotes taken off, cut to a length that reads as one line.</summary>
    private static string Shorten(string aDescription)
    {
        var vText = aDescription.Replace("`", string.Empty, StringComparison.Ordinal).Trim();
        var vStop = vText.IndexOf(". ", StringComparison.Ordinal);
        if (vStop > 0)
        {
            vText = vText[..(vStop + 1)];
        }

        return vText.Length <= DescriptionLimit ? vText : vText[..(DescriptionLimit - 1)].TrimEnd() + "…";
    }

    [GeneratedRegex(@"\s*\(icon [^)]*\)")]
    private static partial Regex IconNote();
}
