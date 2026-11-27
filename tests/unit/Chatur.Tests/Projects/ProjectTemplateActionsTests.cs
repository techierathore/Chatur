using System.Data;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Chatur.Core.Projects;
using ChaturDb;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Chatur.Tests.Projects;

/// <summary>
/// Tests for <see cref="ProjectTemplateActions.Plan"/> and <see cref="ProjectTemplateActions.CreateAsync"/> (BRD-244,
/// BRD-247, BRD-276 to BRD-280): for each starting point a person meets (nothing open, an empty folder, a folder that
/// is not empty, a folder with projects and no solution, a folder with a solution, a solution open) the exact paths on
/// disk after Create are compared, and the commands run. The launcher is a fake that makes the files
/// <c>dotnet new</c> would; the database and folders are real and temporary.
/// </summary>
/// <remarks>
/// Owner's test of 23 Nov 2026: the earlier tests only asked whether some solution file was in an empty folder, so a
/// folder holding one file of Chatur's own got the solution one folder too deep and every test passed.
/// </remarks>
public sealed class ProjectTemplateActionsTests : IDisposable
{
    private readonly string objDatabasePath = Path.Combine(Chatur.Tests.Support.TestScratch.Root, $"chatur-template-tests-{Guid.NewGuid():N}.db");
    private readonly string objRoot = Path.Combine(Chatur.Tests.Support.TestScratch.Root, $"chatur-template-root-{Guid.NewGuid():N}");
    private readonly FakeLauncher objLauncher = new();
    private readonly FakeOpener objOpener = new();
    private readonly FixedPathConnectionFactory objConnections;
    private readonly ProjectTemplateActions objSut;
    private readonly string objSolution;
    private readonly int objProjectId;

    /// <summary>Makes a migrated database, a folder with a solution file in it, and the solution's project row with one stored run target.</summary>
    public ProjectTemplateActionsTests()
    {
        var vResult = ChaturDbMigrator.Migrate($"Data Source={objDatabasePath}");
        Assert.True(vResult.Successful, vResult.Error?.Message);
        Directory.CreateDirectory(objRoot);
        objSolution = Path.Combine(objRoot, "Sln.sln");
        File.WriteAllText(objSolution, "x");

        objConnections = new FixedPathConnectionFactory(objDatabasePath);
        objSut = new ProjectTemplateActions(objConnections, objLauncher, objOpener);
        using var vConnection = objConnections.OpenConnection();
        vConnection.Execute("INSERT INTO Project (ProjectFolderId, Name, Path, Kind, LastOpenedUtc) VALUES (NULL, 'Sln', @Path, 'Solution', NULL);", new { Path = objSolution });
        objProjectId = vConnection.ExecuteScalar<int>("SELECT ProjectId FROM Project WHERE Path = @Path;", new { Path = objSolution });
        vConnection.Execute(
            "INSERT INTO RunTarget (ProjectId, Name, Command, Platform, IsLastUsed) VALUES (@ProjectId, 'Old', '{}', '', 0);",
            new { ProjectId = objProjectId });
    }

    /// <summary>Every file under a folder, relative to it, with forward slashes, in name order: the disk as the person sees it.</summary>
    private static string[] Disk(string aFolder) =>
        Directory.Exists(aFolder)
            ? Directory.EnumerateFiles(aFolder, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(aFolder, f).Replace('\\', '/'))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToArray()
            : [];

    private int AddFolderProject(string aFolder)
    {
        using var vConnection = objConnections.OpenConnection();
        vConnection.Execute("INSERT INTO Project (ProjectFolderId, Name, Path, Kind, LastOpenedUtc) VALUES (NULL, @Name, @Path, 'Folder', NULL);", new { Name = Path.GetFileName(aFolder), Path = aFolder });
        return vConnection.ExecuteScalar<int>("SELECT ProjectId FROM Project WHERE Path = @Path;", new { Path = aFolder });
    }

