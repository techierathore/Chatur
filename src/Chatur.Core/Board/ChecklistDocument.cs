using System.Text.RegularExpressions;
using Chatur.Core.Actions;

namespace Chatur.Core.Board;

/// <summary>
/// Reads the requirements out of a checklist file's text and rewrites one row's status in it. Pure
/// text in, text out, so the file format is tested without a disk (REQ-FN-056, REQ-FN-057).
/// </summary>
public static partial class ChecklistDocument
{
    private const string TableHeading = "## Requirements Status";

    [GeneratedRegex(@"^## Page: (?<name>.+?) \(`(?<route>[^`]*)`\)")]
    private static partial Regex PageHeading();

    [GeneratedRegex(@"^- \*\*(?<id>REQ-[A-Z]+-\d+)\*\*")]
    private static partial Regex DetailStart();

    [GeneratedRegex(@"\*Mockup:\*\s+(?<path>\S+)")]
    private static partial Regex MockupPart();

    [GeneratedRegex(@"^\s+-\s+\*Acceptance:\*\s*(?<text>.*)$")]
    private static partial Regex AcceptanceLine();

    [GeneratedRegex(@"\s+—\s+\*\*[^*]+\*\*\s*$")]
    private static partial Regex StatusSuffix();

    [GeneratedRegex(@"^REQ-(?<type>[A-Z]+)-\d+$")]
    private static partial Regex RequirementId();

    /// <summary>The phase a checklist file name belongs to: <c>-P2-Checklist.md</c> is 2, a plain <c>-Checklist.md</c> is 1.</summary>
    /// <param name="aFileName">The checklist's file name.</param>
    public static int PhaseOf(string aFileName)
    {
        var vMatch = Regex.Match(aFileName, @"-P(?<n>\d+)-Checklist\.md$", RegexOptions.IgnoreCase);
        return vMatch.Success ? int.Parse(vMatch.Groups["n"].Value, System.Globalization.CultureInfo.InvariantCulture) : 1;
    }

    /// <summary>Folds the checklist's own status word into one of the four <see cref="BoardStatuses"/>; a word that fits none comes back as it is.</summary>
    /// <param name="aFileStatus">The status cell's text.</param>
    public static string ToBoardStatus(string aFileStatus)
    {
        var vWord = aFileStatus.Replace("*", string.Empty, StringComparison.Ordinal).Trim().ToLowerInvariant();
        return vWord switch
        {
            "not started" => BoardStatuses.Planned,
            "in progress" or "implemented" or "partial" or "needs re-verify" => BoardStatuses.InProgress,
            "verified" or "done (pre-existing)" => BoardStatuses.Verified,
            "blocked" or "fail" => BoardStatuses.Blocked,
            _ => aFileStatus.Trim(),
        };
    }

    /// <summary>The checklist's own word for a board status, as it is written into the status cell.</summary>
    /// <param name="aBoardStatus">One of <see cref="BoardStatuses.All"/>.</param>
    /// <exception cref="ArgumentException">The status is not one of the four.</exception>
    public static string ToFileStatus(string aBoardStatus) => aBoardStatus switch
    {
        BoardStatuses.Planned => "Not Started",
        BoardStatuses.InProgress => "In Progress",
        BoardStatuses.Verified => "Verified",
        BoardStatuses.Blocked => "Blocked",
        _ => throw new ArgumentException($"'{aBoardStatus}' is not a board status.", nameof(aBoardStatus)),
    };

    /// <summary>Every requirement in the file's text, in file order.</summary>
    /// <param name="aText">The checklist file's full text.</param>
    /// <param name="aRelativePath">The file's path relative to the project folder.</param>
    /// <param name="aPhase">The phase the file belongs to.</param>
    public static IReadOnlyList<RequirementItem> Parse(string aText, string aRelativePath, int aPhase)
    {
        var vLines = aText.Split('\n').Select(aLine => aLine.TrimEnd('\r')).ToArray();
        var vDetails = ReadDetails(vLines);
        var vItems = new List<RequirementItem>();
        var vInTable = false;
        foreach (var vLine in vLines)
        {
            if (vLine.StartsWith("## ", StringComparison.Ordinal))
            {
                vInTable = vLine.StartsWith(TableHeading, StringComparison.Ordinal);
                continue;
            }

            var vItem = vInTable ? ToItem(vLine, aRelativePath, aPhase, vDetails) : null;
            if (vItem is not null)
            {
                vItems.Add(vItem);
            }
        }

        return vItems;
    }

