using System.Globalization;
using System.Text;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;

namespace Chatur.Core.Projects;

/// <summary>
/// <see cref="IProjectOpenActions"/> over the <c>Project</c> table: a project opened from the File
/// menu is a row with no named folder (<c>ProjectFolderId</c> is null), selected through
/// <see cref="IProjectActions.SelectProjectAsync"/> exactly as one chosen from the recent list is
/// (REQ-UI-073). A new solution is made with <c>dotnet new sln</c> and a clone with <c>git clone</c>,
/// both through <see cref="IProcessLauncher"/>.
/// </summary>
public sealed class ProjectOpenActions : IProjectOpenActions
{
    /// <summary>The <c>Setting</c> row's key that holds the selected project's id; the same row <see cref="ProjectActions"/> keeps.</summary>
    private const string SelectedProjectSettingKey = "SelectedProjectId";

    private readonly IDbConnectionFactory objConnections;
    private readonly IProjectActions objProjects;
    private readonly IProcessLauncher objLauncher;
    private readonly AppState objAppState;

    /// <summary>Creates the actions.</summary>
    /// <param name="aConnections">Opens connections to Chatur's own database.</param>
    /// <param name="aProjects">Selects the project once it is a row.</param>
    /// <param name="aLauncher">Runs <c>dotnet</c> and <c>git</c>.</param>
    /// <param name="aAppState">The shared, in-memory state every window reads.</param>
    public ProjectOpenActions(IDbConnectionFactory aConnections, IProjectActions aProjects, IProcessLauncher aLauncher, AppState aAppState)
    {
        objConnections = aConnections;
        objProjects = aProjects;
        objLauncher = aLauncher;
        objAppState = aAppState;
    }