    /// <summary>
    /// Starting point 1, nothing open (owner's test of 25 Nov 2026): the person picks a location, and the solution's folder
    /// is not there. Create makes that folder named after the solution, the solution file in it and the project in a
    /// folder of its own inside it, also when the two names are the same; nothing lands directly in the location.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-018 REQ-FN-310 the solution gets a folder of its own in the picked location and each project one inside it")]
    public async Task NothingOpenSolutionGetsItsOwnFolder()
    {
        var vPicked = Path.Combine(objRoot, "Picked");
        var vSolutionFolder = Path.Combine(vPicked, "Todo");

        var vMade = await objSut.CreateAsync(new NewProjectRequest(vPicked, null, "Todo", string.Empty, "console", "net10.0"));

        Assert.Equal(["Todo/Todo.slnx", "Todo/Todo/Todo.csproj"], Disk(vPicked));
        Assert.Equal(Path.Combine(vSolutionFolder, "Todo.slnx"), vMade.SolutionFile);
        Assert.Equal(Path.Combine(vSolutionFolder, "Todo", "Todo.csproj"), vMade.ProjectFile);
        Assert.True(vMade.OpenedNow);
        Assert.Equal(Path.Combine(vSolutionFolder, "Todo.slnx"), objOpener.Opened);
        Assert.Equal(
            [
                $"new sln --name \"Todo\" -o \"{vSolutionFolder}\"",
                $"new console -n \"Todo\" -o \"{Path.Combine(vSolutionFolder, "Todo")}\" -f net10.0",
                $"sln \"{Path.Combine(vSolutionFolder, "Todo.slnx")}\" add \"{Path.Combine(vSolutionFolder, "Todo", "Todo.csproj")}\""
            ],
            objLauncher.Commands);
        Assert.All(objLauncher.Runs.Where(r => r.Arguments != "--version"), r => Assert.Equal(vSolutionFolder, r.Directory));
    }

    /// <summary>
    /// Starting point 2, an empty folder open: the solution gets its own folder inside it (the owner's step 1 of 25 Nov
    /// 2026, where the solution used to land beside the project) and the open folder stays in the project list.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-018 REQ-FN-310 with an empty folder open the solution gets its own folder inside it")]
    public async Task EmptyFolderGetsTheSolutionInItsOwnFolder()
    {
        var vFolder = Directory.CreateDirectory(Path.Combine(objRoot, "Empty")).FullName;
        AddFolderProject(vFolder);

        var vMade = await objSut.CreateAsync(new NewProjectRequest(vFolder, null, "Todo", "Todo.App", "console", "net10.0", true));

        Assert.Equal(["Todo/Todo.App/Todo.App.csproj", "Todo/Todo.slnx"], Disk(vFolder));
        Assert.Equal(Path.Combine(vFolder, "Todo", "Todo.slnx"), objOpener.Opened);
        Assert.Empty(vMade.ProjectsAdded);
        using var vCheck = objConnections.OpenConnection();
        Assert.Null(vCheck.ExecuteScalar<string?>("SELECT RemovedUtc FROM Project WHERE Path = @Path AND Kind = 'Folder';", new { Path = vFolder }));
    }

