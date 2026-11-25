using System.Collections.Concurrent;
using System.Text;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;

namespace Chatur.Core.Projects;

/// <summary>
/// <see cref="IProjectTemplateActions"/> over the machine's own <c>dotnet</c>, run through
/// <see cref="IProcessLauncher"/> in the solution's folder (BRD-244, BRD-247, BRD-276, REQ-UI-112). The template list
/// and each template's frameworks are read once per process, because <c>dotnet new</c> takes seconds to answer.
/// </summary>
public sealed class ProjectTemplateActions : IProjectTemplateActions
{
    // Process-wide, for the same reason as BuildRunActions' run registry: the actions are scoped, the
    // machine's templates are not.
    private static readonly SemaphoreSlim objListGate = new(1, 1);
    private static IReadOnlyList<ProjectTemplate>? objTemplateCache;
    private static readonly ConcurrentDictionary<string, TemplateFrameworks> objFrameworkCache = new(StringComparer.OrdinalIgnoreCase);
    private static string? objSolutionExtension;

    /// <summary>The characters no file name may hold on either system Chatur runs on, so a name made on one opens on the other.</summary>
    private static readonly char[] InvalidNameChars = ['/', '\\', ':', '*', '?', '"', '<', '>', '|', '\0'];

    private static readonly HashSet<string> ProjectExtensions = new(StringComparer.OrdinalIgnoreCase) { ".csproj", ".fsproj", ".vbproj" };

    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "node_modules", "packages" };

    private readonly IDbConnectionFactory objConnections;
    private readonly IProcessLauncher objLauncher;
    private readonly IProjectOpenActions objOpener;

    /// <summary>Creates the actions.</summary>
    /// <param name="aConnections">Opens connections to Chatur's own database.</param>
    /// <param name="aLauncher">Runs <c>dotnet</c>.</param>
    /// <param name="aOpener">Opens the solution a new project was made in.</param>
    public ProjectTemplateActions(IDbConnectionFactory aConnections, IProcessLauncher aLauncher, IProjectOpenActions aOpener)
    {
        objConnections = aConnections;
        objLauncher = aLauncher;
        objOpener = aOpener;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProjectTemplate>> TemplatesAsync(int? aProjectId, CancellationToken aCt = default)
    {
        if (objTemplateCache is { } vCached)
        {
            return vCached;
        }

        var vFolder = await WorkingFolderAsync(aProjectId, aCt).ConfigureAwait(false);
        await objListGate.WaitAsync(aCt).ConfigureAwait(false);
        try
        {
            if (objTemplateCache is { } vAgain)
            {
                return vAgain;
            }

            var vOutput = await RunAsync(DotnetTemplateOutput.ListArguments, vFolder, "Listing the project templates", aCt).ConfigureAwait(false);
            var vTemplates = DotnetTemplateOutput.ParseTemplates(vOutput);
            if (vTemplates.Count == 0)
            {
                throw new InvalidOperationException("dotnet new list showed no project templates.");
            }

            objTemplateCache = vTemplates;
            return vTemplates;
        }
        finally
        {
            objListGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<TemplateFrameworks> FrameworksAsync(int? aProjectId, string aShortName, CancellationToken aCt = default)
    {
        if (objFrameworkCache.TryGetValue(aShortName, out var vCached))
        {
            return vCached;
        }

        var vFolder = await WorkingFolderAsync(aProjectId, aCt).ConfigureAwait(false);
        var vOutput = await RunAsync(DotnetTemplateOutput.HelpArguments(aShortName), vFolder, "Reading the template's frameworks", aCt).ConfigureAwait(false);
        var vFrameworks = DotnetTemplateOutput.ParseFrameworks(vOutput);
        objFrameworkCache[aShortName] = vFrameworks;
        return vFrameworks;
    }

    /// <inheritdoc />
    public async Task<string> SolutionExtensionAsync(int? aProjectId, CancellationToken aCt = default)
    {
        if (objSolutionExtension is { } vCached)
        {
            return vCached;
        }

        var vFolder = await WorkingFolderAsync(aProjectId, aCt).ConfigureAwait(false);
        var vOutput = await RunAsync("--version", vFolder, "Reading the .NET version", aCt).ConfigureAwait(false);
        var vExtension = DotnetTemplateOutput.SolutionExtensionFor(vOutput);
        objSolutionExtension = vExtension;
        return vExtension;
    }

    /// <inheritdoc />
    public NewProjectPlan Plan(NewProjectRequest aRequest, string aSolutionExtension)
    {
        ArgumentNullException.ThrowIfNull(aRequest);
        var vOpenSolution = string.IsNullOrWhiteSpace(aRequest.SolutionFile) ? null : Path.GetFullPath(aRequest.SolutionFile);
        var vLocationText = (aRequest.Location ?? string.Empty).Trim().Trim('"');
        if (vLocationText.Length == 0 && vOpenSolution is not null)
        {
            vLocationText = Path.GetDirectoryName(vOpenSolution) ?? string.Empty;
        }

        if (vLocationText.Length == 0)
        {
            return new NewProjectPlan(string.Empty, null, false, null, null, [], "Choose the folder the solution goes in.");
        }

        if (!Path.IsPathFullyQualified(vLocationText))
        {
            return new NewProjectPlan(vLocationText, null, false, null, null, [], "Give the full path of the folder.");
        }

        var vFolder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(vLocationText));
        if (File.Exists(vFolder))
        {
            return new NewProjectPlan(vFolder, null, false, null, null, [], $"{vFolder} is a file. Choose a folder.");
        }

        // The solution: the one that is open, else the one the folder already has, else a new one in the folder itself.
        string? vProblem = null;
        var vSolution = vOpenSolution;
        var vExists = vOpenSolution is not null;
        IReadOnlyList<string> vToAdd = [];
        var vSolutionName = (aRequest.SolutionName ?? string.Empty).Trim();
        if (vSolution is null)
        {
            try
            {
                vSolution = Directory.Exists(vFolder)
                    ? Directory.EnumerateFiles(vFolder).Where(ProjectOpenActions.IsSolutionFile).OrderBy(f => f, StringComparer.OrdinalIgnoreCase).FirstOrDefault()
                    : null;
                vExists = vSolution is not null;
                if (!vExists)
                {
                    vToAdd = ProjectsIn(vFolder);
                }
            }
            catch (Exception aException) when (aException is IOException or UnauthorizedAccessException)
            {
                vProblem = $"{vFolder} could not be read: {aException.Message}";
            }

            if (!vExists && vSolutionName.Length > 0)
            {
                vProblem ??= NameProblem(vSolutionName, "solution");
                vSolution = vProblem is null ? Path.Combine(vFolder, vSolutionName + aSolutionExtension) : null;
            }
        }

        // The project: one folder directly under the solution's folder, named after the project.
        var vProjectName = (aRequest.ProjectName ?? string.Empty).Trim();
        if (vProjectName.Length == 0 && !vExists)
        {
            vProjectName = vSolutionName;
        }

        string? vProjectFolder = null;
        string? vProjectFile = null;
        if (vProjectName.Length > 0 && (vProblem ??= NameProblem(vProjectName, "project")) is null)
        {
            vProjectFolder = Path.Combine(vFolder, vProjectName);
            vProjectFile = Path.Combine(vProjectFolder, vProjectName + ".csproj");
            if (File.Exists(vProjectFolder) || (Directory.Exists(vProjectFolder) && Directory.EnumerateFileSystemEntries(vProjectFolder).Any()))
            {
                vProblem = $"{vProjectFolder} already exists and is not empty. Give the project another name.";
            }
        }

        return new NewProjectPlan(vFolder, vSolution, vExists, vProjectFolder, vProjectFile, vToAdd, vProblem);
    }

    /// <inheritdoc />
    public async Task<NewProjectResult> CreateAsync(NewProjectRequest aRequest, CancellationToken aCt = default)
    {
        ArgumentNullException.ThrowIfNull(aRequest);
        var vExtension = string.IsNullOrWhiteSpace(aRequest.SolutionFile) && !SolutionIsIn(aRequest.Location)
            ? await SolutionExtensionAsync(null, aCt).ConfigureAwait(false)
            : ".slnx";
        var vPlan = Plan(aRequest, vExtension);
        if (vPlan.Problem is { } vProblem)
        {
            throw new InvalidOperationException(vProblem);
        }

        if (vPlan.SolutionFile is null)
        {
            throw new InvalidOperationException("Give the solution a name.");
        }

        if (vPlan.ProjectFolder is null)
        {
            throw new InvalidOperationException("Give the project a name.");
        }

        var vFolder = vPlan.Folder;
        var vProjectName = Path.GetFileName(vPlan.ProjectFolder);
        var vMadeFolder = !Directory.Exists(vFolder);
        Directory.CreateDirectory(vFolder);
        var vSolution = vPlan.SolutionFile;
        var vMadeSolution = false;
        string? vProject;
        try
        {
            if (!vPlan.SolutionExists)
            {
                await RunAsync($"new sln --name \"{Path.GetFileNameWithoutExtension(vSolution)}\" -o \"{vFolder}\"", vFolder, "Making the solution", aCt).ConfigureAwait(false);

                // `dotnet new sln` writes .sln or .slnx by SDK version; either is the solution.
                vSolution = Directory.GetFiles(vFolder).Where(ProjectOpenActions.IsSolutionFile).OrderBy(f => f, StringComparer.OrdinalIgnoreCase).FirstOrDefault()
                    ?? throw new InvalidOperationException("The solution was made but its file was not found.");
                vMadeSolution = true;
            }

            await RunAsync(DotnetTemplateOutput.NewArguments(aRequest.ShortName, vProjectName, vPlan.ProjectFolder, aRequest.Framework), vFolder, "Making the project", aCt).ConfigureAwait(false);
            vProject = Directory.Exists(vPlan.ProjectFolder)
                ? Directory.GetFiles(vPlan.ProjectFolder, "*.*proj").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).FirstOrDefault()
                : null;
            if (vProject is null)
            {
                throw new InvalidOperationException("The project was made but its project file was not found.");
            }
        }
        catch (Exception aException) when (aException is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            // Nothing half-made is left behind: an empty solution of ours, and the folder when it was made for it.
            if (vMadeSolution && File.Exists(vSolution))
            {
                File.Delete(vSolution);
            }

            if (vMadeFolder && Directory.Exists(vFolder) && !Directory.EnumerateFileSystemEntries(vFolder).Any())
            {
                Directory.Delete(vFolder);
            }

            throw;
        }

        var vFound = vPlan.ProjectsToAdd.Select(p => Path.GetFullPath(Path.Combine(vFolder, p))).ToArray();
        await RunAsync(DotnetTemplateOutput.AddToSolutionArguments(vSolution, [vProject, .. vFound]), vFolder, "Adding the project to the solution", aCt).ConfigureAwait(false);

        // A solution that was not the open one is opened; the open one stays as it is.
        var vOpenedNow = string.IsNullOrWhiteSpace(aRequest.SolutionFile);
        int vProjectId;
        if (vOpenedNow)
        {
            vProjectId = (await objOpener.OpenSolutionAsync(vSolution, aCt).ConfigureAwait(false)).ProjectId;
        }
        else
        {
            using var vRead = objConnections.OpenConnection();
            vProjectId = await vRead.QuerySingleOrDefaultAsync<int?>(
                new CommandDefinition("SELECT ProjectId FROM Project WHERE Path = @Path;", new { Path = aRequest.SolutionFile }, cancellationToken: aCt)).ConfigureAwait(false)
                ?? (await objOpener.OpenSolutionAsync(vSolution, aCt).ConfigureAwait(false)).ProjectId;
        }

        using (var vConnection = objConnections.OpenConnection())
        {
            // The stored run targets are the old project list's; forgetting them makes the next read find the new project.
            await vConnection.ExecuteAsync(
                new CommandDefinition("DELETE FROM RunTarget WHERE ProjectId = @ProjectId;", new { ProjectId = vProjectId }, cancellationToken: aCt)).ConfigureAwait(false);

            // The folder that was open now holds the solution: the solution stands for it in the project list.
            await vConnection.ExecuteAsync(new CommandDefinition(
                "UPDATE Project SET RemovedUtc = @Now WHERE Kind = 'Folder' AND Path = @Path AND RemovedUtc IS NULL;",
                new { Now = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture), Path = vFolder },
                cancellationToken: aCt)).ConfigureAwait(false);
        }

        return new NewProjectResult(vProjectId, vSolution, vProject, vFound, vOpenedNow);
    }

    /// <summary>Whether a folder already holds a solution file, so no new one will be named.</summary>
    private static bool SolutionIsIn(string? aFolder)
    {
        var vFolder = (aFolder ?? string.Empty).Trim().Trim('"');
        try
        {
            return vFolder.Length > 0 && Directory.Exists(vFolder) && Directory.EnumerateFiles(vFolder).Any(ProjectOpenActions.IsSolutionFile);
        }
        catch (Exception aException) when (aException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// The project files a folder holds, relative to it and in name order: its own and those up to four folders down,
    /// leaving out build output, packages and hidden folders. Empty when the folder is not there.
    /// </summary>
    private static IReadOnlyList<string> ProjectsIn(string aFolder)
    {
        var vFound = new List<string>();
        if (!Directory.Exists(aFolder))
        {
            return vFound;
        }

        void Walk(string aCurrent, int aDepth)
        {
            foreach (var vFile in Directory.EnumerateFiles(aCurrent))
            {
                if (Path.GetExtension(vFile) is { } vExtension && ProjectExtensions.Contains(vExtension))
                {
                    vFound.Add(Path.GetRelativePath(aFolder, vFile));
                }
            }

            if (aDepth >= 4)
            {
                return;
            }

            foreach (var vChild in Directory.EnumerateDirectories(aCurrent))
            {
                var vName = Path.GetFileName(vChild);
                if (vName.StartsWith('.') || SkippedFolders.Contains(vName))
                {
                    continue;
                }

                try
                {
                    Walk(vChild, aDepth + 1);
                }
                catch (Exception aException) when (aException is IOException or UnauthorizedAccessException)
                {
                    // A folder that cannot be read holds no project Chatur can add.
                }
            }
        }

        Walk(aFolder, 0);
        vFound.Sort(StringComparer.OrdinalIgnoreCase);
        return vFound;
    }

    /// <summary>Why a name cannot be a folder and a <c>dotnet new</c> name, in plain words; <see langword="null"/> when it can.</summary>
    private static string? NameProblem(string aName, string aWhat) =>
        aName.Length == 0 || aName.IndexOfAny(InvalidNameChars) >= 0 || aName.StartsWith('.') || aName.EndsWith('.') || aName.Contains('"')
            ? $"Give the {aWhat} a name that can be a folder name, without slashes or other special characters."
            : null;

    /// <summary>
    /// Where <c>dotnet new list</c> and a template's help run: the open solution's folder, else the open folder,
    /// else the person's own folder. The folder matters only for a <c>global.json</c> that pins the SDK.
    /// </summary>
    private async Task<string> WorkingFolderAsync(int? aProjectId, CancellationToken aCt)
    {
        string? vPath = null;
        if (aProjectId is { } vProjectId)
        {
            using var vConnection = objConnections.OpenConnection();
            vPath = await vConnection.QuerySingleOrDefaultAsync<string>(
                new CommandDefinition("SELECT Path FROM Project WHERE ProjectId = @ProjectId;", new { ProjectId = vProjectId }, cancellationToken: aCt)).ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(vPath) && File.Exists(vPath) && Path.GetDirectoryName(vPath) is { } vSolutionFolder)
        {
            return vSolutionFolder;
        }

        if (!string.IsNullOrEmpty(vPath) && Directory.Exists(vPath))
        {
            return vPath;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    /// <summary>Runs <c>dotnet</c> and returns what it said; throws what it said when it fails.</summary>
    private async Task<string> RunAsync(string aArguments, string aWorkingDirectory, string aWhat, CancellationToken aCt)
    {
        var vOutput = new StringBuilder();
        int vExit;
        try
        {
            vExit = await objLauncher.RunAsync("dotnet", aArguments, aWorkingDirectory, aLine =>
            {
                lock (vOutput)
                {
                    vOutput.AppendLine(aLine);
                }
            }, aCt).ConfigureAwait(false);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new InvalidOperationException($"{aWhat} needs dotnet, and it was not found on this machine.");
        }

        string vText;
        lock (vOutput)
        {
            vText = vOutput.ToString();
        }

        if (vExit != 0)
        {
            var vLines = vText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var vTail = string.Join(Environment.NewLine, vLines.TakeLast(12));
            throw new InvalidOperationException($"{aWhat} failed (dotnet {aArguments}).{(vTail.Length == 0 ? string.Empty : Environment.NewLine + vTail)}");
        }

        return vText;
    }
}
