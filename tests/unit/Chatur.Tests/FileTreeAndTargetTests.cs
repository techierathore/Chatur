using System.Diagnostics;
using Chatur.Core.BuildRun;
using Chatur.Core.Files;
using Chatur.Core.Platform;
using Chatur.Core.Projects;
using Chatur.Core;
using Chatur.Tests.Support;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// The owner's run of 16 Nov on his own solution: Open a file stopped Chatur on a folder it could not
/// read, the tree read the whole folder at once, and a solution offered no run target. Each test was
/// written to fail on the code of that night and pass after the fix (triage-and-fix, 18 Nov 2026).
/// </summary>
public sealed class FileTreeAndTargetTests : MigratedDatabaseFixture
{
    private readonly string objRoot = Path.Combine(Chatur.Tests.Support.TestScratch.Root, $"chatur-tests-tree-{Guid.NewGuid():N}");
    private readonly List<string> objLocked = [];

    /// <summary>Creates the temporary project folder.</summary>
    public FileTreeAndTargetTests()
    {
        Directory.CreateDirectory(objRoot);
    }

    /// <summary>
    /// When the opened folder holds a folder Windows will not list (as Documents\My Music does), then
    /// reading the tree does not stop: the other entries come back and the locked folder is still shown.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-039 a folder that cannot be read does not stop the tree")]
    public async Task UnreadableFolderDoesNotStopTheTree()
    {
        Directory.CreateDirectory(Path.Combine(objRoot, "open"));
        File.WriteAllText(Path.Combine(objRoot, "open", "a.txt"), "a");
        File.WriteAllText(Path.Combine(objRoot, "root.txt"), "r");
        var vLocked = Path.Combine(objRoot, "locked");
        Directory.CreateDirectory(vLocked);
        File.WriteAllText(Path.Combine(vLocked, "secret.txt"), "s");
        Lock(vLocked);
        var vProjectId = CreateProject("Tree", objRoot);

        var vTree = await new FileActions(CreateFactory(), new NoLauncher()).TreeAsync(vProjectId);

        var vNames = vTree.Select(aNode => aNode.Name).ToList();
        Assert.Contains("locked", vNames);
        Assert.Contains("open", vNames);
        Assert.Contains("root.txt", vNames);
    }

    /// <summary>
    /// When a project opens, then the tree reads only its top level: a folder's contents are read when
    /// that folder is opened, never the whole tree at once.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-039 the tree reads a folder only when it is opened")]
    public async Task TreeReadsOnlyTheTopLevelAtFirst()
    {
        Directory.CreateDirectory(Path.Combine(objRoot, "outer", "inner"));
        File.WriteAllText(Path.Combine(objRoot, "outer", "inner", "deep.txt"), "d");
        var vProjectId = CreateProject("Tree", objRoot);

        var vTree = await new FileActions(CreateFactory(), new NoLauncher()).TreeAsync(vProjectId);

        var vOuter = Assert.Single(vTree, aNode => aNode.Name == "outer");
        Assert.True(vOuter.IsFolder);
        Assert.Empty(vOuter.Children);
    }

    /// <summary>
    /// When a solution is opened (its project row holds the .sln file's path), then the run target picker
    /// lists the solution's runnable projects — a program and a web project — and its test project, whose run
    /// runs its tests (owner, 21 Nov 2026: "a class library with tests"), but never the class library itself.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-011 a solution offers its runnable projects as targets")]
    public async Task SolutionOffersItsRunnableProjects()
    {
        var vSolution = WriteSolution();
        var vProjectId = CreateProject("Shop", vSolution);

        var vTargets = await new BuildRunActions(CreateFactory(), new NoLauncher()).TargetsAsync(vProjectId);

        var vNames = vTargets.Select(aTarget => aTarget.Name).ToList();
        Assert.Contains(vNames, aName => aName.StartsWith("Shop.Api", StringComparison.Ordinal));
        Assert.Contains(vNames, aName => aName.StartsWith("Shop.Tool", StringComparison.Ordinal));
        Assert.DoesNotContain(vNames, aName => aName.StartsWith("Shop.Models", StringComparison.Ordinal));
        Assert.Contains(vNames, aName => aName.StartsWith("Shop.Tests", StringComparison.Ordinal));
    }

