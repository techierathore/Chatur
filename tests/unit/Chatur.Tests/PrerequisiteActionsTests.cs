using Chatur.Core.Actions;
using Chatur.Core.Prerequisites;
using Chatur.Tests.Support;
using Xunit;

namespace Chatur.Tests;

/// <summary>Tests for <see cref="PrerequisiteActions"/> (REQ-FN-011, REQ-FN-013).</summary>
public sealed class PrerequisiteActionsTests : MigratedDatabaseFixture
{
    private readonly List<string> objTempFolders = [];
    /// <summary>
    /// When every tool the catalog probes answers with a version, then each row comes back ready with
    /// its found version and no fix command.
    /// </summary>
    [Fact]
    public async Task ProbeAsyncReportsEveryToolReady()
    {
        var vRunner = new FakeCommandRunner(aCommand => aCommand switch
        {
            "dotnet" => new ExternalCommandResult(true, 0, "10.0.401\n"),
            "git" => new ExternalCommandResult(true, 0, "git version 2.46.0\n"),
            "node" => new ExternalCommandResult(true, 0, "v22.3.0\n"),
            "npm" => new ExternalCommandResult(true, 0, "10.8.1\n"),
            _ => new ExternalCommandResult(false, -1, string.Empty),
        });
        var vActions = new PrerequisiteActions(vRunner, new FakeReleaseClient(null), CreateFactory());

        var vChecks = await vActions.ProbeAsync(null);

        Assert.Equal(ToolProbeCatalog.Default.Count, vChecks.Count);
        Assert.All(vChecks, aCheck => Assert.Equal(ToolCheckState.Ready, aCheck.State));
        Assert.Contains(vChecks, aCheck => aCheck.ToolName == "git" && aCheck.FoundVersion == "2.46.0");
        Assert.Contains(vChecks, aCheck => aCheck.ToolName == "Node" && aCheck.FoundVersion == "22.3.0");
    }

    /// <summary>
    /// When a tool is not on the machine at all, then its row is reported missing, with no found
    /// version, and a fix command ready to copy.
    /// </summary>
    [Fact]
    public async Task ProbeAsyncReportsAMissingToolWithAFixCommand()
    {
        var vRunner = new FakeCommandRunner(aCommand => aCommand == "dotnet"
            ? new ExternalCommandResult(true, 0, "10.0.401\n")
            : new ExternalCommandResult(false, -1, string.Empty));
        var vActions = new PrerequisiteActions(vRunner, new FakeReleaseClient(null), CreateFactory());

        var vChecks = await vActions.ProbeAsync(null);

        var vGitRow = Assert.Single(vChecks, aCheck => aCheck.ToolName == "git");
        Assert.Equal(ToolCheckState.Missing, vGitRow.State);
        Assert.Null(vGitRow.FoundVersion);
        Assert.False(string.IsNullOrWhiteSpace(vGitRow.FixCommand));
    }

    /// <summary>
    /// When no project is selected, then Doctor still probes the base set — REQ-FN-011's fallback,
    /// unchanged from before this fix.
    /// </summary>
    [Fact]
    public async Task ProbeAsyncProbesTheBaseSetWhenNoProjectIsSelected()
    {
        var vActions = new PrerequisiteActions(AlwaysReady(), new FakeReleaseClient(null), CreateFactory());

        var vChecks = await vActions.ProbeAsync(null);

        Assert.Equal(ToolProbeCatalog.Default.Select(t => t.ToolName), vChecks.Select(c => c.ToolName));
    }

    /// <summary>
    /// REQ-FN-011: when the selected project is a plain Node project (a <c>package.json</c>, no
    /// <c>.sln</c>/<c>.csproj</c>), then Doctor lists git and Node/npm — never the .NET SDK, which
    /// this project does not need.
    /// </summary>
    [Fact]
    public async Task ProbeAsyncListsNodeAndNpmForAPackageJsonProject()
    {
        var vRoot = CreateTempProjectFolder();
        File.WriteAllText(Path.Combine(vRoot, "package.json"), "{}");
        var vProjectId = CreateProject("node-app", vRoot);

        var vActions = new PrerequisiteActions(AlwaysReady(), new FakeReleaseClient(null), CreateFactory());
        var vChecks = await vActions.ProbeAsync(vProjectId);

        Assert.Contains(vChecks, c => c.ToolName == "git");
        Assert.Contains(vChecks, c => c.ToolName == "Node");
        Assert.Contains(vChecks, c => c.ToolName == "npm");
        Assert.DoesNotContain(vChecks, c => c.ToolName == ".NET SDK");
    }

