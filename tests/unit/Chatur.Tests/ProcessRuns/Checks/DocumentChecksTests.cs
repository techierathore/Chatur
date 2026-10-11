using Chatur.Core.ProcessRuns;
using Chatur.Core.ProcessRuns.Checks;
using Chatur.Tests.Support;
using Xunit;

namespace Chatur.Tests.ProcessRuns.Checks;

/// <summary>
/// Tests for the checks that judge the documents a process leaves on disk: day1-greenfield, split-brd, deploy-checklist,
/// mockups, amend-docs, triage-issues and metrics-report (REQ-FN-100, 092, 101, 102, 097, 098, 099).
/// </summary>
public sealed class DocumentChecksTests
{
    /// <summary>When the brief names no measure of success, then the brief check fails and names what is missing; when it names all three, it passes (REQ-FN-100).</summary>
    [Fact(DisplayName = "REQ-FN-100 the brief must name an audience, a problem and a measure of success")]
    public void BriefMustNameThree()
    {
        using var vProject = new TempProject().Write("docs/X-Brief.md", "The audience is shops. The problem is counting.");

        var vBad = DocumentChecks.BriefNamesThree(vProject.Context());
        vProject.Write("docs/X-Brief.md", ProjectFixtures.GoodBrief);
        var vGood = DocumentChecks.BriefNamesThree(vProject.Context());

        Assert.False(vBad.Passed);
        Assert.Contains("no measure of success", vBad.Result, StringComparison.Ordinal);
        Assert.True(vGood.Passed);
    }

    /// <summary>When there is no brief at all, then the check fails rather than passing (REQ-FN-100).</summary>
    [Fact(DisplayName = "REQ-FN-100 a missing brief fails the check")]
    public void MissingBriefFails()
    {
        using var vProject = new TempProject();

        Assert.False(DocumentChecks.BriefExists(vProject.Context()).Passed);
    }

    /// <summary>When a BRD item has no acceptance line or an id is written twice, then the BRD check fails (REQ-FN-100).</summary>
    [Fact(DisplayName = "REQ-FN-100 every BRD item needs an acceptance line and no id may repeat")]
    public void BrdRulesNeedAcceptanceAndUniqueIds()
    {
        using var vProject = new TempProject().Write("docs/X-BRD.md", "- **BRD-1** — One.\n- **BRD-1** — Again.\n  - *Acceptance:* When a, then b.\n");

        var vResult = DocumentChecks.BrdRules(vProject.Context());
        vProject.Write("docs/X-BRD.md", ProjectFixtures.GoodBrd);

        Assert.False(vResult.Passed);
        Assert.Contains("BRD-1 appears twice", vResult.Result, StringComparison.Ordinal);
        Assert.Contains("no acceptance line", vResult.Result, StringComparison.Ordinal);
        Assert.True(DocumentChecks.BrdRules(vProject.Context()).Passed);
    }

    /// <summary>
    /// When the architecture, UI design, a mockup per screen and the checklist exist and agree, then the day-1 check passes;
    /// when a screen's mockup or a BRD item's checklist row is missing, it fails (REQ-FN-100).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-100 day1-greenfield passes only when the documents exist and agree")]
    public void Day1PassesOnlyWhenDocumentsAgree()
    {
        using var vProject = ProjectFixtures.FullDay1(new TempProject());
        var vGood = DocumentChecks.Day1Documents(vProject.Context());

        File.Delete(Path.Combine(vProject.Root, "docs", "mockups", "sign-in.html"));
        var vNoMockup = DocumentChecks.Day1Documents(vProject.Context());
        vProject.Write("docs/mockups/sign-in.html", ProjectFixtures.GoodMockup).Write("docs/X-BRD.md", ProjectFixtures.GoodBrd + "- **BRD-3** — Third.\n  - *Acceptance:* When a, then b.\n");
        var vNoRow = DocumentChecks.Day1Documents(vProject.Context());

        Assert.True(vGood.Passed, vGood.Result);
        Assert.False(vNoMockup.Passed);
        Assert.Contains("no mockup on disk", vNoMockup.Result, StringComparison.Ordinal);
        Assert.False(vNoRow.Passed);
        Assert.Contains("BRD-3 has no checklist row", vNoRow.Result, StringComparison.Ordinal);
    }

