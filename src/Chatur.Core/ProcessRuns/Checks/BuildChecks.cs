using Chatur.Core.Actions;

namespace Chatur.Core.ProcessRuns.Checks;

/// <summary>
/// The checks that judge Chatur's own build and test run of the project (REQ-FN-093, REQ-FN-094): the build and the
/// tests are run by Chatur itself, not taken from the agent's word.
/// </summary>
public static class BuildChecks
{
    /// <summary>Builds the project, and fails when the build does not succeed.</summary>
    /// <param name="aContext">What the check reads.</param>
    /// <param name="aCt">A token that cancels the build.</param>
    public static Task<ProcessStepOutcome> BuildSucceedsAsync(CheckContext aContext, CancellationToken aCt) =>
        aContext.Build.RunAsync(aContext.RunId, aContext.Position, aContext.Folder, aCt);

    /// <summary>Fails when the build Chatur last ran left a warning behind, or did not succeed.</summary>
    /// <param name="aStore">Keeps the run's commands.</param>
    /// <param name="aRunId">The run.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    public static async Task<ProcessStepOutcome> NoWarningAsync(RunEvidenceStore aStore, int aRunId, CancellationToken aCt)
    {
        var vBuilds = await aStore.CommandsAsync(aRunId, RunCommandKind.Build, aCt).ConfigureAwait(false);
        if (vBuilds.Count == 0)
        {
            return new ProcessStepOutcome(true, "Nothing was built, so no build warning was left.");
        }

        var vFailed = vBuilds.FirstOrDefault(aBuild => aBuild.Outcome != RunCommandOutcome.Succeeded);
        if (vFailed is not null)
        {
            return new ProcessStepOutcome(false, $"The build did not succeed: {vFailed.Reason}");
        }

        var vWarnings = vBuilds.Sum(aBuild => aBuild.Warnings ?? 0);
        return vWarnings > 0
            ? new ProcessStepOutcome(false, $"The build left {vWarnings} warning{(vWarnings == 1 ? string.Empty : "s")} behind. Fix them and run again.")
            : new ProcessStepOutcome(true, "The build succeeded and left no warning behind.");
    }

    /// <summary>Runs the project's tests and records every test; the outcome only says how they came out (the next check judges it).</summary>
    /// <param name="aContext">What the check reads.</param>
    /// <param name="aCt">A token that cancels the tests.</param>
    public static Task<ProcessStepOutcome> RunTestsAsync(CheckContext aContext, CancellationToken aCt) =>
        aContext.Tests.RunAsync(aContext.RunId, aContext.Position, aContext.Folder, aCt);

    /// <summary>Fails unless the tests Chatur ran reported results and none failed.</summary>
    /// <param name="aStore">Keeps the run's tests.</param>
    /// <param name="aRunId">The run.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    public static async Task<ProcessStepOutcome> TestsPassAsync(RunEvidenceStore aStore, int aRunId, CancellationToken aCt)
    {
        var vEvidence = await aStore.TestEvidenceAsync(aRunId, aCt).ConfigureAwait(false);
        if (!vEvidence.HasCommands)
        {
            return CheckOutcome.Fail("the tests pass", "the project has no tests to run, so nothing shows that they pass.");
        }

        if (!vEvidence.RanAny)
        {
            return CheckOutcome.Fail("the tests pass", $"the tests could not run: {string.Join(" ", vEvidence.NotRunReasons)}");
        }

        var vFailed = vEvidence.Tests.Where(aTest => aTest.Outcome == ProcessTestOutcome.Failed).Select(aTest => $"{aTest.Name} failed").ToList();
        if (vEvidence.NotRunReasons.Count > 0)
        {
            vFailed.AddRange(vEvidence.NotRunReasons);
        }

        var vPassed = vEvidence.Tests.Count(aTest => aTest.Outcome == ProcessTestOutcome.Passed);
        return CheckOutcome.From("the tests pass", vFailed, $"{vPassed} tests passed.");
    }
}
