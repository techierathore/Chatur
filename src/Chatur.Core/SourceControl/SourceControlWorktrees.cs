using System.Globalization;
using Chatur.Core.Actions;
using Dapper;

namespace Chatur.Core.SourceControl;

/// <summary>
/// The working-copy half of <see cref="SourceControlActions"/> (REQ-FN-079, REQ-FN-080, REQ-UI-068): a second folder of the
/// repository on its own branch, made and removed with <c>git worktree</c>. A copy a run works in is recorded as a project
/// row no list shows, so the run machinery, which reads and writes a project's folder, works inside the copy unchanged.
/// </summary>
public sealed partial class SourceControlActions : IWorktreeActions
{
    private const string WorktreeRunningState = "running";
    private const string WorktreeWaitingState = "waiting";

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkingCopy>> ListWorkingCopiesAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vWorkingDirectory = await ResolveWorkingDirectoryAsync(aProjectId, aCt).ConfigureAwait(false);
        var vEntries = await ReadWorktreesAsync(vWorkingDirectory, aCt).ConfigureAwait(false);
        var vCopies = new List<WorkingCopy>(vEntries.Count);
        for (var vIndex = 0; vIndex < vEntries.Count; vIndex++)
        {
            var vEntry = vEntries[vIndex];
            if (!Directory.Exists(vEntry.Path))
            {
                continue;
            }

            var vMade = Directory.GetCreationTimeUtc(vEntry.Path);
            if (vIndex == 0)
            {
                vCopies.Add(new WorkingCopy(vEntry.Path, vEntry.Branch, true, null, null, "in use", vMade));
                continue;
            }

            var vRun = await NewestRunInAsync(vEntry.Path, aCt).ConfigureAwait(false);
            var vState = vRun is null ? "free" : vRun.State is WorktreeRunningState or WorktreeWaitingState ? "in use" : "finished";
            vCopies.Add(new WorkingCopy(vEntry.Path, vEntry.Branch, false, vRun?.RunId, vRun?.ProcessCode, vState, vMade));
        }

