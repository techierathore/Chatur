using Chatur.Core.Actions;
using Chatur.Core.BuildRun;
using Chatur.Core.ProcessRuns;
using Chatur.Core.ProcessRuns.Checks;
using Chatur.Core.Platform;
using Chatur.Tests.Support;
using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Chatur.Tests.ProcessRuns.Checks;

/// <summary>
/// Tests for the checks that judge the checklists, the status and the phases files, and Chatur's own build and test run:
/// build-phase, fix-issues, handoff-phase and refresh-status (REQ-FN-093, 094, 095, 096).
/// </summary>
public sealed class StatusChecksTests : MigratedDatabaseFixture
{
    private static string Checklist(params (string Id, string Status, string Remarks)[] aRows)
    {
        var vTable = string.Join("\n", aRows.Select(aRow => $"| {aRow.Id} | Row {aRow.Id} | {aRow.Status} | 0% | {aRow.Remarks} | |"));
        var vDetails = string.Join("\n", aRows.Select(aRow => $"- **{aRow.Id}** — Row {aRow.Id}. *BRD:* BRD-1\n  - *Acceptance:* When a, then b."));
        return $"# Checklist\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|-------------|--------|---|---------|---------|\n{vTable}\n\n## Page: P (`/p`)\n\n{vDetails}\n";
    }

    /// <summary>
    /// When an open row is still Not Started, or an Implemented row has no test carrying its id, then the build-phase check
    /// fails naming it; when every open row is Implemented and tested, it passes (REQ-FN-093).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-093 build-phase passes only when every open row is Implemented and a test carries its id")]
    public void BuildPhaseNeedsEveryOpenRowImplementedAndTested()
    {
        using var vProject = new TempProject()
            .Write("docs/X-Checklist.md", Checklist(("REQ-FN-001", "Implemented", string.Empty), ("REQ-FN-002", "Not Started", string.Empty)))
            .Write("tests/unit/OneTests.cs", "[Fact(DisplayName = \"REQ-FN-001 does it\")]");

        var vOpen = StatusChecks.OpenRowsImplemented(vProject.Context());
        vProject.Write("docs/X-Checklist.md", Checklist(("REQ-FN-001", "Implemented", string.Empty), ("REQ-FN-002", "Implemented", string.Empty)));
        var vUntested = StatusChecks.OpenRowsImplemented(vProject.Context());
        vProject.Write("tests/unit/TwoTests.cs", "[Fact(DisplayName = \"REQ-FN-002 does it\")]");
        var vDone = StatusChecks.OpenRowsImplemented(vProject.Context());

        Assert.False(vOpen.Passed);
        Assert.Contains("REQ-FN-002 is still Not Started", vOpen.Result, StringComparison.Ordinal);
        Assert.False(vUntested.Passed);
        Assert.Contains("no test under tests/ carries REQ-FN-002", vUntested.Result, StringComparison.Ordinal);
        Assert.True(vDone.Passed, vDone.Result);
    }

    /// <summary>When a build leaves a warning behind, or does not succeed, the no-warning check fails; a clean build passes (REQ-FN-093, REQ-FN-094).</summary>
    [Fact(DisplayName = "REQ-FN-093 Chatur's own build must leave no warning")]
    public async Task OwnBuildMustLeaveNoWarning()
    {
        var vStore = new RunEvidenceStore(CreateFactory(), new SystemClock());
        var vRunId = StartRun();
        await vStore.SaveCommandAsync(vRunId, 4, Build(RunCommandOutcome.Succeeded, 0, null));
        var vClean = await BuildChecks.NoWarningAsync(vStore, vRunId, default);
        await vStore.ClearAsync(vRunId, RunCommandKind.Build);
        await vStore.SaveCommandAsync(vRunId, 4, Build(RunCommandOutcome.Succeeded, 0, 2));
        var vWarned = await BuildChecks.NoWarningAsync(vStore, vRunId, default);
        await vStore.ClearAsync(vRunId, RunCommandKind.Build);
        await vStore.SaveCommandAsync(vRunId, 4, Build(RunCommandOutcome.Failed, 1, null));
        var vFailed = await BuildChecks.NoWarningAsync(vStore, vRunId, default);

        Assert.True(vClean.Passed);
        Assert.False(vWarned.Passed);
        Assert.Contains("2 warnings", vWarned.Result, StringComparison.Ordinal);
        Assert.False(vFailed.Passed);
    }

