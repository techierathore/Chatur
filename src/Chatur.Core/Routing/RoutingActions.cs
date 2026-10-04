using System.Text.Json;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Dapper;

namespace Chatur.Core.Routing;

/// <summary>
/// <see cref="IRoutingActions"/> over the <c>Role.Tier</c> column, the <c>RoutingEntry</c> and
/// <c>RoutingSetting</c> tables, and the <c>RoutingEscalation</c> audit table (Architecture §7
/// "Models"). Every write here is a plain database row, so REQ-FN-023 ("routing is remembered")
/// needs no extra code — it falls out of using the database at all.
/// </summary>
public sealed class RoutingActions : IRoutingActions
{
    /// <summary>The kinds of work Settings ▸ Routing shows, matching <c>docs/mockups/settings-routing.html</c>.</summary>
    private static readonly IReadOnlySet<string> KnownWorkKinds = new HashSet<string>
    {
        "chat", "plan", "write-code", "review-code", "summarise", "verify"
    };

    /// <summary>The setting that records the tiers have been filled once, so an emptied chain is not refilled.</summary>
    private const string TiersInitialisedKey = "TiersInitialised";

    private readonly IDbConnectionFactory objDb;

    /// <summary>Creates the action implementation.</summary>
    /// <param name="aDb">Opens connections to Chatur's own database.</param>
    public RoutingActions(IDbConnectionFactory aDb)
    {
        objDb = aDb;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoutingTier>> TiersAsync(CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        var vTiers = new List<RoutingTier>(3);
        for (var vTier = 1; vTier <= 3; vTier++)
        {
            vTiers.Add(new RoutingTier(vTier, await ReadChainAsync(vConnection, vTier, aCt).ConfigureAwait(false)));
        }

        return vTiers;
    }

    /// <inheritdoc />
    public async Task SetRoleTierAsync(int aRoleId, int aTier, CancellationToken aCt = default)
    {
        RequireValidTier(aTier);

        using var vConnection = objDb.OpenConnection();
        var vCommand = new CommandDefinition(
            "UPDATE Role SET Tier = @aTier WHERE RoleId = @aRoleId;",
            new { aTier, aRoleId },
            cancellationToken: aCt);
        var vAffected = await vConnection.ExecuteAsync(vCommand).ConfigureAwait(false);
        if (vAffected == 0)
        {
            throw new KeyNotFoundException($"Role {aRoleId} does not exist.");
        }
    }

    /// <inheritdoc />
    public Task SetWorkTierAsync(string aWorkKind, int aTier, CancellationToken aCt = default)
    {
        RequireValidTier(aTier);
        RequireKnownWorkKind(aWorkKind);
        return UpsertWorkTierAsync(aWorkKind, aTier, aCt);
    }

    /// <inheritdoc />
    public async Task ReorderChainAsync(int aTier, IReadOnlyList<int> aModelIdsInOrder, CancellationToken aCt = default)
    {
        RequireValidTier(aTier);

        using var vConnection = objDb.OpenConnection();
        if (aModelIdsInOrder.Count > 0)
        {
            var vExistsCommand = new CommandDefinition(
                "SELECT COUNT(*) FROM Model WHERE ModelId IN @aModelIdsInOrder;",
                new { aModelIdsInOrder },
                cancellationToken: aCt);
            var vFound = await vConnection.ExecuteScalarAsync<long>(vExistsCommand).ConfigureAwait(false);
            if (vFound != aModelIdsInOrder.Count)
            {
                throw new ArgumentException("Every model in the chain must already be a known model.", nameof(aModelIdsInOrder));
            }
        }

        var vJson = JsonSerializer.Serialize(aModelIdsInOrder);
        var vUpdate = new CommandDefinition(
            "UPDATE RoutingSetting SET Value = @vJson WHERE Key = @vKey;",
            new { vJson, vKey = TierChainKey(aTier) },
            cancellationToken: aCt);
        await vConnection.ExecuteAsync(vUpdate).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RecordEscalationAsync(string aWorkKind, int aFromTier, int aToTier, string aReason, CancellationToken aCt = default)
    {
        RequireValidTier(aFromTier);
        RequireValidTier(aToTier);
        RequireKnownWorkKind(aWorkKind);
        if (string.IsNullOrWhiteSpace(aReason))
        {
            throw new ArgumentException("An escalation must say why it happened.", nameof(aReason));
        }

        using var vConnection = objDb.OpenConnection();
        var vInsert = new CommandDefinition(
            """
            INSERT INTO RoutingEscalation (WorkKind, FromTier, ToTier, Reason, CreatedUtc)
            VALUES (@aWorkKind, @aFromTier, @aToTier, @aReason, @vNowUtc);
            """,
            new { aWorkKind, aFromTier, aToTier, aReason, vNowUtc = DateTime.UtcNow.ToString("O") },
            cancellationToken: aCt);
        await vConnection.ExecuteAsync(vInsert).ConfigureAwait(false);

        await UpsertWorkTierAsync(vConnection, aWorkKind, aToTier, aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, int>> WorkTiersAsync(CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        var vCommand = new CommandDefinition(
            "SELECT WorkKind, Tier FROM RoutingEntry WHERE WorkKind IS NOT NULL;",
            cancellationToken: aCt);
        var vRows = await vConnection.QueryAsync<WorkTierRow>(vCommand).ConfigureAwait(false);
        return vRows.ToDictionary(aRow => aRow.WorkKind, aRow => (int)aRow.Tier);
    }

    /// <inheritdoc />
    public async Task<int> EscalationThresholdAsync(CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        var vCommand = new CommandDefinition(
            "SELECT Value FROM RoutingSetting WHERE Key = 'EscalationThreshold';",
            cancellationToken: aCt);
        var vValue = await vConnection.QuerySingleOrDefaultAsync<string?>(vCommand).ConfigureAwait(false);
        return vValue is null ? 3 : int.Parse(vValue);
    }

    /// <inheritdoc />
    public async Task SetEscalationThresholdAsync(int aThreshold, CancellationToken aCt = default)
    {
        if (aThreshold < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(aThreshold), "The escalation threshold must be at least 1.");
        }

        using var vConnection = objDb.OpenConnection();
        var vCommand = new CommandDefinition(
            "UPDATE RoutingSetting SET Value = @vValue WHERE Key = 'EscalationThreshold';",
            new { vValue = aThreshold.ToString() },
            cancellationToken: aCt);
        await vConnection.ExecuteAsync(vCommand).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ResetTiersAsync(CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        await WriteShippedChainsAsync(vConnection, aCt).ConfigureAwait(false);
        await MarkTiersInitialisedAsync(vConnection, aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task EnsureShippedTiersAsync(CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        var vMarked = await vConnection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT COUNT(*) FROM RoutingSetting WHERE Key = @TiersInitialisedKey;",
            new { TiersInitialisedKey },
            cancellationToken: aCt)).ConfigureAwait(false);
        if (vMarked > 0)
        {
            return;
        }

        var vModels = await vConnection.ExecuteScalarAsync<long>(new CommandDefinition("SELECT COUNT(*) FROM Model;", cancellationToken: aCt)).ConfigureAwait(false);
        if (vModels == 0)
        {
            return;
        }

        await WriteShippedChainsAsync(vConnection, aCt).ConfigureAwait(false);
        await MarkTiersInitialisedAsync(vConnection, aCt).ConfigureAwait(false);
    }

    private static async Task WriteShippedChainsAsync(System.Data.IDbConnection aConnection, CancellationToken aCt)
    {
        var vRows = await aConnection.QueryAsync<ModelTierRow>(new CommandDefinition(
            "SELECT ModelId, Tier FROM Model ORDER BY ModelId;",
            cancellationToken: aCt)).ConfigureAwait(false);
        var vModels = vRows.ToList();
        for (var vTier = 1; vTier <= 3; vTier++)
        {
            var vJson = JsonSerializer.Serialize(vModels.Where(aRow => aRow.Tier == vTier).Select(aRow => (int)aRow.ModelId).ToArray());
            await aConnection.ExecuteAsync(new CommandDefinition(
                "UPDATE RoutingSetting SET Value = @vJson WHERE Key = @vKey;",
                new { vJson, vKey = TierChainKey(vTier) },
                cancellationToken: aCt)).ConfigureAwait(false);
        }
    }

    private static Task<int> MarkTiersInitialisedAsync(System.Data.IDbConnection aConnection, CancellationToken aCt) =>
        aConnection.ExecuteAsync(new CommandDefinition(
            "INSERT OR IGNORE INTO RoutingSetting (Key, Value) VALUES (@TiersInitialisedKey, '1');",
            new { TiersInitialisedKey },
            cancellationToken: aCt));

    private async Task UpsertWorkTierAsync(string aWorkKind, int aTier, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        await UpsertWorkTierAsync(vConnection, aWorkKind, aTier, aCt).ConfigureAwait(false);
    }

    private static async Task UpsertWorkTierAsync(System.Data.IDbConnection aConnection, string aWorkKind, int aTier, CancellationToken aCt)
    {
        var vUpdate = new CommandDefinition(
            "UPDATE RoutingEntry SET Tier = @aTier WHERE WorkKind = @aWorkKind AND RoleId IS NULL;",
            new { aTier, aWorkKind },
            cancellationToken: aCt);
        var vAffected = await aConnection.ExecuteAsync(vUpdate).ConfigureAwait(false);
        if (vAffected > 0)
        {
            return;
        }

        var vInsert = new CommandDefinition(
            "INSERT INTO RoutingEntry (RoleId, WorkKind, Tier, SortOrder) VALUES (NULL, @aWorkKind, @aTier, 0);",
            new { aWorkKind, aTier },
            cancellationToken: aCt);
        await aConnection.ExecuteAsync(vInsert).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<int>> ReadChainAsync(System.Data.IDbConnection aConnection, int aTier, CancellationToken aCt)
    {
        var vCommand = new CommandDefinition(
            "SELECT Value FROM RoutingSetting WHERE Key = @vKey;",
            new { vKey = TierChainKey(aTier) },
            cancellationToken: aCt);
        var vJson = await aConnection.QuerySingleOrDefaultAsync<string?>(vCommand).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(vJson))
        {
            return Array.Empty<int>();
        }

        return JsonSerializer.Deserialize<int[]>(vJson) ?? Array.Empty<int>();
    }

    private static string TierChainKey(int aTier) => $"Tier{aTier}Chain";

    private static void RequireValidTier(int aTier)
    {
        if (aTier is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(aTier), "A tier must be 1, 2 or 3.");
        }
    }

    private static void RequireKnownWorkKind(string aWorkKind)
    {
        if (!KnownWorkKinds.Contains(aWorkKind))
        {
            throw new ArgumentException($"\"{aWorkKind}\" is not a kind of work Chatur knows.", nameof(aWorkKind));
        }
    }

    /// <summary>
    /// One row of <c>RoutingEntry</c> for a kind of work, read by Dapper. <c>Tier</c> stays
    /// <see cref="long"/> — SQLite's native INTEGER width — until narrowed on purpose: Dapper's
    /// object materialization demands an exact type match against what Microsoft.Data.Sqlite hands
    /// back, and an <see cref="int"/> property here throws at read time, not at compile time.
    /// </summary>
    private sealed class WorkTierRow
    {
        public string WorkKind { get; set; } = string.Empty;

        public long Tier { get; set; }
    }

    private sealed class ModelTierRow
    {
        public long ModelId { get; set; }

        public long Tier { get; set; }
    }
}
