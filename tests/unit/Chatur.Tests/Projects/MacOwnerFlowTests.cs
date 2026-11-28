using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Chatur.Core;
using Chatur.Core.Actions;
using Chatur.Core.BuildRun;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Chatur.Core.Prerequisites;
using Chatur.Core.Projects;
using Chatur.Tests.Support;
using ChaturDb;
using Microsoft.Data.Sqlite;
using Xunit;
using Xunit.Abstractions;

namespace Chatur.Tests.Projects;

/// <summary>
/// The owner's test parts 1 to 13 (docs/Chatur-UsageGuide.md, "Owner's test 1" to "13") done through Chatur.Core's own
/// action layer with the real <c>dotnet</c> of this Mac: New project, adding projects, Run, Stop, the target picker,
/// several startup projects, a test project and a folder with no solution. Every test runs with the process
/// <c>PATH</c> first cut to the one a Finder-started Mac app gets (<c>/usr/bin:/bin:/usr/sbin:/sbin</c>) and then rebuilt
/// with <see cref="HomebrewPath.ForThisMacApp"/>, as MauiProgram does at start, and the launcher finds a bare command
/// only on that <c>PATH</c>, as the Mac app does. The browser is never opened: the launcher records the address.
/// The projects are made under the repository's <c>bin/MacFlowTest-*</c> folder, whose own build files keep them apart
/// from Chatur's, and that folder is deleted at the end (27 Nov 2026, the owner's first night on a Mac).
/// </summary>
public sealed class MacOwnerFlowTests : IClassFixture<MacOwnerFlowTests.Workspace>
{
    /// <summary>The <c>PATH</c> launchd gives an app started from Finder, the Dock or <c>open</c>.</summary>
    internal const string LaunchdPath = "/usr/bin:/bin:/usr/sbin:/sbin";

    private static readonly TimeSpan BuildTimeout = TimeSpan.FromMinutes(4);

    private readonly Workspace objFlow;
    private readonly ITestOutputHelper objOutput;

    /// <summary>Takes the shared workspace.</summary>
    /// <param name="aFlow">The folder, database and actions every test of this class shares.</param>
    /// <param name="aOutput">Where the Output lines a test proved are written, for the person reading the run.</param>
    public MacOwnerFlowTests(Workspace aFlow, ITestOutputHelper aOutput)
    {
        objFlow = aFlow;
        objOutput = aOutput;
    }

    /// <summary>
    /// Owner's test 1. Given an empty folder OwnerTest opened as a folder, when New project makes a Blazor Web App with
    /// the solution name Checking-Chatur, then the plan names OwnerTest/Checking-Chatur, its Checking-Chatur.slnx and the
    /// project folder Checking-Chatur inside it; afterwards OwnerTest holds only Checking-Chatur, which holds only the
    /// .slnx and the project folder, and the solution lists the project. Before that, with only launchd's PATH, Create
    /// fails with "needs dotnet, and it was not found" and leaves OwnerTest empty: the PATH rebuild is what lets the Mac
    /// app find dotnet at all.
    /// </summary>
    [MacDotnetFact]
    public Task NewBlazorSolutionLayout() => WithMacAppPathAsync(async () =>
    {
        var vOwnerTest = Directory.CreateDirectory(Path.Combine(objFlow.Root, "OwnerTest")).FullName;
        await objFlow.Opener.OpenFolderAsync(vOwnerTest);
        var vRequest = new NewProjectRequest(vOwnerTest, null, "Checking-Chatur", string.Empty, "blazor", null, true);

        var vPlan = objFlow.Templates.Plan(vRequest, ".slnx");
        var vSolutionFolder = Path.Combine(vOwnerTest, "Checking-Chatur");
        Assert.Null(vPlan.Problem);
        Assert.Equal(vSolutionFolder, vPlan.Folder);
        Assert.Equal(Path.Combine(vSolutionFolder, "Checking-Chatur.slnx"), vPlan.SolutionFile);
        Assert.Equal(Path.Combine(vSolutionFolder, "Checking-Chatur"), vPlan.ProjectFolder);

        Environment.SetEnvironmentVariable("PATH", LaunchdPath);
        var vNoDotnet = await Assert.ThrowsAsync<InvalidOperationException>(() => objFlow.Templates.CreateAsync(vRequest));
        Assert.Contains("needs dotnet, and it was not found", vNoDotnet.Message, StringComparison.Ordinal);
        Assert.Empty(Directory.EnumerateFileSystemEntries(vOwnerTest));
        Environment.SetEnvironmentVariable("PATH", HomebrewPath.ForThisMacApp(LaunchdPath));

        var vMade = await objFlow.Templates.CreateAsync(vRequest);

        Assert.Equal(["Checking-Chatur"], Names(vOwnerTest));
        Assert.Equal(["Checking-Chatur", "Checking-Chatur.slnx"], Names(vSolutionFolder));
        Assert.Equal(Path.Combine(vSolutionFolder, "Checking-Chatur.slnx"), vMade.SolutionFile);
        Assert.Equal(Path.Combine(vSolutionFolder, "Checking-Chatur", "Checking-Chatur.csproj"), vMade.ProjectFile);
        Assert.Equal(new[] { vMade.ProjectFile }, SolutionProjects.Listed(vMade.SolutionFile));
        Assert.True(vMade.OpenedNow);
        Assert.Equal(vMade.SolutionFile, objFlow.AppState.SelectedProject?.Path);
    });

