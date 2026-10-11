using System.Text;
using Chatur.Core.Actions;
using Chatur.Core.ProcessRuns;
using Chatur.Tests.Support;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Chatur.Tests.ProcessRuns;

/// <summary>
/// Tests for the verdicts a <c>verify-phase</c> run writes into the project's checklist. They run
/// Chatur's real build and test steps against a real temporary .NET test project whose tests carry
/// requirement ids, one passing and one failing, so a verdict is proved from real results and not from
/// a stand-in (REQ-UI-048, REQ-FN-053, REQ-FN-055).
/// </summary>
public sealed class RunVerdictTests : MigratedDatabaseFixture
{
    private const string ChecklistPath = "docs/App-P2-Checklist.md";

    private const string Checklist =
        "# App P2 checklist\r\n" +
        "\r\n" +
        "## Requirements Status\r\n" +
        "\r\n" +
        "| Req ID | Title | Status | % | Remarks | Detail |\r\n" +
        "|---|---|---|---|---|---|\r\n" +
        "| REQ-FN-001 | First | Implemented | 75% | [REQ-FN-001] built. | [view](#d-1) |\r\n" +
        "| REQ-FN-002 | Second | Implemented | 75% | [REQ-FN-002] built. | [view](#d-2) |\r\n" +
        "| REQ-FN-003 | Third | Not Started | 0% | | [view](#d-3) |\r\n" +
        "| REQ-FN-004 | Fourth | Verified | 100% | done before. | [view](#d-4) |\r\n" +
        "| REQ-FN-005 | Fifth | Implemented | 50% | [REQ-FN-005] built. | [view](#d-5) |\r\n" +
        "| REQ-FN-006 | Sixth | Implemented | 75% | ⚠ visual: clipped. | [view](#d-6) |\r\n" +
        "| REQ-FN-007 | Seventh | Needs re-verify | 75% | earlier note. | [view](#d-7) |\r\n" +
        "| REQ-FN-008 | Eighth | Blocked | 25% | waits. | [view](#d-8) |\r\n" +
        "| REQ-FN-009 | Ninth | N/A | 0% | | [view](#d-9) |\r\n" +
        "\r\n" +
        "## Page: Home (`/`)\r\n" +
        "\r\n" +
        "- **REQ-FN-001** — First. *BRD:* BRD-1 — **Implemented**\r\n" +
        "  - *Acceptance:* When a, then b.\r\n" +
        "- **REQ-FN-005** — Fifth. *BRD:* BRD-5 — **Implemented**\r\n";

    private readonly string objProjectPath = Path.Combine(Path.GetTempPath(), $"chatur-verdict-{Guid.NewGuid():N}");
    private readonly string objAppData = Path.Combine(Path.GetTempPath(), $"chatur-verdict-data-{Guid.NewGuid():N}");

    /// <summary>
    /// When an approved verify run builds and tests a real project, then a requirement whose tests all
    /// passed is Verified at 100%, one with a failing test is Needs re-verify naming that test, one no
    /// test carries is Needs re-verify "no test carries", a row with a recorded defect is graded too,
    /// rows that are not waiting are left alone, every other line is byte for byte as it was, and the
    /// byte-order mark and CRLF line endings are kept (REQ-UI-048).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-048 an approved run writes Verified only where a passing test carries the id")]
    public async Task ApprovedRunGradesEachRequirementFromItsTests()
    {
        FixtureDotNetProject.Write(objProjectPath);
        WriteChecklist(Checklist, bom: true);
        var vActions = CreateActions();
        var vRunId = StartRun(CreateProject("P", objProjectPath));

        var vWaiting = await vActions.AdvanceAsync(vRunId);
        Assert.Equal(ProcessRunState.Waiting, vWaiting!.State);
        var vAfter = await vActions.AnswerAsync(vRunId, ProcessAnswer.Yes);
        var vVerdicts = (await vActions.VerdictsAsync(vRunId)).ToDictionary(aVerdict => aVerdict.RequirementId);

        Assert.Equal(ProcessRunState.Finished, vAfter!.State);
        Assert.Equal(["REQ-FN-001", "REQ-FN-002", "REQ-FN-005", "REQ-FN-006", "REQ-FN-007"], vVerdicts.Keys.Order());
        AssertVerified(vVerdicts["REQ-FN-001"], vRunId);
        AssertVerified(vVerdicts["REQ-FN-006"], vRunId);
        AssertVerified(vVerdicts["REQ-FN-007"], vRunId);
        AssertNeedsReverify(vVerdicts["REQ-FN-002"], 75, "1 of 1 test failed: REQ-FN-002 second requirement fails.");
        AssertNeedsReverify(vVerdicts["REQ-FN-005"], 50, "no test carries REQ-FN-005.");
        AssertEvidence(vVerdicts);
        AssertOnlyGradedLinesChanged();
        AssertBomAndCrlfKept();
        Assert.Contains("| REQ-FN-002 | Second | Needs re-verify | 75% | [REQ-FN-002] built. [run ", ReadChecklistLines()[7], StringComparison.Ordinal);
        Assert.Contains("exit code 0", vAfter.Steps[1].Result, StringComparison.Ordinal);
        Assert.Contains("limit 10 min", vAfter.Steps[1].Result, StringComparison.Ordinal);
        Assert.StartsWith("3 tests ran: 2 passed, 1 failed", vAfter.Steps[3].Result, StringComparison.Ordinal);
    }

