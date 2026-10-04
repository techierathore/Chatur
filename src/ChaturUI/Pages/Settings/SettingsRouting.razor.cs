using Chatur.Core.Actions;

namespace ChaturUI.Pages.Settings;

/// <summary>
/// Settings ▸ Routing (mockups/settings-routing.html). Orders each tier's fallback chain
/// (REQ-FN-020), sets a tier for each role (REQ-FN-018) and for each kind of work (REQ-FN-019), and
/// sets how many failed fixes move work up a tier (REQ-FN-022). Every change is written straight
/// to the database, so routing is as it was left after reopening Settings (REQ-FN-023). The role
/// tier is the same <c>Role.Tier</c> column the Agents page edits.
/// </summary>
public partial class SettingsRouting
{
    /// <summary>The kinds of work, with their label and why, as drawn in the mockup. The tier shown before
    /// an owner chooses one is the mockup's own.</summary>
    private static readonly (string Kind, string Label, string Why, int DefaultTier)[] WorkKinds =
    [
        ("write-code", "Write code", "The work the whole build rests on", 1),
        ("review-code", "Review code", "A missed fault costs more than the tokens", 1),
        ("plan", "Plan", "Short, and read by you before it runs", 2),
        ("verify", "Verify", "Reads a result that is already written down", 2),
        ("chat", "Chat", "Answers you wait for, so speed counts", 2),
        ("summarise", "Summarise", "Cheap, frequent, and never leaves the machine", 3)
    ];

    private IReadOnlyList<RoutingTier>? objTiers;
    private IReadOnlyList<ModelSummary> objModels = [];
    private IReadOnlyList<AgentRow>? objAgents;
    private IReadOnlyList<WorkRow> objWork = [];
    private int objThreshold = 3;

    /// <summary>A role's row in "A tier for each agent".</summary>
    /// <param name="RoleId">The role.</param>
    /// <param name="Key">The role's code without dashes, the prefix of its test hooks.</param>
    /// <param name="Name">The role's name.</param>
    /// <param name="Tier">The tier it uses now.</param>
    /// <param name="FirstModel">The first model in that tier's chain, or a dash when the chain is empty.</param>
    private sealed record AgentRow(int RoleId, string Key, string Name, int Tier, string FirstModel);

