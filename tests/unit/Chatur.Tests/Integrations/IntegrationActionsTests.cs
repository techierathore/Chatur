using Chatur.Core;
using Chatur.Core.Actions;
using Chatur.Core.Integrations;
using Chatur.Core.Platform;
using Chatur.Tests.Support;
using ChaturDb;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chatur.Tests.Integrations;

/// <summary>
/// Tests for <see cref="IntegrationActions"/> and <see cref="IntegrationToken"/> against a real, migrated database kept
/// under the test project's own bin folder and deleted when the test ends (REQ-FN-081 to REQ-FN-083, REQ-UI-069).
/// </summary>
public sealed class IntegrationActionsTests : IDisposable
{
    private readonly string objDatabasePath = Path.Combine(AppContext.BaseDirectory, $"integrations-{Guid.NewGuid():N}.db");
    private readonly string objConnectionString;
    private readonly FakeProcesses objProcesses = new();

    /// <summary>Migrates a fresh database for each test.</summary>
    public IntegrationActionsTests()
    {
        objConnectionString = $"Data Source={objDatabasePath}";
        Assert.True(ChaturDbMigrator.Migrate(objConnectionString).Successful);
        using var vConnection = new SqliteConnection(objConnectionString);
        vConnection.Execute("INSERT INTO Project (Name, Path, Kind) VALUES ('TfLens', 'C:/x/TfLens', 'dotnet')");
    }

    /// <summary>When Integrations opens, then every action is listed with what starts it and a METHOD /path address, and only the built one can be on (REQ-FN-081).</summary>
    [Fact(DisplayName = "REQ-FN-081 every action is listed with what starts it and its address")]
    public async Task EveryActionIsListedWithStarterAndAddress()
    {
        var vActions = (await Create().ListActionsAsync()).ToList();

        Assert.Equal(6, vActions.Count);
        Assert.All(vActions, aAction =>
        {
            Assert.False(string.IsNullOrWhiteSpace(aAction.StartedBy));
            Assert.Matches(@"^(GET|POST) /v1/chatur/\S+$", aAction.Address);
        });
        Assert.True(vActions.Single(aAction => aAction.Code == "start").Enabled);
        Assert.DoesNotContain(vActions, aAction => !aAction.Built && aAction.Enabled);
    }

    /// <summary>When the owner turns the start action off, then it is off on the next read; an unbuilt action cannot be turned on (REQ-FN-081).</summary>
    [Fact(DisplayName = "REQ-FN-081 the owner turns an action off and an unbuilt one cannot be on")]
    public async Task OwnerTurnsAnActionOff()
    {
        var vActions = Create();

        await vActions.SetActionEnabledAsync("start", false);

        Assert.False((await vActions.ListActionsAsync()).Single(aAction => aAction.Code == "start").Enabled);
        await Assert.ThrowsAsync<ArgumentException>(() => vActions.SetActionEnabledAsync("board", true));
        await Assert.ThrowsAsync<ArgumentException>(() => vActions.SetActionEnabledAsync("nothing", true));
    }

    /// <summary>When a tool calls start with a token made here, then the process starts on the named project and the run is listed (REQ-FN-082).</summary>
    [Fact(DisplayName = "REQ-FN-082 an outside call starts the process and the run is listed")]
    public async Task OutsideCallStartsProcessAndRunIsListed()
    {
        var vActions = Create();
        var vToken = await vActions.CreateTokenAsync();

        var vResult = await vActions.StartFromOutsideAsync(vToken.Value, "tflens", "verify-phase");

        Assert.Equal(OutsideCallOutcome.Started, vResult.Outcome);
        Assert.Equal(("verify-phase", 1), (objProcesses.Started.Single().Process, vResult.RunId));
        var vRuns = await vActions.ListOutsideRunsAsync();
        Assert.Equal([1], vRuns.Select(aRun => aRun.RunId));
        Assert.Equal("TfLens", vRuns[0].ProjectName);
    }

    /// <summary>When the token is missing, wrong or the action is off, then nothing starts (REQ-FN-082).</summary>
    [Fact(DisplayName = "REQ-FN-082 no token, a wrong token or an action that is off starts nothing")]
    public async Task RefusedCallsStartNothing()
    {
        var vActions = Create();
        var vToken = await vActions.CreateTokenAsync();
        var vWrong = vToken.Value[..^1] + (vToken.Value[^1] == '0' ? '1' : '0');

        Assert.Equal(OutsideCallOutcome.Unauthorized, (await vActions.StartFromOutsideAsync(null, "TfLens", "verify-phase")).Outcome);
        Assert.Equal(OutsideCallOutcome.Unauthorized, (await vActions.StartFromOutsideAsync(vWrong, "TfLens", "verify-phase")).Outcome);
        Assert.Equal(OutsideCallOutcome.Refused, (await vActions.StartFromOutsideAsync(vToken.Value, "TfLens", "not-a-process")).Outcome);
        Assert.Equal(OutsideCallOutcome.Refused, (await vActions.StartFromOutsideAsync(vToken.Value, "Nobody", "verify-phase")).Outcome);
        await vActions.SetActionEnabledAsync("start", false);
        Assert.Equal(OutsideCallOutcome.NotThere, (await vActions.StartFromOutsideAsync(vToken.Value, "TfLens", "verify-phase")).Outcome);
        Assert.Empty(objProcesses.Started);
    }

