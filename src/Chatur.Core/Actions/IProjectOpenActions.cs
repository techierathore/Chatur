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
