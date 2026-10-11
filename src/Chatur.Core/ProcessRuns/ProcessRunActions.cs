using System.Collections.Concurrent;
using System.Globalization;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Chatur.Core.Processes;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Chatur.Core.ProcessRuns;

/// <summary>
/// <see cref="IProcessRunActions"/> over the <c>ProcessRun</c> row <c>IProcessActions.StartAsync</c>
/// writes and the <c>ProcessRunStep</c> rows of the steps already finished. The database is the only
/// place a run's position is kept, so a new instance after a restart carries on exactly where the old
/// one stopped (REQ-FN-055). The steps themselves are <see cref="ProcessCatalogue"/>'s.
/// </summary>
public sealed class ProcessRunActions : IProcessRunActions, IUnattendedProcessRuns
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> RunLocks = new();

    private readonly IDbConnectionFactory objConnections;
    private readonly IProcessStepExecutor objExecutor;
    private readonly IMeasurementActions objMeasurements;
    private readonly IRunVerdictWriter objVerdicts;
    private readonly RunEvidenceStore objEvidence;
    private readonly IStepAgentActions? objStepAgents;
    private readonly IClock objClock;
    private readonly ILogger<ProcessRunActions> objLogger;

    /// <summary>Creates the action implementation.</summary>
    /// <param name="aConnections">Opens connections to Chatur's database.</param>
    /// <param name="aExecutor">Carries out the steps and checks.</param>
    /// <param name="aMeasurements">Appends the run's record when it ends (REQ-FN-054).</param>
    /// <param name="aVerdicts">Writes a verifying run's verdicts into the checklist (REQ-UI-048).</param>
    /// <param name="aEvidence">Reads what the run's tests showed, for the evidence beside each verdict.</param>
    /// <param name="aClock">The time source.</param>
    /// <param name="aLogger">Where a failure inside Chatur is logged.</param>
    /// <param name="aStepAgents">Shows what the agent a step was handed to did, and ends its work when the owner stops the run (REQ-FN-087, REQ-FN-090); <see langword="null"/> when steps are not handed.</param>
    public ProcessRunActions(
        IDbConnectionFactory aConnections,
        IProcessStepExecutor aExecutor,
        IMeasurementActions aMeasurements,
        IRunVerdictWriter aVerdicts,
        RunEvidenceStore aEvidence,
        IClock aClock,
        ILogger<ProcessRunActions> aLogger,
        IStepAgentActions? aStepAgents = null)
    {
        objStepAgents = aStepAgents;
        objConnections = aConnections;
        objExecutor = aExecutor;
        objMeasurements = aMeasurements;
        objVerdicts = aVerdicts;
        objEvidence = aEvidence;
        objClock = aClock;
        objLogger = aLogger;
    }

    /// <inheritdoc />
    public async Task<ProcessRunView?> GetAsync(int aRunId, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRun = await vConnection.QuerySingleOrDefaultAsync<RunRow>(new CommandDefinition(
            "SELECT RunId, ProjectId, ProcessCode, State, CurrentStep, StartedUtc, EndedUtc FROM ProcessRun WHERE RunId = @aRunId",
            new { aRunId },
            cancellationToken: aCt)).ConfigureAwait(false);
        var vDefinition = vRun is null ? null : ProcessCatalogue.Find(vRun.ProcessCode);
        if (vRun is null || vDefinition is null)
        {
            return null;
        }

        var vDone = (await vConnection.QueryAsync<DoneRow>(new CommandDefinition(
            "SELECT Position, State, Result, Answer FROM ProcessRunStep WHERE RunId = @aRunId",
            new { aRunId },
            cancellationToken: aCt)).ConfigureAwait(false)).ToDictionary(aRow => aRow.Position);
        var vWording = (await vConnection.QueryAsync<WordingRow>(new CommandDefinition(
            "SELECT StepCode, Text FROM ProcessStepText WHERE ProcessCode = @ProcessCode",
            new { vRun.ProcessCode },
            cancellationToken: aCt)).ConfigureAwait(false)).ToDictionary(aRow => aRow.StepCode, aRow => aRow.Text);

        var vWorks = objStepAgents is null
            ? new Dictionary<int, ProcessStepWork>()
            : (await objStepAgents.WorksAsync(aRunId, aCt).ConfigureAwait(false)).ToDictionary(aEntry => aEntry.Key, aEntry => aEntry.Value);
        var vState = ParseState(vRun.State);
        var vSteps = new List<ProcessRunStepView>(vDefinition.Steps);
        for (var vPosition = 1; vPosition <= vDefinition.Steps; vPosition++)
        {
            var vCode = $"s{vPosition}";
            vDone.TryGetValue(vPosition, out var vRow);
            var vStepState = vRow is not null
                ? (vRow.State == "failed" ? ProcessStepState.Failed : ProcessStepState.Done)
                : vPosition == vRun.CurrentStep && vState == ProcessRunState.Running ? ProcessStepState.Running
                : vPosition == vRun.CurrentStep && vState == ProcessRunState.Waiting ? ProcessStepState.Waiting
                : ProcessStepState.ToCome;
            vSteps.Add(new ProcessRunStepView(
                vPosition,
                vCode,
                vWording.TryGetValue(vCode, out var vText) ? vText : vCode,
                vDefinition.KindAt(vPosition),
                vStepState,
                vRow?.Result,
                vRow?.Answer,
                vWorks.GetValueOrDefault(vPosition)));
        }

        var vCurrent = vState == ProcessRunState.Finished ? (int?)null : vRun.CurrentStep;
        return new ProcessRunView(
            vRun.RunId,
            vRun.ProjectId,
            vRun.ProcessCode,
            vState,
            ParseStamp(vRun.StartedUtc),
            vRun.EndedUtc is null ? null : ParseStamp(vRun.EndedUtc),
            vCurrent,
            vSteps);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProcessRunSummary>> ListAsync(int aProjectId, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRows = await vConnection.QueryAsync<RunRow>(new CommandDefinition(
            "SELECT RunId, ProjectId, ProcessCode, State, CurrentStep, StartedUtc, EndedUtc FROM ProcessRun WHERE ProjectId = @aProjectId ORDER BY RunId DESC",
            new { aProjectId },
            cancellationToken: aCt)).ConfigureAwait(false);

        return vRows.Select(aRow =>
        {
            var vState = ParseState(aRow.State);
            return new ProcessRunSummary(
                aRow.RunId,
                aRow.ProcessCode,
                vState,
                ParseStamp(aRow.StartedUtc),
                vState == ProcessRunState.Finished ? null : aRow.CurrentStep);
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<ProcessRunView?> AdvanceAsync(int aRunId, CancellationToken aCt = default)
    {
        var vLock = RunLocks.GetOrAdd(aRunId, static _ => new SemaphoreSlim(1, 1));
        await vLock.WaitAsync(aCt).ConfigureAwait(false);
        try
        {
            return await AdvanceLockedAsync(aRunId, null, aCt).ConfigureAwait(false);
        }
        finally
        {
            vLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ProcessRunView?> AdvanceUnattendedAsync(int aRunId, UnattendedAdvance aPolicy, CancellationToken aCt = default)
    {
        var vLock = RunLocks.GetOrAdd(aRunId, static _ => new SemaphoreSlim(1, 1));
        await vLock.WaitAsync(aCt).ConfigureAwait(false);
        try
        {
            var vRun = await GetAsync(aRunId, aCt).ConfigureAwait(false);
            if (vRun is { State: ProcessRunState.Waiting, CurrentPosition: { } vWaitingAt })
            {
                // A run the owner left at a stop is handed on: the stop is answered below like any other.
                await SetRunAsync(aRunId, ProcessRunState.Running, vWaitingAt, aCt).ConfigureAwait(false);
            }

            return await AdvanceLockedAsync(aRunId, aPolicy, aCt).ConfigureAwait(false);
        }
        finally
        {
            vLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ProcessRunView?> AnswerAsync(int aRunId, ProcessAnswer aAnswer, CancellationToken aCt = default)
    {
        var vLock = RunLocks.GetOrAdd(aRunId, static _ => new SemaphoreSlim(1, 1));
        await vLock.WaitAsync(aCt).ConfigureAwait(false);
        try
        {
            var vRun = await GetAsync(aRunId, aCt).ConfigureAwait(false);
            if (vRun is null)
            {
                return null;
            }

            if (vRun.State != ProcessRunState.Waiting || vRun.CurrentPosition is not { } vPosition)
            {
                throw new InvalidOperationException("The run is not waiting for an answer.");
            }

            if (aAnswer == ProcessAnswer.Later)
            {
                return vRun;
            }

            await RecordStepAsync(aRunId, vPosition, "done", $"You answered {aAnswer.ToString().ToLowerInvariant()}", aAnswer.ToString(), aCt).ConfigureAwait(false);
            if (vPosition >= vRun.Steps.Count)
            {
                await EndRunAsync(vRun, ProcessRunState.Finished, aCt).ConfigureAwait(false);
            }
            else
            {
                await SetRunAsync(aRunId, ProcessRunState.Running, vPosition + 1, aCt).ConfigureAwait(false);
            }

            return await AdvanceLockedAsync(aRunId, null, aCt).ConfigureAwait(false);
        }
        finally
        {
            vLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ProcessRunView?> StopAsync(int aRunId, CancellationToken aCt = default)
    {
        // A step handed to an agent holds the run's lock for as long as the agent works, so the agent is told to stop
        // before the lock is asked for; its step then ends as stopped by the owner and the run with it (REQ-FN-090).
        if (objStepAgents is not null)
        {
            await objStepAgents.StopAsync(aRunId, aCt).ConfigureAwait(false);
        }

        var vLock = RunLocks.GetOrAdd(aRunId, static _ => new SemaphoreSlim(1, 1));
        await vLock.WaitAsync(aCt).ConfigureAwait(false);
        try
        {
            var vRun = await GetAsync(aRunId, aCt).ConfigureAwait(false);
            if (vRun is null || vRun.State is ProcessRunState.Stopped or ProcessRunState.Finished)
            {
                return vRun;
            }

            await EndRunAsync(vRun, ProcessRunState.Stopped, aCt).ConfigureAwait(false);
            return await GetAsync(aRunId, aCt).ConfigureAwait(false);
        }
        finally
        {
            vLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProcessRunVerdict>> VerdictsAsync(int aRunId, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRows = await vConnection.QueryAsync<VerdictRow>(new CommandDefinition(
            "SELECT RequirementId, ChecklistFile, Status, Percent, Remark FROM ProcessRunVerdict WHERE RunId = @aRunId ORDER BY ProcessRunVerdictId",
            new { aRunId },
            cancellationToken: aCt)).ConfigureAwait(false);
        var vEvidence = await objEvidence.TestEvidenceAsync(aRunId, aCt).ConfigureAwait(false);
        return vRows.Select(aRow => new ProcessRunVerdict(
            aRow.RequirementId,
            aRow.ChecklistFile,
            aRow.Status,
            (int)aRow.Percent,
            aRow.Remark,
            vEvidence.For(aRow.RequirementId))).ToList();
    }

    private async Task<ProcessRunView?> AdvanceLockedAsync(int aRunId, UnattendedAdvance? aPolicy, CancellationToken aCt)
    {
        var vRun = await GetAsync(aRunId, aCt).ConfigureAwait(false);
        var vDefinition = vRun is null ? null : ProcessCatalogue.Find(vRun.ProcessCode);
        while (vRun is { State: ProcessRunState.Running, CurrentPosition: { } vPosition } && vDefinition is not null)
        {
            if (aPolicy?.ShouldYield is { } vShouldYield && await vShouldYield().ConfigureAwait(false))
            {
                return vRun;
            }

            var vStep = vRun.Steps[vPosition - 1];
            if (vStep.Kind == ProcessStepKind.OwnerStop && aPolicy is not null)
            {
                // Unattended: the run answers the stop itself and says so (REQ-FN-061).
                await RecordStepAsync(aRunId, vPosition, "done", "Answered yes by the end-to-end run", ProcessAnswer.Yes.ToString(), aCt).ConfigureAwait(false);
                if (aPolicy.OnOwnerStopAnswered is { } vTell)
                {
                    await vTell(vPosition, vStep.Wording).ConfigureAwait(false);
                }

                if (vPosition >= vRun.Steps.Count)
                {
                    await EndRunAsync(vRun, ProcessRunState.Finished, aCt).ConfigureAwait(false);
                }
                else
                {
                    await SetRunAsync(aRunId, ProcessRunState.Running, vPosition + 1, aCt).ConfigureAwait(false);
                }

                vRun = await GetAsync(aRunId, aCt).ConfigureAwait(false);
                continue;
            }

            if (vStep.Kind == ProcessStepKind.OwnerStop)
            {
                // Pause here and do nothing further until the owner answers (REQ-FN-053).
                await SetRunAsync(aRunId, ProcessRunState.Waiting, vPosition, aCt).ConfigureAwait(false);
                return await GetAsync(aRunId, aCt).ConfigureAwait(false);
            }

            ProcessStepOutcome vOutcome;
            try
            {
                vOutcome = aPolicy is null
                    ? await objExecutor.ExecuteAsync(aRunId, vRun.ProjectId, vRun.ProcessCode, vStep, aCt).ConfigureAwait(false)
                    : await ExecuteUnattendedAsync(aRunId, vRun, vStep, aPolicy.StallTimeout, aCt).ConfigureAwait(false);
            }
            catch (Exception aEx) when (aPolicy is null && aEx is not OperationCanceledException)
            {
                objLogger.LogError(aEx, "Step {Position} of run {RunId} failed inside Chatur.", vPosition, aRunId);
                vOutcome = new ProcessStepOutcome(false, "This step could not be carried out. The reason is in the log.");
            }

            if (vDefinition.IsVerifying && vPosition == vDefinition.VerdictStep && vOutcome.Passed)
            {
                try
                {
                    vOutcome = new ProcessStepOutcome(true, await objVerdicts.WriteAsync(vRun, vDefinition, aCt).ConfigureAwait(false));
                }
                catch (Exception aEx) when (aEx is not OperationCanceledException)
                {
                    objLogger.LogError(aEx, "Writing the verdicts of run {RunId} failed.", aRunId);
                    await RecordStepAsync(aRunId, vPosition, "failed", "The verdicts could not be written into the checklist. The reason is in the log.", null, aCt).ConfigureAwait(false);
                    await EndRunAsync(vRun, ProcessRunState.Stopped, aCt).ConfigureAwait(false);
                    return await GetAsync(aRunId, aCt).ConfigureAwait(false);
                }
            }

            if (vOutcome.StoppedByOwner)
            {
                await RecordStepAsync(aRunId, vPosition, "failed", vOutcome.Result, null, aCt).ConfigureAwait(false);
                await EndRunAsync(vRun, ProcessRunState.Stopped, aCt).ConfigureAwait(false);
                return await GetAsync(aRunId, aCt).ConfigureAwait(false);
            }

            // A check that fails stops the run there (REQ-FN-088); so does a handed step whose agent could not work it.
            if (!vOutcome.Passed)
            {
                await RecordStepAsync(aRunId, vPosition, "failed", vOutcome.Result, null, aCt).ConfigureAwait(false);
                await EndRunAsync(vRun, ProcessRunState.Stopped, aCt).ConfigureAwait(false);
                return await GetAsync(aRunId, aCt).ConfigureAwait(false);
            }

            await RecordStepAsync(aRunId, vPosition, "done", vOutcome.Result, null, aCt).ConfigureAwait(false);
            if (vPosition >= vRun.Steps.Count)
            {
                await EndRunAsync(vRun, ProcessRunState.Finished, aCt).ConfigureAwait(false);
            }
            else
            {
                await SetRunAsync(aRunId, ProcessRunState.Running, vPosition + 1, aCt).ConfigureAwait(false);
            }

            vRun = await GetAsync(aRunId, aCt).ConfigureAwait(false);
        }

        return vRun;
    }

    /// <summary>
    /// Runs one step under the stall limit. A usage limit passes through; a step past the limit is a
    /// <see cref="StepStalledException"/>; anything else it throws is a <see cref="StepCrashedException"/>. In each case
    /// nothing is recorded, so the run is still on the step.
    /// </summary>
    private async Task<ProcessStepOutcome> ExecuteUnattendedAsync(int aRunId, ProcessRunView aRun, ProcessRunStepView aStep, TimeSpan aStallTimeout, CancellationToken aCt)
    {
        using var vLimit = CancellationTokenSource.CreateLinkedTokenSource(aCt);
        vLimit.CancelAfter(aStallTimeout);
        // Any model call this step makes is made for the run, so a usage limit reaches it as a ModelUsageLimitException (REQ-FN-064).
        using var vForRun = ModelCallScope.ForRun();
        try
        {
            return await objExecutor.ExecuteAsync(aRunId, aRun.ProjectId, aRun.ProcessCode, aStep, vLimit.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!aCt.IsCancellationRequested)
        {
            throw new StepStalledException(aStep.Position, aStallTimeout);
        }
        catch (Exception aEx) when (aEx is not (OperationCanceledException or ModelUsageLimitException))
        {
            objLogger.LogError(aEx, "Step {Position} of run {RunId} crashed.", aStep.Position, aRunId);
            throw new StepCrashedException(aStep.Position, aEx);
        }
    }

    private async Task RecordStepAsync(int aRunId, int aPosition, string aState, string? aResult, string? aAnswer, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO ProcessRunStep (RunId, Position, State, Result, Answer, FinishedUtc)
            VALUES (@aRunId, @aPosition, @aState, @aResult, @aAnswer, @vNow)
            ON CONFLICT (RunId, Position) DO UPDATE
               SET State = excluded.State, Result = excluded.Result, Answer = excluded.Answer, FinishedUtc = excluded.FinishedUtc
            """,
            new { aRunId, aPosition, aState, aResult, aAnswer, vNow = Stamp(objClock.UtcNow) },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    private async Task SetRunAsync(int aRunId, ProcessRunState aState, int aCurrentStep, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            "UPDATE ProcessRun SET State = @State, CurrentStep = @aCurrentStep WHERE RunId = @aRunId",
            new { State = aState.ToString().ToLowerInvariant(), aCurrentStep, aRunId },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <summary>Marks the run over and appends its record to the project's runs file, signed <c>chatur</c> (REQ-FN-054).</summary>
    private async Task EndRunAsync(ProcessRunView aRun, ProcessRunState aState, CancellationToken aCt)
    {
        var vEnded = objClock.UtcNow;
        using (var vConnection = objConnections.OpenConnection())
        {
            await vConnection.ExecuteAsync(new CommandDefinition(
                "UPDATE ProcessRun SET State = @State, EndedUtc = @vEnded WHERE RunId = @RunId",
                new { State = aState.ToString().ToLowerInvariant(), vEnded = Stamp(vEnded), aRun.RunId },
                cancellationToken: aCt)).ConfigureAwait(false);
        }

        var vFields = new Dictionary<string, string?>
        {
            ["kind"] = "run",
            ["cmd"] = aRun.ProcessCode,
            ["mode"] = "process",
            ["started"] = Stamp(aRun.StartedUtc),
            ["ended"] = Stamp(vEnded),
            ["duration_s"] = Math.Max(0, (vEnded - aRun.StartedUtc).TotalSeconds).ToString("0", CultureInfo.InvariantCulture)
        };

        // A failed measurement write is logged by the measurement writer and never stops the work.
        await objMeasurements.AppendAsync(aRun.ProjectId, "runs.jsonl", vFields, aCt).ConfigureAwait(false);
    }

    private static ProcessRunState ParseState(string aState) =>
        Enum.TryParse<ProcessRunState>(aState, true, out var vState) ? vState : ProcessRunState.Running;

    private static string Stamp(DateTime aUtc) => aUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static DateTime ParseStamp(string aStamp) =>
        DateTime.Parse(aStamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private sealed class RunRow
    {
        public int RunId { get; set; }

        public int ProjectId { get; set; }

        public string ProcessCode { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public int CurrentStep { get; set; }

        public string StartedUtc { get; set; } = string.Empty;

        public string? EndedUtc { get; set; }
    }

    private sealed class DoneRow
    {
        public int Position { get; set; }

        public string State { get; set; } = string.Empty;

        public string? Result { get; set; }

        public string? Answer { get; set; }
    }

    private sealed class VerdictRow
    {
        public string RequirementId { get; set; } = string.Empty;

        public string ChecklistFile { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public long Percent { get; set; }

        public string Remark { get; set; } = string.Empty;
    }

    private sealed class WordingRow
    {
        public string StepCode { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;
    }
}