    /// <inheritdoc />
    public async Task<ProjectSummary> OpenSolutionAsync(string aPath, CancellationToken aCt = default)
    {
        var vPath = CleanPath(aPath);
        string? vSolution = null;
        if (File.Exists(vPath) && string.Equals(Path.GetExtension(vPath), ".sln", StringComparison.OrdinalIgnoreCase))
        {
            vSolution = vPath;
        }
        else if (Directory.Exists(vPath))
        {
            vSolution = Directory.GetFiles(vPath, "*.sln").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        }

        if (vSolution is null)
        {
            throw new InvalidOperationException(Directory.Exists(vPath)
                ? "That folder holds no solution (.sln) file."
                : "Nothing was found at that path. Give the path of a .sln file, or of the folder that holds one.");
        }

        return await RegisterAndSelectAsync(Path.GetFileNameWithoutExtension(vSolution), vSolution, "Solution", aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ProjectSummary> OpenFolderAsync(string aPath, CancellationToken aCt = default)
    {
        var vPath = CleanPath(aPath);
        if (!Directory.Exists(vPath))
        {
            throw new InvalidOperationException("That folder does not exist.");
        }

        return await RegisterAndSelectAsync(Path.GetFileName(vPath), vPath, "Folder", aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ProjectSummary> CreateSolutionAsync(string aParentFolder, string aName, CancellationToken aCt = default)
    {
        var vName = (aName ?? string.Empty).Trim();
        if (vName.Length == 0 || vName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || vName.StartsWith('.'))
        {
            throw new InvalidOperationException("Give the project a name that can be a folder name, without slashes or other special characters.");
        }

        var vParent = CleanPath(aParentFolder);
        if (!Directory.Exists(vParent))
        {
            throw new InvalidOperationException("The folder to make it in does not exist.");
        }

        var vFolder = Path.Combine(vParent, vName);
        if (Directory.Exists(vFolder) && Directory.EnumerateFileSystemEntries(vFolder).Any())
        {
            throw new InvalidOperationException($"{vName} already exists in that folder and is not empty.");
        }

        Directory.CreateDirectory(vFolder);
        await RunAsync("dotnet", $"new sln --name \"{vName}\"", vFolder, "Making the solution", aCt).ConfigureAwait(false);

        // `dotnet new sln` writes .sln or .slnx by SDK version; either is the solution.
        var vSolution = Directory.GetFiles(vFolder, "*.sln*").FirstOrDefault()
            ?? throw new InvalidOperationException("The solution was made but its file was not found.");
        return await RegisterAndSelectAsync(vName, vSolution, "Solution", aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ProjectSummary> CloneAsync(string aAddress, string aParentFolder, CancellationToken aCt = default)
    {
        var vAddress = (aAddress ?? string.Empty).Trim();
        if (vAddress.Length == 0)
        {
            throw new InvalidOperationException("Paste the repository's address.");
        }

        var vParent = CleanPath(aParentFolder);
        if (!Directory.Exists(vParent))
        {
            throw new InvalidOperationException("The folder to clone into does not exist.");
        }

        var vName = RepositoryName(vAddress);
        if (vName.Length == 0)
        {
            throw new InvalidOperationException("The address does not name a repository.");
        }

        var vTarget = Path.Combine(vParent, vName);
        if (Directory.Exists(vTarget) && Directory.EnumerateFileSystemEntries(vTarget).Any())
        {
            throw new InvalidOperationException($"{vName} already exists in that folder and is not empty.");
        }

        await RunAsync("git", $"clone \"{vAddress}\" \"{vTarget}\"", vParent, "The clone", aCt).ConfigureAwait(false);

        return Directory.GetFiles(vTarget, "*.sln").Length > 0
            ? await OpenSolutionAsync(vTarget, aCt).ConfigureAwait(false)
            : await OpenFolderAsync(vTarget, aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CloseProjectAsync(CancellationToken aCt = default)
    {
        using (var vConnection = objConnections.OpenConnection())
        {
            await vConnection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM Setting WHERE Key = @SettingKey;",
                    new { SettingKey = SelectedProjectSettingKey },
                    cancellationToken: aCt)).ConfigureAwait(false);
        }

        objAppState.SetSelectedProject(null);
    }

    /// <summary>The repository's folder name: the last part of the address without <c>.git</c>.</summary>
    /// <param name="aAddress">The address as the owner gave it.</param>
    public static string RepositoryName(string aAddress)
    {
        var vTrimmed = aAddress.TrimEnd('/', '\\');
        var vLast = vTrimmed[(vTrimmed.LastIndexOfAny(['/', '\\', ':']) + 1)..];
        if (vLast.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            vLast = vLast[..^4];
        }

        return vLast.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ? string.Empty : vLast;
    }

    private static string CleanPath(string aPath) => Path.GetFullPath((aPath ?? string.Empty).Trim().Trim('"'));

    /// <summary>Runs a command and throws what it said when it fails.</summary>
    private async Task RunAsync(string aCommand, string aArguments, string aWorkingDirectory, string aWhat, CancellationToken aCt)
    {
        var vOutput = new StringBuilder();
        int vExit;
        try
        {
            vExit = await objLauncher.RunAsync(aCommand, aArguments, aWorkingDirectory, aLine => vOutput.AppendLine(aLine), aCt).ConfigureAwait(false);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new InvalidOperationException($"{aWhat} needs {aCommand}, and it was not found on this machine.");
        }

        if (vExit != 0)
        {
            var vLast = vOutput.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
            throw new InvalidOperationException($"{aWhat} failed{(vLast is null ? "." : $": {vLast}")}");
        }
    }

    /// <summary>Makes the path a project (bringing back one that was forgotten) and selects it.</summary>
    private async Task<ProjectSummary> RegisterAndSelectAsync(string aName, string aPath, string aKind, CancellationToken aCt)
    {
        long vId;
        using (var vConnection = objConnections.OpenConnection())
        {
            await vConnection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO Project (ProjectFolderId, Name, Path, Kind, LastOpenedUtc) VALUES (NULL, @Name, @Path, @Kind, NULL)
                    ON CONFLICT (Path) DO UPDATE SET RemovedUtc = NULL;
                    """,
                    new { Name = aName, Path = aPath, Kind = aKind },
                    cancellationToken: aCt)).ConfigureAwait(false);
            vId = await vConnection.ExecuteScalarAsync<long>(
                new CommandDefinition("SELECT ProjectId FROM Project WHERE Path = @Path;", new { Path = aPath }, cancellationToken: aCt)).ConfigureAwait(false);
        }

        await objProjects.SelectProjectAsync((int)vId, aCt).ConfigureAwait(false);
        return objAppState.SelectedProject ?? throw new InvalidOperationException("The project was made but could not be selected.");
    }
}
