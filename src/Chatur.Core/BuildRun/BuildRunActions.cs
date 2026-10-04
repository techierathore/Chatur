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
    // it are different scopes. Keyed by ProjectId — one run at a time per project.
    private static readonly ConcurrentDictionary<int, RunState> objRunStates = new();

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

        if (vRows.Count == 0 && !string.IsNullOrEmpty(vProjectPath) && Directory.Exists(vProjectPath))
        {
            vRows = await DiscoverAndStoreTargetsAsync(vConnection, aProjectId, vProjectPath).ConfigureAwait(false);
        }

        return vRows
            .Select(r => new RunTarget(r.RunTargetId, r.Name, r.Platform, IsAvailableOnThisMachine(r.Platform)))
            .ToList();
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

        var (vProjectPath, vSpec) = await LoadTargetAsync(aProjectId, aRunTargetId, aCt).ConfigureAwait(false);

        var vFullPath = Path.Combine(vProjectPath, vSpec.Path);
        var vArguments = $"build \"{vFullPath}\" -c {vSpec.Config}" + (vSpec.Tfm is null ? string.Empty : $" -f {vSpec.Tfm}");
        vState.Publish(vGeneration, new OutputLine($"> dotnet {vArguments}", false));

        var vErrors = new List<BuildError>();
        var vStopwatch = Stopwatch.StartNew();
        int vExitCode;
        try
        {
            vExitCode = await objLauncher.RunAsync(
                "dotnet",
                vArguments,
                vProjectPath,
                aLine =>
                {
                    var vError = ParseBuildError(aLine, vProjectPath);
                    if (vError is not null)
                    {
                        vErrors.Add(vError);
                    }

                    vState.Publish(vGeneration, new OutputLine(aLine, vError is not null));
                },
                vState.LinkedToken(aCt)).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            vState.Publish(vGeneration, new OutputLine("Build stopped.", true));
            vStopwatch.Stop();
            return new BuildResult(false, vStopwatch.Elapsed, vErrors);
        }
        finally
        {
            vStopwatch.Stop();
        }

        var vSucceeded = vExitCode == 0;
        vState.Publish(vGeneration, new OutputLine(
            vSucceeded ? $"Build succeeded in {vStopwatch.Elapsed.TotalSeconds:0.0}s" : $"Build FAILED in {vStopwatch.Elapsed.TotalSeconds:0.0}s",
            !vSucceeded));

        await MarkLastUsedAsync(aProjectId, aRunTargetId, aCt).ConfigureAwait(false);
        return new BuildResult(vSucceeded, vStopwatch.Elapsed, vErrors);
    }

    /// <inheritdoc />
    public Task RunAsync(int aProjectId, int aRunTargetId, CancellationToken aCt = default)
    {
        // Starts and returns — REQ-UI-013 only asks that "the program starts", not that this call
        // waits for it to finish. Cleared synchronously, before the background task's first await, for
        // the same reason as BuildAsync: a caller that subscribes the moment this returns must never
        // read a line left over from the project's last build or run.
        var vState = GetOrCreateState(aProjectId);
        var vGeneration = vState.Begin();

        _ = RunInBackgroundAsync(aProjectId, aRunTargetId, aCt, vState, vGeneration);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(int aProjectId, CancellationToken aCt = default)
    {
        if (objRunStates.TryGetValue(aProjectId, out var vState))
        {
            vState.Publish(vState.CurrentGeneration, new OutputLine("Stopping — ending every process the run started…", false));
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

    private async Task RunInBackgroundAsync(int aProjectId, int aRunTargetId, CancellationToken aCt, RunState aState, int aGeneration)
    {
        var (vProjectPath, vSpec) = await LoadTargetAsync(aProjectId, aRunTargetId, aCt).ConfigureAwait(false);
        var vState = aState;

        var vFullPath = Path.Combine(vProjectPath, vSpec.Path);
        var vArguments = $"run --project \"{vFullPath}\" -c {vSpec.Config} --no-launch-profile" + (vSpec.Tfm is null ? string.Empty : $" -f {vSpec.Tfm}");
        vState.Publish(aGeneration, new OutputLine($"> dotnet {vArguments}", false));

        try
        {
            var vExitCode = await objLauncher.RunAsync(
                "dotnet",
                vArguments,
                vProjectPath,
                aLine => vState.Publish(aGeneration, new OutputLine(aLine, false)),
                vState.LinkedToken(aCt)).ConfigureAwait(false);

            vState.Publish(aGeneration, new OutputLine(
                vExitCode == 0 ? "Process exited." : $"Process exited with code {vExitCode}.",
                vExitCode != 0));
        }
        catch (OperationCanceledException)
        {
            vState.Publish(aGeneration, new OutputLine("Run stopped.", false));
        }
        catch (Exception aException)
        {
            vState.Publish(aGeneration, new OutputLine($"Failed to run: {aException.Message}", true));
        }

        await MarkLastUsedAsync(aProjectId, aRunTargetId, aCt).ConfigureAwait(false);
    }

    private static RunState GetOrCreateState(int aProjectId) =>
        objRunStates.GetOrAdd(aProjectId, _ => new RunState());

    private async Task<(string ProjectPath, TargetSpec Spec)> LoadTargetAsync(int aProjectId, int aRunTargetId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        var vProjectPath = await vConnection.QuerySingleAsync<string>(
            "SELECT Path FROM Project WHERE ProjectId = @aProjectId",
            new { aProjectId }).ConfigureAwait(false);

        var vCommand = await vConnection.QuerySingleAsync<string>(
            "SELECT Command FROM RunTarget WHERE RunTargetId = @aRunTargetId",
            new { aRunTargetId }).ConfigureAwait(false);

        var vSpec = JsonSerializer.Deserialize<TargetSpec>(vCommand) ?? throw new InvalidOperationException("The run target's own command could not be read.");
        return (vProjectPath, vSpec);
    }

    private async Task MarkLastUsedAsync(int aProjectId, int aRunTargetId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        await vConnection.ExecuteAsync(
            "UPDATE RunTarget SET IsLastUsed = CASE WHEN RunTargetId = @aRunTargetId THEN 1 ELSE 0 END WHERE ProjectId = @aProjectId",
            new { aProjectId, aRunTargetId }).ConfigureAwait(false);
    }

    private static async Task<List<RunTargetRow>> DiscoverAndStoreTargetsAsync(System.Data.IDbConnection aConnection, int aProjectId, string aProjectPath)
    {
        var vDiscovered = DiscoverTargets(aProjectPath);
        foreach (var vRow in vDiscovered)
        {
            var vId = await aConnection.ExecuteScalarAsync<long>(
                "INSERT INTO RunTarget (ProjectId, Name, Command, Platform, IsLastUsed) VALUES (@aProjectId, @Name, @Command, @Platform, 0); SELECT last_insert_rowid();",
                new { aProjectId, vRow.Name, vRow.Command, vRow.Platform }).ConfigureAwait(false);
            vRow.RunTargetId = (int)vId;
        }

        return vDiscovered;
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

        foreach (var vCsproj in SafeEnumerateFiles(aProjectPath, "*.csproj"))
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
            var vSln = SafeEnumerateFiles(aProjectPath, "*.sln").FirstOrDefault();
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

        foreach (var vTestProject in vTestProjects)
        {
            var vName = Path.GetFileNameWithoutExtension(vTestProject);
            vResult.Add(NewTargetRow($"{vName} · Tests", vTestProject, aProjectPath, "Debug", null, string.Empty));
        }

        return vResult;
    }

    private static RunTargetRow NewTargetRow(string aName, string aFullPath, string aProjectPath, string aConfig, string? aTfm, string aPlatform)
    {
        var vSpec = new TargetSpec(Path.GetRelativePath(aProjectPath, aFullPath), aConfig, aTfm);
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

    private static IEnumerable<string> SafeEnumerateFiles(string aRoot, string aPattern)
    {
        var vExcluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj", ".git", "node_modules", ".vs", ".idea" };
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
                vSubdirectories = Directory.EnumerateDirectories(vDirectory).Where(d => !vExcluded.Contains(Path.GetFileName(d)));
            }
            catch (UnauthorizedAccessException)
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

    /// <summary>A run target's command, serialised into the <c>RunTarget.Command</c> column.</summary>
    private sealed record TargetSpec(string Path, string Config, string? Tfm);

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
