using System.Globalization;
using System.Text.RegularExpressions;
using Chatur.Core.Actions;
using Chatur.Core.Board;

namespace Chatur.Core.ProcessRuns.Checks;

/// <summary>
/// The checks that judge the project's checklists, status and phases files: build-phase, fix-issues, handoff-phase
/// and refresh-status (REQ-FN-093, 094, 095, 096). They read the files on disk, never the agent's account of them.
/// </summary>
public static partial class StatusChecks
{
    private static readonly string[] StillOpen = ["not started", "in progress", "partial"];
    private static readonly string[] Closed = ["verified", "done (pre-existing)", "n/a"];

    [GeneratedRegex(@"^\|\s*(?<s>[^|]+?)\s*\|\s*(?<n>\d+)\s*\|", RegexOptions.Multiline)]
    private static partial Regex CountTableRow();

    [GeneratedRegex(@"^[\s\-*]*(?<s>[A-Za-z][A-Za-z ()/\-]*?)\s*:\s*\*{0,2}(?<n>\d+)\*{0,2}\s*$", RegexOptions.Multiline)]
    private static partial Regex CountColonLine();

    /// <summary>build-phase step 2, handoff-phase step 1, refresh-status step 2: the project has a checklist with rows to read.</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome ChecklistReadable(CheckContext aContext)
    {
        var vRows = aContext.Documents.Requirements();
        return vRows.Count == 0
            ? CheckOutcome.Fail("the checklist can be read", "the project has no checklist with requirement rows in docs/.")
            : CheckOutcome.Pass($"{aContext.Documents.Checklists().Count} checklists read, {vRows.Count} requirement rows.");
    }

    /// <summary>build-phase step 3: the cluster is the open rows of the phase, and each has an acceptance line to build from (REQ-FN-093).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome ClusterIsOpenRows(CheckContext aContext)
    {
        var vRows = PhaseRows(aContext);
        if (vRows is null)
        {
            return CheckOutcome.Fail("the cluster names only requirements of this phase", "the project has no checklist.");
        }

        var vOpen = vRows.Where(aRow => StillOpen.Contains(ProjectDocuments.StatusKey(aRow.FileStatus))).ToList();
        var vProblems = vOpen.Where(aRow => aRow.Acceptance.Length == 0).Select(aRow => $"{aRow.Id} has no acceptance line").ToList();
        return CheckOutcome.From("the cluster names only requirements of this phase", vProblems, $"{vOpen.Count} open rows in {aContext.Documents.NewestChecklist()}, each with an acceptance line.");
    }

    /// <summary>build-phase step 10: every open row is Implemented and a test carries its id (REQ-FN-093).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome OpenRowsImplemented(CheckContext aContext)
    {
        var vRows = PhaseRows(aContext);
        if (vRows is null)
        {
            return CheckOutcome.Fail("every open row is Implemented", "the project has no checklist.");
        }

        var vProblems = vRows.Where(aRow => StillOpen.Contains(ProjectDocuments.StatusKey(aRow.FileStatus)))
            .Select(aRow => $"{aRow.Id} is still {aRow.FileStatus}").ToList();
        var vCarried = IdsInTests(aContext.Folder);
        var vImplemented = vRows.Where(aRow => ProjectDocuments.StatusKey(aRow.FileStatus) == "implemented").ToList();
        vProblems.AddRange(vImplemented.Where(aRow => !vCarried.Contains(aRow.Id)).Select(aRow => $"no test under tests/ carries {aRow.Id}"));
        return CheckOutcome.From("every open row is Implemented", vProblems, $"No open row is left in {aContext.Documents.NewestChecklist()}; {vImplemented.Count} Implemented rows each have a test carrying their id.");
    }

    /// <summary>fix-issues step 7: no row verification left FAIL or Needs re-verify is still one (REQ-FN-094).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome FailedRowsImplemented(CheckContext aContext)
    {
        var vRows = aContext.Documents.Requirements();
        if (vRows.Count == 0)
        {
            return CheckOutcome.Fail("each failed row is Implemented again", "the project has no checklist with requirement rows.");
        }

        var vLeft = vRows.Where(aRow => ProjectDocuments.StatusKey(aRow.FileStatus) is "fail" or "needs re-verify").Select(aRow => $"{aRow.Id} is still {aRow.FileStatus}").ToList();
        return CheckOutcome.From("each failed row is Implemented again", vLeft, "No row is left FAIL or Needs re-verify.");
    }