    /// <summary>When a UI row names a mockup that does not exist, then the day-1 check fails on it (REQ-FN-100).</summary>
    [Fact(DisplayName = "REQ-FN-100 a UI row's mockup must exist")]
    public void UiRowMockupMustExist()
    {
        using var vProject = ProjectFixtures.FullDay1(new TempProject()).Write("docs/X-Checklist.md", ProjectFixtures.GoodChecklist.Replace("mockups/sign-in.html", "mockups/gone.html", StringComparison.Ordinal));

        var vResult = DocumentChecks.Day1Documents(vProject.Context());

        Assert.False(vResult.Passed);
        Assert.Contains("REQ-UI-001's mockup mockups/gone.html does not exist", vResult.Result, StringComparison.Ordinal);
    }

    /// <summary>
    /// When every BRD id lands in exactly one phase and each phase has its BRD and checklist, then the split-brd checks pass;
    /// when an id is in two phases or a phase file is missing, they fail (REQ-FN-092).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-092 split-brd passes only when every BRD id is in exactly one phase and each phase has its files")]
    public void SplitPassesOnlyWhenEveryIdIsInOnePhase()
    {
        const string Phases = "| Phase | Name | Screens | BRD range | Status |\n|---|---|---|---|---|\n| 1 | One | A | BRD-1 | done |\n| 2 | Two | B | BRD-2, BRD-4 to BRD-5 | planned |\n";
        using var vProject = new TempProject()
            .Write("docs/X-Phases.md", Phases)
            .Write("docs/X-BRD.md", "- **BRD-1** — One.\n  - *Acceptance:* a\n")
            .Write("docs/X-P2-BRD.md", "- **BRD-2** — Two.\n  - *Acceptance:* a\n- **BRD-4** — Four.\n  - *Acceptance:* a\n- **BRD-5** — Five.\n  - *Acceptance:* a\n")
            .Write("docs/X-Checklist.md", "x")
            .Write("docs/X-P2-Checklist.md", "x");

        var vRanges = DocumentChecks.PhasesHaveRanges(vProject.Context());
        var vOne = DocumentChecks.EveryIdInOnePhase(vProject.Context());
        var vFiles = DocumentChecks.PhaseFilesWhole(vProject.Context());
        vProject.Write("docs/X-Phases.md", Phases.Replace("BRD-1 |", "BRD-1, BRD-2 |", StringComparison.Ordinal));
        var vTwice = DocumentChecks.EveryIdInOnePhase(vProject.Context());
        File.Delete(Path.Combine(vProject.Root, "docs", "X-P2-Checklist.md"));
        vProject.Write("docs/X-Phases.md", Phases);
        var vNoChecklist = DocumentChecks.PhaseFilesWhole(vProject.Context());

        Assert.True(vRanges.Passed, vRanges.Result);
        Assert.True(vOne.Passed, vOne.Result);
        Assert.True(vFiles.Passed, vFiles.Result);
        Assert.False(vTwice.Passed);
        Assert.Contains("BRD-2 is in phases 1 and 2", vTwice.Result, StringComparison.Ordinal);
        Assert.False(vNoChecklist.Passed);
        Assert.Contains("phase 2 has no checklist file", vNoChecklist.Result, StringComparison.Ordinal);
    }

