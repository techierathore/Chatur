namespace Chatur.Core.Guards;

/// <summary>
/// One rule a tool request must pass before it reaches a tool (Architecture §5 "Guards";
/// REQ-FN-036). Every guard has its own unit tests covering what it allows and what it refuses
/// (REQ-NFR-005).
/// </summary>
public interface IToolGuard
{
    /// <summary>
    /// Decides whether this guard's rule allows the request.
    /// </summary>
    /// <param name="aRequest">The tool request to evaluate.</param>
    /// <param name="aCt">A token that cancels the evaluation.</param>
    Task<GuardResult> EvaluateAsync(ToolRequest aRequest, CancellationToken aCt = default);
}