        return vCopies;
    }

    /// <inheritdoc />
    public async Task<WorkingCopyChoices> WorkingCopyChoicesAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vWorkingDirectory = await ResolveWorkingDirectoryAsync(aProjectId, aCt).ConfigureAwait(false);
        var vEntries = await ReadWorktreesAsync(vWorkingDirectory, aCt).ConfigureAwait(false);
        var vTaken = vEntries.Select(aEntry => aEntry.Branch).ToHashSet(StringComparer.Ordinal);
        var vBranches = (await BranchesAsync(aProjectId, aCt).ConfigureAwait(false)).Where(aBranch => !vTaken.Contains(aBranch)).ToList();
        var vRoot = vEntries[0].Path;
        var vParent = Path.GetDirectoryName(vRoot.TrimEnd(Path.DirectorySeparatorChar, '/')) ?? vRoot;
        return new WorkingCopyChoices(vBranches, Path.Combine(vParent, ".chatur", "wt"));
    }

    /// <inheritdoc />
    public async Task<WorkingCopy> AddWorkingCopyAsync(int aProjectId, string aBranch, string aFolder, bool aNewBranch, CancellationToken aCt = default)
    {
        if (string.IsNullOrWhiteSpace(aBranch))
        {
            throw new InvalidOperationException("Choose a branch for the copy.");
        }

        if (string.IsNullOrWhiteSpace(aFolder))
        {
            throw new InvalidOperationException("Name a folder for the copy.");
        }

        var vWorkingDirectory = await ResolveWorkingDirectoryAsync(aProjectId, aCt).ConfigureAwait(false);
        var vMain = (await ReadWorktreesAsync(vWorkingDirectory, aCt).ConfigureAwait(false))[0].Path;
        var vFolder = Path.GetFullPath(aFolder.Trim());
        if (SamePath(vFolder, vMain) || IsInside(vFolder, vMain))
        {
            throw new InvalidOperationException($"The folder must be outside {vMain}.");
        }

        if (Directory.Exists(vFolder) && Directory.EnumerateFileSystemEntries(vFolder).Any())
        {
            throw new InvalidOperationException($"{vFolder} is not empty. Choose an empty folder.");
        }

        if (File.Exists(vFolder))
        {
            throw new InvalidOperationException($"{vFolder} is a file. Choose a folder.");
        }

        var vBranch = aBranch.Trim();
        var vResult = aNewBranch
            ? await RunGitAsync(vWorkingDirectory, aCt, "worktree", "add", "-b", vBranch, vFolder).ConfigureAwait(false)
            : await RunGitAsync(vWorkingDirectory, aCt, "worktree", "add", vFolder, vBranch).ConfigureAwait(false);
        vResult.EnsureSuccess($"add a working copy of '{vBranch}' to");

        await WorkingCopyProjectAsync(aProjectId, vFolder, aCt).ConfigureAwait(false);
        var vAdded = (await ListWorkingCopiesAsync(aProjectId, aCt).ConfigureAwait(false)).First(aCopy => SamePath(aCopy.Path, vFolder));
        return vAdded;
    }

    /// <inheritdoc />
    public async Task RemoveWorkingCopyAsync(int aProjectId, string aFolder, CancellationToken aCt = default)
    {
        var vCopies = await ListWorkingCopiesAsync(aProjectId, aCt).ConfigureAwait(false);
        var vCopy = vCopies.FirstOrDefault(aCopy => SamePath(aCopy.Path, aFolder))
            ?? throw new InvalidOperationException($"{aFolder} is not a working copy of this project.");
        if (vCopy.IsMain)
        {
            throw new InvalidOperationException("This is the folder you are working in. It cannot be removed here.");
        }

        if (vCopy.State == "in use")
        {
            throw new InvalidOperationException($"Run #{vCopy.RunId} is working in this copy. Stop the run first, then remove it.");
        }

        var vWorkingDirectory = await ResolveWorkingDirectoryAsync(aProjectId, aCt).ConfigureAwait(false);
        var vResult = await RunGitAsync(vWorkingDirectory, aCt, "worktree", "remove", "--force", vCopy.Path).ConfigureAwait(false);
        vResult.EnsureSuccess("remove the working copy from");

        using var vConnection = objConnectionFactory.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            """
            DELETE FROM Project
            WHERE Path = @Path AND RemovedUtc IS NOT NULL AND ProjectFolderId IS NULL
              AND NOT EXISTS (SELECT 1 FROM ProcessRun r WHERE r.ProjectId = Project.ProjectId);
            """,
            new { vCopy.Path },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> WorkingCopyProjectAsync(int aProjectId, string aFolder, CancellationToken aCt = default)
    {
        var vCopies = await ListWorkingCopiesAsync(aProjectId, aCt).ConfigureAwait(false);
        var vCopy = vCopies.FirstOrDefault(aCopy => !aCopy.IsMain && SamePath(aCopy.Path, aFolder))
            ?? throw new InvalidOperationException($"{aFolder} is not a working copy of this project.");

        using var vConnection = objConnectionFactory.OpenConnection();
        var vExisting = await vConnection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(
            "SELECT ProjectId FROM Project WHERE Path = @Path;", new { vCopy.Path }, cancellationToken: aCt)).ConfigureAwait(false);
        if (vExisting is { } vId)
        {
            return vId;
        }

        return await vConnection.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            INSERT INTO Project (ProjectFolderId, Name, Path, Kind, LastOpenedUtc, RemovedUtc)
            VALUES (NULL, @Name, @Path, 'Folder', NULL, @Now);
            SELECT last_insert_rowid();
            """,
            new { Name = System.IO.Path.GetFileName(vCopy.Path.TrimEnd(System.IO.Path.DirectorySeparatorChar, '/')), vCopy.Path, Now = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<long> WorkingCopyBytesAsync(string aFolder, CancellationToken aCt = default) =>
        Task.Run(
            () =>
            {
                long vTotal = 0;
                if (!Directory.Exists(aFolder))
                {
                    return vTotal;
                }

                var vOptions = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint };
                foreach (var vFile in new DirectoryInfo(aFolder).EnumerateFiles("*", vOptions))
                {
                    aCt.ThrowIfCancellationRequested();
                    vTotal += vFile.Length;
                }

                return vTotal;
            },
            aCt);

    private async Task<WorktreeRunRow?> NewestRunInAsync(string aPath, CancellationToken aCt)
    {
        using var vConnection = objConnectionFactory.OpenConnection();
        return await vConnection.QueryFirstOrDefaultAsync<WorktreeRunRow>(new CommandDefinition(
            "SELECT r.RunId, r.ProcessCode, r.State FROM ProcessRun r JOIN Project p ON p.ProjectId = r.ProjectId WHERE p.Path = @aPath ORDER BY r.RunId DESC LIMIT 1;",
            new { aPath },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <summary>The worktrees git lists for the repository, the main one first.</summary>
    private static async Task<List<WorktreeEntry>> ReadWorktreesAsync(string aWorkingDirectory, CancellationToken aCt)
    {
        var vResult = await RunGitAsync(aWorkingDirectory, aCt, "worktree", "list", "--porcelain").ConfigureAwait(false);
        vResult.EnsureSuccess("list the working copies of");
        var vEntries = new List<WorktreeEntry>();
        string? vPath = null;
        var vBranch = string.Empty;
        void Flush()
        {
            if (vPath is not null)
            {
                vEntries.Add(new WorktreeEntry(System.IO.Path.GetFullPath(vPath), vBranch));
            }

            vPath = null;
            vBranch = string.Empty;
        }

        foreach (var vLine in vResult.StandardOutput.Split('\n'))
        {
            var vText = vLine.TrimEnd('\r');
            if (vText.StartsWith("worktree ", StringComparison.Ordinal))
            {
                Flush();
                vPath = vText["worktree ".Length..];
            }
            else if (vText.StartsWith("branch refs/heads/", StringComparison.Ordinal))
            {
                vBranch = vText["branch refs/heads/".Length..];
            }
        }

        Flush();
        return vEntries;
    }

    private static bool SamePath(string aLeft, string aRight) =>
        string.Equals(
            System.IO.Path.GetFullPath(aLeft).TrimEnd(System.IO.Path.DirectorySeparatorChar),
            System.IO.Path.GetFullPath(aRight).TrimEnd(System.IO.Path.DirectorySeparatorChar),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static bool IsInside(string aFolder, string aParent) =>
        System.IO.Path.GetFullPath(aFolder).StartsWith(
            System.IO.Path.GetFullPath(aParent).TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private sealed record WorktreeEntry(string Path, string Branch);

    private sealed class WorktreeRunRow
    {
        public int RunId { get; set; }

        public string ProcessCode { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;
    }
}