    /// <summary>
    /// Owner's tests 3 to 5. Given nothing open, when New project makes a Console App with solution FromNothing and
    /// project TryConsole at a typed location, then the location holds only FromNothing, with FromNothing.slnx and
    /// TryConsole/TryConsole.csproj and Program.cs, and TryConsole is the target. When a Blazor Web App TryWeb, a Class
    /// Library TryLib and an xUnit Test Project TryLib.Tests are added to the open solution, then each is one folder
    /// in the solution's folder and listed by the solution; TryWeb becomes the target and stays the target after the
    /// library and the test project, which cannot run.
    /// </summary>
    [MacDotnetFact]
    public Task FromNothingAddsProjects() => WithMacAppPathAsync(async () =>
    {
        var vSolution = await objFlow.FromNothingAsync();

        Assert.Equal(["FromNothing"], Names(vSolution.Location));
        var vFolder = Path.Combine(vSolution.Location, "FromNothing");
        Assert.Equal(Path.Combine(vFolder, "FromNothing.slnx"), vSolution.SolutionFile);
        Assert.Equal(["FromNothing.slnx", "TryConsole", "TryLib", "TryLib.Tests", "TryWeb"], Names(vFolder));
        Assert.True(File.Exists(Path.Combine(vFolder, "TryConsole", "TryConsole.csproj")));
        Assert.True(File.Exists(Path.Combine(vFolder, "TryConsole", "Program.cs")));
        Assert.Equal(
            ["TryConsole", "TryLib", "TryLib.Tests", "TryWeb"],
            SolutionProjects.Listed(vSolution.SolutionFile).Select(p => Path.GetFileNameWithoutExtension(p)).Order(StringComparer.Ordinal).ToArray());

        Assert.Equal("TryConsole", vSolution.TargetAfterConsole);
        Assert.Equal("TryWeb", vSolution.TargetAfterWeb);
        Assert.Equal("TryWeb", vSolution.TargetAfterLibrary);
        Assert.Equal("TryWeb", vSolution.TargetAfterTests);
        Assert.DoesNotContain(vSolution.FinalTargets, t => t.Name == "TryLib");
        Assert.Contains(vSolution.FinalTargets, t => t.Name == "TryLib.Tests" && t.IsTest);
    });

