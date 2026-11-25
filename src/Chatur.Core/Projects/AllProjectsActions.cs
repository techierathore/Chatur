using System.Globalization;
using Chatur.Core.Actions;
using Chatur.Core.Board;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;

namespace Chatur.Core.Projects;

/// <summary>
/// <see cref="IAllProjectsActions"/> over the projects <see cref="IProjectActions"/> lists and each
/// project's own <c>docs/*Checklist*.md</c> files. The files are the only store for requirement counts.
/// </summary>
public sealed class AllProjectsActions : IAllProjectsActions
{
    /// <summary>A project worked on within this many days is <c>active</c>; longer ago it is <c>paused</c>.</summary>
    private const int ActiveDays = 14;

    private readonly IProjectActions objProjects;
    private readonly IDbConnectionFactory objConnections;
    private readonly IClock objClock;

    /// <summary>Creates the actions.</summary>
    /// <param name="aProjects">Lists the projects and selects one.</param>
    /// <param name="aConnections">Opens connections to the account's database, for the session count.</param>
    /// <param name="aClock">The current time.</param>
    public AllProjectsActions(IProjectActions aProjects, IDbConnectionFactory aConnections, IClock aClock)
    {
        objProjects = aProjects;
        objConnections = aConnections;
        objClock = aClock;
    }

    /// <inheritdoc />
    public async Task<AllProjectsView> LoadAsync(string? aSearch, AllProjectsOrder aOrder, CancellationToken aCt = default)
    {
        var vProjects = await objProjects.ScanAsync(aCt).ConfigureAwait(false);
        var vNow = objClock.UtcNow;
        var vAll = new List<ProjectStanding>(vProjects.Count);
        foreach (var vProject in vProjects)
        {
            vAll.Add(await StandingOfAsync(vProject, vNow, aCt).ConfigureAwait(false));
        }

        var vSearch = (aSearch ?? string.Empty).Trim();
        IEnumerable<ProjectStanding> vShown = vAll;
        if (vSearch.Length > 0)
        {
            vShown = vAll.Where(aStanding =>
                aStanding.Name.Contains(vSearch, StringComparison.OrdinalIgnoreCase)
                || aStanding.Path.Contains(vSearch, StringComparison.OrdinalIgnoreCase));
        }

        var vOrdered = aOrder switch
        {
            AllProjectsOrder.Name => vShown.OrderBy(aStanding => aStanding.Name, StringComparer.OrdinalIgnoreCase),
            AllProjectsOrder.OpenRequirements => vShown.OrderByDescending(aStanding => aStanding.Open).ThenBy(aStanding => aStanding.Name, StringComparer.OrdinalIgnoreCase),
            _ => vShown.OrderByDescending(aStanding => aStanding.LastWorkedUtc.HasValue).ThenByDescending(aStanding => aStanding.LastWorkedUtc).ThenBy(aStanding => aStanding.Name, StringComparer.OrdinalIgnoreCase),
        };

        return new AllProjectsView(
            vOrdered.ToList(),
            vAll.Count,
            vAll.Sum(aStanding => aStanding.Open),
            vAll.Sum(aStanding => aStanding.Verified),
            await SessionsSinceAsync(vNow.AddDays(-7), aCt).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public Task OpenAsync(int aProjectId, CancellationToken aCt = default) => objProjects.SelectProjectAsync(aProjectId, aCt);

    private static async Task<ProjectStanding> StandingOfAsync(ProjectSummary aProject, DateTime aNow, CancellationToken aCt)
    {
        var vFolder = FolderOf(aProject.Path);
        var vRequirements = new List<RequirementItem>();
        int? vPhase = null;
        var vDocs = vFolder is null ? null : Path.Combine(vFolder, "docs");
        if (vDocs is not null && Directory.Exists(vDocs))
        {
            foreach (var vPath in Directory.EnumerateFiles(vDocs, "*Checklist*.md"))
            {
                var vName = "docs/" + Path.GetFileName(vPath);
                var vPhaseOfFile = ChecklistDocument.PhaseOf(vName);
                vPhase = Math.Max(vPhase ?? 0, vPhaseOfFile);
                try
                {
                    var vText = await File.ReadAllTextAsync(vPath, aCt).ConfigureAwait(false);
                    vRequirements.AddRange(ChecklistDocument.Parse(vText, vName, vPhaseOfFile));
                }
                catch (Exception vException) when (vException is IOException or UnauthorizedAccessException)
                {
                    // A checklist that cannot be read counts for nothing; the project is still listed.
                }
            }
        }

        var vVerified = vRequirements.Count(aItem => aItem.BoardStatus == BoardStatuses.Verified);
        var vOpen = vRequirements.Count(aItem => aItem.BoardStatus is BoardStatuses.Planned or BoardStatuses.InProgress or BoardStatuses.Blocked);
        var vState = aProject.LastOpenedUtc is null
            ? "not opened"
            : (aNow - aProject.LastOpenedUtc.Value).TotalDays <= ActiveDays ? "active" : "paused";
        return new ProjectStanding(aProject.ProjectId, aProject.Name, aProject.Path, vPhase, vRequirements.Count, vOpen, vVerified, aProject.LastOpenedUtc, vState);
    }

    private static string? FolderOf(string aProjectPath)
    {
        if (Directory.Exists(aProjectPath))
        {
            return aProjectPath;
        }

        return File.Exists(aProjectPath) ? Path.GetDirectoryName(aProjectPath) : null;
    }

    private async Task<int> SessionsSinceAsync(DateTime aSinceUtc, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        return (int)await vConnection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                "SELECT COUNT(1) FROM Session WHERE StartedUtc >= @Since;",
                new { Since = aSinceUtc.ToString("O", CultureInfo.InvariantCulture) },
                cancellationToken: aCt)).ConfigureAwait(false);
    }
}
