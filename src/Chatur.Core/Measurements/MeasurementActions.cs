using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Chatur.Core.Measurements;

/// <summary>
/// <see cref="IMeasurementActions"/> over the project's <c>docs/metrics</c> files and the
/// <c>RefusedWrite</c> table. <see cref="StreamsAsync"/> is built (cluster L); <see cref="AppendAsync"/>
/// is cluster K's — see its own doc comment.
/// </summary>
public sealed class MeasurementActions : IMeasurementActions
{
    private static readonly string[] StreamNames =
    [
        "runs.jsonl", "gates.jsonl", "misses.jsonl", EstimatesStream, "sessions.jsonl", "commits.jsonl"
    ];

    /// <summary>The Estimator's own stream (REQ-FN-114): listed for its records, never written through <see cref="AppendAsync"/>.</summary>
    private const string EstimatesStream = "estimates.jsonl";

    /// <summary>One append lock per stream file, so two sessions writing the same project's stream at
    /// once cannot interleave their lines (Architecture §5 "Measurements" — appended, never edited).</summary>
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> AppendLocks = new();

    private readonly IDbConnectionFactory objConnections;
    private readonly ILogger<MeasurementActions> objLogger;

    /// <summary>
    /// Creates the action implementation.
    /// </summary>
    /// <param name="aConnections">Opens connections to Chatur's database.</param>
    /// <param name="aLogger">Where a refused or failed write is logged (Architecture §5 "Errors").</param>
    public MeasurementActions(IDbConnectionFactory aConnections, ILogger<MeasurementActions> aLogger)
    {
        objConnections = aConnections;
        objLogger = aLogger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MeasurementStream>> StreamsAsync(int aProjectId, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vProjectPath = await vConnection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(
                "SELECT Path FROM Project WHERE ProjectId = @ProjectId",
                new { ProjectId = aProjectId },
                cancellationToken: aCt)).ConfigureAwait(false);

        var vResult = new List<MeasurementStream>(StreamNames.Length);
        foreach (var vStreamName in StreamNames)
        {
            var vRecordCount = 0;
            var vFieldsEmpty = 0;
            DateTime? vNewest = null;

            if (vProjectPath is not null)
            {
                var vFilePath = Path.Combine(MetricsFolderOf(vProjectPath), vStreamName);
                if (File.Exists(vFilePath))
                {
                    var vLines = await File.ReadAllLinesAsync(vFilePath, aCt).ConfigureAwait(false);
                    foreach (var vLine in vLines)
                    {
                        if (string.IsNullOrWhiteSpace(vLine))
                        {
                            continue;
                        }

                        vRecordCount++;
                        // An estimate's null figure means "no precedent", not a field left empty, so that stream counts none.
                        vFieldsEmpty += vStreamName == EstimatesStream ? 0 : CountEmptyFields(vLine);
                        var vAt = TryReadAt(vLine);
                        if (vAt is { } vParsed && (vNewest is null || vParsed > vNewest))
                        {
                            vNewest = vParsed;
                        }
                    }
                }
            }

            var vLastFailedUtc = await vConnection.QuerySingleOrDefaultAsync<string>(
                new CommandDefinition(
                    @"SELECT CreatedUtc FROM RefusedWrite
                      WHERE ProjectId = @ProjectId AND StreamName = @StreamName
                      ORDER BY RefusedWriteId DESC LIMIT 1",
                    new { ProjectId = aProjectId, StreamName = vStreamName },
                    cancellationToken: aCt)).ConfigureAwait(false);

            var vLastWriteFailed = vLastFailedUtc is not null
                && (vNewest is null
                    || DateTime.Parse(vLastFailedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) > vNewest);

            var vRefused = await vConnection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    "SELECT COUNT(*) FROM RefusedWrite WHERE ProjectId = @ProjectId AND StreamName = @StreamName",
                    new { ProjectId = aProjectId, StreamName = vStreamName },
                    cancellationToken: aCt)).ConfigureAwait(false);