    /// <summary>When the project has no phases file, one BRD and one checklist pass, and anything else fails (REQ-FN-092).</summary>
    [Fact(DisplayName = "REQ-FN-092 a one-phase project passes on its one BRD and checklist")]
    public void OnePhaseProjectPasses()
    {
        using var vProject = new TempProject().Write("docs/X-BRD.md", ProjectFixtures.GoodBrd).Write("docs/X-Checklist.md", ProjectFixtures.GoodChecklist);
        var vPass = DocumentChecks.EveryIdInOnePhase(vProject.Context());
        vProject.Write("docs/X-P2-BRD.md", ProjectFixtures.GoodBrd);

        Assert.True(vPass.Passed);
        Assert.False(DocumentChecks.EveryIdInOnePhase(vProject.Context()).Passed);
    }

    /// <summary>
    /// When the deployment checklist was written in the run with every section and each item names who does it, then the
    /// checks pass; an old file, a missing section or an item with no owner fails (REQ-FN-101).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-101 deploy-checklist passes only when the list written in the run has every section and every item names who")]
    public void DeployChecklistMustBeWhole()
    {
        const string Good = "# Deploy\n\n## Before\n- [ ] Tag the version — Who: owner\n## Deploy\n- [ ] Run the pipeline — Who: pipeline\n## After\n- [ ] Smoke it — Who: agent\n## Rollback\n- [ ] Redeploy the last version — Who: owner\n## Secrets\n- [ ] Rotate keys — Who: owner\n## Proven\n- [x] Build passes — Who: agent\n";
        using var vProject = new TempProject().Write("docs/X-Deploy-Checklist.md", Good);
        var vRun = DateTime.UtcNow.AddMinutes(-1);
        var vWhole = DocumentChecks.DeployListWhole(vProject.Context(vRun));
        var vWho = DocumentChecks.DeployItemsNameWho(vProject.Context(vRun));
        var vAreas = DocumentChecks.DeployCoversAreas(vProject.Context(vRun));
        vProject.Write("docs/X-Deploy-Checklist.md", Good.Replace("## Rollback\n- [ ] Redeploy the last version — Who: owner\n", string.Empty, StringComparison.Ordinal).Replace("Run the pipeline — Who: pipeline", "Run the pipeline", StringComparison.Ordinal));
        var vNoRollback = DocumentChecks.DeployListWhole(vProject.Context(vRun));
        var vNoOwner = DocumentChecks.DeployItemsNameWho(vProject.Context(vRun));
        vProject.Touch("docs/X-Deploy-Checklist.md", DateTime.UtcNow.AddDays(-3));
        var vOld = DocumentChecks.DeployListWhole(vProject.Context(vRun));

        Assert.True(vWhole.Passed, vWhole.Result);
        Assert.True(vWho.Passed, vWho.Result);
        Assert.True(vAreas.Passed, vAreas.Result);
        Assert.False(vNoRollback.Passed);
        Assert.Contains("rollback", vNoRollback.Result, StringComparison.Ordinal);
        Assert.False(vNoOwner.Passed);
        Assert.Contains("Run the pipeline", vNoOwner.Result, StringComparison.Ordinal);
        Assert.False(vOld.Passed);
        Assert.Contains("written during this run", vOld.Result, StringComparison.Ordinal);
    }

