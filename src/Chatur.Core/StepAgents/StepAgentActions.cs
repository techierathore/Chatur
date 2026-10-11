using System.Globalization;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Chatur.Core.ProcessRuns;
using Chatur.Core.Processes;
using Dapper;

namespace Chatur.Core.StepAgents;

/// <summary>
/// <see cref="IStepAgentActions"/> over the existing agent path. A work step starts a session with the process's agent
/// (<see cref="IAgentActions.StartSessionAsync"/>) and sends it the step's task through <see cref="IAgentActions.SendAsync"/>,
/// so the guard pipeline (rights, ask-me-first, the source-control refusal), the routed tier and the session record are
/// the agent's own. What the agent did is read back from that session's <c>SessionEvent</c> and <c>Change</c> rows, so
/// there is one record of it (REQ-FN-084..087, REQ-FN-090).
/// </summary>
public sealed class StepAgentActions : IStepAgentActions
{
    private const string WorkingState = "working";
    private const string FinishedState = "finished";
    private const string StoppedState = "stopped";

    private readonly IDbConnectionFactory objConnections;
    private readonly IAgentActions objAgents;
    private readonly IClock objClock;

    /// <summary>Creates the action implementation.</summary>
    /// <param name="aConnections">Opens connections to Chatur's database.</param>
    /// <param name="aAgents">Starts the session and sends the task.</param>
    /// <param name="aClock">The time source.</param>
    public StepAgentActions(IDbConnectionFactory aConnections, IAgentActions aAgents, IClock aClock)
    {
        objConnections = aConnections;
        objAgents = aAgents;
        objClock = aClock;
    }

    /// <inheritdoc />
    public async Task<ProcessStepWork> HandAsync(int aRunId, int aPosition, CancellationToken aCt = default)
    {
        var vRun = await ReadRunAsync(aRunId, aCt).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Run {aRunId} was not found.");
        var vDefinition = ProcessCatalogue.Find(vRun.ProcessCode)
            ?? throw new InvalidOperationException($"Process {vRun.ProcessCode} is not a known process.");

        var vExisting = await ReadRowAsync(aRunId, aPosition, aCt).ConfigureAwait(false);
        if (vExisting is { State: FinishedState or StoppedState })
        {
            return await ToWorkAsync(vExisting, aCt).ConfigureAwait(false);
        }

        var vRole = await ReadRoleAsync(vDefinition.Agent, aCt).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"The agent \"{vDefinition.Agent}\" of {vRun.ProcessCode} is not one of Chatur's agents.");

        string vMessage;
        int vSessionId;
        if (vExisting is not null)
        {
            // The step was handed before and was interrupted (a usage limit, a stall): carry on in the same session.
            vSessionId = (int)vExisting.SessionId;
            vMessage = StepTaskText.Resume();
        }
        else
        {
            var vSession = await objAgents.StartSessionAsync(vRun.ProjectId, (int)vRole.RoleId, aCt).ConfigureAwait(false);
            vSessionId = vSession.SessionId;
            vMessage = await BuildTaskAsync(vRun, vDefinition, aPosition, aCt).ConfigureAwait(false);
            await InsertRowAsync(aRunId, aPosition, vSessionId, vRole, vMessage, aCt).ConfigureAwait(false);
        }

        var vMode = await ModeAsync(vSessionId, vRun.ProjectId, aCt).ConfigureAwait(false);
        string? vAnsweringModel = null;
        await foreach (var vChunk in objAgents.SendAsync(vSessionId, vMessage, (int)vRole.RoleId, null, vMode, aCt).ConfigureAwait(false))
        {
            if (vChunk.IsFinal)
            {
                vAnsweringModel = vChunk.ModelName;
            }
        }

        var vAfter = await ReadRowAsync(aRunId, aPosition, aCt).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The handed step's record is gone.");
        if (vAfter.State == StoppedState)
        {
            return await ToWorkAsync(vAfter, aCt).ConfigureAwait(false);
        }

