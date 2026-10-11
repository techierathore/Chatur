namespace Chatur.Core.ProcessRuns.Checks;

/// <summary>
/// Which check judges which row of which process (the per-process table of phase 3's BRD, REQ-FN-092..102). A check
/// reads the project's files on disk; a row that is a check but has no entry here fails instead of passing, so a
/// check never passes on "not carried out" (REQ-FN-088).
/// </summary>
public static class ProcessChecks
{
    /// <summary>One check: reads the project, says whether it holds.</summary>
    /// <param name="aContext">What the check reads.</param>
    /// <param name="aCt">A token that cancels the check.</param>
    public delegate Task<Chatur.Core.Actions.ProcessStepOutcome> Check(CheckContext aContext, CancellationToken aCt);

    /// <summary>The check for a row, or <see langword="null"/> when the process has none there.</summary>
    /// <param name="aProcessCode">The process.</param>
    /// <param name="aPosition">The 1-based row.</param>
    public static Check? Find(string aProcessCode, int aPosition) => (aProcessCode.ToLowerInvariant(), aPosition) switch
    {
        ("day1-greenfield", 3) => Sync(DocumentChecks.BriefExists),
        ("day1-greenfield", 4) => Sync(DocumentChecks.BriefNamesThree),
        ("day1-greenfield", 5) => Sync(DocumentChecks.BrdRules),
        ("day1-greenfield", 8) => Sync(DocumentChecks.Day1Documents),

        ("split-brd", 2) => Sync(DocumentChecks.PhasesHaveRanges),
        ("split-brd", 3) => Sync(DocumentChecks.EveryIdInOnePhase),
        ("split-brd", 5) => Sync(DocumentChecks.PhaseFilesWhole),

        ("build-phase", 2) => Sync(StatusChecks.ChecklistReadable),
        ("build-phase", 3) => Sync(StatusChecks.ClusterIsOpenRows),
        ("build-phase", 4) => BuildChecks.BuildSucceedsAsync,
        ("build-phase", 5) => (aContext, aCt) => BuildChecks.NoWarningAsync(aContext.Store, aContext.RunId, aCt),
        ("build-phase", 7) => BuildChecks.RunTestsAsync,
        ("build-phase", 8) => (aContext, aCt) => BuildChecks.TestsPassAsync(aContext.Store, aContext.RunId, aCt),
        ("build-phase", 10) => Sync(StatusChecks.OpenRowsImplemented),

        ("fix-issues", 3) => BuildChecks.BuildSucceedsAsync,
        ("fix-issues", 4) => (aContext, aCt) => BuildChecks.NoWarningAsync(aContext.Store, aContext.RunId, aCt),
        ("fix-issues", 5) => BuildChecks.RunTestsAsync,
        ("fix-issues", 6) => (aContext, aCt) => BuildChecks.TestsPassAsync(aContext.Store, aContext.RunId, aCt),
        ("fix-issues", 7) => Sync(StatusChecks.FailedRowsImplemented),

        ("handoff-phase", 1) => Sync(StatusChecks.ChecklistReadable),
        ("handoff-phase", 2) => Sync(StatusChecks.NoRowOpen),
        ("handoff-phase", 4) => Sync(StatusChecks.HandoffRecorded),

        ("refresh-status", 2) => Sync(StatusChecks.ChecklistReadable),
        ("refresh-status", 3) => Sync(StatusChecks.CountsAddUp),
        ("refresh-status", 4) => Sync(StatusChecks.StatusMatchesChecklists),

        ("deploy-checklist", 2) => Sync(DocumentChecks.DeployListWhole),
        ("deploy-checklist", 3) => Sync(DocumentChecks.DeployItemsNameWho),
        ("deploy-checklist", 5) => Sync(DocumentChecks.DeployCoversAreas),

        ("mockups", 2) => Sync(DocumentChecks.ScreensHaveMockups),
        ("mockups", 3) => Sync(DocumentChecks.ScreensHaveDesign),
        ("mockups", 4) => Sync(DocumentChecks.UiRowsLinked),

        ("amend-docs", 2 or 3) => async (aContext, aCt) => DocumentChecks.TouchedDocumentsNamed(aContext, await TouchedAsync(aContext, aCt).ConfigureAwait(false)),
        ("amend-docs", 5) => async (aContext, aCt) => DocumentChecks.DocumentsAgree(aContext, await TouchedAsync(aContext, aCt).ConfigureAwait(false)),

        ("triage-issues", 2) => Sync(DocumentChecks.ReportsReadable),
        ("triage-issues", 3) => Sync(DocumentChecks.ReportsHaveChecklist),
        ("triage-issues", 4) => Sync(DocumentChecks.ReportsHaveRows),

        ("metrics-report", 3) => Sync(DocumentChecks.RecordsAddUp),
        ("metrics-report", 4) => Sync(DocumentChecks.ReportMatchesRecords),

        _ => null
    };

    /// <summary>Whether Chatur carries out the row itself (it runs the project's tests) rather than handing it to the agent (the agent holds no right to run tests).</summary>
    /// <param name="aProcessCode">The process.</param>
    /// <param name="aPosition">The 1-based row.</param>
    public static bool ChaturRunsItself(string aProcessCode, int aPosition) => (aProcessCode.ToLowerInvariant(), aPosition) is ("build-phase", 7) or ("fix-issues", 5);

    private static Check Sync(Func<CheckContext, Chatur.Core.Actions.ProcessStepOutcome> aCheck) =>
        (aContext, _) => Task.FromResult(aCheck(aContext));

    /// <summary>The documents the agent named as touched in its final message for step 2 of amend-docs.</summary>
    private static async Task<IReadOnlyList<string>> TouchedAsync(CheckContext aContext, CancellationToken aCt)
    {
        var vWork = aContext.Agents is null ? null : await aContext.Agents.WorkAsync(aContext.RunId, 2, aCt).ConfigureAwait(false);
        return DocumentChecks.NamedDocuments(aContext.Documents, vWork?.FinalMessage);
    }
}
