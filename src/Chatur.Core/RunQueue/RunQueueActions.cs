using System.Globalization;
using Chatur.Core.Actions;
using Chatur.Core.AutoRun;
using Chatur.Core.Board;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Chatur.Core.ProcessRuns;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Chatur.Core.RunQueue;

/// <summary>
/// <see cref="IRunQueueActions"/> over the <c>RunQueueItem</c> rows. A row only says "queued" or
/// "started"; where a started run stands is always read from its end-to-end run
/// (<see cref="IAutoRunActions"/>), so the queue and the run can never disagree. One run goes at a time:
/// the next waiting row is started only when no row is going.
/// </summary>
public sealed class RunQueueActions : IRunQueueActions
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private readonly IDbConnectionFactory objConnections;
    private readonly IAutoRunActions objAutoRuns;
    private readonly OpenRequirementReader objOpen;
    private readonly IClock objClock;
    private readonly ILogger<RunQueueActions> objLogger;

    /// <summary>Creates the action implementation.</summary>
    /// <param name="aConnections">Opens connections to Chatur's database.</param>
    /// <param name="aAutoRuns">Starts, pauses and resumes the end-to-end runs.</param>
    /// <param name="aFolders">Finds a project's folder to read its checklists.</param>
    /// <param name="aClock">The time source.</param>
    /// <param name="aLogger">Where a run that cannot be started is logged.</param>
    public RunQueueActions(
        IDbConnectionFactory aConnections,
        IAutoRunActions aAutoRuns,
        ProjectFolders aFolders,
        IClock aClock,
        ILogger<RunQueueActions> aLogger)
    {
        objConnections = aConnections;
        objAutoRuns = aAutoRuns;
        objOpen = new OpenRequirementReader(aFolders);
        objClock = aClock;
        objLogger = aLogger;
    }

    /// <inheritdoc />
    public async Task<RunQueueView> ListAsync(CancellationToken aCt = default)
    {
        await Gate.WaitAsync(aCt).ConfigureAwait(false);
        try
        {
            await StartNextAsync(aCt).ConfigureAwait(false);
            var vItems = await SnapshotAsync(aCt).ConfigureAwait(false);
            var vUnfinished = vItems.Where(aItem => aItem.State != RunQueueState.Finished);
            var vFinished = vItems.Where(aItem => aItem.State == RunQueueState.Finished).Reverse();
            return new RunQueueView(
                [.. vUnfinished, .. vFinished],
                vItems.Count(aItem => aItem.State == RunQueueState.Running),
                vItems.Count(aItem => aItem.State == RunQueueState.Waiting));
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<RunQueueAdded> QueueAsync(int aProjectId, string aBrief, CancellationToken aCt = default)
    {
        var vBrief = (aBrief ?? string.Empty).Trim();
        if (vBrief.Length < AutoRunBrief.MinLength || vBrief.Length > RunQueueBrief.MaxLength)
        {
            throw new ArgumentException($"The brief needs {AutoRunBrief.MinLength} to {RunQueueBrief.MaxLength} characters.", nameof(aBrief));
        }

        await Gate.WaitAsync(aCt).ConfigureAwait(false);
        try
        {
            using var vConnection = objConnections.OpenConnection();
            var vName = await vConnection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
                "SELECT Name FROM Project WHERE ProjectId = @aProjectId", new { aProjectId = aProjectId }, cancellationToken: aCt)).ConfigureAwait(false)
                ?? throw new ArgumentException("That project is not known.", nameof(aProjectId));
            var vAlready = (await SnapshotAsync(aCt).ConfigureAwait(false))
                .Any(aItem => aItem.ProjectId == aProjectId && aItem.State != RunQueueState.Finished);
            var vId = await vConnection.ExecuteScalarAsync<int>(new CommandDefinition(
                "INSERT INTO RunQueueItem (ProjectId, Brief, Status, QueuedUtc) VALUES (@aProjectId, @vBrief, 'Queued', @vNow); SELECT last_insert_rowid();",
                new { aProjectId, vBrief, vNow = Stamp(objClock.UtcNow) },
                cancellationToken: aCt)).ConfigureAwait(false);
            var vItem = (await SnapshotAsync(aCt).ConfigureAwait(false)).First(aItem => aItem.QueueItemId == vId);
            return new RunQueueAdded(vItem, vAlready);
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<RunQueueItem?> PauseAsync(int aQueueItemId, CancellationToken aCt = default)
    {
        await Gate.WaitAsync(aCt).ConfigureAwait(false);
        try
        {
            var vItem = await FindAsync(aQueueItemId, aCt).ConfigureAwait(false);
            if (vItem is null)
            {
                return null;
            }

            if (vItem.State == RunQueueState.Running && vItem.AutoRunId is int vRunId)
            {
                await objAutoRuns.PauseAsync(vRunId, aCt).ConfigureAwait(false);
            }
            else if (vItem.State == RunQueueState.Waiting && vItem.AutoRunId is not null)
            {
                await SetAsync(aQueueItemId, RunQueueStatus.Started, vItem.AutoRunId, vItem.ResumeFocus, aCt).ConfigureAwait(false);
            }

            return await FindAsync(aQueueItemId, aCt).ConfigureAwait(false);
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<RunQueueItem?> ResumeAsync(int aQueueItemId, CancellationToken aCt = default)
    {
        await Gate.WaitAsync(aCt).ConfigureAwait(false);
        try
        {
            var vItem = await FindAsync(aQueueItemId, aCt).ConfigureAwait(false);
            if (vItem is { State: RunQueueState.Paused, AutoRunId: int vRunId })
            {
                var vBusy = (await SnapshotAsync(aCt).ConfigureAwait(false)).Any(aItem => aItem.State == RunQueueState.Running);
                if (vBusy)
                {
                    await SetAsync(aQueueItemId, RunQueueStatus.Queued, vRunId, vItem.ResumeFocus, aCt).ConfigureAwait(false);
                }
                else
                {
                    await ResumeRunAsync(aQueueItemId, vItem.ProjectId, vRunId, aCt).ConfigureAwait(false);
                }

                return await FindAsync(aQueueItemId, aCt).ConfigureAwait(false);
            }

            return vItem;
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(int aQueueItemId, CancellationToken aCt = default)
    {
        await Gate.WaitAsync(aCt).ConfigureAwait(false);
        try
        {
            var vItem = await FindAsync(aQueueItemId, aCt).ConfigureAwait(false);
            if (vItem is null || !vItem.CanRemove)
            {
                return false;
            }

            using var vConnection = objConnections.OpenConnection();
            await vConnection.ExecuteAsync(new CommandDefinition(
                "UPDATE RunQueueItem SET Status = 'Removed', RemovedUtc = @vNow WHERE RunQueueItemId = @aQueueItemId",
                new { aQueueItemId, vNow = Stamp(objClock.UtcNow) },
                cancellationToken: aCt)).ConfigureAwait(false);
            if (vItem.AutoRunId is int vRunId)
            {
                await StopQuietlyAsync(vRunId, aCt).ConfigureAwait(false);
            }

            return true;
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string?> NextRequirementAsync(int aQueueItemId, CancellationToken aCt = default)
    {
        var vItem = await FindAsync(aQueueItemId, aCt).ConfigureAwait(false);
        return vItem is null ? null : await NextRequirementForProjectAsync(vItem.ProjectId, aCt).ConfigureAwait(false);
    }

    private async Task StartNextAsync(CancellationToken aCt)
    {
        var vItems = await SnapshotAsync(aCt).ConfigureAwait(false);
        if (vItems.Any(aItem => aItem.State == RunQueueState.Running))
        {
            return;
        }

        foreach (var vWaiting in vItems.Where(aItem => aItem.State == RunQueueState.Waiting))
        {
            if (await TryStartAsync(vWaiting, aCt).ConfigureAwait(false))
            {
                return;
            }
        }
    }

    private async Task<bool> TryStartAsync(RunQueueItem aItem, CancellationToken aCt)
    {
        try
        {
            if (aItem.AutoRunId is int vRunId)
            {
                await ResumeRunAsync(aItem.QueueItemId, aItem.ProjectId, vRunId, aCt).ConfigureAwait(false);
                return true;
            }

            var vRun = await objAutoRuns.StartAsync(aItem.ProjectId, aItem.Brief, aCt).ConfigureAwait(false);
            await SetAsync(aItem.QueueItemId, RunQueueStatus.Started, vRun.AutoRunId, null, aCt).ConfigureAwait(false);
            return true;
        }
        catch (InvalidOperationException vBusy)
        {
            objLogger.LogInformation(vBusy, "Queued run {QueueItemId} waits: its project already has a run going", aItem.QueueItemId);
            return false;
        }
        catch (ArgumentException vBad)
        {
            objLogger.LogWarning(vBad, "Queued run {QueueItemId} cannot be started and reads as lost", aItem.QueueItemId);
            await SetAsync(aItem.QueueItemId, RunQueueStatus.Started, null, null, aCt).ConfigureAwait(false);
            return false;
        }
    }

    private async Task ResumeRunAsync(int aQueueItemId, int aProjectId, int aRunId, CancellationToken aCt)
    {
        var vFocus = await NextRequirementForProjectAsync(aProjectId, aCt).ConfigureAwait(false);
        await SetAsync(aQueueItemId, RunQueueStatus.Started, aRunId, vFocus, aCt).ConfigureAwait(false);
        await objAutoRuns.ResumeAsync(aRunId, aCt).ConfigureAwait(false);
    }

    private async Task StopQuietlyAsync(int aRunId, CancellationToken aCt)
    {
        try
        {
            await objAutoRuns.StopAsync(aRunId, aCt).ConfigureAwait(false);
        }
        catch (InvalidOperationException vFailed)
        {
            objLogger.LogWarning(vFailed, "The run {AutoRunId} behind a removed queue row could not be stopped", aRunId);
        }
    }

    private async Task<string?> NextRequirementForProjectAsync(int aProjectId, CancellationToken aCt) =>
        (await objOpen.FocusAsync(aProjectId, aCt).ConfigureAwait(false))?.Id;

    private async Task<RunQueueItem?> FindAsync(int aQueueItemId, CancellationToken aCt) =>
        (await SnapshotAsync(aCt).ConfigureAwait(false)).FirstOrDefault(aItem => aItem.QueueItemId == aQueueItemId);

    private async Task<List<RunQueueItem>> SnapshotAsync(CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRows = (await vConnection.QueryAsync<RunQueueRow>(new CommandDefinition(
            "SELECT q.RunQueueItemId, q.ProjectId, p.Name AS ProjectName, q.Brief, q.Status, q.AutoRunId, q.ResumeFocus " +
            "FROM RunQueueItem q JOIN Project p ON p.ProjectId = q.ProjectId WHERE q.Status <> 'Removed' ORDER BY q.RunQueueItemId",
            cancellationToken: aCt)).ConfigureAwait(false)).ToList();
        var vItems = new List<RunQueueItem>();
        var vPosition = 0;
        foreach (var vRow in vRows)
        {
            var vRun = vRow.AutoRunId is int vId ? await objAutoRuns.GetAsync(vId, aCt).ConfigureAwait(false) : null;
            var vState = StateOf(vRow, vRun);
            vPosition += vState == RunQueueState.Finished ? 0 : 1;
            vItems.Add(ToItem(vRow, vRun, vState, vState == RunQueueState.Finished ? null : vPosition));
        }

        return vItems;
    }

    private static RunQueueState StateOf(RunQueueRow aRow, AutoRunView? aRun)
    {
        if (aRow.Status == RunQueueStatus.Queued)
        {
            return RunQueueState.Waiting;
        }

        return aRun is null ? RunQueueState.Lost : aRun.State switch
        {
            AutoRunState.Running or AutoRunState.WaitingForLimit => RunQueueState.Running,
            AutoRunState.Paused => RunQueueState.Paused,
            _ => RunQueueState.Finished,
        };
    }

    private RunQueueItem ToItem(RunQueueRow aRow, AutoRunView? aRun, RunQueueState aState, int? aPosition)
    {
        var vStarted = aState is RunQueueState.Waiting or RunQueueState.Lost ? null : aRun?.StartedUtc;
        var vElapsed = vStarted is null ? (TimeSpan?)null : (aRun!.EndedUtc ?? objClock.UtcNow) - vStarted.Value;
        return new RunQueueItem(
            aRow.RunQueueItemId,
            aPosition,
            aRow.ProjectId,
            aRow.ProjectName,
            aRow.Brief,
            Shorten(aRow.Brief),
            aState,
            vStarted,
            vElapsed is { } vSpan && vSpan < TimeSpan.Zero ? TimeSpan.Zero : vElapsed,
            aRow.AutoRunId,
            aRow.ResumeFocus,
            aState == RunQueueState.Running,
            aState == RunQueueState.Paused,
            aState is RunQueueState.Waiting or RunQueueState.Paused or RunQueueState.Lost);
    }

    private async Task SetAsync(int aQueueItemId, string aStatus, int? aAutoRunId, string? aFocus, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            "UPDATE RunQueueItem SET Status = @aStatus, AutoRunId = @aAutoRunId, ResumeFocus = @aFocus WHERE RunQueueItemId = @aQueueItemId",
            new { aQueueItemId, aStatus, aAutoRunId, aFocus },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    private static string Shorten(string aBrief)
    {
        var vLine = aBrief.ReplaceLineEndings(" ").Trim();
        return vLine.Length <= RunQueueBrief.ShortLength ? vLine : vLine[..(RunQueueBrief.ShortLength - 1)].TrimEnd() + "…";
    }

    private static string Stamp(DateTime aUtc) => aUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
}