    /// <summary>
    /// When the owner answers no at the approval stop, or the run is stopped there, then the run still
    /// really built and tested but no verdict is written and no line of the checklist changes
    /// (REQ-UI-048).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-048 a refused or stopped approval writes nothing")]
    public async Task RefusedOrStoppedApprovalWritesNothing()
    {
        FixtureDotNetProject.Write(objProjectPath);
        WriteChecklist(Checklist);
        var vActions = CreateActions();
        var vProjectId = CreateProject("P", objProjectPath);
        var vStopped = StartRun(vProjectId);
        await vActions.AdvanceAsync(vStopped);

        await vActions.StopAsync(vStopped);

        Assert.Equal(Checklist, ReadChecklist());
        Assert.Empty(await vActions.VerdictsAsync(vStopped));

        var vRefused = StartRun(vProjectId);
        await vActions.AdvanceAsync(vRefused);
        var vAfter = await vActions.AnswerAsync(vRefused, ProcessAnswer.No);

        Assert.Equal(ProcessRunState.Finished, vAfter!.State);
        Assert.Equal(Checklist, ReadChecklist());
        Assert.Empty(await vActions.VerdictsAsync(vRefused));
        Assert.Contains("did not approve", vAfter.Steps[6].Result, StringComparison.Ordinal);
    }

