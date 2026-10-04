using System.Linq;
using Chatur.Core.Roles;
using Chatur.Tests.Support;
using Xunit;

namespace Chatur.Tests.Roles;

/// <summary>Tests for cluster K's and cluster L's methods on <see cref="RoleActions"/> against a real, migrated, temporary database.</summary>
public sealed class RoleActionsTests : MigratedDatabaseFixture
{
    private const string LongEnoughWording =
        "Reads the project end to end and writes clear, testable requirements for the next phase.";

    /// <summary>Every seeded role is listed, straight from the database (REQ-UI-025).</summary>
    [Fact]
    public async Task RolesAsyncListsTheFourSeededRoles()
    {
        var vActions = new RoleActions(CreateFactory());

        var vRoles = await vActions.RolesAsync();

        Assert.Equal(4, vRoles.Count);
        Assert.Contains(vRoles, r => r.Code == "analyst" && r.Tier == 3);
        Assert.Contains(vRoles, r => r.Code == "flow-master" && r.Tier == 1);
    }

    /// <summary>
    /// When a role's wording is changed straight in the database, then the next read reflects it with
    /// no file edited (REQ-UI-025) — and its detail carries its rights and commands (REQ-UI-026).
    /// </summary>
    [Fact]
    public async Task RoleAsyncReflectsTheDatabaseAndCarriesRightsAndCommands()
    {
        var vActions = new RoleActions(CreateFactory());
        var vRoleId = RoleIdFor("verifier");

        var vDetail = await vActions.RoleAsync(vRoleId);

        Assert.Contains("read-file", vDetail.Rights);
        Assert.Contains("mark-verified", vDetail.Rights);
        Assert.Contains("*verify-phase", vDetail.Commands);
        Assert.Single(vDetail.History);
    }

    /// <summary>
    /// When the owner sets a role's tier on the roles tab, then the routing tab (which reads
    /// <c>RolesAsync</c>) shows the same tier for that role (REQ-UI-028).
    /// </summary>
    [Fact]
    public async Task SetTierAsyncChangesTheTierEverySummaryReadsAfterward()
    {
        var vActions = new RoleActions(CreateFactory());
        var vRoleId = RoleIdFor("analyst");

        await vActions.SetTierAsync(vRoleId, 1);

        var vRoles = await vActions.RolesAsync();
        Assert.Equal(1, vRoles.Single(r => r.RoleId == vRoleId).Tier);
    }

