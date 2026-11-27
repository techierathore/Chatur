using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;

namespace Chatur.Core.BuildRun;

/// <summary>
/// <see cref="IBuildRunActions"/> over the selected project's own build tooling: discovers the
/// targets this machine can build from the project's own <c>.csproj</c>/<c>.sln</c> files, builds and
/// runs them through <see cref="IProcessLauncher"/>, and keeps each project's live output and running
/// process in a process-wide registry so a build or run started by one request can be streamed and
/// stopped by another (Architecture §7 "Build and run"; REQ-UI-011 through REQ-UI-016).
/// </summary>
public sealed class BuildRunActions : IBuildRunActions
{
    // Shared across every scoped instance of this class, for the same reason as
    // Agent.AgentSessionRegistry: the request that starts a run and the request that streams or stops
    // it are different scopes. Keyed by account and ProjectId — one run at a time per project, and
    // two accounts' databases both number their projects from 1, so one account never streams or stops
    // another's run (BRD-216).
    private static readonly ConcurrentDictionary<string, RunState> objRunStates = new();

    private static readonly Regex ErrorLineRegex = new(
        @"^\s*(?<file>[^()\r\n]+?)\((?<line>\d+)(,\d+)?\)\s*:\s*error\s+\S+\s*:\s*(?<message>.+?)(\s*\[.*\])?\s*$",
        RegexOptions.Compiled);

    private static readonly string[] BuildConfigurations = ["Debug", "Release"];

    private readonly IDbConnectionFactory objDb;
    private readonly IProcessLauncher objLauncher;

