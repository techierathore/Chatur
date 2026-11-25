using Chatur.Core.Actions;
using Chatur.Core.BuildRun;
using Chatur.Core.Platform;
using Chatur.Tests.Support;
using Xunit;

namespace Chatur.Tests.BuildRun;

/// <summary>
/// Run, as the owner's evening test of 21 Nov 2026 asked for it (REQ-UI-107, REQ-FN-306, REQ-UI-108): the build shows
/// first, a failed build says so and starts nothing, a web project runs with its own launch profile and opens in the
/// browser at its address once it says it is listening, and the program's exit code is always shown.
/// </summary>
public sealed class RunSequenceTests : MigratedDatabaseFixture
{
    private readonly string objRoot = Path.Combine(TestScratch.Root, $"run-sequence-{Guid.NewGuid():N}");

    /// <summary>Makes the scratch folder for one test.</summary>
    public RunSequenceTests()
    {
        Directory.CreateDirectory(objRoot);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        try
        {
            Directory.Delete(objRoot, recursive: true);
        }
        catch (IOException)
        {
            // a file still held is left for the next run's bin clean
        }
    }

    /// <summary>
    /// When a web project with launch settings is run, then it is built first, run with its own launch profile and
    /// without a second build, and once it prints "Now listening on" the browser opens at the profile's address.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-306 a web project runs with its launch profile and opens at its address once listening")]
    public async Task WebProjectOpensInBrowser()
    {
        var vSolution = WriteSolution("Web", "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        Directory.CreateDirectory(Path.Combine(objRoot, "Web", "Properties"));
        File.WriteAllText(Path.Combine(objRoot, "Web", "Properties", "launchSettings.json"), """
            {
              // comments and trailing commas are allowed, as Visual Studio writes them
              "profiles": {
                "IIS Express": { "commandName": "IISExpress" },
                "http": { "commandName": "Project", "launchBrowser": true, "applicationUrl": "http://localhost:5170;http://localhost:5171", "launchUrl": "swagger", },
              }
            }
            """);
        var vLauncher = new ScriptedLauncher(aArguments => aArguments.StartsWith("run", StringComparison.Ordinal)
            ? ["info: Microsoft.Hosting.Lifetime[14]", "      Now listening on: http://localhost:5170", "Now listening on: http://localhost:5171"]
            : ["Build succeeded."]);
        var vLines = await RunToEndAsync(vSolution, vLauncher);

        Assert.StartsWith("build ", vLauncher.Commands[0], StringComparison.Ordinal);
        Assert.StartsWith("run ", vLauncher.Commands[1], StringComparison.Ordinal);
        Assert.Contains("--no-build", vLauncher.Commands[1], StringComparison.Ordinal);
        Assert.Contains("--launch-profile \"http\"", vLauncher.Commands[1], StringComparison.Ordinal);
        Assert.Equal(["http://localhost:5170/swagger"], vLauncher.Opened);
        Assert.Contains(vLines, aLine => aLine.Text == "Opening http://localhost:5170/swagger in the browser");
        Assert.Contains(vLines, aLine => aLine.Text == "Process exited with code 0." && !aLine.IsError);
    }

    /// <summary>
    /// When the build of a run fails, then Output says the build failed, names the error with file and line, and the
    /// program is not started.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-108 a run whose build fails says so with its errors and starts nothing")]
    public async Task FailedBuildStartsNothing()
    {
        var vSolution = WriteSolution("Tool", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        var vError = Path.Combine(objRoot, "Tool", "Program.cs") + "(3,1): error CS1002: ; expected [" + Path.Combine(objRoot, "Tool", "Tool.csproj") + "]";
        var vLauncher = new ScriptedLauncher(_ => [vError], aExitCode: 1);

        var vLines = await RunToEndAsync(vSolution, vLauncher);

        Assert.Single(vLauncher.Commands);
        Assert.Contains(vLines, aLine => aLine.IsError && aLine.Text.StartsWith("Build FAILED", StringComparison.Ordinal)
            && aLine.Text.Contains("1 error", StringComparison.Ordinal)
            && aLine.Text.Contains("Tool/Program.cs(3)", StringComparison.Ordinal)
            && aLine.Text.EndsWith("The program was not started.", StringComparison.Ordinal));
        Assert.Contains(vLines, aLine => aLine.IsError && aLine.Text == vError);
    }

    /// <summary>When a console program ends with a code that is not 0, then Output shows that code, marked as an error.</summary>
    [Fact(DisplayName = "REQ-UI-108 a program that ends shows its exit code")]
    public async Task EndedProgramShowsExitCode()
    {
        var vSolution = WriteSolution("Tool", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        var vLauncher = new ScriptedLauncher(aArguments => aArguments.StartsWith("run", StringComparison.Ordinal) ? ["hello"] : [], aRunExitCode: 3);

        var vLines = await RunToEndAsync(vSolution, vLauncher);

        Assert.Contains("--no-launch-profile", vLauncher.Commands[1], StringComparison.Ordinal);
        Assert.Empty(vLauncher.Opened);
        Assert.Contains(vLines, aLine => aLine.Text == "hello");
        Assert.Contains(vLines, aLine => aLine.Text == "Process exited with code 3." && aLine.IsError);
    }

    /// <summary>When a solution's test project is run, then its tests run with dotnet test after the build.</summary>
    [Fact(DisplayName = "REQ-UI-107 running a solution's test project runs its tests")]
    public async Task TestProjectRunsItsTests()
    {
        var vSolution = WriteSolution("Lib.Tests", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include=\"Microsoft.NET.Test.Sdk\" Version=\"17.*\" /></ItemGroup></Project>");
        var vLauncher = new ScriptedLauncher(_ => ["Passed!  - Failed: 0, Passed: 1"]);

        await RunToEndAsync(vSolution, vLauncher);

        Assert.StartsWith("test ", vLauncher.Commands[1], StringComparison.Ordinal);
        Assert.Contains("--no-build", vLauncher.Commands[1], StringComparison.Ordinal);
    }

    /// <summary>
    /// When Run is pressed with a target that is no longer the project's (its targets were read again under new ids),
    /// then Output says so in a "Failed to run:" line, which ends the run, instead of showing nothing while Run stays
    /// busy (the owner's evening test of 21 Nov 2026).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-107 a run that cannot start says why in Output and ends")]
    public async Task RunThatCannotStartSaysWhy()
    {
        var vSolution = WriteSolution("Tool", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        var vProjectId = CreateProject("App", vSolution);
        var vLauncher = new ScriptedLauncher(_ => []);
        var vActions = new BuildRunActions(CreateFactory(), vLauncher);
        var vTarget = Assert.Single(await vActions.TargetsAsync(vProjectId));

        await vActions.RunAsync(vProjectId, vTarget.RunTargetId + 1000);

        var vLines = new List<OutputLine>();
        using var vCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await foreach (var vLine in vActions.StreamOutputAsync(vProjectId, vCts.Token))
        {
            vLines.Add(vLine);
            if (vLine.Text.StartsWith("Failed to run:", StringComparison.Ordinal))
            {
                break;
            }
        }

        Assert.Empty(vLauncher.Commands);
        var vFailed = Assert.Single(vLines, aLine => aLine.Text.StartsWith("Failed to run:", StringComparison.Ordinal));
        Assert.True(vFailed.IsError);
        Assert.Contains("target picker", vFailed.Text, StringComparison.Ordinal);
    }

    /// <summary>The profile reader picks the first Project profile and writes a wildcard host as localhost.</summary>
    [Fact(DisplayName = "REQ-FN-306 the listening line and the profile give the browser address")]
    public void ListeningAddressIsBrowsable()
    {
        Assert.Equal("http://localhost:8080", LaunchSettings.ListeningAddress("Now listening on: http://[::]:8080"));
        Assert.Equal("http://localhost:5000", LaunchSettings.ListeningAddress("      Now listening on: http://0.0.0.0:5000"));
        Assert.Null(LaunchSettings.ListeningAddress("Application started."));
        Assert.Equal("http://localhost:5000", LaunchSettings.BrowserAddress(null, "http://localhost:5000"));
        Assert.True(LaunchSettings.IsWebProject("<Project Sdk=\"Microsoft.NET.Sdk.Web\">"));
        Assert.False(LaunchSettings.IsWebProject("<Project Sdk=\"Microsoft.NET.Sdk\">"));
    }

    private string WriteSolution(string aProject, string aProjectText)
    {
        Directory.CreateDirectory(Path.Combine(objRoot, aProject));
        File.WriteAllText(Path.Combine(objRoot, aProject, $"{aProject}.csproj"), aProjectText);
        var vSolution = Path.Combine(objRoot, "App.sln");
        File.WriteAllText(vSolution, $"Microsoft Visual Studio Solution File, Format Version 12.00\nProject(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{aProject}\", \"{aProject}\\{aProject}.csproj\", \"{{11111111-1111-1111-1111-111111111111}}\"\nEndProject\n");
        return vSolution;
    }

    /// <summary>Runs the solution's one target and reads Output until the run's last line.</summary>
    private async Task<List<OutputLine>> RunToEndAsync(string aSolution, ScriptedLauncher aLauncher)
    {
        var vProjectId = CreateProject("App", aSolution);
        var vActions = new BuildRunActions(CreateFactory(), aLauncher);
        var vTarget = Assert.Single(await vActions.TargetsAsync(vProjectId));

        await vActions.RunAsync(vProjectId, vTarget.RunTargetId);

        var vLines = new List<OutputLine>();
        using var vCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await foreach (var vLine in vActions.StreamOutputAsync(vProjectId, vCts.Token))
        {
            vLines.Add(vLine);
            if (vLine.Text.StartsWith("Process exited", StringComparison.Ordinal) || vLine.Text.Contains("was not started", StringComparison.Ordinal))
            {
                break;
            }
        }

        return vLines;
    }

    /// <summary>A launcher that answers each command with the lines a script gives it, and records what it was asked.</summary>
    private sealed class ScriptedLauncher(Func<string, string[]> aScript, int aExitCode = 0, int aRunExitCode = 0) : IProcessLauncher
    {
        public List<string> Commands { get; } = [];

        public List<string> Opened { get; } = [];

        public Task OpenInEditorAsync(string aFilePath, CancellationToken aCt = default) => Task.CompletedTask;

        public Task OpenWithDefaultAppAsync(string aFilePath, CancellationToken aCt = default) => Task.CompletedTask;

        public Task OpenBrowserAsync(string aUrl, CancellationToken aCt = default)
        {
            lock (Opened)
            {
                Opened.Add(aUrl);
            }

            return Task.CompletedTask;
        }

        public Task<int> RunAsync(string aCommand, string aArguments, string aWorkingDirectory, Action<string> aOnOutputLine, CancellationToken aCt = default)
        {
            Commands.Add(aArguments);
            foreach (var vLine in aScript(aArguments))
            {
                aOnOutputLine(vLine);
            }

            return Task.FromResult(aArguments.StartsWith("build", StringComparison.Ordinal) ? aExitCode : aRunExitCode);
        }
    }
}