    /// <summary>
    /// When every screen has an anchored mockup and a UI design section with regions and states, and every UI row's mockup
    /// exists, then the mockups checks pass; a mockup with no anchor, a section with no error state, or a UI row naming a
    /// missing mockup fails (REQ-FN-102).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-102 mockups passes only when every screen is drawn, anchored, designed and linked")]
    public void MockupsMustBeDrawnAndLinked()
    {
        using var vProject = ProjectFixtures.FullDay1(new TempProject());
        var vAnchored = DocumentChecks.ScreensHaveMockups(vProject.Context());
        var vDesigned = DocumentChecks.ScreensHaveDesign(vProject.Context());
        var vLinked = DocumentChecks.UiRowsLinked(vProject.Context());
        vProject.Write("docs/mockups/sign-in.html", "<html></html>");
        var vNoAnchor = DocumentChecks.ScreensHaveMockups(vProject.Context());
        vProject.Write("docs/X-UIDesign.md", ProjectFixtures.GoodUiDesign.Replace(" · error: the message", string.Empty, StringComparison.Ordinal));
        var vNoError = DocumentChecks.ScreensHaveDesign(vProject.Context());
        vProject.Write("docs/X-Checklist.md", ProjectFixtures.GoodChecklist.Replace("mockups/sign-in.html", "mockups/none.html", StringComparison.Ordinal));
        var vBroken = DocumentChecks.UiRowsLinked(vProject.Context());

        Assert.True(vAnchored.Passed, vAnchored.Result);
        Assert.True(vDesigned.Passed, vDesigned.Result);
        Assert.True(vLinked.Passed, vLinked.Result);
        Assert.False(vNoAnchor.Passed);
        Assert.Contains("no data-testid anchor", vNoAnchor.Result, StringComparison.Ordinal);
        Assert.False(vNoError.Passed);
        Assert.Contains("error state", vNoError.Result, StringComparison.Ordinal);
        Assert.False(vBroken.Passed);
        Assert.Contains("mockups/none.html does not exist", vBroken.Result, StringComparison.Ordinal);
    }

    /// <summary>
    /// When the documents named as touched changed, no BRD id was lost and each new BRD item has a checklist row with an
    /// acceptance line, then the amend-docs check passes; a document that did not change, a lost id or a new item with no
    /// row fails (REQ-FN-097).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-097 amend-docs passes only when touched documents changed, no BRD id was lost and new items have rows")]
    public void AmendDocsMustAgree()
    {
        using var vProject = ProjectFixtures.FullDay1(new TempProject());
        var vBaseline = ProcessRunBaseline.Snapshot(vProject.Root);
        File.SetLastWriteTimeUtc(Path.Combine(vProject.Root, "docs", "X-BRD.md"), DateTime.UtcNow.AddDays(-1));
        vBaseline = ProcessRunBaseline.Snapshot(vProject.Root);
        string[] vTouched = ["docs/X-BRD.md", "docs/X-Checklist.md"];

        var vUnchanged = DocumentChecks.DocumentsAgree(vProject.Context(aBaseline: vBaseline), vTouched);
        vProject.Write("docs/X-BRD.md", ProjectFixtures.GoodBrd + "- **BRD-3** — Third.\n  - *Acceptance:* When a, then b.\n");
        vProject.Write("docs/X-Checklist.md", ProjectFixtures.GoodChecklist.Replace("| REQ-FN-001 | Account | Not Started | 0% | | |", "| REQ-FN-001 | Account | Not Started | 0% | | |\n| REQ-FN-002 | Third | Not Started | 0% | | |", StringComparison.Ordinal) + "- **REQ-FN-002** — Third. *BRD:* BRD-3\n  - *Acceptance:* When a, then b.\n");
        var vAgrees = DocumentChecks.DocumentsAgree(vProject.Context(aBaseline: vBaseline), vTouched);
        vProject.Write("docs/X-Checklist.md", ProjectFixtures.GoodChecklist + "\n");
        var vNoRow = DocumentChecks.DocumentsAgree(vProject.Context(aBaseline: vBaseline), vTouched);
        vProject.Write("docs/X-BRD.md", "- **BRD-1** — Renumbered.\n  - *Acceptance:* a\n- **BRD-3** — Third.\n  - *Acceptance:* a\n");
        var vLost = DocumentChecks.DocumentsAgree(vProject.Context(aBaseline: vBaseline), vTouched);

        Assert.False(vUnchanged.Passed);
        Assert.Contains("did not change", vUnchanged.Result, StringComparison.Ordinal);
        Assert.True(vAgrees.Passed, vAgrees.Result);
        Assert.False(vNoRow.Passed);
        Assert.Contains("new BRD-3 has no checklist row", vNoRow.Result, StringComparison.Ordinal);
        Assert.False(vLost.Passed);
        Assert.Contains("BRD-2 was removed or renumbered", vLost.Result, StringComparison.Ordinal);
    }