    /// <summary>
    /// Creates the action set.
    /// </summary>
    /// <param name="aDb">Opens connections to Chatur's own database.</param>
    /// <param name="aLauncher">Runs the actual build and run commands on this machine.</param>
    public BuildRunActions(IDbConnectionFactory aDb, IProcessLauncher aLauncher)
    {
        objDb = aDb;
        objLauncher = aLauncher;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RunTarget>> TargetsAsync(int aProjectId, CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        var vProjectPath = await vConnection.QuerySingleOrDefaultAsync<string>(
            "SELECT Path FROM Project WHERE ProjectId = @aProjectId",
            new { aProjectId }).ConfigureAwait(false);

        var vRows = (await vConnection.QueryAsync<RunTargetRow>(
            "SELECT RunTargetId, Name, Command, Platform, IsLastUsed FROM RunTarget WHERE ProjectId = @aProjectId ORDER BY IsLastUsed DESC, RunTargetId",
            new { aProjectId }).ConfigureAwait(false)).ToList();

        // A solution's path is its .sln file, not a folder: the search used to ask Directory.Exists of it,
        // find nothing, and offer "No target found" (owner, 18 Nov 2026, Xpenser). A solution whose
        // stored targets are only the solution itself was stored by that older search; it is read again.
        var vIsSolution = !string.IsNullOrEmpty(vProjectPath) && File.Exists(vProjectPath) && SolutionProjects.IsSolutionFile(vProjectPath);
        string? vRememberedName = null;
        if (vIsSolution && vRows.Count > 0)
        {
            // Read again when the stored targets are the older search's (the solution itself) or no longer on
            // disk: a branch switch can move a project (Xpenser's saurabhdev keeps them under Source/), and a
            // stale target ran "The provided file path does not exist". The chosen one is kept by its name.
            var vRoot = Path.GetDirectoryName(vProjectPath!)!;
            var vStored = vRows.Select(r => Path.GetFullPath(Path.Combine(vRoot, SpecOf(r).Path))).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var vStale = vRows.All(r => SolutionProjects.IsSolutionFile(SpecOf(r).Path))
                || vRows.Any(r => !File.Exists(Path.Combine(vRoot, SpecOf(r).Path)))
                || SolutionProjects.TestProjects(vProjectPath!).Any(t => !vStored.Contains(t))
                // A project added to the solution since (by New project, an agent or outside Chatur) is offered at once.
                || SolutionProjects.Runnable(vProjectPath!).Any(r => !vStored.Contains(r.Path));
            if (vStale && (SolutionProjects.Runnable(vProjectPath!).Count > 0 || SolutionProjects.TestProjects(vProjectPath!).Count > 0))
            {
                vRememberedName = vRows.FirstOrDefault(r => r.IsLastUsed)?.Name;
                await vConnection.ExecuteAsync("DELETE FROM RunTarget WHERE ProjectId = @aProjectId", new { aProjectId }).ConfigureAwait(false);
                vRows = [];
            }
        }

        if (vRows.Count == 0 && !string.IsNullOrEmpty(vProjectPath))
        {
            if (vIsSolution)
            {
                vRows = await StoreTargetsAsync(vConnection, aProjectId, DiscoverSolutionTargets(vProjectPath!)).ConfigureAwait(false);
                if (vRows.FirstOrDefault(r => r.Name == vRememberedName) is { } vRemembered)
                {
                    await vConnection.ExecuteAsync(
                        "UPDATE RunTarget SET IsLastUsed = CASE WHEN RunTargetId = @RunTargetId THEN 1 ELSE 0 END WHERE ProjectId = @aProjectId",
                        new { vRemembered.RunTargetId, aProjectId }).ConfigureAwait(false);
                    vRemembered.IsLastUsed = true;
                    vRows = [vRemembered, .. vRows.Where(r => r != vRemembered)];
                }
            }
            else if (Directory.Exists(vProjectPath))
            {
                vRows = await StoreTargetsAsync(vConnection, aProjectId, DiscoverTargets(vProjectPath)).ConfigureAwait(false);
            }
        }

        return vRows
            .Select(r => new RunTarget(r.RunTargetId, r.Name, r.Platform, IsAvailableOnThisMachine(r.Platform), SpecOf(r).Path.Replace('\\', '/'), SpecOf(r).Test))
            .ToList();
    }

    /// <inheritdoc />
    public async Task SetRunTargetAsync(int aProjectId, int aRunTargetId, CancellationToken aCt = default)
    {
        // Choosing one project to run ends a Multiple startup choice (REQ-FN-324).
        await SetStartupAsync(aProjectId, [], aCt).ConfigureAwait(false);
        await MarkLastUsedAsync(aProjectId, aRunTargetId, aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> SetRunTargetForProjectFileAsync(int aProjectId, string aProjectFile, CancellationToken aCt = default)
    {
        var vRoot = await FolderOfAsync(aProjectId).ConfigureAwait(false);
        if (vRoot is null || string.IsNullOrWhiteSpace(aProjectFile))
        {
            return false;
        }

        var vRelative = Path.GetRelativePath(vRoot, aProjectFile).Replace('\\', '/');
        var vTarget = (await TargetsAsync(aProjectId, aCt).ConfigureAwait(false))
            .FirstOrDefault(t => t.IsAvailableOnThisMachine && !t.IsTest && string.Equals(t.ProjectFile, vRelative, StringComparison.OrdinalIgnoreCase));
        if (vTarget is null)
        {
            return false;
        }

        await SetRunTargetAsync(aProjectId, vTarget.RunTargetId, aCt).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RunTarget>> StartupAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vFiles = await ReadStartupFilesAsync(aProjectId).ConfigureAwait(false);
        if (vFiles.Count < 2)
        {
            return [];
        }

        var vTargets = await TargetsAsync(aProjectId, aCt).ConfigureAwait(false);
        var vChosen = new List<RunTarget>();
        foreach (var vFile in vFiles)
        {
            var vMatch = vTargets.Where(t => !t.IsTest && string.Equals(t.ProjectFile, vFile, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.IsAvailableOnThisMachine).FirstOrDefault();
            if (vMatch is { IsAvailableOnThisMachine: true })
            {
                vChosen.Add(vMatch);
            }
        }

        return vChosen.Count < 2 ? [] : vChosen;
    }

    /// <inheritdoc />
    public async Task SetStartupAsync(int aProjectId, IReadOnlyList<string> aProjectFiles, CancellationToken aCt = default)
    {
        var vFiles = aProjectFiles.Select(f => f.Replace('\\', '/')).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        using var vConnection = objDb.OpenConnection();
        var vKey = StartupSettingKey(aProjectId);
        if (vFiles.Count < 2)
        {
            await vConnection.ExecuteAsync("DELETE FROM Setting WHERE Key = @vKey", new { vKey }).ConfigureAwait(false);
            return;
        }

        await vConnection.ExecuteAsync(
            "INSERT INTO Setting (Key, Value) VALUES (@vKey, @vValue) ON CONFLICT (Key) DO UPDATE SET Value = excluded.Value",
            new { vKey, vValue = JsonSerializer.Serialize(vFiles) }).ConfigureAwait(false);
    }

    private static string StartupSettingKey(int aProjectId) => $"Startup.Projects.{aProjectId}";

    private async Task<List<string>> ReadStartupFilesAsync(int aProjectId)
    {
        using var vConnection = objDb.OpenConnection();
        var vKey = StartupSettingKey(aProjectId);
        var vValue = await vConnection.QuerySingleOrDefaultAsync<string>("SELECT Value FROM Setting WHERE Key = @vKey", new { vKey }).ConfigureAwait(false);
        if (string.IsNullOrEmpty(vValue))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(vValue) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>The folder a project's targets are relative to: the one around a solution file, or the project's own folder; null when the project is gone.</summary>
    private async Task<string?> FolderOfAsync(int aProjectId)
    {
        using var vConnection = objDb.OpenConnection();
        var vPath = await vConnection.QuerySingleOrDefaultAsync<string>("SELECT Path FROM Project WHERE ProjectId = @aProjectId", new { aProjectId }).ConfigureAwait(false);
        if (string.IsNullOrEmpty(vPath))
        {
            return null;
        }

        return File.Exists(vPath) ? Path.GetDirectoryName(vPath) : vPath;
    }

    private static TargetSpec SpecOf(RunTargetRow aRow)
    {
        try
        {
            return JsonSerializer.Deserialize<TargetSpec>(aRow.Command) ?? new TargetSpec(string.Empty, "Debug", null);
        }
        catch (JsonException)
        {
            return new TargetSpec(string.Empty, "Debug", null);
        }
    }

    /// <inheritdoc />
    public async Task<BuildResult> BuildAsync(int aProjectId, int aRunTargetId, CancellationToken aCt = default)
    {
        // Cleared synchronously, before the first await, so a StreamOutputAsync subscription started
        // the instant this call returns can never read a stale line left over from the last build or
        // run of this same project (REQ-UI-014) — an await here first would leave a window where a
        // fresh subscriber reads the old buffer before this one clears it.
        var vState = GetOrCreateState(aProjectId);
        var vGeneration = vState.Begin();

        string vProjectPath;
        TargetSpec vSpec;
        try
        {
            (vProjectPath, vSpec) = await LoadTargetAsync(aProjectId, aRunTargetId, aCt).ConfigureAwait(false);
        }
        catch (InvalidOperationException aException)
        {
            vState.Publish(vGeneration, new OutputLine($"Build FAILED in 0.0s — {aException.Message}", true));
            return new BuildResult(false, TimeSpan.Zero, []);
        }

        // Several startup projects build together (REQ-FN-324).
        var vStartup = await StartupAsync(aProjectId, aCt).ConfigureAwait(false);
        BuildResult? vResult;
        if (vStartup.Count > 1)
        {
            vResult = await BuildStartupAsync(vState, vGeneration, aProjectId, vStartup, aCt).ConfigureAwait(false);
            if (vResult is { Succeeded: true })
            {
                vState.Publish(vGeneration, new OutputLine($"Build succeeded in {vResult.Duration.TotalSeconds:0.0}s — {vStartup.Count} projects", false));
            }

            await MarkLastUsedAsync(aProjectId, aRunTargetId, aCt).ConfigureAwait(false);
            return vResult ?? new BuildResult(false, TimeSpan.Zero, []);
        }

        vResult = await BuildCoreAsync(vState, vGeneration, vProjectPath, vSpec, aCt).ConfigureAwait(false);
        if (vResult is null)
        {
            return new BuildResult(false, TimeSpan.Zero, []);
        }

        vState.Publish(vGeneration, new OutputLine(
            vResult.Succeeded ? $"Build succeeded in {vResult.Duration.TotalSeconds:0.0}s" : BuildFailedLine(vResult),
            !vResult.Succeeded));

        await MarkLastUsedAsync(aProjectId, aRunTargetId, aCt).ConfigureAwait(false);
        return vResult;
    }

    /// <summary>
    /// Builds each startup project in turn and says so once, like a solution build (REQ-FN-324). The first project that
    /// fails ends it; null when the build was stopped.
    /// </summary>
    private async Task<BuildResult?> BuildStartupAsync(RunState aState, int aGeneration, int aProjectId, IReadOnlyList<RunTarget> aTargets, CancellationToken aCt)
    {
        var vErrors = new List<BuildError>();
        var vStopwatch = Stopwatch.StartNew();
        foreach (var vTarget in aTargets)
        {
            var (vPath, vSpec) = await LoadTargetAsync(aProjectId, vTarget.RunTargetId, aCt).ConfigureAwait(false);
            var vResult = await BuildCoreAsync(aState, aGeneration, vPath, vSpec, aCt).ConfigureAwait(false);
            if (vResult is null)
            {
                return null;
            }

            vErrors.AddRange(vResult.Errors);
            if (!vResult.Succeeded)
            {
                var vFailed = new BuildResult(false, vStopwatch.Elapsed, vErrors);
                aState.Publish(aGeneration, new OutputLine(BuildFailedLine(vFailed), true));
                return vFailed;
            }
        }

        return new BuildResult(true, vStopwatch.Elapsed, vErrors);
    }

    /// <summary>
    /// Builds one target with <c>dotnet build</c>, every line shown as it comes and each compiler error marked
    /// (REQ-UI-012, REQ-UI-107). Returns null when the build was stopped.
    /// </summary>
    private async Task<BuildResult?> BuildCoreAsync(RunState aState, int aGeneration, string aProjectPath, TargetSpec aSpec, CancellationToken aCt)
    {
        var vFullPath = Path.Combine(aProjectPath, aSpec.Path);
        var vArguments = $"build \"{vFullPath}\" -c {aSpec.Config}" + (aSpec.Tfm is null ? string.Empty : $" -f {aSpec.Tfm}");
        aState.Publish(aGeneration, new OutputLine($"> dotnet {vArguments}", false));

        var vErrors = new List<BuildError>();
        var vStopwatch = Stopwatch.StartNew();
        int vExitCode;
        try
        {
            vExitCode = await objLauncher.RunAsync(
                "dotnet",
                vArguments,
                aProjectPath,
                aLine =>
                {
                    var vError = ParseBuildError(aLine, aProjectPath);
                    if (vError is not null && !vErrors.Contains(vError))
                    {
                        vErrors.Add(vError);
                    }

                    aState.Publish(aGeneration, new OutputLine(aLine, vError is not null));
                },
                aState.LinkedToken(aCt)).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            aState.Publish(aGeneration, new OutputLine("Build stopped.", true));
            return null;
        }
        finally
        {
            vStopwatch.Stop();
        }

        return new BuildResult(vExitCode == 0, vStopwatch.Elapsed, vErrors);
    }

    /// <summary>"Build FAILED", how long it took and how many errors, so a failed build says so in words (REQ-UI-108).</summary>
    private static string BuildFailedLine(BuildResult aResult) =>
        $"Build FAILED in {aResult.Duration.TotalSeconds:0.0}s — {aResult.Errors.Count} error{(aResult.Errors.Count == 1 ? string.Empty : "s")}"
        + (aResult.Errors.Count > 0 ? $"; first: {aResult.Errors[0].FilePath}({aResult.Errors[0].Line}): {aResult.Errors[0].Message}" : string.Empty);

    /// <inheritdoc />
    public Task RunAsync(int aProjectId, int aRunTargetId, CancellationToken aCt = default)
    {
        // Starts and returns — REQ-UI-013 only asks that "the program starts", not that this call
        // waits for it to finish. Cleared synchronously, before the background task's first await, for
        // the same reason as BuildAsync: a caller that subscribes the moment this returns must never
        // read a line left over from the project's last build or run.
        var vState = GetOrCreateState(aProjectId);
        var vGeneration = vState.Begin();

        _ = RunGuardedAsync(aProjectId, aRunTargetId, aCt, vState, vGeneration, objDb.CurrentUserId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(int aProjectId, CancellationToken aCt = default)
    {
        if (objRunStates.TryGetValue(StateKey(aProjectId), out var vState))
        {
            vState.Publish(vState.CurrentGeneration, new OutputLine("Stopping — ending every process the run started", false));
            vState.Stop();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<OutputLine> StreamOutputAsync(int aProjectId, [EnumeratorCancellation] CancellationToken aCt = default)
    {
        var vState = GetOrCreateState(aProjectId);
        var vSent = 0;
        var vGeneration = vState.CurrentGeneration;

        while (!aCt.IsCancellationRequested)
        {
            List<OutputLine> vBatch;
            lock (vState.Lock)
            {
                // A new build or run clears the buffer and bumps the generation (RunState.Begin) — an
                // already-open subscriber has to restart from the top, or it would sit forever at an
                // offset past the end of the freshly cleared list and never see the new lines.
                if (vState.Generation != vGeneration)
                {
                    vGeneration = vState.Generation;
                    vSent = 0;
                }

                vBatch = vSent < vState.Lines.Count ? vState.Lines.GetRange(vSent, vState.Lines.Count - vSent) : [];
            }

            foreach (var vLine in vBatch)
            {
                // A subscriber that has been replaced (the Output strip opens a fresh one for every build
                // or run) must not deliver the rest of a batch it read before it was cancelled: that copy
                // of "> dotnet run" landed after the strip was cleared and the line showed twice
                // (MISS-Chatur-20261117-02).
                if (aCt.IsCancellationRequested)
                {
                    yield break;
                }

                vSent++;
                yield return vLine;
            }

            try
            {
                await Task.Delay(150, aCt).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Runs <see cref="RunInBackgroundAsync"/> so that nothing it meets can end it unseen. Root cause of the owner's
    /// evening test (21 Nov 2026, "Chatur said it was running, no output showed anywhere, and Stop only ended it"): the
    /// run was started fire-and-forget, and an exception before the program started (a run target read again and
    /// stored under a new id, a project row no longer there) ended that task with nobody to see it. Output kept
    /// "nothing run yet", no end line was written, so Run stayed busy and only Stop cleared it. Now every such failure
    /// is a "Failed to run:" line in Output, which also ends the run.
    /// </summary>
    private async Task RunGuardedAsync(int aProjectId, int aRunTargetId, CancellationToken aCt, RunState aState, int aGeneration, string? aUserId)
    {
        try
        {
            await RunInBackgroundAsync(aProjectId, aRunTargetId, aCt, aState, aGeneration, aUserId).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            aState.Publish(aGeneration, new OutputLine("Run stopped.", false));
        }
        catch (Exception aException)
        {
            aState.Publish(aGeneration, new OutputLine($"Failed to run: {aException.Message}", true));
        }
    }

    private async Task RunInBackgroundAsync(int aProjectId, int aRunTargetId, CancellationToken aCt, RunState aState, int aGeneration, string? aUserId)
    {
        // The run outlives the request that started it: it stays the starting account's (BRD-216).
        using var vPin = AccountScope.Enter(aUserId);

        // Several startup projects start together (REQ-FN-324).
        var vStartup = await StartupAsync(aProjectId, aCt).ConfigureAwait(false);
        if (vStartup.Count > 1)
        {
            await RunStartupAsync(aProjectId, aRunTargetId, vStartup, aCt, aState, aGeneration).ConfigureAwait(false);
            return;
        }

        var (vProjectPath, vSpec) = await LoadTargetAsync(aProjectId, aRunTargetId, aCt).ConfigureAwait(false);
        var vState = aState;

        // Run builds first, in the open, then runs what it built (REQ-UI-107, owner's evening test of 21 Nov 2026):
        // `dotnet run` alone builds silently, so a first build showed nothing but its own command line for as long as
        // it took, and a failed one was only the program's exit.
        var vBuild = await BuildCoreAsync(vState, aGeneration, vProjectPath, vSpec, aCt).ConfigureAwait(false);
        if (vBuild is null)
        {
            return;
        }

        if (!vBuild.Succeeded)
        {
            vState.Publish(aGeneration, new OutputLine(BuildFailedLine(vBuild) + ". The program was not started.", true));
            await MarkLastUsedAsync(aProjectId, aRunTargetId, aCt).ConfigureAwait(false);
            return;
        }

        vState.Publish(aGeneration, new OutputLine($"Built in {vBuild.Duration.TotalSeconds:0.0}s — starting it", false));

        var vFullPath = Path.Combine(vProjectPath, vSpec.Path);
        if (SolutionProjects.IsSolutionFile(vFullPath))
        {
            vState.Publish(aGeneration, new OutputLine("A solution builds but does not run by itself: choose one of its projects to run.", true));
            await MarkLastUsedAsync(aProjectId, aRunTargetId, aCt).ConfigureAwait(false);
            return;
        }

        await RunProgramAsync(vState, aGeneration, vProjectPath, vSpec, null, aCt).ConfigureAwait(false);
        await MarkLastUsedAsync(aProjectId, aRunTargetId, aCt).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds every startup project, then starts them all together, each line of each program shown with the project's
    /// name before it, the way Visual Studio runs several startup projects (REQ-FN-324). Stop ends them all.
    /// </summary>
    private async Task RunStartupAsync(int aProjectId, int aRunTargetId, IReadOnlyList<RunTarget> aTargets, CancellationToken aCt, RunState aState, int aGeneration)
    {
        var vBuild = await BuildStartupAsync(aState, aGeneration, aProjectId, aTargets, aCt).ConfigureAwait(false);
        if (vBuild is null)
        {
            return;
        }

        if (!vBuild.Succeeded)
        {
            aState.Publish(aGeneration, new OutputLine("The programs were not started.", true));
            return;
        }

        var vRuns = new List<Task>();
        foreach (var vTarget in aTargets)
        {
            var (vProjectPath, vSpec) = await LoadTargetAsync(aProjectId, vTarget.RunTargetId, aCt).ConfigureAwait(false);
            var vName = Path.GetFileNameWithoutExtension(vSpec.Path);
            vRuns.Add(RunProgramAsync(aState, aGeneration, vProjectPath, vSpec, vName, aCt));
        }

        aState.Publish(aGeneration, new OutputLine($"Starting {aTargets.Count} projects together: {string.Join(", ", aTargets.Select(t => Path.GetFileNameWithoutExtension(t.ProjectFile)))}", false));
        await Task.WhenAll(vRuns).ConfigureAwait(false);
        aState.Publish(aGeneration, new OutputLine($"All {aTargets.Count} projects ended.", false));
        await MarkLastUsedAsync(aProjectId, aRunTargetId, aCt).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs one built project (or its tests) and shows its lines until it ends, its exit code always among them
    /// (REQ-UI-108). With a <paramref name="aTag"/> every line is shown as <c>[tag] line</c>, for several projects at once.
    /// </summary>
    private async Task RunProgramAsync(RunState aState, int aGeneration, string aProjectPath, TargetSpec aSpec, string? aTag, CancellationToken aCt)
    {
        var vFullPath = Path.Combine(aProjectPath, aSpec.Path);
        var vPrefix = aTag is null ? string.Empty : $"[{aTag}] ";
        var vProfile = aSpec.Test ? null : LaunchSettings.ProjectProfile(vFullPath);
        var vIsWeb = !aSpec.Test && IsWebProjectFile(vFullPath);
        var vTfm = aSpec.Tfm is null ? string.Empty : $" -f {aSpec.Tfm}";
        var vArguments = aSpec.Test
            ? $"test \"{vFullPath}\" -c {aSpec.Config} --no-build{vTfm}"
            : $"run --project \"{vFullPath}\" -c {aSpec.Config} --no-build"
                + (vProfile is null ? " --no-launch-profile" : $" --launch-profile \"{vProfile.Name}\"") + vTfm;
        aState.Publish(aGeneration, new OutputLine($"{vPrefix}> dotnet {vArguments}", false));

        var vBrowserOpened = 0;
        var vLevels = new ProgramLineLevels();
        try
        {
            var vExitCode = await objLauncher.RunAsync(
                "dotnet",
                vArguments,
                aProjectPath,
                aLine =>
                {
                    // A line is an error by its own level, never because it came on standard error (REQ-UI-107, BRD-235).
                    var vLevel = vLevels.Read(aLine);
                    aState.Publish(aGeneration, new OutputLine(vPrefix + aLine, vLevel == ProgramLineLevel.Error, vLevel == ProgramLineLevel.Warning));

                    // A web project opens in the browser once it says it is listening (REQ-FN-306), once per run.
                    if ((vIsWeb || vProfile?.LaunchBrowser == true)
                        && LaunchSettings.ListeningAddress(aLine) is { } vListening
                        && Interlocked.Exchange(ref vBrowserOpened, 1) == 0)
                    {
                        var vAddress = LaunchSettings.BrowserAddress(vProfile, vListening);
                        aState.Publish(aGeneration, new OutputLine($"{vPrefix}Opening {vAddress} in the browser", false));
                        _ = objLauncher.OpenBrowserAsync(vAddress, CancellationToken.None);
                    }
                },
                aState.LinkedToken(aCt)).ConfigureAwait(false);

            // The exit code is always shown, 0 included (REQ-UI-108).
            aState.Publish(aGeneration, new OutputLine($"{vPrefix}Process exited with code {vExitCode}.", vExitCode != 0));
        }
        catch (OperationCanceledException)
        {
            aState.Publish(aGeneration, new OutputLine($"{vPrefix}Run stopped.", false));
        }
        catch (Exception aException)
        {
            aState.Publish(aGeneration, new OutputLine($"{vPrefix}Failed to run: {aException.Message}", true));
        }
    }

    /// <summary>Whether the project file is a web project, read from its SDK; a project that cannot be read is not.</summary>
    private static bool IsWebProjectFile(string aProjectFile)
    {
        try
        {
            return File.Exists(aProjectFile) && LaunchSettings.IsWebProject(File.ReadAllText(aProjectFile));
        }
        catch (Exception aException) when (aException is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private RunState GetOrCreateState(int aProjectId) =>
        objRunStates.GetOrAdd(StateKey(aProjectId), _ => new RunState());

    private string StateKey(int aProjectId) => $"{objDb.AccountKey}|{aProjectId}";

    private async Task<(string ProjectPath, TargetSpec Spec)> LoadTargetAsync(int aProjectId, int aRunTargetId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        var vProjectPath = await vConnection.QuerySingleOrDefaultAsync<string>(
            "SELECT Path FROM Project WHERE ProjectId = @aProjectId",
            new { aProjectId }).ConfigureAwait(false)
            ?? throw new InvalidOperationException("this project is no longer in your list. Open it again from File.");

        var vCommand = await vConnection.QuerySingleOrDefaultAsync<string>(
            "SELECT Command FROM RunTarget WHERE RunTargetId = @aRunTargetId AND ProjectId = @aProjectId",
            new { aRunTargetId, aProjectId }).ConfigureAwait(false)
            ?? throw new InvalidOperationException("the chosen target is no longer one of this project's: its targets were read again. Choose it in the target picker and press Run again.");

        TargetSpec? vSpec;
        try
        {
            vSpec = JsonSerializer.Deserialize<TargetSpec>(vCommand);
        }
        catch (JsonException)
        {
            vSpec = null;
        }

        if (vSpec is null)
        {
            throw new InvalidOperationException("the run target's own command could not be read. Choose the target again.");
        }

        // A target's path is relative to the folder around a solution file, never to the file itself.
        var vRoot = File.Exists(vProjectPath) ? Path.GetDirectoryName(vProjectPath) ?? vProjectPath : vProjectPath;
        if (!Directory.Exists(vRoot))
        {
            throw new InvalidOperationException($"the project's folder is not there any more: {vRoot}");
        }

        return (vRoot, vSpec);
    }

    private async Task MarkLastUsedAsync(int aProjectId, int aRunTargetId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        await vConnection.ExecuteAsync(
            "UPDATE RunTarget SET IsLastUsed = CASE WHEN RunTargetId = @aRunTargetId THEN 1 ELSE 0 END WHERE ProjectId = @aProjectId",
            new { aProjectId, aRunTargetId }).ConfigureAwait(false);
    }

    private static async Task<List<RunTargetRow>> StoreTargetsAsync(System.Data.IDbConnection aConnection, int aProjectId, List<RunTargetRow> aDiscovered)
    {
        foreach (var vRow in aDiscovered)
        {
            var vId = await aConnection.ExecuteScalarAsync<long>(
                "INSERT INTO RunTarget (ProjectId, Name, Command, Platform, IsLastUsed) VALUES (@aProjectId, @Name, @Command, @Platform, 0); SELECT last_insert_rowid();",
                new { aProjectId, vRow.Name, vRow.Command, vRow.Platform }).ConfigureAwait(false);
            vRow.RunTargetId = (int)vId;
        }

        return aDiscovered;
    }

    /// <summary>
    /// A solution's targets (BRD-227): each runnable project it lists, by name, once per platform for a
    /// project that targets several; with none runnable, the solution itself, so Build still works.
    /// </summary>
    private static List<RunTargetRow> DiscoverSolutionTargets(string aSolutionPath)
    {
        var vRoot = Path.GetDirectoryName(aSolutionPath)!;
        var vResult = new List<RunTargetRow>();
        foreach (var vProject in SolutionProjects.Runnable(aSolutionPath))
        {
            var vName = Path.GetFileNameWithoutExtension(vProject.Path);
            var vPlatformTfms = vProject.TargetFrameworks.Where(t => PlatformFor(t).Platform.Length > 0).ToList();
            if (vPlatformTfms.Count == 0)
            {
                vResult.Add(NewTargetRow(vName, vProject.Path, vRoot, "Debug", vProject.TargetFrameworks.Count > 1 ? vProject.TargetFrameworks[0] : null, string.Empty));
                continue;
            }

            foreach (var vTfm in vPlatformTfms)
            {
                var (vPlatform, vLabel) = PlatformFor(vTfm);
                vResult.Add(NewTargetRow($"{vName} · {vLabel}", vProject.Path, vRoot, "Debug", vTfm, vPlatform));
            }
        }

        // A class library is tested through its test project: running that project runs its tests (REQ-UI-107).
        foreach (var vTests in SolutionProjects.TestProjects(aSolutionPath))
        {
            vResult.Add(NewTargetRow(Path.GetFileNameWithoutExtension(vTests), vTests, vRoot, "Debug", null, string.Empty, true));
        }

        if (vResult.Count == 0)
        {
            foreach (var vConfig in BuildConfigurations)
            {
                vResult.Add(NewTargetRow($"{Path.GetFileNameWithoutExtension(aSolutionPath)} · {vConfig}", aSolutionPath, vRoot, vConfig, null, string.Empty));
            }
        }

        return vResult;
    }

    /// <summary>
    /// Finds the targets a project offers by reading its own <c>.csproj</c>/<c>.sln</c> files — never
    /// by asking the owner to describe the project, since the files already say what it is.
    /// </summary>
    private static List<RunTargetRow> DiscoverTargets(string aProjectPath)
    {
        var vResult = new List<RunTargetRow>();
        var vTestProjects = new List<string>();
        var vPlainProjects = new List<string>();
        string? vMultiTfmProject = null;
        string[] vTfms = [];

        foreach (var vCsproj in ProjectToolchain.EnumerateFiles(aProjectPath, "*.csproj"))
        {
            string vText;
            try
            {
                vText = File.ReadAllText(vCsproj);
            }
            catch (IOException)
            {
                continue;
            }

            if (vText.Contains("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase))
            {
                vTestProjects.Add(vCsproj);
                continue;
            }

            var vMultiMatch = Regex.Match(vText, "<TargetFrameworks>([^<]+)</TargetFrameworks>");
            if (vMultiMatch.Success && vMultiTfmProject is null)
            {
                vMultiTfmProject = vCsproj;
                vTfms = vMultiMatch.Groups[1].Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
            else
            {
                vPlainProjects.Add(vCsproj);
            }
        }

        if (vMultiTfmProject is not null)
        {
            foreach (var vTfm in vTfms)
            {
                var (vPlatform, vLabel) = PlatformFor(vTfm);
                foreach (var vConfig in BuildConfigurations)
                {
                    vResult.Add(NewTargetRow($"{vLabel} · {vConfig}", vMultiTfmProject, aProjectPath, vConfig, vTfm, vPlatform));
                }
            }
        }
        else
        {
            var vSln = ProjectToolchain.EnumerateFiles(aProjectPath, "*.sln").FirstOrDefault();
            if (vSln is not null)
            {
                foreach (var vConfig in BuildConfigurations)
                {
                    vResult.Add(NewTargetRow($"{Path.GetFileNameWithoutExtension(vSln)} · {vConfig}", vSln, aProjectPath, vConfig, null, string.Empty));
                }
            }
            else if (vPlainProjects.Count > 0)
            {
                var vCsproj = vPlainProjects[0];
                var vName = Path.GetFileNameWithoutExtension(vCsproj);
                foreach (var vConfig in BuildConfigurations)
                {
                    vResult.Add(NewTargetRow($"{vName} · {vConfig}", vCsproj, aProjectPath, vConfig, null, string.Empty));
                }
            }
        }

        // A folder can hold additional head projects alongside the main multi-target project. Each
        // head is listed once by its project file name so the Workbench target menu matches the
        // mockup (Web harness, Tests, …).
        if (vMultiTfmProject is not null)
        {
            foreach (var vPlainProject in vPlainProjects)
            {
                var vName = Path.GetFileNameWithoutExtension(vPlainProject);
                vResult.Add(NewTargetRow(vName, vPlainProject, aProjectPath, "Debug", null, string.Empty));
            }
        }

        foreach (var vTestProject in vTestProjects)
        {
            var vName = Path.GetFileNameWithoutExtension(vTestProject);
            vResult.Add(NewTargetRow(vName, vTestProject, aProjectPath, "Debug", null, string.Empty, true));
        }

        return vResult;
    }

    private static RunTargetRow NewTargetRow(string aName, string aFullPath, string aProjectPath, string aConfig, string? aTfm, string aPlatform, bool aTest = false)
    {
        var vSpec = new TargetSpec(Path.GetRelativePath(aProjectPath, aFullPath), aConfig, aTfm, aTest);
        return new RunTargetRow
        {
            RunTargetId = 0,
            Name = aName,
            Command = JsonSerializer.Serialize(vSpec),
            Platform = aPlatform,
            IsLastUsed = false
        };
    }

    private static (string Platform, string Label) PlatformFor(string aTfm)
    {
        if (aTfm.Contains("windows", StringComparison.OrdinalIgnoreCase))
        {
            return ("Windows", "Windows");
        }

        if (aTfm.Contains("maccatalyst", StringComparison.OrdinalIgnoreCase))
        {
            return ("MacCatalyst", "Mac Catalyst");
        }

        if (aTfm.Contains("-ios", StringComparison.OrdinalIgnoreCase))
        {
            return ("iOS", "iOS");
        }

        if (aTfm.Contains("-android", StringComparison.OrdinalIgnoreCase))
        {
            return ("Android", "Android");
        }

        return (string.Empty, aTfm);
    }

    /// <summary>Whether this machine can build a target for the given platform (REQ-UI-011).</summary>
    private static bool IsAvailableOnThisMachine(string aPlatform) => aPlatform switch
    {
        "Windows" => RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
        "MacCatalyst" => RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
        "iOS" => RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
        _ => true
    };

    private static BuildError? ParseBuildError(string aLine, string aProjectPath)
    {
        var vMatch = ErrorLineRegex.Match(aLine);
        if (!vMatch.Success)
        {
            return null;
        }

        var vFile = vMatch.Groups["file"].Value.Trim();
        if (Path.IsPathRooted(vFile))
        {
            try
            {
                vFile = Path.GetRelativePath(aProjectPath, vFile);
            }
            catch (ArgumentException)
            {
                // Keep the rooted path when it is not under the project (e.g. an SDK-generated file).
            }
        }

        var vLine = int.Parse(vMatch.Groups["line"].Value);
        return new BuildError(vFile.Replace('\\', '/'), vLine, vMatch.Groups["message"].Value.Trim());
    }

    /// <summary>A run target's command, serialised into the <c>RunTarget.Command</c> column.</summary>
    private sealed record TargetSpec(string Path, string Config, string? Tfm, bool Test = false);

    /// <summary>A mutable projection of a <c>RunTarget</c> row, so a newly discovered row can be given its id after the insert.</summary>
    private sealed class RunTargetRow
    {
        public int RunTargetId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Command { get; set; } = string.Empty;

        public string Platform { get; set; } = string.Empty;

        public bool IsLastUsed { get; set; }
    }

    /// <summary>
    /// One project's live build/run output and the cancellation for whatever is producing it. Shared
    /// across requests through <see cref="objRunStates"/> (REQ-UI-014, REQ-UI-015).
    /// </summary>
    private sealed class RunState
    {
        public object Lock { get; } = new();

        public List<OutputLine> Lines { get; } = [];

        /// <summary>Bumped every time <see cref="Begin"/> clears <see cref="Lines"/>, so a subscriber already reading them knows to restart from the top.</summary>
        public int Generation { get; private set; }

        private CancellationTokenSource objSource = new();

        /// <summary>
        /// Clears the buffer for a fresh build or run, cancels whatever the previous one was still
        /// doing, and returns the new generation number for <see cref="Publish"/> to check against
        /// later.
        /// </summary>
        public int Begin()
        {
            CancellationTokenSource vOldSource;
            int vGeneration;
            lock (Lock)
            {
                Lines.Clear();
                Generation++;
                vGeneration = Generation;
                vOldSource = objSource;
                objSource = new CancellationTokenSource();
            }

            vOldSource.Cancel();
            vOldSource.Dispose();
            return vGeneration;
        }

        public CancellationToken LinkedToken(CancellationToken aCt)
        {
            lock (Lock)
            {
                return CancellationTokenSource.CreateLinkedTokenSource(objSource.Token, aCt).Token;
            }
        }

        /// <summary>
        /// Appends a line, but only when <paramref name="aGeneration"/> is still the current one — a
        /// build or run that a newer one has already superseded (its process took a moment to actually
        /// unwind after being cancelled) must never append its trailing lines into the new run's
        /// buffer (REQ-UI-014, REQ-UI-015).
        /// </summary>
        /// <param name="aGeneration">The generation number the caller captured from <see cref="Begin"/> when its own build or run started.</param>
        /// <param name="aLine">The line to append.</param>
        public void Publish(int aGeneration, OutputLine aLine)
        {
            lock (Lock)
            {
                if (Generation == aGeneration)
                {
                    Lines.Add(aLine);
                }
            }
        }

        /// <summary>The generation currently accepting lines, for a caller that only intends "whatever is running right now" rather than one it started itself.</summary>
        public int CurrentGeneration
        {
            get
            {
                lock (Lock)
                {
                    return Generation;
                }
            }
        }

        public void Stop()
        {
            lock (Lock)
            {
                objSource.Cancel();
            }
        }
    }
}
