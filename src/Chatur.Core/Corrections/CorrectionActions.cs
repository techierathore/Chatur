using System.Text.Json;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Dapper;

namespace Chatur.Core.Corrections;

/// <summary>
/// <see cref="ICorrectionActions"/> over the <c>Correction</c> table. <see cref="ListAsync"/> is
/// built (cluster L); the writing methods below are cluster K's — see their own doc comments.
/// </summary>
public sealed class CorrectionActions : ICorrectionActions
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IDbConnectionFactory objConnections;

    /// <summary>
    /// Creates the action implementation.
    /// </summary>
    /// <param name="aConnections">Opens connections to Chatur's database.</param>
    public CorrectionActions(IDbConnectionFactory aConnections)
    {
        objConnections = aConnections;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Correction>> ListAsync(CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRows = await vConnection.QueryAsync<CorrectionRow>(
            new CommandDefinition(
                "SELECT CorrectionId, Target, Before, After, Why, Status, CreatedUtc FROM Correction ORDER BY CreatedUtc DESC",
                cancellationToken: aCt)).ConfigureAwait(false);

        return vRows.Select(ToCorrection).ToList();
    }

    /// <summary>The <c>Target</c> prefix of a rule correction: <c>rule:&lt;RuleId&gt;</c>. A role's target stays its bare code.</summary>
    private const string RulePrefix = "rule:";

    /// <summary>The <c>Target</c> prefix of a step correction: <c>step:&lt;ProcessCode&gt;/&lt;StepCode&gt;</c>.</summary>
    private const string StepPrefix = "step:";

    /// <inheritdoc />
    /// <remarks>
    /// A role's change is a versioned save exactly like <see cref="Roles.RoleActions.SaveAsync"/>
    /// (new <c>Role.Version</c>, a <c>RoleVersionHistory</c> row); a rule or a step has no history
    /// table, so its own <c>Version</c> counts up and the correction row itself holds the old text.
    /// The wording change and the correction row are written in one transaction.
    /// </remarks>
    public async Task<Correction> CorrectWordingAsync(string aKind, string aTarget, string aNewWording, string aReason, CancellationToken aCt = default)
    {
        if (string.IsNullOrWhiteSpace(aReason))
        {
            throw new ArgumentException("A correction needs a reason.", nameof(aReason));
        }

        if (string.IsNullOrWhiteSpace(aNewWording))
        {
            throw new ArgumentException("A correction needs the new wording.", nameof(aNewWording));
        }

        using var vConnection = objConnections.OpenConnection();
        using var vTransaction = vConnection.BeginTransaction();
        var vNowUtc = DateTime.UtcNow.ToString("O");
        string vTargetName;
        string vBefore;

        switch (aKind.Trim().ToLowerInvariant())
        {
            case "role":
            {
                if (aNewWording.Length < 40)
                {
                    throw new ArgumentException("A role's wording must be at least 40 characters.", nameof(aNewWording));
                }

                var vRole = await vConnection.QuerySingleOrDefaultAsync<RoleWordingRow>(new CommandDefinition(
                    "SELECT RoleId, Wording, Version FROM Role WHERE Code = @aTarget",
                    new { aTarget },
                    vTransaction,
                    cancellationToken: aCt)).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"No role with code \"{aTarget}\".");

                var vNewVersion = vRole.Version + 1;
                await vConnection.ExecuteAsync(new CommandDefinition(
                    "UPDATE Role SET Wording = @aNewWording, Version = @vNewVersion, ValidFromUtc = @vNowUtc WHERE RoleId = @RoleId",
                    new { aNewWording, vNewVersion, vNowUtc, vRole.RoleId },
                    vTransaction,
                    cancellationToken: aCt)).ConfigureAwait(false);
                await vConnection.ExecuteAsync(new CommandDefinition(
                    """
                    INSERT INTO RoleVersionHistory (RoleId, Version, Wording, ValidFromUtc, WhatChanged)
                    VALUES (@RoleId, @vNewVersion, @aNewWording, @vNowUtc, @WhatChanged)
                    """,
                    new { vRole.RoleId, vNewVersion, aNewWording, vNowUtc, WhatChanged = $"Corrected: {aReason}" },
                    vTransaction,
                    cancellationToken: aCt)).ConfigureAwait(false);

                vTargetName = aTarget;
                vBefore = vRole.Wording;
                break;
            }

            case "rule":
            {
                if (!int.TryParse(aTarget, out var vRuleId))
                {
                    throw new ArgumentException("A rule's target is its numeric id.", nameof(aTarget));
                }

                vBefore = await vConnection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
                    "SELECT Text FROM Rule WHERE RuleId = @vRuleId",
                    new { vRuleId },
                    vTransaction,
                    cancellationToken: aCt)).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"No rule with id {vRuleId}.");
                await vConnection.ExecuteAsync(new CommandDefinition(
                    "UPDATE Rule SET Text = @aNewWording, Version = Version + 1 WHERE RuleId = @vRuleId",
                    new { aNewWording, vRuleId },
                    vTransaction,
                    cancellationToken: aCt)).ConfigureAwait(false);

                vTargetName = RulePrefix + vRuleId;
                break;
            }

            case "step":
            {
                var vParts = aTarget.Split('/', 2);
                if (vParts.Length != 2)
                {
                    throw new ArgumentException("A step's target is \"process/step\".", nameof(aTarget));
                }

                vBefore = await vConnection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
                    "SELECT Text FROM ProcessStepText WHERE ProcessCode = @ProcessCode AND StepCode = @StepCode",
                    new { ProcessCode = vParts[0], StepCode = vParts[1] },
                    vTransaction,
                    cancellationToken: aCt)).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"No step \"{aTarget}\".");
                await vConnection.ExecuteAsync(new CommandDefinition(
                    "UPDATE ProcessStepText SET Text = @aNewWording, Version = Version + 1 WHERE ProcessCode = @ProcessCode AND StepCode = @StepCode",
                    new { aNewWording, ProcessCode = vParts[0], StepCode = vParts[1] },
                    vTransaction,
                    cancellationToken: aCt)).ConfigureAwait(false);

                vTargetName = StepPrefix + aTarget;
                break;
            }

            default:
                throw new ArgumentException($"A correction's kind is role, rule or step, not \"{aKind}\".", nameof(aKind));
        }

        var vCorrectionId = await vConnection.ExecuteScalarAsync<long>(new CommandDefinition(
            """
            INSERT INTO Correction (Target, Before, After, Why, Status, CreatedUtc)
            VALUES (@vTargetName, @vBefore, @aNewWording, @aReason, 'Proposed', @vNowUtc);
            SELECT last_insert_rowid();
            """,
            new { vTargetName, vBefore, aNewWording, aReason, vNowUtc },
            vTransaction,
            cancellationToken: aCt)).ConfigureAwait(false);

        vTransaction.Commit();
        return new Correction((int)vCorrectionId, vTargetName, vBefore, aNewWording, aReason, "Proposed", DateTime.Parse(vNowUtc, null, System.Globalization.DateTimeStyles.RoundtripKind));
    }

    /// <summary>A role's id, current wording and version, read with native SQLite widths.</summary>
    private sealed class RoleWordingRow
    {
        public long RoleId { get; set; }

        public string Wording { get; set; } = string.Empty;

        public long Version { get; set; }
    }

    /// <inheritdoc />
    /// <exception cref="KeyNotFoundException">No correction has <paramref name="aCorrectionId"/>.</exception>
    public async Task KeepAsync(int aCorrectionId, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vAffected = await vConnection.ExecuteAsync(new CommandDefinition(
            "UPDATE Correction SET Status = 'Kept' WHERE CorrectionId = @aCorrectionId",
            new { aCorrectionId },
            cancellationToken: aCt)).ConfigureAwait(false);
        if (vAffected == 0)
        {
            throw new KeyNotFoundException($"No correction with id {aCorrectionId}.");
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// When <c>Target</c> names a role's <c>Code</c> the role's wording is put back to <c>Before</c> as a new, versioned save (the same trail
    /// <see cref="Roles.RoleActions.SaveAsync"/> writes), so the history panel shows the undo
    /// honestly rather than silently rewriting the row. A <c>rule:&lt;id&gt;</c> or
    /// <c>step:&lt;process&gt;/&lt;step&gt;</c> target has its text put back the same way. A
    /// <c>Target</c> that names anything else still moves to <c>Undone</c>.
    /// </remarks>
    /// <exception cref="KeyNotFoundException">No correction has <paramref name="aCorrectionId"/>.</exception>
    public async Task UndoAsync(int aCorrectionId, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();

        var vCorrectionRow = await vConnection.QuerySingleOrDefaultAsync<CorrectionRow>(new CommandDefinition(
            "SELECT CorrectionId, Target, Before, After, Why, Status, CreatedUtc FROM Correction WHERE CorrectionId = @aCorrectionId",
            new { aCorrectionId },
            cancellationToken: aCt)).ConfigureAwait(false);
        if (vCorrectionRow is null)
        {
            throw new KeyNotFoundException($"No correction with id {aCorrectionId}.");
        }

        var vCorrection = ToCorrection(vCorrectionRow);

        await vConnection.ExecuteAsync(new CommandDefinition(
            "UPDATE Correction SET Status = 'Undone' WHERE CorrectionId = @aCorrectionId",
            new { aCorrectionId },
            cancellationToken: aCt)).ConfigureAwait(false);

        if (string.IsNullOrEmpty(vCorrection.Before))
        {
            return;
        }

        if (vCorrection.Target.StartsWith(RulePrefix, StringComparison.Ordinal)
            && int.TryParse(vCorrection.Target.AsSpan(RulePrefix.Length), out var vRuleId))
        {
            await vConnection.ExecuteAsync(new CommandDefinition(
                "UPDATE Rule SET Text = @Before, Version = Version + 1 WHERE RuleId = @vRuleId",
                new { vCorrection.Before, vRuleId },
                cancellationToken: aCt)).ConfigureAwait(false);
            return;
        }

        if (vCorrection.Target.StartsWith(StepPrefix, StringComparison.Ordinal))
        {
            var vParts = vCorrection.Target[StepPrefix.Length..].Split('/', 2);
            if (vParts.Length == 2)
            {
                await vConnection.ExecuteAsync(new CommandDefinition(
                    "UPDATE ProcessStepText SET Text = @Before, Version = Version + 1 WHERE ProcessCode = @ProcessCode AND StepCode = @StepCode",
                    new { vCorrection.Before, ProcessCode = vParts[0], StepCode = vParts[1] },
                    cancellationToken: aCt)).ConfigureAwait(false);
            }

            return;
        }

        var vRoleId = await vConnection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(
            "SELECT RoleId FROM Role WHERE Code = @Target",
            new { vCorrection.Target },
            cancellationToken: aCt)).ConfigureAwait(false);
        if (vRoleId is null)
        {
            return;
        }

        var vCurrentVersion = await vConnection.QuerySingleAsync<int>(new CommandDefinition(
            "SELECT Version FROM Role WHERE RoleId = @vRoleId", new { vRoleId }, cancellationToken: aCt))
            .ConfigureAwait(false);
        var vNewVersion = vCurrentVersion + 1;
        var vNowUtc = DateTime.UtcNow.ToString("O");

        await vConnection.ExecuteAsync(new CommandDefinition(
            "UPDATE Role SET Wording = @Before, Version = @vNewVersion, ValidFromUtc = @vNowUtc WHERE RoleId = @vRoleId",
            new { vCorrection.Before, vNewVersion, vNowUtc, vRoleId },
            cancellationToken: aCt)).ConfigureAwait(false);

        await vConnection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO RoleVersionHistory (RoleId, Version, Wording, ValidFromUtc, WhatChanged)
            VALUES (@vRoleId, @vNewVersion, @Before, @vNowUtc, 'Correction undone.')
            """,
            new { vRoleId, vNewVersion, vCorrection.Before, vNowUtc },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> ExportSeedAsync(CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vKept = (await vConnection.QueryAsync<SeedRow>(new CommandDefinition(
            "SELECT Target, After, Why FROM Correction WHERE Status = 'Kept' ORDER BY CreatedUtc",
            cancellationToken: aCt)).ConfigureAwait(false)).ToList();

        var vFile = new
        {
            schema = "chatur-corrections-seed",
            version = 1,
            exportedUtc = DateTime.UtcNow.ToString("O"),
            corrections = vKept
        };
        return JsonSerializer.Serialize(vFile, JsonOptions);
    }

    /// <summary>One kept correction's own shape in the seed export.</summary>
    private sealed class SeedRow
    {
        public string Target { get; set; } = string.Empty;

        public string? After { get; set; }

        public string Why { get; set; } = string.Empty;
    }

    /// <summary>
    /// Turns a SQLite row (whose <c>INTEGER</c> column always comes back as <see cref="long"/>, and
    /// whose <c>CreatedUtc</c> is stored as <c>TEXT</c>) into <see cref="Correction"/>. Dapper's
    /// constructor-based materialisation for a record needs an exact type match, so a plain
    /// <c>QueryAsync&lt;Correction&gt;</c> throws <see cref="InvalidOperationException"/> — this row
    /// type plus a narrowing cast and an explicit parse is the fix.
    /// </summary>
    private static Correction ToCorrection(CorrectionRow aRow) =>
        new(
            (int)aRow.CorrectionId,
            aRow.Target,
            aRow.Before,
            aRow.After,
            aRow.Why,
            aRow.Status,
            DateTime.Parse(aRow.CreatedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind));

    /// <summary>One <c>Correction</c> row, read with its native SQLite column widths.</summary>
    private sealed class CorrectionRow
    {
        public long CorrectionId { get; set; }

        public string Target { get; set; } = string.Empty;

        public string Before { get; set; } = string.Empty;

        public string After { get; set; } = string.Empty;

        public string Why { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string CreatedUtc { get; set; } = string.Empty;
    }
}
