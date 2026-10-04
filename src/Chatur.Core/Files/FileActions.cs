using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;

namespace Chatur.Core.Files;

/// <summary>
/// <see cref="IFileActions"/> over the selected project's own folder on disk. Not built yet; see the
/// individual method docs for the owning cluster.
/// </summary>
public sealed class FileActions : IFileActions
{
    private static readonly HashSet<string> ExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", "node_modules", ".vs", ".idea"
    };

    private readonly IDbConnectionFactory objDb;
    private readonly IProcessLauncher objProcessLauncher;

    /// <summary>
    /// Creates the action set.
    /// </summary>
    /// <param name="aDb">Opens connections to Chatur's own database, to read the project's own path.</param>
    /// <param name="aProcessLauncher">Opens a file in the owner's editor or the machine's default application (REQ-FN-045, REQ-FN-046).</param>
    public FileActions(IDbConnectionFactory aDb, IProcessLauncher aProcessLauncher)
    {
        objDb = aDb;
        objProcessLauncher = aProcessLauncher;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileTreeNode>> TreeAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vProjectPath = await LoadProjectPathAsync(aProjectId, aCt).ConfigureAwait(false);
        if (string.IsNullOrEmpty(vProjectPath) || !Directory.Exists(vProjectPath))
        {
            return [];
        }

        return BuildNodes(vProjectPath, vProjectPath);
    }

    /// <inheritdoc />
    public async Task<string> ReadAsync(int aProjectId, string aRelativePath, CancellationToken aCt = default)
    {
        var vProjectPath = await LoadProjectPathAsync(aProjectId, aCt).ConfigureAwait(false);
        var vFullPath = ResolveSafePath(vProjectPath, aRelativePath);
        return await File.ReadAllTextAsync(vFullPath, aCt).ConfigureAwait(false);
    }

    private async Task<string> LoadProjectPathAsync(int aProjectId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        var vCommand = new CommandDefinition(
            "SELECT Path FROM Project WHERE ProjectId = @aProjectId",
            new { aProjectId },
            cancellationToken: aCt);
        return await vConnection.QuerySingleOrDefaultAsync<string>(vCommand).ConfigureAwait(false) ?? string.Empty;
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

    private static IReadOnlyList<FileTreeNode> BuildNodes(string aRoot, string aFolder)
    {
        var vNodes = new List<FileTreeNode>();

        var vDirectories = Directory.EnumerateDirectories(aFolder)
            .Where(d => !ExcludedDirectoryNames.Contains(Path.GetFileName(d)))
            .OrderBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase);

        foreach (var vDirectory in vDirectories)
        {
            var vRelative = Path.GetRelativePath(aRoot, vDirectory).Replace('\\', '/');
            vNodes.Add(new FileTreeNode(Path.GetFileName(vDirectory), vRelative, true, BuildNodes(aRoot, vDirectory)));
        }

        var vFiles = Directory.EnumerateFiles(aFolder)
            .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase);

        foreach (var vFile in vFiles)
        {
            var vRelative = Path.GetRelativePath(aRoot, vFile).Replace('\\', '/');
            vNodes.Add(new FileTreeNode(Path.GetFileName(vFile), vRelative, false, []));
        }

        return vNodes;
    }

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

    /// <inheritdoc />
    /// <remarks>Resolves the file to an absolute path under the project's own root, then hands it to <see cref="IProcessLauncher.OpenInEditorAsync"/> (REQ-FN-045).</remarks>
    public async Task OpenInEditorAsync(int aProjectId, string aRelativePath, CancellationToken aCt = default)
    {
        var vProjectPath = await LoadProjectPathAsync(aProjectId, aCt).ConfigureAwait(false);
        var vFullPath = ResolveSafePath(vProjectPath, aRelativePath);
        await objProcessLauncher.OpenInEditorAsync(vFullPath, aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>Resolves the file to an absolute path under the project's own root, then hands it to <see cref="IProcessLauncher.OpenWithDefaultAppAsync"/> (REQ-FN-046).</remarks>
    public async Task OpenWithDefaultAppAsync(int aProjectId, string aRelativePath, CancellationToken aCt = default)
    {
        var vProjectPath = await LoadProjectPathAsync(aProjectId, aCt).ConfigureAwait(false);
        var vFullPath = ResolveSafePath(vProjectPath, aRelativePath);
        await objProcessLauncher.OpenWithDefaultAppAsync(vFullPath, aCt).ConfigureAwait(false);
    }
}