        aCt.ThrowIfCancellationRequested();
        var vFinal = await LastAssistantAsync(vSessionId, CancellationToken.None).ConfigureAwait(false);
        await FinishRowAsync(aRunId, aPosition, vAnsweringModel ?? vAfter.ModelName, vFinal?.Payload, vAnsweringModel is null, CancellationToken.None).ConfigureAwait(false);
        var vDone = await ReadRowAsync(aRunId, aPosition, CancellationToken.None).ConfigureAwait(false);
        return await ToWorkAsync(vDone!, CancellationToken.None).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ProcessStepWork?> WorkAsync(int aRunId, int aPosition, CancellationToken aCt = default)
    {
        var vRow = await ReadRowAsync(aRunId, aPosition, aCt).ConfigureAwait(false);
        return vRow is null ? null : await ToWorkAsync(vRow, aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, ProcessStepWork>> WorksAsync(int aRunId, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRows = (await vConnection.QueryAsync<WorkRow>(new CommandDefinition(
            WorkSelect + " WHERE RunId = @aRunId ORDER BY Position",
            new { aRunId },
            cancellationToken: aCt)).ConfigureAwait(false)).ToList();
        var vWorks = new Dictionary<int, ProcessStepWork>();
        foreach (var vRow in vRows)
        {
            vWorks[(int)vRow.Position] = await ToWorkAsync(vRow, aCt).ConfigureAwait(false);
        }

        return vWorks;
    }

    /// <inheritdoc />
    public async Task<bool> StopAsync(int aRunId, CancellationToken aCt = default)
    {
        WorkRow? vWorking;
        using (var vConnection = objConnections.OpenConnection())
        {
            vWorking = await vConnection.QueryFirstOrDefaultAsync<WorkRow>(new CommandDefinition(
                WorkSelect + " WHERE RunId = @aRunId AND State = @WorkingState ORDER BY Position DESC LIMIT 1",
                new { aRunId, WorkingState },
                cancellationToken: aCt)).ConfigureAwait(false);
            if (vWorking is not null)
            {
                // Marked first, so the waiting hand-over finds the step stopped by the owner when its send returns.
                await vConnection.ExecuteAsync(new CommandDefinition(
                    "UPDATE ProcessRunStepWork SET State = @StoppedState, FinishedUtc = @vNow WHERE ProcessRunStepWorkId = @Id",
                    new { StoppedState, vNow = Stamp(objClock.UtcNow), vWorking.Id },
                    cancellationToken: aCt)).ConfigureAwait(false);
            }
        }

        if (vWorking is null)
        {
            return false;
        }

        await objAgents.StopAsync((int)vWorking.SessionId, aCt).ConfigureAwait(false);
        return true;
    }

    private const string WorkSelect =
        "SELECT ProcessRunStepWorkId AS Id, RunId, Position, SessionId, AgentName, ModelName, Tier, State, Task, FinalMessage FROM ProcessRunStepWork";

    private async Task<string> BuildTaskAsync(RunRow aRun, ProcessDefinition aDefinition, int aPosition, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        var vProject = await vConnection.QuerySingleOrDefaultAsync<ProjectRow>(new CommandDefinition(
            "SELECT Name, Path FROM Project WHERE ProjectId = @ProjectId",
            new { aRun.ProjectId },
            cancellationToken: aCt)).ConfigureAwait(false);
        var vWording = await vConnection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "SELECT Text FROM ProcessStepText WHERE ProcessCode = @ProcessCode AND StepCode = @StepCode",
            new { aRun.ProcessCode, StepCode = $"s{aPosition}" },
            cancellationToken: aCt)).ConfigureAwait(false) ?? $"s{aPosition}";
        return StepTaskText.Build(aDefinition, aRun.RunId, aPosition, vWording, vProject?.Name ?? "the project", vProject?.Path ?? string.Empty);
    }

