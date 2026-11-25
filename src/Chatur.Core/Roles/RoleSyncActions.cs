using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Chatur.Core.Accounts;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;

namespace Chatur.Core.Roles;

/// <summary>
/// <see cref="IRoleSyncActions"/>: one record per machine in an <see cref="IRoleSyncStore"/>, the roles
/// export file as the payload, and a fingerprint (ignoring versions and dates) to tell whether anything
/// really changed. What this machine had at its last sync is kept in its own <c>Setting</c> rows.
/// </summary>
public sealed class RoleSyncActions : IRoleSyncActions
{
    private const string LastHashKey = "RoleSyncLastHash";
    private const string LastSyncedKey = "RoleSyncLastSyncedUtc";
    private const string ChangedKey = "RoleSyncChangedUtc";

    private readonly IRoleActions objRoles;
    private readonly IRoleSyncStore objStore;
    private readonly IDbConnectionFactory objConnections;
    private readonly IClock objClock;
    private readonly AppState objAppState;

    /// <summary>Creates the actions.</summary>
    /// <param name="aRoles">Exports and imports the roles and rules.</param>
    /// <param name="aStore">The shared place the machines leave their roles and rules.</param>
    /// <param name="aConnections">Opens the account's database and the install's.</param>
    /// <param name="aClock">The current time.</param>
    /// <param name="aAppState">Says which account is signed in.</param>
    public RoleSyncActions(IRoleActions aRoles, IRoleSyncStore aStore, IDbConnectionFactory aConnections, IClock aClock, AppState aAppState)
    {
        objRoles = aRoles;
        objStore = aStore;
        objConnections = aConnections;
        objClock = aClock;
        objAppState = aAppState;
    }

    /// <inheritdoc />
    public async Task<RoleSyncStatus> StatusAsync(CancellationToken aCt = default) => await BuildStatusAsync(null, aCt).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<RoleSyncStatus> SyncNowAsync(CancellationToken aCt = default)
    {
        if (!objStore.IsAvailable || AccountId() is not { } vAccount)
        {
            return await BuildStatusAsync(null, aCt).ConfigureAwait(false);
        }

        var vDeviceId = await InstallationId.GetOrCreateAsync(objConnections, objClock, aCt).ConfigureAwait(false);
        var vNow = objClock.UtcNow;
        var vJson = await objRoles.ExportAsync(aCt).ConfigureAwait(false);
        var vHash = HashOf(vJson);
        var vLastHash = ReadSetting(LastHashKey);
        var vChangedHere = vLastHash is null || vLastHash != vHash;

        var vRecords = await objStore.ListAsync(vAccount, aCt).ConfigureAwait(false);
        var vNewestElsewhere = vRecords
            .Where(aRecord => aRecord.DeviceId != vDeviceId)
            .OrderByDescending(aRecord => aRecord.ChangedUtc)
            .FirstOrDefault();

        string vOutcome;
        var vChangedUtc = ReadSetting(ChangedKey) is { } vStored ? Parse(vStored) : vNow;
        var vDiffers = vNewestElsewhere is not null && vNewestElsewhere.ContentHash != vHash;
        if (vChangedHere && vLastHash is null && vDiffers)
        {
            // First sync of this machine: take what the other machine has rather than overwrite it.
            (vJson, vHash, vChangedUtc, vOutcome) = await TakeAsync(vNewestElsewhere!, aCt).ConfigureAwait(false);
        }
        else if (vChangedHere)
        {
            vChangedUtc = vNow;
            var vLastSynced = Parse(ReadSetting(LastSyncedKey) ?? string.Empty);
            vOutcome = vDiffers && vNewestElsewhere!.ContentHash != vLastHash && vNewestElsewhere.ChangedUtc > vLastSynced
                ? "Both machines changed; this machine's roles and rules were kept and sent."
                : "Sent this machine's roles and rules.";
        }
        else if (vNewestElsewhere is not null && vNewestElsewhere.ContentHash != vHash && vNewestElsewhere.ChangedUtc > vChangedUtc)
        {
            (vJson, vHash, vChangedUtc, vOutcome) = await TakeAsync(vNewestElsewhere, aCt).ConfigureAwait(false);
        }
        else
        {
            vOutcome = "Already up to date.";
        }

        var vRecord = new RoleSyncRecord(vDeviceId, Environment.MachineName, vNow, vChangedUtc, vHash, CountOf(vJson, "roles"), CountOf(vJson, "rules"), vJson);
        await objStore.SaveAsync(vAccount, vRecord, aCt).ConfigureAwait(false);
        WriteSetting(LastHashKey, vHash);
        WriteSetting(LastSyncedKey, vNow.ToString("O", CultureInfo.InvariantCulture));
        WriteSetting(ChangedKey, vChangedUtc.ToString("O", CultureInfo.InvariantCulture));
        return await BuildStatusAsync(vOutcome, aCt).ConfigureAwait(false);
    }

