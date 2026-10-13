using Chatur.Core.Actions;
using Chatur.Core.Platform;
using Chatur.Core.Processes;
using Chatur.Tests.Support;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chatur.Tests.Processes;

/// <summary>
/// Tests for <see cref="ProcessActions"/> and <see cref="ProcessDocumentGuard"/> against a real,
/// migrated, temporary SQLite database and a real temporary project folder (REQ-FN-049,
/// REQ-FN-050, REQ-FN-051).
/// </summary>
public sealed class ProcessActionsTests : MigratedDatabaseFixture
{
    private readonly string objProjectRoot = Path.Combine(Path.GetTempPath(), $"chatur-proc-{Guid.NewGuid():N}");
    private readonly ClockStub objClock = new(new DateTime(2026, 10, 6, 9, 0, 0, DateTimeKind.Utc));
    private readonly ProcessActions objSut;
    private readonly int objProjectId;

    /// <summary>Creates a project folder with a BRD, a checklist, a status document and an unrelated file.</summary>
    public ProcessActionsTests()
    {
        Directory.CreateDirectory(Path.Combine(objProjectRoot, "docs"));
        File.WriteAllText(Path.Combine(objProjectRoot, "docs", "App-P1-BRD.md"), "old");
        File.WriteAllText(Path.Combine(objProjectRoot, "docs", "App-P2-BRD.md"), "brd");
        File.WriteAllText(Path.Combine(objProjectRoot, "docs", "App-P2-Checklist.md"), "checklist");
        File.WriteAllText(Path.Combine(objProjectRoot, "README.md"), "readme");
        objProjectId = CreateProject("App", objProjectRoot);
        objSut = new ProcessActions(CreateFactory(), objClock, NullLogger<ProcessActions>.Instance);
    }

    /// <summary>When Processes opens, then all twelve TechieFlow commands are listed with what each does, in order.</summary>
    [Fact]
    public async Task ListShowsAllTwelveCommands()
    {
        var vList = await objSut.ListAsync(objProjectId);

        Assert.Equal(12, vList.Count);
        Assert.Equal("day1-greenfield", vList[0].Code);
        Assert.Equal("metrics-report", vList[^1].Code);
        Assert.All(vList, aRow => Assert.False(string.IsNullOrWhiteSpace(aRow.Description)));
    }

    /// <summary>The steps, checks and owner stops of verify-phase are seven, four and one.</summary>
    [Fact]
    public async Task VerifyPhaseShapeIsSevenFourOne()
    {
        var vVerify = (await objSut.ListAsync(objProjectId)).Single(aRow => aRow.Code == "verify-phase");

        Assert.Equal((7, 4, 1), (vVerify.Steps, vVerify.Checks, vVerify.OwnerStops));
    }

    /// <summary>Every command has seeded wording for every one of its rows, so no row of any process is blank.</summary>
    [Fact]
    public async Task EveryStepOfEveryCommandHasWording()
    {
        foreach (var vProcess in ProcessCatalogue.All)
        {
            var vSteps = await objSut.StepsAsync(vProcess.Code);

            Assert.Equal(vProcess.Steps, vSteps.Count);
            Assert.All(vSteps, aStep => Assert.False(string.IsNullOrWhiteSpace(aStep.Wording), $"{vProcess.Code} {aStep.StepCode}"));
            Assert.Equal(vProcess.OwnerStops, vSteps.Count(aStep => aStep.Kind == ProcessStepKind.OwnerStop));
        }
    }

    /// <summary>A command never started on the project has no last run; once started, it shows when.</summary>
    [Fact]
    public async Task LastRunIsEmptyUntilTheCommandIsStarted()
    {
        Assert.All(await objSut.ListAsync(objProjectId), aRow => Assert.Null(aRow.LastRunUtc));

        await objSut.StartAsync(objProjectId, "build-phase");

        var vBuild = (await objSut.ListAsync(objProjectId)).Single(aRow => aRow.Code == "build-phase");
        Assert.Equal(objClock.UtcNow, vBuild.LastRunUtc);
        Assert.True(vBuild.IsRunning);
        Assert.NotNull(vBuild.RunningRunId);
    }

