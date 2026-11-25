using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Proves REQ-NFR-013 (BRD-252, owner's test of 22 Nov 2026): the owner's test and the tests behind it start from the two
/// states a person really meets — the first start after install with nothing open, and a start with an empty folder
/// open — never from a solution made by hand. On 22 Nov the owner's test told him to press a New project that the
/// "Nothing is open yet" screen did not have, and every check behind it began from a hand-made solution, so all passed.
/// The documents and the test files are the artefacts under test.
/// </summary>
public sealed class OwnerTestStatesTests
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

    /// <summary>The owner's test opens on "Nothing is open yet" and presses New project there, then works from an empty folder.</summary>
    [Fact(DisplayName = "REQ-NFR-013 the owner's test starts from Nothing is open yet and from an empty folder")]
    public void OwnerTestStartsFromTheRealStates()
    {
        var vGuide = File.ReadAllText(Path.Combine(Root(), "docs", "Chatur-UsageGuide.md"));
        var vStart = vGuide.IndexOf("### Owner's test 1", StringComparison.Ordinal);
        Assert.True(vStart >= 0, "the UsageGuide has an Owner's test 1");
        var vFirst = vGuide[vStart..vGuide.IndexOf("### Owner's test 2", vStart, StringComparison.Ordinal)];

        Assert.Contains("Nothing is open yet", vFirst, StringComparison.Ordinal);
        Assert.Contains("New project", vFirst, StringComparison.Ordinal);
        Assert.Contains("test data folder", vFirst, StringComparison.Ordinal);
        Assert.Contains("an empty folder", vGuide[vStart..], StringComparison.Ordinal);
    }

    /// <summary>The tests behind New project begin from nothing open and from an empty folder, and the installed app's test does too.</summary>
    [Fact(DisplayName = "REQ-NFR-013 the New project tests and the installed app's test start from the two real states")]
    public void TestsStartFromTheRealStates()
    {
        var vStates = File.ReadAllText(Path.Combine(Root(), "tests", "verify", "new-project-states.spec.ts"));
        Assert.Contains("with nothing open, New project on Nothing is open yet", vStates, StringComparison.Ordinal);
        Assert.Contains("with an empty folder open, New project shows the folder", vStates, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft Visual Studio Solution File", vStates, StringComparison.Ordinal);

        var vInstalled = File.ReadAllText(Path.Combine(Root(), "tests", "verify", "installed-owner-test.spec.ts"));
        Assert.Contains("empty-start-title", vInstalled, StringComparison.Ordinal);
        Assert.Contains("way-new-project", vInstalled, StringComparison.Ordinal);
        Assert.Contains("CHATUR_TEST_DATA", vInstalled, StringComparison.Ordinal);
    }

    /// <summary>
    /// REQ-NFR-017 (BRD-275, owner's brief of 22 Nov 2026 evening): the owner's test keeps its parts and gains one that
    /// walks the measurements screens and one that types a command to each of the four agents; the installed app's test
    /// runs both. Since 23 Nov 2026 (BRD-281) the test has eleven parts, and these two are parts 10 and 11.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-017 the owner's test keeps its eight parts and adds the measurements screens and a command to each agent")]
    public void OwnerTestHasTheMeasurementsAndAgentParts()
    {
        var vGuide = File.ReadAllText(Path.Combine(Root(), "docs", "Chatur-UsageGuide.md"));
        for (var vPart = 1; vPart <= 11; vPart++)
        {
            Assert.Contains($"### Owner's test {vPart}:", vGuide, StringComparison.Ordinal);
        }

        var vNine = Section(vGuide, "### Owner's test 10:");
        foreach (var vWord in new[] { "Project ▸ Measurements", "Save as zip", "Measurements across projects", "not measured", "no price" })
        {
            Assert.Contains(vWord, vNine, StringComparison.Ordinal);
        }

        var vTen = Section(vGuide, "### Owner's test 11:");
        foreach (var vWord in new[] { "Analyst: `*", "Architect: `*", "Flow master: `*", "Verifier: `*" })
        {
            Assert.Contains(vWord, vTen, StringComparison.Ordinal);
        }

        var vInstalled = File.ReadAllText(Path.Combine(Root(), "tests", "verify", "installed-owner-test.spec.ts"));
        Assert.Contains("owner\\'s test 10:", vInstalled, StringComparison.Ordinal);
        Assert.Contains("owner\\'s test 11:", vInstalled, StringComparison.Ordinal);
        foreach (var vAgent in new[] { "'analyst'", "'architect'", "'flow-master'", "'verifier'" })
        {
            Assert.Contains($"talkTo(vMain, {vAgent})", vInstalled, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// REQ-NFR-019 (BRD-281, owner's test of 23 Nov 2026): part 1 of the owner's test is the owner's own case (open an
    /// empty folder, New project, look at the disk), and every folder the owner's test makes is under this repository's
    /// bin folder: nothing beside the owner's projects, nothing on the root of a drive.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-019 the owner's test starts with the empty-folder case and makes every folder under this repository's bin folder")]
    public void OwnerTestFoldersAreUnderTheRepositoryBinFolder()
    {
        var vGuide = File.ReadAllText(Path.Combine(Root(), "docs", "Chatur-UsageGuide.md"));
        var vOne = Section(vGuide, "### Owner's test 1:");
        foreach (var vWord in new[] { "Open a folder", "File ▸ New project", "Will be created in", @"Checking-Chatur.slnx", @"Checking-Chatur\Checking-Chatur.csproj" })
        {
            Assert.Contains(vWord, vOne, StringComparison.Ordinal);
        }

        var vStart = vGuide.IndexOf("### Owner's test 1:", StringComparison.Ordinal);
        var vEnd = vGuide.IndexOf("\n### If the key is refused", vStart, StringComparison.Ordinal);
        var vOwnerTest = vEnd < 0 ? vGuide[vStart..] : vGuide[vStart..vEnd];
        var vPaths = System.Text.RegularExpressions.Regex.Matches(vOwnerTest, @"[A-Za-z]:\\[^\s`""',;)]+").Select(m => m.Value).ToArray();
        Assert.NotEmpty(vPaths);
        Assert.All(vPaths, p => Assert.StartsWith(@"C:\1MyCode\Chatur", p, StringComparison.OrdinalIgnoreCase));
        // The repository itself is named once (the measurements part opens it); every other path is its bin folder or under it.
        Assert.All(
            vPaths.Select(p => p.TrimEnd('.')).Where(p => !string.Equals(p, @"C:\1MyCode\Chatur", StringComparison.OrdinalIgnoreCase)),
            p => Assert.StartsWith(@"C:\1MyCode\Chatur\bin\", p + @"\", StringComparison.OrdinalIgnoreCase));
        for (var vPart = 1; vPart <= 9; vPart++)
        {
            Assert.Contains(@"C:\1MyCode\Chatur\bin", Section(vGuide, $"### Owner's test {vPart}:"), StringComparison.OrdinalIgnoreCase);
        }

        var vInstalled = File.ReadAllText(Path.Combine(Root(), "tests", "verify", "installed-owner-test.spec.ts"));
        Assert.Contains("owner\\'s test 1: the installed Chatur starts on Nothing is open yet; with an empty folder open", vInstalled, StringComparison.Ordinal);
        Assert.Contains("no folder of the same name inside itself", vInstalled, StringComparison.Ordinal);
    }

    /// <summary>
    /// REQ-NFR-018 (BRD-280): New project's tests compare the exact files on disk for each starting point: nothing
    /// open, an empty folder, a folder that is not empty, a folder with projects and no solution, a folder with a
    /// solution. A test that only asks whether some solution file exists is what let the owner's case through.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-018 the New project tests compare the exact files on disk for every starting point")]
    public void NewProjectTestsCompareExactPaths()
    {
        var vStates = File.ReadAllText(Path.Combine(Root(), "tests", "verify", "new-project-states.spec.ts"));
        foreach (var vStart in new[]
                 {
                     "with nothing open, New project",
                     "with an empty folder open, New project",
                     "a folder that is open and holds only a docs folder",
                     "with a folder open that has a project and no solution",
                     "with a folder open that has a solution"
                 })
        {
            Assert.Contains(vStart, vStates, StringComparison.Ordinal);
        }

        Assert.True(System.Text.RegularExpressions.Regex.Matches(vStates, @"expect\(disk\(v\w+\)[^)]*\)\.toEqual\(").Count >= 5, "each starting point compares the whole file list");
        Assert.DoesNotContain(".some((f) => /\\.slnx?$/.test(f))", vStates, StringComparison.Ordinal);

        var vUnit = File.ReadAllText(Path.Combine(Root(), "tests", "unit", "Chatur.Tests", "Projects", "ProjectTemplateActionsTests.cs"));
        Assert.True(System.Text.RegularExpressions.Regex.Matches(vUnit, @"DisplayName = ""REQ-NFR-018 ").Count >= 5, "five unit tests carry the row");
    }

    private static string Section(string aText, string aHeading)
    {
        var vStart = aText.IndexOf(aHeading, StringComparison.Ordinal);
        Assert.True(vStart >= 0, $"the UsageGuide has {aHeading}");
        var vEnd = aText.IndexOf("\n### ", vStart + aHeading.Length, StringComparison.Ordinal);
        return vEnd < 0 ? aText[vStart..] : aText[vStart..vEnd];
    }
}
