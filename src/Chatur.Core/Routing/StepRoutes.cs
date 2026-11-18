using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Processes;
using Dapper;

namespace Chatur.Core.Routing;

/// <summary>The model and tier one process's handed steps run on.</summary>
/// <param name="ModelId">The model's id, or <see langword="null"/> when no connected model is filed under the tier.</param>
/// <param name="Model">The model's identifier, or <see langword="null"/> with it.</param>
/// <param name="Tier">The tier the steps run on.</param>
public sealed record StepModelRoute(int? ModelId, string? Model, int Tier);

/// <summary>
/// The one rule that names the model a process's step runs on, used both by the estimate before a run starts and by the
/// step hand-over that runs it, so the two can never name different models (REQ-FN-019, REQ-FN-020, REQ-FN-111;
/// MISS-Chatur-20261110-06). The tier is the work tier Settings ▸ Routing set for the process's kind of work, else the
/// owning agent's own tier (2 when the role is not found, as the agent loop does). The model is the first connected
/// model of that tier's chain, in the order Routing stored it; with no chain, the first connected model filed under
/// that tier.
/// </summary>
public sealed class StepRoutes
{
    /// <summary>The kind of work each estimated process does, as Settings ▸ Routing names it.</summary>
    private static readonly IReadOnlyDictionary<string, string> WorkKinds = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["build-phase"] = "write-code",
        ["verify-phase"] = "verify",
        ["fix-issues"] = "write-code",
        ["triage-issues"] = "review-code"
    };

    private readonly IDbConnectionFactory objConnections;
    private readonly IRoutingActions objRouting;

    /// <summary>Creates the rule over Chatur's database and its routing settings.</summary>
    /// <param name="aConnections">Opens connections to Chatur's database.</param>
    /// <param name="aRouting">The tiers, their chains and the work tiers set on Settings ▸ Routing.</param>
    public StepRoutes(IDbConnectionFactory aConnections, IRoutingActions aRouting)
    {
        objConnections = aConnections;
        objRouting = aRouting;
    }

    /// <summary>The kind of work a process does, or <see langword="null"/> for a process with none.</summary>
    /// <param name="aProcessCode">The process, e.g. <c>build-phase</c>.</param>
    public static string? WorkKindOf(string aProcessCode) => WorkKinds.GetValueOrDefault(aProcessCode);

    /// <summary>The model and tier the given process's steps run on now.</summary>
    /// <param name="aProcessCode">The process, e.g. <c>build-phase</c>.</param>
    /// <param name="aCt">Cancels the read.</param>
    public async Task<StepModelRoute> ForProcessAsync(string aProcessCode, CancellationToken aCt = default)
    {
        var vWorkTiers = await objRouting.WorkTiersAsync(aCt).ConfigureAwait(false);
        var vChains = await objRouting.TiersAsync(aCt).ConfigureAwait(false);
        var vAgent = ProcessCatalogue.Find(aProcessCode)?.Agent ?? string.Empty;

        using var vConnection = objConnections.OpenConnection();
        var vTier = WorkKindOf(aProcessCode) is { } vWorkKind && vWorkTiers.TryGetValue(vWorkKind, out var vSet)
            ? vSet
            : await vConnection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(
                "SELECT Tier FROM Role WHERE lower(Name) = lower(@vAgent) ORDER BY Version DESC LIMIT 1;",
                new { vAgent },
                cancellationToken: aCt)).ConfigureAwait(false) ?? 2;

        foreach (var vModelId in vChains.FirstOrDefault(aChain => aChain.Tier == vTier)?.ModelIdsInOrder ?? [])
        {
            var vInChain = await vConnection.QuerySingleOrDefaultAsync<ModelRow>(new CommandDefinition(
                "SELECT m.ModelId, m.Identifier FROM Model m JOIN Provider p ON p.ProviderId = m.ProviderId WHERE m.ModelId = @vModelId AND p.State = 'Connected';",
                new { vModelId },
                cancellationToken: aCt)).ConfigureAwait(false);
            if (vInChain is not null)
            {
                return new StepModelRoute((int)vInChain.ModelId, vInChain.Identifier, vTier);
            }
        }

        var vFiled = await vConnection.QueryFirstOrDefaultAsync<ModelRow>(new CommandDefinition(
            "SELECT m.ModelId, m.Identifier FROM Model m JOIN Provider p ON p.ProviderId = m.ProviderId WHERE m.Tier = @vTier AND p.State = 'Connected' ORDER BY m.ModelId LIMIT 1;",
            new { vTier },
            cancellationToken: aCt)).ConfigureAwait(false);
        return new StepModelRoute(vFiled is null ? null : (int)vFiled.ModelId, vFiled?.Identifier, vTier);
    }

    private sealed class ModelRow
    {
        public long ModelId { get; set; }

        public string Identifier { get; set; } = string.Empty;
    }
}
