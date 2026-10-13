using System.Data;
using Chatur.Core;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Chatur.Core.Projects;
using ChaturDb;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Tests for <see cref="ProjectOpenActions"/>, the File menu's ways into a project (REQ-UI-073),
/// against a real temporary SQLite file and real temporary folders.
/// </summary>
public sealed class ProjectOpenActionsTests : IDisposable
{
    private readonly string objDatabasePath = Path.Combine(Path.GetTempPath(), $"chatur-open-tests-{Guid.NewGuid():N}.db");
    private readonly string objRoot = Path.Combine(Path.GetTempPath(), $"chatur-open-root-{Guid.NewGuid():N}");
    private readonly AppState objAppState = new();
    private readonly FakeLauncher objLauncher = new();
    private readonly ProjectOpenActions objSut;

    /// <summary>Migrates a fresh temporary database and makes a fresh temporary folder for every test.</summary>
    public ProjectOpenActionsTests()
    {
        var vResult = ChaturDbMigrator.Migrate($"Data Source={objDatabasePath}");
        Assert.True(vResult.Successful, vResult.Error?.Message);
        Directory.CreateDirectory(objRoot);

        var vConnections = new FixedPathConnectionFactory(objDatabasePath);
        var vProjects = new ProjectActions(vConnections, new FixedClock(), objAppState);
        objSut = new ProjectOpenActions(vConnections, vProjects, objLauncher, objAppState);
    }

    /// <summary>When the owner opens a folder, then it becomes the selected project, named after the folder.</summary>
    [Fact]
    public async Task OpenedFolderIsSelected()
    {
        var vFolder = Directory.CreateDirectory(Path.Combine(objRoot, "plain")).FullName;

        var vProject = await objSut.OpenFolderAsync(vFolder);

        Assert.Equal("plain", vProject.Name);
        Assert.Equal("Folder", vProject.Kind);
        Assert.Equal(vProject.ProjectId, objAppState.SelectedProject?.ProjectId);
    }