    /// <summary>
    /// REQ-FN-011: when the selected project has a <c>.csproj</c> with <c>&lt;UseMaui&gt;true&lt;/UseMaui&gt;</c>,
    /// then Doctor also lists the MAUI workload alongside the .NET SDK — this repository's own shape
    /// (<c>src/Chatur/Chatur.csproj</c>).
    /// </summary>
    [Fact]
    public async Task ProbeAsyncListsTheMauiWorkloadForAMauiProject()
    {
        var vRoot = CreateTempProjectFolder();
        File.WriteAllText(Path.Combine(vRoot, "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><UseMaui>true</UseMaui></PropertyGroup></Project>");
        var vProjectId = CreateProject("maui-app", vRoot);

        var vActions = new PrerequisiteActions(AlwaysReady(), new FakeReleaseClient(null), CreateFactory());
        var vChecks = await vActions.ProbeAsync(vProjectId);

        Assert.Contains(vChecks, c => c.ToolName == ".NET SDK");
        Assert.Contains(vChecks, c => c.ToolName == "MAUI workload");
    }

    /// <summary>
    /// REQ-FN-011: when the selected project has a plain (non-MAUI) <c>.csproj</c>, then Doctor lists
    /// the .NET SDK and git only — not Node, npm, Docker or the MAUI workload it does not need.
    /// </summary>
    [Fact]
    public async Task ProbeAsyncListsOnlyDotNetSdkAndGitForAPlainDotNetProject()
    {
        var vRoot = CreateTempProjectFolder();
        File.WriteAllText(Path.Combine(vRoot, "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        var vProjectId = CreateProject("plain-app", vRoot);

        var vActions = new PrerequisiteActions(AlwaysReady(), new FakeReleaseClient(null), CreateFactory());
        var vChecks = await vActions.ProbeAsync(vProjectId);

        Assert.Equal(new[] { "git", ".NET SDK" }, vChecks.Select(c => c.ToolName));
    }

    /// <summary>REQ-FN-011: a <c>Dockerfile</c> adds Docker to the list, on top of whatever else the project needs.</summary>
    [Fact]
    public async Task ProbeAsyncListsDockerWhenADockerfileExists()
    {
        var vRoot = CreateTempProjectFolder();
        File.WriteAllText(Path.Combine(vRoot, "Dockerfile"), "FROM scratch");
        var vProjectId = CreateProject("dockerised-app", vRoot);

        var vActions = new PrerequisiteActions(AlwaysReady(), new FakeReleaseClient(null), CreateFactory());
        var vChecks = await vActions.ProbeAsync(vProjectId);

        Assert.Contains(vChecks, c => c.ToolName == "Docker");
        Assert.Contains(vChecks, c => c.ToolName == "git");
    }

    /// <summary>
    /// REQ-FN-011's own fallback: when the selected project's folder no longer exists on disk (moved
    /// or deleted), then Doctor still probes the base set rather than coming back empty.
    /// </summary>
    [Fact]
    public async Task ProbeAsyncFallsBackToTheBaseSetWhenTheProjectFolderIsGone()
    {
        var vProjectId = CreateProject("gone", "/tmp/chatur-tests/this-folder-does-not-exist-" + Guid.NewGuid().ToString("N"));

        var vActions = new PrerequisiteActions(AlwaysReady(), new FakeReleaseClient(null), CreateFactory());
        var vChecks = await vActions.ProbeAsync(vProjectId);

        Assert.Equal(ToolProbeCatalog.Default.Select(t => t.ToolName), vChecks.Select(c => c.ToolName));
    }

    /// <summary>
    /// When GitHub has no release for the Chatur repository — the honest state today, before the
    /// release pipeline has published anything — then Doctor reports no newer build, never a guess.
    /// </summary>
    [Fact]
    public async Task NewerBuildAsyncReportsNothingWhenNoReleaseExists()
    {
        var vActions = new PrerequisiteActions(new FakeCommandRunner(_ => new ExternalCommandResult(false, -1, string.Empty)), new FakeReleaseClient(null), CreateFactory());

        var vOffer = await vActions.NewerBuildAsync();

        Assert.Null(vOffer);
    }

    private static FakeCommandRunner AlwaysReady() => new(aCommand => aCommand switch
    {
        "dotnet" => new ExternalCommandResult(true, 0, "10.0.401\n"),
        "git" => new ExternalCommandResult(true, 0, "git version 2.46.0\n"),
        "node" => new ExternalCommandResult(true, 0, "v22.3.0\n"),
        "npm" => new ExternalCommandResult(true, 0, "10.8.1\n"),
        "docker" => new ExternalCommandResult(true, 0, "Docker version 27.0.0, build abc123\n"),
        _ => new ExternalCommandResult(false, -1, string.Empty),
    });

    private string CreateTempProjectFolder()
    {
        var vRoot = Path.Combine(Path.GetTempPath(), $"chatur-doctor-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(vRoot);
        objTempFolders.Add(vRoot);
        return vRoot;
    }

    /// <summary>Deletes every temporary project folder this test class created, on top of the base fixture's own database cleanup.</summary>
    public override void Dispose()
    {
        foreach (var vFolder in objTempFolders)
        {
            try
            {
                if (Directory.Exists(vFolder))
                {
                    Directory.Delete(vFolder, recursive: true);
                }
            }
            catch (IOException)
            {
                // best-effort cleanup only — never fail a test run over a leftover temp folder.
            }
        }

        base.Dispose();
    }

    private sealed class FakeCommandRunner : IExternalCommandRunner
    {
        private readonly Func<string, ExternalCommandResult> objAnswer;

        public FakeCommandRunner(Func<string, ExternalCommandResult> aAnswer)
        {
            objAnswer = aAnswer;
        }

        public Task<ExternalCommandResult> RunAsync(string aCommand, string aArguments, CancellationToken aCt = default) =>
            Task.FromResult(objAnswer(aCommand));
    }

    private sealed class FakeReleaseClient : IGitHubReleaseClient
    {
        private readonly GitHubRelease? objRelease;

        public FakeReleaseClient(GitHubRelease? aRelease)
        {
            objRelease = aRelease;
        }

        public Task<GitHubRelease?> LatestReleaseAsync(CancellationToken aCt = default) => Task.FromResult(objRelease);
    }
}