    /// <summary>handoff-phase step 2: no row of the phase is open (REQ-FN-095).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome NoRowOpen(CheckContext aContext)
    {
        var vRows = PhaseRows(aContext);
        if (vRows is null)
        {
            return CheckOutcome.Fail("the phase has no open requirement", "the project has no checklist.");
        }

        var vOpen = vRows.Where(aRow =>
        {
            var vStatus = ProjectDocuments.StatusKey(aRow.FileStatus);
            return !Closed.Contains(vStatus) && !(vStatus == "blocked" && aRow.Remarks.Trim().Length > 0);
        }).Select(aRow => $"{aRow.Id} is {aRow.FileStatus}").ToList();
        return CheckOutcome.From("the phase has no open requirement", vOpen, $"All {vRows.Count} rows of {aContext.Documents.NewestChecklist()} are Verified, Done (pre-existing), N/A, or Blocked with a reason.");
    }

    /// <summary>handoff-phase step 4: the status document carries a hand-off dated today and the phases file marks the phase done (REQ-FN-095).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome HandoffRecorded(CheckContext aContext)
    {
        var vDocuments = aContext.Documents;
        var vProblems = new List<string>();
        var vToday = new[] { aContext.TodayUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), aContext.TodayUtc.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) };
        if (!vDocuments.Exists(ProjectDocuments.StatusPath))
        {
            vProblems.Add("the status document does not exist");
        }
        else
        {
            var vStatus = vDocuments.Read(ProjectDocuments.StatusPath);
            var vHandoffLines = vStatus.Split('\n').Where(aLine => Regex.IsMatch(aLine, @"hand-?\s?off", RegexOptions.IgnoreCase)).ToList();
            if (vHandoffLines.Count == 0)
            {
                vProblems.Add("the status document has no hand-off entry");
            }
            else if (!vHandoffLines.Any(aLine => vToday.Any(aDay => aLine.Contains(aDay, StringComparison.Ordinal))))
            {
                vProblems.Add($"no hand-off entry in the status document is dated {vToday[0]}");
            }
        }

        var vNewest = vDocuments.NewestChecklist();
        var vPhase = vNewest is null ? (int?)null : ChecklistDocument.PhaseOf(vNewest);
        var vPhases = vDocuments.PhasesFiles().FirstOrDefault();
        if (vPhases is null)
        {
            vProblems.Add("the project has no phases file");
        }
        else if (vPhase is null)
        {
            vProblems.Add("the project has no checklist to say which phase is closing");
        }
        else
        {
            var vRow = ProjectDocuments.ParsePhases(vDocuments.Read(vPhases)).FirstOrDefault(aRow => aRow.Phase == vPhase);
            if (vRow is null)
            {
                vProblems.Add($"{vPhases} has no row for phase {vPhase}");
            }
            else if (!vRow.Status.Contains("done", StringComparison.OrdinalIgnoreCase))
            {
                vProblems.Add($"{vPhases} does not mark phase {vPhase} done (it says \"{vRow.Status}\")");
            }
        }

        return CheckOutcome.From("the status and phases files record the hand-off", vProblems, $"The status document records the hand-off of {vToday[0]} and the phases file marks phase {vPhase} done.");
    }

    /// <summary>refresh-status step 3: the counts per status add up to the number of rows (REQ-FN-096).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome CountsAddUp(CheckContext aContext)
    {
        var vRows = aContext.Documents.Requirements();
        if (vRows.Count == 0)
        {
            return CheckOutcome.Fail("the counts add up to the requirement total", "the project has no checklist with requirement rows.");
        }

        var vCounts = CountByStatus(vRows);
        var vSum = vCounts.Values.Sum();
        return vSum == vRows.Count
            ? CheckOutcome.Pass($"{string.Join(", ", vCounts.Select(aCount => $"{aCount.Value} {aCount.Key}"))} add up to the {vRows.Count} rows.")
            : CheckOutcome.Fail("the counts add up to the requirement total", $"the counts add up to {vSum} but there are {vRows.Count} rows.");
    }

    /// <summary>refresh-status step 4: the status document was written in the run and counts each status as the checklists do (REQ-FN-096).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome StatusMatchesChecklists(CheckContext aContext)
    {
        var vDocuments = aContext.Documents;
        var vWritten = vDocuments.WrittenUtc(ProjectDocuments.StatusPath);
        if (vWritten is null)
        {
            return CheckOutcome.Fail("the status matches the checklists", "the status document does not exist.");
        }

        if (vWritten < aContext.RunStartedUtc.AddSeconds(-2))
        {
            return CheckOutcome.Fail("the status matches the checklists", "the status document was not written during this run.");
        }

        var vAll = vDocuments.Requirements();
        var vNewestFile = vDocuments.NewestChecklist();
        var vNewest = vAll.Where(aRow => aRow.ChecklistFile == vNewestFile).ToList();
        var vReported = ReportedCounts(vDocuments.Read(ProjectDocuments.StatusPath));
        if (vReported.Count == 0)
        {
            return CheckOutcome.Fail("the status matches the checklists", "the status document gives no count per status (a table row such as \"| Implemented | 7 |\").");
        }

        // The status document counts the phase being worked or every checklist; either way of counting is the checklists' own.
        var vAgainstAll = Disagreements(vReported, CountByStatus(vAll));
        var vAgainstNewest = Disagreements(vReported, CountByStatus(vNewest));
        return vAgainstAll.Count == 0 || vAgainstNewest.Count == 0
            ? CheckOutcome.Pass($"The status document counts {string.Join(", ", vReported.Select(aCount => $"{aCount.Value} {aCount.Key}"))}, as the checklists do.")
            : CheckOutcome.From("the status matches the checklists", vAgainstNewest, string.Empty);
    }

    private static List<RequirementItem>? PhaseRows(CheckContext aContext)
    {
        var vFile = aContext.Documents.NewestChecklist();
        return vFile is null ? null : aContext.Documents.Requirements().Where(aRow => aRow.ChecklistFile == vFile).ToList();
    }

    private static Dictionary<string, int> CountByStatus(IEnumerable<RequirementItem> aRows) =>
        aRows.GroupBy(aRow => ProjectDocuments.StatusKey(aRow.FileStatus)).ToDictionary(aGroup => aGroup.Key, aGroup => aGroup.Count());

    private static Dictionary<string, int> ReportedCounts(string aText)
    {
        var vCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Match vMatch in CountTableRow().Matches(aText))
        {
            vCounts[ProjectDocuments.StatusKey(vMatch.Groups["s"].Value)] = int.Parse(vMatch.Groups["n"].Value, CultureInfo.InvariantCulture);
        }

        foreach (Match vMatch in CountColonLine().Matches(aText))
        {
            vCounts.TryAdd(ProjectDocuments.StatusKey(vMatch.Groups["s"].Value), int.Parse(vMatch.Groups["n"].Value, CultureInfo.InvariantCulture));
        }

        return vCounts;
    }

    private static List<string> Disagreements(Dictionary<string, int> aReported, Dictionary<string, int> aActual)
    {
        var vKnown = new[] { "not started", "in progress", "implemented", "partial", "needs re-verify", "fail", "verified", "blocked", "done (pre-existing)", "n/a" };
        var vProblems = new List<string>();
        foreach (var (vStatus, vCount) in aActual)
        {
            if (!aReported.TryGetValue(vStatus, out var vGiven))
            {
                vProblems.Add($"the status document gives no count for {vStatus} (the checklists have {vCount})");
            }
            else if (vGiven != vCount)
            {
                vProblems.Add($"the status document counts {vGiven} {vStatus} but the checklists have {vCount}");
            }
        }

        vProblems.AddRange(aReported.Where(aEntry => vKnown.Contains(aEntry.Key) && !aActual.ContainsKey(aEntry.Key) && aEntry.Value != 0)
            .Select(aEntry => $"the status document counts {aEntry.Value} {aEntry.Key} but the checklists have none"));
        return vProblems;
    }

    private static HashSet<string> IdsInTests(string aFolder)
    {
        var vIds = new HashSet<string>(StringComparer.Ordinal);
        var vTests = Path.Combine(aFolder, "tests");
        if (!Directory.Exists(vTests))
        {
            return vIds;
        }

        var vSkipped = new[] { "bin", "obj", "node_modules", ".artifacts", "TestResults" };
        var vPending = new Stack<string>();
        vPending.Push(vTests);
        while (vPending.Count > 0)
        {
            var vFolder = vPending.Pop();
            foreach (var vFile in Directory.EnumerateFiles(vFolder).Where(aFile => new FileInfo(aFile).Length < 2_000_000))
            {
                if (IsTextFile(vFile))
                {
                    foreach (var vId in RequirementIdScanner.Scan(File.ReadAllText(vFile)))
                    {
                        vIds.Add(vId);
                    }
                }
            }

            foreach (var vSub in Directory.EnumerateDirectories(vFolder).Where(aSub => !vSkipped.Contains(Path.GetFileName(aSub), StringComparer.OrdinalIgnoreCase)))
            {
                vPending.Push(vSub);
            }
        }

        return vIds;
    }

    private static bool IsTextFile(string aPath) =>
        Path.GetExtension(aPath).ToLowerInvariant() is ".cs" or ".ts" or ".js" or ".py" or ".java" or ".go" or ".rb" or ".razor" or ".json" or ".sh" or ".spec" or ".kt" or ".swift" or ".rs" or ".php";
}
