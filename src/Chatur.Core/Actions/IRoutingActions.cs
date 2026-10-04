namespace Chatur.Core.Actions;

/// <summary>
/// The tiers, their fallback chains, and which tier a role or a kind of work uses (Architecture §7
/// "Models"; page Settings ▸ Routing).
/// </summary>
public interface IRoutingActions
{
    /// <summary>Every tier and its fallback chain (REQ-FN-020, REQ-FN-023).</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<RoutingTier>> TiersAsync(CancellationToken aCt = default);

    /// <summary>
    /// Sets the tier a role's own work uses (REQ-FN-018).
    /// </summary>
    /// <param name="aRoleId">The role.</param>
    /// <param name="aTier">The tier, 1 to 3.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    Task SetRoleTierAsync(int aRoleId, int aTier, CancellationToken aCt = default);

    /// <summary>
    /// Sets the tier a kind of work uses, overriding the role's own tier for that work (REQ-FN-019).
    /// </summary>
    /// <param name="aWorkKind">The kind of work, e.g. <c>"fix-build"</c>.</param>
    /// <param name="aTier">The tier, 1 to 3.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    Task SetWorkTierAsync(string aWorkKind, int aTier, CancellationToken aCt = default);

    /// <summary>
    /// Sets the order a tier's models are tried in (REQ-FN-020).
    /// </summary>
    /// <param name="aTier">The tier to reorder.</param>
    /// <param name="aModelIdsInOrder">The models, in the new order.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    Task ReorderChainAsync(int aTier, IReadOnlyList<int> aModelIdsInOrder, CancellationToken aCt = default);

    /// <summary>
    /// Records that a kind of work climbed to a stronger tier after repeated failures (REQ-FN-022).
    /// Also applies the move — <paramref name="aWorkKind"/> uses <paramref name="aToTier"/> from
    /// here on, exactly as <see cref="SetWorkTierAsync"/> would.
    /// </summary>
    /// <param name="aWorkKind">The kind of work that climbed.</param>
    /// <param name="aFromTier">The tier it was on.</param>
    /// <param name="aToTier">The tier it climbed to.</param>
    /// <param name="aReason">Why it climbed, for the routing history.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    Task RecordEscalationAsync(string aWorkKind, int aFromTier, int aToTier, string aReason, CancellationToken aCt = default);

    /// <summary>The tier each kind of work is currently set to, for the routing tab's work-tiers table (REQ-FN-019).</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyDictionary<string, int>> WorkTiersAsync(CancellationToken aCt = default);

    /// <summary>How many times the same fix must fail before its work climbs a tier (REQ-FN-022).</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<int> EscalationThresholdAsync(CancellationToken aCt = default);

    /// <summary>
    /// Sets how many times the same fix must fail before its work climbs a tier (REQ-FN-022).
    /// </summary>
    /// <param name="aThreshold">The new threshold; at least 1.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    Task SetEscalationThresholdAsync(int aThreshold, CancellationToken aCt = default);

    /// <summary>
    /// Puts the three tiers' chains back the way Chatur ships them: each connected model in the tier it
    /// was filed under when its provider was added, in the order it was added (REQ-FN-020).
    /// </summary>
    /// <param name="aCt">A token that cancels the write.</param>
    Task ResetTiersAsync(CancellationToken aCt = default);

    /// <summary>
    /// Fills the tiers the shipped way the first time models exist, and never again, so a chain the owner
    /// later empties stays empty. Does nothing while there are no models (REQ-FN-020, REQ-FN-023).
    /// </summary>
    /// <param name="aCt">A token that cancels the write.</param>
    Task EnsureShippedTiersAsync(CancellationToken aCt = default);
}