    /// <summary>
    /// Returns the file's text with one requirement's status changed. The status cell takes the
    /// checklist's own word; the percentage follows (Not Started 0, Verified 100, In Progress 50 when it
    /// was 0 or 100); the detail bullet's trailing status word, where the file has one, follows too.
    /// Every other character of the text is unchanged, line endings included.
    /// </summary>
    /// <param name="aText">The checklist file's full text.</param>
    /// <param name="aRequirementId">The requirement to change.</param>
    /// <param name="aBoardStatus">One of <see cref="BoardStatuses.All"/>.</param>
    /// <exception cref="ArgumentException">The status is not one of the four, or the file has no row for the requirement.</exception>
    public static string WithStatus(string aText, string aRequirementId, string aBoardStatus)
    {
        var vFileStatus = ToFileStatus(aBoardStatus);
        return Rewrite(aText, aRequirementId, vFileStatus, aLine => RewriteRow(aLine, vFileStatus));
    }

    /// <summary>
    /// Returns the file's text with one requirement's verdict written: the status cell takes the
    /// checklist's own word, the percentage cell the given percentage, and the remark is appended to the
    /// Remarks cell after what is already there. The detail bullet's trailing status word follows. Every
    /// other character of the text is unchanged, line endings included (REQ-UI-048).
    /// </summary>
    /// <param name="aText">The checklist file's full text.</param>
    /// <param name="aRequirementId">The requirement graded.</param>
    /// <param name="aFileStatus">The checklist's own status word, e.g. <c>Verified</c>.</param>
    /// <param name="aPercent">The percentage, written as <c>NN%</c>.</param>
    /// <param name="aRemark">The dated remark to append; a pipe in it is escaped.</param>
    /// <exception cref="ArgumentException">The file has no row for the requirement.</exception>
    public static string WithVerdict(string aText, string aRequirementId, string aFileStatus, int aPercent, string aRemark) =>
        Rewrite(aText, aRequirementId, aFileStatus, aLine =>
        {
            var vCells = SplitCells(aLine);
            vCells[2] = aFileStatus;
            vCells[3] = aPercent.ToString(System.Globalization.CultureInfo.InvariantCulture) + "%";
            var vRemark = aRemark.Replace("|", "\\|", StringComparison.Ordinal);
            vCells[4] = vCells[4].Length == 0 ? vRemark : vCells[4] + " " + vRemark;
            return "| " + string.Join(" | ", vCells) + " |";
        });

    private static string Rewrite(string aText, string aRequirementId, string aFileStatus, Func<string, string> aRowRewriter)
    {
        var vLines = aText.Split('\n');
        var vRowFound = false;
        var vInTable = false;
        for (var vIndex = 0; vIndex < vLines.Length; vIndex++)
        {
            var vLine = vLines[vIndex].TrimEnd('\r');
            var vEnding = vLines[vIndex][vLine.Length..];
            if (vLine.StartsWith("## ", StringComparison.Ordinal))
            {
                vInTable = vLine.StartsWith(TableHeading, StringComparison.Ordinal);
            }
            else if (vInTable && IsRowOf(vLine, aRequirementId))
            {
                vLines[vIndex] = aRowRewriter(vLine) + vEnding;
                vRowFound = true;
            }
            else if (vLine.StartsWith($"- **{aRequirementId}**", StringComparison.Ordinal) && StatusSuffix().IsMatch(vLine))
            {
                vLines[vIndex] = StatusSuffix().Replace(vLine, $" — **{aFileStatus}**") + vEnding;
            }
        }

        if (!vRowFound)
        {
            throw new ArgumentException($"The checklist has no row for {aRequirementId}.", nameof(aRequirementId));
        }

        return string.Join('\n', vLines);
    }