    /// <summary>When any test Chatur ran failed, or no test ran, the tests-pass check fails; when all passed it passes (REQ-FN-093, REQ-FN-094).</summary>
    [Fact(DisplayName = "REQ-FN-094 the tests Chatur runs itself must all pass")]
    public async Task OwnTestsMustAllPass()
    {
        var vStore = new RunEvidenceStore(CreateFactory(), new SystemClock());
        var vRunId = StartRun();
        var vNone = await BuildChecks.TestsPassAsync(vStore, vRunId, default);
        await vStore.SaveCommandAsync(vRunId, 5, new RunCommandRecord(RunCommandKind.Test, "dotnet test", RunCommandOutcome.Succeeded, 0, null, 1, 1, string.Empty, null));
        await vStore.SaveTestsAsync(vRunId, [new ReportedTest("dotnet", "REQ-FN-001 a", ProcessTestOutcome.Passed, ["REQ-FN-001"], null, 1)]);
        var vPassed = await BuildChecks.TestsPassAsync(vStore, vRunId, default);
        await vStore.SaveTestsAsync(vRunId, [new ReportedTest("dotnet", "REQ-FN-002 b", ProcessTestOutcome.Failed, ["REQ-FN-002"], "boom", 1)]);
        var vFailed = await BuildChecks.TestsPassAsync(vStore, vRunId, default);

        Assert.False(vNone.Passed);
        Assert.Contains("no tests to run", vNone.Result, StringComparison.Ordinal);
        Assert.True(vPassed.Passed, vPassed.Result);
        Assert.False(vFailed.Passed);
        Assert.Contains("REQ-FN-002 b failed", vFailed.Result, StringComparison.Ordinal);
    }

    /// <summary>When a row verification left FAIL or Needs re-verify is still that, the fix-issues check fails; when each is Implemented again it passes (REQ-FN-094).</summary>
    [Fact(DisplayName = "REQ-FN-094 fix-issues passes only when no row is left FAIL or Needs re-verify")]
    public void FixIssuesNeedsEveryFailedRowImplementedAgain()
    {
        using var vProject = new TempProject().Write("docs/X-Checklist.md", Checklist(("REQ-FN-001", "FAIL", string.Empty), ("REQ-FN-002", "Needs re-verify", string.Empty), ("REQ-FN-003", "Verified", string.Empty)));

        var vLeft = StatusChecks.FailedRowsImplemented(vProject.Context());
        vProject.Write("docs/X-Checklist.md", Checklist(("REQ-FN-001", "Implemented", string.Empty), ("REQ-FN-002", "Implemented", string.Empty), ("REQ-FN-003", "Verified", string.Empty)));
        var vDone = StatusChecks.FailedRowsImplemented(vProject.Context());

        Assert.False(vLeft.Passed);
        Assert.Contains("REQ-FN-001 is still FAIL", vLeft.Result, StringComparison.Ordinal);
        Assert.Contains("REQ-FN-002 is still Needs re-verify", vLeft.Result, StringComparison.Ordinal);
        Assert.True(vDone.Passed, vDone.Result);
    }

