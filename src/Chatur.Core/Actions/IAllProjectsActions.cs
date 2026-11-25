namespace Chatur.Core.Actions;

/// <summary>
/// Every project Chatur knows, on one view (page All projects, <c>/all-projects</c>; BRD-145 to BRD-147).
/// </summary>
public interface IAllProjectsActions
{
    /// <summary>
    /// Lists every project Chatur knows with its phase, how its requirements stand and when it was last
    /// worked on (REQ-FN-076, REQ-UI-067). The checklists are read from disk each time, so nothing is stale.
    /// </summary>
    /// <param name="aSearch">Narrows to projects whose name or folder holds this text; empty lists all.</param>
    /// <param name="aOrder">How to order the list.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<AllProjectsView> LoadAsync(string? aSearch, AllProjectsOrder aOrder, CancellationToken aCt = default);

    /// <summary>
    /// Makes a project the selected one, so Project home shows it (REQ-FN-077).
    /// </summary>
    /// <param name="aProjectId">The project to open.</param>
    /// <param name="aCt">A token that cancels the selection.</param>
    Task OpenAsync(int aProjectId, CancellationToken aCt = default);
}
