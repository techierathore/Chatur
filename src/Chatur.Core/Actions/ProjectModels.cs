namespace Chatur.Core.Actions;

/// <summary>A folder the owner named for Chatur to search for projects (REQ-UI-005).</summary>
/// <param name="FolderId">The row's identity.</param>
/// <param name="Path">The folder's absolute path.</param>
public sealed record ProjectFolder(int FolderId, string Path);

/// <summary>A project found by searching the named folders, or opened directly (REQ-UI-006).</summary>
/// <param name="ProjectId">The row's identity.</param>
/// <param name="Name">The project's display name.</param>
/// <param name="Path">The project's absolute path — a solution file or a bare folder.</param>
/// <param name="Kind">What kind of project this is, e.g. <c>"Solution"</c> or <c>"Folder"</c>.</param>
/// <param name="LastOpenedUtc">When the project was last opened, or <see langword="null"/> if never.</param>
public sealed record ProjectSummary(int ProjectId, string Name, string Path, string Kind, DateTime? LastOpenedUtc);
