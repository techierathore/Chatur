using System.Text.RegularExpressions;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Proves REQ-NFR-004: a verified requirement's verdict in the checklist names both the Mac and
/// Windows. The checklist is the artefact under test: the convention must be written down, and the
/// verdict form it prescribes must be one a reader can tell is complete.
/// </summary>
public sealed class VerdictConventionTests
{
    private static readonly Regex objVerdict = new(
        @"\[REQ-[A-Z]+-\d+\]\s+Mac:\s*(?<mac>[^;]+);\s*Windows:\s*(?<win>.+)$", RegexOptions.Compiled);

    private static string Checklist()
    {
        var vDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (vDirectory is not null && !File.Exists(Path.Combine(vDirectory.FullName, "Chatur.sln")))
        {
            vDirectory = vDirectory.Parent;
        }

        Assert.NotNull(vDirectory);
        return File.ReadAllText(Path.Combine(vDirectory!.FullName, "docs", "Chatur-Checklist.md"));
    }

    /// <summary>
    /// When the checklist is read, then its verdict convention section states that a verdict names the
    /// Mac and Windows, and that Mac is verified first.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-004 the checklist states the two-machine verdict form")]
    public void ChecklistStatesTheTwoMachineVerdictForm()
    {
        var vText = Checklist();

        Assert.Contains("## Verdict convention (REQ-NFR-004)", vText);
        Assert.Contains("[REQ-xxx] Mac: <verdict>; Windows: <verdict>", vText);
        Assert.Contains("verified on Mac Catalyst first", vText);
    }

    /// <summary>
    /// When a Remarks cell follows the convention, then the form is recognised with both machines'
    /// verdicts read out.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-004 the convention form names both machines")]
    public void ConventionFormNamesBothMachines()
    {
        var vMatch = objVerdict.Match("[REQ-FN-001] Mac: not registered (owner); Windows: Verified");

        Assert.True(vMatch.Success);
        Assert.Equal("not registered (owner)", vMatch.Groups["mac"].Value.Trim());
        Assert.Equal("Verified", vMatch.Groups["win"].Value.Trim());
    }

    /// <summary>When a verdict names only one machine, then it is not the convention form.</summary>
    [Fact(DisplayName = "REQ-NFR-004 a verdict naming one machine is not the convention form")]
    public void AVerdictNamingOneMachineIsNotTheConventionForm()
    {
        Assert.DoesNotMatch(objVerdict, "[REQ-FN-001] Windows: Verified");
        Assert.DoesNotMatch(objVerdict, "[REQ-FN-001] Mac: Verified");
    }
}