    /// <summary>
    /// The owner's case of 25 Nov 2026 (which replaces that of 23 Nov): the solution and the project have the same
    /// name, and the open folder holds other things. The solution's folder is made inside it, and the project goes in
    /// a folder of the same name inside that; what was in the folder is not touched.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-018 REQ-FN-310 a solution and a project of the same name each get their own folder, one inside the other")]
    public async Task SameNamesGetNestedFolders()
    {
        var vFolder = Directory.CreateDirectory(Path.Combine(objRoot, "ChaturOwnerTest")).FullName;
        Directory.CreateDirectory(Path.Combine(vFolder, "docs", "metrics"));
        File.WriteAllText(Path.Combine(vFolder, "docs", "metrics", "sessions.jsonl"), "{}\n");
        AddFolderProject(vFolder);

        var vInner = Path.Combine(vFolder, "Checking-Chatur");
        var vPlan = objSut.Plan(new NewProjectRequest(vFolder, null, "Checking-Chatur", string.Empty, "blazor", null, true), ".slnx");
        Assert.Equal(vInner, vPlan.Folder);
        Assert.Equal(Path.Combine(vInner, "Checking-Chatur.slnx"), vPlan.SolutionFile);
        Assert.Equal(Path.Combine(vInner, "Checking-Chatur"), vPlan.ProjectFolder);
        Assert.Null(vPlan.Problem);

        await objSut.CreateAsync(new NewProjectRequest(vFolder, null, "Checking-Chatur", string.Empty, "blazor", null, true));

        Assert.Equal(["Checking-Chatur/Checking-Chatur.slnx", "Checking-Chatur/Checking-Chatur/Checking-Chatur.csproj", "docs/metrics/sessions.jsonl"], Disk(vFolder));
    }