    /// <summary>When the agent's final message names no existing document as touched, then the check fails; documents that exist are found (REQ-FN-097).</summary>
    [Fact(DisplayName = "REQ-FN-097 touched documents are the existing ones the agent names")]
    public void TouchedDocumentsAreTheExistingOnesNamed()
    {
        using var vProject = ProjectFixtures.FullDay1(new TempProject());

        var vNamed = DocumentChecks.NamedDocuments(new ProjectDocuments(vProject.Root), "Amended docs/X-BRD.md and X-Checklist.md, and docs/Imaginary.md.");

        Assert.Equal(["docs/X-BRD.md", "docs/X-Checklist.md"], vNamed);
        Assert.False(DocumentChecks.TouchedDocumentsNamed(vProject.Context(), []).Passed);
    }

    /// <summary>
    /// When every report is a reopened row or a new Not Started row with an acceptance line and nothing under src or tests
    /// changed, then the triage check passes; an unmatched report, or a changed source file, fails (REQ-FN-098).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-098 triage-issues passes only when every report has a row and src and tests are untouched")]
    public void TriageMustGiveEveryReportARowAndLeaveSourceAlone()
    {
        using var vProject = ProjectFixtures.FullDay1(new TempProject())
            .Write("src/App.cs", "class App {}")
            .Write("docs/issues/report-1.md", "# Sign in button does nothing\nSee REQ-UI-001.\n")
            .Write("docs/issues/report-2.md", "# Export loses the last row\nIt drops one row.\n");
        var vBaseline = ProcessRunBaseline.Snapshot(vProject.Root);
        vProject.Write(
            "docs/X-Checklist.md",
            ProjectFixtures.GoodChecklist.Replace("| REQ-FN-001 | Account | Not Started | 0% | | |", "| REQ-FN-001 | Account | Not Started | 0% | | |\n| REQ-FN-002 | Export loses the last row | Not Started | 0% | docs/issues/report-2.md | |", StringComparison.Ordinal)
            + "- **REQ-FN-002** — Export loses the last row. *BRD:* BRD-2\n  - *Acceptance:* When exported, then every row is there.\n");

        var vGood = DocumentChecks.ReportsHaveRows(vProject.Context(aBaseline: vBaseline));
        vProject.Write("src/App.cs", "class App { int X; }");
        var vChanged = DocumentChecks.ReportsHaveRows(vProject.Context(aBaseline: vBaseline));
        vProject.Write("src/App.cs", "class App {}").Write("docs/issues/report-3.md", "# Unrelated crash\n");
        var vUnmatched = DocumentChecks.ReportsHaveRows(vProject.Context(aBaseline: ProcessRunBaseline.Snapshot(vProject.Root)));

        Assert.True(vGood.Passed, vGood.Result);
        Assert.False(vChanged.Passed);
        Assert.Contains("src/App.cs changed", vChanged.Result, StringComparison.Ordinal);
        Assert.False(vUnmatched.Passed);
        Assert.Contains("report-3.md", vUnmatched.Result, StringComparison.Ordinal);
    }

    /// <summary>When a report names a row that is on no checklist, then the routing check fails (REQ-FN-098).</summary>
    [Fact(DisplayName = "REQ-FN-098 a report naming a row that is on no checklist has no checklist to go to")]
    public void ReportMustNameARowOnAChecklist()
    {
        using var vProject = ProjectFixtures.FullDay1(new TempProject()).Write("docs/issues/r.md", "# Broken\nREQ-FN-999 fails.\n");

        var vResult = DocumentChecks.ReportsHaveChecklist(vProject.Context());

        Assert.False(vResult.Passed);
        Assert.Contains("REQ-FN-999, which is on no checklist", vResult.Result, StringComparison.Ordinal);
    }

