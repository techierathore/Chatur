using System.Data;
using System.Globalization;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;

namespace Chatur.Core.AgentWorkspace;

/// <summary>
/// <see cref="IAgentWorkspaceActions"/> over the <c>Role</c>, <c>RoleRight</c>, <c>RoleCommand</c> and
/// <c>Session</c> tables. It only reads sessions and opens new ones; nothing here closes one, which is
/// what keeps the thread an agent was left with open (REQ-FN-060).
/// </summary>
public sealed class AgentWorkspaceActions : IAgentWorkspaceActions
{
    private const int TitleLength = 80;

    private readonly IDbConnectionFactory objDb;
    private readonly IClock objClock;

    /// <summary>Creates the action set.</summary>
    /// <param name="aDb">Opens connections to Chatur's own database.</param>
    /// <param name="aClock">Timestamps a session the moment it is opened.</param>
    public AgentWorkspaceActions(IDbConnectionFactory aDb, IClock aClock)
    {
        objDb = aDb;
        objClock = aClock;
    }

    /// <inheritdoc />
    public async Task<AgentWorkspaceView> WorkspaceAsync(string aAgentCode, int aProjectId, CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        var vAgents = await AgentsAsync(vConnection, aProjectId, aCt).ConfigureAwait(false);
        var vAgent = vAgents.FirstOrDefault(a => string.Equals(a.Code, aAgentCode, StringComparison.OrdinalIgnoreCase))
            ?? vAgents.FirstOrDefault()
            ?? throw new InvalidOperationException("No agent is defined, so there is no workspace to show.");

        var vRights = (await vConnection.QueryAsync<RightRow>(new CommandDefinition(
            "SELECT Action AS Action, Allowed AS Allowed FROM RoleRight WHERE RoleId = @RoleId ORDER BY RoleRightId;",
            new { vAgent.RoleId },
            cancellationToken: aCt)).ConfigureAwait(false)).ToList();
        var vHeld = vRights.Where(r => r.Allowed != 0).Select(r => r.Action).ToHashSet(StringComparer.Ordinal);

        var vAllowed = vRights.Where(r => r.Allowed != 0).Select(r => new WorkspaceRight(r.Action, WorkspaceRightLabels.Allowed(r.Action))).ToList();
        var vDenied = vRights.Where(r => r.Allowed == 0).Select(r => new WorkspaceDeniedRight(r.Action, WorkspaceRightLabels.Denied(r.Action))).ToList();

        var vCommands = (await CommandsAsync(vConnection, vAgent.RoleId, aCt).ConfigureAwait(false))
            .Where(c => c.RequiredAction is null || vHeld.Contains(c.RequiredAction))
            .Select(c => new WorkspaceCommand(c.Command.TrimStart('*'), c.Description))
            .ToList();

        var vSessions = await OpenSessionsAsync(vConnection, vAgent.RoleId, aProjectId, aCt).ConfigureAwait(false);
        return new AgentWorkspaceView(vAgent, vAgents, vAllowed, vDenied, vCommands, vSessions);
    }