    /// <summary>The mode a handed step runs in: go-ahead for an unattended run (REQ-FN-091), else the owner's own session mode on the project.</summary>
    private async Task<SessionMode> ModeAsync(int aSessionId, int aProjectId, CancellationToken aCt)
    {
        if (ModelCallScope.IsForRun)
        {
            return SessionMode.GoAhead;
        }

        using var vConnection = objConnections.OpenConnection();
        var vMode = await vConnection.QueryFirstOrDefaultAsync<string>(new CommandDefinition(
            """
            SELECT Mode FROM Session
            WHERE ProjectId = @aProjectId AND SessionId <> @aSessionId AND SessionId NOT IN (SELECT SessionId FROM ProcessRunStepWork)
            ORDER BY StartedUtc DESC, SessionId DESC LIMIT 1
            """,
            new { aProjectId, aSessionId },
            cancellationToken: aCt)).ConfigureAwait(false);
        return Enum.TryParse<SessionMode>(vMode, out var vParsed) ? vParsed : SessionMode.AskFirst;
    }

    private async Task InsertRowAsync(int aRunId, int aPosition, int aSessionId, RoleRow aRole, string aTask, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        var vModel = await vConnection.QueryFirstOrDefaultAsync<string>(new CommandDefinition(
            """
            SELECT m.Identifier FROM Model m JOIN Provider p ON p.ProviderId = m.ProviderId
            WHERE m.Tier = @Tier AND p.State = 'Connected' ORDER BY m.ModelId LIMIT 1
            """,
            new { aRole.Tier },
            cancellationToken: aCt)).ConfigureAwait(false)
            ?? await vConnection.QueryFirstOrDefaultAsync<string>(new CommandDefinition(
                "SELECT m.Identifier FROM Model m JOIN Provider p ON p.ProviderId = m.ProviderId WHERE p.State = 'Connected' ORDER BY m.ModelId LIMIT 1",
                cancellationToken: aCt)).ConfigureAwait(false);
        await vConnection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO ProcessRunStepWork (RunId, Position, SessionId, AgentName, ModelName, Tier, State, Task, StartedUtc)
            VALUES (@aRunId, @aPosition, @aSessionId, @AgentName, @vModel, @Tier, @WorkingState, @aTask, @vNow)
            """,
            new { aRunId, aPosition, aSessionId, AgentName = aRole.Name, vModel, aRole.Tier, WorkingState, aTask, vNow = Stamp(objClock.UtcNow) },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    private async Task FinishRowAsync(int aRunId, int aPosition, string? aModel, string? aFinal, bool aNoModel, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE ProcessRunStepWork
               SET State = @FinishedState, ModelName = @aModel, FinalMessage = @aFinal, FinishedUtc = @vNow
             WHERE RunId = @aRunId AND Position = @aPosition AND State = @WorkingState
            """,
            new { FinishedState, WorkingState, aModel = aNoModel ? null : aModel, aFinal, vNow = Stamp(objClock.UtcNow), aRunId, aPosition },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    private async Task<WorkRow?> ReadRowAsync(int aRunId, int aPosition, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        return await vConnection.QuerySingleOrDefaultAsync<WorkRow>(new CommandDefinition(
            WorkSelect + " WHERE RunId = @aRunId AND Position = @aPosition",
            new { aRunId, aPosition },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    private async Task<RunRow?> ReadRunAsync(int aRunId, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        return await vConnection.QuerySingleOrDefaultAsync<RunRow>(new CommandDefinition(
            "SELECT RunId, ProjectId, ProcessCode FROM ProcessRun WHERE RunId = @aRunId",
            new { aRunId },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    private async Task<RoleRow?> ReadRoleAsync(string aName, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        return await vConnection.QueryFirstOrDefaultAsync<RoleRow>(new CommandDefinition(
            "SELECT RoleId, Name, Tier FROM Role WHERE lower(Name) = lower(@aName) ORDER BY Version DESC LIMIT 1",
            new { aName },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    private async Task<EventRow?> LastAssistantAsync(int aSessionId, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        return await vConnection.QueryFirstOrDefaultAsync<EventRow>(new CommandDefinition(
            "SELECT Kind, Payload, Model FROM SessionEvent WHERE SessionId = @aSessionId AND Kind = 'assistant' ORDER BY Seq DESC LIMIT 1",
            new { aSessionId },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <summary>The work as the session shows it now: files from its changes, commands and refusals from its events.</summary>
    private async Task<ProcessStepWork> ToWorkAsync(WorkRow aRow, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        var vSessionId = (int)aRow.SessionId;
        var vChanges = (await vConnection.QueryAsync<ChangeRow>(new CommandDefinition(
            "SELECT FilePath, Status FROM Change WHERE SessionId = @vSessionId ORDER BY ChangeId",
            new { vSessionId },
            cancellationToken: aCt)).ConfigureAwait(false)).ToList();
        var vEvents = (await vConnection.QueryAsync<EventRow>(new CommandDefinition(
            "SELECT Kind, Payload, Model FROM SessionEvent WHERE SessionId = @vSessionId ORDER BY Seq",
            new { vSessionId },
            cancellationToken: aCt)).ConfigureAwait(false)).ToList();

        var vWritten = vChanges.Where(aChange => aChange.Status == "Approved").Select(aChange => aChange.FilePath).Distinct(StringComparer.Ordinal).ToList();
        var vProposed = vChanges.Where(aChange => aChange.Status == "Proposed").Select(aChange => aChange.FilePath).Distinct(StringComparer.Ordinal).ToList();
        var vCommands = vEvents.Where(aEvent => aEvent.Kind == "tool" && aEvent.Payload.StartsWith(StepTaskText.BuildCommand, StringComparison.Ordinal))
            .Select(aEvent => aEvent.Payload).ToList();
        var vRefusals = vEvents
            .Where(aEvent => aEvent.Kind == "refusal" && !aEvent.Payload.Contains("ask me first", StringComparison.OrdinalIgnoreCase))
            .Select(aEvent => aEvent.Payload).ToList();
        var vModel = vEvents.LastOrDefault(aEvent => aEvent.Kind == "assistant" && aEvent.Model is not null)?.Model ?? aRow.ModelName;
        var vState = aRow.State switch
        {
            FinishedState => ProcessStepWorkState.Finished,
            StoppedState => ProcessStepWorkState.Stopped,
            _ => ProcessStepWorkState.Working
        };

        return new ProcessStepWork(
            vSessionId,
            aRow.AgentName,
            vModel,
            (int)aRow.Tier,
            vState,
            aRow.Task,
            vWritten,
            vProposed,
            vCommands,
            vRefusals,
            aRow.FinalMessage);
    }

    private static string Stamp(DateTime aUtc) => aUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private sealed class WorkRow
    {
        public long Id { get; set; }

        public long RunId { get; set; }

        public long Position { get; set; }

        public long SessionId { get; set; }

        public string AgentName { get; set; } = string.Empty;

        public string? ModelName { get; set; }

        public long Tier { get; set; }

        public string State { get; set; } = string.Empty;

        public string Task { get; set; } = string.Empty;

        public string? FinalMessage { get; set; }
    }

    private sealed class RunRow
    {
        public int RunId { get; set; }

        public int ProjectId { get; set; }

        public string ProcessCode { get; set; } = string.Empty;
    }

    private sealed class RoleRow
    {
        public long RoleId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Tier { get; set; }
    }

    private sealed class ProjectRow
    {
        public string Name { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;
    }

    private sealed class ChangeRow
    {
        public string FilePath { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }

    private sealed class EventRow
    {
        public string Kind { get; set; } = string.Empty;

        public string Payload { get; set; } = string.Empty;

        public string? Model { get; set; }
    }
}
