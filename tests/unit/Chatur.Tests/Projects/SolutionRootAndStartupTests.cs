using Chatur.Core;
using Dapper;
using Chatur.Core.Actions;
using Chatur.Core.BuildRun;
using Chatur.Core.Files;
using Chatur.Core.Platform;
using Chatur.Core.Projects;
using Chatur.Tests.Support;
using Xunit;

namespace Chatur.Tests.Projects;

/// <summary>
/// The owner's test of 25 Nov 2026: the solution is the root of the Files tree with its projects beneath it, a project
/// just added is the one that runs, several projects can start together, and a folder with projects and no solution is
/// looked at before it opens. Each test is one of those sentences.
/// </summary>
public sealed class SolutionRootAndStartupTests : MigratedDatabaseFixture
{
    private const string ConsoleProject = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>";
    private const string LibraryProject = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>";

    private readonly string objRoot = Path.Combine(TestScratch.Root, $"chatur-tests-solroot-{Guid.NewGuid():N}");

    /// <summary>Creates the temporary folder.</summary>
    public SolutionRootAndStartupTests()
    {
        Directory.CreateDirectory(objRoot);
    }

    private string AddProject(string aName, string aText)
    {
        Directory.CreateDirectory(Path.Combine(objRoot, aName));
        var vFile = Path.Combine(objRoot, aName, aName + ".csproj");
        File.WriteAllText(vFile, aText);
        return vFile;
    }

    private string WriteSolution(string aName, params string[] aProjectNames)
    {
        var vLines = aProjectNames.Select(n => $"  <Project Path=\"{n}/{n}.csproj\" />");
        var vFile = Path.Combine(objRoot, aName + ".slnx");
        File.WriteAllText(vFile, "<Solution>\n" + string.Join("\n", vLines) + "\n</Solution>");
        return vFile;
    }

    /// <summary>When a project has a solution, then the tree has one root, the solution, with its projects first and the solution file not beside them (REQ-UI-125).</summary>
    [Fact(DisplayName = "REQ-UI-125 the solution is the root of the tree and its projects come first")]
    public async Task SolutionIsTheRootOfTheTree()
    {
        AddProject("Web", ConsoleProject);
        AddProject("Api", ConsoleProject);
        Directory.CreateDirectory(Path.Combine(objRoot, "notes"));
        File.WriteAllText(Path.Combine(objRoot, "readme.md"), "hi");
        var vSolution = WriteSolution("Shop", "Web", "Api");
        var vProjectId = CreateProject("Shop", vSolution);

        var vTree = await new FileActions(CreateFactory()).TreeAsync(vProjectId);

        var vRoot = Assert.Single(vTree);
        Assert.True(vRoot.IsSolution);
        Assert.Equal("Shop", vRoot.Name);
        Assert.Equal(["Web", "Api", "notes", "readme.md"], vRoot.Children.Select(c => c.Name).ToArray());
        Assert.Equal([true, true, false, false], vRoot.Children.Select(c => c.IsProject).ToArray());
        Assert.DoesNotContain(vRoot.Children, c => c.Name.EndsWith(".slnx", StringComparison.Ordinal));
    }

    /// <summary>When a folder was opened first and its solution was made afterwards, then the tree shows the solution as its root too (REQ-UI-125).</summary>
    [Fact(DisplayName = "REQ-UI-125 a folder opened before its solution was made shows the solution as the root")]
    public async Task FolderOpenedFirstGetsTheSolutionRootLater()
    {
        AddProject("Tool", ConsoleProject);
        var vProjectId = CreateProject("Tool", objRoot);
        var vFiles = new FileActions(CreateFactory());

        var vBefore = await vFiles.TreeAsync(vProjectId);
        Assert.Equal(["Tool"], vBefore.Select(n => n.Name).ToArray());
        Assert.DoesNotContain(vBefore, n => n.IsSolution);

        WriteSolution("Later", "Tool");
        var vAfter = await vFiles.TreeAsync(vProjectId);

        var vRoot = Assert.Single(vAfter);
        Assert.True(vRoot.IsSolution);
        Assert.Equal("Later", vRoot.Name);
        Assert.True(Assert.Single(vRoot.Children).IsProject);
    }

