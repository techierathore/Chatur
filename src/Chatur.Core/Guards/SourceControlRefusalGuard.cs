namespace Chatur.Core.Guards;

/// <summary>
/// Refuses every source-control command a model asks for, with no exception (REQ-UI-033, Coding
/// Standards "A model never runs a source-control command").
/// </summary>
/// <remarks>
/// Implemented by cluster O ahead of cluster G, so REQ-NFR-005 has a real rule to test rather than a
/// stub. The primary match is <c>"run-source-control"</c> — the exact <c>RoleRight.Action</c> name
/// <c>0002-SeedRoles.sql</c> already seeds as refused for every role — plus a small set of common git
/// porcelain names and a <c>"git-"</c>/<c>"source-control-"</c> prefix, as a defensive fallback until
/// cluster G's agent-loop tool-name catalogue exists. Cluster G should fold that catalogue's real
/// names into <see cref="SourceControlToolNames"/> as they land; doing so is additive, never a
/// workaround, because the rule itself (refuse, no exception) does not change.
/// </remarks>
public sealed class SourceControlRefusalGuard : IToolGuard
{
    /// <summary>The source-control tool names this guard refuses, matched case-insensitively.</summary>
    private static readonly HashSet<string> SourceControlToolNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "run-source-control",
        "source-control",
        "commit",
        "push",
        "pull",
        "checkout",
        "merge",
        "rebase",
        "branch",
        "reset",
        "clone",
        "stash",
        "tag",
    };

    /// <inheritdoc />
    public Task<GuardResult> EvaluateAsync(ToolRequest aRequest, CancellationToken aCt = default)
    {
        var vIsSourceControl =
            SourceControlToolNames.Contains(aRequest.ToolName) ||
            aRequest.ToolName.StartsWith("git-", StringComparison.OrdinalIgnoreCase) ||
            aRequest.ToolName.StartsWith("source-control-", StringComparison.OrdinalIgnoreCase);

        var vResult = vIsSourceControl
            ? GuardResult.Refuse($"\"{aRequest.ToolName}\" is a source-control command; a model never runs one.")
            : GuardResult.Allow();

        return Task.FromResult(vResult);
    }
}