    private static bool IsRowOf(string aLine, string aRequirementId)
    {
        var vCells = SplitCells(aLine);
        return vCells.Count >= 5 && vCells[0] == aRequirementId;
    }

    private static string RewriteRow(string aLine, string aFileStatus)
    {
        var vCells = SplitCells(aLine);
        var vPercent = int.TryParse(vCells[3].TrimEnd('%'), out var vParsed) ? vParsed : 0;
        vCells[2] = aFileStatus;
        vCells[3] = aFileStatus switch
        {
            "Not Started" => "0%",
            "Verified" => "100%",
            "In Progress" when vPercent is 0 or 100 => "50%",
            _ => vCells[3],
        };
        return "| " + string.Join(" | ", vCells) + " |";
    }

    private static RequirementItem? ToItem(string aLine, string aRelativePath, int aPhase, Dictionary<string, Detail> aDetails)
    {
        var vCells = SplitCells(aLine);
        if (vCells.Count < 5 || !RequirementId().IsMatch(vCells[0]))
        {
            return null;
        }

        var vId = vCells[0];
        var vDetail = aDetails.GetValueOrDefault(vId) ?? new Detail();
        var vStatus = vCells[2].Replace("*", string.Empty, StringComparison.Ordinal).Trim();
        var vAgent = RequirementAgents.ForType(RequirementId().Match(vId).Groups["type"].Value);
        _ = int.TryParse(vCells[3].TrimEnd('%'), out var vPercent);
        return new RequirementItem(
            vId,
            "REQ-" + RequirementId().Match(vId).Groups["type"].Value,
            vCells[1].Replace("\\|", "|", StringComparison.Ordinal),
            vStatus,
            ToBoardStatus(vStatus),
            vPercent,
            vCells[4].Replace("\\|", "|", StringComparison.Ordinal),
            vAgent.Code,
            vAgent.Name,
            aPhase,
            aRelativePath,
            vDetail.Acceptance,
            vDetail.ScreenName,
            vDetail.ScreenRoute,
            vDetail.Mockup);
    }

    private static Dictionary<string, Detail> ReadDetails(string[] aLines)
    {
        var vDetails = new Dictionary<string, Detail>(StringComparer.Ordinal);
        var vName = string.Empty;
        string? vRoute = null;
        Detail? vCurrent = null;
        foreach (var vLine in aLines)
        {
            var vPage = PageHeading().Match(vLine);
            if (vLine.StartsWith("## ", StringComparison.Ordinal))
            {
                vName = vPage.Success ? vPage.Groups["name"].Value : string.Empty;
                vRoute = vPage.Success ? vPage.Groups["route"].Value : null;
                vCurrent = null;
                continue;
            }

            var vStart = DetailStart().Match(vLine);
            if (vStart.Success)
            {
                var vMockup = MockupPart().Match(vLine);
                vCurrent = new Detail { ScreenName = vName, ScreenRoute = vRoute, Mockup = vMockup.Success ? vMockup.Groups["path"].Value : null };
                vDetails[vStart.Groups["id"].Value] = vCurrent;
                continue;
            }

            var vAcceptance = AcceptanceLine().Match(vLine);
            if (vCurrent is not null && vAcceptance.Success)
            {
                vCurrent.Acceptance = vAcceptance.Groups["text"].Value.Trim();
            }
        }

        return vDetails;
    }

    private static List<string> SplitCells(string aLine)
    {
        var vTrimmed = aLine.Trim();
        if (!vTrimmed.StartsWith('|'))
        {
            return [];
        }

        vTrimmed = vTrimmed[1..];
        if (vTrimmed.EndsWith('|') && !vTrimmed.EndsWith("\\|", StringComparison.Ordinal))
        {
            vTrimmed = vTrimmed[..^1];
        }

        return Regex.Split(vTrimmed, @"(?<!\\)\|").Select(aCell => aCell.Trim()).ToList();
    }

    private sealed class Detail
    {
        public string Acceptance { get; set; } = string.Empty;

        public string ScreenName { get; init; } = string.Empty;

        public string? ScreenRoute { get; init; }

        public string? Mockup { get; init; }
    }
}