    /// <summary>When the owner starts a process, a running run for that project begins on its first step.</summary>
    [Fact]
    public async Task StartBeginsTheFirstStepOnTheProject()
    {
        var vStarted = await objSut.StartAsync(objProjectId, "verify-phase");

        Assert.Equal(objProjectId, vStarted.ProjectId);
        Assert.Equal("running", vStarted.State);
        Assert.Equal(1, vStarted.CurrentStep);
        Assert.Equal("Read the phase checklist and collect every requirement", vStarted.FirstStepWording);
        Assert.False(vStarted.AlreadyRunning);
    }

    /// <summary>Starting a process that is already running on the project returns that run instead of a second one.</summary>
    [Fact]
    public async Task StartingARunningProcessAgainReturnsTheSameRun()
    {
        var vFirst = await objSut.StartAsync(objProjectId, "verify-phase");
        var vSecond = await objSut.StartAsync(objProjectId, "verify-phase");

        Assert.Equal(vFirst.RunId, vSecond.RunId);
        Assert.True(vSecond.AlreadyRunning);
    }

    /// <summary>An unknown command or an unknown project is refused and starts nothing.</summary>
    [Fact]
    public async Task StartRefusesAnUnknownCommandOrProject()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => objSut.StartAsync(objProjectId, "rm-rf"));
        await Assert.ThrowsAsync<ArgumentException>(() => objSut.StartAsync(9999, "verify-phase"));

        Assert.All(await objSut.ListAsync(objProjectId), aRow => Assert.False(aRow.IsRunning));
    }

    /// <summary>When a step's wording is changed in the database, then the next read returns the new wording (REQ-UI-046's data path).</summary>
    [Fact]
    public async Task ChangedWordingIsReadBackWithoutAnyFile()
    {
        using (var vConnection = new SqliteConnection(ConnectionString))
        {
            await vConnection.ExecuteAsync("UPDATE ProcessStepText SET Text = 'Reworded' WHERE ProcessCode = 'verify-phase' AND StepCode = 's2'");
        }

        var vSteps = await objSut.StepsAsync("verify-phase");

        Assert.Equal("Reworded", vSteps[1].Wording);
    }

    /// <summary>The panel names the project's newest BRD and checklist and the status document, and nothing else.</summary>
    [Fact]
    public async Task DocumentsNameTheProjectsOwnThree()
    {
        var vDocuments = await objSut.DocumentsAsync(objProjectId);

        Assert.Equal(new ProcessDocuments("docs/App-P2-BRD.md", "docs/App-P2-Checklist.md", "PROJECT-STATUS.md"), vDocuments);
    }

    /// <summary>When a process writes, then the BRD, the checklist and the status document are written.</summary>
    [Fact]
    public async Task TheThreeDocumentsAreWritten()
    {
        await objSut.WriteDocumentAsync(objProjectId, "docs/App-P2-BRD.md", "new brd");
        await objSut.WriteDocumentAsync(objProjectId, "docs/App-P2-Checklist.md", "new checklist");
        await objSut.WriteDocumentAsync(objProjectId, "PROJECT-STATUS.md", "new status");

        Assert.Equal("new brd", File.ReadAllText(Path.Combine(objProjectRoot, "docs", "App-P2-BRD.md")));
        Assert.Equal("new checklist", File.ReadAllText(Path.Combine(objProjectRoot, "docs", "App-P2-Checklist.md")));
        Assert.Equal("new status", File.ReadAllText(Path.Combine(objProjectRoot, "PROJECT-STATUS.md")));
    }

    /// <summary>Any other file is refused and the folder is left exactly as it was.</summary>
    [Theory]
    [InlineData("README.md")]
    [InlineData("docs/notes.md")]
    [InlineData("docs/sub/App-P2-BRD.md")]
    [InlineData("../outside-BRD.md")]
    [InlineData("docs/../README.md")]
    [InlineData("docs/App-P2-Checklist.html")]
    [InlineData("src/Program.cs")]
    [InlineData("chatur.db")]
    [InlineData("/etc/passwd")]
    public async Task AnythingElseIsRefusedAndNothingIsWritten(string aPath)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => objSut.WriteDocumentAsync(objProjectId, aPath, "x"));

        var vFiles = Directory.EnumerateFiles(objProjectRoot, "*", SearchOption.AllDirectories)
            .Select(aFile => Path.GetRelativePath(objProjectRoot, aFile).Replace('\\', '/'))
            .OrderBy(aFile => aFile, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(
            new List<string> { "README.md", "docs/App-P1-BRD.md", "docs/App-P2-BRD.md", "docs/App-P2-Checklist.md" }
                .OrderBy(aFile => aFile, StringComparer.Ordinal),
            vFiles);
        Assert.Equal("readme", File.ReadAllText(Path.Combine(objProjectRoot, "README.md")));
    }

    /// <summary>The guard resolves only a path that stays inside the project folder.</summary>
    [Fact]
    public void GuardResolvesOnlyInsideTheProject()
    {
        Assert.True(ProcessDocumentGuard.TryResolve(objProjectRoot, "docs/X-Checklist.md", out var vFull));
        Assert.StartsWith(Path.GetFullPath(objProjectRoot), vFull);
        Assert.False(ProcessDocumentGuard.TryResolve(objProjectRoot, "docs/../../X-Checklist.md", out _));
    }

    /// <summary>REQ-UI-075: saving a step's wording stores it, counts its version up, shows it in the next StepsAsync and leaves a kept correction holding the old words.</summary>
    [Fact]
    public async Task SavedStepWordingIsStoredVersionedAndRemembered()
    {
        var vOld = (await objSut.StepsAsync("verify-phase")).First(aStep => aStep.StepCode == "s2").Wording;

        await objSut.SaveStepWordingAsync("verify-phase", "s2", "  Build the whole solution first  ");

        var vNow = (await objSut.StepsAsync("verify-phase")).First(aStep => aStep.StepCode == "s2");
        Assert.Equal("Build the whole solution first", vNow.Wording);
        using var vConnection = new SqliteConnection(ConnectionString);
        Assert.Equal(2, vConnection.ExecuteScalar<int>("SELECT Version FROM ProcessStepText WHERE ProcessCode='verify-phase' AND StepCode='s2'"));
        var vCorrection = vConnection.QuerySingle<(string Target, string Before, string After, string Status)>(
            "SELECT Target, Before, After, Status FROM Correction");
        Assert.Equal(("step:verify-phase/s2", vOld, "Build the whole solution first", "Kept"), vCorrection);
    }

    /// <summary>REQ-UI-075: an empty wording, an unknown process and an unknown step are each refused and nothing is written.</summary>
    [Fact]
    public async Task SavingBlankOrUnknownStepWordingIsRefused()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => objSut.SaveStepWordingAsync("verify-phase", "s1", "   "));
        await Assert.ThrowsAsync<ArgumentException>(() => objSut.SaveStepWordingAsync("no-such-process", "s1", "x"));
        await Assert.ThrowsAsync<ArgumentException>(() => objSut.SaveStepWordingAsync("verify-phase", "s99", "x"));
        await Assert.ThrowsAsync<ArgumentException>(() => objSut.SaveStepWordingAsync("verify-phase", "x1", "x"));

        using var vConnection = new SqliteConnection(ConnectionString);
        Assert.Equal(0, vConnection.ExecuteScalar<int>("SELECT COUNT(*) FROM Correction"));
        Assert.Equal(1, vConnection.ExecuteScalar<int>("SELECT Version FROM ProcessStepText WHERE ProcessCode='verify-phase' AND StepCode='s1'"));
    }

    /// <summary>Removes the temporary project folder.</summary>
    public override void Dispose()
    {
        base.Dispose();
        if (Directory.Exists(objProjectRoot))
        {
            Directory.Delete(objProjectRoot, recursive: true);
        }
    }

    private sealed class ClockStub : IClock
    {
        public ClockStub(DateTime aNow) => UtcNow = aNow;

        public DateTime UtcNow { get; }
    }
}
