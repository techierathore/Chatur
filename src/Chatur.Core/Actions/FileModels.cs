namespace Chatur.Core.Actions;

/// <summary>One row of the project's file tree (REQ-UI-039).</summary>
/// <param name="Name">The file or folder's own name.</param>
/// <param name="RelativePath">The path relative to the project root.</param>
/// <param name="IsFolder">Whether this row opens and closes rather than opening a tab.</param>
/// <param name="Children">The rows inside this folder; empty for a file.</param>
public sealed record FileTreeNode(string Name, string RelativePath, bool IsFolder, IReadOnlyList<FileTreeNode> Children);
