using System.Net;
using Chatur.Core.Accounts;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using ChaturDb;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Tests for <see cref="AppManagerDeviceIdHandler"/> against a real, migrated, temporary database: a
/// downloaded Chatur's installed-app key needs this installation's id on every App Manager call
/// (AppManager API usage guide §2.1.1; REQ-NFR-006, decision 2026-10-06).
/// </summary>
public sealed class AppManagerDeviceIdHandlerTests : IDisposable
{
    private readonly string objDatabasePath = Path.Combine(Path.GetTempPath(), $"chatur-deviceid-tests-{Guid.NewGuid():N}.db");

    /// <summary>Runs the real migrations against a fresh temporary database before every test.</summary>
    public AppManagerDeviceIdHandlerTests()
    {
        var vResult = ChaturDbMigrator.Migrate($"Data Source={objDatabasePath}");
        Assert.True(vResult.Successful, vResult.Error?.Message);
    }

    /// <summary>
    /// When Chatur calls App Manager twice, then both calls carry X-Device-Id, it is the same id both
    /// times, and it is the id device sign-in uses for this installation.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-006 every App Manager call carries this installation's device id")]
    public async Task EveryCallCarriesTheInstallationsDeviceId()
    {
        var vDb = new SqliteConnectionFactory(new FixedAppPaths(objDatabasePath));
        var vRecorder = new RecordingHandler();
        using var vClient = new HttpClient(new AppManagerDeviceIdHandler(vDb, new FixedClock()) { InnerHandler = vRecorder })
        {
            BaseAddress = new Uri("https://appmanager.test/"),
        };

        await vClient.GetAsync("AuthSvc/public-key");
        await vClient.PostAsync("AuthSvc/refresh", new StringContent("{}"));

        var vExpected = await InstallationId.GetOrCreateAsync(vDb, new FixedClock());
        Assert.Equal(2, vRecorder.DeviceIds.Count);
        Assert.All(vRecorder.DeviceIds, aId => Assert.Equal(vExpected, aId));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(objDatabasePath))
        {
            File.Delete(objDatabasePath);
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string?> DeviceIds { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage aRequest, CancellationToken aCt)
        {
            DeviceIds.Add(aRequest.Headers.TryGetValues(AppManagerDeviceIdHandler.HeaderName, out var vValues) ? vValues.Single() : null);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FixedAppPaths : IAppPaths
    {
        public FixedAppPaths(string aDatabasePath) => DatabasePath = aDatabasePath;

        public string AppDataFolder => Path.GetDirectoryName(DatabasePath) ?? ".";

        public string DatabasePath { get; }

        public string LogFolder => AppDataFolder;
    }
}
