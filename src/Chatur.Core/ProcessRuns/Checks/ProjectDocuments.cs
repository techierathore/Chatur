using System.Globalization;
using System.Text.RegularExpressions;
using Chatur.Core.Actions;
using Chatur.Core.Board;

namespace Chatur.Core.ProcessRuns.Checks;

/// <summary>One BRD item as a BRD file holds it.</summary>
/// <param name="Number">The number after <c>BRD-</c>.</param>
/// <param name="Struck">Whether the item is struck through (removed, its id kept).</param>
/// <param name="HasAcceptance">Whether an acceptance line follows the item.</param>
public sealed record BrdItem(int Number, bool Struck, bool HasAcceptance);

/// <summary>One row of the phases file.</summary>
/// <param name="Phase">The phase number.</param>
/// <param name="Ids">Every BRD id the row's range covers.</param>
/// <param name="Status">The row's last cell.</param>
public sealed record PhaseRow(int Phase, IReadOnlySet<int> Ids, string Status);

/// <summary>One screen section of a UI design document.</summary>
/// <param name="Name">The screen's name.</param>
/// <param name="MockupPath">The mockup the section links, project-relative, or <see langword="null"/>.</param>
/// <param name="HasRegions">Whether the section lists its regions.</param>
/// <param name="MissingStates">Which of empty, loading and error the section does not describe.</param>
public sealed record DesignScreen(string Name, string? MockupPath, bool HasRegions, IReadOnlyList<string> MissingStates);

/// <summary>
/// Finds and reads a project's documents by the names the framework gives them (<c>-BRD.md</c>, <c>-Checklist.md</c>,
/// <c>Phases</c>, <c>UIDesign</c>, <c>docs/mockups/*.html</c>, the status document). Pure file reading, so the checks
/// that judge a project read the project and nothing else.
/// </summary>
public sealed partial class ProjectDocuments
{
    /// <summary>The status document's project-relative path.</summary>
    public const string StatusPath = "PROJECT-STATUS.md";

    private readonly string objRoot;

    /// <summary>Creates the reader.</summary>
    /// <param name="aRoot">The project's folder.</param>
    public ProjectDocuments(string aRoot)
    {
        objRoot = aRoot;
    }

    /// <summary>The project's folder.</summary>
    public string Root => objRoot;

    [GeneratedRegex(@"^\s*(?:[-*+]\s+|#{1,6}\s+)?(?<strike>~~)?\*{0,2}BRD-(?<n>\d+)\*{0,2}(?:~~)?(?=[\s—:.\-]|$)")]
    private static partial Regex BrdItemStart();

    [GeneratedRegex(@"BRD-(?<a>\d+)\s*(?:to|–|—|-)\s*BRD-(?<b>\d+)")]
    private static partial Regex BrdRange();

    [GeneratedRegex(@"BRD-(?<n>\d+)")]
    private static partial Regex BrdSingle();

    [GeneratedRegex(@"^###\s+Screen:\s*(?<name>[^(\r\n]+?)\s*(?:\(.*\))?\s*$", RegexOptions.Multiline)]
    private static partial Regex ScreenHeading();

    [GeneratedRegex(@"mockups/[A-Za-z0-9_.\-]+\.html")]
    private static partial Regex MockupLink();

    [GeneratedRegex(@"-P(?<n>\d+)-")]
    private static partial Regex PhaseInName();

    /// <summary>The phase a document file name belongs to: <c>-P3-</c> in the name is 3, none is 1.</summary>
    /// <param name="aPath">The file name or path.</param>
    public static int PhaseOf(string aPath)
    {
        var vMatch = PhaseInName().Match(Path.GetFileName(aPath));
        return vMatch.Success ? int.Parse(vMatch.Groups["n"].Value, CultureInfo.InvariantCulture) : 1;
    }

    /// <summary>The <c>.md</c> files directly under <c>docs/</c> whose name satisfies the filter, project-relative, in name order.</summary>
    /// <param name="aFilter">Tested against the lower-cased file name.</param>
    public IReadOnlyList<string> Docs(Func<string, bool> aFilter)
    {
        var vDocs = Path.Combine(objRoot, "docs");
        if (!Directory.Exists(vDocs))
        {
            return [];
        }

        return Directory.EnumerateFiles(vDocs, "*.md")
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(aName => aFilter(aName.ToLowerInvariant()))
            .Order(StringComparer.Ordinal)
            .Select(aName => "docs/" + aName)
            .ToList();
    }

    /// <summary>The brief documents.</summary>
    public IReadOnlyList<string> Briefs() => Docs(aName => aName.Contains("brief", StringComparison.Ordinal));