    /// <summary>A folder that is not there is refused with a reason, and nothing is selected.</summary>
    [Fact]
    public async Task MissingFolderIsRefused()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => objSut.OpenFolderAsync(Path.Combine(objRoot, "missing")));

        Assert.Null(objAppState.SelectedProject);
    }

    /// <summary>Opening the same folder twice gives the same project, not a second row.</summary>
    [Fact]
    public async Task OpeningTheSameFolderTwiceGivesOneProject()
    {
        var vFolder = Directory.CreateDirectory(Path.Combine(objRoot, "twice")).FullName;

        var vFirst = await objSut.OpenFolderAsync(vFolder);
        var vSecond = await objSut.OpenFolderAsync(vFolder);

        Assert.Equal(vFirst.ProjectId, vSecond.ProjectId);
    }

    /// <summary>When the owner gives a .sln file, then the solution is the project, named after the file.</summary>
    [Fact]
    public async Task SolutionFileOpensAsSolution()
    {
        var vSolution = Path.Combine(objRoot, "App.sln");
        await File.WriteAllTextAsync(vSolution, "Microsoft Visual Studio Solution File");

        var vProject = await objSut.OpenSolutionAsync(vSolution);

        Assert.Equal("App", vProject.Name);
        Assert.Equal("Solution", vProject.Kind);
        Assert.Equal(vSolution, vProject.Path);
    }

    /// <summary>When the owner gives the folder that holds a .sln, then the solution in it opens.</summary>
    [Fact]
    public async Task FolderWithSolutionOpensAsSolution()
    {
        await File.WriteAllTextAsync(Path.Combine(objRoot, "Inside.sln"), "x");

        var vProject = await objSut.OpenSolutionAsync(objRoot);

        Assert.Equal("Inside", vProject.Name);
    }

    /// <summary>A folder with no solution in it is refused with a reason that says so.</summary>
    [Fact]
    public async Task FolderWithoutSolutionIsRefused()
    {
        var vError = await Assert.ThrowsAsync<InvalidOperationException>(() => objSut.OpenSolutionAsync(objRoot));

        Assert.Contains("no solution", vError.Message);
    }

    /// <summary>When the owner closes the project, then nothing is selected and the choice is not kept for the next start.</summary>
    [Fact]
    public async Task ClosingTheProjectClearsTheSelection()
    {
        var vFolder = Directory.CreateDirectory(Path.Combine(objRoot, "closing")).FullName;
        await objSut.OpenFolderAsync(vFolder);

        await objSut.CloseProjectAsync();

        Assert.Null(objAppState.SelectedProject);
        var vProjects = new ProjectActions(new FixedPathConnectionFactory(objDatabasePath), new FixedClock(), new AppState());
        Assert.Null(await vProjects.SelectedProjectAsync());
    }

    /// <summary>A new project makes its folder, runs the solution template in it, and opens the solution it wrote.</summary>
    [Fact]
    public async Task NewProjectMakesTheSolutionAndOpensIt()
    {
        objLauncher.OnRun = (aCommand, aArguments, aDirectory) => File.WriteAllText(Path.Combine(aDirectory, "Fresh.sln"), "x");

        var vProject = await objSut.CreateSolutionAsync(objRoot, "Fresh");

        Assert.Equal("dotnet", objLauncher.Command);
        Assert.Contains("new sln", objLauncher.Arguments);
        Assert.Equal("Fresh", vProject.Name);
        Assert.Equal(Path.Combine(objRoot, "Fresh", "Fresh.sln"), vProject.Path);
    }

    /// <summary>A name with a slash cannot be a folder name, so no project is made.</summary>
    [Fact]
    public async Task NewProjectNameWithSlashIsRefused()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => objSut.CreateSolutionAsync(objRoot, "a/b"));

        Assert.Null(objLauncher.Command);
    }

    /// <summary>When the template step fails, then the owner is told what it said and nothing is opened.</summary>
    [Fact]
    public async Task FailedTemplateStepIsReported()
    {
        objLauncher.ExitCode = 1;
        objLauncher.Output = "The template could not be found";

        var vError = await Assert.ThrowsAsync<InvalidOperationException>(() => objSut.CreateSolutionAsync(objRoot, "Broken"));

        Assert.Contains("could not be found", vError.Message);
        Assert.Null(objAppState.SelectedProject);
    }

    /// <summary>A clone goes into a folder named after the repository, and the result opens as the project.</summary>
    [Fact]
    public async Task CloneOpensTheClonedFolder()
    {
        objLauncher.OnRun = (aCommand, aArguments, aDirectory) => Directory.CreateDirectory(Path.Combine(aDirectory, "repo"));

        var vProject = await objSut.CloneAsync("https://example.com/owner/repo.git", objRoot);

        Assert.Equal("clone", objLauncher.Arguments?.Split(' ')[0]);
        Assert.Equal("repo", vProject.Name);
    }

    /// <summary>The folder name is the last part of the address without its extension, for web, ssh and local addresses alike.</summary>
    [Theory]
    [InlineData("https://github.com/techierathore/Chatur.git", "Chatur")]
    [InlineData("https://github.com/techierathore/Chatur/", "Chatur")]
    [InlineData("someone@github.com:techierathore/Chatur.git", "Chatur")]
    [InlineData("/home/someone/src/thing", "thing")]
    public void RepositoryNameIsTheLastPartOfTheAddress(string aAddress, string aExpected)
    {
        Assert.Equal(aExpected, ProjectOpenActions.RepositoryName(aAddress));
    }

    /// <summary>Removes the temporary database and folders.</summary>
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(objDatabasePath))
        {
            File.Delete(objDatabasePath);
        }

        if (Directory.Exists(objRoot))
        {
            Directory.Delete(objRoot, true);
        }
    }

    private sealed class FakeLauncher : IProcessLauncher
    {
        public string? Command { get; private set; }

        public string? Arguments { get; private set; }

        public int ExitCode { get; set; }

        public string Output { get; set; } = string.Empty;

        public Action<string, string, string>? OnRun { get; set; }

        public Task OpenInEditorAsync(string aFilePath, CancellationToken aCt = default) => Task.CompletedTask;

        public Task OpenWithDefaultAppAsync(string aFilePath, CancellationToken aCt = default) => Task.CompletedTask;

        public Task<int> RunAsync(string aCommand, string aArguments, string aWorkingDirectory, Action<string> aOnOutputLine, CancellationToken aCt = default)
        {
            Command = aCommand;
            Arguments = aArguments;
            if (Output.Length > 0)
            {
                aOnOutputLine(Output);
            }

            if (ExitCode == 0)
            {
                OnRun?.Invoke(aCommand, aArguments, aWorkingDirectory);
            }

            return Task.FromResult(ExitCode);
        }
    }

    private sealed class FixedPathConnectionFactory : IDbConnectionFactory
    {
        private readonly string objConnectionString;

        public FixedPathConnectionFactory(string aDatabasePath)
        {
            objConnectionString = $"Data Source={aDatabasePath}";
        }

        public IDbConnection OpenConnection()
        {
            var vConnection = new SqliteConnection(objConnectionString);
            vConnection.Open();
            return vConnection;
        }
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 10, 13, 9, 0, 0, DateTimeKind.Utc);
    }
}