    /// <summary>
    /// Owner's test 7. Given FromNothing, when TryConsole is chosen and Run pressed, then Output shows "> dotnet build",
    /// "> dotnet run", "Hello, World!" and "Process exited with code 0.". When the ";" is taken off Program.cs's line
    /// and Run pressed again, then the build fails, Output says "The program was not started." with Program.cs as the
    /// first error, and no "dotnet run" or "Hello, World!" line follows. Program.cs is put back afterwards.
    /// </summary>
    [MacDotnetFact]
    public Task ConsoleRunAndBrokenBuild() => WithMacAppPathAsync(async () =>
    {
        var vSolution = await objFlow.FromNothingAsync();
        var vTarget = await objFlow.ChooseAsync(vSolution.ProjectId, "TryConsole");

        await objFlow.BuildRun.RunAsync(vSolution.ProjectId, vTarget.RunTargetId);
        var vGood = await ReadUntilAsync(vSolution.ProjectId, l => l.StartsWith("Process exited with code", StringComparison.Ordinal) || l.Contains("not started", StringComparison.Ordinal));
        Report("c) console run", vGood);
        Assert.Contains(vGood, l => l.StartsWith("> dotnet build", StringComparison.Ordinal));
        Assert.Contains(vGood, l => l.StartsWith("> dotnet run --project", StringComparison.Ordinal));
        Assert.Contains("Hello, World!", vGood);
        Assert.Equal("Process exited with code 0.", vGood[^1]);

        var vProgram = Path.Combine(Path.GetDirectoryName(vSolution.SolutionFile)!, "TryConsole", "Program.cs");
        var vText = await File.ReadAllTextAsync(vProgram);
        try
        {
            await File.WriteAllTextAsync(vProgram, vText.Replace("Console.WriteLine(\"Hello, World!\");", "Console.WriteLine(\"Hello, World!\")", StringComparison.Ordinal));
            await objFlow.BuildRun.RunAsync(vSolution.ProjectId, vTarget.RunTargetId);
            var vBroken = await ReadUntilAsync(vSolution.ProjectId, l => l.StartsWith("Process exited with code", StringComparison.Ordinal) || l.Contains("not started", StringComparison.Ordinal));
            Report("c) broken console run", vBroken);
            Assert.EndsWith(". The program was not started.", vBroken[^1], StringComparison.Ordinal);
            Assert.StartsWith("Build FAILED", vBroken[^1], StringComparison.Ordinal);
            Assert.Contains("first: TryConsole/Program.cs(1): ; expected", vBroken[^1], StringComparison.Ordinal);
            Assert.DoesNotContain(vBroken, l => l.StartsWith("> dotnet run", StringComparison.Ordinal));
            Assert.DoesNotContain("Hello, World!", vBroken);
        }
        finally
        {
            await File.WriteAllTextAsync(vProgram, vText);
        }
    });

    /// <summary>
    /// Owner's test 6. Given FromNothing with TryWeb chosen, when Run is pressed, then Output shows the build, then
    /// "Now listening on: http://localhost:port" and "Opening http://localhost:port in the browser", and the browser is
    /// asked for that one address once. Something really listens on the port. When Stop is pressed, then Output says
    /// "Run stopped.", the process that listened has ended (Stop ends the whole tree, not only <c>dotnet run</c>) and
    /// the port takes no connection any more.
    /// </summary>
    [MacDotnetFact]
    public Task WebRunOpensBrowserAndStops() => WithMacAppPathAsync(async () =>
    {
        var vSolution = await objFlow.FromNothingAsync();
        var vTarget = await objFlow.ChooseAsync(vSolution.ProjectId, "TryWeb");
        objFlow.Launcher.ClearBrowserOpens();

        await objFlow.BuildRun.RunAsync(vSolution.ProjectId, vTarget.RunTargetId);
        int? vListener = null;
        try
        {
            var vStarted = await ReadUntilAsync(vSolution.ProjectId, l => l.StartsWith("Opening ", StringComparison.Ordinal) || l.StartsWith("Process exited with code", StringComparison.Ordinal) || l.Contains("not started", StringComparison.Ordinal));
            var vListening = Assert.Single(vStarted, l => l.Contains("Now listening on: http://localhost:", StringComparison.Ordinal));
            var vAddress = Regex.Match(vListening, @"http://localhost:\d+").Value;
            var vPort = new Uri(vAddress).Port;
            Assert.Equal($"Opening {vAddress} in the browser", vStarted[^1]);
            Assert.True(SpinWait.SpinUntil(() => objFlow.Launcher.BrowserOpens.Count > 0, TimeSpan.FromSeconds(5)));
            Assert.Equal(new[] { vAddress }, objFlow.Launcher.BrowserOpens);
            vListener = ListenerOf(vPort);
            Assert.NotNull(vListener);
            Assert.True(Accepts(vPort));

            await objFlow.BuildRun.StopAsync(vSolution.ProjectId);
            var vStopped = await ReadUntilAsync(vSolution.ProjectId, l => l == "Run stopped." || l.StartsWith("Process exited with code", StringComparison.Ordinal));
            Report("d) web run and stop", vStopped);
            Assert.Equal("Run stopped.", vStopped[^1]);
            Assert.True(SpinWait.SpinUntil(() => HasEnded(vListener.Value), TimeSpan.FromSeconds(15)), $"process {vListener} still runs after Stop");
            Assert.True(SpinWait.SpinUntil(() => !Accepts(vPort), TimeSpan.FromSeconds(15)), $"port {vPort} still takes connections after Stop");
            vListener = null;
        }
        finally
        {
            await objFlow.BuildRun.StopAsync(vSolution.ProjectId);
            Workspace.EndIfRunning(vListener);
        }
    });

