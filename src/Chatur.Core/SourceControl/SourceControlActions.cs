using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Dapper;

namespace Chatur.Core.SourceControl;

/// <summary>
/// <see cref="ISourceControlActions"/> over the selected project's own git repository, driven by
/// invoking the <c>git</c> executable as a subprocess (Coding Standards "A model never runs a
/// source-control command" — this class is Chatur's own trusted code, never a tool a model can ask
/// for directly; the guards refuse a model's own source-control request before it ever reaches
/// here). <c>ChangesAsync</c>, <c>DiffAsync</c>, <c>CheckInAsync</c>, <c>PushAsync</c>,
/// <c>PullAsync</c> and <c>StatusAsync</c> are cluster M's (REQ-UI-042 through REQ-UI-044);
/// <c>BranchesAsync</c>, <c>SwitchBranchAsync</c> and <c>RunBranchCheckInAsync</c> are cluster N's
/// (REQ-FN-047, REQ-FN-048).
/// </summary>
public sealed partial class SourceControlActions : ISourceControlActions
{
    /// <summary>Separates the fields of one check-in in the log's output; written for <c>%x1f</c>.</summary>
    private const char FieldSeparator = '\u001f';

    /// <summary>Ends one check-in in the log's output; written for <c>%x1e</c>.</summary>
    private const char RecordSeparator = '\u001e';

    /// <summary>The prefix of every branch a run checks in to by itself (REQ-FN-048).</summary>
    private const string RunBranchPrefix = "run/";

    private readonly IDbConnectionFactory objConnectionFactory;

