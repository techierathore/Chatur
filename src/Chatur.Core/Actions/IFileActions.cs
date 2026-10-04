namespace Chatur.Core.Actions;

/// <summary>
/// Browsing, reading, saving and opening the selected project's files (Architecture §7; page
/// Workbench's files panel, tabs and editor).
/// </summary>
public interface IFileActions
{
    /// <summary>The selected project's file tree (REQ-UI-039).</summary>
    /// <param name="aProjectId">The project to browse.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<FileTreeNode>> TreeAsync(int aProjectId, CancellationToken aCt = default);

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