    /// <summary>
    /// When a folder cannot be read, then its row says so (not readable), and opening it gives nothing
    /// rather than an error (BRD-217).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-096 a folder that cannot be read is marked not readable and opens to nothing")]
    public async Task UnreadableFolderIsMarkedAndOpensToNothing()
    {
        var vLocked = Path.Combine(objRoot, "locked");
        Directory.CreateDirectory(vLocked);
        Directory.CreateDirectory(Path.Combine(objRoot, "open"));
        Lock(vLocked);
        var vProjectId = CreateProject("Tree", objRoot);
        var vFiles = new FileActions(CreateFactory(), new NoLauncher());

        var vTree = await vFiles.TreeAsync(vProjectId);

        Assert.False(Assert.Single(vTree, aNode => aNode.Name == "locked").IsReadable);
        Assert.True(Assert.Single(vTree, aNode => aNode.Name == "open").IsReadable);
        Assert.Empty(await vFiles.ChildrenAsync(vProjectId, "locked"));
    }

    /// <summary>When a folder is opened, then its own entries come back, and only one level of them (BRD-217).</summary>
    [Fact(DisplayName = "REQ-UI-096 opening a folder reads its entries one level deep")]
    public async Task OpeningAFolderReadsOneLevel()
    {
        Directory.CreateDirectory(Path.Combine(objRoot, "outer", "inner"));
        File.WriteAllText(Path.Combine(objRoot, "outer", "inner", "deep.txt"), "d");
        File.WriteAllText(Path.Combine(objRoot, "outer", "near.txt"), "n");
        var vProjectId = CreateProject("Tree", objRoot);

        var vChildren = await new FileActions(CreateFactory(), new NoLauncher()).ChildrenAsync(vProjectId, "outer");

        Assert.Equal(["inner", "near.txt"], vChildren.Select(aNode => aNode.Name));
        Assert.Equal("outer/inner", vChildren[0].RelativePath);
        Assert.Empty(vChildren[0].Children);
    }

    /// <summary>
    /// When a project opens, then names starting with a dot and .vs are not listed; when the account turns
    /// on Show hidden files, they are, and the choice is kept (BRD-225).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-103 hidden files stay hidden until the account shows them")]
    public async Task HiddenFilesStayHiddenUntilShown()
    {
        Directory.CreateDirectory(Path.Combine(objRoot, ".github"));
        Directory.CreateDirectory(Path.Combine(objRoot, ".vs"));
        Directory.CreateDirectory(Path.Combine(objRoot, "src"));
        File.WriteAllText(Path.Combine(objRoot, ".editorconfig"), "x");
        File.WriteAllText(Path.Combine(objRoot, "readme.md"), "x");
        var vProjectId = CreateProject("Tree", objRoot);
        var vFiles = new FileActions(CreateFactory(), new NoLauncher());

        Assert.Equal(["src", "readme.md"], (await vFiles.TreeAsync(vProjectId)).Select(aNode => aNode.Name));

        await vFiles.SetShowHiddenAsync(true);

        Assert.True(await new FileActions(CreateFactory(), new NoLauncher()).ShowHiddenAsync());
        Assert.Equal([".github", ".vs", "src", ".editorconfig", "readme.md"], (await vFiles.TreeAsync(vProjectId)).Select(aNode => aNode.Name));
    }

    /// <summary>
    /// When the last project's folder cannot be read as Chatur starts, then reopening gives no project and
    /// one plain line naming it and why, every time Start asks (BRD-218).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-097 a last project that cannot be read is not reopened and the reason is given")]
    public async Task UnreadableLastProjectIsNotReopened()
    {
        var vProjectId = CreateProject("Xpenser", objRoot);
        var vState = new AppState();
        var vProjects = new ProjectActions(CreateFactory(), new SystemClock(), vState);
        await vProjects.SelectProjectAsync(vProjectId);
        Lock(objRoot);

        var vReopen = await vProjects.ReopenAsync();

        Assert.Null(vReopen.Project);
        Assert.Contains("Xpenser", vReopen.Problem);
        Assert.Contains("not allowed to read", vReopen.Problem);
        Assert.Null(vState.SelectedProject);

        // A second pass over Start (a web page is drawn twice) still says why.
        Assert.Contains("Xpenser", (await vProjects.ReopenAsync()).Problem);
    }