    /// <summary>When a project just added can run, then it is the one that runs; a library has no target and changes nothing (REQ-FN-309, REQ-UI-106).</summary>
    [Fact(DisplayName = "REQ-FN-309 a project just added becomes the project that runs")]
    public async Task ProjectJustAddedIsTheOneThatRuns()
    {
        var vFirst = AddProject("First", ConsoleProject);
        var vLibrary = AddProject("Lib", LibraryProject);
        var vSolution = WriteSolution("Pair", "First", "Lib");
        var vProjectId = CreateProject("Pair", vSolution);
        var vActions = new BuildRunActions(CreateFactory(), new NoLauncher());
        Assert.Equal("First", (await vActions.TargetsAsync(vProjectId))[0].Name);

        var vSecond = AddProject("Second", ConsoleProject);
        WriteSolution("Pair", "First", "Lib", "Second");
        Assert.True(await vActions.SetRunTargetForProjectFileAsync(vProjectId, vSecond));
        Assert.Equal("Second", (await vActions.TargetsAsync(vProjectId))[0].Name);

        Assert.False(await vActions.SetRunTargetForProjectFileAsync(vProjectId, vLibrary));
        Assert.Equal("Second", (await vActions.TargetsAsync(vProjectId))[0].Name);
        Assert.NotNull(vFirst);
    }

    /// <summary>When the project just added is a web project (the owner's Blazor Web App), then it is the one that runs, ahead of a console project that ran before (REQ-FN-309).</summary>
    [Fact(DisplayName = "REQ-FN-309 a web project just added becomes the project that runs")]
    public async Task WebProjectJustAddedIsTheOneThatRuns()
    {
        AddProject("TryConsole", ConsoleProject);
        var vSolution = WriteSolution("FromNothing", "TryConsole");
        var vProjectId = CreateProject("FromNothing", vSolution);
        var vActions = new BuildRunActions(CreateFactory(), new NoLauncher());
        var vFirst = (await vActions.TargetsAsync(vProjectId))[0];
        await vActions.SetRunTargetAsync(vProjectId, vFirst.RunTargetId);

        // New project forgets the stored targets, adds the project to the solution, then asks for it to run.
        var vWeb = AddProject("TryWeb", "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable></PropertyGroup></Project>");
        WriteSolution("FromNothing", "TryConsole", "TryWeb");
        using (var vConnection = CreateFactory().OpenConnection())
        {
            vConnection.Execute("DELETE FROM RunTarget WHERE ProjectId = @vProjectId", new { vProjectId });
        }

        Assert.True(await vActions.SetRunTargetForProjectFileAsync(vProjectId, vWeb));
        Assert.Equal(["TryWeb", "TryConsole"], (await vActions.TargetsAsync(vProjectId)).Select(t => t.Name).ToArray());
    }