    /// <summary>
    /// When the report written in the run has totals equal to those added up again from the run records, then the check
    /// passes; a wrong total, a missing total or a report not written in the run fails (REQ-FN-099).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-099 metrics-report passes only when the report's totals equal the records'")]
    public void MetricsReportMustMatchTheRecords()
    {
        using var vProject = new TempProject()
            .Write("docs/metrics/runs.jsonl", "{\"kind\":\"run\",\"tokens_in\":\"10\",\"tokens_out\":\"90\",\"build_result\":\"fail\"}\n{\"kind\":\"run\",\"tokens_in\":5,\"tokens_out\":5,\"build_result\":\"pass\"}\n")
            .Write("docs/metrics/gates.jsonl", "{\"kind\":\"gate\",\"verdict\":\"FAIL\"}\n{\"kind\":\"gate\",\"verdict\":\"Verified\"}\n");
        var vRun = DateTime.UtcNow.AddMinutes(-1);

        var vTotals = DocumentChecks.RecordTotals(new ProjectDocuments(vProject.Root));
        vProject.Write("docs/X-Metrics-Report.md", "# Report\n\n| Total | Count |\n|---|---|\n| Runs | 2 |\n| Tokens | 110 |\n| Failures | 2 |\n");
        var vMatches = DocumentChecks.ReportMatchesRecords(vProject.Context(vRun));
        vProject.Write("docs/X-Metrics-Report.md", "Runs: 2\nTokens: 100\nFailures: 2\n");
        var vWrong = DocumentChecks.ReportMatchesRecords(vProject.Context(vRun));
        vProject.Write("docs/X-Metrics-Report.md", "Runs: 2\nTokens: 110\n");
        var vMissing = DocumentChecks.ReportMatchesRecords(vProject.Context(vRun));

        Assert.Equal(new DocumentChecks.RecordTotalsResult(2, 110, 2), vTotals);
        Assert.True(vMatches.Passed, vMatches.Result);
        Assert.False(vWrong.Passed);
        Assert.Contains("says 100 tokens but the records add up to 110", vWrong.Result, StringComparison.Ordinal);
        Assert.False(vMissing.Passed);
        Assert.Contains("gives no failures total", vMissing.Result, StringComparison.Ordinal);
    }

    /// <summary>When the records are missing, the metrics check fails rather than passing on an empty total (REQ-FN-099).</summary>
    [Fact(DisplayName = "REQ-FN-099 a project with no run records cannot pass the metrics check")]
    public void NoRecordsFailsTheMetricsCheck()
    {
        using var vProject = new TempProject().Write("docs/X-Metrics-Report.md", "Runs: 0\nTokens: 0\nFailures: 0\n");

        Assert.False(DocumentChecks.RecordsAddUp(vProject.Context()).Passed);
        Assert.False(DocumentChecks.ReportMatchesRecords(vProject.Context()).Passed);
    }

