using System.Globalization;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;

namespace Chatur.Core.Projects;

/// <summary>
/// <see cref="IProjectActions"/> backed by the <c>ProjectFolder</c> and <c>Project</c> tables
/// (Architecture §4). Naming, removing and scanning folders is cluster E's work (REQ-UI-005,
/// REQ-UI-006, REQ-UI-008, REQ-UI-009); selecting a project and having the choice survive a restart
/// is cluster E/F's (REQ-UI-007, REQ-UI-010, REQ-FN-006, REQ-FN-007).
/// </summary>
public sealed class ProjectActions : IProjectActions
{
    /// <summary>The <c>Setting</c> row's key that holds the selected project's id (REQ-FN-006).</summary>
    private const string SelectedProjectSettingKey = "SelectedProjectId";

    private readonly IDbConnectionFactory objConnections;
    private readonly IClock objClock;
    private readonly AppState objAppState;

    /// <summary>
    /// Creates the actions.
    /// </summary>
    /// <param name="aConnections">Opens connections to Chatur's own database.</param>
    /// <param name="aClock">The current time, stamped onto <c>Project.LastOpenedUtc</c> on selection.</param>
    /// <param name="aAppState">The shared, in-memory state every window reads.</param>
    public ProjectActions(IDbConnectionFactory aConnections, IClock aClock, AppState aAppState)
    {
        objConnections = aConnections;
        objClock = aClock;
        objAppState = aAppState;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProjectFolder>> ListFoldersAsync(CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRows = await vConnection.QueryAsync<RawFolderRow>(
            new CommandDefinition("SELECT ProjectFolderId, Path FROM ProjectFolder ORDER BY Path;", cancellationToken: aCt))
            .ConfigureAwait(false);
        return vRows.Select(aRow => new ProjectFolder((int)aRow.ProjectFolderId, aRow.Path)).ToList();
    }

    /// <inheritdoc />
    public async Task<ProjectFolder> AddFolderAsync(string aPath, CancellationToken aCt = default)
    {
        if (string.IsNullOrWhiteSpace(aPath))
        {
            throw new ArgumentException("A folder needs a path.", nameof(aPath));
        }

        var vPath = aPath.Trim();
        using var vConnection = objConnections.OpenConnection();

        var vExisting = await vConnection.QuerySingleOrDefaultAsync<RawFolderRow>(
            new CommandDefinition(
                "SELECT ProjectFolderId, Path FROM ProjectFolder WHERE Path = @Path;",
                new { Path = vPath },
                cancellationToken: aCt))
            .ConfigureAwait(false);
        if (vExisting is not null)
        {
            return new ProjectFolder((int)vExisting.ProjectFolderId, vExisting.Path);
        }

        var vNewId = await vConnection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                "INSERT INTO ProjectFolder (Path, CreatedUtc) VALUES (@Path, @CreatedUtc); SELECT last_insert_rowid();",
                new { Path = vPath, CreatedUtc = objClock.UtcNow.ToString("O", CultureInfo.InvariantCulture) },
                cancellationToken: aCt))
            .ConfigureAwait(false);