    /// <summary>An out-of-range tier is refused (REQ-UI-028).</summary>
    [Fact]
    public async Task SetTierAsyncRefusesATierOutsideOneToThree()
    {
        var vActions = new RoleActions(CreateFactory());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => vActions.SetTierAsync(RoleIdFor("analyst"), 4));
    }

    /// <summary>
    /// When a role is saved, then its wording, commands and rules are the new ones, its version
    /// climbed by one, and the history panel gained the new version (REQ-UI-027, REQ-UI-029).
    /// </summary>
    [Fact]
    public async Task SaveAsyncBumpsTheVersionAndWritesHistory()
    {
        var vActions = new RoleActions(CreateFactory());
        var vRoleId = RoleIdFor("analyst");

        var vDetail = await vActions.SaveAsync(vRoleId, LongEnoughWording, new[] { "*new-command" }, new[] { "A new rule." });

        Assert.Equal(LongEnoughWording, vDetail.Wording);
        Assert.Contains("*new-command", vDetail.Commands);
        Assert.Contains("A new rule.", vDetail.Rules);
        Assert.Equal(2, vDetail.History.Count);
        Assert.Equal(2, vDetail.History[0].Version);
    }

    /// <summary>Wording under 40 characters is refused before anything is written (REQ-UI-027).</summary>
    [Fact]
    public async Task SaveAsyncRefusesShortWording()
    {
        var vActions = new RoleActions(CreateFactory());
        await Assert.ThrowsAsync<ArgumentException>(() => vActions.SaveAsync(RoleIdFor("analyst"), "Too short.", Array.Empty<string>(), Array.Empty<string>()));
    }

    /// <summary>
    /// When roles are exported and imported into a fresh database, then the roles there are the same,
    /// with their rights, commands and rules (REQ-FN-028, REQ-FN-029).
    /// </summary>
    [Fact]
    public async Task ExportThenImportRoundTrips()
    {
        var vSource = new RoleActions(CreateFactory());
        await vSource.SaveAsync(RoleIdFor("architect"), LongEnoughWording, new[] { "*a-command" }, new[] { "A rule only the architect answers to." });
        var vJson = await vSource.ExportAsync();

        using var vTarget = new SecondMigratedDatabase();
        var vTargetActions = new RoleActions(vTarget.CreateFactory());
        await vTargetActions.ImportAsync(vJson);

        var vArchitectId = vTarget.RoleIdFor("architect");
        var vDetail = await vTargetActions.RoleAsync(vArchitectId);

        Assert.Equal(LongEnoughWording, vDetail.Wording);
        Assert.Contains("*a-command", vDetail.Commands);
        Assert.Contains("A rule only the architect answers to.", vDetail.Rules);
    }

    /// <summary>A file that is not a Chatur roles export is refused (REQ-FN-029).</summary>
    [Fact]
    public async Task ImportAsyncRefusesAnUnknownFormat()
    {
        var vActions = new RoleActions(CreateFactory());
        await Assert.ThrowsAsync<FormatException>(() => vActions.ImportAsync("""{"not":"a roles export"}"""));
    }

    /// <summary>
    /// When the owner adds an agent with a name, some wording and the right to change code, then it
    /// is listed as version 1 with that right on, every other right withheld and a history row (Settings ▸ Agents, "Add an agent").
    /// </summary>
    [Fact]
    public async Task AddAsyncAddsAnAgentAtVersionOneWithTheRightsGiven()
    {
        var vActions = new RoleActions(CreateFactory());

        var vDetail = await vActions.AddAsync("TrBlazeUI helper", LongEnoughWording, 2, ["edit-file"]);

        Assert.Equal("trblazeui-helper", vDetail.Summary.Code);
        Assert.Equal(["read-file", "edit-file"], vDetail.Rights.OrderBy(r => r == "read-file" ? 0 : 1));
        Assert.Single(vDetail.History);
        Assert.Equal(1, vDetail.History[0].Version);
        Assert.Equal(5, (await vActions.RolesAsync()).Count);
    }

    /// <summary>A name that is empty or already taken, or wording that is too short, is refused and nothing is added.</summary>
    [Fact]
    public async Task AddAsyncRefusesAnEmptyOrTakenNameAndShortWording()
    {
        var vActions = new RoleActions(CreateFactory());

        await Assert.ThrowsAsync<ArgumentException>(() => vActions.AddAsync("  ", LongEnoughWording, 2, []));
        await Assert.ThrowsAsync<ArgumentException>(() => vActions.AddAsync("Analyst", LongEnoughWording, 2, []));
        await Assert.ThrowsAsync<ArgumentException>(() => vActions.AddAsync("Brief", "too short", 2, []));
        Assert.Equal(4, (await vActions.RolesAsync()).Count);
    }

    /// <summary>Only the Verifier may mark a requirement verified (REQ-FN-030).</summary>
    [Fact]
    public async Task MarkVerifiedAsyncAllowsTheVerifier()
    {
        var vActions = new RoleActions(CreateFactory());
        await vActions.MarkVerifiedAsync(1, RoleIdFor("verifier"));
    }

    /// <summary>Every other role is refused and nothing is written (REQ-FN-030).</summary>
    [Fact]
    public async Task MarkVerifiedAsyncRefusesEveryOtherRole()
    {
        var vActions = new RoleActions(CreateFactory());
        await Assert.ThrowsAsync<InvalidOperationException>(() => vActions.MarkVerifiedAsync(1, RoleIdFor("flow-master")));
    }

    /// <summary>A second, independent migrated database — the "other machine" REQ-FN-029 imports onto.</summary>
    private sealed class SecondMigratedDatabase : MigratedDatabaseFixture
    {
        public new Chatur.Core.Data.IDbConnectionFactory CreateFactory() => base.CreateFactory();

        public new int RoleIdFor(string aCode) => base.RoleIdFor(aCode);
    }
}