    /// <summary>
    /// When the project's build fails, then the run stops on the build step with its exit code and the
    /// end of its output as the reason, no test ran, and nothing is written (REQ-FN-053, REQ-UI-048).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-053 a failed build stops the run on that step with nothing written")]
    public async Task FailedBuildStopsTheRunAndWritesNothing()
    {
        FixtureDotNetProject.Write(objProjectPath, aBrokenBuild: true);
        WriteChecklist(Checklist);
        var vActions = CreateActions();
        var vRunId = StartRun(CreateProject("P", objProjectPath));

        var vAfter = await vActions.AdvanceAsync(vRunId);

        Assert.Equal(ProcessRunState.Stopped, vAfter!.State);
        Assert.Equal(2, vAfter.CurrentPosition);
        Assert.Equal(ProcessStepState.Failed, vAfter.Steps[1].State);
        Assert.Contains("exited with code 1", vAfter.Steps[1].Result, StringComparison.Ordinal);
        Assert.Contains("error", vAfter.Steps[1].Result, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(ProcessStepState.ToCome, vAfter.Steps[3].State);
        Assert.Equal(Checklist, ReadChecklist());
        Assert.Empty(await vActions.VerdictsAsync(vRunId));
        await Assert.ThrowsAsync<InvalidOperationException>(() => vActions.AnswerAsync(vRunId, ProcessAnswer.Yes));
    }

    /// <summary>
    /// When the project has no tests at all, then every graded requirement is Needs re-verify "no test
    /// carries this id" and none is ever Verified (REQ-UI-048).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-048 a project with no tests never gets a Verified")]
    public async Task ProjectWithoutTestsNeverGetsVerified()
    {
        Directory.CreateDirectory(Path.Combine(objProjectPath, "docs"));
        File.WriteAllText(Path.Combine(objProjectPath, "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        File.WriteAllText(Path.Combine(objProjectPath, "Program.cs"), "System.Console.WriteLine(1);");
        File.WriteAllText(Path.Combine(objProjectPath, "Directory.Build.props"), "<Project />");
        WriteChecklist(Checklist);
        var vActions = CreateActions();
        var vRunId = StartRun(CreateProject("P", objProjectPath));
        await vActions.AdvanceAsync(vRunId);

        var vAfter = await vActions.AnswerAsync(vRunId, ProcessAnswer.Yes);
        var vVerdicts = await vActions.VerdictsAsync(vRunId);

        Assert.Equal(ProcessRunState.Finished, vAfter!.State);
        Assert.StartsWith("No tests found", vAfter.Steps[3].Result, StringComparison.Ordinal);
        Assert.Equal(5, vVerdicts.Count);
        Assert.All(vVerdicts, aVerdict =>
        {
            Assert.Equal("Needs re-verify", aVerdict.Status);
            Assert.Contains($"no test carries {aVerdict.RequirementId}.", aVerdict.Remark, StringComparison.Ordinal);
            Assert.Equal(0, aVerdict.Evidence!.TestsFound);
            Assert.False(aVerdict.Evidence.TestsRan);
        });
        Assert.DoesNotContain("| Verified | 100% | [REQ", ReadChecklist(), StringComparison.Ordinal);
    }

    /// <summary>
    /// When the tests run past their time limit, then they are stopped, the step records the limit,
    /// and every requirement is Needs re-verify "tests could not run" (REQ-UI-048).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-048 tests stopped at their time limit grade as could not run")]
    public async Task TestsThatRunPastTheirLimitCannotVerifyAnything()
    {
        FixtureDotNetProject.Write(objProjectPath);
        WriteChecklist(Checklist);
        var vActions = CreateActions(new ProcessRunOptions { TestTimeout = TimeSpan.FromSeconds(1) });
        var vRunId = StartRun(CreateProject("P", objProjectPath));
        await vActions.AdvanceAsync(vRunId);

        var vAfter = await vActions.AnswerAsync(vRunId, ProcessAnswer.Yes);
        var vVerdicts = await vActions.VerdictsAsync(vRunId);

        Assert.Equal(ProcessRunState.Finished, vAfter!.State);
        Assert.Contains("limit of 1 s", vAfter.Steps[3].Result, StringComparison.Ordinal);
        Assert.Equal(5, vVerdicts.Count);
        Assert.All(vVerdicts, aVerdict =>
        {
            Assert.Equal("Needs re-verify", aVerdict.Status);
            Assert.Contains("tests could not run:", aVerdict.Remark, StringComparison.Ordinal);
        });
    }

    /// <summary>When the project has no checklist, then an approved run still finishes and writes nothing (REQ-UI-048).</summary>
    [Fact(DisplayName = "REQ-UI-048 a project without a checklist finishes with no verdict")]
    public async Task ProjectWithoutChecklistFinishesWithNoVerdict()
    {
        Directory.CreateDirectory(objProjectPath);
        var vActions = CreateActions();
        var vRunId = StartRun(CreateProject("P", objProjectPath));
        await vActions.AdvanceAsync(vRunId);

        var vAfter = await vActions.AnswerAsync(vRunId, ProcessAnswer.Yes);

        Assert.Equal(ProcessRunState.Finished, vAfter!.State);
        Assert.Empty(await vActions.VerdictsAsync(vRunId));
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        foreach (var vFolder in new[] { objProjectPath, objAppData })
        {
            if (Directory.Exists(vFolder))
            {
                Directory.Delete(vFolder, true);
            }
        }
    }

    private static void AssertVerified(ProcessRunVerdict aVerdict, int aRunId)
    {
        Assert.Equal("Verified", aVerdict.Status);
        Assert.Equal(100, aVerdict.Percent);
        Assert.Equal(ChecklistPath, aVerdict.ChecklistFile);
        Assert.StartsWith($"[run {aRunId}] verify-phase ", aVerdict.Remark, StringComparison.Ordinal);
        Assert.Contains($"Verified — 1 of 1 test carrying {aVerdict.RequirementId} passed.", aVerdict.Remark, StringComparison.Ordinal);
    }

    private static void AssertNeedsReverify(ProcessRunVerdict aVerdict, int aPercent, string aReason)
    {
        Assert.Equal("Needs re-verify", aVerdict.Status);
        Assert.Equal(aPercent, aVerdict.Percent);
        Assert.EndsWith("Needs re-verify — " + aReason, aVerdict.Remark, StringComparison.Ordinal);
    }

    private static void AssertEvidence(Dictionary<string, ProcessRunVerdict> aVerdicts)
    {
        var vPassing = aVerdicts["REQ-FN-001"].Evidence!;
        Assert.True(vPassing.TestsRan);
        Assert.Equal((1, 1, 0), (vPassing.TestsFound, vPassing.TestsPassed, vPassing.TestsFailed));
        var vFailing = aVerdicts["REQ-FN-002"].Evidence!;
        Assert.Equal((1, 0, 1), (vFailing.TestsFound, vFailing.TestsPassed, vFailing.TestsFailed));
        var vTest = Assert.Single(vFailing.Tests);
        Assert.Equal(FixtureDotNetProject.FailingName, vTest.Name);
        Assert.Equal(ProcessTestOutcome.Failed, vTest.Outcome);
        Assert.Contains("deliberate failure", vTest.Message, StringComparison.Ordinal);
        var vNone = aVerdicts["REQ-FN-005"].Evidence!;
        Assert.True(vNone.TestsRan);
        Assert.Equal(0, vNone.TestsFound);
    }

    private void AssertOnlyGradedLinesChanged()
    {
        var vBefore = Checklist.Split("\r\n");
        var vNow = ReadChecklist().Split("\r\n");
        Assert.Equal(vBefore.Length, vNow.Length);
        var vChanged = Enumerable.Range(0, vBefore.Length)
            .Where(aIndex => vBefore[aIndex] != vNow[aIndex])
            .Select(aIndex => vBefore[aIndex][..Math.Min(14, vBefore[aIndex].Length)])
            .ToList();
        Assert.Equal(["| REQ-FN-001 |", "| REQ-FN-002 |", "| REQ-FN-005 |", "| REQ-FN-006 |", "| REQ-FN-007 |", "- **REQ-FN-001", "- **REQ-FN-005"], vChanged);
        Assert.EndsWith("| [view](#d-1) |", vNow[6], StringComparison.Ordinal);
        Assert.EndsWith("— **Verified**", vNow[18], StringComparison.Ordinal);
        Assert.EndsWith("— **Needs re-verify**", vNow[20], StringComparison.Ordinal);
    }

    private void AssertBomAndCrlfKept()
    {
        var vBytes = File.ReadAllBytes(Path.Combine(objProjectPath, ChecklistPath));
        Assert.True(vBytes.AsSpan(0, 3).SequenceEqual(Encoding.UTF8.GetPreamble()));
        var vText = Encoding.UTF8.GetString(vBytes, 3, vBytes.Length - 3);
        Assert.Equal(Checklist.Split("\r\n").Length, vText.Split("\r\n").Length);
        Assert.DoesNotContain("\n", vText.Replace("\r\n", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    private void WriteChecklist(string aText, bool bom = false)
    {
        Directory.CreateDirectory(Path.Combine(objProjectPath, "docs"));
        File.WriteAllText(Path.Combine(objProjectPath, ChecklistPath), aText, new UTF8Encoding(bom));
    }

    private string ReadChecklist() => File.ReadAllText(Path.Combine(objProjectPath, ChecklistPath));

    private string[] ReadChecklistLines() => ReadChecklist().Split("\r\n");

    private ProcessRunActions CreateActions(ProcessRunOptions? aOptions = null) =>
        ProcessRunWiring.Actions(CreateFactory(), ProcessRunWiring.Executor(CreateFactory(), objAppData, aOptions ?? new ProcessRunOptions()));

    private int StartRun(int aProjectId)
    {
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText =
            """
            INSERT INTO ProcessRun (ProjectId, ProcessCode, State, CurrentStep, StartedUtc)
            VALUES (@ProjectId, 'verify-phase', 'running', 1, @Started);
            SELECT last_insert_rowid();
            """;
        vCommand.Parameters.AddWithValue("@ProjectId", aProjectId);
        vCommand.Parameters.AddWithValue("@Started", DateTime.UtcNow.ToString("O"));
        return (int)(long)vCommand.ExecuteScalar()!;
    }
}
