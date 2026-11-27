using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Chatur.Core.Actions;
using Chatur.Core.Agent;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Chatur.Core.CompareModels;

/// <summary>
/// <see cref="ICompareModelsActions"/> over the <c>Comparison</c> and <c>ComparisonResult</c> tables, TechieRag (through
/// <see cref="IAgentLlmProviderFactory"/>, as the chat builds its models) and the project's own <c>docs/metrics</c>
/// (through <see cref="IMeasurementActions"/>, in TfLens's shape). BRD-291 to BRD-297.
/// </summary>
public sealed class CompareModelsActions : ICompareModelsActions
{
    /// <summary>The <c>cmd</c> every comparison's run record carries.</summary>
    public const string RunCommand = "compare-models";

    /// <summary>The <c>gate</c> a person's own mark carries in <c>gates.jsonl</c>: TfLens has no value for it (see docs/Chatur-TechieFlow-Feedback.md).</summary>
    public const string OwnerMarkGate = "owner-mark";

    /// <summary>The folder under the project's bin folder that holds each comparison's copies.</summary>
    public const string CopiesFolder = "chatur-compare";

    /// <summary>How a model's start is written: to the millisecond, so each model's run record has a <c>started</c> of its own.</summary>
    public const string StartedFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

    private const string Running = "running";
    private const string Done = "done";
    private const string Failed = "failed";

    private readonly IDbConnectionFactory objDb;
    private readonly IAgentLlmProviderFactory objLlmFactory;
    private readonly IMeasurementActions objMeasurements;
    private readonly IClock objClock;
    private readonly ILogger<CompareModelsActions> objLogger;

    /// <summary>One write at a time to the comparison's rows, since every model ends on its own thread.</summary>
    private readonly SemaphoreSlim objWriteLock = new(1, 1);