    /// <summary>
    /// Creates the actions.
    /// </summary>
    /// <param name="aConnectionFactory">Where the project's own path is read from.</param>
    public SourceControlActions(IDbConnectionFactory aConnectionFactory)
    {
        objConnectionFactory = aConnectionFactory;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The project is unknown, or git refused the command.</exception>
    public async Task<IReadOnlyList<ChangedFile>> ChangesAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vWorkingDirectory = await ResolveWorkingDirectoryAsync(aProjectId, aCt).ConfigureAwait(false);

        var vStatusResult = await RunGitAsync(vWorkingDirectory, aCt, "status", "--porcelain=v1", "-z").ConfigureAwait(false);
        vStatusResult.EnsureSuccess("read the changes of");
        var vEntries = ParsePorcelainStatus(vStatusResult.StandardOutput);
        if (vEntries.Count == 0)
        {
            return Array.Empty<ChangedFile>();
        }

        var vCounts = new Dictionary<string, (int Added, int Removed)>(StringComparer.Ordinal);
        if (await HasHeadAsync(vWorkingDirectory, aCt).ConfigureAwait(false))
        {
            var vNumstatResult = await RunGitAsync(vWorkingDirectory, aCt, "diff", "HEAD", "--numstat", "-z").ConfigureAwait(false);
            vNumstatResult.EnsureSuccess("read the changes of");
            vCounts = ParseNumstat(vNumstatResult.StandardOutput);
        }

        var vChanges = new List<ChangedFile>(vEntries.Count);
        foreach (var vEntry in vEntries)
        {
            if (vCounts.TryGetValue(vEntry.Path, out var vCount))
            {
                vChanges.Add(new ChangedFile(vEntry.Path, vEntry.State, vCount.Added, vCount.Removed));
                continue;
            }

            if (vEntry.State == "added")
            {
                var vLines = await CountFileLinesAsync(Path.Combine(vWorkingDirectory, vEntry.Path), aCt).ConfigureAwait(false);
                vChanges.Add(new ChangedFile(vEntry.Path, vEntry.State, vLines, 0));
                continue;
            }

            vChanges.Add(new ChangedFile(vEntry.Path, vEntry.State, 0, 0));
        }

        return vChanges;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The project is unknown, or git refused the command.</exception>
    public async Task<string> DiffAsync(int aProjectId, string aFilePath, CancellationToken aCt = default)
    {
        var vWorkingDirectory = await ResolveWorkingDirectoryAsync(aProjectId, aCt).ConfigureAwait(false);

        var vTrackedResult = await RunGitAsync(vWorkingDirectory, aCt, "ls-files", "--error-unmatch", "--", aFilePath).ConfigureAwait(false);
        var vHasHead = await HasHeadAsync(vWorkingDirectory, aCt).ConfigureAwait(false);

        if (vTrackedResult.ExitCode != 0 || !vHasHead)
        {
            // A new file has no last check-in to compare against — show it as entirely added.
            var vFullPath = Path.Combine(vWorkingDirectory, aFilePath);
            var vText = File.Exists(vFullPath) ? await File.ReadAllTextAsync(vFullPath, aCt).ConfigureAwait(false) : string.Empty;
            var vLines = SplitLines(vText);
            var vHeader = new[] { "--- /dev/null", $"+++ b/{aFilePath}" };
            return string.Join('\n', vHeader.Concat(vLines.Select(aLine => "+" + aLine)));
        }

        var vDiffResult = await RunGitAsync(vWorkingDirectory, aCt, "diff", "HEAD", "--", aFilePath).ConfigureAwait(false);
        vDiffResult.EnsureSuccess($"read the difference for '{aFilePath}' in");
        return vDiffResult.StandardOutput;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">No file was chosen, or the message is blank.</exception>
    /// <exception cref="InvalidOperationException">The project is unknown, or git refused the command.</exception>
    public async Task CheckInAsync(int aProjectId, IReadOnlyList<string> aFilePaths, string aMessage, CancellationToken aCt = default)
    {
        if (aFilePaths.Count == 0)
        {
            throw new ArgumentException("Choose at least one file to check in.", nameof(aFilePaths));
        }

        if (string.IsNullOrWhiteSpace(aMessage))
        {
            throw new ArgumentException("A check-in needs a message.", nameof(aMessage));
        }

        var vWorkingDirectory = await ResolveWorkingDirectoryAsync(aProjectId, aCt).ConfigureAwait(false);

        var vAddArguments = new List<string> { "add", "--" };
        vAddArguments.AddRange(aFilePaths);
        var vAddResult = await RunGitAsync(vWorkingDirectory, aCt, vAddArguments.ToArray()).ConfigureAwait(false);
        vAddResult.EnsureSuccess("stage the chosen files in");

        var vCommitResult = await RunGitAsync(vWorkingDirectory, aCt, "commit", "-m", aMessage).ConfigureAwait(false);
        vCommitResult.EnsureSuccess("check in");
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The project is unknown, or git refused the command.</exception>
    public async Task<SourceControlStatus> PushAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vWorkingDirectory = await ResolveWorkingDirectoryAsync(aProjectId, aCt).ConfigureAwait(false);
        var vPushResult = await RunGitAsync(vWorkingDirectory, aCt, "push").ConfigureAwait(false);
        vPushResult.EnsureSuccess("push");
        return await StatusAsync(aProjectId, aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The project is unknown, or git refused the command.</exception>
    public async Task<SourceControlStatus> PullAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vWorkingDirectory = await ResolveWorkingDirectoryAsync(aProjectId, aCt).ConfigureAwait(false);
        var vPullResult = await RunGitAsync(vWorkingDirectory, aCt, "pull").ConfigureAwait(false);
        vPullResult.EnsureSuccess("pull");
        return await StatusAsync(aProjectId, aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The project is unknown, or git refused the command.</exception>
    public async Task<SourceControlStatus> StatusAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vWorkingDirectory = await ResolveWorkingDirectoryAsync(aProjectId, aCt).ConfigureAwait(false);

        var vBranchResult = await RunGitAsync(vWorkingDirectory, aCt, "branch", "--show-current").ConfigureAwait(false);
        var vBranch = vBranchResult.ExitCode == 0 ? vBranchResult.StandardOutput.Trim() : string.Empty;

        // Refresh the remote-tracking ref first, so "ahead"/"behind" reflects what the server holds
        // right now rather than the last time this repository happened to talk to it. A repository
        // with no remote configured yet simply fails this quietly — there is nothing to fetch.
        await RunGitAsync(vWorkingDirectory, aCt, "fetch", "--quiet").ConfigureAwait(false);

        var vCountResult = await RunGitAsync(vWorkingDirectory, aCt, "rev-list", "--left-right", "--count", "@{upstream}...HEAD").ConfigureAwait(false);
        if (vCountResult.ExitCode != 0)
        {
            // No upstream configured yet (a fresh branch with nothing pushed) — nothing to be ahead or behind of.
            return new SourceControlStatus(vBranch, 0, 0);
        }

        var vParts = vCountResult.StandardOutput.Trim().Split('\t', StringSplitOptions.RemoveEmptyEntries);
        var vBehind = vParts.Length > 0 && int.TryParse(vParts[0], out var vBehindCount) ? vBehindCount : 0;
        var vAhead = vParts.Length > 1 && int.TryParse(vParts[1], out var vAheadCount) ? vAheadCount : 0;

        return new SourceControlStatus(vBranch, vAhead, vBehind);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> BranchesAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vWorkingDirectory = await ResolveWorkingDirectoryAsync(aProjectId, aCt).ConfigureAwait(false);
        var vResult = await RunGitAsync(vWorkingDirectory, aCt, "branch", "--format=%(refname:short)").ConfigureAwait(false);
        vResult.EnsureSuccess("read the branches of");

        return vResult.StandardOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    /// <inheritdoc />
    public async Task SwitchBranchAsync(int aProjectId, string aBranchName, CancellationToken aCt = default)
    {
        var vWorkingDirectory = await ResolveWorkingDirectoryAsync(aProjectId, aCt).ConfigureAwait(false);
        var vResult = await RunGitAsync(vWorkingDirectory, aCt, "checkout", aBranchName).ConfigureAwait(false);
        vResult.EnsureSuccess($"switch to branch '{aBranchName}' of");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommitEntry>> HistoryAsync(int aProjectId, int aCount, CancellationToken aCt = default)
    {
        var vWorkingDirectory = await ResolveWorkingDirectoryAsync(aProjectId, aCt).ConfigureAwait(false);
        if (aCount < 1 || !await HasHeadAsync(vWorkingDirectory, aCt).ConfigureAwait(false))
        {
            return Array.Empty<CommitEntry>();
        }

        // The branch the owner is on plus every run branch, so a check-in only a process's own branch
        // holds shows in the list marked as the process's (REQ-FN-048).
        var vRunBranches = (await BranchesAsync(aProjectId, aCt).ConfigureAwait(false))
            .Where(aBranch => aBranch.StartsWith(RunBranchPrefix, StringComparison.Ordinal));
        var vLogArguments = new List<string> { "log", $"--max-count={aCount}", "--format=%H%x1f%s%x1f%an%x1f%cI%x1e", "HEAD" };
        vLogArguments.AddRange(vRunBranches);
        var vLogResult = await RunGitAsync(vWorkingDirectory, aCt, vLogArguments.ToArray()).ConfigureAwait(false);
        vLogResult.EnsureSuccess("read the history of");

        var vProcessOnly = await ProcessOnlyHashesAsync(vWorkingDirectory, aCt).ConfigureAwait(false);
        return ParseLog(vLogResult.StandardOutput)
            .Select(aEntry => new CommitEntry(aEntry.Hash[..Math.Min(7, aEntry.Hash.Length)], aEntry.Message, aEntry.Author, aEntry.WhenUtc, vProcessOnly.Contains(aEntry.Hash)))
            .ToList();
    }

    /// <inheritdoc />
    /// <remarks>
    /// A run branch counts only the check-ins no other branch holds, so a branch that has been
    /// merged away — nothing left on it to read — is not listed.
    /// </remarks>
    public async Task<IReadOnlyList<ProcessBranch>> ProcessBranchesAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vWorkingDirectory = await ResolveWorkingDirectoryAsync(aProjectId, aCt).ConfigureAwait(false);
        var vBranches = await BranchesAsync(aProjectId, aCt).ConfigureAwait(false);
        var vRunBranches = vBranches.Where(aBranch => aBranch.StartsWith(RunBranchPrefix, StringComparison.Ordinal)).ToList();
        var vOwnerBranches = vBranches.Where(aBranch => !aBranch.StartsWith(RunBranchPrefix, StringComparison.Ordinal)).ToList();

        var vResult = new List<ProcessBranch>();
        foreach (var vBranch in vRunBranches)
        {
            var vArguments = new List<string> { "log", vBranch };
            if (vOwnerBranches.Count > 0)
            {
                vArguments.Add("--not");
                vArguments.AddRange(vOwnerBranches);
            }

            vArguments.Add("--format=%H%x1f%s%x1f%an%x1f%cI%x1e");
            var vLogResult = await RunGitAsync(vWorkingDirectory, aCt, vArguments.ToArray()).ConfigureAwait(false);
            vLogResult.EnsureSuccess($"read the check-ins of branch '{vBranch}' in");

            var vEntries = ParseLog(vLogResult.StandardOutput);
            if (vEntries.Count == 0)
            {
                continue;
            }

            // The log lists newest first; the ids read oldest first so a range reads low to high.
            var vRequirementIds = Enumerable.Reverse(vEntries)
                .SelectMany(aEntry => RequirementIdPattern().Matches(aEntry.Message).Select(aMatch => aMatch.Value))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            vResult.Add(new ProcessBranch(vBranch, vRequirementIds, vEntries.Count, vEntries[0].WhenUtc));
        }

        return vResult.OrderByDescending(aBranch => aBranch.LastCheckInUtc).ToList();
    }

    /// <summary>
    /// The full hashes of the check-ins that only a run branch holds — none of the owner's own
    /// branches has them yet — so the history can mark what a process wrote.
    /// </summary>
    private static async Task<HashSet<string>> ProcessOnlyHashesAsync(string aWorkingDirectory, CancellationToken aCt)
    {
        var vNone = new HashSet<string>(StringComparer.Ordinal);
        var vBranchResult = await RunGitAsync(aWorkingDirectory, aCt, "branch", "--format=%(refname:short)").ConfigureAwait(false);
        if (vBranchResult.ExitCode != 0)
        {
            return vNone;
        }

        var vBranches = vBranchResult.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var vRunBranches = vBranches.Where(aBranch => aBranch.StartsWith(RunBranchPrefix, StringComparison.Ordinal)).ToList();
        var vOwnerBranches = vBranches.Where(aBranch => !aBranch.StartsWith(RunBranchPrefix, StringComparison.Ordinal)).ToList();
        if (vRunBranches.Count == 0 || vOwnerBranches.Count == 0)
        {
            return vNone;
        }

        var vArguments = new List<string> { "rev-list" };
        vArguments.AddRange(vRunBranches);
        vArguments.Add("--not");
        vArguments.AddRange(vOwnerBranches);
        var vResult = await RunGitAsync(aWorkingDirectory, aCt, vArguments.ToArray()).ConfigureAwait(false);
        return vResult.ExitCode == 0
            ? vResult.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.Ordinal)
            : vNone;
    }

    /// <summary>Parses the log's <c>%H%x1f%s%x1f%an%x1f%cI%x1e</c> output, newest first.</summary>
    private static List<LogEntry> ParseLog(string aOutput)
    {
        var vResult = new List<LogEntry>();
        foreach (var vRecord in aOutput.Split(RecordSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var vFields = vRecord.Split(FieldSeparator);
            if (vFields.Length < 4 || !DateTimeOffset.TryParse(vFields[3], CultureInfo.InvariantCulture, DateTimeStyles.None, out var vWhen))
            {
                continue;
            }

            vResult.Add(new LogEntry(vFields[0], vFields[1], vFields[2], vWhen.UtcDateTime));
        }

        return vResult;
    }

    /// <summary>One check-in as the log reports it: its full hash, first message line, author and commit time.</summary>
    private sealed record LogEntry(string Hash, string Message, string Author, DateTime WhenUtc);

    /// <summary>A requirement id as a check-in message writes it, e.g. <c>REQ-FN-048</c>.</summary>
    [GeneratedRegex(@"REQ-[A-Z]+-\d+")]
    private static partial Regex RequirementIdPattern();

    /// <inheritdoc />
    /// <remarks>
    /// Committing straight onto the run's own branch in the main working copy would mean checking
    /// that branch out there, which git refuses whenever an untracked file on disk would collide
    /// with what the target branch already holds — exactly the case here, since the run's edited
    /// files usually are not yet tracked on whatever branch the owner is on. So this method never
    /// touches the main working copy's checked-out branch at all: it adds a second, temporary
    /// working copy of the same repository onto the run's own branch (creating the branch from the
    /// current commit the first time a run uses it), copies just <paramref name="aFilePaths"/> into
    /// it from the main working copy, stages and commits them there with <paramref name="aMessage"/>
    /// — the caller puts the requirement ids in it — and removes the temporary working copy again.
    /// The owner's own branch and working copy are never moved by an automatic run.
    /// </remarks>
    public async Task RunBranchCheckInAsync(int aProjectId, string aBranchName, IReadOnlyList<string> aFilePaths, string aMessage, CancellationToken aCt = default)
    {
        if (aFilePaths is null || aFilePaths.Count == 0)
        {
            throw new ArgumentException("A run's automatic check-in needs at least one file.", nameof(aFilePaths));
        }

        var vWorkingDirectory = await ResolveWorkingDirectoryAsync(aProjectId, aCt).ConfigureAwait(false);
        var vRunWorkingCopy = Path.Combine(Path.GetTempPath(), $"chatur-run-{Guid.NewGuid():N}");

        try
        {
            var vExistingBranches = await BranchesAsync(aProjectId, aCt).ConfigureAwait(false);
            var vAddWorkingCopyResult = vExistingBranches.Contains(aBranchName, StringComparer.Ordinal)
                ? await RunGitAsync(vWorkingDirectory, aCt, "worktree", "add", vRunWorkingCopy, aBranchName).ConfigureAwait(false)
                : await RunGitAsync(vWorkingDirectory, aCt, "worktree", "add", vRunWorkingCopy, "-b", aBranchName).ConfigureAwait(false);
            vAddWorkingCopyResult.EnsureSuccess($"start the run's own branch '{aBranchName}' in");

            foreach (var vFilePath in aFilePaths)
            {
                CopyIntoRunWorkingCopy(vWorkingDirectory, vRunWorkingCopy, vFilePath);
            }

            var vAddArguments = new List<string> { "add", "-A", "--" };
            vAddArguments.AddRange(aFilePaths);
            var vAddResult = await RunGitAsync(vRunWorkingCopy, aCt, vAddArguments.ToArray()).ConfigureAwait(false);
            vAddResult.EnsureSuccess("stage the run's files in");

            var vCommitResult = await RunGitAsync(vRunWorkingCopy, aCt, "commit", "-m", aMessage).ConfigureAwait(false);
            vCommitResult.EnsureSuccess("commit the run's own check-in in");
        }
        finally
        {
            if (Directory.Exists(vRunWorkingCopy))
            {
                await RunGitAsync(vWorkingDirectory, aCt, "worktree", "remove", vRunWorkingCopy, "--force").ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Copies one file the run changed from the main working copy into the run's own temporary
    /// working copy, creating the destination folder as needed; a file the run deleted is removed
    /// from the temporary working copy instead, so the following <c>git add -A</c> stages the
    /// deletion.
    /// </summary>
    private static void CopyIntoRunWorkingCopy(string aMainWorkingDirectory, string aRunWorkingCopy, string aFilePath)
    {
        var vSource = Path.Combine(aMainWorkingDirectory, aFilePath);
        var vDestination = Path.Combine(aRunWorkingCopy, aFilePath);

        if (!File.Exists(vSource))
        {
            if (File.Exists(vDestination))
            {
                File.Delete(vDestination);
            }

            return;
        }

        var vDestinationFolder = Path.GetDirectoryName(vDestination);
        if (!string.IsNullOrEmpty(vDestinationFolder))
        {
            Directory.CreateDirectory(vDestinationFolder);
        }

        File.Copy(vSource, vDestination, overwrite: true);
    }

    /// <summary>
    /// The folder <c>git</c> commands run in for a project: git itself walks up from here to find
    /// the repository root, so a solution-file path resolves to its containing folder and a bare
    /// folder is used as-is.
    /// </summary>
    /// <param name="aProjectId">The project to resolve.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <exception cref="InvalidOperationException">No project with that id exists.</exception>
    private async Task<string> ResolveWorkingDirectoryAsync(int aProjectId, CancellationToken aCt)
    {
        using var vConnection = objConnectionFactory.OpenConnection();
        var vCommand = new CommandDefinition(
            "SELECT Path FROM Project WHERE ProjectId = @ProjectId;",
            new { ProjectId = aProjectId },
            cancellationToken: aCt);
        var vProjectPath = await vConnection.QuerySingleOrDefaultAsync<string>(vCommand).ConfigureAwait(false);
        if (vProjectPath is null)
        {
            throw new InvalidOperationException($"No project with id {aProjectId} exists.");
        }

        return File.Exists(vProjectPath) ? Path.GetDirectoryName(vProjectPath) ?? vProjectPath : vProjectPath;
    }

    /// <summary>Whether the repository has at least one check-in yet (REQ-UI-042).</summary>
    private static async Task<bool> HasHeadAsync(string aWorkingDirectory, CancellationToken aCt)
    {
        var vResult = await RunGitAsync(aWorkingDirectory, aCt, "rev-parse", "--verify", "-q", "HEAD").ConfigureAwait(false);
        return vResult.ExitCode == 0;
    }

    /// <summary>Counts the lines of a file on disk, for a new file's "lines added" figure (REQ-UI-042).</summary>
    private static async Task<int> CountFileLinesAsync(string aFullPath, CancellationToken aCt)
    {
        if (!File.Exists(aFullPath))
        {
            return 0;
        }

        var vText = await File.ReadAllTextAsync(aFullPath, aCt).ConfigureAwait(false);
        return SplitLines(vText).Length;
    }

    /// <summary>
    /// Splits text into lines the way a text file's own line count reads — a trailing newline ends
    /// the last line rather than starting an extra empty one.
    /// </summary>
    private static string[] SplitLines(string aText)
    {
        if (aText.Length == 0)
        {
            return Array.Empty<string>();
        }

        var vNormalized = aText.Replace("\r\n", "\n");
        if (vNormalized.EndsWith('\n'))
        {
            vNormalized = vNormalized[..^1];
        }

        return vNormalized.Length == 0 ? Array.Empty<string>() : vNormalized.Split('\n');
    }

    /// <summary>
    /// Parses <c>git status --porcelain=v1 -z</c> output into a path and a three-state summary
    /// (REQ-UI-042).
    /// </summary>
    private static List<StatusEntry> ParsePorcelainStatus(string aOutput)
    {
        var vResult = new List<StatusEntry>();
        var vTokens = aOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        for (var vIndex = 0; vIndex < vTokens.Length; vIndex++)
        {
            var vToken = vTokens[vIndex];
            if (vToken.Length < 4)
            {
                continue;
            }

            var vX = vToken[0];
            var vY = vToken[1];
            var vPath = vToken[3..];

            if (vX is 'R' or 'C' || vY is 'R' or 'C')
            {
                // A rename or copy carries the original path as its own NUL-terminated field next;
                // Chatur shows the file at the path it has now.
                vIndex++;
            }

            var vState = (vX, vY) switch
            {
                ('?', '?') => "added",
                ('A', _) => "added",
                (_, 'A') => "added",
                ('D', _) => "deleted",
                (_, 'D') => "deleted",
                _ => "modified",
            };

            vResult.Add(new StatusEntry(vPath, vState));
        }

        return vResult;
    }

    /// <summary>
    /// Parses <c>git diff --numstat -z</c> output into added/removed counts per path (REQ-UI-042).
    /// </summary>
    private static Dictionary<string, (int Added, int Removed)> ParseNumstat(string aOutput)
    {
        var vResult = new Dictionary<string, (int Added, int Removed)>(StringComparer.Ordinal);
        var vTokens = aOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        for (var vIndex = 0; vIndex < vTokens.Length; vIndex++)
        {
            var vFields = vTokens[vIndex].Split('\t');
            if (vFields.Length < 3)
            {
                continue;
            }

            var vPath = vFields[2];
            if (vPath.Length == 0 && vIndex + 1 < vTokens.Length)
            {
                // A rename: the old and new paths follow as their own NUL-terminated fields.
                vIndex++;
                vPath = vTokens[vIndex];
            }

            var vAdded = int.TryParse(vFields[0], out var vAddedCount) ? vAddedCount : 0;
            var vRemoved = int.TryParse(vFields[1], out var vRemovedCount) ? vRemovedCount : 0;
            vResult[vPath] = (vAdded, vRemoved);
        }

        return vResult;
    }

    /// <summary>One entry from <c>git status --porcelain</c>: a path and its three-state summary.</summary>
    private readonly record struct StatusEntry(string Path, string State);

    /// <summary>
    /// Runs <c>git</c> as a subprocess in <paramref name="aWorkingDirectory"/> and captures its
    /// output. This is the only place <c>Chatur.Core</c> invokes a source-control command — never a
    /// shell, never a string the caller builds by concatenation.
    /// </summary>
    /// <param name="aWorkingDirectory">The folder to run the command in.</param>
    /// <param name="aCt">A token that stops the process.</param>
    /// <param name="aArguments">Each argument, passed to the process untouched — never joined into one string.</param>
    private static async Task<GitResult> RunGitAsync(string aWorkingDirectory, CancellationToken aCt, params string[] aArguments)
    {
        var vStartInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = aWorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var vArgument in aArguments)
        {
            vStartInfo.ArgumentList.Add(vArgument);
        }

        using var vProcess = new Process { StartInfo = vStartInfo };
        vProcess.Start();

        var vStandardOutputTask = vProcess.StandardOutput.ReadToEndAsync(aCt);
        var vStandardErrorTask = vProcess.StandardError.ReadToEndAsync(aCt);
        await vProcess.WaitForExitAsync(aCt).ConfigureAwait(false);

        return new GitResult(vProcess.ExitCode, await vStandardOutputTask.ConfigureAwait(false), await vStandardErrorTask.ConfigureAwait(false));
    }

    /// <summary>One run of the <c>git</c> executable: its exit code and its two output streams.</summary>
    private sealed record GitResult(int ExitCode, string StandardOutput, string StandardError)
    {
        /// <summary>
        /// Throws with git's own message when the command failed — the owner (or the log, for an
        /// automatic run) sees the tool's own words, never a guess (Architecture §5 "Errors").
        /// </summary>
        /// <param name="aWhatFailed">A verb phrase completing "Could not … the repository.".</param>
        /// <exception cref="InvalidOperationException">The command's exit code was not 0.</exception>
        public void EnsureSuccess(string aWhatFailed)
        {
            if (ExitCode != 0)
            {
                var vDetail = string.IsNullOrWhiteSpace(StandardError) ? StandardOutput : StandardError;
                throw new InvalidOperationException($"Could not {aWhatFailed} the repository: {vDetail.Trim()}");
            }
        }
    }
}
