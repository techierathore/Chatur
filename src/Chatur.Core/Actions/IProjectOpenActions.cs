namespace Chatur.Core.Actions;

/// <summary>
/// The ways into a project other than picking one from the list: open a solution or a folder from
/// disk, start a new solution, clone a repository, or close the one open (Architecture §7 "Projects";
/// the File menu, REQ-UI-073). Each of the first four ends with the project selected, as if it had
/// been chosen from the recent list.
/// </summary>
public interface IProjectOpenActions
{
    /// <summary>Opens a solution: a <c>.sln</c> file, or a folder that holds one.</summary>
    /// <param name="aPath">The solution file's, or its folder's, absolute path.</param>
    /// <param name="aCt">A token that cancels the open.</param>
    /// <returns>The project, now selected.</returns>
    /// <exception cref="InvalidOperationException">Nothing there, or no solution file in the folder.</exception>
    Task<ProjectSummary> OpenSolutionAsync(string aPath, CancellationToken aCt = default);

    /// <summary>Opens any folder on disk as a project, whether it holds a solution or not.</summary>
    /// <param name="aPath">The folder's absolute path.</param>
    /// <param name="aCt">A token that cancels the open.</param>
    /// <returns>The project, now selected.</returns>
    /// <exception cref="InvalidOperationException">The folder does not exist.</exception>
    Task<ProjectSummary> OpenFolderAsync(string aPath, CancellationToken aCt = default);

    /// <summary>
    /// Looks at a folder before it is opened (REQ-FN-325): whether it holds a solution, and the projects it holds when it
    /// does not, so the person can be told and asked first. Nothing is changed.
    /// </summary>
    /// <param name="aPath">The folder's absolute path.</param>
    /// <param name="aCt">A token that cancels the look.</param>
    Task<FolderCheck> CheckFolderAsync(string aPath, CancellationToken aCt = default) => Task.FromResult(new FolderCheck(true, []));

    /// <summary>
    /// Opens a file (BRD-215): the project is the nearest folder above it that holds a solution or a
    /// repository, or else the file's own folder, and the file is the one to show first.
    /// </summary>
    /// <param name="aPath">The file's absolute path.</param>
    /// <param name="aCt">A token that cancels the open.</param>
    /// <returns>The project, now selected, and the file's path relative to it.</returns>
    /// <exception cref="InvalidOperationException">There is no file at that path.</exception>
    Task<ProjectFileOpened> OpenFileAsync(string aPath, CancellationToken aCt = default);

    /// <summary>Starts a new, empty solution in a new folder named after it, and opens it.</summary>
    /// <param name="aParentFolder">The folder the new project's folder is made in.</param>
    /// <param name="aName">The new solution's name.</param>
    /// <param name="aCt">A token that cancels the creation.</param>
    /// <returns>The project, now selected.</returns>
    /// <exception cref="InvalidOperationException">The name is not usable, the folder is taken, or the template step failed.</exception>
    Task<ProjectSummary> CreateSolutionAsync(string aParentFolder, string aName, CancellationToken aCt = default);

    /// <summary>Clones a repository into a folder named after it, and opens it.</summary>
    /// <param name="aAddress">The repository's address, from GitHub or anywhere else.</param>
    /// <param name="aParentFolder">The folder the clone is made in.</param>
    /// <param name="aCt">A token that cancels the clone.</param>
    /// <returns>The project, now selected.</returns>
    /// <exception cref="InvalidOperationException">The address is empty, the folder is taken, or the clone failed.</exception>
    Task<ProjectSummary> CloneAsync(string aAddress, string aParentFolder, CancellationToken aCt = default);

    /// <summary>Closes the open project: nothing is selected, and nothing is reopened at the next start.</summary>
    /// <param name="aCt">A token that cancels the close.</param>
    Task CloseProjectAsync(CancellationToken aCt = default);
}

/// <summary>What <see cref="IProjectOpenActions.OpenFileAsync"/> opened (BRD-215).</summary>
/// <param name="Project">The project that holds the file, now selected.</param>
/// <param name="RelativePath">The file's path relative to the project's folder, with <c>/</c> between parts, as the files panel names it.</param>
public sealed record ProjectFileOpened(ProjectSummary Project, string RelativePath);

/// <summary>What <see cref="IProjectOpenActions.CheckFolderAsync"/> found in a folder (REQ-FN-325).</summary>
/// <param name="HasSolution">Whether a <c>.sln</c> or <c>.slnx</c> file is directly in the folder.</param>
/// <param name="Projects">The project files in the folder and up to four folders down, relative to it, when it holds no solution; empty otherwise.</param>
public sealed record FolderCheck(bool HasSolution, IReadOnlyList<string> Projects);
