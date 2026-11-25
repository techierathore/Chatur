namespace Chatur.Core.Actions;

/// <summary>
/// The folders Chatur searches, the projects found in them, and which one is selected (Architecture
/// §7 "Projects"; page Start / Workbench's project switcher).
/// </summary>
public interface IProjectActions
{
    /// <summary>The folders named on Start (REQ-UI-005).</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<ProjectFolder>> ListFoldersAsync(CancellationToken aCt = default);

    /// <summary>
    /// Names a new folder for Chatur to search (REQ-UI-005).
    /// </summary>
    /// <param name="aPath">The folder's absolute path.</param>
    /// <param name="aCt">A token that cancels the add.</param>
    Task<ProjectFolder> AddFolderAsync(string aPath, CancellationToken aCt = default);

    /// <summary>
    /// Removes a named folder. The folder on disk is untouched (REQ-UI-009).
    /// </summary>
    /// <param name="aFolderId">The folder to remove.</param>
    /// <param name="aCt">A token that cancels the removal.</param>
    Task RemoveFolderAsync(int aFolderId, CancellationToken aCt = default);

    /// <summary>
    /// Searches every named folder and returns what it found (REQ-UI-006).
    /// </summary>
    /// <param name="aCt">A token that cancels the scan.</param>
    Task<IReadOnlyList<ProjectSummary>> ScanAsync(CancellationToken aCt = default);

    /// <summary>
    /// The projects already known, most recently opened first, read from the database alone: no folder on disk is
    /// searched. For lists that must never wait on the disk, such as the menu bar's (owner, 20 Nov 2026).
    /// </summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<ProjectSummary>> KnownAsync(CancellationToken aCt = default) => ScanAsync(aCt);

    /// <summary>
    /// The project every screen is currently showing (REQ-FN-006, REQ-FN-007).
    /// </summary>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The selected project, or <see langword="null"/> when none is selected.</returns>
    Task<ProjectSummary?> SelectedProjectAsync(CancellationToken aCt = default);

    /// <summary>
    /// Selects a project as the one every screen now works in (REQ-UI-007, REQ-UI-010).
    /// </summary>
    /// <param name="aProjectId">The project to select.</param>
    /// <param name="aCt">A token that cancels the selection.</param>
    Task SelectProjectAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// Drops a project from the recent list. The folder on disk is untouched, and the project stays
    /// dropped when its folder is scanned again (UI Design "Screen: Start", <c>forget</c>).
    /// </summary>
    /// <param name="aProjectId">The project to drop from the list.</param>
    /// <param name="aCt">A token that cancels the removal.</param>
    Task ForgetProjectAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// Start's reopening of the last project (REQ-FN-006, BRD-218): the selected project when its folder
    /// is there and can be read; otherwise nothing is opened and the reason comes back, so Start
    /// shows it in one line instead of the Workbench failing on it.
    /// </summary>
    /// <param name="aCt">A token that cancels the check.</param>
    async Task<ProjectReopen> ReopenAsync(CancellationToken aCt = default) => new(await SelectedProjectAsync(aCt).ConfigureAwait(false), null);
}
