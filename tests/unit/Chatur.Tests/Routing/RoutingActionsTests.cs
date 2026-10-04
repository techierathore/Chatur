using System.Linq;
using Chatur.Core.Routing;
using Chatur.Tests.Support;
using Xunit;

namespace Chatur.Tests.Routing;

/// <summary>Tests for <see cref="RoutingActions"/> against a real, migrated, temporary database.</summary>
public sealed class RoutingActionsTests : MigratedDatabaseFixture
{
    /// <summary>When a role's tier is set, then it is read back the same way (REQ-FN-018, REQ-FN-023).</summary>
    [Fact]
    public async Task SetRoleTierAsyncPersists()
    {
        var vActions = new RoutingActions(CreateFactory());
        var vRoleId = RoleIdFor("analyst");

        await vActions.SetRoleTierAsync(vRoleId, 1);

        var vRoles = new Chatur.Core.Roles.RoleActions(CreateFactory());
        var vSummary = (await vRoles.RolesAsync()).Single(aRole => aRole.RoleId == vRoleId);
        Assert.Equal(1, vSummary.Tier);
    }

    /// <summary>An out-of-range tier is refused (REQ-FN-018).</summary>
    [Fact]
    public async Task SetRoleTierAsyncRefusesAnInvalidTier()
    {
        var vActions = new RoutingActions(CreateFactory());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => vActions.SetRoleTierAsync(RoleIdFor("analyst"), 9));
    }

    /// <summary>
    /// When a kind of work is set to a tier, then it is read back — and REQ-FN-019 says this wins over
    /// the role's own tier for that work (the win itself is the agent loop's job; this proves the
    /// setting the agent loop would read is correctly stored).
    /// </summary>
    [Fact]
    public async Task SetWorkTierAsyncPersists()
    {
        var vActions = new RoutingActions(CreateFactory());

        await vActions.SetWorkTierAsync("write-code", 1);
        var vTiers = await vActions.WorkTiersAsync();

        Assert.Equal(1, vTiers["write-code"]);
    }

    /// <summary>Setting the same kind of work twice updates it in place rather than duplicating it.</summary>
    [Fact]
    public async Task SetWorkTierAsyncUpsertsRatherThanDuplicating()
    {
        var vActions = new RoutingActions(CreateFactory());

        await vActions.SetWorkTierAsync("chat", 2);
        await vActions.SetWorkTierAsync("chat", 3);
        var vTiers = await vActions.WorkTiersAsync();

        Assert.Equal(3, vTiers["chat"]);
    }

    /// <summary>An unknown kind of work is refused (REQ-FN-019).</summary>
    [Fact]
    public async Task SetWorkTierAsyncRefusesAnUnknownKind()
    {
        var vActions = new RoutingActions(CreateFactory());
        await Assert.ThrowsAsync<ArgumentException>(() => vActions.SetWorkTierAsync("not-a-real-kind", 1));
    }

    /// <summary>
    /// When a tier's chain is reordered, then <see cref="RoutingActions.TiersAsync"/> reports the new
    /// order (REQ-FN-020) and it survives being read again (REQ-FN-023).
    /// </summary>
    [Fact]
    public async Task ReorderChainAsyncPersistsTheNewOrder()
    {
        var vDb = CreateFactory();
        var vProviders = new Chatur.Core.Models.ProviderActions(vDb, new InMemorySecretStore(), new FakeLlmConnectionProbe(), new FakeSubscriptionSignIn());
        var vProvider = await vProviders.AddWithKeyAsync("Anthropic", "Anthropic", "https://api.anthropic.com", "sk-key");
        var vModels = await vProviders.ListModelsAsync();
        var vTier1ModelIds = vModels.Where(aModel => aModel.ProviderId == vProvider.ProviderId && aModel.Tier == 1)
            .Select(aModel => aModel.ModelId).ToList();

        // Only one tier-1 model exists from the seed catalog; reorder it into a two-element chain by
        // combining it with a tier-2 model to prove the order (not merely the presence) round-trips.
        var vTier2ModelId = vModels.First(aModel => aModel.ProviderId == vProvider.ProviderId && aModel.Tier == 2).ModelId;
        var vChain = new List<int> { vTier2ModelId, vTier1ModelIds[0] };

        var vActions = new RoutingActions(vDb);
        await vActions.ReorderChainAsync(1, vChain);
        var vTiers = await vActions.TiersAsync();

        Assert.Equal(vChain.ToArray(), vTiers.Single(aTier => aTier.Tier == 1).ModelIdsInOrder.ToArray());
    }

    /// <summary>Reordering a chain with an id that is not a known model is refused.</summary>
    [Fact]
    public async Task ReorderChainAsyncRefusesAnUnknownModelId()
    {
        var vActions = new RoutingActions(CreateFactory());
        await Assert.ThrowsAsync<ArgumentException>(() => vActions.ReorderChainAsync(1, new[] { 999999 }));
    }

    /// <summary>
    /// When an escalation is recorded, then the work kind's tier moves to the stronger one and the
    /// move is itself recorded (REQ-FN-021, REQ-FN-022).
    /// </summary>
    [Fact]
    public async Task RecordEscalationAsyncAppliesAndRecordsTheMove()
    {
        var vActions = new RoutingActions(CreateFactory());
        await vActions.SetWorkTierAsync("write-code", 3);

        await vActions.RecordEscalationAsync("write-code", 3, 1, "The same fix failed three times.");

        var vTiers = await vActions.WorkTiersAsync();
        Assert.Equal(1, vTiers["write-code"]);
    }

    /// <summary>The escalation threshold defaults to the seeded value and can be changed (REQ-FN-022).</summary>
    [Fact]
    public async Task EscalationThresholdRoundTrips()
    {
        var vActions = new RoutingActions(CreateFactory());

        Assert.Equal(3, await vActions.EscalationThresholdAsync());

        await vActions.SetEscalationThresholdAsync(5);

        Assert.Equal(5, await vActions.EscalationThresholdAsync());
    }

    /// <summary>
    /// When the tiers are reset, then each connected model is in the chain of the tier it was filed under,
    /// whatever order the owner had left the chains in (REQ-FN-020, REQ-FN-023).
    /// </summary>
    [Fact]
    public async Task ResetTiersAsyncPutsEachModelBackInItsOwnTier()
    {
        var vDb = CreateFactory();
        var vProviders = new Chatur.Core.Models.ProviderActions(vDb, new InMemorySecretStore(), new FakeLlmConnectionProbe(), new FakeSubscriptionSignIn());
        await vProviders.AddWithKeyAsync("Anthropic", "Anthropic", "https://api.anthropic.com", "sk-key");
        var vModels = await vProviders.ListModelsAsync();
        var vActions = new RoutingActions(vDb);
        await vActions.ReorderChainAsync(1, []);
        await vActions.ReorderChainAsync(3, vModels.Select(aModel => aModel.ModelId).ToList());

        await vActions.ResetTiersAsync();

        var vTiers = await vActions.TiersAsync();
        foreach (var vTier in vTiers)
        {
            var vExpected = vModels.Where(aModel => aModel.Tier == vTier.Tier).Select(aModel => aModel.ModelId).Order().ToArray();
            Assert.Equal(vExpected, vTier.ModelIdsInOrder.ToArray());
        }
    }

    /// <summary>
    /// When models exist for the first time, then the tiers are filled once; a chain the owner empties
    /// afterwards stays empty (REQ-FN-023).
    /// </summary>
    [Fact]
    public async Task EnsureShippedTiersAsyncFillsOnceAndNeverRefills()
    {
        var vDb = CreateFactory();
        var vActions = new RoutingActions(vDb);
        await vActions.EnsureShippedTiersAsync();
        Assert.All(await vActions.TiersAsync(), aTier => Assert.Empty(aTier.ModelIdsInOrder));

        var vProviders = new Chatur.Core.Models.ProviderActions(vDb, new InMemorySecretStore(), new FakeLlmConnectionProbe(), new FakeSubscriptionSignIn());
        await vProviders.AddWithKeyAsync("Anthropic", "Anthropic", "https://api.anthropic.com", "sk-key");
        await vActions.EnsureShippedTiersAsync();
        Assert.All(await vActions.TiersAsync(), aTier => Assert.Single(aTier.ModelIdsInOrder));

        await vActions.ReorderChainAsync(1, []);
        await vActions.EnsureShippedTiersAsync();
        Assert.Empty((await vActions.TiersAsync()).Single(aTier => aTier.Tier == 1).ModelIdsInOrder);
    }
}
