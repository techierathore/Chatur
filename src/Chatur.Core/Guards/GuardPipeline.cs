namespace Chatur.Core.Guards;

/// <summary>
/// Runs every guard over a tool request in turn; the first refusal wins (REQ-FN-036). The agent loop
/// calls this once per tool request — never a single guard directly — so a new guard is added here
/// and nowhere else.
/// </summary>
public sealed class GuardPipeline
{
    private readonly IReadOnlyList<IToolGuard> objGuards;

    /// <summary>
    /// Creates the pipeline over every registered guard, in the order they run.
    /// </summary>
    /// <param name="aGuards">Every guard to run, resolved from dependency injection.</param>
    public GuardPipeline(IEnumerable<IToolGuard> aGuards)
    {
        objGuards = aGuards.ToList();
    }

    /// <summary>
    /// Evaluates a tool request against every guard, stopping at the first refusal.
    /// </summary>
    /// <param name="aRequest">The tool request to evaluate.</param>
    /// <param name="aCt">A token that cancels the evaluation.</param>
    /// <returns>The first refusal, or an allowing result when every guard allows the request.</returns>
    /// <exception cref="NotImplementedException">
    /// REQ-FN-036 — cluster G. Each <see cref="IToolGuard"/> this loop calls still throws; this
    /// method itself has no REQ-specific logic and needs no change once they are built.
    /// </exception>
    public async Task<GuardResult> EvaluateAsync(ToolRequest aRequest, CancellationToken aCt = default)
    {
        foreach (var vGuard in objGuards)
        {
            var vResult = await vGuard.EvaluateAsync(aRequest, aCt).ConfigureAwait(false);
            if (!vResult.Allowed)
            {
                return vResult;
            }
        }

        return GuardResult.Allow();
    }
}
