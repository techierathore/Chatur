using System.Text.Json;
using System.Text.Json.Nodes;
using Chatur.Core.Roles;
using Chatur.Core.Platform;

namespace Chatur.WebHarness.Services;

/// <summary>
/// Stands in for App Manager in the browser checks (REQ-FN-078): one JSON file, named by
/// <c>RoleSync:StoreFile</c> in the harness's own configuration, relative to the harness data folder. A
/// second "machine" under the same account is a second record with another device id, written through the
/// test-only routes in <c>Program.cs</c>. Test only; the MAUI head has no such store.
/// </summary>
public sealed class HarnessRoleSyncStore : IRoleSyncStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly string objFile;
    private readonly object objLock = new();

    /// <summary>Creates the store over a file.</summary>
    /// <param name="aFile">The file's absolute path.</param>
    public HarnessRoleSyncStore(string aFile)
    {
        objFile = aFile;
    }

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <inheritdoc />
    public string? UnavailableReason => null;

    /// <inheritdoc />
    public Task<IReadOnlyList<RoleSyncRecord>> ListAsync(string aAccountId, CancellationToken aCt = default)
    {
        lock (objLock)
        {
            return Task.FromResult<IReadOnlyList<RoleSyncRecord>>(Read().TryGetValue(aAccountId, out var vRecords) ? vRecords : []);
        }
    }

    /// <inheritdoc />
    public Task SaveAsync(string aAccountId, RoleSyncRecord aRecord, CancellationToken aCt = default)
    {
        lock (objLock)
        {
            var vAll = Read();
            var vRecords = vAll.TryGetValue(aAccountId, out var vExisting) ? vExisting.ToList() : [];
            vRecords.RemoveAll(aOld => aOld.DeviceId == aRecord.DeviceId);
            vRecords.Add(aRecord);
            vAll[aAccountId] = vRecords;
            File.WriteAllText(objFile, JsonSerializer.Serialize(vAll, Options));
        }

        return Task.CompletedTask;
    }

    /// <summary>Every account's records, for the test-only routes.</summary>
    public IReadOnlyDictionary<string, List<RoleSyncRecord>> Snapshot()
    {
        lock (objLock)
        {
            return Read();
        }
    }

    /// <summary>Forgets everything the store holds, when a check ends.</summary>
    public void Clear()
    {
        lock (objLock)
        {
            File.Delete(objFile);
        }
    }

    /// <summary>The device id of the second machine the test-only routes act as.</summary>
    public const string PeerDeviceId = "harness-peer-machine";

    /// <summary>
    /// Acts as the second machine of the account that has synced here: it starts from what that account's
    /// other machine sent (or its own last record), adds or removes one global rule, and syncs.
    /// </summary>
    /// <param name="aPeerName">The second machine's name.</param>
    /// <param name="aAddRule">A global rule to add, or <see langword="null"/>.</param>
    /// <param name="aRemoveRule">A global rule to remove, or <see langword="null"/>.</param>
    /// <returns><see langword="false"/> when nothing has synced here yet, so there is no account to act in.</returns>
    public bool PeerChange(string aPeerName, string? aAddRule, string? aRemoveRule)
    {
        lock (objLock)
        {
            var vAll = Read();
            var vNewest = vAll
                .SelectMany(aPair => aPair.Value.Select(aRecord => (Account: aPair.Key, Record: aRecord)))
                .OrderByDescending(aPair => aPair.Record.SyncedUtc)
                .Cast<(string Account, RoleSyncRecord Record)?>()
                .FirstOrDefault();
            if (vNewest is null)
            {
                return false;
            }

            var vAccount = vNewest.Value.Account;
            var vRecords = vAll[vAccount];
            var vBase = vRecords.FirstOrDefault(aRecord => aRecord.DeviceId == PeerDeviceId) ?? vNewest.Value.Record;
            var vRoot = (JsonObject)JsonNode.Parse(vBase.Json)!;
            var vRulesKey = vRoot.First(aProperty => string.Equals(aProperty.Key, "rules", StringComparison.OrdinalIgnoreCase)).Key;
            var vRules = (JsonArray)vRoot[vRulesKey]!;
            static string Text(JsonObject aRule) => aRule.First(aProperty => string.Equals(aProperty.Key, "text", StringComparison.OrdinalIgnoreCase)).Value!.GetValue<string>();
            if (aRemoveRule is not null)
            {
                foreach (var vRule in vRules.OfType<JsonObject>().Where(aRule => Text(aRule) == aRemoveRule).ToList())
                {
                    vRules.Remove(vRule);
                }
            }

            if (aAddRule is not null)
            {
                var vPascal = vRules.OfType<JsonObject>().FirstOrDefault()?.First().Key is { Length: > 0 } vFirst && char.IsUpper(vFirst[0]);
                vRules.Add(vPascal
                    ? new JsonObject { ["Scope"] = "global", ["Text"] = aAddRule, ["Version"] = 1 }
                    : new JsonObject { ["scope"] = "global", ["text"] = aAddRule, ["version"] = 1 });
            }

            var vJson = vRoot.ToJsonString(Options);
            var vRoles = vRoot.First(aProperty => string.Equals(aProperty.Key, "roles", StringComparison.OrdinalIgnoreCase)).Value as JsonArray;
            var vNow = DateTime.UtcNow;
            vRecords.RemoveAll(aOld => aOld.DeviceId == PeerDeviceId);
            vRecords.Add(new RoleSyncRecord(PeerDeviceId, aPeerName, vNow, vNow, RoleSyncActions.HashOf(vJson), vRoles?.Count ?? 0, vRules.Count, vJson));
            File.WriteAllText(objFile, JsonSerializer.Serialize(vAll, Options));
            return true;
        }
    }

    private Dictionary<string, List<RoleSyncRecord>> Read() =>
        File.Exists(objFile)
            ? JsonSerializer.Deserialize<Dictionary<string, List<RoleSyncRecord>>>(File.ReadAllText(objFile), Options) ?? []
            : [];
}