    /// <summary>When the last project's folder is there and readable, then it is reopened with no problem line (REQ-FN-006).</summary>
    [Fact(DisplayName = "REQ-FN-006 a readable last project is reopened")]
    public async Task ReadableLastProjectIsReopened()
    {
        var vProjectId = CreateProject("Xpenser", objRoot);
        var vProjects = new ProjectActions(CreateFactory(), new SystemClock(), new AppState());
        await vProjects.SelectProjectAsync(vProjectId);

        var vReopen = await vProjects.ReopenAsync();

        Assert.Equal(vProjectId, vReopen.Project?.ProjectId);
        Assert.Null(vReopen.Problem);
    }

    /// <summary>
    /// When the person sets a project to run, then the next time the account opens that solution it is
    /// the one offered first (BRD-228).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-106 the project set to run is remembered for the solution")]
    public async Task ProjectSetToRunIsRemembered()
    {
        var vProjectId = CreateProject("Shop", WriteSolution());
        var vTargets = await new BuildRunActions(CreateFactory(), new NoLauncher()).TargetsAsync(vProjectId);
        var vLast = vTargets.Last(aTarget => aTarget.ProjectFile == "Shop.Tool/Shop.Tool.csproj");

        await new BuildRunActions(CreateFactory(), new NoLauncher()).SetRunTargetAsync(vProjectId, vLast.RunTargetId);

        var vAfter = await new BuildRunActions(CreateFactory(), new NoLauncher()).TargetsAsync(vProjectId);
        Assert.Equal(vLast.RunTargetId, vAfter[0].RunTargetId);
        Assert.Equal("Shop.Tool/Shop.Tool.csproj", vAfter[0].ProjectFile);
    }

    /// <summary>
    /// When a branch switch moves the solution's projects (Xpenser's saurabhdev keeps them under Source/),
    /// then the targets are read again from the solution, and the project set to run stays the one set.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-106 a project that moved on disk is found again and stays the one to run")]
    public async Task MovedProjectIsFoundAgainAndStaysChosen()
    {
        var vSolution = WriteSolution();
        var vProjectId = CreateProject("Shop", vSolution);
        var vTool = (await new BuildRunActions(CreateFactory(), new NoLauncher()).TargetsAsync(vProjectId)).Single(aTarget => aTarget.Name == "Shop.Tool");
        await new BuildRunActions(CreateFactory(), new NoLauncher()).SetRunTargetAsync(vProjectId, vTool.RunTargetId);

        Directory.CreateDirectory(Path.Combine(objRoot, "Source"));
        Directory.Move(Path.Combine(objRoot, "Shop.Tool"), Path.Combine(objRoot, "Source", "Shop.Tool"));
        File.WriteAllText(vSolution, File.ReadAllText(vSolution).Replace("\"Shop.Tool\\Shop.Tool.csproj\"", "\"Source\\Shop.Tool\\Shop.Tool.csproj\""));

        var vAfter = await new BuildRunActions(CreateFactory(), new NoLauncher()).TargetsAsync(vProjectId);

        Assert.Equal("Shop.Tool", vAfter[0].Name);
        Assert.Equal("Source/Shop.Tool/Shop.Tool.csproj", vAfter[0].ProjectFile);
    }

    /// <summary>When the solution is the newer XML .slnx, then its runnable projects are offered the same way (BRD-227).</summary>
    [Fact(DisplayName = "REQ-UI-105 an .slnx solution offers its runnable projects")]
    public async Task SlnxSolutionOffersItsRunnableProjects()
    {
        WriteSolution();
        var vSlnx = Path.Combine(objRoot, "Shop.slnx");
        File.WriteAllText(vSlnx, "<Solution><Project Path=\"Shop.Api/Shop.Api.csproj\" /><Project Path=\"Shop.Models/Shop.Models.csproj\" /></Solution>");
        var vProjectId = CreateProject("Shop", vSlnx);

        var vTargets = await new BuildRunActions(CreateFactory(), new NoLauncher()).TargetsAsync(vProjectId);

        Assert.Equal(["Shop.Api"], vTargets.Select(aTarget => aTarget.Name));
    }

