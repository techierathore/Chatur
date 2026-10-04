namespace Chatur.Core.Actions;

/// <summary>
/// Probing the tools a project needs and reporting Chatur's own version (Architecture §7 "Doctor";
/// page Prerequisites).
/// </summary>
public interface IPrerequisiteActions
{
    /// <summary>
    /// Probes every tool the given project needs, or just Chatur's own prerequisites when no project
    /// is open (REQ-FN-011, REQ-FN-012).
    /// </summary>
    /// <param name="aProjectId">The project to probe for, or <see langword="null"/> when none is open.</param>
    /// <param name="aCt">A token that cancels the probe.</param>
    Task<IReadOnlyList<ToolCheck>> ProbeAsync(int? aProjectId, CancellationToken aCt = default);

    /// <summary>This Chatur's own version and the commit it was built from (REQ-UI-018).</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<ChaturBuildInfo> OwnVersionAsync(CancellationToken aCt = default);

    /// <summary>
    /// Checks whether a newer nightly build exists for this machine (REQ-FN-013).
    /// </summary>
    /// <param name="aCt">A token that cancels the check.</param>
    /// <returns>The newer build's identity, or <see langword="null"/> when this is already the newest.</returns>
    Task<ChaturBuildInfo?> NewerBuildAsync(CancellationToken aCt = default);
}