    /// <summary>The BRD documents (<c>...BRD.md</c>).</summary>
    public IReadOnlyList<string> Brds() => Docs(aName => aName.EndsWith("brd.md", StringComparison.Ordinal));

    /// <summary>The architecture documents.</summary>
    public IReadOnlyList<string> Architectures() => Docs(aName => aName.Contains("architecture", StringComparison.Ordinal));

    /// <summary>The UI design documents.</summary>
    public IReadOnlyList<string> UiDesigns() => Docs(aName => aName.Contains("uidesign", StringComparison.Ordinal) || aName.Contains("ui-design", StringComparison.Ordinal));

    /// <summary>The requirement checklists (not the deployment checklist).</summary>
    public IReadOnlyList<string> Checklists() => Docs(aName => aName.Contains("checklist", StringComparison.Ordinal) && !aName.Contains("deploy", StringComparison.Ordinal));

    /// <summary>The phases documents.</summary>
    public IReadOnlyList<string> PhasesFiles() => Docs(aName => aName.Contains("phases", StringComparison.Ordinal));

    /// <summary>The deployment checklists.</summary>
    public IReadOnlyList<string> DeployChecklists() => Docs(aName => aName.Contains("deploy", StringComparison.Ordinal) && aName.Contains("checklist", StringComparison.Ordinal));

    /// <summary>The metrics reports.</summary>
    public IReadOnlyList<string> MetricsReports() => Docs(aName => aName.Contains("metrics", StringComparison.Ordinal) && aName.Contains("report", StringComparison.Ordinal));

    /// <summary>The HTML mockups under <c>docs/mockups</c>, project-relative.</summary>
    public IReadOnlyList<string> Mockups()
    {
        var vFolder = Path.Combine(objRoot, "docs", "mockups");
        return Directory.Exists(vFolder)
            ? Directory.EnumerateFiles(vFolder, "*.html").Order(StringComparer.Ordinal).Select(aPath => "docs/mockups/" + Path.GetFileName(aPath)).ToList()
            : [];
    }

    /// <summary>Whether a project-relative file exists.</summary>
    /// <param name="aRelative">The path.</param>
    public bool Exists(string aRelative) => File.Exists(Full(aRelative));

    /// <summary>When a project-relative file was last written, or <see langword="null"/> when it does not exist.</summary>
    /// <param name="aRelative">The path.</param>
    public DateTime? WrittenUtc(string aRelative) => File.Exists(Full(aRelative)) ? File.GetLastWriteTimeUtc(Full(aRelative)) : null;

    /// <summary>A project-relative file's text, or an empty string when it does not exist.</summary>
    /// <param name="aRelative">The path.</param>
    public string Read(string aRelative) => File.Exists(Full(aRelative)) ? File.ReadAllText(Full(aRelative)) : string.Empty;

