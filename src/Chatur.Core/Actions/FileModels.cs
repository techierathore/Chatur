namespace Chatur.Core.Actions;

/// <summary>One row of the project's file tree (REQ-UI-039).</summary>
/// <param name="Name">The file or folder's own name.</param>
/// <param name="RelativePath">The path relative to the project root.</param>
/// <param name="IsFolder">Whether this row opens and closes rather than opening a tab.</param>
/// <param name="Children">The rows inside this folder; empty for a file, and empty for a folder whose contents are read only when it is opened (BRD-217).</param>
/// <param name="IsReadable">False for a folder Windows will not list (for example Documents\My Music): it is shown, marked not readable, and never opened (BRD-217).</param>
/// <param name="IsSolution">True for the root row when the project has a solution: the row stands for the solution, its <paramref name="RelativePath"/> is the solution file's name, and its <paramref name="Children"/> are already filled in (REQ-UI-125).</param>
/// <param name="IsProject">True for a folder that holds one of the solution's projects, so Files can draw it as a project (REQ-UI-125).</param>
public sealed record FileTreeNode(string Name, string RelativePath, bool IsFolder, IReadOnlyList<FileTreeNode> Children, bool IsReadable = true, bool IsSolution = false, bool IsProject = false);

/// <summary>One place a search found something in the project's files (REQ-UI-073, Edit ▸ Find in files and Go to file).</summary>
/// <param name="RelativePath">The file's path relative to the project root.</param>
/// <param name="Line">The 1-based line the text was found on; 0 when the file was found by its name.</param>
/// <param name="Text">The line's text, trimmed; empty when the file was found by its name.</param>
public sealed record FileSearchHit(string RelativePath, int Line, string Text);
