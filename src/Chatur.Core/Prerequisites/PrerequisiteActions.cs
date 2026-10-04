using System.Globalization;
using System.Reflection;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Dapper;

namespace Chatur.Core.Prerequisites;

/// <summary>
/// <see cref="IPrerequisiteActions"/> over this machine's own installed tools and, for
/// <see cref="NewerBuildAsync"/>, the Chatur repository's GitHub releases.
/// </summary>
public sealed class PrerequisiteActions : IPrerequisiteActions
{
    private static readonly HashSet<string> ExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", "node_modules", ".vs", ".idea"
    };

    private readonly IExternalCommandRunner objCommandRunner;
    private readonly IGitHubReleaseClient objReleaseClient;
    private readonly IDbConnectionFactory objDb;

    /// <summary>Creates the actions.</summary>
    /// <param name="aCommandRunner">Probes each tool's version.</param>
    /// <param name="aReleaseClient">Reads the newest GitHub release of the Chatur repository.</param>
    /// <param name="aDb">Reads the selected project's own path, to derive what it needs (REQ-FN-011).</param>
    public PrerequisiteActions(IExternalCommandRunner aCommandRunner, IGitHubReleaseClient aReleaseClient, IDbConnectionFactory aDb)
    {
        objCommandRunner = aCommandRunner;
        objReleaseClient = aReleaseClient;
        objDb = aDb;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Derives which tools to probe from the selected project's own files (REQ-FN-011's acceptance:
    /// "each tool it needs is listed") — a <c>.sln</c>/<c>.csproj</c> means the .NET SDK (plus the
    /// MAUI workload when a <c>.csproj</c> sets <c>UseMaui</c>), a <c>package.json</c> means Node and
    /// npm, a <c>Dockerfile</c>/<c>docker-compose.y[a]ml</c> means Docker, and git is always probed.
    /// Falls back to <see cref="ToolProbeCatalog.Default"/> — Chatur's own build/test tools — when no
    /// project is selected, or the selected project's path cannot be read from disk (a moved or
    /// deleted folder), so Doctor never comes back with an empty table.
    /// </remarks>
    public async Task<IReadOnlyList<ToolCheck>> ProbeAsync(int? aProjectId, CancellationToken aCt = default)
    {
        var vCatalog = aProjectId is null
            ? ToolProbeCatalog.Default
            : await ResolveProjectCatalogAsync(aProjectId.Value, aCt).ConfigureAwait(false);

        var vChecks = new List<ToolCheck>();
        var vFixCommand = OperatingSystem.IsMacOS();

        foreach (var vTool in vCatalog)
        {
            var vResult = await objCommandRunner.RunAsync(vTool.Command, vTool.VersionArguments, aCt).ConfigureAwait(false);

            if (!vResult.Started)
            {
                vChecks.Add(new ToolCheck(
                    vTool.ToolName, vTool.NeededVersion, null, ToolCheckState.Missing,
                    vFixCommand ? vTool.MacFixCommand : vTool.WindowsFixCommand));
                continue;
            }

            var vFoundVersion = vTool.ParseVersion(vResult.StandardOutput);
            vChecks.Add(new ToolCheck(
                vTool.ToolName,
                vTool.NeededVersion,
                vFoundVersion,
                vFoundVersion is null ? ToolCheckState.Unknown : ToolCheckState.Ready,
                vFoundVersion is null ? (vFixCommand ? vTool.MacFixCommand : vTool.WindowsFixCommand) : null));
        }

        return vChecks;
    }

    /// <summary>
    /// Reads the selected project's own path and derives its tool catalog from what is actually on
    /// disk (REQ-FN-011); falls back to <see cref="ToolProbeCatalog.Default"/> when the project row
    /// or its folder cannot be found, rather than showing an empty table.
    /// </summary>
    private async Task<IReadOnlyList<ToolProbeDefinition>> ResolveProjectCatalogAsync(int aProjectId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        var vProjectPath = await vConnection.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition("SELECT Path FROM Project WHERE ProjectId = @aProjectId;", new { aProjectId }, cancellationToken: aCt))
            .ConfigureAwait(false);

        if (vProjectPath is null)
        {
            return ToolProbeCatalog.Default;
        }

        var vProjectRoot = File.Exists(vProjectPath) ? (Path.GetDirectoryName(vProjectPath) ?? vProjectPath) : vProjectPath;
        if (!Directory.Exists(vProjectRoot))
        {
            return ToolProbeCatalog.Default;
        }

        var vSignals = DetectProjectSignals(vProjectRoot);
        return ToolProbeCatalog.ForProject(vSignals);
    }

    /// <summary>
    /// Reads the project's own files to build the signals <see cref="ToolProbeCatalog.ForProject"/>
    /// derives its tool list from (REQ-FN-011) — real disk IO, kept separate from that pure function
    /// so the derivation itself stays unit testable with no folder at all.
    /// </summary>
    private static ProjectToolSignals DetectProjectSignals(string aProjectRoot)
    {
        var vHasSolution = SafeEnumerateFiles(aProjectRoot, "*.sln").Any();
        var vCsprojFiles = SafeEnumerateFiles(aProjectRoot, "*.csproj").ToList();
        var vHasDotNetProject = vHasSolution || vCsprojFiles.Count > 0;

        var vUsesMaui = vCsprojFiles.Any(vCsproj =>
        {
            try
            {
                return File.ReadAllText(vCsproj).Contains("<UseMaui>true</UseMaui>", StringComparison.OrdinalIgnoreCase);
            }
            catch (IOException)
            {
                return false;
            }
        });

        var vHasNodePackage = SafeEnumerateFiles(aProjectRoot, "package.json").Any();
        var vHasDocker = SafeEnumerateFiles(aProjectRoot, "Dockerfile").Any()
            || SafeEnumerateFiles(aProjectRoot, "docker-compose.yml").Any()
            || SafeEnumerateFiles(aProjectRoot, "docker-compose.yaml").Any();

        return new ProjectToolSignals(vHasDotNetProject, vUsesMaui, vHasNodePackage, vHasDocker);
    }

    /// <summary>
    /// Walks a folder tree for files matching <paramref name="aPattern"/>, skipping the directories a
    /// real project never needs looked inside (<c>bin</c>, <c>obj</c>, <c>node_modules</c>, …) and any
    /// folder this process cannot read — the same shape <c>BuildRunActions.SafeEnumerateFiles</c> uses
    /// for the identical reason (Architecture §4: reading the project's own files, never asking the
    /// owner to describe it).
    /// </summary>
    private static IEnumerable<string> SafeEnumerateFiles(string aRoot, string aPattern)
    {
        var vStack = new Stack<string>();
        vStack.Push(aRoot);

        while (vStack.Count > 0)
        {
            var vDirectory = vStack.Pop();
            IEnumerable<string> vFiles;
            IEnumerable<string> vSubdirectories;
            try
            {
                vFiles = Directory.EnumerateFiles(vDirectory, aPattern);
                vSubdirectories = Directory.EnumerateDirectories(vDirectory).Where(d => !ExcludedDirectoryNames.Contains(Path.GetFileName(d)));
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var vFile in vFiles)
            {
                yield return vFile;
            }

            foreach (var vSubdirectory in vSubdirectories)
            {
                vStack.Push(vSubdirectory);
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// REQ-UI-018 (cluster J). Reads the version and commit the build stamped into the running
    /// process's own assembly attributes (<see cref="AssemblyInformationalVersionAttribute"/>,
    /// <see cref="AssemblyMetadataAttribute"/> — see Directory.Build.props "Version and commit
    /// stamping") rather than a hard-coded string, so a new build always reports itself correctly
    /// with no source change here. <see cref="NewerBuildAsync"/> calls this method too, so the two
    /// checks always agree on what "this Chatur" currently is.
    /// </remarks>
    public Task<ChaturBuildInfo> OwnVersionAsync(CancellationToken aCt = default) =>
        Task.FromResult(ReadOwnBuildInfo());

    /// <inheritdoc />
    /// <remarks>
    /// Honest by construction: <see cref="IGitHubReleaseClient.LatestReleaseAsync"/> returns
    /// <see langword="null"/> for a network failure, a repository that does not exist, and a
    /// repository with no releases yet, and all three fall straight through to <see langword="null"/>
    /// here — never a fabricated "newer build". Compared against <see cref="OwnVersionAsync"/> rather
    /// than a second, separate notion of "current version", so the two always agree on what "this
    /// Chatur" currently is. The actual comparison is <see cref="NightlyRelease.EvaluateOffer"/>, a
    /// pure function kept unit testable without a network or assembly reflection.
    /// </remarks>
    public async Task<ChaturBuildInfo?> NewerBuildAsync(CancellationToken aCt = default)
    {
        var vRelease = await objReleaseClient.LatestReleaseAsync(aCt).ConfigureAwait(false);
        if (vRelease is null)
        {
            return null;
        }

        var vOwn = await OwnVersionAsync(aCt).ConfigureAwait(false);
        return NightlyRelease.EvaluateOffer(vRelease, vOwn, OperatingSystem.IsMacOS());
    }

    /// <summary>
    /// Builds this Chatur's own <see cref="ChaturBuildInfo"/> from the running process's assembly
    /// attributes (REQ-UI-018).
    /// </summary>
    private static ChaturBuildInfo ReadOwnBuildInfo()
    {
        var vAssembly = Assembly.GetEntryAssembly() ?? typeof(PrerequisiteActions).Assembly;

        var vInformationalVersion = vAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "0.0.0";

        // The build appends "+<commit>" to the informational version (Directory.Build.props); strip
        // it back off so Version stays a plain semver string — the commit is reported once, through
        // the AssemblyMetadata read below.
        var vPlusIndex = vInformationalVersion.IndexOf('+');
        var vVersion = vPlusIndex >= 0 ? vInformationalVersion[..vPlusIndex] : vInformationalVersion;

        var vMetadata = vAssembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(aEntry => aEntry.Key is not null)
            .ToDictionary(aEntry => aEntry.Key!, aEntry => aEntry.Value ?? string.Empty);

        var vCommit = vMetadata.GetValueOrDefault("Chatur.CommitSha", "unknown");
        var vBuiltUtc = vMetadata.TryGetValue("Chatur.BuiltUtc", out var vBuiltRaw)
            && DateTime.TryParse(
                vBuiltRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var vParsedBuiltUtc)
                ? vParsedBuiltUtc
                : DateTime.UtcNow;

        return new ChaturBuildInfo(vVersion, vCommit, vBuiltUtc);
    }
}
