namespace Chatur.Core.Actions;

/// <summary>
/// Browsing, reading, saving and opening the selected project's files (Architecture §7; page
/// Workbench's files panel, tabs and editor).
/// </summary>
public interface IFileActions
{
    /// <summary>
    /// The top level of the selected project's file tree (REQ-UI-039): a folder's own contents are left
    /// empty and read by <see cref="ChildrenAsync"/> when it is opened, and a folder that cannot be read
    /// comes back marked <see cref="FileTreeNode.IsReadable"/> false rather than as an exception (BRD-217).
    /// Hidden names are left out unless the account shows them (BRD-225).
    /// </summary>
    /// <param name="aProjectId">The project to browse.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<FileTreeNode>> TreeAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>The entries of one folder of the project, read when that folder is opened in Files (BRD-217).</summary>
    /// <param name="aProjectId">The project to browse.</param>
    /// <param name="aRelativeFolder">The folder's path relative to the project root.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<FileTreeNode>> ChildrenAsync(int aProjectId, string aRelativeFolder, CancellationToken aCt = default) =>
        Task.FromResult<IReadOnlyList<FileTreeNode>>([]);

    /// <summary>
    /// Every file of the project at once, for Go to file and Find in files (REQ-UI-073); a folder that
    /// cannot be read is skipped. Folders come back with their contents filled in.
    /// </summary>
    /// <param name="aProjectId">The project to search.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<FileTreeNode>> WholeTreeAsync(int aProjectId, CancellationToken aCt = default) => TreeAsync(aProjectId, aCt);

    /// <summary>Whether the signed-in account shows hidden files in Files: names starting with a dot, and .vs (BRD-225).</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<bool> ShowHiddenAsync(CancellationToken aCt = default) => Task.FromResult(false);

    /// <summary>Keeps the account's View ▸ Show hidden files choice (BRD-225).</summary>
    /// <param name="aShow">Whether hidden files are shown.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    Task SetShowHiddenAsync(bool aShow, CancellationToken aCt = default) => Task.CompletedTask;

    /// <summary>
    /// Reads a file's text so it can open in a tab (REQ-UI-040).
    /// </summary>
    /// <param name="aProjectId">The project the file belongs to.</param>
    /// <param name="aRelativePath">The file's path relative to the project root.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<string> ReadAsync(int aProjectId, string aRelativePath, CancellationToken aCt = default);

    /// <summary>
    /// Writes a file's new text to disk (REQ-FN-044).
    /// </summary>
    /// <param name="aProjectId">The project the file belongs to.</param>
    /// <param name="aRelativePath">The file's path relative to the project root.</param>
    /// <param name="aContent">The text to write.</param>
    /// <param name="aCt">A token that cancels the save.</param>
    Task SaveAsync(int aProjectId, string aRelativePath, string aContent, CancellationToken aCt = default);

    /// <summary>
    /// Opens a file in the owner's chosen editor (REQ-FN-045).
    /// </summary>
    /// <param name="aProjectId">The project the file belongs to.</param>
    /// <param name="aRelativePath">The file's path relative to the project root.</param>
    /// <param name="aCt">A token that cancels the launch.</param>
    Task OpenInEditorAsync(int aProjectId, string aRelativePath, CancellationToken aCt = default);

    /// <summary>
    /// Opens a file with whatever program the machine associates with it (REQ-FN-046).
    /// </summary>
    /// <param name="aProjectId">The project the file belongs to.</param>
    /// <param name="aRelativePath">The file's path relative to the project root.</param>
    /// <param name="aCt">A token that cancels the launch.</param>
    Task OpenWithDefaultAppAsync(int aProjectId, string aRelativePath, CancellationToken aCt = default);
}
