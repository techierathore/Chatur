using Chatur.Core.Board;
using Chatur.Core.Data;
using Chatur.Core.Processes;
using Dapper;

namespace Chatur.Core.ProcessRuns;

/// <summary>Finds a project's folder and the checklist a verifying run reads and writes.</summary>
public sealed class ProjectFolders
{
    private readonly IDbConnectionFactory objConnections;

    /// <summary>Creates the locator.</summary>
    /// <param name="aConnections">Opens connections to Chatur's database.</param>
    public ProjectFolders(IDbConnectionFactory aConnections)
    {
        objConnections = aConnections;
    }

    /// <summary>The newest-phase checklist directly under a project's <c>docs</c> folder, project-relative, or <see langword="null"/>.</summary>
    /// <param name="aFolder">The project's folder.</param>
    public static string? NewestChecklist(string aFolder)
    {
        var vDocs = Path.Combine(aFolder, "docs");
        if (!Directory.Exists(vDocs))
        {
            return null;
        }

        return Directory.EnumerateFiles(vDocs, "*Checklist*.md")
            .Select(aPath => "docs/" + Path.GetFileName(aPath))
            .Where(ProcessDocumentGuard.IsDocumentName)
            .OrderByDescending(aName => ChecklistDocument.PhaseOf(aName))
            .ThenBy(aName => aName, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    /// <summary>The project's folder: its path, or the folder holding it when the path is a solution or project file; <see langword="null"/> when it is not on disk.</summary>
    /// <param name="aProjectId">The project.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    public async Task<string?> FolderAsync(int aProjectId, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vPath = await vConnection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "SELECT Path FROM Project WHERE ProjectId = @aProjectId",
            new { aProjectId },
            cancellationToken: aCt)).ConfigureAwait(false);
        var vFolder = vPath is null ? null : File.Exists(vPath) ? Path.GetDirectoryName(vPath) : vPath;
        return vFolder is not null && Directory.Exists(vFolder) ? vFolder : null;
    }
}
