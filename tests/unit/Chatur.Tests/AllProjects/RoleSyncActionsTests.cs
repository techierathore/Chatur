using Chatur.Core;
using Chatur.Core.Actions;
using Chatur.Core.Platform;
using Chatur.Core.Roles;
using Chatur.Tests.Support;
using ChaturDb;
using Xunit;

namespace Chatur.Tests.AllProjects;

/// <summary>
/// Tests for the roles sync (REQ-FN-078): two machines are two migrated databases under the test bin folder,
/// one account, one in-memory store standing in for App Manager.
/// </summary>
public sealed class RoleSyncActionsTests : IDisposable
{
    private readonly List<string> objFiles = [];
    private readonly MemoryStore objStore = new();
    private readonly Machine objHere;
    private readonly Machine objThere;

    /// <summary>Starts two machines with the seeded roles.</summary>
    public RoleSyncActionsTests()
    {
        objHere = NewMachine();
        objThere = NewMachine();
    }

    /// <summary>Removes both databases.</summary>
    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var vFile in objFiles)
        {
            File.Delete(vFile);
        }
    }

    /// <summary>When a rule is added on one machine and it syncs, then the other has it after it syncs (REQ-FN-078).</summary>
    [Fact]
    public async Task ARuleChangedOnOneMachineArrivesOnTheOther()
    {
        await objHere.Sync.SyncNowAsync();
        await objThere.Sync.SyncNowAsync(); // first sync takes what is there; the seeds are the same
        await objHere.Roles.SaveAsync(RoleId(objHere, "analyst"), new string('w', 50), [], ["A rule made here."]);

        var vSent = await objHere.Sync.SyncNowAsync();
        var vTaken = await objThere.Sync.SyncNowAsync();

        Assert.Contains("Sent", vSent.Outcome);
        Assert.Contains("Took", vTaken.Outcome);
        var vDetail = await objThere.Roles.RoleAsync(RoleId(objThere, "analyst"));
        Assert.Contains("A rule made here.", vDetail.Rules);
        Assert.Equal(new string('w', 50), vDetail.Wording);
        Assert.All((await objThere.Sync.StatusAsync()).Machines, aMachine => Assert.True(aMachine.UpToDate));
    }

    /// <summary>A rule removed on one machine is removed on the other, not left behind.</summary>
    [Fact]
    public async Task ARemovedRuleIsRemovedOnTheOtherMachine()
    {
        await objHere.Roles.SaveAsync(RoleId(objHere, "analyst"), new string('w', 50), [], ["Short lived."]);
        await objHere.Sync.SyncNowAsync();
        await objThere.Sync.SyncNowAsync();
        await objHere.Roles.SaveAsync(RoleId(objHere, "analyst"), new string('w', 50), [], []);
        await objHere.Sync.SyncNowAsync();

        await objThere.Sync.SyncNowAsync();

        Assert.DoesNotContain("Short lived.", (await objThere.Roles.RoleAsync(RoleId(objThere, "analyst"))).Rules);
    }

    /// <summary>When nothing changed anywhere, a sync changes nothing and says so.</summary>
    [Fact]
    public async Task SyncingWithNothingChangedSaysSo()
    {
        await objHere.Sync.SyncNowAsync();

        var vAgain = await objHere.Sync.SyncNowAsync();

        Assert.Equal("Already up to date.", vAgain.Outcome);
    }

    /// <summary>A head with no store says App Manager keeps nothing yet, and sends nothing.</summary>
    [Fact]
    public async Task WithoutAStoreNothingIsSentAndTheReasonIsGiven()
    {
        var vMachine = NewMachine(new UnavailableRoleSyncStore());

        var vStatus = await vMachine.Sync.SyncNowAsync();

        Assert.False(vStatus.Available);
        Assert.NotNull(vStatus.UnavailableReason);
        Assert.Null(vStatus.Outcome);
    }

    private static int RoleId(Machine aMachine, string aCode) => aMachine.Roles.RolesAsync().GetAwaiter().GetResult().Single(aRole => aRole.Code == aCode).RoleId;

    private Machine NewMachine(IRoleSyncStore? aStore = null)
    {
        var vFile = Path.Combine(AppContext.BaseDirectory, $"rolesync-{Guid.NewGuid():N}.db");
        objFiles.Add(vFile);
        var vString = $"Data Source={vFile}";
        Assert.True(ChaturDbMigrator.Migrate(vString).Successful);
        var vFactory = new TestDbConnectionFactory(vString);
        var vState = new AppState();
        vState.SetCurrentUser(new CurrentUser("u1", "a@b.c", "A"));
        var vRoles = new RoleActions(vFactory);
        return new Machine(vRoles, new RoleSyncActions(vRoles, aStore ?? objStore, vFactory, new SystemClock(), vState));
    }

    private sealed record Machine(RoleActions Roles, RoleSyncActions Sync);

    private sealed class MemoryStore : IRoleSyncStore
    {
        private readonly List<RoleSyncRecord> objRecords = [];

        public bool IsAvailable => true;

        public string? UnavailableReason => null;

        public Task<IReadOnlyList<RoleSyncRecord>> ListAsync(string aAccountId, CancellationToken aCt = default) =>
            Task.FromResult<IReadOnlyList<RoleSyncRecord>>(objRecords.ToList());

        public Task SaveAsync(string aAccountId, RoleSyncRecord aRecord, CancellationToken aCt = default)
        {
            objRecords.RemoveAll(aOld => aOld.DeviceId == aRecord.DeviceId);
            objRecords.Add(aRecord);
            return Task.CompletedTask;
        }
    }
}