    /// <summary>
    /// When a row of the phase is open, the hand-off check fails; Verified, Done (pre-existing), N/A and Blocked with a reason
    /// are closed; and the status and phases files must record the hand-off dated that day (REQ-FN-095).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-095 handoff-phase passes only when no row is open and the status and phases files record the hand-off")]
    public void HandoffNeedsAClosedPhaseAndTheRecord()
    {
        var vToday = DateTime.UtcNow;
        var vDay = vToday.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        using var vProject = new TempProject()
            .Write("docs/X-P2-Checklist.md", Checklist(("REQ-FN-001", "Verified", string.Empty), ("REQ-FN-002", "Blocked", string.Empty), ("REQ-FN-003", "Implemented", string.Empty)))
            .Write("docs/X-Phases.md", "| Phase | Name | Screens | BRD range | Status |\n|---|---|---|---|---|\n| 2 | Two | A | BRD-1 | building |\n")
            .Write("PROJECT-STATUS.md", "# Status\n\nNo hand-off yet.\n");

        var vOpen = StatusChecks.NoRowOpen(vProject.Context());
        vProject.Write("docs/X-P2-Checklist.md", Checklist(("REQ-FN-001", "Verified", string.Empty), ("REQ-FN-002", "Blocked", "waits on the library"), ("REQ-FN-003", "N/A", string.Empty), ("REQ-FN-004", "Done (pre-existing)", string.Empty)));
        var vClosed = StatusChecks.NoRowOpen(vProject.Context());
        var vNothing = StatusChecks.HandoffRecorded(vProject.Context(aToday: vToday));
        vProject.Write("PROJECT-STATUS.md", $"# Status\n\n- {vDay} hand-off of phase 2: all rows closed.\n");
        var vNotDone = StatusChecks.HandoffRecorded(vProject.Context(aToday: vToday));
        vProject.Write("docs/X-Phases.md", "| Phase | Name | Screens | BRD range | Status |\n|---|---|---|---|---|\n| 2 | Two | A | BRD-1 | done |\n");
        var vRecorded = StatusChecks.HandoffRecorded(vProject.Context(aToday: vToday));
        vProject.Write("PROJECT-STATUS.md", "# Status\n\n- 2020-01-01 hand-off of phase 2.\n");
        var vOldDate = StatusChecks.HandoffRecorded(vProject.Context(aToday: vToday));

        Assert.False(vOpen.Passed);
        Assert.Contains("REQ-FN-002 is Blocked", vOpen.Result, StringComparison.Ordinal);
        Assert.Contains("REQ-FN-003 is Implemented", vOpen.Result, StringComparison.Ordinal);
        Assert.True(vClosed.Passed, vClosed.Result);
        Assert.False(vNothing.Passed);
        Assert.False(vNotDone.Passed);
        Assert.Contains("does not mark phase 2 done", vNotDone.Result, StringComparison.Ordinal);
        Assert.True(vRecorded.Passed, vRecorded.Result);
        Assert.False(vOldDate.Passed);
        Assert.Contains("dated", vOldDate.Result, StringComparison.Ordinal);
    }