    /// <summary>The token's account is readable before the secret is checked, and only a hash of it is kept (REQ-FN-082).</summary>
    [Fact(DisplayName = "REQ-FN-082 a token names its account and only its hash is kept")]
    public async Task TokenNamesAccountAndOnlyHashIsKept()
    {
        var vToken = IntegrationToken.Make("user-42");
        Assert.True(IntegrationToken.TryReadAccount(vToken, out var vAccount));
        Assert.Equal("user-42", vAccount);
        Assert.False(IntegrationToken.TryReadAccount("chatur_pat_x", out _));

        var vMade = await Create().CreateTokenAsync();
        using var vConnection = new SqliteConnection(objConnectionString);
        var vStored = vConnection.Query<string>("SELECT TokenHash || Hint FROM IntegrationToken").Single();
        Assert.DoesNotContain(vMade.Value, vStored);
    }

    /// <summary>When the verifier records a check of a built application's window, then it is listed with its screenshots; none kept or another platform is refused (REQ-FN-083).</summary>
    [Fact(DisplayName = "REQ-FN-083 a window check is listed with its screenshots and a check with none is refused")]
    public async Task WindowCheckIsListedWithScreenshots()
    {
        var vActions = Create();
        await vActions.RecordWindowCheckAsync(new WindowCheck("TfLens · Windows", "Windows", "DESK", 12, new DateTime(2026, 9, 22, 8, 40, 0, DateTimeKind.Utc), "passed"));

        var vChecks = await vActions.ListWindowChecksAsync();

        Assert.Equal(12, vChecks.Single().ScreenshotsKept);
        Assert.True(vChecks[0].Passed);
        await Assert.ThrowsAsync<ArgumentException>(() => vActions.RecordWindowCheckAsync(new WindowCheck("A", "Windows", "M", 0, DateTime.UtcNow, "passed")));
        await Assert.ThrowsAsync<ArgumentException>(() => vActions.RecordWindowCheckAsync(new WindowCheck("A", "Linux", "M", 3, DateTime.UtcNow, "passed")));
    }

    /// <summary>When a release is published, then the newest release's Mac and Windows downloads are listed with who signed them and the notarisation state (REQ-UI-069).</summary>
    [Fact(DisplayName = "REQ-UI-069 the newest release lists signed Mac and Windows downloads with their state")]
    public async Task NewestReleaseListsSignedDownloads()
    {
        var vActions = Create();
        var vWhen = new DateTime(2026, 9, 20, 18, 40, 0, DateTimeKind.Utc);
        await vActions.RecordDownloadAsync(new SignedDownload("0.0.9", "Mac", "Chatur-0.0.9.dmg", "Old Signer", NotarisationState.NotNotarised, vWhen.AddDays(-9)));
        await vActions.RecordDownloadAsync(new SignedDownload("0.1.0", "Windows", "Chatur-0.1.0.msix", "A certificate", NotarisationState.NotApplicable, vWhen.AddMinutes(12)));
        await vActions.RecordDownloadAsync(new SignedDownload("0.1.0", "Mac", "Chatur-0.1.0.dmg", "Developer ID", NotarisationState.Notarised, vWhen));

        var vDownloads = await vActions.ListDownloadsAsync();

        Assert.Equal(["Mac", "Windows"], vDownloads.Select(aDownload => aDownload.Platform));
        Assert.All(vDownloads, aDownload => Assert.Equal("0.1.0", aDownload.Version));
        Assert.Equal(NotarisationState.Notarised, vDownloads[0].Notarisation);
        await Assert.ThrowsAsync<ArgumentException>(() => vActions.RecordDownloadAsync(new SignedDownload("0.2.0", "Mac", "x.dmg", " ", NotarisationState.Notarised, vWhen)));
    }

    /// <summary>Deletes the temporary database.</summary>
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(objDatabasePath))
        {
            File.Delete(objDatabasePath);
        }
    }

    private IntegrationActions Create() =>
        new(new TestDbConnectionFactory(objConnectionString), objProcesses, new FixedClock(), new AppState(), NullLogger<IntegrationActions>.Instance);

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeProcesses : IProcessActions
    {
        public List<(int ProjectId, string Process)> Started { get; } = [];

        public Task<ProcessRunStarted> StartAsync(int aProjectId, string aProcessCode, CancellationToken aCt = default)
        {
            Started.Add((aProjectId, aProcessCode));
            return Task.FromResult(new ProcessRunStarted(Started.Count, aProcessCode, aProjectId, "Running", 1, string.Empty, DateTime.UtcNow, false));
        }

        public Task<IReadOnlyList<ProcessSummary>> ListAsync(int? aProjectId, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<ProcessStepInfo>> StepsAsync(string aProcessCode, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task SaveStepWordingAsync(string aProcessCode, string aStepCode, string aText, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task<ProcessDocuments?> DocumentsAsync(int aProjectId, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task WriteDocumentAsync(int aProjectId, string aRelativePath, string aContent, CancellationToken aCt = default) => throw new NotSupportedException();
    }
}