    /// <summary>A kind of work's row in "A tier for each kind of work".</summary>
    /// <param name="Kind">The kind of work, e.g. <c>write-code</c>.</param>
    /// <param name="Label">What it is called on screen.</param>
    /// <param name="Why">Why it sits where it does.</param>
    /// <param name="Tier">The tier it uses now.</param>
    private sealed record WorkRow(string Kind, string Label, string Why, int Tier);

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await RoutingActions.EnsureShippedTiersAsync();
        await LoadAsync();
    }

    /// <summary>Puts the three tiers back the way Chatur ships them. The mockup draws no confirmation, so
    /// this acts at once and says so in a toast (REQ-FN-020).</summary>
    private async Task ResetTiersAsync()
    {
        await RoutingActions.ResetTiersAsync();
        await LoadAsync();
        Toast.Success("The three tiers are back the way Chatur ships them.");
    }

    private async Task LoadAsync()
    {
        objModels = await ProviderActions.ListModelsAsync();
        objTiers = await RoutingActions.TiersAsync();
        var vRoles = await RoleActions.RolesAsync();
        var vWorkTiers = await RoutingActions.WorkTiersAsync();
        objThreshold = await RoutingActions.EscalationThresholdAsync();

        objAgents = vRoles
            .Select(aRole => new AgentRow(aRole.RoleId, aRole.Code.Replace("-", string.Empty), aRole.Name, aRole.Tier, FirstModelOf(aRole.Tier)))
            .ToList();
        objWork = WorkKinds
            .Select(aKind => new WorkRow(aKind.Kind, aKind.Label, aKind.Why, vWorkTiers.TryGetValue(aKind.Kind, out var vTier) ? vTier : aKind.DefaultTier))
            .ToList();
    }

    private string FirstModelOf(int aTier)
    {
        var vChain = objTiers?.FirstOrDefault(aTier2 => aTier2.Tier == aTier)?.ModelIdsInOrder;
        return vChain is { Count: > 0 } ? ModelOf(vChain[0])?.Identifier ?? "—" : "—";
    }

    private ModelSummary? ModelOf(int aModelId) => objModels.FirstOrDefault(aModel => aModel.ModelId == aModelId);

    private List<ModelSummary> AvailableModels(RoutingTier aTier) =>
        objModels.Where(aModel => !aTier.ModelIdsInOrder.Contains(aModel.ModelId)).ToList();

    private IReadOnlyList<int> ChainOf(int aTier) =>
        objTiers?.FirstOrDefault(aChain => aChain.Tier == aTier)?.ModelIdsInOrder ?? [];

    /// <summary>Appends the chosen model to the end of a tier's chain (REQ-FN-020).</summary>
    private async Task AddAsync(int aTier, string? aModelId)
    {
        if (!int.TryParse(aModelId, out var vModelId))
        {
            return;
        }

        await WriteChainAsync(aTier, [.. ChainOf(aTier), vModelId]);
    }

    /// <summary>Takes the model at a zero-based position out of a tier's chain (REQ-FN-020).</summary>
    private async Task RemoveAsync(int aTier, int aIndex)
    {
        var vChain = ChainOf(aTier).ToList();
        vChain.RemoveAt(aIndex);
        await WriteChainAsync(aTier, vChain);
    }

    /// <summary>Swaps the model at a zero-based position with its neighbour, one step up (-1) or down (1) (REQ-FN-020).</summary>
    private async Task MoveAsync(int aTier, int aIndex, int aStep)
    {
        var vChain = ChainOf(aTier).ToList();
        var vTarget = aIndex + aStep;
        if (vTarget < 0 || vTarget >= vChain.Count)
        {
            return;
        }

        (vChain[aIndex], vChain[vTarget]) = (vChain[vTarget], vChain[aIndex]);
        await WriteChainAsync(aTier, vChain);
    }

    private async Task WriteChainAsync(int aTier, IReadOnlyList<int> aModelIds)
    {
        try
        {
            await RoutingActions.ReorderChainAsync(aTier, aModelIds);
            await LoadAsync();
        }
        catch (ArgumentException aEx)
        {
            Toast.Error(aEx.Message);
        }
    }

    /// <summary>Sets the tier a role's own work uses (REQ-FN-018).</summary>
    private async Task SetAgentTierAsync(AgentRow aAgent, string? aTier)
    {
        if (!int.TryParse(aTier, out var vTier))
        {
            return;
        }

        await RoutingActions.SetRoleTierAsync(aAgent.RoleId, vTier);
        await LoadAsync();
    }

    /// <summary>Sets the tier a kind of work uses (REQ-FN-019).</summary>
    private async Task SetWorkTierAsync(WorkRow aWork, string? aTier)
    {
        if (!int.TryParse(aTier, out var vTier))
        {
            return;
        }

        await RoutingActions.SetWorkTierAsync(aWork.Kind, vTier);
        await LoadAsync();
    }

    /// <summary>Sets how many failed fixes move work up a tier; ignores anything below 1 (REQ-FN-022).</summary>
    private async Task SetThresholdAsync(string? aValue)
    {
        if (!int.TryParse(aValue, out var vThreshold) || vThreshold < 1)
        {
            return;
        }

        await RoutingActions.SetEscalationThresholdAsync(vThreshold);
        objThreshold = vThreshold;
    }

    private static string CountText(int aCount) => aCount == 1 ? "1 model" : $"{aCount} models";

    private static string TierLabel(int aTier) =>
        aTier switch
        {
            1 => "Tier 1 — the strongest",
            2 => "Tier 2 — everyday",
            _ => "Tier 3 — on this machine"
        };
}
