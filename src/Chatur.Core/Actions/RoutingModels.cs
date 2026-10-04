namespace Chatur.Core.Actions;

/// <summary>One tier's fallback chain of models, in the order they are tried (REQ-FN-020).</summary>
/// <param name="Tier">The tier number, 1 to 3.</param>
/// <param name="ModelIdsInOrder">The models in this tier, in the order Chatur tries them.</param>
public sealed record RoutingTier(int Tier, IReadOnlyList<int> ModelIdsInOrder);
