namespace Chatur.Core.Actions;

/// <summary>One row of the project's file tree (REQ-UI-039).</summary>
/// <param name="Name">The file or folder's own name.</param>
/// <param name="RelativePath">The path relative to the project root.</param>
/// <param name="IsFolder">Whether this row opens and closes rather than opening a tab.</param>
/// <param name="Children">The rows inside this folder; empty for a file.</param>
public sealed record FileTreeNode(string Name, string RelativePath, bool IsFolder, IReadOnlyList<FileTreeNode> Children);

/// <summary>One place a search found something in the project's files (REQ-UI-073, Edit ▸ Find in files and Go to file).</summary>
/// <param name="RelativePath">The file's path relative to the project root.</param>
/// <param name="Line">The 1-based line the text was found on; 0 when the file was found by its name.</param>
/// <param name="Text">The line's text, trimmed; empty when the file was found by its name.</param>
public sealed record FileSearchHit(string RelativePath, int Line, string Text);