    /// <summary>When a solution's run target is built, then the build runs on the project's own file inside the solution's folder (BRD-227).</summary>
    [Fact(DisplayName = "REQ-UI-105 running a solution's project builds that project in the solution's folder")]
    public async Task RunningASolutionProjectUsesItsOwnFile()
    {
        var vSolution = WriteSolution();
        var vProjectId = CreateProject("Shop", vSolution);
        var vLauncher = new NoLauncher();
        var vActions = new BuildRunActions(CreateFactory(), vLauncher);
        var vApi = (await vActions.TargetsAsync(vProjectId)).Single(aTarget => aTarget.Name == "Shop.Api");

        await vActions.BuildAsync(vProjectId, vApi.RunTargetId);

        Assert.Equal(objRoot, vLauncher.WorkingDirectory);
        Assert.Contains(Path.Combine(objRoot, "Shop.Api", "Shop.Api.csproj"), vLauncher.Arguments);
    }

    /// <summary>Writes a small solution shaped like the owner's: a web API, a console tool, a library and a test project.</summary>
    private string WriteSolution()
    {
        WriteProject("Shop.Api", "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        WriteProject("Shop.Tool", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        WriteProject("Shop.Models", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        WriteProject("Shop.Tests", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include=\"Microsoft.NET.Test.Sdk\" Version=\"17.*\" /></ItemGroup></Project>");
        var vLines = new List<string> { "Microsoft Visual Studio Solution File, Format Version 12.00" };
        foreach (var vName in new[] { "Shop.Api", "Shop.Tool", "Shop.Models", "Shop.Tests" })
        {
            vLines.Add($"Project(\"{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}\") = \"{vName}\", \"{vName}\\{vName}.csproj\", \"{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}\"");
            vLines.Add("EndProject");
        }

        var vPath = Path.Combine(objRoot, "Shop.sln");
        File.WriteAllLines(vPath, vLines);
        return vPath;
    }

    private void WriteProject(string aName, string aText)
    {
        Directory.CreateDirectory(Path.Combine(objRoot, aName));
        File.WriteAllText(Path.Combine(objRoot, aName, aName + ".csproj"), aText);
    }

    /// <summary>
    /// Makes a folder that cannot be listed, as Windows makes Documents\My Music: a deny entry for
    /// everyone on Windows, no permissions at all elsewhere. Fails the test when the lock did not take.
    /// </summary>
    private void Lock(string aFolder)
    {
        if (OperatingSystem.IsWindows())
        {
            RunIcacls($"\"{aFolder}\" /deny *S-1-1-0:(RD)");
        }
        else
        {
            File.SetUnixFileMode(aFolder, UnixFileMode.None);
        }

        objLocked.Add(aFolder);
        Assert.ThrowsAny<UnauthorizedAccessException>(() => Directory.EnumerateFileSystemEntries(aFolder).ToList());
    }

    private static void Unlock(string aFolder)
    {
        if (OperatingSystem.IsWindows())
        {
            RunIcacls($"\"{aFolder}\" /remove:d *S-1-1-0");
        }
        else
        {
            File.SetUnixFileMode(aFolder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private static void RunIcacls(string aArguments)
    {
        using var vProcess = Process.Start(new ProcessStartInfo("icacls", aArguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        })!;
        vProcess.WaitForExit();
    }

    /// <summary>Unlocks and deletes the temporary folder as well as the database.</summary>
    public override void Dispose()
    {
        foreach (var vFolder in objLocked)
        {
            Unlock(vFolder);
        }

        if (Directory.Exists(objRoot))
        {
            Directory.Delete(objRoot, true);
        }

        base.Dispose();
    }

    private sealed class NoLauncher : IProcessLauncher
    {
        public string? WorkingDirectory { get; private set; }

        public string Arguments { get; private set; } = string.Empty;

        public Task OpenInEditorAsync(string aFilePath, CancellationToken aCt = default) => Task.CompletedTask;

        public Task OpenWithDefaultAppAsync(string aFilePath, CancellationToken aCt = default) => Task.CompletedTask;

        public Task<int> RunAsync(string aCommand, string aArguments, string aWorkingDirectory, Action<string> aOnOutputLine, CancellationToken aCt = default)
        {
            WorkingDirectory = aWorkingDirectory;
            Arguments = aArguments;
            return Task.FromResult(0);
        }
    }
}
