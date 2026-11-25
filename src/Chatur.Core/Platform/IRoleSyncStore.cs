namespace Chatur.Core.Platform;

/// <summary>
/// What one machine has put into the shared store about its roles and rules (REQ-FN-078, BRD-148).
/// </summary>
/// <param name="DeviceId">The machine's device identifier (the one App Manager knows it by).</param>
/// <param name="DeviceName">A name to show for the machine.</param>
/// <param name="SyncedUtc">When this machine last synced.</param>
/// <param name="ChangedUtc">When this machine's roles and rules last changed.</param>
/// <param name="ContentHash">A fingerprint of the roles and rules, ignoring version numbers and dates.</param>
/// <param name="Agents">How many agents (roles) the machine holds.</param>
/// <param name="Rules">How many rules the machine holds.</param>
/// <param name="Json">Every role, right, command and rule, in the roles export format.</param>
public sealed record RoleSyncRecord(
    string DeviceId,
    string DeviceName,
    DateTime SyncedUtc,
    DateTime ChangedUtc,
    string ContentHash,
    int Agents,
    int Rules,
    string Json);

/// <summary>
/// The shared place a developer's machines leave their roles and rules for each other: App Manager on a
/// real machine; a file the web harness names in its own configuration in the browser checks. One
/// record per machine per account.
/// </summary>
public interface IRoleSyncStore
{
    /// <summary>Whether this store can be reached at all. When false, nothing is sent or taken.</summary>
    bool IsAvailable { get; }

    /// <summary>Why the store cannot be reached, in plain words; <see langword="null"/> when it can.</summary>
    string? UnavailableReason { get; }

    /// <summary>Every machine's record for an account.</summary>
    /// <param name="aAccountId">The App Manager user id of the signed-in account.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<RoleSyncRecord>> ListAsync(string aAccountId, CancellationToken aCt = default);

    /// <summary>Puts a machine's record in the store, replacing that machine's earlier one.</summary>
    /// <param name="aAccountId">The App Manager user id of the signed-in account.</param>
    /// <param name="aRecord">The record to keep.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    Task SaveAsync(string aAccountId, RoleSyncRecord aRecord, CancellationToken aCt = default);
}