    /// <summary>Creates the actions.</summary>
    /// <param name="aDb">Chatur's database.</param>
    /// <param name="aLlmFactory">Builds a TechieRag provider for a connected model, as the chat does.</param>
    /// <param name="aMeasurements">Appends to the project's <c>docs/metrics</c> in TfLens's shape.</param>
    /// <param name="aClock">The time.</param>
    /// <param name="aLogger">Where a failed clean-up or measurement is logged.</param>
    public CompareModelsActions(
        IDbConnectionFactory aDb,
        IAgentLlmProviderFactory aLlmFactory,
        IMeasurementActions aMeasurements,
        IClock aClock,
        ILogger<CompareModelsActions> aLogger)
    {
        objDb = aDb;
        objLlmFactory = aLlmFactory;
        objMeasurements = aMeasurements;
        objClock = aClock;
        objLogger = aLogger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CompareModelOption>> ModelsAsync(CancellationToken aCt = default)
    {
        var vRows = await ConnectedModelsAsync(aCt).ConfigureAwait(false);
        var vTaken = new HashSet<string>(StringComparer.Ordinal);
        var vResult = new List<CompareModelOption>(vRows.Count);
        foreach (var vRow in vRows)
        {
            var vSlug = CompareWorkspace.SafeName(vRow.ModelIdentifier);
            if (!vTaken.Add(vSlug))
            {
                vSlug = $"{vSlug}-{vRow.ModelId.ToString(CultureInfo.InvariantCulture)}";
                vTaken.Add(vSlug);
            }

            vResult.Add(new CompareModelOption((int)vRow.ModelId, vRow.ModelIdentifier, vRow.ProviderName, vRow.Connector, vSlug));
        }

        return vResult;
    }

    /// <inheritdoc />
    public string? PickRefusal(int aCount) => aCount switch
    {
        0 => "Pick two to four models to compare.",
        1 => "One model is not a comparison — pick at least two.",
        > ICompareModelsActions.MaxModels => $"{aCount} models picked — four at most; untick {(aCount - ICompareModelsActions.MaxModels == 1 ? "one" : (aCount - ICompareModelsActions.MaxModels).ToString(CultureInfo.InvariantCulture))}.",
        _ => null
    };

    /// <inheritdoc />
    public async Task<Comparison> RunAsync(CompareRequest aRequest, Func<Comparison, Task>? aOnChange = null, CancellationToken aCt = default)
    {
        var vModelIds = aRequest.ModelIds.Distinct().ToList();
        if (PickRefusal(vModelIds.Count) is { } vRefusal)
        {
            throw new ArgumentException(vRefusal, nameof(aRequest));
        }

        if (string.IsNullOrWhiteSpace(aRequest.Task))
        {
            throw new ArgumentException("Type the task every model is given.", nameof(aRequest));
        }

        var vConnected = (await ConnectedModelsAsync(aCt).ConfigureAwait(false)).ToDictionary(aRow => (int)aRow.ModelId);
        var vModels = new List<ConnectedModel>(vModelIds.Count);
        foreach (var vId in vModelIds)
        {
            if (!vConnected.TryGetValue(vId, out var vRow))
            {
                throw new ArgumentException("A model picked is no longer connected; pick again.", nameof(aRequest));
            }

            vModels.Add(new ConnectedModel((int)vRow.ModelId, (int)vRow.ProviderId, vRow.ProviderName, vRow.Connector, vRow.BaseUrl, vRow.SecretName, vRow.ModelIdentifier));
        }

        var vProjectPath = await ProjectPathAsync(aRequest.ProjectId, aCt).ConfigureAwait(false)
            ?? throw new ArgumentException("The project is not known; open it again.", nameof(aRequest));
        var vProjectRoot = Path.GetFullPath(CompareWorkspace.ProjectRoot(vProjectPath));
        if (!Directory.Exists(vProjectRoot))
        {
            throw new ArgumentException($"The project's folder \"{vProjectRoot}\" is not there.", nameof(aRequest));
        }

        // The rows first, so the screen shows a card per model reading "running" (UI Design, Compare models, loading).
        var vComparisonId = await InsertComparisonAsync(aRequest, vModels, aCt).ConfigureAwait(false);
        await NotifyAsync(vComparisonId, aOnChange).ConfigureAwait(false);

        var vBefore = aRequest.Kind == CompareKind.Question ? CompareWorkspace.Snapshot(vProjectRoot, true) : null;
        var vCopies = Path.Combine(vProjectRoot, "bin", CopiesFolder, vComparisonId.ToString(CultureInfo.InvariantCulture));
        bool? vCopiesRemoved = null;
        try
        {
            var vResults = await ResultRowsAsync(vComparisonId, CancellationToken.None).ConfigureAwait(false);
            var vRuns = vResults.Select((aRow, aIndex) =>
                RunOneAsync(aRequest, vModels[aIndex], aRow, vProjectRoot, vCopies, aOnChange, aCt)).ToList();
            await Task.WhenAll(vRuns).ConfigureAwait(false);
        }
        finally
        {
            if (aRequest.Kind == CompareKind.Change)
            {
                vCopiesRemoved = RemoveCopies(vCopies);
            }
        }

        bool? vUnchanged = vBefore is null ? null : CompareWorkspace.Same(vBefore, CompareWorkspace.Snapshot(vProjectRoot, true));
        await FinishComparisonAsync(vComparisonId, vUnchanged, vCopiesRemoved).ConfigureAwait(false);
        var vFinished = await OpenAsync(vComparisonId, CancellationToken.None).ConfigureAwait(false);
        if (aOnChange is not null)
        {
            await aOnChange(vFinished!).ConfigureAwait(false);
        }

        return vFinished!;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ComparisonSummary>> HistoryAsync(int aProjectId, CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        var vComparisons = (await vConnection.QueryAsync<ComparisonRow>(new CommandDefinition(
            "SELECT * FROM Comparison WHERE ProjectId = @aProjectId ORDER BY ComparisonId DESC;",
            new { aProjectId },
            cancellationToken: aCt)).ConfigureAwait(false)).ToList();
        var vResults = (await vConnection.QueryAsync<ResultRow>(new CommandDefinition(
            "SELECT r.* FROM ComparisonResult r JOIN Comparison c ON c.ComparisonId = r.ComparisonId WHERE c.ProjectId = @aProjectId ORDER BY r.ComparisonId, r.Position;",
            new { aProjectId },
            cancellationToken: aCt)).ConfigureAwait(false)).ToLookup(aRow => aRow.ComparisonId);

        return vComparisons.Select(aRow =>
        {
            var vRows = vResults[aRow.ComparisonId].ToList();
            return new ComparisonSummary(
                (int)aRow.ComparisonId,
                ParseUtc(aRow.StartedUtc),
                KindOf(aRow.Kind),
                aRow.Task,
                vRows.Select(aResult => aResult.ModelIdentifier).ToList(),
                vRows.Count(aResult => aResult.Mark == "right"),
                vRows.Count(aResult => aResult.Mark == "wrong"),
                vRows.Count(aResult => aResult.Mark is null));
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<Comparison?> OpenAsync(int aComparisonId, CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        var vRow = await vConnection.QuerySingleOrDefaultAsync<ComparisonRow>(new CommandDefinition(
            "SELECT * FROM Comparison WHERE ComparisonId = @aComparisonId;",
            new { aComparisonId },
            cancellationToken: aCt)).ConfigureAwait(false);
        if (vRow is null)
        {
            return null;
        }

        var vResults = await vConnection.QueryAsync<ResultRow>(new CommandDefinition(
            "SELECT * FROM ComparisonResult WHERE ComparisonId = @aComparisonId ORDER BY Position;",
            new { aComparisonId },
            cancellationToken: aCt)).ConfigureAwait(false);

        return new Comparison(
            (int)vRow.ComparisonId,
            (int)vRow.ProjectId,
            KindOf(vRow.Kind),
            vRow.Task,
            vRow.State,
            ParseUtc(vRow.StartedUtc),
            vRow.EndedUtc is null ? null : ParseUtc(vRow.EndedUtc),
            vRow.ProjectUnchanged is null ? null : vRow.ProjectUnchanged != 0,
            vRow.CopiesRemoved is null ? null : vRow.CopiesRemoved != 0,
            vResults.Select(ToResult).ToList());
    }

    /// <inheritdoc />
    public async Task<Comparison> MarkAsync(int aResultId, bool aRight, CancellationToken aCt = default)
    {
        MarkTarget? vTarget;
        using (var vConnection = objDb.OpenConnection())
        {
            vTarget = await vConnection.QuerySingleOrDefaultAsync<MarkTarget>(new CommandDefinition(
                "SELECT r.ComparisonId, r.State, r.StartedUtc, c.ProjectId FROM ComparisonResult r JOIN Comparison c ON c.ComparisonId = r.ComparisonId WHERE r.ComparisonResultId = @aResultId;",
                new { aResultId },
                cancellationToken: aCt)).ConfigureAwait(false);
        }

        if (vTarget is null)
        {
            throw new InvalidOperationException("That result is not known.");
        }

        if (vTarget.State == Running)
        {
            throw new InvalidOperationException("The model is still running; mark it when it has finished.");
        }

        var vMark = aRight ? "right" : "wrong";
        await objWriteLock.WaitAsync(aCt).ConfigureAwait(false);
        try
        {
            using var vConnection = objDb.OpenConnection();
            await vConnection.ExecuteAsync(new CommandDefinition(
                "UPDATE ComparisonResult SET Mark = @vMark, MarkedUtc = @vNow WHERE ComparisonResultId = @aResultId;",
                new { vMark, vNow = objClock.UtcNow.ToString("O", CultureInfo.InvariantCulture), aResultId },
                cancellationToken: aCt)).ConfigureAwait(false);
        }
        finally
        {
            objWriteLock.Release();
        }

        // A mark is a verdict on that model's run: a new gates record each time, the latest one standing (REQ-FN-330).
        await AppendSafelyAsync((int)vTarget.ProjectId, "gates.jsonl", new Dictionary<string, string?>
        {
            ["kind"] = "gate",
            ["run_id"] = vTarget.StartedUtc,
            ["verdict"] = aRight ? "Verified" : "FAIL",
            ["gate"] = aRight ? null : OwnerMarkGate,
            ["gates_run"] = OwnerMarkGate
        }).ConfigureAwait(false);

        return (await OpenAsync((int)vTarget.ComparisonId, CancellationToken.None).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Runs one model and keeps what it did, whatever happens: an error is its result, never the comparison's
    /// (UI Design, Compare models, error). For a task that changes files the model's copy is made first, and what it
    /// changed is listed against the project before the copies are removed.
    /// </summary>
    private async Task RunOneAsync(
        CompareRequest aRequest,
        ConnectedModel aModel,
        ResultRow aRow,
        string aProjectRoot,
        string aCopies,
        Func<Comparison, Task>? aOnChange,
        CancellationToken aCt)
    {
        var vWatch = Stopwatch.StartNew();
        CompareModelOutcome? vOutcome = null;
        string? vError = null;
        IReadOnlyList<CompareChangedFile> vFiles = [];
        try
        {
            var vRoot = aProjectRoot;
            if (aRequest.Kind == CompareKind.Change)
            {
                vRoot = Path.Combine(aCopies, CompareWorkspace.SafeName(aModel.ModelIdentifier) + "-" + aRow.Position.ToString(CultureInfo.InvariantCulture));
                CompareWorkspace.Copy(aProjectRoot, vRoot);
            }

            var vProvider = await objLlmFactory.CreateAsync(aModel, aCt).ConfigureAwait(false);
            vOutcome = await CompareModelRunner.RunAsync(
                vProvider,
                aModel.ModelIdentifier,
                aRequest.Kind,
                new CompareWorkspace(vRoot),
                aRequest.Task,
                $"chatur-compare-{aRow.ComparisonId.ToString(CultureInfo.InvariantCulture)}-{aRow.Position.ToString(CultureInfo.InvariantCulture)}",
                aCt).ConfigureAwait(false);

            if (aRequest.Kind == CompareKind.Change)
            {
                vFiles = CompareWorkspace.Changes(aProjectRoot, vRoot);
            }
        }
        catch (OperationCanceledException) when (aCt.IsCancellationRequested)
        {
            vError = "Stopped before it finished.";
        }
        catch (Exception vException)
        {
            vError = $"{aModel.ProviderName} did not answer: {vException.Message}";
        }

        vWatch.Stop();
        var vState = vError is null ? Done : Failed;
        var vEnded = ParseUtc(aRow.StartedUtc).AddMilliseconds(vWatch.ElapsedMilliseconds);

        await objWriteLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            using var vConnection = objDb.OpenConnection();
            await vConnection.ExecuteAsync(new CommandDefinition(
                """
                UPDATE ComparisonResult
                SET State = @vState, EndedUtc = @vEndedText, DurationMs = @vDurationMs, TokensIn = @vTokensIn, TokensOut = @vTokensOut,
                    CostUsd = @vCost, Answer = @vAnswer, Error = @vError, ChangedFiles = @vFilesJson
                WHERE ComparisonResultId = @vId;
                """,
                new
                {
                    vState,
                    vEndedText = vEnded.ToString(StartedFormat, CultureInfo.InvariantCulture),
                    vDurationMs = vWatch.ElapsedMilliseconds,
                    vTokensIn = vOutcome?.TokensIn,
                    vTokensOut = vOutcome?.TokensOut,
                    vCost = vOutcome?.CostUsd is { } vPrice ? (double?)(double)vPrice : null,
                    vAnswer = vOutcome?.Answer,
                    vError,
                    vFilesJson = aRequest.Kind == CompareKind.Change ? JsonSerializer.Serialize(vFiles) : null,
                    vId = aRow.ComparisonResultId
                })).ConfigureAwait(false);
        }
        finally
        {
            objWriteLock.Release();
        }

        // One run record per model, in TfLens's shape (REQ-FN-330, REQ-NFR-021).
        await AppendSafelyAsync(aRequest.ProjectId, "runs.jsonl", new Dictionary<string, string?>
        {
            ["kind"] = "run",
            ["cmd"] = RunCommand,
            ["mode"] = aRequest.Kind == CompareKind.Question ? "question" : "change",
            ["started"] = aRow.StartedUtc,
            ["ended"] = vEnded.ToString(StartedFormat, CultureInfo.InvariantCulture),
            ["duration_s"] = Math.Round(vWatch.Elapsed.TotalSeconds).ToString("0", CultureInfo.InvariantCulture),
            ["model"] = aModel.ModelIdentifier,
            ["tokens_in"] = vOutcome?.TokensIn?.ToString(CultureInfo.InvariantCulture),
            ["tokens_out"] = vOutcome?.TokensOut?.ToString(CultureInfo.InvariantCulture),
            ["cost_usd"] = vOutcome?.CostUsd?.ToString(CultureInfo.InvariantCulture),
            ["files_written"] = aRequest.Kind == CompareKind.Change ? vFiles.Count.ToString(CultureInfo.InvariantCulture) : null
        }).ConfigureAwait(false);

        await NotifyAsync(aRow.ComparisonId, aOnChange).ConfigureAwait(false);
    }

    /// <summary>Removes a comparison's copies, and <c>bin/chatur-compare</c> itself when nothing else is left in it.</summary>
    /// <returns>Whether the comparison's copies are gone.</returns>
    private bool RemoveCopies(string aCopies)
    {
        for (var vAttempt = 0; vAttempt < 5 && Directory.Exists(aCopies); vAttempt++)
        {
            try
            {
                ClearReadOnly(aCopies);
                Directory.Delete(aCopies, true);
            }
            catch (Exception vException) when (vException is IOException or UnauthorizedAccessException)
            {
                objLogger.LogWarning(vException, "Could not remove the comparison copies at {Folder}; trying again.", aCopies);
                Thread.Sleep(200 * (vAttempt + 1));
            }
        }

        var vParent = Path.GetDirectoryName(aCopies);
        try
        {
            if (vParent is not null && Directory.Exists(vParent) && !Directory.EnumerateFileSystemEntries(vParent).Any())
            {
                Directory.Delete(vParent);
            }
        }
        catch (Exception vException) when (vException is IOException or UnauthorizedAccessException)
        {
            // Another comparison is making its copies there at this moment; it removes the folder itself.
        }

        return !Directory.Exists(aCopies);
    }

    private static void ClearReadOnly(string aFolder)
    {
        foreach (var vFile in Directory.EnumerateFiles(aFolder, "*", SearchOption.AllDirectories))
        {
            var vAttributes = File.GetAttributes(vFile);
            if ((vAttributes & FileAttributes.ReadOnly) != 0)
            {
                File.SetAttributes(vFile, vAttributes & ~FileAttributes.ReadOnly);
            }
        }
    }

    /// <summary>A measurement never stops a comparison (REQ-UI-035); <see cref="IMeasurementActions.AppendAsync"/> records a refusal itself.</summary>
    private async Task AppendSafelyAsync(int aProjectId, string aStream, IReadOnlyDictionary<string, string?> aFields)
    {
        try
        {
            await objMeasurements.AppendAsync(aProjectId, aStream, aFields, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception vException)
        {
            objLogger.LogWarning(vException, "A compare-models record for {Stream} was not written.", aStream);
        }
    }

    private async Task NotifyAsync(long aComparisonId, Func<Comparison, Task>? aOnChange)
    {
        if (aOnChange is null)
        {
            return;
        }

        try
        {
            if (await OpenAsync((int)aComparisonId, CancellationToken.None).ConfigureAwait(false) is { } vNow)
            {
                await aOnChange(vNow).ConfigureAwait(false);
            }
        }
        catch (Exception vException)
        {
            // The screen showing progress is never allowed to stop the models.
            objLogger.LogWarning(vException, "Showing comparison {ComparisonId}'s progress failed.", aComparisonId);
        }
    }

    private async Task<int> InsertComparisonAsync(CompareRequest aRequest, List<ConnectedModel> aModels, CancellationToken aCt)
    {
        var vNow = objClock.UtcNow;
        await objWriteLock.WaitAsync(aCt).ConfigureAwait(false);
        try
        {
            using var vConnection = objDb.OpenConnection();
            using var vTransaction = vConnection.BeginTransaction();
            var vId = await vConnection.ExecuteScalarAsync<long>(new CommandDefinition(
                """
                INSERT INTO Comparison (ProjectId, Kind, Task, State, StartedUtc)
                VALUES (@ProjectId, @vKind, @Task, 'running', @vStarted);
                SELECT last_insert_rowid();
                """,
                new { aRequest.ProjectId, vKind = KindText(aRequest.Kind), Task = aRequest.Task.Trim(), vStarted = vNow.ToString(StartedFormat, CultureInfo.InvariantCulture) },
                vTransaction,
                cancellationToken: aCt)).ConfigureAwait(false);

            // Each model's start is its run's id in docs/metrics, so no two may share a millisecond (REQ-FN-330).
            var vLast = await LatestStartAsync(vConnection, vTransaction, aCt).ConfigureAwait(false);
            for (var vIndex = 0; vIndex < aModels.Count; vIndex++)
            {
                var vStart = Truncate(objClock.UtcNow);
                if (vLast is not null && vStart <= vLast.Value)
                {
                    vStart = vLast.Value.AddMilliseconds(1);
                }

                vLast = vStart;
                var vModel = aModels[vIndex];
                await vConnection.ExecuteAsync(new CommandDefinition(
                    """
                    INSERT INTO ComparisonResult (ComparisonId, Position, ModelId, ModelIdentifier, ProviderName, Connector, State, StartedUtc)
                    VALUES (@vId, @vPosition, @ModelId, @ModelIdentifier, @ProviderName, @Connector, 'running', @vStarted);
                    """,
                    new
                    {
                        vId,
                        vPosition = vIndex + 1,
                        vModel.ModelId,
                        vModel.ModelIdentifier,
                        vModel.ProviderName,
                        vModel.Connector,
                        vStarted = vStart.ToString(StartedFormat, CultureInfo.InvariantCulture)
                    },
                    vTransaction,
                    cancellationToken: aCt)).ConfigureAwait(false);
            }

            vTransaction.Commit();
            return (int)vId;
        }
        finally
        {
            objWriteLock.Release();
        }
    }

    private static async Task<DateTime?> LatestStartAsync(System.Data.IDbConnection aConnection, System.Data.IDbTransaction aTransaction, CancellationToken aCt)
    {
        var vText = await aConnection.ExecuteScalarAsync<string?>(new CommandDefinition(
            "SELECT MAX(StartedUtc) FROM ComparisonResult;", transaction: aTransaction, cancellationToken: aCt)).ConfigureAwait(false);
        return vText is null ? null : ParseUtc(vText);
    }

    private async Task FinishComparisonAsync(int aComparisonId, bool? aUnchanged, bool? aCopiesRemoved)
    {
        await objWriteLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            using var vConnection = objDb.OpenConnection();
            await vConnection.ExecuteAsync(
                """
                UPDATE Comparison
                SET State = CASE WHEN EXISTS (SELECT 1 FROM ComparisonResult WHERE ComparisonId = @aComparisonId AND State = 'done') THEN 'done' ELSE 'failed' END,
                    EndedUtc = @vEnded, ProjectUnchanged = @vUnchanged, CopiesRemoved = @vRemoved
                WHERE ComparisonId = @aComparisonId;
                """,
                new
                {
                    aComparisonId,
                    vEnded = objClock.UtcNow.ToString(StartedFormat, CultureInfo.InvariantCulture),
                    vUnchanged = aUnchanged is null ? (int?)null : aUnchanged.Value ? 1 : 0,
                    vRemoved = aCopiesRemoved is null ? (int?)null : aCopiesRemoved.Value ? 1 : 0
                }).ConfigureAwait(false);
        }
        finally
        {
            objWriteLock.Release();
        }
    }

    private async Task<List<ResultRow>> ResultRowsAsync(int aComparisonId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        return (await vConnection.QueryAsync<ResultRow>(new CommandDefinition(
            "SELECT * FROM ComparisonResult WHERE ComparisonId = @aComparisonId ORDER BY Position;",
            new { aComparisonId },
            cancellationToken: aCt)).ConfigureAwait(false)).ToList();
    }

    private async Task<List<ModelRow>> ConnectedModelsAsync(CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        return (await vConnection.QueryAsync<ModelRow>(new CommandDefinition(
            """
            SELECT m.ModelId AS ModelId, m.ProviderId AS ProviderId, p.Name AS ProviderName, p.Connector AS Connector,
                   p.BaseUrl AS BaseUrl, p.SecretName AS SecretName, m.Identifier AS ModelIdentifier
            FROM Model m
            JOIN Provider p ON p.ProviderId = m.ProviderId
            WHERE p.State = 'Connected'
            ORDER BY p.Name, m.Tier, m.ModelId;
            """,
            cancellationToken: aCt)).ConfigureAwait(false)).ToList();
    }

    private async Task<string?> ProjectPathAsync(int aProjectId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        return await vConnection.QuerySingleOrDefaultAsync<string?>(new CommandDefinition(
            "SELECT Path FROM Project WHERE ProjectId = @aProjectId;",
            new { aProjectId },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    private static CompareResult ToResult(ResultRow aRow) => new(
        (int)aRow.ComparisonResultId,
        (int)aRow.Position,
        aRow.ModelId is null ? null : (int)aRow.ModelId.Value,
        aRow.ModelIdentifier,
        aRow.ProviderName,
        aRow.Connector,
        aRow.State,
        ParseUtc(aRow.StartedUtc),
        aRow.DurationMs,
        aRow.TokensIn,
        aRow.TokensOut,
        aRow.CostUsd is null ? null : (decimal)aRow.CostUsd.Value,
        aRow.Answer,
        aRow.Error,
        string.IsNullOrEmpty(aRow.ChangedFiles) ? [] : JsonSerializer.Deserialize<List<CompareChangedFile>>(aRow.ChangedFiles) ?? [],
        aRow.Mark);

    private static string KindText(CompareKind aKind) => aKind == CompareKind.Question ? "question" : "change";

    private static CompareKind KindOf(string aText) => aText == "change" ? CompareKind.Change : CompareKind.Question;

    private static DateTime Truncate(DateTime aUtc) =>
        new(aUtc.Ticks - (aUtc.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);

    private static DateTime ParseUtc(string aText) =>
        DateTime.Parse(aText, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

    private sealed record ModelRow(long ModelId, long ProviderId, string ProviderName, string Connector, string BaseUrl, string? SecretName, string ModelIdentifier);

    private sealed class ComparisonRow
    {
        public long ComparisonId { get; set; }

        public long ProjectId { get; set; }

        public string Kind { get; set; } = string.Empty;

        public string Task { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string StartedUtc { get; set; } = string.Empty;

        public string? EndedUtc { get; set; }

        public long? ProjectUnchanged { get; set; }

        public long? CopiesRemoved { get; set; }
    }

    private sealed class ResultRow
    {
        public long ComparisonResultId { get; set; }

        public long ComparisonId { get; set; }

        public long Position { get; set; }

        public long? ModelId { get; set; }

        public string ModelIdentifier { get; set; } = string.Empty;

        public string ProviderName { get; set; } = string.Empty;

        public string Connector { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string StartedUtc { get; set; } = string.Empty;

        public string? EndedUtc { get; set; }

        public long? DurationMs { get; set; }

        public long? TokensIn { get; set; }

        public long? TokensOut { get; set; }

        public double? CostUsd { get; set; }

        public string? Answer { get; set; }

        public string? Error { get; set; }

        public string? ChangedFiles { get; set; }

        public string? Mark { get; set; }

        public string? MarkedUtc { get; set; }
    }

    private sealed class MarkTarget
    {
        public long ComparisonId { get; set; }

        public string State { get; set; } = string.Empty;

        public string StartedUtc { get; set; } = string.Empty;

        public long ProjectId { get; set; }
    }
}