    /// <summary>
    /// Starting point 3, a folder with projects and no solution (BRD-276): the plan names the projects found, so the
    /// dialog can say so first; Create makes the solution in that folder and adds them and the new project to it.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-018 REQ-UI-124 a folder with projects and no solution gets a solution there that takes them in")]
    public async Task FolderWithProjectsAndNoSolutionGetsOneThatTakesThemIn()
    {
        var vFolder = Directory.CreateDirectory(Path.Combine(objRoot, "Loose")).FullName;
        Directory.CreateDirectory(Path.Combine(vFolder, "OwnerTest", "bin", "Debug"));
        File.WriteAllText(Path.Combine(vFolder, "OwnerTest", "OwnerTest.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(vFolder, "OwnerTest", "bin", "Debug", "Left.csproj"), "<Project />");
        AddFolderProject(vFolder);
        var vRequest = new NewProjectRequest(vFolder, null, "Work", "Second", "console", null, true);

        var vPlan = objSut.Plan(vRequest, ".slnx");
        Assert.Equal([Path.Combine("OwnerTest", "OwnerTest.csproj")], vPlan.ProjectsToAdd);
        Assert.False(vPlan.SolutionExists);
        Assert.Equal(vFolder, vPlan.Folder);

        // A location that is not the open folder gathers nothing, however many projects lie below it (the owner's bin of 25 Nov).
        var vElsewhere = objSut.Plan(vRequest with { InOpenFolder = false }, ".slnx");
        Assert.Empty(vElsewhere.ProjectsToAdd);
        Assert.Equal(Path.Combine(vFolder, "Work"), vElsewhere.Folder);

        var vMade = await objSut.CreateAsync(vRequest);

        Assert.Equal(["OwnerTest/OwnerTest.csproj", "OwnerTest/bin/Debug/Left.csproj", "Second/Second.csproj", "Work.slnx"], Disk(vFolder));
        Assert.Equal([Path.Combine(vFolder, "OwnerTest", "OwnerTest.csproj")], vMade.ProjectsAdded);
        Assert.Equal(
            $"sln \"{Path.Combine(vFolder, "Work.slnx")}\" add \"{Path.Combine(vFolder, "Second", "Second.csproj")}\" \"{Path.Combine(vFolder, "OwnerTest", "OwnerTest.csproj")}\"",
            objLauncher.Commands[^1]);
    }

    /// <summary>
    /// Starting point 4, a folder open that holds a solution (BRD-277): the project is made one folder under it and
    /// added to that solution; no second solution is made, and the solution is opened in place of the folder.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-018 REQ-FN-323 a folder with a solution gets the project added to that solution and no second solution")]
    public async Task FolderWithSolutionGetsTheProjectAdded()
    {
        AddFolderProject(objRoot);
        var vRequest = new NewProjectRequest(objRoot, null, "Ignored", "Tools", "classlib", null);

        var vPlan = objSut.Plan(vRequest, ".slnx");
        Assert.True(vPlan.SolutionExists);
        Assert.Equal(objSolution, vPlan.SolutionFile);
        Assert.Equal(Path.Combine(objRoot, "Tools"), vPlan.ProjectFolder);

        var vMade = await objSut.CreateAsync(vRequest);

        Assert.Equal(["Sln.sln", "Tools/Tools.csproj"], Disk(objRoot));
        Assert.Equal(objSolution, vMade.SolutionFile);
        Assert.Equal(objSolution, objOpener.Opened);
        Assert.DoesNotContain(objLauncher.Commands, c => c.StartsWith("new sln", StringComparison.Ordinal));
    }

    /// <summary>
    /// With the solution itself open: dotnet new runs in the solution's folder with the name, location and framework,
    /// dotnet sln add follows with the new .csproj, the solution's stored run targets are kept (reading them again finds the new project, and the project that ran stays chosen), and the
    /// solution, already open, is not opened again.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-112 with a solution open the project is made one folder under it, added, and the run targets kept")]
    public async Task ProjectIsMadeThenAddedAndTargetsForgotten()
    {
        var vLocation = Path.Combine(objRoot, "Tools");

        var vMade = await objSut.CreateAsync(new NewProjectRequest(string.Empty, objSolution, string.Empty, "Tools", "console", "net10.0"));

        Assert.Equal(Path.Combine(vLocation, "Tools.csproj"), vMade.ProjectFile);
        Assert.Equal(objProjectId, vMade.ProjectId);
        Assert.False(vMade.OpenedNow);
        Assert.Null(objOpener.Opened);
        Assert.Equal(
            [$"new console -n \"Tools\" -o \"{vLocation}\" -f net10.0", $"sln \"{objSolution}\" add \"{vMade.ProjectFile}\""],
            objLauncher.Commands);
        Assert.Equal(["Sln.sln", "Tools/Tools.csproj"], Disk(objRoot));
        using var vConnection = objConnections.OpenConnection();
        Assert.Equal(1, vConnection.ExecuteScalar<int>("SELECT COUNT(*) FROM RunTarget WHERE ProjectId = @ProjectId;", new { ProjectId = objProjectId }));
    }

    /// <summary>
    /// REQ-UI-126: the plan gives the two full paths the dialog shows, following the names as they are typed, and
    /// changes nothing on disk. No name yet: no path. A solution name alone names the project too.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-126 the plan names the solution file and the project folder as the names are typed and changes nothing")]
    public void PlanNamesTheTwoPathsAndChangesNothing()
    {
        var vFolder = Directory.CreateDirectory(Path.Combine(objRoot, "Fresh")).FullName;

        var vNoName = objSut.Plan(new NewProjectRequest(vFolder, null, string.Empty, string.Empty, "console", null), ".slnx");
        Assert.Null(vNoName.SolutionFile);
        Assert.Null(vNoName.ProjectFolder);

        var vSolutionOnly = objSut.Plan(new NewProjectRequest(vFolder, null, "Todo", string.Empty, "console", null), ".sln");
        Assert.Equal(Path.Combine(vFolder, "Todo", "Todo.sln"), vSolutionOnly.SolutionFile);
        Assert.Equal(Path.Combine(vFolder, "Todo", "Todo"), vSolutionOnly.ProjectFolder);

        var vBoth = objSut.Plan(new NewProjectRequest(vFolder, null, "Todo", "Todo.App", "console", null), ".slnx");
        Assert.Equal(Path.Combine(vFolder, "Todo", "Todo.slnx"), vBoth.SolutionFile);
        Assert.Equal(Path.Combine(vFolder, "Todo", "Todo.App"), vBoth.ProjectFolder);
        Assert.Equal(Path.Combine(vFolder, "Todo", "Todo.App", "Todo.App.csproj"), vBoth.ProjectFile);
        Assert.Equal(Path.Combine(vFolder, "Todo"), vBoth.Folder);

        var vNowhere = objSut.Plan(new NewProjectRequest(string.Empty, null, "Todo", string.Empty, "console", null), ".slnx");
        Assert.NotNull(vNowhere.Problem);

        Assert.Empty(Disk(vFolder));
        Assert.Empty(objLauncher.Runs);
    }

    /// <summary>When dotnet new fails, then the error says what it printed, the solution Chatur had just made is taken away again, and nothing is opened.</summary>
    [Fact(DisplayName = "REQ-FN-310 a failed dotnet new is reported and leaves no half-made solution behind")]
    public async Task FailedNewIsReportedAndNothingIsLeft()
    {
        var vFolder = Directory.CreateDirectory(Path.Combine(objRoot, "Fails")).FullName;
        objLauncher.FailOn = "new nope";
        objLauncher.Output = "Template \"nope\" could not be found.";

        var vError = await Assert.ThrowsAsync<InvalidOperationException>(() => objSut.CreateAsync(new NewProjectRequest(vFolder, null, "Todo", string.Empty, "nope", null)));

        Assert.Contains("could not be found", vError.Message);
        Assert.Empty(Disk(vFolder));
        Assert.Null(objOpener.Opened);
        using var vConnection = objConnections.OpenConnection();
        Assert.Equal(1, vConnection.ExecuteScalar<int>("SELECT COUNT(*) FROM RunTarget WHERE ProjectId = @ProjectId;", new { ProjectId = objProjectId }));
    }

    /// <summary>When a name has a slash, then nothing runs and nothing is made.</summary>
    [Fact(DisplayName = "REQ-UI-112 a name that cannot be a folder name is refused before anything runs")]
    public async Task BadNameIsRefusedBeforeAnythingRuns()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => objSut.CreateAsync(new NewProjectRequest(string.Empty, objSolution, string.Empty, "a/b", "console", null)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => objSut.CreateAsync(new NewProjectRequest(Path.Combine(objRoot, "New"), null, "a:b", string.Empty, "console", null)));

        Assert.Empty(objLauncher.Commands);
        Assert.False(Directory.Exists(Path.Combine(objRoot, "New")));
    }

    /// <summary>When the project's folder already holds files, then it is refused, nothing runs, and the person's file is as it was.</summary>
    [Fact(DisplayName = "REQ-FN-310 a project folder that already holds files is refused and nothing of the person's is touched")]
    public async Task NonEmptyProjectFolderIsRefused()
    {
        var vTaken = Directory.CreateDirectory(Path.Combine(objRoot, "Taken")).FullName;
        await File.WriteAllTextAsync(Path.Combine(vTaken, "keep.txt"), "mine");

        var vError = await Assert.ThrowsAsync<InvalidOperationException>(() => objSut.CreateAsync(new NewProjectRequest(string.Empty, objSolution, string.Empty, "Taken", "console", null)));

        Assert.Contains("already exists and is not empty", vError.Message, StringComparison.Ordinal);
        Assert.Empty(objLauncher.Commands);
        Assert.Equal("mine", File.ReadAllText(Path.Combine(vTaken, "keep.txt")));
    }

    /// <summary>The solution file's extension follows the SDK: .slnx from .NET 10, .sln before it or when the version cannot be read.</summary>
    [Fact(DisplayName = "REQ-UI-126 the solution file's extension shown follows the machine's .NET version")]
    public void SolutionExtensionFollowsTheSdk()
    {
        Assert.Equal(".slnx", DotnetTemplateOutput.SolutionExtensionFor("10.0.100\n"));
        Assert.Equal(".slnx", DotnetTemplateOutput.SolutionExtensionFor("11.0.100-preview.1"));
        Assert.Equal(".sln", DotnetTemplateOutput.SolutionExtensionFor("9.0.305"));
        Assert.Equal(".sln", DotnetTemplateOutput.SolutionExtensionFor("not a version"));
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

    /// <summary>REQ-UI-112: the templates are read with nothing open, in the person's own folder, so the dialog lists them in every state.</summary>
    [Fact(DisplayName = "REQ-UI-112 the templates are read with nothing open")]
    public async Task TemplatesAreReadWithNothingOpen()
    {
        objLauncher.Output = "Template Name   Short Name  Language  Tags\n--------------  ----------  --------  ------\nConsole App     console     [C#],F#   Common/Console\n";

        IReadOnlyList<ProjectTemplate>? vTemplates;
        try
        {
            vTemplates = await objSut.TemplatesAsync(null);
        }
        catch (InvalidOperationException)
        {
            vTemplates = null;
        }

        // The list is cached per process, so an earlier test may already hold it; either way no solution was needed.
        Assert.True(vTemplates is null || vTemplates.Count > 0);
        Assert.All(objLauncher.Runs, r => Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), r.Directory));
    }

