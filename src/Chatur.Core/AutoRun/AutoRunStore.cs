using System.Globalization;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;

namespace Chatur.Core.AutoRun;

/// <summary>The rows of an end-to-end run: the run, the processes of its chain and its record (REQ-FN-061, REQ-FN-064, REQ-FN-065).</summary>
public sealed class AutoRunStore
{
    private readonly IDbConnectionFactory objConnections;
    private readonly IClock objClock;

    /// <summary>Creates the store.</summary>
    /// <param name="aConnections">Opens connections to Chatur's database.</param>
    /// <param name="aClock">The time source.</param>
    public AutoRunStore(IDbConnectionFactory aConnections, IClock aClock)
    {
        objConnections = aConnections;
        objClock = aClock;
    }

    /// <summary>Reads one run's row.</summary>
    /// <param name="aAutoRunId">The run.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    public async Task<AutoRunRow?> RunAsync(int aAutoRunId, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        return await vConnection.QuerySingleOrDefaultAsync<AutoRunRow>(new CommandDefinition(
            RunSelect + " WHERE r.AutoRunId = @aAutoRunId",
            new { aAutoRunId },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <summary>The run on a project that is going now, or <see langword="null"/>.</summary>
    /// <param name="aProjectId">The project.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    public async Task<AutoRunRow?> GoingAsync(int aProjectId, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        return await vConnection.QueryFirstOrDefaultAsync<AutoRunRow>(new CommandDefinition(
            RunSelect + " WHERE r.ProjectId = @aProjectId AND r.State IN ('Running', 'WaitingForLimit', 'Paused') ORDER BY r.AutoRunId DESC LIMIT 1",
            new { aProjectId },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <summary>A project's runs, newest first.</summary>
    /// <param name="aProjectId">The project.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    public async Task<IReadOnlyList<AutoRunRow>> ListAsync(int aProjectId, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        return (await vConnection.QueryAsync<AutoRunRow>(new CommandDefinition(
            RunSelect + " WHERE r.ProjectId = @aProjectId ORDER BY r.AutoRunId DESC",
            new { aProjectId },
            cancellationToken: aCt)).ConfigureAwait(false)).ToList();
    }

    /// <summary>The chain's rows of a run, in order.</summary>
    /// <param name="aAutoRunId">The run.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    public async Task<IReadOnlyList<AutoRunProcessRow>> ProcessesAsync(int aAutoRunId, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        return (await vConnection.QueryAsync<AutoRunProcessRow>(new CommandDefinition(
            "SELECT Position, ProcessCode, State, ProcessRunId, Retries FROM AutoRunProcess WHERE AutoRunId = @aAutoRunId ORDER BY Position",
            new { aAutoRunId },
            cancellationToken: aCt)).ConfigureAwait(false)).ToList();
    }

    /// <summary>A run's record, oldest first.</summary>
    /// <param name="aAutoRunId">The run.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    public async Task<IReadOnlyList<AutoRunEvent>> EventsAsync(int aAutoRunId, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRows = await vConnection.QueryAsync<EventRow>(new CommandDefinition(
            "SELECT AutoRunEventId, AtUtc, Kind, ProcessPosition, ProcessCode, Message, UntilUtc FROM AutoRunEvent WHERE AutoRunId = @aAutoRunId ORDER BY AutoRunEventId",
            new { aAutoRunId },
            cancellationToken: aCt)).ConfigureAwait(false);
        return vRows.Select(aRow => new AutoRunEvent(
            (int)aRow.AutoRunEventId,
            ParseStamp(aRow.AtUtc),
            Enum.Parse<AutoRunEventKind>(aRow.Kind),
            aRow.ProcessPosition is { } vPosition ? (int)vPosition : null,
            aRow.ProcessCode,
            aRow.Message,
            aRow.UntilUtc is null ? null : ParseStamp(aRow.UntilUtc))).ToList();
    }

    /// <summary>Writes a new run with its chain and a Started entry, and returns its id.</summary>
    /// <param name="aProjectId">The project.</param>
    /// <param name="aBrief">The brief.</param>
    /// <param name="aChain">The process codes of the chain, in order.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    public async Task<int> CreateAsync(int aProjectId, string aBrief, IReadOnlyList<string> aChain, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        var vNow = Stamp(objClock.UtcNow);
        var vId = await vConnection.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            INSERT INTO AutoRun (ProjectId, Brief, State, CurrentProcess, StartedUtc)
            VALUES (@aProjectId, @aBrief, 'Running', 1, @vNow);
            SELECT last_insert_rowid();
            """,
            new { aProjectId, aBrief, vNow },
            cancellationToken: aCt)).ConfigureAwait(false);
        for (var vIndex = 0; vIndex < aChain.Count; vIndex++)
        {
            await vConnection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO AutoRunProcess (AutoRunId, Position, ProcessCode, State) VALUES (@vId, @Position, @Code, 'ToCome')",
                new { vId, Position = vIndex + 1, Code = aChain[vIndex] },
                cancellationToken: aCt)).ConfigureAwait(false);
        }

        await AddEventAsync(vId, AutoRunEventKind.Started, null, null, "The run started from the brief.", null, aCt).ConfigureAwait(false);
        return vId;
    }

    /// <summary>Sets a run's state and the places that go with it; <see langword="null"/> clears a place.</summary>
    /// <param name="aAutoRunId">The run.</param>
    /// <param name="aState">The new state.</param>
    /// <param name="aStopReason">Why it stopped, for <see cref="AutoRunState.Stopped"/>.</param>
    /// <param name="aResumeAtUtc">When it carries on, for <see cref="AutoRunState.WaitingForLimit"/>.</param>
    /// <param name="aEnded">Whether the run is over, so its end is stamped.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    public async Task SetStateAsync(int aAutoRunId, AutoRunState aState, string? aStopReason, DateTime? aResumeAtUtc, bool aEnded, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE AutoRun
               SET State = @State, StopReason = @aStopReason, ResumeAtUtc = @Resume,
                   EndedUtc = CASE WHEN @aEnded = 1 THEN @vNow ELSE EndedUtc END
             WHERE AutoRunId = @aAutoRunId
            """,
            new
            {
                State = aState.ToString(),
                aStopReason,
                Resume = aResumeAtUtc is null ? null : Stamp(aResumeAtUtc.Value),
                aEnded,
                vNow = Stamp(objClock.UtcNow),
                aAutoRunId
            },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <summary>Moves a run to the next process of its chain.</summary>
    /// <param name="aAutoRunId">The run.</param>
    /// <param name="aPosition">The place in the chain it is now on.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    public async Task SetCurrentAsync(int aAutoRunId, int aPosition, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            "UPDATE AutoRun SET CurrentProcess = @aPosition WHERE AutoRunId = @aAutoRunId",
            new { aPosition, aAutoRunId },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <summary>Sets the state of one process of the chain, and the process run that carries it out when given.</summary>
    /// <param name="aAutoRunId">The run.</param>
    /// <param name="aPosition">The place in the chain.</param>
    /// <param name="aState">The new state.</param>
    /// <param name="aProcessRunId">The process run, or <see langword="null"/> to leave it as it is.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    public async Task SetProcessAsync(int aAutoRunId, int aPosition, AutoRunProcessState aState, int? aProcessRunId, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            "UPDATE AutoRunProcess SET State = @State, ProcessRunId = COALESCE(@aProcessRunId, ProcessRunId) WHERE AutoRunId = @aAutoRunId AND Position = @aPosition",
            new { State = aState.ToString(), aProcessRunId, aAutoRunId, aPosition },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <summary>Counts one retry against a process of the chain.</summary>
    /// <param name="aAutoRunId">The run.</param>
    /// <param name="aPosition">The place in the chain.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    public async Task AddRetryAsync(int aAutoRunId, int aPosition, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            "UPDATE AutoRunProcess SET Retries = Retries + 1 WHERE AutoRunId = @aAutoRunId AND Position = @aPosition",
            new { aAutoRunId, aPosition },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <summary>Adds an entry to a run's record.</summary>
    /// <param name="aAutoRunId">The run.</param>
    /// <param name="aKind">What happened.</param>
    /// <param name="aPosition">The process it concerns, or <see langword="null"/>.</param>
    /// <param name="aProcessCode">The process it concerns, or <see langword="null"/>.</param>
    /// <param name="aMessage">One plain sentence.</param>
    /// <param name="aUntilUtc">When a wait ends, or <see langword="null"/>.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    public async Task AddEventAsync(int aAutoRunId, AutoRunEventKind aKind, int? aPosition, string? aProcessCode, string aMessage, DateTime? aUntilUtc, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO AutoRunEvent (AutoRunId, AtUtc, Kind, ProcessPosition, ProcessCode, Message, UntilUtc)
            VALUES (@aAutoRunId, @vNow, @Kind, @aPosition, @aProcessCode, @aMessage, @Until)
            """,
            new
            {
                aAutoRunId,
                vNow = Stamp(objClock.UtcNow),
                Kind = aKind.ToString(),
                aPosition,
                aProcessCode,
                aMessage,
                Until = aUntilUtc is null ? null : Stamp(aUntilUtc.Value)
            },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <summary>A time as the database holds it.</summary>
    /// <param name="aUtc">The time.</param>
    public static string Stamp(DateTime aUtc) => aUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    /// <summary>A time read back from the database.</summary>
    /// <param name="aStamp">The stored text.</param>
    public static DateTime ParseStamp(string aStamp) =>
        DateTime.Parse(aStamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private const string RunSelect =
        """
        SELECT r.AutoRunId, r.ProjectId, p.Name AS ProjectName, r.Brief, r.State, r.CurrentProcess,
               r.StartedUtc, r.EndedUtc, r.StopReason, r.ResumeAtUtc
          FROM AutoRun r JOIN Project p ON p.ProjectId = r.ProjectId
        """;

    private sealed class EventRow
    {
        public long AutoRunEventId { get; set; }

        public string AtUtc { get; set; } = string.Empty;

        public string Kind { get; set; } = string.Empty;

        public long? ProcessPosition { get; set; }

        public string? ProcessCode { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? UntilUtc { get; set; }
    }
}