            vResult.Add(new MeasurementStream(vStreamName, vRecordCount, vNewest, vLastWriteFailed, vFieldsEmpty, vRefused));
        }

        return vResult;
    }

    /// <inheritdoc />
    public async Task<MeasurementFolder?> FolderAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vProject = await ReadProjectAsync(aProjectId, aCt).ConfigureAwait(false);
        if (vProject is null)
        {
            return null;
        }

        var vFolder = MetricsFolderOf(vProject.Value.Path);
        var vRunsPath = Path.Combine(vFolder, "runs.jsonl");
        string? vNewest = null;
        if (File.Exists(vRunsPath))
        {
            var vLines = await File.ReadAllLinesAsync(vRunsPath, aCt).ConfigureAwait(false);
            var vLast = vLines.LastOrDefault(aLine => !string.IsNullOrWhiteSpace(aLine));
            vNewest = vLast is null ? null : Indent(vLast);
        }

        return new MeasurementFolder(vFolder, vNewest);
    }

    private static string Indent(string aJsonLine)
    {
        try
        {
            using var vDoc = JsonDocument.Parse(aJsonLine);
            return JsonSerializer.Serialize(vDoc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return aJsonLine;
        }
    }

    private static int CountEmptyFields(string aJsonLine)
    {
        try
        {
            using var vDoc = JsonDocument.Parse(aJsonLine);
            if (vDoc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return 0;
            }

            return vDoc.RootElement.EnumerateObject().Count(aProperty =>
                aProperty.Value.ValueKind == JsonValueKind.Null
                || (aProperty.Value.ValueKind == JsonValueKind.String && aProperty.Value.GetString() is { Length: 0 }));
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    private static DateTime? TryReadAt(string aJsonLine)
    {
        try
        {
            using var vDoc = JsonDocument.Parse(aJsonLine);
            // A record is dated with TechieFlow's "ts"; Chatur's records before 25 Nov 2026 used "at", and still read.
            if ((vDoc.RootElement.TryGetProperty("ts", out var vAt) || vDoc.RootElement.TryGetProperty("at", out vAt))
                && vAt.ValueKind == JsonValueKind.String
                && DateTime.TryParse(vAt.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var vParsed))
            {
                return vParsed;
            }
        }
        catch (JsonException)
        {
            // A malformed line still counts toward the record total; it just cannot date it.
        }

        return null;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Every record is signed with <c>harness: "chatur"</c> (REQ-FN-041) and dated with <c>ts</c> — the
    /// same field <see cref="TryReadAt"/> above reads back for <see cref="StreamsAsync"/>'s "newest
    /// record" column. A field this stream does not know, or a value outside its closed list
    /// (<c>docs/Chatur-Metrics-Schema.md</c>), refuses the whole write rather than writing part of a
    /// record (REQ-FN-043); anything the caller left out is written empty, never guessed
    /// (REQ-FN-042). Neither a refusal nor a disk failure throws — both are logged and recorded in
    /// <c>RefusedWrite</c>, and the caller's session carries on (REQ-UI-035).
    /// </remarks>
    public Task AppendAsync(int aProjectId, string aStreamName, IReadOnlyDictionary<string, string?> aFields, CancellationToken aCt = default) =>
        AppendCoreAsync(aProjectId, aStreamName, aFields, aCt);

    /// <summary>
    /// The one writer. A record is a fact about something that happened (a model call, a build, a run), so the folder is made
    /// for it when it is not there yet (owner's test of 25 Nov 2026: a real chat in a new folder left nothing in
    /// <c>docs/metrics</c>, because the 23 Nov rule wrote a chat turn only where the folder already was). A folder
    /// that is only opened, and a chat that never reached a model, write nothing, since nothing calls this. Only the stream written to gets a
    /// file, and no empty file is ever made.
    /// </summary>
    private async Task AppendCoreAsync(int aProjectId, string aStreamName, IReadOnlyDictionary<string, string?> aFields, CancellationToken aCt)
    {
        var vStreamFileName = NormalizeStreamName(aStreamName);

        var vProjectRow = await ReadProjectAsync(aProjectId, aCt).ConfigureAwait(false);
        if (vProjectRow is null)
        {
            // No Project row to attribute a RefusedWrite to, and nowhere on disk to write.
            objLogger.LogWarning("Measurement write refused: project {ProjectId} does not exist.", aProjectId);
            return;
        }

        var vMetricsFolder = MetricsFolderOf(vProjectRow.Value.Path);
        if (!MeasurementSchema.TryValidate(vStreamFileName, aFields, out var vRefusalReason))
        {
            await RefuseAsync(aProjectId, vStreamFileName, vRefusalReason!, aCt).ConfigureAwait(false);
            return;
        }

        var vLine = BuildLine(vStreamFileName, vProjectRow.Value.Name, aFields);
        var vFilePath = Path.Combine(vMetricsFolder, vStreamFileName);
        var vLock = AppendLocks.GetOrAdd(vFilePath, static _ => new SemaphoreSlim(1, 1));

        await vLock.WaitAsync(aCt).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(vMetricsFolder);
            await File.AppendAllTextAsync(vFilePath, vLine + Environment.NewLine, aCt).ConfigureAwait(false);
        }
        catch (Exception aEx)
        {
            objLogger.LogError(aEx, "Measurement write to {Stream} failed for project {ProjectId}.", vStreamFileName, aProjectId);
            await RefuseAsync(aProjectId, vStreamFileName, $"Write failed: {aEx.Message}", aCt).ConfigureAwait(false);
        }
        finally
        {
            vLock.Release();
        }
    }

    private async Task<(string Name, string Path)?> ReadProjectAsync(int aProjectId, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRow = await vConnection.QuerySingleOrDefaultAsync<ProjectRow>(new CommandDefinition(
            "SELECT Name, Path FROM Project WHERE ProjectId = @aProjectId",
            new { aProjectId },
            cancellationToken: aCt)).ConfigureAwait(false);
        return vRow is null ? null : (vRow.Name, vRow.Path);
    }

    private async Task RefuseAsync(int aProjectId, string aStreamFileName, string aReason, CancellationToken aCt)
    {
        objLogger.LogWarning("Measurement write refused for {Stream}: {Reason}", aStreamFileName, aReason);

        using var vConnection = objConnections.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO RefusedWrite (ProjectId, StreamName, Reason, CreatedUtc)
            VALUES (@aProjectId, @aStreamFileName, @aReason, @vNowUtc)
            """,
            new { aProjectId, aStreamFileName, aReason, vNowUtc = DateTime.UtcNow.ToString("O") },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <summary>
    /// One record in TechieFlow's own shape (SCHEMA.md §1): <c>v</c> a number, <c>ts</c> to the second with a <c>Z</c>,
    /// <c>kind</c>, <c>app</c>, <c>project_type</c>, and <c>harness</c> naming Chatur as the tool (REQ-FN-041), then the
    /// stream's fields typed as the schema types them. A figure not measured is <c>null</c>, never a guess (REQ-FN-042).
    /// </summary>
    private static string BuildLine(string aStreamFileName, string aAppName, IReadOnlyDictionary<string, string?> aFields)
    {
        var vRecord = new Dictionary<string, object?>
        {
            ["v"] = 1,
            ["ts"] = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)
        };

        var vKnownKinds = MeasurementSchema.KindsByStream[aStreamFileName];
        var vRequestedKind = aFields.TryGetValue("kind", out var vKindValue) ? vKindValue : null;
        vRecord["kind"] = string.IsNullOrEmpty(vRequestedKind)
            ? (vKnownKinds.Count == 1 ? vKnownKinds.First() : string.Empty)
            : vRequestedKind;
        vRecord["app"] = aAppName;
        vRecord["project_type"] = "app";
        vRecord["project_type_inferred"] = true;
        vRecord["harness"] = "chatur";

        foreach (var vField in MeasurementSchema.KnownFieldsByStream[aStreamFileName])
        {
            var vValue = aFields.TryGetValue(vField, out var vText) ? vText : null;
            vRecord[vField] = TypedValue(vField, vValue);
        }

        return JsonSerializer.Serialize(vRecord);
    }

    private static object? TypedValue(string aField, string? aValue)
    {
        if (string.IsNullOrWhiteSpace(aValue))
        {
            return MeasurementSchema.ArrayFields.Contains(aField) ? Array.Empty<string>() : null;
        }

        if (MeasurementSchema.IntFields.Contains(aField))
        {
            return (long)Math.Round(double.Parse(aValue, NumberStyles.Float, CultureInfo.InvariantCulture), MidpointRounding.AwayFromZero);
        }

        if (MeasurementSchema.NumberFields.Contains(aField))
        {
            return double.Parse(aValue, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        if (MeasurementSchema.ArrayFields.Contains(aField))
        {
            return aValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        if (MeasurementSchema.BoolFields.Contains(aField))
        {
            return string.Equals(aValue, "true", StringComparison.OrdinalIgnoreCase);
        }

        return aValue;
    }

    private static string NormalizeStreamName(string aStreamName)
    {
        var vTrimmed = aStreamName.Trim();
        return vTrimmed.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase) ? vTrimmed : vTrimmed + ".jsonl";
    }

    /// <summary>The <c>Project</c> table's own shape, read by Dapper.</summary>
    private sealed class ProjectRow
    {
        public string Name { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;
    }

    /// <summary>
    /// A project's <c>docs/metrics</c> folder. A solution's path is its .sln or .slnx file, so its folder is the one
    /// around that file: before, the file itself was taken for a folder and every write was refused with "Cannot
    /// create '…\MyTasks.sln'" (owner's log, 21 Nov 2026).
    /// </summary>
    /// <param name="aProjectPath">The project's stored path: a folder, or a solution file.</param>
    private static string MetricsFolderOf(string aProjectPath)
    {
        var vRoot = File.Exists(aProjectPath) ? Path.GetDirectoryName(aProjectPath) ?? aProjectPath : aProjectPath;
        return Path.Combine(vRoot, "docs", "metrics");
    }
}