    private sealed class FakeOpener : IProjectOpenActions
    {
        public string? Opened { get; private set; }

        public Task<ProjectSummary> OpenSolutionAsync(string aPath, CancellationToken aCt = default)
        {
            Opened = aPath;
            return Task.FromResult(new ProjectSummary(99, Path.GetFileNameWithoutExtension(aPath), aPath, "Solution", null));
        }

        public Task<ProjectSummary> OpenFolderAsync(string aPath, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task<ProjectFileOpened> OpenFileAsync(string aPath, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task<ProjectSummary> CreateSolutionAsync(string aParentFolder, string aName, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task<ProjectSummary> CloneAsync(string aAddress, string aParentFolder, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task CloseProjectAsync(CancellationToken aCt = default) => Task.CompletedTask;
    }

    /// <summary>
    /// Stands in for <c>dotnet</c>: records every run and makes the files the real command would (<c>--version</c>
    /// prints a .NET 10 version, <c>new sln</c> writes the .slnx, <c>new &lt;template&gt;</c> the project folder and file).
    /// </summary>
    private sealed class FakeLauncher : IProcessLauncher
    {
        public List<(string Arguments, string Directory)> Runs { get; } = [];

        /// <summary>The commands run, without the one-off version read (cached per process, so it may or may not be there).</summary>
        public List<string> Commands => Runs.Select(r => r.Arguments).Where(a => a != "--version").ToList();

        /// <summary>The start of the command that fails with exit code 1; <see langword="null"/> when every command works.</summary>
        public string? FailOn { get; set; }

        public string Output { get; set; } = string.Empty;

        public Task OpenWithDefaultAppAsync(string aFilePath, CancellationToken aCt = default) => Task.CompletedTask;

        public Task<int> RunAsync(string aCommand, string aArguments, string aWorkingDirectory, Action<string> aOnOutputLine, CancellationToken aCt = default)
        {
            Assert.Equal("dotnet", aCommand);
            Runs.Add((aArguments, aWorkingDirectory));
            if (aArguments == "--version")
            {
                aOnOutputLine("10.0.100");
                return Task.FromResult(0);
            }

            if (Output.Length > 0)
            {
                aOnOutputLine(Output);
            }

            if (FailOn is not null && aArguments.StartsWith(FailOn, StringComparison.Ordinal))
            {
                return Task.FromResult(1);
            }

            var vQuoted = System.Text.RegularExpressions.Regex.Matches(aArguments, "\"([^\"]*)\"").Select(m => m.Groups[1].Value).ToArray();
            if (aArguments.StartsWith("new sln", StringComparison.Ordinal))
            {
                File.WriteAllText(Path.Combine(vQuoted[1], vQuoted[0] + ".slnx"), "<Solution />");
            }
            else if (aArguments.StartsWith("new ", StringComparison.Ordinal) && vQuoted.Length >= 2)
            {
                Directory.CreateDirectory(vQuoted[1]);
                File.WriteAllText(Path.Combine(vQuoted[1], vQuoted[0] + ".csproj"), "<Project />");
            }

            return Task.FromResult(0);
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
}
