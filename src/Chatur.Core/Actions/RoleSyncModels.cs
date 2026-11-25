namespace Chatur.Core.Actions;

/// <summary>One machine on the sync panel (REQ-FN-078).</summary>
/// <param name="DeviceId">The machine's device identifier.</param>
/// <param name="Name">The machine's name.</param>
/// <param name="IsThisMachine">Whether this is the machine Chatur is running on.</param>
/// <param name="Agents">How many agents it holds.</param>
/// <param name="Rules">How many rules it holds.</param>
/// <param name="LastSeenUtc">When it last synced; <see langword="null"/> if it never has.</param>
/// <param name="UpToDate">Whether its roles and rules match this machine's.</param>
public sealed record RoleSyncMachine(string DeviceId, string Name, bool IsThisMachine, int Agents, int Rules, DateTime? LastSeenUtc, bool UpToDate);

/// <summary>What the sync panel shows (REQ-FN-078).</summary>
/// <param name="Available">Whether there is anywhere to sync through.</param>
/// <param name="UnavailableReason">Why not, in plain words.</param>
/// <param name="LastSyncedUtc">When this machine last synced; <see langword="null"/> if it never has.</param>
/// <param name="Machines">This machine first, then the others.</param>
/// <param name="Outcome">One line saying what the last sync did; <see langword="null"/> before one is run.</param>
public sealed record RoleSyncStatus(
    bool Available,
    string? UnavailableReason,
    DateTime? LastSyncedUtc,
    IReadOnlyList<RoleSyncMachine> Machines,
    string? Outcome);
