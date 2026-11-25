namespace Chatur.Core.Platform;

/// <summary>
/// The store a real machine has until App Manager offers somewhere to keep a developer's roles and rules
/// (docs/Chatur-AppManager-Feedback.md): it says so, and sends and takes nothing.
/// </summary>
public sealed class UnavailableRoleSyncStore : IRoleSyncStore
{
    /// <inheritdoc />
    public bool IsAvailable => false;

    /// <inheritdoc />
    public string? UnavailableReason => "App Manager does not yet keep a developer's roles and rules, so there is nothing to sync through.";

    /// <inheritdoc />
    public Task<IReadOnlyList<RoleSyncRecord>> ListAsync(string aAccountId, CancellationToken aCt = default) =>
        Task.FromResult<IReadOnlyList<RoleSyncRecord>>([]);

    /// <inheritdoc />
    public Task SaveAsync(string aAccountId, RoleSyncRecord aRecord, CancellationToken aCt = default) =>
        throw new InvalidOperationException(UnavailableReason);
}