    /// <summary>
    /// Owner's test 8. Given FromNothing, when TryLib.Tests is chosen and Run pressed, then it is built, Output shows
    /// "> dotnet test", a summary with no failed test, and "Process exited with code 0.".
    /// </summary>
    [MacDotnetFact]
    public Task TestProjectRunsDotnetTest() => WithMacAppPathAsync(async () =>
    {
        var vSolution = await objFlow.FromNothingAsync();
        var vTarget = await objFlow.ChooseAsync(vSolution.ProjectId, "TryLib.Tests");

        await objFlow.BuildRun.RunAsync(vSolution.ProjectId, vTarget.RunTargetId);
        var vLines = await ReadUntilAsync(vSolution.ProjectId, l => l.StartsWith("Process exited with code", StringComparison.Ordinal) || l.Contains("not started", StringComparison.Ordinal));
        Report("e) test project run", vLines);
        Assert.Contains(vLines, l => l.StartsWith("> dotnet test", StringComparison.Ordinal));
        Assert.Contains(vLines, l => Regex.IsMatch(l, @"failed:\s*0\b", RegexOptions.IgnoreCase));
        Assert.DoesNotContain(vLines, l => Regex.IsMatch(l, @"failed:\s*[1-9]", RegexOptions.IgnoreCase));
        Assert.Equal("Process exited with code 0.", vLines[^1]);
    });

    /// <summary>
    /// Owner's test 9. Given FromNothing, when TryConsole and TryWeb are the startup projects, then only those two of
    /// the runnable ones are offered, both are built and started together, and their lines carry their names:
    /// "[TryConsole] Hello, World!" and "[TryWeb] Now listening on: ...". When Stop is pressed, then TryWeb says
    /// "[TryWeb] Run stopped.", the last line is "All 2 projects ended." and TryWeb's port is free again.
    /// </summary>
    [MacDotnetFact]
    public Task TwoStartupProjectsRunAndStop() => WithMacAppPathAsync(async () =>
    {
        var vSolution = await objFlow.FromNothingAsync();
        var vTargets = await objFlow.BuildRun.TargetsAsync(vSolution.ProjectId);
        Assert.Equal(["TryConsole", "TryWeb"], vTargets.Where(t => !t.IsTest).Select(t => t.Name).Order(StringComparer.Ordinal).ToArray());
        await objFlow.BuildRun.SetStartupAsync(vSolution.ProjectId, ["TryConsole/TryConsole.csproj", "TryWeb/TryWeb.csproj"]);
        Assert.Equal(2, (await objFlow.BuildRun.StartupAsync(vSolution.ProjectId)).Count);

        await objFlow.BuildRun.RunAsync(vSolution.ProjectId, vTargets[0].RunTargetId);
        int? vListener = null;
        try
        {
            var vStarted = await ReadUntilAsync(vSolution.ProjectId, l => l.StartsWith("[TryWeb] Opening ", StringComparison.Ordinal) || l.Contains("not started", StringComparison.Ordinal) || l.StartsWith("All 2", StringComparison.Ordinal));
            var vListening = Assert.Single(vStarted, l => l.StartsWith("[TryWeb] ", StringComparison.Ordinal) && l.Contains("Now listening on: http://localhost:", StringComparison.Ordinal));
            var vPort = new Uri(Regex.Match(vListening, @"http://localhost:\d+").Value).Port;
            vListener = ListenerOf(vPort);
            Assert.NotNull(vListener);

            await objFlow.BuildRun.StopAsync(vSolution.ProjectId);
            var vEnded = await ReadUntilAsync(vSolution.ProjectId, l => l.StartsWith("All 2 projects ended.", StringComparison.Ordinal));
            Report("f) two startup projects", vEnded);
            Assert.Contains("[TryConsole] Hello, World!", vEnded);
            Assert.Contains("[TryConsole] Process exited with code 0.", vEnded);
            Assert.Contains("[TryWeb] Run stopped.", vEnded);
            Assert.Equal("All 2 projects ended.", vEnded[^1]);
            Assert.True(SpinWait.SpinUntil(() => HasEnded(vListener.Value), TimeSpan.FromSeconds(15)), $"process {vListener} still runs after Stop");
            Assert.True(SpinWait.SpinUntil(() => !Accepts(vPort), TimeSpan.FromSeconds(15)), $"port {vPort} still takes connections after Stop");
            vListener = null;
        }
        finally
        {
            await objFlow.BuildRun.StopAsync(vSolution.ProjectId);
            await objFlow.BuildRun.SetStartupAsync(vSolution.ProjectId, []);
            Workspace.EndIfRunning(vListener);
        }
    });

