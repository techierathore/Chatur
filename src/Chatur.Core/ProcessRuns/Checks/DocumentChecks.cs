using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Chatur.Core.Actions;
using Chatur.Core.Board;

namespace Chatur.Core.ProcessRuns.Checks;

/// <summary>
/// The checks that judge the documents a process leaves on disk: day1-greenfield, split-brd, deploy-checklist,
/// mockups, amend-docs, triage-issues and metrics-report (REQ-FN-100, 092, 101, 102, 097, 098, 099). Each reads the
/// project's files and nothing else; a missing file is a reason to fail, never a pass.
/// </summary>
public static partial class DocumentChecks
{
    private static readonly string[] DeploySections = ["before", "deploy", "after", "rollback", "secrets", "proven"];

    [GeneratedRegex(@"^\s*[-*]\s+\[[ xX]\]")]
    private static partial Regex CheckboxItem();

    [GeneratedRegex(@"\b(?:who|owner|by)\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex NamesWho();

    [GeneratedRegex(@"^#{1,6}\s+(?<h>.+)$", RegexOptions.Multiline)]
    private static partial Regex Heading();

    [GeneratedRegex(@"[A-Za-z0-9_./\-]+\.(?:md|html)")]
    private static partial Regex DocumentName();

    [GeneratedRegex(@"^[\s|*_\-]*(?<k>runs|tokens|failures)\b[\s|*_:=\-]*(?<n>\d[\d,_]*)", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex ReportTotal();

    [GeneratedRegex(@"^- \*\*(?<id>REQ-[A-Z]+-\d+)\*\*(?<rest>.*)$", RegexOptions.Multiline)]
    private static partial Regex ChecklistDetail();

    /// <summary>day1-greenfield, step 3: the brief exists and is not empty (REQ-FN-100).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome BriefExists(CheckContext aContext)
    {
        var vBriefs = aContext.Documents.Briefs().Where(aFile => aContext.Documents.Read(aFile).Trim().Length > 0).ToList();
        return vBriefs.Count == 0
            ? CheckOutcome.Fail("the project brief exists", "no brief with text was found in docs/ (a file whose name contains \"Brief\").")
            : CheckOutcome.Pass($"The brief is {vBriefs[0]}.");
    }

    /// <summary>day1-greenfield, step 4: the brief names an audience, a problem and a measure of success (REQ-FN-100).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome BriefNamesThree(CheckContext aContext)
    {
        var vBrief = aContext.Documents.Briefs().FirstOrDefault(aFile => aContext.Documents.Read(aFile).Trim().Length > 0);
        if (vBrief is null)
        {
            return CheckOutcome.Fail("the brief names an audience, a problem and a measure of success", "there is no brief to read.");
        }

        var vText = aContext.Documents.Read(vBrief);
        var vMissing = new List<string>();
        if (!vText.Contains("audience", StringComparison.OrdinalIgnoreCase))
        {
            vMissing.Add("no audience");
        }

        if (!vText.Contains("problem", StringComparison.OrdinalIgnoreCase))
        {
            vMissing.Add("no problem");
        }

        if (!vText.Contains("success", StringComparison.OrdinalIgnoreCase) && !vText.Contains("measure", StringComparison.OrdinalIgnoreCase))
        {
            vMissing.Add("no measure of success");
        }

        return CheckOutcome.From("the brief names an audience, a problem and a measure of success", vMissing.Select(aMissing => $"{vBrief} has {aMissing}").ToList(), $"{vBrief} names an audience, a problem and a measure of success.");
    }

    /// <summary>day1-greenfield, step 5: the BRD exists, every item has an acceptance line and no id is repeated (REQ-FN-100).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome BrdRules(CheckContext aContext)
    {
        var vBrds = aContext.Documents.Brds();
        if (vBrds.Count == 0)
        {
            return CheckOutcome.Fail("the BRD exists and follows the document rules", "no BRD was found in docs/ (a file ending \"-BRD.md\").");
        }

        var vProblems = new List<string>();
        var vSeen = new HashSet<int>();
        var vItems = 0;
        foreach (var vFile in vBrds)
        {
            foreach (var vItem in ProjectDocuments.ParseBrd(aContext.Documents.Read(vFile)))
            {
                vItems++;
                if (!vSeen.Add(vItem.Number))
                {
                    vProblems.Add($"BRD-{vItem.Number} appears twice");
                }

                if (!vItem.Struck && !vItem.HasAcceptance)
                {
                    vProblems.Add($"BRD-{vItem.Number} in {vFile} has no acceptance line");
                }
            }
        }

        if (vItems == 0)
        {
            vProblems.Add("the BRD holds no BRD-n item");
        }

        return CheckOutcome.From("the BRD exists and follows the document rules", vProblems, $"The BRD holds {vItems} items, each with an acceptance line, none repeated.");
    }

    /// <summary>day1-greenfield, step 8: architecture, UI design, a mockup per screen and the checklist exist, and the ids agree (REQ-FN-100).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome Day1Documents(CheckContext aContext)
    {
        var vDocuments = aContext.Documents;
        var vProblems = new List<string>();
        if (vDocuments.Architectures().Count == 0)
        {
            vProblems.Add("no Architecture document");
        }

        var vDesigns = vDocuments.UiDesigns();
        if (vDesigns.Count == 0)
        {
            vProblems.Add("no UI design document");
        }

        var vScreens = vDesigns.SelectMany(aFile => ProjectDocuments.ParseScreens(vDocuments.Read(aFile)).Select(aScreen => (File: aFile, Screen: aScreen))).ToList();
        if (vDesigns.Count > 0 && vScreens.Count == 0)
        {
            vProblems.Add("the UI design has no \"### Screen:\" section");
        }

        foreach (var (vFile, vScreen) in vScreens)
        {
            if (vScreen.MockupPath is null || !vDocuments.Exists(vScreen.MockupPath))
            {
                vProblems.Add($"screen \"{vScreen.Name}\" in {vFile} has no mockup on disk");
            }
        }

        var vBrdNumbers = vDocuments.DefinedBrdNumbers();
        var vRows = vDocuments.Requirements();
        if (vDocuments.Checklists().Count == 0 || vRows.Count == 0)
        {
            vProblems.Add("no checklist with requirement rows");
        }
        else
        {
            var vNamed = ChecklistBrdNumbers(vDocuments);
            foreach (var vNumber in vBrdNumbers.Order().Where(aNumber => !vNamed.Contains(aNumber)))
            {
                vProblems.Add($"BRD-{vNumber} has no checklist row");
            }

            foreach (var vDuplicate in vRows.GroupBy(aRow => aRow.Id, StringComparer.Ordinal).Where(aGroup => aGroup.Count() > 1))
            {
                vProblems.Add($"{vDuplicate.Key} is a checklist row twice");
            }

            vProblems.AddRange(UiRowsWithoutMockup(vDocuments, vRows));
        }

        return CheckOutcome.From(
            "the documents are all there and agree",
            vProblems,
            $"Architecture, UI design ({vScreens.Count} screens with mockups) and the checklist ({vRows.Count} rows) are on disk and agree with the BRD.");
    }

    /// <summary>split-brd, step 2: the phases file gives every phase a BRD range (REQ-FN-092).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome PhasesHaveRanges(CheckContext aContext)
    {
        var vDocuments = aContext.Documents;
        var vFile = vDocuments.PhasesFiles().FirstOrDefault();
        if (vFile is null)
        {
            return OnePhaseOnly(vDocuments, "every phase has a BRD range");
        }

        var vRows = ProjectDocuments.ParsePhases(vDocuments.Read(vFile));
        var vProblems = vRows.Count == 0 ? ["the phases file has no phase row"] : vRows.Where(aRow => aRow.Ids.Count == 0).Select(aRow => $"phase {aRow.Phase} has no BRD range").ToList();
        return CheckOutcome.From("every phase has a BRD range", vProblems, $"{vFile} gives each of its {vRows.Count} phases a BRD range.");
    }

    /// <summary>split-brd, step 3: every BRD id lands in exactly one phase (REQ-FN-092).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome EveryIdInOnePhase(CheckContext aContext)
    {
        var vDocuments = aContext.Documents;
        var vFile = vDocuments.PhasesFiles().FirstOrDefault();
        if (vFile is null)
        {
            return OnePhaseOnly(vDocuments, "every BRD id is in exactly one phase");
        }

        var vRows = ProjectDocuments.ParsePhases(vDocuments.Read(vFile));
        var vProblems = new List<string>();
        foreach (var vNumber in vDocuments.DefinedBrdNumbers().Order())
        {
            var vIn = vRows.Where(aRow => aRow.Ids.Contains(vNumber)).Select(aRow => aRow.Phase).ToList();
            if (vIn.Count == 0)
            {
                vProblems.Add($"BRD-{vNumber} is in no phase");
            }
            else if (vIn.Count > 1)
            {
                vProblems.Add($"BRD-{vNumber} is in phases {string.Join(" and ", vIn)}");
            }
        }

        return CheckOutcome.From("every BRD id is in exactly one phase", vProblems, $"Every BRD id lands in exactly one of the {vRows.Count} phases.");
    }

    /// <summary>split-brd, step 5: each phase has its BRD and checklist, and the phase files hold exactly the ids the phases file gives them (REQ-FN-092).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome PhaseFilesWhole(CheckContext aContext)
    {
        var vDocuments = aContext.Documents;
        var vFile = vDocuments.PhasesFiles().FirstOrDefault();
        if (vFile is null)
        {
            return OnePhaseOnly(vDocuments, "each phase has its BRD and checklist");
        }

        var vRows = ProjectDocuments.ParsePhases(vDocuments.Read(vFile));
        var vProblems = new List<string>();
        foreach (var vRow in vRows)
        {
            var vBrd = vDocuments.Brds().FirstOrDefault(aBrd => ProjectDocuments.PhaseOf(aBrd) == vRow.Phase);
            if (vBrd is null)
            {
                vProblems.Add($"phase {vRow.Phase} has no BRD file");
            }
            else
            {
                var vDefined = ProjectDocuments.ParseBrd(vDocuments.Read(vBrd)).Select(aItem => aItem.Number).ToHashSet();
                vProblems.AddRange(vDefined.Where(aNumber => !vRow.Ids.Contains(aNumber)).Order().Select(aNumber => $"BRD-{aNumber} is in {vBrd} but not in phase {vRow.Phase}'s range"));
                vProblems.AddRange(vRow.Ids.Where(aNumber => !vDefined.Contains(aNumber)).Order().Select(aNumber => $"BRD-{aNumber} is in phase {vRow.Phase}'s range but {vBrd} does not hold it"));
            }

            if (!vDocuments.Checklists().Any(aChecklist => ProjectDocuments.PhaseOf(aChecklist) == vRow.Phase))
            {
                vProblems.Add($"phase {vRow.Phase} has no checklist file");
            }
        }

        return CheckOutcome.From("each phase has its BRD and checklist", vProblems, $"Each of the {vRows.Count} phases has its BRD and checklist, holding the ids it was given.");
    }

    /// <summary>deploy-checklist, step 2: a deployment checklist was written in the run with every section (REQ-FN-101).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome DeployListWhole(CheckContext aContext)
    {
        var vFile = WrittenInRun(aContext, aContext.Documents.DeployChecklists());
        if (vFile is null)
        {
            return CheckOutcome.Fail("the deployment checklist has every section", "no deployment checklist was written during this run.");
        }

        var vHeadings = Heading().Matches(aContext.Documents.Read(vFile)).Select(aMatch => aMatch.Groups["h"].Value).ToList();
        var vMissing = DeploySections.Where(aSection => !vHeadings.Any(aHeading => aHeading.Contains(aSection, StringComparison.OrdinalIgnoreCase))).ToList();
        return CheckOutcome.From("the deployment checklist has every section", vMissing.Select(aSection => $"{vFile} has no \"{aSection}\" section").ToList(), $"{vFile} has the before, deploy, after, rollback, secrets and proven sections.");
    }

    /// <summary>deploy-checklist, step 3: every item is a checkbox that names who does it (REQ-FN-101).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome DeployItemsNameWho(CheckContext aContext)
    {
        var vFile = WrittenInRun(aContext, aContext.Documents.DeployChecklists());
        if (vFile is null)
        {
            return CheckOutcome.Fail("every item names who does it", "no deployment checklist was written during this run.");
        }

        var vItems = aContext.Documents.Read(vFile).Split('\n').Where(aLine => CheckboxItem().IsMatch(aLine)).ToList();
        if (vItems.Count == 0)
        {
            return CheckOutcome.Fail("every item names who does it", $"{vFile} has no checkbox item (\"- [ ] ...\").");
        }

        var vUnowned = vItems.Where(aItem => !NamesWho().IsMatch(aItem)).Select(aItem => $"\"{Shorten(aItem.Trim())}\" names no one").ToList();
        return CheckOutcome.From("every item names who does it", vUnowned, $"All {vItems.Count} items in {vFile} name who does them.");
    }

    /// <summary>deploy-checklist, step 5: the list covers the pipeline, secrets and versions (REQ-FN-101).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome DeployCoversAreas(CheckContext aContext)
    {
        var vFile = WrittenInRun(aContext, aContext.Documents.DeployChecklists());
        if (vFile is null)
        {
            return CheckOutcome.Fail("the list covers the pipeline, secrets and versions", "no deployment checklist was written during this run.");
        }

        var vText = aContext.Documents.Read(vFile);
        var vMissing = new[] { "pipeline", "secret", "version" }.Where(aWord => !vText.Contains(aWord, StringComparison.OrdinalIgnoreCase)).ToList();
        return CheckOutcome.From("the list covers the pipeline, secrets and versions", vMissing.Select(aWord => $"{vFile} never mentions {aWord}").ToList(), $"{vFile} covers the pipeline, secrets and versions.");
    }

    /// <summary>mockups, step 2: every screen of the phase has a mockup with <c>data-testid</c> anchors (REQ-FN-102).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome ScreensHaveMockups(CheckContext aContext)
    {
        var vScreens = PhaseScreens(aContext, out var vDesign);
        if (vScreens.Count == 0)
        {
            return CheckOutcome.Fail("every screen has an anchored mockup", vDesign is null ? "the project has no UI design document." : $"{vDesign} has no \"### Screen:\" section.");
        }

        var vProblems = new List<string>();
        foreach (var vScreen in vScreens)
        {
            if (vScreen.MockupPath is null || !aContext.Documents.Exists(vScreen.MockupPath))
            {
                vProblems.Add($"screen \"{vScreen.Name}\" has no mockup file");
            }
            else if (!aContext.Documents.Read(vScreen.MockupPath).Contains("data-testid", StringComparison.Ordinal))
            {
                vProblems.Add($"{vScreen.MockupPath} has no data-testid anchor");
            }
        }

        return CheckOutcome.From("every screen has an anchored mockup", vProblems, $"All {vScreens.Count} screens of {vDesign} have a mockup with data-testid anchors.");
    }

    /// <summary>mockups, step 3: every screen has a UI design section with its regions and its empty, loading and error states (REQ-FN-102).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome ScreensHaveDesign(CheckContext aContext)
    {
        var vScreens = PhaseScreens(aContext, out var vDesign);
        if (vScreens.Count == 0)
        {
            return CheckOutcome.Fail("every screen has a UI design section", vDesign is null ? "the project has no UI design document." : $"{vDesign} has no \"### Screen:\" section.");
        }

        var vProblems = new List<string>();
        foreach (var vScreen in vScreens)
        {
            if (!vScreen.HasRegions)
            {
                vProblems.Add($"screen \"{vScreen.Name}\" lists no regions");
            }

            if (vScreen.MissingStates.Count > 0)
            {
                vProblems.Add($"screen \"{vScreen.Name}\" does not describe its {string.Join(" and ", vScreen.MissingStates)} state");
            }
        }

        return CheckOutcome.From("every screen has a UI design section", vProblems, $"All {vScreens.Count} screens of {vDesign} list their regions and their empty, loading and error states.");
    }

    /// <summary>mockups, step 4: every UI row names a mockup that exists (REQ-FN-102).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome UiRowsLinked(CheckContext aContext)
    {
        var vRows = aContext.Documents.Requirements();
        var vProblems = UiRowsWithoutMockup(aContext.Documents, vRows);
        var vUiRows = vRows.Count(aRow => aRow.Type == "REQ-UI");
        return CheckOutcome.From("every UI row names a mockup that exists", vProblems, $"All {vUiRows} UI rows name a mockup that exists.");
    }

    /// <summary>amend-docs, steps 2 and 3: the step before named at least one document that exists (REQ-FN-097).</summary>
    /// <param name="aContext">What the check reads.</param>
    /// <param name="aTouched">The documents the agent named, project-relative.</param>
    public static ProcessStepOutcome TouchedDocumentsNamed(CheckContext aContext, IReadOnlyList<string> aTouched) =>
        aTouched.Count == 0
            ? CheckOutcome.Fail("every touched document is named", "the agent named no existing document (a project-relative path such as docs/Project-BRD.md) in its final message for step 2.")
            : CheckOutcome.Pass($"{aTouched.Count} documents are named as touched: {string.Join(", ", aTouched)}.");

    /// <summary>amend-docs, step 5: touched documents changed, no BRD id was renumbered or deleted, each new item has a checklist row (REQ-FN-097).</summary>
    /// <param name="aContext">What the check reads.</param>
    /// <param name="aTouched">The documents named as touched.</param>
    public static ProcessStepOutcome DocumentsAgree(CheckContext aContext, IReadOnlyList<string> aTouched)
    {
        if (aContext.Baseline.Count == 0)
        {
            return CheckOutcome.Fail("the documents agree", "no snapshot of the project was taken when the run began, so what changed cannot be judged.");
        }

        var vProblems = new List<string>();
        if (aTouched.Count == 0)
        {
            vProblems.Add("no touched document is named");
        }

        foreach (var vFile in aTouched)
        {
            var vNow = aContext.Documents.Exists(vFile) ? ProcessRunBaseline.Fingerprint(aContext.Documents.Full(vFile)) : null;
            if (vNow is null || (aContext.Baseline.TryGetValue(vFile, out var vBefore) && vBefore == vNow))
            {
                vProblems.Add($"{vFile} did not change during the run");
            }
        }

        var vNewNumbers = new List<int>();
        foreach (var vBrd in aContext.Documents.Brds())
        {
            var vNow = ProjectDocuments.ParseBrd(aContext.Documents.Read(vBrd)).Select(aItem => aItem.Number).ToHashSet();
            if (aContext.Baseline.TryGetValue(ProcessRunBaseline.BrdIdsPrefix + vBrd, out var vBeforeIds))
            {
                var vBefore = vBeforeIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(aId => int.Parse(aId, CultureInfo.InvariantCulture)).ToList();
                vProblems.AddRange(vBefore.Where(aId => !vNow.Contains(aId)).Select(aId => $"BRD-{aId} was removed or renumbered in {vBrd}"));
                vNewNumbers.AddRange(vNow.Where(aId => !vBefore.Contains(aId)));
            }
            else
            {
                vNewNumbers.AddRange(vNow);
            }
        }

        var vAcceptance = ChecklistBrdAcceptance(aContext.Documents);
        foreach (var vNumber in vNewNumbers.Distinct().Order())
        {
            if (!vAcceptance.TryGetValue(vNumber, out var vHasAcceptance))
            {
                vProblems.Add($"new BRD-{vNumber} has no checklist row");
            }
            else if (!vHasAcceptance)
            {
                vProblems.Add($"new BRD-{vNumber}'s checklist row has no acceptance line");
            }
        }

        return CheckOutcome.From("the documents agree", vProblems, $"{aTouched.Count} touched documents changed, no BRD id was lost, and {vNewNumbers.Distinct().Count()} new items each have a checklist row.");
    }

    /// <summary>The documents a final message names that exist in the project, in the order named.</summary>
    /// <param name="aDocuments">The project's documents.</param>
    /// <param name="aMessage">The agent's final message.</param>
    public static IReadOnlyList<string> NamedDocuments(ProjectDocuments aDocuments, string? aMessage)
    {
        if (string.IsNullOrWhiteSpace(aMessage))
        {
            return [];
        }

        return DocumentName().Matches(aMessage)
            .Select(aMatch => aMatch.Value.Trim('.', '/'))
            .Select(aDocuments.ResolveNamed)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>triage-issues, step 2: the reports can be read (REQ-FN-098).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome ReportsReadable(CheckContext aContext)
    {
        var vReports = Reports(aContext.Documents);
        return CheckOutcome.Pass(vReports.Count == 0
            ? "There is no report in docs/issues/, so there is nothing to triage."
            : $"{vReports.Count} reports read from docs/issues/.");
    }

    /// <summary>triage-issues, step 3: every report has a checklist to go to (REQ-FN-098).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome ReportsHaveChecklist(CheckContext aContext)
    {
        var vReports = Reports(aContext.Documents);
        if (vReports.Count == 0)
        {
            return CheckOutcome.Pass("There is no report in docs/issues/, so there is nothing to route.");
        }

        var vRows = aContext.Documents.Requirements();
        if (aContext.Documents.Checklists().Count == 0)
        {
            return CheckOutcome.Fail("every report has a checklist to go to", "the project has no checklist.");
        }

        var vProblems = new List<string>();
        foreach (var vReport in vReports)
        {
            foreach (var vId in RequirementIdScanner.Scan(vReport.Text).Where(aId => vRows.All(aRow => aRow.Id != aId)))
            {
                vProblems.Add($"{vReport.File} names {vId}, which is on no checklist");
            }
        }

        return CheckOutcome.From("every report has a checklist to go to", vProblems, $"All {vReports.Count} reports name rows on a checklist, or can become new rows on {aContext.Documents.NewestChecklist()}.");
    }

    /// <summary>triage-issues, step 4: every report is a reopened or new row, and nothing under src or tests changed (REQ-FN-098).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome ReportsHaveRows(CheckContext aContext)
    {
        var vProblems = new List<string>();
        var vReports = Reports(aContext.Documents);
        var vRows = aContext.Documents.Requirements();
        var vOpen = new[] { "not started", "in progress", "fail", "needs re-verify", "partial", "blocked" };
        foreach (var vReport in vReports)
        {
            var vIds = RequirementIdScanner.Scan(vReport.Text);
            if (vIds.Count > 0)
            {
                foreach (var vId in vIds)
                {
                    var vRow = vRows.FirstOrDefault(aRow => aRow.Id == vId);
                    if (vRow is null || !vOpen.Contains(ProjectDocuments.StatusKey(vRow.FileStatus)))
                    {
                        vProblems.Add($"{vReport.File} names {vId}, which is not reopened");
                    }
                }
            }
            else
            {
                var vNew = vRows.FirstOrDefault(aRow =>
                    ProjectDocuments.StatusKey(aRow.FileStatus) == "not started"
                    && (aRow.Title.Contains(vReport.Title, StringComparison.OrdinalIgnoreCase) || aRow.Remarks.Contains(vReport.File, StringComparison.OrdinalIgnoreCase)));
                if (vNew is null)
                {
                    vProblems.Add($"{vReport.File} (\"{Shorten(vReport.Title)}\") has no new Not Started row");
                }
                else if (vNew.Acceptance.Length == 0)
                {
                    vProblems.Add($"{vNew.Id}, the row for {vReport.File}, has no acceptance line");
                }
            }
        }

        if (aContext.Baseline.Count == 0)
        {
            vProblems.Add("no snapshot of the project was taken when the run began, so it cannot be shown that src and tests are untouched");
        }
        else
        {
            vProblems.AddRange(ChangedUnder(aContext, "src/", "tests/").Select(aPath => $"{aPath} changed, and triage may not change src or tests"));
        }

        return CheckOutcome.From("every report is a reopened or new row and nothing under src or tests changed", vProblems, $"All {vReports.Count} reports are a reopened or new checklist row, and src and tests are as they were.");
    }

    /// <summary>metrics-report, step 3: the run records can be read and added up (REQ-FN-099).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome RecordsAddUp(CheckContext aContext)
    {
        var vTotals = RecordTotals(aContext.Documents);
        return vTotals is null
            ? CheckOutcome.Fail("the totals match the records", "docs/metrics/runs.jsonl does not exist, so there are no run records to add up.")
            : CheckOutcome.Pass($"The records add up to {vTotals.Runs} runs, {vTotals.Tokens} tokens and {vTotals.Failures} failures.");
    }

    /// <summary>metrics-report, step 4: the report written in the run has the totals the records add up to (REQ-FN-099).</summary>
    /// <param name="aContext">What the check reads.</param>
    public static ProcessStepOutcome ReportMatchesRecords(CheckContext aContext)
    {
        var vFile = WrittenInRun(aContext, aContext.Documents.MetricsReports());
        if (vFile is null)
        {
            return CheckOutcome.Fail("the report matches the records", "no metrics report was written during this run (docs/ file name containing \"Metrics\" and \"Report\").");
        }

        var vTotals = RecordTotals(aContext.Documents);
        if (vTotals is null)
        {
            return CheckOutcome.Fail("the report matches the records", "docs/metrics/runs.jsonl does not exist, so there are no run records to add up.");
        }

        var vReported = ReportTotal().Matches(aContext.Documents.Read(vFile))
            .GroupBy(aMatch => aMatch.Groups["k"].Value.ToLowerInvariant())
            .ToDictionary(aGroup => aGroup.Key, aGroup => long.Parse(aGroup.First().Groups["n"].Value.Replace(",", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture));
        var vProblems = new List<string>();
        foreach (var (vName, vExpected) in new[] { ("runs", vTotals.Runs), ("tokens", vTotals.Tokens), ("failures", vTotals.Failures) })
        {
            if (!vReported.TryGetValue(vName, out var vGiven))
            {
                vProblems.Add($"{vFile} gives no {vName} total");
            }
            else if (vGiven != vExpected)
            {
                vProblems.Add($"{vFile} says {vGiven} {vName} but the records add up to {vExpected}");
            }
        }

        return CheckOutcome.From("the report matches the records", vProblems, $"{vFile} gives {vTotals.Runs} runs, {vTotals.Tokens} tokens and {vTotals.Failures} failures, as the records add up.");
    }

    /// <summary>The totals the run records add up to.</summary>
    /// <param name="Runs">Records in <c>runs.jsonl</c>.</param>
    /// <param name="Tokens">The sum of <c>tokens_in</c> and <c>tokens_out</c> over them.</param>
    /// <param name="Failures">Runs whose <c>build_result</c> is fail plus gate records whose verdict is FAIL.</param>
    public sealed record RecordTotalsResult(long Runs, long Tokens, long Failures);

    /// <summary>Adds up <c>docs/metrics/*.jsonl</c>, or returns <see langword="null"/> when there is no <c>runs.jsonl</c>.</summary>
    /// <param name="aDocuments">The project's documents.</param>
    public static RecordTotalsResult? RecordTotals(ProjectDocuments aDocuments)
    {
        if (!aDocuments.Exists("docs/metrics/runs.jsonl"))
        {
            return null;
        }

        long vRuns = 0;
        long vTokens = 0;
        long vFailures = 0;
        foreach (var vLine in aDocuments.Read("docs/metrics/runs.jsonl").Split('\n').Where(aLine => aLine.Trim().Length > 0))
        {
            if (!TryParse(vLine, out var vRecord))
            {
                continue;
            }

            vRuns++;
            vTokens += Number(vRecord, "tokens_in") + Number(vRecord, "tokens_out");
            if (string.Equals(Text(vRecord, "build_result"), "fail", StringComparison.OrdinalIgnoreCase))
            {
                vFailures++;
            }
        }

        foreach (var vLine in aDocuments.Read("docs/metrics/gates.jsonl").Split('\n').Where(aLine => aLine.Trim().Length > 0))
        {
            if (TryParse(vLine, out var vRecord) && string.Equals(Text(vRecord, "verdict"), "FAIL", StringComparison.OrdinalIgnoreCase))
            {
                vFailures++;
            }
        }

        return new RecordTotalsResult(vRuns, vTokens, vFailures);
    }

    private static bool TryParse(string aLine, out JsonElement aRecord)
    {
        try
        {
            aRecord = JsonDocument.Parse(aLine).RootElement.Clone();
            return aRecord.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            aRecord = default;
            return false;
        }
    }

    private static string Text(JsonElement aRecord, string aName) =>
        aRecord.TryGetProperty(aName, out var vValue) ? vValue.ToString() : string.Empty;

    private static long Number(JsonElement aRecord, string aName) =>
        long.TryParse(Text(aRecord, aName), NumberStyles.Integer, CultureInfo.InvariantCulture, out var vNumber) ? vNumber : 0;

    private static ProcessStepOutcome OnePhaseOnly(ProjectDocuments aDocuments, string aWhat)
    {
        var vBrds = aDocuments.Brds();
        var vChecklists = aDocuments.Checklists();
        return vBrds.Count == 1 && vChecklists.Count == 1
            ? CheckOutcome.Pass("The project has no phases file and one BRD and one checklist: a single phase.")
            : CheckOutcome.Fail(aWhat, $"there is no phases file, and the project has {vBrds.Count} BRDs and {vChecklists.Count} checklists rather than one of each.");
    }

    private static string? WrittenInRun(CheckContext aContext, IReadOnlyList<string> aCandidates) =>
        aCandidates
            .Where(aFile => aContext.Documents.WrittenUtc(aFile) is { } vWritten && vWritten >= aContext.RunStartedUtc.AddSeconds(-2))
            .OrderByDescending(aFile => aContext.Documents.WrittenUtc(aFile))
            .FirstOrDefault();

    private static IReadOnlyList<DesignScreen> PhaseScreens(CheckContext aContext, out string? aDesign)
    {
        var vNewest = aContext.Documents.NewestChecklist();
        var vPhase = vNewest is null ? 1 : ChecklistDocument.PhaseOf(vNewest);
        var vDesigns = aContext.Documents.UiDesigns();
        aDesign = vDesigns.FirstOrDefault(aFile => ProjectDocuments.PhaseOf(aFile) == vPhase)
            ?? vDesigns.OrderByDescending(ProjectDocuments.PhaseOf).FirstOrDefault();
        return aDesign is null ? [] : ProjectDocuments.ParseScreens(aContext.Documents.Read(aDesign));
    }

    private static List<string> UiRowsWithoutMockup(ProjectDocuments aDocuments, IReadOnlyList<RequirementItem> aRows) =>
        aRows.Where(aRow => aRow.Type == "REQ-UI" && (aRow.Mockup is null || aDocuments.ResolveNamed(aRow.Mockup) is null))
            .Select(aRow => aRow.Mockup is null ? $"{aRow.Id} names no mockup" : $"{aRow.Id}'s mockup {aRow.Mockup} does not exist")
            .ToList();

    private static HashSet<int> ChecklistBrdNumbers(ProjectDocuments aDocuments) =>
        ChecklistBrdAcceptance(aDocuments).Keys.ToHashSet();

    /// <summary>For each BRD number a checklist detail line names, whether that row has an acceptance line.</summary>
    private static Dictionary<int, bool> ChecklistBrdAcceptance(ProjectDocuments aDocuments)
    {
        var vResult = new Dictionary<int, bool>();
        foreach (var vFile in aDocuments.Checklists())
        {
            var vText = aDocuments.Read(vFile);
            var vAcceptance = ChecklistDocument.Parse(vText, vFile, ChecklistDocument.PhaseOf(vFile)).ToDictionary(aRow => aRow.Id, aRow => aRow.Acceptance.Length > 0, StringComparer.Ordinal);
            foreach (Match vDetail in ChecklistDetail().Matches(vText))
            {
                foreach (Match vBrd in Regex.Matches(vDetail.Groups["rest"].Value, @"BRD-(\d+)"))
                {
                    var vNumber = int.Parse(vBrd.Groups[1].Value, CultureInfo.InvariantCulture);
                    var vHas = vAcceptance.TryGetValue(vDetail.Groups["id"].Value, out var vOk) && vOk;
                    vResult[vNumber] = vResult.TryGetValue(vNumber, out var vBefore) ? vBefore || vHas : vHas;
                }
            }
        }

        return vResult;
    }

    private sealed record Report(string File, string Title, string Text);

    private static List<Report> Reports(ProjectDocuments aDocuments)
    {
        var vFolder = Path.Combine(aDocuments.Root, "docs", "issues");
        if (!Directory.Exists(vFolder))
        {
            return [];
        }

        return Directory.EnumerateFiles(vFolder)
            .Where(aPath => aPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || aPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal)
            .Select(aPath =>
            {
                var vText = File.ReadAllText(aPath);
                var vTitle = vText.Split('\n').Select(aLine => aLine.Trim().TrimStart('#').Trim()).FirstOrDefault(aLine => aLine.Length > 0) ?? Path.GetFileNameWithoutExtension(aPath);
                return new Report("docs/issues/" + Path.GetFileName(aPath), vTitle, vText);
            })
            .ToList();
    }

    private static List<string> ChangedUnder(CheckContext aContext, params string[] aPrefixes)
    {
        var vNow = ProcessRunBaseline.Snapshot(aContext.Folder);
        var vPaths = vNow.Keys.Concat(aContext.Baseline.Keys).Distinct(StringComparer.Ordinal)
            .Where(aPath => aPrefixes.Any(aPrefix => aPath.StartsWith(aPrefix, StringComparison.Ordinal)));
        return vPaths.Where(aPath => !aContext.Baseline.TryGetValue(aPath, out var vBefore) || !vNow.TryGetValue(aPath, out var vCurrent) || vBefore != vCurrent)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private static string Shorten(string aText) => aText.Length <= 70 ? aText : aText[..67] + "...";
}
