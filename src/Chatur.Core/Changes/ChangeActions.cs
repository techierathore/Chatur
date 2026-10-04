using Chatur.Core.Actions;
using Chatur.Core.Data;
using Dapper;

namespace Chatur.Core.Changes;

/// <summary>
/// <see cref="IChangeActions"/> over the <c>Change</c> table. Not built yet; see the individual
/// method docs for the owning cluster.
/// </summary>
public sealed class ChangeActions : IChangeActions
{
    private readonly IDbConnectionFactory objDb;
    private readonly IFileActions objFileActions;

    /// <summary>
    /// Creates the action set.
    /// </summary>
    /// <param name="aDb">Opens connections to Chatur's own database.</param>
    /// <param name="aFileActions">Writes an approved change's text to disk (REQ-FN-033), reusing the same project-root sandboxing as every other file write.</param>
    public ChangeActions(IDbConnectionFactory aDb, IFileActions aFileActions)
    {
        objDb = aDb;
        objFileActions = aFileActions;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProposedChange>> ProposedAsync(int aSessionId, CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        var vRows = await vConnection.QueryAsync<ChangeRow>(
            new CommandDefinition(
                "SELECT ChangeId AS ChangeId, SessionId AS SessionId, FilePath AS FilePath, Before AS Before, After AS After, Status AS Status FROM Change WHERE SessionId = @aSessionId ORDER BY ChangeId",
                new { aSessionId },
                cancellationToken: aCt)).ConfigureAwait(false);

        return vRows
            .Select(r => new ProposedChange((int)r.ChangeId, (int)r.SessionId, r.FilePath, r.Before ?? string.Empty, r.After ?? string.Empty, r.Status))
            .ToList();
    }

    // SQLite's INTEGER columns come back through Microsoft.Data.Sqlite as Int64; Dapper's
    // constructor-based materialization for a record needs an exact type match, so every row record
    // here takes `long` for an integer column and narrows to `int` at the boundary back to the
    // public, `int`-based Actions DTOs.
    private sealed record ChangeRow(long ChangeId, long SessionId, string FilePath, string? Before, string? After, string Status);

    /// <inheritdoc />
    /// <remarks>
    /// Reads the change and the project it belongs to (via its session), writes <see cref="ProposedChange.After"/>
    /// to that file through <see cref="IFileActions.SaveAsync"/> (REQ-FN-033), then moves the row to
    /// <c>"Approved"</c>. A change already settled (not <c>"Proposed"</c>) is left alone — approving
    /// twice never writes the file twice.
    /// </remarks>
    public async Task ApproveAsync(int aChangeId, CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        var vRow = await vConnection.QuerySingleOrDefaultAsync<ChangeToApplyRow>(
            new CommandDefinition(
                """
                SELECT c.ChangeId AS ChangeId, c.FilePath AS FilePath, c.After AS After, c.Status AS Status, s.ProjectId AS ProjectId
                FROM Change c
                JOIN Session s ON s.SessionId = c.SessionId
                WHERE c.ChangeId = @aChangeId
                """,
                new { aChangeId },
                cancellationToken: aCt)).ConfigureAwait(false);

        if (vRow is null)
        {
            throw new InvalidOperationException($"Change {aChangeId} was not found.");
        }

        if (!string.Equals(vRow.Status, "Proposed", StringComparison.Ordinal))
        {
            return;
        }

        await objFileActions.SaveAsync((int)vRow.ProjectId, vRow.FilePath, vRow.After ?? string.Empty, aCt).ConfigureAwait(false);

        await vConnection.ExecuteAsync(
            new CommandDefinition(
                "UPDATE Change SET Status = 'Approved' WHERE ChangeId = @aChangeId;",
                new { aChangeId },
                cancellationToken: aCt)).ConfigureAwait(false);
    }

    private sealed record ChangeToApplyRow(long ChangeId, string FilePath, string? After, string Status, long ProjectId);

    /// <inheritdoc />
    /// <remarks>Moves the row to <c>"Rejected"</c> without touching the file on disk (REQ-FN-034).</remarks>
    public async Task RejectAsync(int aChangeId, CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        await vConnection.ExecuteAsync(
            new CommandDefinition(
                "UPDATE Change SET Status = 'Rejected' WHERE ChangeId = @aChangeId AND Status = 'Proposed';",
                new { aChangeId },
                cancellationToken: aCt)).ConfigureAwait(false);
    }
}
