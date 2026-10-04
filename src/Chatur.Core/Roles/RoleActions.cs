using System.Text.Json;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Dapper;

namespace Chatur.Core.Roles;

/// <summary>
/// <see cref="IRoleActions"/> over the <c>Role</c>, <c>RoleRight</c>, <c>RoleCommand</c>,
/// <c>Rule</c> and <c>RoleVersionHistory</c> tables. <see cref="RolesAsync"/> and
/// <see cref="RoleAsync"/> are built (cluster L); the writing methods below are cluster K's — see
/// their own doc comments.
/// </summary>
public sealed class RoleActions : IRoleActions
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IDbConnectionFactory objConnections;

    /// <summary>
    /// Creates the action implementation.
    /// </summary>
    /// <param name="aConnections">Opens connections to Chatur's database.</param>
    public RoleActions(IDbConnectionFactory aConnections)
    {
        objConnections = aConnections;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleSummary>> RolesAsync(CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRows = await vConnection.QueryAsync<RoleSummaryRow>(
            new CommandDefinition(
                "SELECT RoleId, Code, Name, Tier FROM Role ORDER BY RoleId",
                cancellationToken: aCt)).ConfigureAwait(false);

        return vRows.Select(ToSummary).ToList();
    }

    /// <inheritdoc />
    /// <exception cref="KeyNotFoundException">No role has <paramref name="aRoleId"/>.</exception>
    public async Task<RoleDetail> RoleAsync(int aRoleId, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();

        var vSummaryRow = await vConnection.QuerySingleOrDefaultAsync<RoleSummaryRow>(
            new CommandDefinition(
                "SELECT RoleId, Code, Name, Tier FROM Role WHERE RoleId = @RoleId",
                new { RoleId = aRoleId },
                cancellationToken: aCt)).ConfigureAwait(false);
        if (vSummaryRow is null)
        {
            throw new KeyNotFoundException($"No role with id {aRoleId}.");
        }

        var vSummary = ToSummary(vSummaryRow);

        var vWording = await vConnection.QuerySingleAsync<string>(
            new CommandDefinition(
                "SELECT Wording FROM Role WHERE RoleId = @RoleId",
                new { RoleId = aRoleId },
                cancellationToken: aCt)).ConfigureAwait(false);

        var vRights = (await vConnection.QueryAsync<string>(
            new CommandDefinition(
                "SELECT Action FROM RoleRight WHERE RoleId = @RoleId AND Allowed = 1 ORDER BY RoleRightId",
                new { RoleId = aRoleId },
                cancellationToken: aCt)).ConfigureAwait(false)).ToList();

        var vCommands = (await vConnection.QueryAsync<string>(
            new CommandDefinition(
                "SELECT Command FROM RoleCommand WHERE RoleId = @RoleId ORDER BY RoleCommandId",
                new { RoleId = aRoleId },
                cancellationToken: aCt)).ConfigureAwait(false)).ToList();

        // No RoleRule membership table exists yet (Coding Standards §Database — a shipped migration
        // is never edited): every role currently answers to the same standing rules.
        var vRules = (await vConnection.QueryAsync<string>(
            new CommandDefinition(
                "SELECT Text FROM Rule WHERE Scope IN ('global', @Code) ORDER BY RuleId",
                new { Code = vSummary.Code },
                cancellationToken: aCt)).ConfigureAwait(false)).ToList();

        var vHistory = (await vConnection.QueryAsync<RoleVersionRow>(
            new CommandDefinition(
                "SELECT Version, ValidFromUtc, WhatChanged, Wording FROM RoleVersionHistory WHERE RoleId = @RoleId ORDER BY Version DESC",
                new { RoleId = aRoleId },
                cancellationToken: aCt)).ConfigureAwait(false))
            .Select(r => new RoleVersion((int)r.Version, DateTime.Parse(r.ValidFromUtc, null, System.Globalization.DateTimeStyles.RoundtripKind), r.WhatChanged, r.Wording))
            .ToList();

        return new RoleDetail(vSummary, vWording, vRights, vCommands, vRules, vHistory);
    }

    /// <summary>
    /// Turns a SQLite row (whose <c>INTEGER</c> columns always come back as <see cref="long"/>) into
    /// <see cref="RoleSummary"/>. Dapper's constructor-based materialisation for a record needs an
    /// exact type match, so a plain <c>QueryAsync&lt;RoleSummary&gt;</c> throws
    /// <see cref="InvalidOperationException"/> — this row type plus a narrowing cast is the fix.
    /// </summary>
    private static RoleSummary ToSummary(RoleSummaryRow aRow) =>
        new((int)aRow.RoleId, aRow.Code, aRow.Name, (int)aRow.Tier);

    /// <summary>The <c>Role</c> table's id/name/tier columns, read with their native SQLite width.</summary>
    private sealed class RoleSummaryRow
    {
        public long RoleId { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public long Tier { get; set; }
    }

    /// <summary>One <c>RoleVersionHistory</c> row, read with its native SQLite column widths.</summary>
    private sealed class RoleVersionRow
    {
        public long Version { get; set; }

        public string ValidFromUtc { get; set; } = string.Empty;

        public string WhatChanged { get; set; } = string.Empty;

        public string Wording { get; set; } = string.Empty;
    }

    /// <summary>
    /// One <c>RoleRight</c> row for <see cref="ExportAsync"/>, read with its native SQLite column
    /// width — <c>Allowed</c> comes back as <see cref="long"/> (SQLite's one integer storage class),
    /// not the <see cref="bool"/> <see cref="RoleExportRight"/> needs for a clean export.
    /// </summary>
    private sealed class RoleRightRow
    {
        public string Action { get; set; } = string.Empty;

        public long Allowed { get; set; }
    }

    /// <summary>One <c>Rule</c> row for <see cref="ExportAsync"/>, read with its native SQLite column width.</summary>
    private sealed class RuleRow
    {
        public string Scope { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public long Version { get; set; }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="aTier"/> is not 1, 2 or 3.</exception>
    /// <exception cref="KeyNotFoundException">No role has <paramref name="aRoleId"/>.</exception>
    public async Task<RoleDetail> SetTierAsync(int aRoleId, int aTier, CancellationToken aCt = default)
    {
        if (aTier is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(aTier), aTier, "A tier is 1, 2 or 3.");
        }

        using var vConnection = objConnections.OpenConnection();
        var vAffected = await vConnection.ExecuteAsync(
            new CommandDefinition(
                "UPDATE Role SET Tier = @aTier WHERE RoleId = @aRoleId",
                new { aTier, aRoleId },
                cancellationToken: aCt)).ConfigureAwait(false);
        if (vAffected == 0)
        {
            throw new KeyNotFoundException($"No role with id {aRoleId}.");
        }

        return await RoleAsync(aRoleId, aCt).ConfigureAwait(false);
    }

    /// <summary>Every action a role holds a right row for (0002-SeedRoles.sql, 0026-M-CorrectWordingRight.sql).</summary>
    private static readonly string[] AllActions = ["read-file", "edit-file", "run-build", "run-source-control", "mark-verified", "correct-wording"];

    /// <inheritdoc />
    /// <exception cref="ArgumentException">The name is empty or already used, or <paramref name="aWording"/> is under 40 characters.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="aTier"/> is not 1, 2 or 3.</exception>
    public async Task<RoleDetail> AddAsync(string aName, string aWording, int aTier, IReadOnlyCollection<string> aAllowedActions, CancellationToken aCt = default)
    {
        var vName = (aName ?? string.Empty).Trim();
        var vCode = string.Join('-', vName.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        vCode = new string(vCode.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        if (vCode.Length == 0)
        {
            throw new ArgumentException("An agent needs a name.", nameof(aName));
        }

        if (aWording is null || aWording.Length < 40)
        {
            throw new ArgumentException("A role's wording must be at least 40 characters.", nameof(aWording));
        }

        if (aTier is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(aTier), aTier, "A tier is 1, 2 or 3.");
        }

        using var vConnection = objConnections.OpenConnection();
        var vTaken = await vConnection.QuerySingleOrDefaultAsync<int?>(
            new CommandDefinition("SELECT RoleId FROM Role WHERE Code = @vCode", new { vCode }, cancellationToken: aCt))
            .ConfigureAwait(false);
        if (vTaken is not null)
        {
            throw new ArgumentException($"An agent called \"{vName}\" already exists.", nameof(aName));
        }

        var vNowUtc = DateTime.UtcNow.ToString("O");
        var vRoleId = await vConnection.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            INSERT INTO Role (Code, Name, Wording, Version, ValidFromUtc, Tier)
            VALUES (@vCode, @vName, @aWording, 1, @vNowUtc, @aTier);
            SELECT last_insert_rowid();
            """,
            new { vCode, vName, aWording, vNowUtc, aTier },
            cancellationToken: aCt)).ConfigureAwait(false);

        await vConnection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO RoleRight (RoleId, Action, Allowed) VALUES (@vRoleId, @Action, @Allowed)",
            AllActions.Select(aAction => new { vRoleId, Action = aAction, Allowed = aAllowedActions.Contains(aAction) || aAction == "read-file" ? 1 : 0 }),
            cancellationToken: aCt)).ConfigureAwait(false);

        await vConnection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO RoleVersionHistory (RoleId, Version, Wording, ValidFromUtc, WhatChanged)
            VALUES (@vRoleId, 1, @aWording, @vNowUtc, 'Added by the owner.')
            """,
            new { vRoleId, aWording, vNowUtc },
            cancellationToken: aCt)).ConfigureAwait(false);

        return await RoleAsync(vRoleId, aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException"><paramref name="aWording"/> is under 40 characters.</exception>
    /// <exception cref="KeyNotFoundException">No role has <paramref name="aRoleId"/>.</exception>
    public async Task<RoleDetail> SaveAsync(int aRoleId, string aWording, IReadOnlyList<string> aCommands, IReadOnlyList<string> aRules, CancellationToken aCt = default)
    {
        if (aWording.Length < 40)
        {
            throw new ArgumentException("A role's wording must be at least 40 characters.", nameof(aWording));
        }

        using var vConnection = objConnections.OpenConnection();

        var vCode = await vConnection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition("SELECT Code FROM Role WHERE RoleId = @RoleId", new { RoleId = aRoleId }, cancellationToken: aCt))
            .ConfigureAwait(false);
        if (vCode is null)
        {
            throw new KeyNotFoundException($"No role with id {aRoleId}.");
        }

        var vCurrentVersion = await vConnection.QuerySingleAsync<int>(
            new CommandDefinition("SELECT Version FROM Role WHERE RoleId = @RoleId", new { RoleId = aRoleId }, cancellationToken: aCt))
            .ConfigureAwait(false);
        var vNewVersion = vCurrentVersion + 1;
        var vNowUtc = DateTime.UtcNow.ToString("O");

        await vConnection.ExecuteAsync(new CommandDefinition(
            "UPDATE Role SET Wording = @aWording, Version = @vNewVersion, ValidFromUtc = @vNowUtc WHERE RoleId = @aRoleId",
            new { aWording, vNewVersion, vNowUtc, aRoleId },
            cancellationToken: aCt)).ConfigureAwait(false);

        await vConnection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM RoleCommand WHERE RoleId = @aRoleId", new { aRoleId }, cancellationToken: aCt)).ConfigureAwait(false);
        if (aCommands.Count > 0)
        {
            await vConnection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO RoleCommand (RoleId, Command, Description) VALUES (@aRoleId, @Command, '')",
                aCommands.Select(aCommand => new { aRoleId, Command = aCommand }),
                cancellationToken: aCt)).ConfigureAwait(false);
        }

        await vConnection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Rule WHERE Scope = @vCode", new { vCode }, cancellationToken: aCt)).ConfigureAwait(false);
        if (aRules.Count > 0)
        {
            await vConnection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO Rule (Scope, Text, Version) VALUES (@vCode, @Text, 1)",
                aRules.Select(aRule => new { vCode, Text = aRule }),
                cancellationToken: aCt)).ConfigureAwait(false);
        }

        await vConnection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO RoleVersionHistory (RoleId, Version, Wording, ValidFromUtc, WhatChanged)
            VALUES (@aRoleId, @vNewVersion, @aWording, @vNowUtc, 'Wording, commands and rules saved.')
            """,
            new { aRoleId, vNewVersion, aWording, vNowUtc },
            cancellationToken: aCt)).ConfigureAwait(false);

        return await RoleAsync(aRoleId, aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> ExportAsync(CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();

        var vRoleRows = (await vConnection.QueryAsync<RoleRow>(
            new CommandDefinition("SELECT RoleId, Code, Name, Wording, Version, ValidFromUtc, Tier FROM Role ORDER BY RoleId", cancellationToken: aCt))
            .ConfigureAwait(false)).ToList();

        var vRoles = new List<RoleExportRole>(vRoleRows.Count);
        foreach (var vRole in vRoleRows)
        {
            var vRights = (await vConnection.QueryAsync<RoleRightRow>(
                new CommandDefinition(
                    "SELECT Action, Allowed FROM RoleRight WHERE RoleId = @RoleId ORDER BY RoleRightId",
                    new { RoleId = vRole.RoleId },
                    cancellationToken: aCt)).ConfigureAwait(false))
                .Select(r => new RoleExportRight(r.Action, r.Allowed != 0))
                .ToList();

            var vCommands = (await vConnection.QueryAsync<RoleExportCommand>(
                new CommandDefinition(
                    "SELECT Command, Description FROM RoleCommand WHERE RoleId = @RoleId ORDER BY RoleCommandId",
                    new { RoleId = vRole.RoleId },
                    cancellationToken: aCt)).ConfigureAwait(false)).ToList();

            vRoles.Add(new RoleExportRole(vRole.Code, vRole.Name, vRole.Wording, (int)vRole.Version, vRole.ValidFromUtc, (int)vRole.Tier, vRights, vCommands));
        }

        var vRules = (await vConnection.QueryAsync<RuleRow>(
            new CommandDefinition("SELECT Scope, Text, Version FROM Rule ORDER BY RuleId", cancellationToken: aCt))
            .ConfigureAwait(false))
            .Select(r => new RoleExportRule(r.Scope, r.Text, (int)r.Version))
            .ToList();

        var vFile = new RoleExportFile("chatur-roles-export", 1, DateTime.UtcNow.ToString("O"), vRoles, vRules);
        return JsonSerializer.Serialize(vFile, JsonOptions);
    }

    /// <inheritdoc />
    /// <exception cref="FormatException"><paramref name="aJson"/> is not a Chatur roles export.</exception>
    public async Task ImportAsync(string aJson, CancellationToken aCt = default)
    {
        var vFile = JsonSerializer.Deserialize<RoleExportFile>(aJson, JsonOptions);
        if (vFile is null || vFile.Schema != "chatur-roles-export")
        {
            throw new FormatException("This file is not a Chatur roles export.");
        }

        using var vConnection = objConnections.OpenConnection();
        var vNowUtc = DateTime.UtcNow.ToString("O");

        foreach (var vRole in vFile.Roles)
        {
            var vExistingRoleId = await vConnection.QuerySingleOrDefaultAsync<int?>(
                new CommandDefinition("SELECT RoleId FROM Role WHERE Code = @Code", new { vRole.Code }, cancellationToken: aCt))
                .ConfigureAwait(false);

            int vRoleId;
            int vNewVersion;
            if (vExistingRoleId is int vFoundRoleId)
            {
                vRoleId = vFoundRoleId;
                var vCurrentVersion = await vConnection.QuerySingleAsync<int>(
                    new CommandDefinition("SELECT Version FROM Role WHERE RoleId = @vRoleId", new { vRoleId }, cancellationToken: aCt))
                    .ConfigureAwait(false);
                vNewVersion = vCurrentVersion + 1;

                await vConnection.ExecuteAsync(new CommandDefinition(
                    "UPDATE Role SET Name = @Name, Wording = @Wording, Tier = @Tier, Version = @vNewVersion, ValidFromUtc = @vNowUtc WHERE RoleId = @vRoleId",
                    new { vRole.Name, vRole.Wording, vRole.Tier, vNewVersion, vNowUtc, vRoleId },
                    cancellationToken: aCt)).ConfigureAwait(false);
            }
            else
            {
                vNewVersion = 1;
                vRoleId = await vConnection.ExecuteScalarAsync<int>(new CommandDefinition(
                    """
                    INSERT INTO Role (Code, Name, Wording, Version, ValidFromUtc, Tier)
                    VALUES (@Code, @Name, @Wording, @vNewVersion, @vNowUtc, @Tier);
                    SELECT last_insert_rowid();
                    """,
                    new { vRole.Code, vRole.Name, vRole.Wording, vNewVersion, vNowUtc, vRole.Tier },
                    cancellationToken: aCt)).ConfigureAwait(false);
            }

            await vConnection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM RoleRight WHERE RoleId = @vRoleId", new { vRoleId }, cancellationToken: aCt)).ConfigureAwait(false);
            if (vRole.Rights.Count > 0)
            {
                await vConnection.ExecuteAsync(new CommandDefinition(
                    "INSERT INTO RoleRight (RoleId, Action, Allowed) VALUES (@vRoleId, @Action, @Allowed)",
                    vRole.Rights.Select(aRight => new { vRoleId, aRight.Action, Allowed = aRight.Allowed ? 1 : 0 }),
                    cancellationToken: aCt)).ConfigureAwait(false);
            }

            await vConnection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM RoleCommand WHERE RoleId = @vRoleId", new { vRoleId }, cancellationToken: aCt)).ConfigureAwait(false);
            if (vRole.Commands.Count > 0)
            {
                await vConnection.ExecuteAsync(new CommandDefinition(
                    "INSERT INTO RoleCommand (RoleId, Command, Description) VALUES (@vRoleId, @Command, @Description)",
                    vRole.Commands.Select(aCommand => new { vRoleId, aCommand.Command, aCommand.Description }),
                    cancellationToken: aCt)).ConfigureAwait(false);
            }

            await vConnection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO RoleVersionHistory (RoleId, Version, Wording, ValidFromUtc, WhatChanged)
                VALUES (@vRoleId, @vNewVersion, @Wording, @vNowUtc, 'Imported from another machine.')
                """,
                new { vRoleId, vNewVersion, vRole.Wording, vNowUtc },
                cancellationToken: aCt)).ConfigureAwait(false);
        }

        foreach (var vRule in vFile.Rules)
        {
            var vAlreadyThere = await vConnection.QuerySingleOrDefaultAsync<int?>(
                new CommandDefinition(
                    "SELECT RuleId FROM Rule WHERE Scope = @Scope AND Text = @Text",
                    new { vRule.Scope, vRule.Text },
                    cancellationToken: aCt)).ConfigureAwait(false);
            if (vAlreadyThere is not null)
            {
                continue;
            }

            await vConnection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO Rule (Scope, Text, Version) VALUES (@Scope, @Text, @Version)",
                new { vRule.Scope, vRule.Text, vRule.Version },
                cancellationToken: aCt)).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException"><paramref name="aActingRoleId"/> is not the Verifier.</exception>
    public async Task MarkVerifiedAsync(int aRequirementId, int aActingRoleId, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();

        var vAllowed = await vConnection.QuerySingleOrDefaultAsync<long?>(
            new CommandDefinition(
                "SELECT Allowed FROM RoleRight WHERE RoleId = @aActingRoleId AND Action = 'mark-verified'",
                new { aActingRoleId },
                cancellationToken: aCt)).ConfigureAwait(false);

        if (vAllowed is null || vAllowed.Value == 0)
        {
            throw new InvalidOperationException("Only the Verifier may mark a requirement verified.");
        }

        await vConnection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO RequirementVerification (RequirementId, VerifiedByRoleId, VerifiedUtc)
            VALUES (@aRequirementId, @aActingRoleId, @vNowUtc)
            """,
            new { aRequirementId, aActingRoleId, vNowUtc = DateTime.UtcNow.ToString("O") },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <summary>
    /// The <c>Role</c> table's own shape, read by Dapper before it becomes an export row —
    /// <c>RoleId</c>, <c>Version</c> and <c>Tier</c> stay <see cref="long"/> (SQLite's native INTEGER
    /// width) until narrowed on purpose; see <see cref="RoleSummaryRow"/>'s doc comment for why.
    /// </summary>
    private sealed class RoleRow
    {
        public long RoleId { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Wording { get; set; } = string.Empty;

        public long Version { get; set; }

        public string ValidFromUtc { get; set; } = string.Empty;

        public long Tier { get; set; }
    }
}