        return new ProjectFolder((int)vNewId, vPath);
    }

    /// <inheritdoc />
    public async Task RemoveFolderAsync(int aFolderId, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        using var vTransaction = vConnection.BeginTransaction();

        // The folder's own projects leave the list with it (REQ-UI-009). A project that has never
        // been worked on is deleted; one with history (a session, a run target, a refused write)
        // cannot be — those rows reference it — and must not lose that history, so it is detached
        // and hidden (RemovedUtc), and ScanAsync brings it back if the folder is named again.
        const string cHasHistory =
            "(EXISTS (SELECT 1 FROM Session s WHERE s.ProjectId = Project.ProjectId) " +
            "OR EXISTS (SELECT 1 FROM RunTarget r WHERE r.ProjectId = Project.ProjectId) " +
            "OR EXISTS (SELECT 1 FROM RefusedWrite w WHERE w.ProjectId = Project.ProjectId))";

        await vConnection.ExecuteAsync(
            new CommandDefinition(
                $"UPDATE Project SET ProjectFolderId = NULL, RemovedUtc = @RemovedUtc WHERE ProjectFolderId = @FolderId AND {cHasHistory};",
                new { FolderId = aFolderId, RemovedUtc = objClock.UtcNow.ToString("O", CultureInfo.InvariantCulture) },
                transaction: vTransaction,
                cancellationToken: aCt))
            .ConfigureAwait(false);

        await vConnection.ExecuteAsync(
            new CommandDefinition(
                "DELETE FROM Project WHERE ProjectFolderId = @FolderId;",
                new { FolderId = aFolderId },
                transaction: vTransaction,
                cancellationToken: aCt))
            .ConfigureAwait(false);

        // A hidden project is no longer offered, so it cannot stay the selected one either.
        await vConnection.ExecuteAsync(
            new CommandDefinition(
                "DELETE FROM Setting WHERE Key = @SettingKey AND Value IN (SELECT CAST(ProjectId AS TEXT) FROM Project WHERE RemovedUtc IS NOT NULL);",
                new { SettingKey = SelectedProjectSettingKey },
                transaction: vTransaction,
                cancellationToken: aCt))
            .ConfigureAwait(false);

        await vConnection.ExecuteAsync(
            new CommandDefinition(
                "DELETE FROM ProjectFolder WHERE ProjectFolderId = @FolderId;",
                new { FolderId = aFolderId },
                transaction: vTransaction,
                cancellationToken: aCt))
            .ConfigureAwait(false);

        vTransaction.Commit();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Hides the row (<c>RemovedUtc</c>) but keeps it attached to its folder, which is what tells
    /// <see cref="ScanAsync"/> not to bring it back; nothing is deleted, so a project with history
    /// keeps it. A forgotten project that was the selected one is no longer selected.
    /// </remarks>
    public async Task ForgetProjectAsync(int aProjectId, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        using var vTransaction = vConnection.BeginTransaction();

        await vConnection.ExecuteAsync(
            new CommandDefinition(
                "UPDATE Project SET RemovedUtc = @RemovedUtc WHERE ProjectId = @ProjectId AND RemovedUtc IS NULL;",
                new { ProjectId = aProjectId, RemovedUtc = objClock.UtcNow.ToString("O", CultureInfo.InvariantCulture) },
                transaction: vTransaction,
                cancellationToken: aCt))
            .ConfigureAwait(false);

        var vSelectionCleared = await vConnection.ExecuteAsync(
            new CommandDefinition(
                "DELETE FROM Setting WHERE Key = @SettingKey AND Value = @ProjectIdText;",
                new { SettingKey = SelectedProjectSettingKey, ProjectIdText = aProjectId.ToString(CultureInfo.InvariantCulture) },
                transaction: vTransaction,
                cancellationToken: aCt))
            .ConfigureAwait(false);

        vTransaction.Commit();

        if (vSelectionCleared > 0)
        {
            objAppState.SetSelectedProject(null);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProjectSummary>> ScanAsync(CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();

        var vFolders = (await vConnection.QueryAsync<RawFolderRow>(
            new CommandDefinition("SELECT ProjectFolderId, Path FROM ProjectFolder;", cancellationToken: aCt))
            .ConfigureAwait(false)).ToList();

        foreach (var vFolder in vFolders)
        {
            aCt.ThrowIfCancellationRequested();

            foreach (var vCandidate in FindCandidates(vFolder.Path))
            {
                // A project hidden when its folder was removed (RemoveFolderAsync) comes back, with
                // its history, when a folder holding it is named again. One the owner forgot
                // (ForgetProjectAsync) keeps its folder and stays hidden: only a detached row returns.
                var vAlreadyKnown = await vConnection.ExecuteScalarAsync<long>(
                    new CommandDefinition(
                        """
                        UPDATE Project SET ProjectFolderId = @ProjectFolderId, RemovedUtc = NULL
                        WHERE Path = @Path AND RemovedUtc IS NOT NULL AND ProjectFolderId IS NULL;
                        SELECT COUNT(1) FROM Project WHERE Path = @Path;
                        """,
                        new { vFolder.ProjectFolderId, vCandidate.Path },
                        cancellationToken: aCt))
                    .ConfigureAwait(false);
                if (vAlreadyKnown > 0)
                {
                    continue;
                }

                await vConnection.ExecuteAsync(
                    new CommandDefinition(
                        "INSERT INTO Project (ProjectFolderId, Name, Path, Kind, LastOpenedUtc) VALUES (@ProjectFolderId, @Name, @Path, @Kind, NULL);",
                        new { vFolder.ProjectFolderId, vCandidate.Name, vCandidate.Path, vCandidate.Kind },
                        cancellationToken: aCt))
                    .ConfigureAwait(false);
            }
        }

        var vRows = await vConnection.QueryAsync<RawProjectRow>(
            new CommandDefinition(
                "SELECT ProjectId, Name, Path, Kind, LastOpenedUtc FROM Project WHERE RemovedUtc IS NULL ORDER BY (LastOpenedUtc IS NULL), LastOpenedUtc DESC, Name;",
                cancellationToken: aCt))
            .ConfigureAwait(false);

        return vRows.Select(ToProjectSummary).ToList();
    }

    /// <summary>
    /// Looks one level into <paramref name="aFolderPath"/> for the projects it holds (REQ-UI-006): a
    /// subfolder with a <c>.sln</c> in it is a Solution named after that file, and every other
    /// subfolder is a bare Folder, exactly as the mockup's "scratch — empty folder" row shows.
    /// </summary>
    /// <param name="aFolderPath">A named folder's absolute path.</param>
    private static List<(string Name, string Path, string Kind)> FindCandidates(string aFolderPath)
    {
        var vResult = new List<(string Name, string Path, string Kind)>();
        if (!Directory.Exists(aFolderPath))
        {
            return vResult;
        }

        string[] vSubfolders;
        try
        {
            vSubfolders = Directory.GetDirectories(aFolderPath);
        }
        catch (Exception aException) when (aException is UnauthorizedAccessException or IOException)
        {
            return vResult;
        }

        foreach (var vSubfolder in vSubfolders)
        {
            var vName = Path.GetFileName(vSubfolder);
            if (string.IsNullOrEmpty(vName) || vName.StartsWith('.'))
            {
                continue;
            }

            string[] vSolutionFiles;
            try
            {
                vSolutionFiles = Directory.GetFiles(vSubfolder, "*.sln");
            }
            catch (Exception aException) when (aException is UnauthorizedAccessException or IOException)
            {
                continue;
            }

            vResult.Add(vSolutionFiles.Length > 0
                ? (Path.GetFileNameWithoutExtension(vSolutionFiles[0]), vSolutionFiles[0], "Solution")
                : (vName, vSubfolder, "Folder"));
        }

        return vResult;
    }

    /// <summary>Maps a raw <c>Project</c> row to the DTO the action layer returns.</summary>
    private static ProjectSummary ToProjectSummary(RawProjectRow aRow) =>
        new(
            (int)aRow.ProjectId,
            aRow.Name,
            aRow.Path,
            aRow.Kind,
            aRow.LastOpenedUtc is null ? null : DateTime.Parse(aRow.LastOpenedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

    /// <summary>
    /// A <c>ProjectFolder</c> row exactly as stored — kept separate from <see cref="ProjectFolder"/>
    /// so a column rename never has to touch the public DTO, and so the id stays a <see cref="long"/>
    /// until it is narrowed on purpose: Microsoft.Data.Sqlite always returns an INTEGER column as
    /// <see cref="long"/>, and Dapper's constructor-based materialization (the only kind a record
    /// with no parameterless constructor gets) demands an exact type match — an <see cref="int"/>
    /// parameter here throws "no parameterless constructor or one matching signature" at read time,
    /// never at compile time.
    /// </summary>
    private sealed record RawFolderRow(long ProjectFolderId, string Path);

    /// <summary>A <c>Project</c> row exactly as stored: see <see cref="RawFolderRow"/> for why its id is a <see cref="long"/>, and <c>LastOpenedUtc</c> is still the raw ISO-8601 text SQLite holds it as.</summary>
    private sealed record RawProjectRow(long ProjectId, string Name, string Path, string Kind, string? LastOpenedUtc);

    /// <inheritdoc />
    /// <remarks>
    /// Reads the <c>Setting</c> row named by <see cref="SelectedProjectSettingKey"/> — written by
    /// <see cref="SelectProjectAsync"/> — and joins it back to <c>Project</c>, so the choice survives
    /// a restart even though <see cref="AppState"/> itself starts empty in every new process
    /// (REQ-FN-006). Also hydrates <see cref="AppState.SelectedProject"/> as a side effect: this is
    /// the one place a freshly started process learns what was selected before, which is what Start
    /// reopens into the Workbench for (REQ-FN-006) and what the Workbench toolbar names afterwards
    /// (REQ-FN-007, cluster G). Returns <see langword="null"/> when nothing was ever selected, or the
    /// selected project no longer exists.
    /// </remarks>
    public async Task<ProjectSummary?> SelectedProjectAsync(CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRow = await vConnection.QueryFirstOrDefaultAsync<RawProjectRow>(
            new CommandDefinition(
                """
                SELECT p.ProjectId, p.Name, p.Path, p.Kind, p.LastOpenedUtc
                FROM Setting s
                JOIN Project p ON p.ProjectId = CAST(s.Value AS INTEGER)
                WHERE s.Key = @SettingKey;
                """,
                new { SettingKey = SelectedProjectSettingKey },
                cancellationToken: aCt)).ConfigureAwait(false);

        var vProject = vRow is null ? null : ToProjectSummary(vRow);
        objAppState.SetSelectedProject(vProject);
        return vProject;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Stamps <c>Project.LastOpenedUtc</c> and writes the <c>Setting</c> row named by
    /// <see cref="SelectedProjectSettingKey"/>, so <see cref="SelectedProjectAsync"/> finds the same
    /// project again after a restart (REQ-FN-006), then updates <see cref="AppState"/> so every open
    /// window shows the new project at once (REQ-UI-007, REQ-UI-010).
    /// </remarks>
    /// <exception cref="InvalidOperationException"><paramref name="aProjectId"/> names no row in <c>Project</c>.</exception>
    public async Task SelectProjectAsync(int aProjectId, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();

        var vRow = await vConnection.QueryFirstOrDefaultAsync<RawProjectRow>(
            new CommandDefinition(
                "SELECT ProjectId, Name, Path, Kind, LastOpenedUtc FROM Project WHERE ProjectId = @ProjectId;",
                new { ProjectId = aProjectId },
                cancellationToken: aCt)).ConfigureAwait(false);

        if (vRow is null)
        {
            throw new InvalidOperationException($"Project {aProjectId} does not exist.");
        }

        var vProject = ToProjectSummary(vRow);

        var vNowUtc = objClock.UtcNow;
        await vConnection.ExecuteAsync(
            new CommandDefinition(
                "UPDATE Project SET LastOpenedUtc = @NowUtc WHERE ProjectId = @ProjectId;",
                new { NowUtc = vNowUtc.ToString("O", CultureInfo.InvariantCulture), ProjectId = aProjectId },
                cancellationToken: aCt)).ConfigureAwait(false);

        await vConnection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO Setting (Key, Value) VALUES (@SettingKey, @ProjectIdText)
                ON CONFLICT (Key) DO UPDATE SET Value = excluded.Value;
                """,
                new
                {
                    SettingKey = SelectedProjectSettingKey,
                    ProjectIdText = aProjectId.ToString(CultureInfo.InvariantCulture)
                },
                cancellationToken: aCt)).ConfigureAwait(false);

        objAppState.SetSelectedProject(vProject with { LastOpenedUtc = vNowUtc });
    }
}