    /// <summary>
    /// Owner's tests 12 and 13. Given a folder LooseOne holding only the console project Loose (made with
    /// <c>dotnet new console</c>), when it is checked before opening, then it has no solution and names Loose/Loose.csproj.
    /// When it is opened as it is and New project makes a Console App with solution Gathered and project Second, then
    /// the plan says Loose/Loose.csproj will be added; afterwards LooseOne holds Gathered.slnx, Loose and Second, the
    /// solution lists both projects, the folder's row is gone from the list in favour of the solution, and Second, just
    /// added, is the target.
    /// </summary>
    [MacDotnetFact]
    public Task LooseProjectGetsSolution() => WithMacAppPathAsync(async () =>
    {
        var vLooseOne = Path.Combine(objFlow.Root, "LooseOne");
        Directory.CreateDirectory(vLooseOne);
        var vExit = await objFlow.Launcher.RunAsync("dotnet", $"new console -n Loose -o \"{Path.Combine(vLooseOne, "Loose")}\"", vLooseOne, _ => { });
        Assert.Equal(0, vExit);

        var vCheck = await objFlow.Opener.CheckFolderAsync(vLooseOne);
        Assert.False(vCheck.HasSolution);
        Assert.Equal(new[] { "Loose/Loose.csproj" }, vCheck.Projects);

        await objFlow.Opener.OpenFolderAsync(vLooseOne);
        var vRequest = new NewProjectRequest(vLooseOne, null, "Gathered", "Second", "console", null, true);
        var vPlan = objFlow.Templates.Plan(vRequest, ".slnx");
        Assert.Equal(new[] { "Loose/Loose.csproj" }, vPlan.ProjectsToAdd);
        Assert.Equal(Path.Combine(vLooseOne, "Gathered.slnx"), vPlan.SolutionFile);

        var vMade = await objFlow.Templates.CreateAsync(vRequest);
        Assert.True(await objFlow.BuildRun.SetRunTargetForProjectFileAsync(vMade.ProjectId, vMade.ProjectFile));

        Assert.Equal(["Gathered.slnx", "Loose", "Second"], Names(vLooseOne));
        Assert.Equal(
            ["Loose", "Second"],
            SolutionProjects.Listed(vMade.SolutionFile).Select(p => Path.GetFileNameWithoutExtension(p)).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal("Second", (await objFlow.BuildRun.TargetsAsync(vMade.ProjectId))[0].Name);
        Assert.Equal(0, objFlow.OpenFolderRows(vLooseOne));
    });

    /// <summary>Runs a test body with launchd's <c>PATH</c> rebuilt as the Mac app rebuilds it, and puts the process's own back afterwards.</summary>
    private static async Task WithMacAppPathAsync(Func<Task> aBody)
    {
        var vOriginal = Environment.GetEnvironmentVariable("PATH");
        try
        {
            Environment.SetEnvironmentVariable("PATH", LaunchdPath);
            Environment.SetEnvironmentVariable("PATH", HomebrewPath.ForThisMacApp(Environment.GetEnvironmentVariable("PATH")));
            await aBody();
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", vOriginal);
        }
    }

    /// <summary>Reads the project's Output from the top until a line meets <paramref name="aLast"/>; fails with what it read when none does in time.</summary>
    private async Task<List<string>> ReadUntilAsync(int aProjectId, Func<string, bool> aLast)
    {
        var vLines = new List<string>();
        using var vTimeout = new CancellationTokenSource(BuildTimeout);
        await foreach (var vLine in objFlow.BuildRun.StreamOutputAsync(aProjectId, vTimeout.Token))
        {
            vLines.Add(vLine.Text);
            if (aLast(vLine.Text))
            {
                return vLines;
            }
        }

        Assert.Fail($"The expected line did not come in {BuildTimeout.TotalMinutes} minutes. Output:{Environment.NewLine}{string.Join(Environment.NewLine, vLines)}");
        return vLines;
    }

    /// <summary>Writes the Output lines a test proved to the test's own output.</summary>
    private void Report(string aWhat, IReadOnlyList<string> aLines) =>
        objOutput.WriteLine($"=== {aWhat}{Environment.NewLine}{string.Join(Environment.NewLine, aLines)}");

    /// <summary>The names directly in a folder, files and folders, in ordinal order.</summary>
    private static string[] Names(string aFolder) =>
        Directory.EnumerateFileSystemEntries(aFolder).Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal).ToArray();

