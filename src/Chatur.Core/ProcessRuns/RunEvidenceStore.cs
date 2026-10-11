using System.Globalization;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;

namespace Chatur.Core.ProcessRuns;

/// <summary>
/// Keeps what a verifying run did and saw in the database (<c>ProcessRunCommand</c>,
/// <c>ProcessRunTest</c>), so the verdict step, and the screen after Chatur is reopened, read the same
/// evidence (REQ-UI-048).
/// </summary>
public sealed class RunEvidenceStore
{
    private readonly IDbConnectionFactory objConnections;
    private readonly IClock objClock;

    /// <summary>Creates the store.</summary>
    /// <param name="aConnections">Opens connections to Chatur's database.</param>
    /// <param name="aClock">The time source.</param>
    public RunEvidenceStore(IDbConnectionFactory aConnections, IClock aClock)
    {
        objConnections = aConnections;
        objClock = aClock;
    }

    /// <summary>Forgets the commands of one kind a run recorded, and for tests every test too, so a step run twice keeps one set.</summary>
    /// <param name="aRunId">The run.</param>
    /// <param name="aKind">The kind of command to forget.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    public async Task ClearAsync(int aRunId, RunCommandKind aKind, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM ProcessRunCommand WHERE RunId = @aRunId AND Kind = @Kind",
            new { aRunId, Kind = aKind.ToString() },
            cancellationToken: aCt)).ConfigureAwait(false);
        if (aKind == RunCommandKind.Test)
        {
            await vConnection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM ProcessRunTest WHERE RunId = @aRunId",
                new { aRunId },
                cancellationToken: aCt)).ConfigureAwait(false);
        }
    }

    /// <summary>Records a command the run ran.</summary>
    /// <param name="aRunId">The run.</param>
    /// <param name="aPosition">The step the command belongs to.</param>
    /// <param name="aRecord">What it came to.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    public async Task SaveCommandAsync(int aRunId, int aPosition, RunCommandRecord aRecord, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO ProcessRunCommand (RunId, Position, Kind, Command, Outcome, ExitCode, Warnings, DurationMs, TimeoutMs, LogTail, Reason, FinishedUtc)
            VALUES (@aRunId, @aPosition, @Kind, @Command, @Outcome, @ExitCode, @Warnings, @DurationMs, @TimeoutMs, @LogTail, @Reason, @vNow)
            """,
            new
            {
                aRunId,
                aPosition,
                Kind = aRecord.Kind.ToString(),
                aRecord.Command,
                Outcome = aRecord.Outcome.ToString(),
                aRecord.ExitCode,
                aRecord.Warnings,
                aRecord.DurationMs,
                aRecord.TimeoutMs,
                aRecord.LogTail,
                aRecord.Reason,
                vNow = objClock.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <summary>Records every test a test command reported.</summary>
    /// <param name="aRunId">The run.</param>
    /// <param name="aTests">The tests.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    public async Task SaveTestsAsync(int aRunId, IEnumerable<ReportedTest> aTests, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        using var vTransaction = vConnection.BeginTransaction();
        foreach (var vTest in aTests)
        {
            await vConnection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO ProcessRunTest (RunId, Source, Name, Outcome, Requirements, Message, DurationMs)
                VALUES (@aRunId, @Source, @Name, @Outcome, @Requirements, @Message, @DurationMs)
                """,
                new
                {
                    aRunId,
                    vTest.Source,
                    vTest.Name,
                    Outcome = vTest.Outcome.ToString(),
                    Requirements = vTest.Requirements.Count == 0 ? string.Empty : "," + string.Join(',', vTest.Requirements) + ",",
                    vTest.Message,
                    vTest.DurationMs
                },
                vTransaction,
                cancellationToken: aCt)).ConfigureAwait(false);
        }

        vTransaction.Commit();
    }

    /// <summary>The commands of one kind the run recorded, in the order they ran.</summary>
    /// <param name="aRunId">The run.</param>
    /// <param name="aKind">The kind of command.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    public async Task<IReadOnlyList<RunCommandRecord>> CommandsAsync(int aRunId, RunCommandKind aKind, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRows = await vConnection.QueryAsync<CommandRow>(new CommandDefinition(
            """
            SELECT Kind, Command, Outcome, ExitCode, Warnings, DurationMs, TimeoutMs, LogTail, Reason
            FROM ProcessRunCommand WHERE RunId = @aRunId AND Kind = @Kind ORDER BY ProcessRunCommandId
            """,
            new { aRunId, Kind = aKind.ToString() },
            cancellationToken: aCt)).ConfigureAwait(false);
        return vRows.Select(aRow => new RunCommandRecord(
            Enum.Parse<RunCommandKind>(aRow.Kind),
            aRow.Command,
            Enum.Parse<RunCommandOutcome>(aRow.Outcome),
            (int?)aRow.ExitCode,
            (int?)aRow.Warnings,
            aRow.DurationMs,
            aRow.TimeoutMs,
            aRow.LogTail,
            aRow.Reason)).ToList();
    }

    /// <summary>Every test the run recorded, in the order reported.</summary>
    /// <param name="aRunId">The run.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    public async Task<IReadOnlyList<ReportedTest>> TestsAsync(int aRunId, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRows = await vConnection.QueryAsync<TestRow>(new CommandDefinition(
            "SELECT Source, Name, Outcome, Requirements, Message, DurationMs FROM ProcessRunTest WHERE RunId = @aRunId ORDER BY ProcessRunTestId",
            new { aRunId },
            cancellationToken: aCt)).ConfigureAwait(false);
        return vRows.Select(aRow => new ReportedTest(
            aRow.Source,
            aRow.Name,
            Enum.Parse<ProcessTestOutcome>(aRow.Outcome),
            aRow.Requirements.Split(',', StringSplitOptions.RemoveEmptyEntries),
            aRow.Message,
            aRow.DurationMs)).ToList();
    }

    /// <summary>What the run's tests showed, ready for grading and for the screen.</summary>
    /// <param name="aRunId">The run.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    public async Task<TestEvidence> TestEvidenceAsync(int aRunId, CancellationToken aCt = default) =>
        TestEvidence.From(
            await CommandsAsync(aRunId, RunCommandKind.Test, aCt).ConfigureAwait(false),
            await TestsAsync(aRunId, aCt).ConfigureAwait(false));

    private sealed class CommandRow
    {
        public string Kind { get; set; } = string.Empty;

        public string Command { get; set; } = string.Empty;

        public string Outcome { get; set; } = string.Empty;

        public long? ExitCode { get; set; }

        public long? Warnings { get; set; }

        public long DurationMs { get; set; }

        public long TimeoutMs { get; set; }

        public string LogTail { get; set; } = string.Empty;

        public string? Reason { get; set; }
    }

    private sealed class TestRow
    {
        public string Source { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Outcome { get; set; } = string.Empty;

        public string Requirements { get; set; } = string.Empty;

        public string? Message { get; set; }

        public long DurationMs { get; set; }
    }
}
