namespace Chatur.Core.Actions;

/// <summary>
/// Searching the selected project's files: by name (Go to file) and by what they say (Find in
/// files). Reads through <see cref="IFileActions"/>, so it looks only where the file tree does
/// (Architecture §7; the Edit menu, REQ-UI-073).
/// </summary>
public interface IFileSearchActions
{
    /// <summary>Finds files by any part of their path, best matches first; an empty name lists the first files.</summary>
    /// <param name="aProjectId">The project to search.</param>
    /// <param name="aName">Part of a file's name or path, in any case.</param>
    /// <param name="aMax">The most files to return.</param>
    /// <param name="aCt">A token that cancels the search.</param>
    Task<IReadOnlyList<FileSearchHit>> FindFilesAsync(int aProjectId, string aName, int aMax = 50, CancellationToken aCt = default);

    /// <summary>Finds the lines of the project's text files that hold some text, in any case.</summary>
    /// <param name="aProjectId">The project to search.</param>
    /// <param name="aText">The text to look for; nothing is found for an empty text.</param>
    /// <param name="aMax">The most lines to return.</param>
    /// <param name="aCt">A token that cancels the search.</param>
    Task<IReadOnlyList<FileSearchHit>> FindInFilesAsync(int aProjectId, string aText, int aMax = 200, CancellationToken aCt = default);
}