    /// <summary>The full path of a project-relative one.</summary>
    /// <param name="aRelative">The path.</param>
    public string Full(string aRelative) => Path.Combine(objRoot, aRelative.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>Resolves a path a document names (relative to the document's folder or to the project) to a project-relative one that exists, or <see langword="null"/>.</summary>
    /// <param name="aNamed">The path as written, e.g. <c>mockups/process-run.html</c>.</param>
    public string? ResolveNamed(string aNamed)
    {
        var vPath = aNamed.Trim().Trim('`', '(', ')').Replace('\\', '/');
        foreach (var vCandidate in new[] { vPath, "docs/" + vPath })
        {
            if (!vCandidate.Contains("..", StringComparison.Ordinal) && Exists(vCandidate))
            {
                return vCandidate;
            }
        }

        return null;
    }

    /// <summary>Every BRD item in a BRD file's text, in order.</summary>
    /// <param name="aText">The BRD's text.</param>
    public static IReadOnlyList<BrdItem> ParseBrd(string aText)
    {
        var vItems = new List<(int Number, bool Struck, bool Acceptance)>();
        foreach (var vLine in aText.Split('\n'))
        {
            var vStart = BrdItemStart().Match(vLine);
            if (vStart.Success)
            {
                var vStruck = vStart.Groups["strike"].Success || vLine.Contains("~~", StringComparison.Ordinal);
                vItems.Add((int.Parse(vStart.Groups["n"].Value, CultureInfo.InvariantCulture), vStruck, vLine.Contains("acceptance", StringComparison.OrdinalIgnoreCase)));
            }
            else if (vItems.Count > 0 && vLine.Contains("acceptance", StringComparison.OrdinalIgnoreCase) && !vLine.TrimStart().StartsWith('|') && !vLine.TrimStart().StartsWith('#'))
            {
                vItems[^1] = vItems[^1] with { Acceptance = true };
            }
        }

        return vItems.Select(aItem => new BrdItem(aItem.Number, aItem.Struck, aItem.Acceptance)).ToList();
    }

    /// <summary>The phase rows of a phases file's text.</summary>
    /// <param name="aText">The phases file's text.</param>
    public static IReadOnlyList<PhaseRow> ParsePhases(string aText)
    {
        var vRows = new List<PhaseRow>();
        foreach (var vLine in aText.Split('\n'))
        {
            var vTrimmed = vLine.Trim();
            if (!vTrimmed.StartsWith('|'))
            {
                continue;
            }

            var vCells = vTrimmed.Trim('|').Split('|').Select(aCell => aCell.Trim()).ToList();
            if (vCells.Count < 3 || !int.TryParse(vCells[0], out var vPhase))
            {
                continue;
            }

            var vIds = new HashSet<int>();
            foreach (var vCell in vCells.Skip(1).Where(aCell => aCell.Contains("BRD-", StringComparison.Ordinal)))
            {
                var vRest = vCell;
                foreach (Match vRange in BrdRange().Matches(vCell))
                {
                    var vFrom = int.Parse(vRange.Groups["a"].Value, CultureInfo.InvariantCulture);
                    var vTo = int.Parse(vRange.Groups["b"].Value, CultureInfo.InvariantCulture);
                    for (var vId = vFrom; vId <= vTo; vId++)
                    {
                        vIds.Add(vId);
                    }

                    vRest = vRest.Replace(vRange.Value, string.Empty, StringComparison.Ordinal);
                }

                foreach (Match vSingle in BrdSingle().Matches(vRest))
                {
                    vIds.Add(int.Parse(vSingle.Groups["n"].Value, CultureInfo.InvariantCulture));
                }
            }

            vRows.Add(new PhaseRow(vPhase, vIds, vCells[^1]));
        }

        return vRows;
    }

    /// <summary>The screen sections of a UI design document's text.</summary>
    /// <param name="aText">The UI design's text.</param>
    public static IReadOnlyList<DesignScreen> ParseScreens(string aText)
    {
        var vMatches = ScreenHeading().Matches(aText);
        var vScreens = new List<DesignScreen>();
        for (var vIndex = 0; vIndex < vMatches.Count; vIndex++)
        {
            var vStart = vMatches[vIndex].Index + vMatches[vIndex].Length;
            var vEnd = aText.Length;
            var vNext = Regex.Match(aText[vStart..], @"^#{2,3}\s", RegexOptions.Multiline);
            if (vNext.Success)
            {
                vEnd = vStart + vNext.Index;
            }

            var vBody = aText[vStart..vEnd];
            var vLink = MockupLink().Match(vBody);
            var vMissing = new[] { "empty", "loading", "error" }
                .Where(aState => !Regex.IsMatch(vBody, $@"\b{aState}\b", RegexOptions.IgnoreCase))
                .ToList();
            vScreens.Add(new DesignScreen(
                vMatches[vIndex].Groups["name"].Value.Trim(),
                vLink.Success ? "docs/" + vLink.Value : null,
                Regex.IsMatch(vBody, @"\|\s*Region\s*\|", RegexOptions.IgnoreCase),
                vMissing));
        }

        return vScreens;
    }

    /// <summary>Every requirement row of every checklist, in file order.</summary>
    public IReadOnlyList<RequirementItem> Requirements()
    {
        var vItems = new List<RequirementItem>();
        foreach (var vFile in Checklists())
        {
            vItems.AddRange(ChecklistDocument.Parse(Read(vFile), vFile, ChecklistDocument.PhaseOf(vFile)));
        }

        return vItems;
    }

    /// <summary>The newest phase's checklist, project-relative, or <see langword="null"/>.</summary>
    public string? NewestChecklist() =>
        Checklists().OrderByDescending(aFile => ChecklistDocument.PhaseOf(aFile)).ThenBy(aFile => aFile, StringComparer.Ordinal).FirstOrDefault();

    /// <summary>The checklist status with the checklist's own decoration (bold marks) removed and compared without regard to case.</summary>
    /// <param name="aFileStatus">The status cell's text.</param>
    public static string StatusKey(string aFileStatus) => aFileStatus.Replace("*", string.Empty, StringComparison.Ordinal).Trim().ToLowerInvariant();

    /// <summary>Every BRD number any BRD file defines.</summary>
    public IReadOnlySet<int> DefinedBrdNumbers() =>
        Brds().SelectMany(aFile => ParseBrd(Read(aFile))).Select(aItem => aItem.Number).ToHashSet();
}