    /// <summary>
    /// When the status document was written in the run and counts each status as the checklists do, the refresh-status check
    /// passes; a wrong count, no count, or a document not written in the run fails (REQ-FN-096).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-096 refresh-status passes only when the status written in the run counts as the checklists do")]
    public void RefreshStatusMustCountAsTheChecklistsDo()
    {
        using var vProject = new TempProject()
            .Write("docs/X-Checklist.md", Checklist(("REQ-FN-001", "Implemented", string.Empty), ("REQ-FN-002", "Implemented", string.Empty), ("REQ-FN-003", "Not Started", string.Empty)));
        var vRun = DateTime.UtcNow.AddMinutes(-1);

        var vSum = StatusChecks.CountsAddUp(vProject.Context(vRun));
        vProject.Write("PROJECT-STATUS.md", "# Status\n\n| Status | Count |\n|---|---|\n| Not Started | 1 |\n| Implemented | 2 |\n");
        var vRight = StatusChecks.StatusMatchesChecklists(vProject.Context(vRun));
        vProject.Write("PROJECT-STATUS.md", "# Status\n\n| Status | Count |\n|---|---|\n| Not Started | 1 |\n| Implemented | 3 |\n");
        var vWrong = StatusChecks.StatusMatchesChecklists(vProject.Context(vRun));
        vProject.Write("PROJECT-STATUS.md", "# Status\n\nAll fine.\n");
        var vNone = StatusChecks.StatusMatchesChecklists(vProject.Context(vRun));
        vProject.Write("PROJECT-STATUS.md", "| Not Started | 1 |\n| Implemented | 2 |\n").Touch("PROJECT-STATUS.md", DateTime.UtcNow.AddDays(-2));
        var vOld = StatusChecks.StatusMatchesChecklists(vProject.Context(vRun));

        Assert.True(vSum.Passed, vSum.Result);
        Assert.True(vRight.Passed, vRight.Result);
        Assert.False(vWrong.Passed);
        Assert.Contains("counts 3 implemented but the checklists have 2", vWrong.Result, StringComparison.Ordinal);
        Assert.False(vNone.Passed);
        Assert.False(vOld.Passed);
        Assert.Contains("not written during this run", vOld.Result, StringComparison.Ordinal);
    }

    /// <summary>
    /// When the project was already built (the agent's own run-build left it up to date) and its code has a warning, then
    /// Chatur's own build still reports it and the no-warning check fails: an incremental build skips the compiler and says
    /// 0 warnings, which would let a warning through (REQ-FN-093, REQ-FN-094).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-093 a warning an earlier build already reported is still found by Chatur's own build")]
    public async Task OwnBuildFindsAWarningAnEarlierBuildLeftUpToDate()
    {
        using var vProject = new TempProject();
        FixtureDotNetProject.Write(vProject.Root);
        vProject.Write("src/Warn.cs", "namespace Fixture; public static class Warn { public static int One() { int unused = 1; return 1; } }\n");
        var vRunner = new ProjectCommandRunner(NullLogger<ProjectCommandRunner>.Instance);
        var vEarlier = await vRunner.RunAsync(new ProjectCommand(ProjectToolchain.DotNetExecutable(), "build --nologo -tl:off -nodeReuse:false", vProject.Root, TimeSpan.FromMinutes(5), 16_000));
        var vStore = new RunEvidenceStore(CreateFactory(), new SystemClock());
        var vRunId = StartRun();

        var vOutcome = await new VerifyBuildStep(vRunner, vStore, new ProcessRunOptions()).RunAsync(vRunId, 4, vProject.Root, default);
        var vCheck = await BuildChecks.NoWarningAsync(vStore, vRunId, default);

        Assert.Equal(0, vEarlier.ExitCode);
        Assert.Contains("1 Warning(s)", vEarlier.Tail, StringComparison.Ordinal); // the agent's own build saw it once; the next one is up to date
        Assert.True(vOutcome.Passed, vOutcome.Result);
        Assert.False(vCheck.Passed);
        Assert.Contains("1 warning", vCheck.Result, StringComparison.Ordinal);
    }

    private static RunCommandRecord Build(RunCommandOutcome aOutcome, int aExit, int? aWarnings) =>
        new(RunCommandKind.Build, "dotnet build", aOutcome, aExit, aWarnings, 1, 1, string.Empty, aOutcome == RunCommandOutcome.Succeeded ? null : "dotnet build exited with code 1.");

    private int StartRun()
    {
        var vProjectId = CreateProject("P", Path.Combine(Path.GetTempPath(), $"chatur-unused-{Guid.NewGuid():N}"));
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        return (int)vConnection.ExecuteScalar<long>(
            "INSERT INTO ProcessRun (ProjectId, ProcessCode, State, CurrentStep, StartedUtc) VALUES (@vProjectId, 'build-phase', 'running', 1, @Started); SELECT last_insert_rowid();",
            new { vProjectId, Started = DateTime.UtcNow.ToString("O") });
    }
}