    /// <summary>
    /// The four processes whose agent holds no file-writing right today (day1-greenfield, amend-docs, triage-issues,
    /// metrics-report) still have checks that pass on the documents a writer would leave. Each row's check, found the way the
    /// run finds it, passes on fixture files when they are whole and fails naming what is missing when they are not
    /// (REQ-FN-100, REQ-FN-097, REQ-FN-098, REQ-FN-099).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-100/097/098/099 the checks of the rights-blocked processes pass on whole documents and name what is missing")]
    public async Task RightsBlockedProcessesChecksPassOnWholeDocuments()
    {
        static async Task<Chatur.Core.Actions.ProcessStepOutcome> Run(string aProcess, int aStep, CheckContext aContext) =>
            await ProcessChecks.Find(aProcess, aStep)!(aContext, default);

        // day1-greenfield: brief (3), brief names three (4), BRD (5), documents (8)
        using var vDay1 = new TempProject();
        Assert.False((await Run("day1-greenfield", 3, vDay1.Context())).Passed);
        ProjectFixtures.FullDay1(vDay1);
        foreach (var vStep in new[] { 3, 4, 5, 8 })
        {
            var vOutcome = await Run("day1-greenfield", vStep, vDay1.Context());
            Assert.True(vOutcome.Passed, $"day1-greenfield step {vStep}: {vOutcome.Result}");
        }

        // amend-docs: steps 2 and 3 pass once a named document exists; the document must then differ from the baseline
        using var vAmend = ProjectFixtures.FullDay1(new TempProject());
        Assert.True(DocumentChecks.TouchedDocumentsNamed(vAmend.Context(), ["docs/X-BRD.md"]).Passed);
        Assert.False(DocumentChecks.TouchedDocumentsNamed(vAmend.Context(), []).Passed);
        File.SetLastWriteTimeUtc(Path.Combine(vAmend.Root, "docs", "X-BRD.md"), DateTime.UtcNow.AddDays(-1));
        var vBaseline = ProcessRunBaseline.Snapshot(vAmend.Root);
        vAmend.Write("docs/X-BRD.md", ProjectFixtures.GoodBrd + "- **BRD-3** — Third.\n  - *Acceptance:* When a, then b.\n");
        vAmend.Write("docs/X-Checklist.md", ProjectFixtures.GoodChecklist.Replace("| REQ-FN-001 | Account | Not Started | 0% | | |", "| REQ-FN-001 | Account | Not Started | 0% | | |\n| REQ-FN-002 | Third | Not Started | 0% | | |", StringComparison.Ordinal) + "- **REQ-FN-002** — Third. *BRD:* BRD-3\n  - *Acceptance:* When a, then b.\n");
        Assert.True(DocumentChecks.DocumentsAgree(vAmend.Context(aBaseline: vBaseline), ["docs/X-BRD.md", "docs/X-Checklist.md"]).Passed);

        // triage-issues: reading and routing pass for a report naming a row of the checklist; the row check passes once it is reopened
        using var vTriage = ProjectFixtures.FullDay1(new TempProject()).Write("docs/issues/r.md", "# Sign in broken\nREQ-UI-001 fails.\n");
        var vTriageBase = ProcessRunBaseline.Snapshot(vTriage.Root);
        Assert.True((await Run("triage-issues", 2, vTriage.Context(aBaseline: vTriageBase))).Passed);
        Assert.True((await Run("triage-issues", 3, vTriage.Context(aBaseline: vTriageBase))).Passed);
        Assert.True((await Run("triage-issues", 4, vTriage.Context(aBaseline: vTriageBase))).Passed); // REQ-UI-001 is Not Started: reopened
        vTriage.Write("docs/issues/r2.md", "# Something new\n");
        var vNew = await Run("triage-issues", 4, vTriage.Context(aBaseline: vTriageBase));
        Assert.False(vNew.Passed);
        Assert.Contains("docs/issues/r2.md", vNew.Result, StringComparison.Ordinal);

        // metrics-report: the records add up (3); the report must be written in the run (4)
        using var vMetrics = new TempProject()
            .Write("docs/metrics/runs.jsonl", "{\"tokens_in\":1,\"tokens_out\":2,\"build_result\":\"pass\"}\n")
            .Write("docs/metrics/gates.jsonl", string.Empty);
        Assert.True((await Run("metrics-report", 3, vMetrics.Context())).Passed);
        var vNoReport = await Run("metrics-report", 4, vMetrics.Context());
        Assert.False(vNoReport.Passed);
        Assert.Contains("no metrics report was written during this run", vNoReport.Result, StringComparison.Ordinal);
        vMetrics.Write("docs/X-Metrics-Report.md", "Runs: 1\nTokens: 3\nFailures: 0\n");
        Assert.True((await Run("metrics-report", 4, vMetrics.Context())).Passed);
        vMetrics.Write("docs/X-Metrics-Report.md", "Runs: 1\nTokens: 3\nFailures: 0\n").Touch("docs/X-Metrics-Report.md", DateTime.UtcNow.AddDays(-2));
        Assert.False((await Run("metrics-report", 4, vMetrics.Context())).Passed); // a report from before the run is not the run's
    }
}
