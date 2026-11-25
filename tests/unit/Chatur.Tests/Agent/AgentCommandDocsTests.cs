using System.Text.RegularExpressions;
using Chatur.Core.Agent.Commands;
using Xunit;

namespace Chatur.Tests.Agent;

/// <summary>
/// Proves REQ-NFR-015 and REQ-NFR-016 (BRD-263, BRD-264): the UsageGuide's "The agents and their commands" section and
/// the README's short list both name exactly the commands in <see cref="AgentCommandCatalogue.All"/>, agent by agent.
/// </summary>
public sealed partial class AgentCommandDocsTests
{
    private static string Root()
    {
        var vDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (vDirectory is not null && !File.Exists(Path.Combine(vDirectory.FullName, "Chatur.sln")))
        {
            vDirectory = vDirectory.Parent;
        }

        Assert.NotNull(vDirectory);
        return vDirectory!.FullName;
    }

    private static string Read(params string[] aParts) =>
        File.ReadAllText(Path.Combine([Root(), .. aParts])).Replace("\r\n", "\n", StringComparison.Ordinal);

    /// <summary>The guide's section, from its heading to the next second-level heading.</summary>
    private static string GuideSection()
    {
        var vGuide = Read("docs", "Chatur-UsageGuide.md");
        var vStart = vGuide.IndexOf("\n### " + AgentCommandCatalogue.GuideHeading + "\n", StringComparison.Ordinal);
        Assert.True(vStart >= 0, "the UsageGuide has the section \"" + AgentCommandCatalogue.GuideHeading + "\"");
        // It ends at the next heading of its own level or above ("#### " is an agent inside it).
        var vEnds = new[] { vGuide.IndexOf("\n## ", vStart + 5, StringComparison.Ordinal), vGuide.IndexOf("\n### ", vStart + 5, StringComparison.Ordinal) }.Where(i => i >= 0).ToList();
        var vEnd = vEnds.Count == 0 ? -1 : vEnds.Min();
        return vGuide[(vStart + 1)..(vEnd < 0 ? vGuide.Length : vEnd)];
    }

    /// <summary>The table rows under one agent's sub-heading: the five cells of each, split on the bars.</summary>
    private static List<string[]> TableRows(string aSection, string aRole)
    {
        var vHeading = "#### " + AgentCommandCatalogue.DisplayNameOf(aRole) + "\n";
        var vStart = aSection.IndexOf(vHeading, StringComparison.Ordinal);
        Assert.True(vStart >= 0, "the section has a sub-heading for " + aRole);
        var vEnd = aSection.IndexOf("\n#### ", vStart + vHeading.Length, StringComparison.Ordinal);
        var vPart = aSection[vStart..(vEnd < 0 ? aSection.Length : vEnd)];
        return vPart.Split('\n')
            .Where(l => l.StartsWith("| `*", StringComparison.Ordinal))
            .Select(l => l.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToArray())
            .ToList();
    }

    /// <summary>The names of the commands in a piece of text: each <c>`*name</c>.</summary>
    private static List<string> CommandNames(string aText) =>
        StarCommand().Matches(aText).Select(m => m.Groups[1].Value).ToList();

    /// <summary>When an agent's table in the guide lacks a command the list has, or names one the list lacks, then this fails.</summary>
    [Fact(DisplayName = "REQ-NFR-015 the guide's command tables match the command list")]
    public void GuideTablesMatchTheList()
    {
        var vSection = GuideSection();
        foreach (var vRole in AgentCommandCatalogue.RoleCodes)
        {
            var vInGuide = TableRows(vSection, vRole).Select(r => CommandNames(r[0]).Single()).Order(StringComparer.Ordinal).ToList();
            var vInCode = AgentCommandCatalogue.ForRole(vRole).Select(c => c.Code).Order(StringComparer.Ordinal).ToList();
            Assert.Equal(vInCode, vInGuide);
        }
    }

    /// <summary>When a row's what, example or where is empty, or differs from what the code gives, then this fails.</summary>
    [Fact(DisplayName = "REQ-NFR-015 every row of the guide's tables has what, example and where from the code")]
    public void GuideRowsAreFilledFromTheCode()
    {
        var vSection = GuideSection();
        foreach (var vRole in AgentCommandCatalogue.RoleCodes)
        {
            var vRows = TableRows(vSection, vRole);
            Assert.NotEmpty(vRows);
            foreach (var vRow in vRows)
            {
                Assert.Equal(4, vRow.Length);
                Assert.All(vRow, vCell => Assert.False(string.IsNullOrWhiteSpace(vCell)));
                var vCommand = AgentCommandCatalogue.ForRole(vRole).Single(c => "`*" + c.Code + "`" == vRow[0]);
                Assert.Equal(vCommand.Description, vRow[1]);
                Assert.Equal("`" + AgentCommandCatalogue.ExampleOf(vCommand) + "`", vRow[2]);
                Assert.Equal(AgentCommandCatalogue.WhereOf(vCommand), vRow[3]);
                Assert.Matches("^(In the chat|A file: `.+`.*|A run on the (Processes|End-to-end run) page)$", vRow[3]);
            }
        }
    }

    /// <summary>When the README lacks a command an agent has, or lists one it does not, or does not link the guide's section, then this fails.</summary>
    [Fact(DisplayName = "REQ-NFR-016 the README lists each agent's commands and links the guide's section")]
    public void ReadmeMatchesTheList()
    {
        var vReadme = Read("README.md");
        Assert.Contains("](" + AgentCommandCatalogue.GuideLink + ")", vReadme, StringComparison.Ordinal);
        foreach (var vRole in AgentCommandCatalogue.RoleCodes)
        {
            var vLine = vReadme.Split('\n').SingleOrDefault(l => l.StartsWith("- **" + AgentCommandCatalogue.DisplayNameOf(vRole) + "**:", StringComparison.Ordinal));
            Assert.NotNull(vLine);
            var vInReadme = CommandNames(vLine!).Order(StringComparer.Ordinal).ToList();
            var vInCode = AgentCommandCatalogue.ForRole(vRole).Select(c => c.Code).Order(StringComparer.Ordinal).ToList();
            Assert.Equal(vInCode, vInReadme);
        }
    }

    /// <summary>The generated markdown carries every command once under its agent, so the document text can be taken from the code.</summary>
    [Fact(DisplayName = "REQ-NFR-015 the generated guide and README text name every command once under its agent")]
    public void GeneratedTextNamesEveryCommand()
    {
        var vGuide = AgentCommandCatalogue.UsageGuideMarkdown();
        var vReadme = AgentCommandCatalogue.ReadmeMarkdown();
        Assert.Equal(AgentCommandCatalogue.All.Count, TableRowCount(vGuide));
        Assert.Equal(AgentCommandCatalogue.All.Count, CommandNames(vReadme).Count);
        Assert.All(AgentCommandCatalogue.All, c =>
        {
            Assert.False(string.IsNullOrWhiteSpace(AgentCommandCatalogue.ExampleOf(c)));
            Assert.StartsWith("*" + c.Code, AgentCommandCatalogue.ExampleOf(c), StringComparison.Ordinal);
            Assert.DoesNotContain("{", AgentCommandCatalogue.ExampleOf(c), StringComparison.Ordinal);
        });
    }

    private static int TableRowCount(string aText) => aText.Split('\n').Count(l => l.StartsWith("| `*", StringComparison.Ordinal));

    [GeneratedRegex(@"`\*([a-z0-9-]+)")]
    private static partial Regex StarCommand();
}
