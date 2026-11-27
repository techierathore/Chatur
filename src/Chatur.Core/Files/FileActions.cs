using Chatur.Core.Actions;
using Chatur.Core.BuildRun;
using Chatur.Core.Data;
using Dapper;

namespace Chatur.Core.Files;

/// <summary>
/// <see cref="IFileActions"/> over the selected project's own folder on disk. The Files tree is read one
/// folder at a time, when that folder is opened, and a folder Windows will not list is shown marked not
/// readable instead of stopping the app (BRD-217; the owner's crash of 16 Nov at this file's
/// <c>Directory.EnumerateDirectories</c>, which read the whole tree at once with nothing to catch it).
/// </summary>
public sealed class FileActions : IFileActions
{
    /// <summary>The account setting that keeps View ▸ Show hidden files (BRD-225).</summary>
    public const string ShowHiddenSettingKey = "Files.ShowHidden";

    // Never listed, hidden files shown or not: build output, the repository's own store, packages.
    private static readonly HashSet<string> ExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", "node_modules", ".idea"
    };

    private readonly IDbConnectionFactory objDb;

    /// <summary>
    /// Creates the action set.
    /// </summary>
    /// <param name="aDb">Opens connections to Chatur's own database, to read the project's own path.</param>
    public FileActions(IDbConnectionFactory aDb)
    {
        objDb = aDb;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileTreeNode>> TreeAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vProjectPath = await LoadProjectPathAsync(aProjectId, aCt).ConfigureAwait(false);
        if (string.IsNullOrEmpty(vProjectPath) || !Directory.Exists(vProjectPath))
        {
            return [];
        }

        var vShowHidden = await ShowHiddenAsync(aCt).ConfigureAwait(false);
        var vLevel = ReadLevel(vProjectPath, vProjectPath, vShowHidden) ?? [];
        var vSolution = SolutionOf(vProjectPath);
        if (vSolution is null)
        {
            return vLevel;
        }

        // The solution is the root of the tree and its projects come first, as in Visual Studio (REQ-UI-125, owner's test of 25 Nov 2026).
        var vSolutionName = Path.GetFileName(vSolution);
        var vChildren = MarkProjects(vLevel.Where(n => !string.Equals(n.Name, vSolutionName, StringComparison.OrdinalIgnoreCase)), vProjectPath, vSolution);
        var vProjects = ProjectFoldersOf(vSolution, vProjectPath);
        var vOrdered = vChildren.Where(n => n.IsProject).OrderBy(n => vProjects.IndexOf(n.RelativePath))
            .Concat(vChildren.Where(n => n.IsFolder && !n.IsProject))
            .Concat(vChildren.Where(n => !n.IsFolder))
            .ToList();
        return [new FileTreeNode(Path.GetFileNameWithoutExtension(vSolution), vSolutionName, true, vOrdered, true, true)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileTreeNode>> ChildrenAsync(int aProjectId, string aRelativeFolder, CancellationToken aCt = default)
    {
        var vProjectPath = await LoadProjectPathAsync(aProjectId, aCt).ConfigureAwait(false);
        var vFolder = ResolveSafePath(vProjectPath, aRelativeFolder);
        if (!Directory.Exists(vFolder))
        {
            return [];
        }

        var vShowHidden = await ShowHiddenAsync(aCt).ConfigureAwait(false);
        var vNodes = ReadLevel(vProjectPath, vFolder, vShowHidden) ?? [];
        return SolutionOf(vProjectPath) is { } vSolution ? MarkProjects(vNodes, vProjectPath, vSolution) : vNodes;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileTreeNode>> WholeTreeAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vProjectPath = await LoadProjectPathAsync(aProjectId, aCt).ConfigureAwait(false);
        if (string.IsNullOrEmpty(vProjectPath) || !Directory.Exists(vProjectPath))
        {
            return [];
        }

        return ReadAll(vProjectPath, vProjectPath, aCt);
    }

    /// <inheritdoc />
    public async Task<bool> ShowHiddenAsync(CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        var vValue = await vConnection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition("SELECT Value FROM Setting WHERE Key = @ShowHiddenSettingKey", new { ShowHiddenSettingKey }, cancellationToken: aCt)).ConfigureAwait(false);
        return vValue == "1";
    }

    /// <inheritdoc />
    public async Task SetShowHiddenAsync(bool aShow, CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO Setting (Key, Value) VALUES (@ShowHiddenSettingKey, @Value) ON CONFLICT (Key) DO UPDATE SET Value = excluded.Value",
            new { ShowHiddenSettingKey, Value = aShow ? "1" : "0" },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> ReadAsync(int aProjectId, string aRelativePath, CancellationToken aCt = default)
    {
        var vProjectPath = await LoadProjectPathAsync(aProjectId, aCt).ConfigureAwait(false);
        var vFullPath = ResolveSafePath(vProjectPath, aRelativePath);
        return await File.ReadAllTextAsync(vFullPath, aCt).ConfigureAwait(false);
    }

    /// <summary>Whether a name is hidden in Files until the account shows hidden files: a leading dot, or Visual Studio's own .vs (BRD-225).</summary>
    /// <param name="aName">A file or folder's own name.</param>
    public static bool IsHiddenName(string aName) =>
        aName.StartsWith('.') || string.Equals(aName, ".vs", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The solution file the tree is rooted in: the first <c>.sln</c> or <c>.slnx</c> directly in the project's folder, so
    /// a project opened first and given its solution afterwards shows the solution as its root too (REQ-UI-125).
    /// </summary>
    private static string? SolutionOf(string aFolder)
    {
        try
        {
            return Directory.Exists(aFolder)
                ? Directory.EnumerateFiles(aFolder).Where(SolutionProjects.IsSolutionFile).OrderBy(f => f, StringComparer.OrdinalIgnoreCase).FirstOrDefault()
                : null;
        }
        catch (Exception aException) when (aException is UnauthorizedAccessException or IOException)
        {
            return null;
        }
    }

    /// <summary>The folders, relative to the solution's folder with forward slashes, that hold the projects the solution lists, in the solution's order.</summary>
    private static List<string> ProjectFoldersOf(string aSolution, string aRoot) =>
        SolutionProjects.Listed(aSolution)
            .Select(p => Path.GetRelativePath(aRoot, Path.GetDirectoryName(p) ?? aRoot).Replace('\\', '/'))
            .ToList();

    private static List<FileTreeNode> MarkProjects(IEnumerable<FileTreeNode> aNodes, string aRoot, string aSolution)
    {
        var vFolders = ProjectFoldersOf(aSolution, aRoot).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return aNodes.Select(n => n.IsFolder && vFolders.Contains(n.RelativePath) ? n with { IsProject = true } : n).ToList();
    }

    private async Task<string> LoadProjectPathAsync(int aProjectId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        var vCommand = new CommandDefinition(
            "SELECT Path FROM Project WHERE ProjectId = @aProjectId",
            new { aProjectId },
            cancellationToken: aCt);
        var vPath = await vConnection.QuerySingleOrDefaultAsync<string>(vCommand).ConfigureAwait(false) ?? string.Empty;

        // A solution's project path is its .sln file; the files to browse are in the folder around it.
        return File.Exists(vPath) ? Path.GetDirectoryName(vPath) ?? string.Empty : vPath;
    }

    private static string ResolveSafePath(string aProjectPath, string aRelativePath)
    {
        var vRoot = Path.GetFullPath(aProjectPath);
        var vFullPath = Path.GetFullPath(Path.Combine(vRoot, aRelativePath));
        if (!vFullPath.StartsWith(vRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("The requested path is outside the project.");
        }

        return vFullPath;
    }

    /// <summary>
    /// One folder's entries, folders first, each folder's own contents left unread. Returns
    /// <see langword="null"/> when the folder itself cannot be listed. A sub-folder is tried for one
    /// entry, so one Windows refuses is marked not readable before anyone opens it.
    /// </summary>
    private static List<FileTreeNode>? ReadLevel(string aRoot, string aFolder, bool aShowHidden)
    {
        if (!TryList(aFolder, out var vDirectories, out var vFiles))
        {
            return null;
        }

        var vNodes = new List<FileTreeNode>();
        foreach (var vDirectory in vDirectories.Where(d => Listed(Path.GetFileName(d), aShowHidden)))
        {
            vNodes.Add(new FileTreeNode(Path.GetFileName(vDirectory), Relative(aRoot, vDirectory), true, [], CanList(vDirectory)));
        }

        foreach (var vFile in vFiles.Where(f => aShowHidden || !IsHiddenName(Path.GetFileName(f))))
        {
            vNodes.Add(new FileTreeNode(Path.GetFileName(vFile), Relative(aRoot, vFile), false, []));
        }

        return vNodes;
    }

    /// <summary>The whole tree below a folder, for the searches; a folder that cannot be read is listed with nothing inside it.</summary>
    private static List<FileTreeNode> ReadAll(string aRoot, string aFolder, CancellationToken aCt)
    {
        aCt.ThrowIfCancellationRequested();
        var vNodes = new List<FileTreeNode>();
        if (!TryList(aFolder, out var vDirectories, out var vFiles))
        {
            return vNodes;
        }

        foreach (var vDirectory in vDirectories.Where(d => Listed(Path.GetFileName(d), true)))
        {
            // Never follows a junction or link: Documents\My Music points back up the tree, and a loop never ends.
            var vIsLink = IsLink(vDirectory);
            List<FileTreeNode> vChildren = vIsLink ? [] : ReadAll(aRoot, vDirectory, aCt);
            vNodes.Add(new FileTreeNode(Path.GetFileName(vDirectory), Relative(aRoot, vDirectory), true, vChildren, !vIsLink && CanList(vDirectory)));
        }

        foreach (var vFile in vFiles)
        {
            vNodes.Add(new FileTreeNode(Path.GetFileName(vFile), Relative(aRoot, vFile), false, []));
        }

        return vNodes;
    }

    private static bool Listed(string aName, bool aShowHidden) =>
        !ExcludedDirectoryNames.Contains(aName) && (aShowHidden || !IsHiddenName(aName));

    private static bool TryList(string aFolder, out List<string> aDirectories, out List<string> aFiles)
    {
        try
        {
            aDirectories = Directory.EnumerateDirectories(aFolder).OrderBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase).ToList();
            aFiles = Directory.EnumerateFiles(aFolder).OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase).ToList();
            return true;
        }
        catch (Exception aException) when (aException is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            aDirectories = [];
            aFiles = [];
            return false;
        }
    }

    private static bool CanList(string aFolder)
    {
        try
        {
            using var vEntries = Directory.EnumerateFileSystemEntries(aFolder).GetEnumerator();
            vEntries.MoveNext();
            return true;
        }
        catch (Exception aException) when (aException is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            return false;
        }
    }

    private static bool IsLink(string aFolder)
    {
        try
        {
            return new DirectoryInfo(aFolder).Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
        catch (Exception aException) when (aException is UnauthorizedAccessException or IOException)
        {
            return true;
        }
    }

    private static string Relative(string aRoot, string aPath) => Path.GetRelativePath(aRoot, aPath).Replace('\\', '/');

    /// <inheritdoc />
    /// <remarks>Writes the new text straight to disk under the project's own root (REQ-FN-044); the same sandboxing as <see cref="ReadAsync"/> refuses a path that would escape the project.</remarks>
    public async Task SaveAsync(int aProjectId, string aRelativePath, string aContent, CancellationToken aCt = default)
    {
        var vProjectPath = await LoadProjectPathAsync(aProjectId, aCt).ConfigureAwait(false);
        var vFullPath = ResolveSafePath(vProjectPath, aRelativePath);
        var vDirectory = Path.GetDirectoryName(vFullPath);
        if (!string.IsNullOrEmpty(vDirectory))
        {
            Directory.CreateDirectory(vDirectory);
        }

        await File.WriteAllTextAsync(vFullPath, aContent, aCt).ConfigureAwait(false);
    }
}