    /// <summary>The id of the process listening on a local TCP port, read with <c>lsof</c>; null when none is.</summary>
    private static int? ListenerOf(int aPort)
    {
        using var vLsof = Process.Start(new ProcessStartInfo("/usr/sbin/lsof", $"-nP -iTCP:{aPort} -sTCP:LISTEN -t")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        })!;
        var vText = vLsof.StandardOutput.ReadToEnd();
        vLsof.WaitForExit();
        var vFirst = vText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return int.TryParse(vFirst, out var vPid) ? vPid : null;
    }

    /// <summary>Whether something on this machine takes a connection on the port.</summary>
    private static bool Accepts(int aPort)
    {
        try
        {
            using var vClient = new TcpClient();
            vClient.Connect("127.0.0.1", aPort);
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    /// <summary>Whether the process with this id has ended.</summary>
    private static bool HasEnded(int aProcessId)
    {
        try
        {
            using var vProcess = Process.GetProcessById(aProcessId);
            return vProcess.HasExited;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    /// <summary>Finds a bare command on a <c>PATH</c> value the way a Mac app whose own folder holds no such program finds it; null when it is not there.</summary>
    /// <param name="aCommand">The command, e.g. <c>dotnet</c>, or a path.</param>
    /// <param name="aPath">The <c>PATH</c> value to search.</param>
    internal static string? FindOnPath(string aCommand, string? aPath)
    {
        if (aCommand.Contains('/', StringComparison.Ordinal))
        {
            return File.Exists(aCommand) ? aCommand : null;
        }

        return (aPath ?? string.Empty).Split(':', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => Path.Combine(f, aCommand))
            .FirstOrDefault(File.Exists);
    }

    /// <summary>A Fact that runs only on a Mac whose <c>dotnet</c> a Finder-started Chatur would find; skipped anywhere else.</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MacDotnetFactAttribute : FactAttribute
    {
        /// <summary>Sets the skip reason when this is not a Mac or it has no dotnet.</summary>
        public MacDotnetFactAttribute()
        {
            if (!HostPlatform.IsMac)
            {
                Skip = "Runs only on a Mac: it drives the real dotnet the way the Mac app does.";
            }
            else if (FindOnPath("dotnet", HomebrewPath.ForThisMacApp(LaunchdPath)) is null)
            {
                Skip = "This Mac has no dotnet where the Mac app looks for it.";
            }
        }
    }

    /// <summary>
    /// What a test sees of FromNothing once it is made: where, its solution and project row, and the target the picker
    /// read after each project was added.
    /// </summary>
    public sealed record FromNothingSolution(
        string Location,
        string SolutionFile,
        int ProjectId,
        string TargetAfterConsole,
        string TargetAfterWeb,
        string TargetAfterLibrary,
        string TargetAfterTests,
        IReadOnlyList<RunTarget> FinalTargets);

    /// <summary>
    /// The folder under the repository's <c>bin</c>, the database and the real actions the tests share; made at the
    /// first use and deleted at the end, with every program a test started ended first.
    /// </summary>
    public sealed class Workspace : IDisposable
    {
        private readonly object objGate = new();
        private Task<FromNothingSolution>? objFromNothing;
        private string? objRoot;
        private FlowConnections? objConnections;
        private ProjectOpenActions? objOpener;
        private ProjectTemplateActions? objTemplates;
        private BuildRunActions? objBuildRun;

        /// <summary>The real launcher every action runs <c>dotnet</c> through.</summary>
        public MacAppLauncher Launcher { get; } = new();

        /// <summary>The shared, in-memory state the actions select projects in.</summary>
        public AppState AppState { get; } = new();

        /// <summary>The workspace folder, <c>bin/MacFlowTest-*</c> under the repository.</summary>
        public string Root => Started().objRoot!;

        /// <summary>Open a folder, a solution; check a folder.</summary>
        public ProjectOpenActions Opener => Started().objOpener!;

        /// <summary>New project.</summary>
        public ProjectTemplateActions Templates => Started().objTemplates!;

        /// <summary>Run, Stop, the target picker and Output.</summary>
        public BuildRunActions BuildRun => Started().objBuildRun!;

        /// <summary>
        /// Owner's tests 3 and 4, once for the whole class: Console App FromNothing/TryConsole at a typed location with
        /// nothing open, then TryWeb, TryLib and TryLib.Tests added to it, each followed by what the New project dialog
        /// does after Create (the project just made becomes the target when it can run).
        /// </summary>
        public Task<FromNothingSolution> FromNothingAsync()
        {
            lock (objGate)
            {
                return objFromNothing ??= MakeFromNothingAsync();
            }
        }

        /// <summary>Chooses the named target in the picker, ending a several-projects choice, and returns it.</summary>
        public async Task<RunTarget> ChooseAsync(int aProjectId, string aName)
        {
            var vTarget = (await BuildRun.TargetsAsync(aProjectId)).Single(t => t.Name == aName);
            await BuildRun.SetRunTargetAsync(aProjectId, vTarget.RunTargetId);
            return vTarget;
        }

        /// <summary>How many rows still stand for a folder opened as it is (not removed).</summary>
        public int OpenFolderRows(string aFolder)
        {
            using var vConnection = objConnections!.OpenConnection();
            using var vCommand = vConnection.CreateCommand();
            vCommand.CommandText = $"SELECT COUNT(*) FROM Project WHERE Kind = 'Folder' AND RemovedUtc IS NULL AND Path = '{aFolder.Replace("'", "''", StringComparison.Ordinal)}';";
            return Convert.ToInt32(vCommand.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>Ends a process a test saw listening, when it still runs; a process already gone is left alone.</summary>
        public static void EndIfRunning(int? aProcessId)
        {
            if (aProcessId is not { } vId || HasEnded(vId))
            {
                return;
            }

            using var vProcess = Process.GetProcessById(vId);
            vProcess.Kill(entireProcessTree: true);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (objRoot is null)
            {
                return;
            }

            Launcher.EndAll();
            SqliteConnection.ClearAllPools();
            for (var vTry = 1; Directory.Exists(objRoot); vTry++)
            {
                try
                {
                    Directory.Delete(objRoot, true);
                }
                catch (IOException) when (vTry < 20)
                {
                    Thread.Sleep(500);
                }
            }
        }

        private Workspace Started()
        {
            lock (objGate)
            {
                if (objRoot is not null)
                {
                    return this;
                }

                var vRoot = Path.Combine(RepositoryRoot(), "bin", $"MacFlowTest-{Guid.NewGuid():N}"[..21]);
                Directory.CreateDirectory(vRoot);
                var vDatabase = Path.Combine(vRoot, "chatur.db");
                var vMigrated = ChaturDbMigrator.Migrate($"Data Source={vDatabase}");
                Assert.True(vMigrated.Successful, vMigrated.Error?.Message);
                objConnections = new FlowConnections($"Data Source={vDatabase}");
                var vProjects = new ProjectActions(objConnections, new SystemClock(), AppState);
                objOpener = new ProjectOpenActions(objConnections, vProjects, Launcher, AppState);
                objTemplates = new ProjectTemplateActions(objConnections, Launcher, objOpener);
                objBuildRun = new BuildRunActions(objConnections, Launcher);
                objRoot = vRoot;
                return this;
            }
        }

        private async Task<FromNothingSolution> MakeFromNothingAsync()
        {
            var vLocation = Directory.CreateDirectory(Path.Combine(Root, "Three")).FullName;
            await Opener.CloseProjectAsync();
            var vConsole = await Templates.CreateAsync(new NewProjectRequest(vLocation, null, "FromNothing", "TryConsole", "console", null));
            var vAfterConsole = await TargetAfterAsync(vConsole);

            async Task<string> AddAsync(string aName, string aTemplate)
            {
                var vFolder = Path.GetDirectoryName(vConsole.SolutionFile)!;
                var vMade = await Templates.CreateAsync(new NewProjectRequest(vFolder, vConsole.SolutionFile, string.Empty, aName, aTemplate, null));
                return await TargetAfterAsync(vMade);
            }

            var vAfterWeb = await AddAsync("TryWeb", "blazor");
            var vAfterLibrary = await AddAsync("TryLib", "classlib");
            var vAfterTests = await AddAsync("TryLib.Tests", "xunit");
            return new FromNothingSolution(
                vLocation,
                vConsole.SolutionFile,
                vConsole.ProjectId,
                vAfterConsole,
                vAfterWeb,
                vAfterLibrary,
                vAfterTests,
                await BuildRun.TargetsAsync(vConsole.ProjectId));
        }

        /// <summary>What the New project dialog does after Create, then the name the target picker reads.</summary>
        private async Task<string> TargetAfterAsync(NewProjectResult aMade)
        {
            await BuildRun.SetRunTargetForProjectFileAsync(aMade.ProjectId, aMade.ProjectFile);
            return (await BuildRun.TargetsAsync(aMade.ProjectId))[0].Name;
        }

        private static string RepositoryRoot()
        {
            for (var vFolder = new DirectoryInfo(AppContext.BaseDirectory); vFolder is not null; vFolder = vFolder.Parent)
            {
                if (File.Exists(Path.Combine(vFolder.FullName, "Chatur.sln")))
                {
                    return vFolder.FullName;
                }
            }

            throw new InvalidOperationException("The repository root (Chatur.sln) was not found above the test's folder.");
        }
    }

    /// <summary>
    /// The test database, with an account key of its own so this class's runs never share Output with another class's
    /// project 1 in <see cref="BuildRunActions"/>' process-wide registry.
    /// </summary>
    private sealed class FlowConnections : IDbConnectionFactory
    {
        private readonly TestDbConnectionFactory objInner;

        public FlowConnections(string aConnectionString)
        {
            objInner = new TestDbConnectionFactory(aConnectionString);
        }

        public string? CurrentUserId { get; } = $"mac-flow-{Guid.NewGuid():N}";

        public IDbConnection OpenConnection() => objInner.OpenConnection();
    }

    /// <summary>
    /// Runs commands as src/Chatur/Platform/MauiProcessLauncher.cs does (output and error lines as they come, the
    /// whole process tree ended on cancel) and records the browser address instead of opening it. A bare command is
    /// found only on <c>PATH</c>: the test host's own folder holds <c>dotnet</c>, the Mac app's does not. MSBuild nodes
    /// and the compiler servers are kept from outliving a build, so no program of the test is left behind.
    /// </summary>
    public sealed class MacAppLauncher : IProcessLauncher
    {
        private readonly ConcurrentQueue<string> objBrowserOpens = new();
        private readonly ConcurrentDictionary<int, DateTime> objStarted = new();

        /// <summary>The addresses the actions asked the browser to open, in order.</summary>
        public IReadOnlyList<string> BrowserOpens => objBrowserOpens.ToArray();

        /// <summary>Forgets the recorded browser addresses.</summary>
        public void ClearBrowserOpens() => objBrowserOpens.Clear();

        /// <inheritdoc />
        public Task OpenWithDefaultAppAsync(string aFilePath, CancellationToken aCt = default)
        {
            objBrowserOpens.Enqueue(aFilePath);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OpenBrowserAsync(string aUrl, CancellationToken aCt = default)
        {
            objBrowserOpens.Enqueue(aUrl);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<int> RunAsync(string aCommand, string aArguments, string aWorkingDirectory, Action<string> aOnOutputLine, CancellationToken aCt = default)
        {
            var vProgram = FindOnPath(aCommand, Environment.GetEnvironmentVariable("PATH"))
                ?? throw new Win32Exception(2, $"{aCommand} was not found on PATH");
            var vStart = new ProcessStartInfo
            {
                FileName = vProgram,
                Arguments = aArguments,
                WorkingDirectory = aWorkingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            vStart.Environment["MSBUILDDISABLENODEREUSE"] = "1";
            vStart.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
            vStart.Environment["UseSharedCompilation"] = "false";
            vStart.Environment["UseRazorBuildServer"] = "false";
            using var vProcess = new Process { StartInfo = vStart, EnableRaisingEvents = true };
            vProcess.OutputDataReceived += (_, aArgs) =>
            {
                if (aArgs.Data is not null)
                {
                    aOnOutputLine(aArgs.Data);
                }
            };
            vProcess.ErrorDataReceived += (_, aArgs) =>
            {
                if (aArgs.Data is not null)
                {
                    aOnOutputLine(aArgs.Data);
                }
            };

            vProcess.Start();
            objStarted[vProcess.Id] = vProcess.StartTime;
            try
            {
                vProcess.BeginOutputReadLine();
                vProcess.BeginErrorReadLine();
                await vProcess.WaitForExitAsync(aCt).ConfigureAwait(false);
                return vProcess.ExitCode;
            }
            catch (OperationCanceledException)
            {
                if (!vProcess.HasExited)
                {
                    vProcess.Kill(entireProcessTree: true);
                }

                throw;
            }
            finally
            {
                objStarted.TryRemove(vProcess.Id, out _);
            }
        }

        /// <summary>Ends, with its tree, every process this launcher started that still runs (the same process, by its start time).</summary>
        public void EndAll()
        {
            foreach (var (vId, vStartTime) in objStarted)
            {
                try
                {
                    using var vProcess = Process.GetProcessById(vId);
                    if (!vProcess.HasExited && vProcess.StartTime == vStartTime)
                    {
                        vProcess.Kill(entireProcessTree: true);
                    }
                }
                catch (Exception aException) when (aException is ArgumentException or InvalidOperationException or Win32Exception)
                {
                    // Already gone.
                }
            }
        }
    }
}
