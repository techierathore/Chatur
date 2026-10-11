using Chatur.Core.Actions;

namespace Chatur.Core.ProcessRuns;

/// <summary>A verdict worked out for one requirement, not yet written.</summary>
/// <param name="RequirementId">The requirement.</param>
/// <param name="Status">The status to write: <c>Verified</c> or <c>Needs re-verify</c>.</param>
/// <param name="Percent">The percentage to write.</param>
/// <param name="Remark">The dated remark to append.</param>
/// <param name="Evidence">The tests behind it.</param>
public sealed record PlannedVerdict(
    string RequirementId,
    string Status,
    int Percent,
    string Remark,
    ProcessRunVerdictEvidence Evidence);