    /// <inheritdoc />
    public async Task<WorkspaceCommandStart> StartCommandAsync(string aAgentCode, int aProjectId, string aCommand, CancellationToken aCt = default)
    {
        var vName = aCommand.Trim().TrimStart('*');
        var vView = await WorkspaceAsync(aAgentCode, aProjectId, aCt).ConfigureAwait(false);
        if (!vView.Commands.Any(c => string.Equals(c.Command, vName, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"{vView.Agent.Name} is not offered the command \"{vName}\".");
        }

        using var vConnection = objDb.OpenConnection();
        var vSessionId = await vConnection.ExecuteScalarAsync<long>(new CommandDefinition(
            @"INSERT INTO Session (ProjectId, RoleId, Mode, State, StartedUtc) VALUES (@aProjectId, @RoleId, 'AskFirst', 'Active', @vStartedUtc);
              SELECT last_insert_rowid();",
            new { aProjectId, vView.Agent.RoleId, vStartedUtc = objClock.UtcNow.ToString("O") },
            cancellationToken: aCt)).ConfigureAwait(false);

        return new WorkspaceCommandStart((int)vSessionId, vView.Agent.RoleId, aProjectId, "*" + vName);
    }

    private static async Task<IReadOnlyList<WorkspaceAgent>> AgentsAsync(IDbConnection aConnection, int aProjectId, CancellationToken aCt)
    {
        var vRows = await aConnection.QueryAsync<AgentRow>(new CommandDefinition(
            @"SELECT r.RoleId AS RoleId, r.Code AS Code, r.Name AS Name,
                     CAST((SELECT COUNT(*) FROM Session s WHERE s.RoleId = r.RoleId AND s.ProjectId = @aProjectId AND s.State != 'Stopped') AS INTEGER) AS OpenSessions
              FROM Role r ORDER BY r.RoleId;",
            new { aProjectId },
            cancellationToken: aCt)).ConfigureAwait(false);
        return vRows.Select(r => new WorkspaceAgent((int)r.RoleId, r.Code, r.Name, (int)r.OpenSessions)).ToList();
    }

    private static async Task<List<CommandRow>> CommandsAsync(IDbConnection aConnection, int aRoleId, CancellationToken aCt) =>
        (await aConnection.QueryAsync<CommandRow>(new CommandDefinition(
            "SELECT Command AS Command, Description AS Description, RequiredAction AS RequiredAction FROM RoleCommand WHERE RoleId = @aRoleId ORDER BY RoleCommandId;",
            new { aRoleId },
            cancellationToken: aCt)).ConfigureAwait(false)).ToList();

    private static async Task<IReadOnlyList<WorkspaceSession>> OpenSessionsAsync(IDbConnection aConnection, int aRoleId, int aProjectId, CancellationToken aCt)
    {
        var vRows = await aConnection.QueryAsync<SessionRow>(new CommandDefinition(
            @"SELECT s.SessionId AS SessionId, s.StartedUtc AS StartedUtc, s.State AS State,
                     (SELECT e.Payload FROM SessionEvent e WHERE e.SessionId = s.SessionId AND e.Kind = 'user' ORDER BY e.Seq LIMIT 1) AS FirstSaid,
                     CAST((SELECT COUNT(*) FROM SessionEvent e WHERE e.SessionId = s.SessionId AND e.Kind IN ('user', 'assistant')) AS INTEGER) AS MessageCount,
                     CAST((SELECT COALESCE(SUM(e.Tokens), 0) FROM SessionEvent e WHERE e.SessionId = s.SessionId) AS INTEGER) AS TokensUsed
              FROM Session s
              WHERE s.RoleId = @aRoleId AND s.ProjectId = @aProjectId AND s.State != 'Stopped'
              ORDER BY s.StartedUtc DESC, s.SessionId DESC;",
            new { aRoleId, aProjectId },
            cancellationToken: aCt)).ConfigureAwait(false);

        return vRows.Select(r => new WorkspaceSession(
            (int)r.SessionId,
            Title(r.FirstSaid),
            DateTime.Parse(r.StartedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            (int)r.MessageCount,
            (int)r.TokensUsed,
            r.State)).ToList();
    }

    private static string Title(string? aFirstSaid)
    {
        if (string.IsNullOrWhiteSpace(aFirstSaid))
        {
            return "New conversation";
        }

        var vOneLine = aFirstSaid.ReplaceLineEndings(" ").Trim();
        return vOneLine.Length <= TitleLength ? vOneLine : vOneLine[..TitleLength].TrimEnd() + "…";
    }

    private sealed record AgentRow(long RoleId, string Code, string Name, long OpenSessions);

    private sealed record RightRow(string Action, long Allowed);

    private sealed record CommandRow(string Command, string Description, string? RequiredAction);

    // A class with settable properties, not a record: with no rows the provider reports the computed
    // columns as byte arrays, and a positional constructor then cannot be matched.
    private sealed class SessionRow
    {
        public long SessionId { get; set; }

        public string StartedUtc { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string? FirstSaid { get; set; }

        public long MessageCount { get; set; }

        public long TokensUsed { get; set; }
    }
}
