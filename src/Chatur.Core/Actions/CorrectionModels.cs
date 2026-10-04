namespace Chatur.Core.Actions;

/// <summary>A change Chatur made to a role, rule or step's wording about itself (REQ-UI-034).</summary>
/// <param name="CorrectionId">The row's identity.</param>
/// <param name="Target">What was changed, e.g. a role's code or a rule's identity.</param>
/// <param name="Before">The wording before the correction.</param>
/// <param name="After">The wording after the correction.</param>
/// <param name="Why">Why Chatur made the correction.</param>
/// <param name="Status">The correction's status: <c>"Proposed"</c>, <c>"Kept"</c> or <c>"Undone"</c>.</param>
/// <param name="CreatedUtc">When Chatur made the correction (REQ-UI-034).</param>
public sealed record Correction(int CorrectionId, string Target, string Before, string After, string Why, string Status, DateTime CreatedUtc);