    /// <summary>Fingerprint of an export: versions and dates left out, lists put in a fixed order.</summary>
    /// <param name="aExportJson">A roles export file's text.</param>
    public static string HashOf(string aExportJson)
    {
        var vNode = JsonNode.Parse(aExportJson) ?? throw new FormatException("Not a roles export.");
        var vCanonical = Canonical(vNode) ?? throw new FormatException("Not a roles export.");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(vCanonical.ToJsonString())));
    }

    private static JsonNode? Canonical(JsonNode? aNode)
    {
        switch (aNode)
        {
            case JsonObject vObject:
                var vClean = new JsonObject();
                foreach (var vProperty in vObject.OrderBy(aProperty => aProperty.Key, StringComparer.Ordinal))
                {
                    if (vProperty.Key.ToLowerInvariant() is "exportedutc" or "validfromutc" or "version")
                    {
                        continue;
                    }

                    vClean[vProperty.Key] = Canonical(vProperty.Value);
                }

                return vClean;
            case JsonArray vArray:
                var vItems = vArray.Select(aItem => Canonical(aItem)).OrderBy(aItem => aItem?.ToJsonString(), StringComparer.Ordinal).ToList();
                return new JsonArray(vItems.ToArray());
            default:
                return aNode?.DeepClone();
        }
    }

    private static string Text(JsonObject aObject, string aName) =>
        aObject.FirstOrDefault(aProperty => string.Equals(aProperty.Key, aName, StringComparison.OrdinalIgnoreCase)).Value?.GetValue<string>() ?? string.Empty;

    private static int CountOf(string aExportJson, string aArray) =>
        (JsonNode.Parse(aExportJson) as JsonObject)?.FirstOrDefault(aProperty => string.Equals(aProperty.Key, aArray, StringComparison.OrdinalIgnoreCase)).Value is JsonArray vArray ? vArray.Count : 0;

    private static DateTime Parse(string aText) =>
        DateTime.TryParse(aText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var vWhen) ? vWhen : DateTime.MinValue;

    /// <summary>Brings another machine's roles and rules in, then removes the rules it no longer has.</summary>
    private async Task<(string Json, string Hash, DateTime ChangedUtc, string Outcome)> TakeAsync(RoleSyncRecord aRemote, CancellationToken aCt)
    {
        await objRoles.ImportAsync(aRemote.Json, aCt).ConfigureAwait(false);
        var vRemoteRules = (JsonNode.Parse(aRemote.Json) as JsonObject)?
            .FirstOrDefault(aProperty => string.Equals(aProperty.Key, "rules", StringComparison.OrdinalIgnoreCase)).Value as JsonArray ?? [];
        var vKeep = vRemoteRules
            .OfType<JsonObject>()
            .Select(aRule => (Scope: Text(aRule, "scope"), Text: Text(aRule, "text")))
            .ToHashSet();
        using (var vConnection = objConnections.OpenConnection())
        {
            var vLocal = (await vConnection.QueryAsync<RuleRow>(
                new CommandDefinition("SELECT RuleId, Scope, Text FROM Rule;", cancellationToken: aCt)).ConfigureAwait(false)).ToList();
            foreach (var vRule in vLocal.Where(aRule => !vKeep.Contains((aRule.Scope, aRule.Text))))
            {
                await vConnection.ExecuteAsync(
                    new CommandDefinition("DELETE FROM Rule WHERE RuleId = @RuleId;", new { vRule.RuleId }, cancellationToken: aCt)).ConfigureAwait(false);
            }
        }

        var vJson = await objRoles.ExportAsync(aCt).ConfigureAwait(false);
        return (vJson, HashOf(vJson), aRemote.ChangedUtc, $"Took the roles and rules changed on {aRemote.DeviceName}.");
    }

    private async Task<RoleSyncStatus> BuildStatusAsync(string? aOutcome, CancellationToken aCt)
    {
        var vDeviceId = await InstallationId.GetOrCreateAsync(objConnections, objClock, aCt).ConfigureAwait(false);
        var vJson = await objRoles.ExportAsync(aCt).ConfigureAwait(false);
        var vHash = HashOf(vJson);
        var vMachines = new List<RoleSyncMachine>
        {
            new(vDeviceId, Environment.MachineName, true, CountOf(vJson, "roles"), CountOf(vJson, "rules"), null, ReadSetting(LastHashKey) == vHash || !objStore.IsAvailable),
        };
        if (objStore.IsAvailable && AccountId() is { } vAccount)
        {
            var vRecords = await objStore.ListAsync(vAccount, aCt).ConfigureAwait(false);
            vMachines.AddRange(vRecords
                .Where(aRecord => aRecord.DeviceId != vDeviceId)
                .OrderBy(aRecord => aRecord.DeviceName, StringComparer.OrdinalIgnoreCase)
                .Select(aRecord => new RoleSyncMachine(aRecord.DeviceId, aRecord.DeviceName, false, aRecord.Agents, aRecord.Rules, aRecord.SyncedUtc, aRecord.ContentHash == vHash)));
        }

        var vLast = ReadSetting(LastSyncedKey);
        return new RoleSyncStatus(objStore.IsAvailable, objStore.UnavailableReason, vLast is null ? null : Parse(vLast), vMachines, aOutcome);
    }

    private sealed class RuleRow
    {
        public long RuleId { get; set; }

        public string Scope { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;
    }

    private string? AccountId() => objAppState.CurrentUser?.UserId;

    private string? ReadSetting(string aKey)
    {
        using var vConnection = objConnections.OpenConnection();
        return vConnection.ExecuteScalar<string?>("SELECT Value FROM Setting WHERE Key = @Key;", new { Key = aKey });
    }

    private void WriteSetting(string aKey, string aValue)
    {
        using var vConnection = objConnections.OpenConnection();
        vConnection.Execute(
            "INSERT INTO Setting (Key, Value) VALUES (@Key, @Value) ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;",
            new { Key = aKey, Value = aValue });
    }
}