    /// <summary>When a library is added after a web project was added, then the web project is still the one that runs, since the stored targets are kept (the owner's Blazor, class library and test project in a row, 25 Nov 2026).</summary>
    [Fact(DisplayName = "REQ-FN-309 adding a library afterwards keeps the project that runs")]
    public async Task AddingALibraryKeepsTheProjectThatRuns()
    {
        AddProject("TryConsole", ConsoleProject);
        var vSolution = WriteSolution("FromNothing", "TryConsole");
        var vProjectId = CreateProject("FromNothing", vSolution);
        var vActions = new BuildRunActions(CreateFactory(), new NoLauncher());
        await vActions.TargetsAsync(vProjectId);

        var vWeb = AddProject("TryWeb", "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        WriteSolution("FromNothing", "TryConsole", "TryWeb");
        Assert.True(await vActions.SetRunTargetForProjectFileAsync(vProjectId, vWeb));

        var vLib = AddProject("TryLib", LibraryProject);
        WriteSolution("FromNothing", "TryConsole", "TryWeb", "TryLib");
        Assert.False(await vActions.SetRunTargetForProjectFileAsync(vProjectId, vLib));

        Assert.Equal("TryWeb", (await vActions.TargetsAsync(vProjectId))[0].Name);
    }

    /// <summary>When projects are chosen to start together, then the choice is kept for the solution, survives targets read again, and choosing one project ends it (REQ-FN-324).</summary>
    [Fact(DisplayName = "REQ-FN-324 the projects chosen to start together are kept for the solution")]
    public async Task StartupChoiceIsKeptPerSolution()
    {
        AddProject("Api", ConsoleProject);
        AddProject("Web", ConsoleProject);
        AddProject("Worker", ConsoleProject);
        var vSolution = WriteSolution("Shop", "Api", "Web", "Worker");
        var vProjectId = CreateProject("Shop", vSolution);
        var vOther = CreateProject("Other", Path.Combine(objRoot, "Other.slnx"));
        var vActions = new BuildRunActions(CreateFactory(), new NoLauncher());
        await vActions.TargetsAsync(vProjectId);

        await vActions.SetStartupAsync(vProjectId, ["Api/Api.csproj", "Web/Web.csproj"]);

        Assert.Equal(["Api", "Web"], (await vActions.StartupAsync(vProjectId)).Select(t => t.Name).ToArray());
        Assert.Empty(await vActions.StartupAsync(vOther));

        // The targets are read again under new ids (a project was added): the choice follows the project files.
        using (var vConnection = CreateFactory().OpenConnection())
        {
            vConnection.Execute("DELETE FROM RunTarget WHERE ProjectId = @vProjectId", new { vProjectId });
        }

        Assert.Equal(["Api", "Web"], (await vActions.StartupAsync(vProjectId)).Select(t => t.Name).ToArray());

        var vWorker = (await vActions.TargetsAsync(vProjectId)).Single(t => t.Name == "Worker");
        await vActions.SetRunTargetAsync(vProjectId, vWorker.RunTargetId);

        Assert.Empty(await vActions.StartupAsync(vProjectId));
    }

    /// <summary>When Run is pressed with two startup projects, then both are built, both start, each program's lines carry its name, and one line says when all have ended (REQ-FN-324).</summary>
    [Fact(DisplayName = "REQ-FN-324 Run builds and starts every startup project together and names each one's lines")]
    public async Task RunStartsEveryStartupProjectTogether()
    {
        AddProject("Api", ConsoleProject);
        AddProject("Web", ConsoleProject);
        var vSolution = WriteSolution("Shop", "Api", "Web");
        var vProjectId = CreateProject("Shop", vSolution);
        var vLauncher = new ScriptedLauncher();
        var vActions = new BuildRunActions(CreateFactory(), vLauncher);
        var vTargets = await vActions.TargetsAsync(vProjectId);
        await vActions.SetStartupAsync(vProjectId, ["Api/Api.csproj", "Web/Web.csproj"]);

        await vActions.RunAsync(vProjectId, vTargets[0].RunTargetId);

        var vLines = new List<OutputLine>();
        using var vCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await foreach (var vLine in vActions.StreamOutputAsync(vProjectId, vCts.Token))
        {
            vLines.Add(vLine);
            if (vLine.Text.StartsWith("All 2 projects ended", StringComparison.Ordinal))
            {
                break;
            }
        }

        Assert.Equal(2, vLauncher.Commands.Count(c => c.StartsWith("build", StringComparison.Ordinal)));
        Assert.Equal(2, vLauncher.Commands.Count(c => c.StartsWith("run", StringComparison.Ordinal)));
        Assert.Contains(vLines, l => l.Text == "[Api] says hello");
        Assert.Contains(vLines, l => l.Text == "[Web] says hello");
        Assert.Contains(vLines, l => l.Text == "[Api] Process exited with code 0.");
        Assert.Contains(vLines, l => l.Text == "[Web] Process exited with code 0.");
        Assert.DoesNotContain(vLines, l => l.Text.StartsWith("Process exited", StringComparison.Ordinal));
    }

    /// <summary>When one startup project does not build, then none of them is started (REQ-FN-324).</summary>
    [Fact(DisplayName = "REQ-FN-324 a startup project that does not build stops all of them from starting")]
    public async Task FailedBuildStartsNoStartupProject()
    {
        AddProject("Api", ConsoleProject);
        AddProject("Web", ConsoleProject);
        var vSolution = WriteSolution("Shop", "Api", "Web");
        var vProjectId = CreateProject("Shop", vSolution);
        var vLauncher = new ScriptedLauncher { BuildExit = 1 };
        var vActions = new BuildRunActions(CreateFactory(), vLauncher);
        var vTargets = await vActions.TargetsAsync(vProjectId);
        await vActions.SetStartupAsync(vProjectId, ["Api/Api.csproj", "Web/Web.csproj"]);

        await vActions.RunAsync(vProjectId, vTargets[0].RunTargetId);

        var vLines = new List<OutputLine>();
        using var vCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await foreach (var vLine in vActions.StreamOutputAsync(vProjectId, vCts.Token))
        {
            vLines.Add(vLine);
            if (vLine.Text.StartsWith("The programs were not started", StringComparison.Ordinal))
            {
                break;
            }
        }

        Assert.DoesNotContain(vLauncher.Commands, c => c.StartsWith("run", StringComparison.Ordinal));
        Assert.Contains(vLines, l => l.IsError && l.Text.StartsWith("Build FAILED", StringComparison.Ordinal));
    }

    /// <summary>When a folder holds a project and no solution, then looking at it says so and names the project; a folder with a solution, or with nothing, is not asked about (REQ-FN-325).</summary>
    [Fact(DisplayName = "REQ-FN-325 a folder with a project and no solution is looked at before it opens")]
    public async Task FolderWithoutSolutionIsCheckedFirst()
    {
        var vSut = new ProjectOpenActions(CreateFactory(), null!, new NoLauncher(), new AppState());
        var vEmpty = Directory.CreateDirectory(Path.Combine(objRoot, "empty")).FullName;
        var vNothing = await vSut.CheckFolderAsync(vEmpty);
        Assert.False(vNothing.HasSolution);
        Assert.Empty(vNothing.Projects);

        AddProject("Loose", ConsoleProject);
        var vLoose = await vSut.CheckFolderAsync(objRoot);
        Assert.False(vLoose.HasSolution);
        Assert.Equal([Path.Combine("Loose", "Loose.csproj")], vLoose.Projects);

        WriteSolution("Gathered", "Loose");
        Assert.True((await vSut.CheckFolderAsync(objRoot)).HasSolution);
        await Assert.ThrowsAsync<InvalidOperationException>(() => vSut.CheckFolderAsync(Path.Combine(objRoot, "missing")));
    }

    /// <summary>Removes the temporary folder.</summary>
    public override void Dispose()
    {
        base.Dispose();
        if (Directory.Exists(objRoot))
        {
            Directory.Delete(objRoot, true);
        }
    }

    private sealed class NoLauncher : IProcessLauncher
    {
        public Task OpenWithDefaultAppAsync(string aFilePath, CancellationToken aCt = default) => Task.CompletedTask;

        public Task<int> RunAsync(string aCommand, string aArguments, string aWorkingDirectory, Action<string> aOnOutputLine, CancellationToken aCt = default) => Task.FromResult(0);
    }

    private sealed class ScriptedLauncher : IProcessLauncher
    {
        public List<string> Commands { get; } = [];

        public int BuildExit { get; init; }

        public Task OpenWithDefaultAppAsync(string aFilePath, CancellationToken aCt = default) => Task.CompletedTask;

        public Task<int> RunAsync(string aCommand, string aArguments, string aWorkingDirectory, Action<string> aOnOutputLine, CancellationToken aCt = default)
        {
            lock (Commands)
            {
                Commands.Add(aArguments);
            }

            if (aArguments.StartsWith("build", StringComparison.Ordinal))
            {
                return Task.FromResult(BuildExit);
            }

            aOnOutputLine("says hello");
            return Task.FromResult(0);
        }
    }
}
