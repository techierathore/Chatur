using System.Globalization;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Chatur.Core.Processes;

/// <summary>
/// <see cref="IProcessActions"/> over <see cref="ProcessCatalogue"/>, the <c>ProcessStepText</c> and
/// <c>ProcessRun</c> tables and the project's own folder (REQ-FN-049, REQ-FN-050, REQ-FN-051).
/// </summary>
public sealed class ProcessActions : IProcessActions
{
    private const string RunningState = "running";

    private readonly IDbConnectionFactory objConnections;
    private readonly IClock objClock;
    private readonly ILogger<ProcessActions> objLogger;

    /// <summary>Creates the action implementation.</summary>
    /// <param name="aConnections">Opens connections to Chatur's database.</param>
    /// <param name="aClock">The current time, so a test can fix it.</param>
    /// <param name="aLogger">Where a refused write is logged.</param>
    public ProcessActions(IDbConnectionFactory aConnections, IClock aClock, ILogger<ProcessActions> aLogger)
    {
        objConnections = aConnections;
        objClock = aClock;
        objLogger = aLogger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProcessSummary>> ListAsync(int? aProjectId, CancellationToken aCt = default)
    {
        var vLast = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        var vRunning = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (aProjectId is { } vProjectId)
        {
            using var vConnection = objConnections.OpenConnection();
            var vRows = await vConnection.QueryAsync<RunRow>(new CommandDefinition(
                "SELECT RunId, ProcessCode, State, StartedUtc, CurrentStep FROM ProcessRun WHERE ProjectId = @vProjectId ORDER BY RunId",
                new { vProjectId },
                cancellationToken: aCt)).ConfigureAwait(false);
            foreach (var vRow in vRows)
            {
                vLast[vRow.ProcessCode] = DateTime.Parse(vRow.StartedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                if (vRow.State == RunningState)
                {
                    vRunning[vRow.ProcessCode] = vRow.RunId;
                }
                else
                {
                    vRunning.Remove(vRow.ProcessCode);
                }
            }
        }

        return ProcessCatalogue.All
            .Select(aProcess => new ProcessSummary(
                aProcess.Code,
                aProcess.Description,
                aProcess.Agent,
                aProcess.Steps,
                aProcess.Checks,
                aProcess.OwnerStops,
                vLast.TryGetValue(aProcess.Code, out var vWhen) ? vWhen : null,
                vRunning.ContainsKey(aProcess.Code),
                vRunning.TryGetValue(aProcess.Code, out var vRunId) ? vRunId : null))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProcessStepInfo>> StepsAsync(string aProcessCode, CancellationToken aCt = default)
    {
        var vProcess = ProcessCatalogue.Find(aProcessCode);
        if (vProcess is null)
        {
            return [];
        }

        using var vConnection = objConnections.OpenConnection();
        var vRows = await vConnection.QueryAsync<StepTextRow>(new CommandDefinition(
            "SELECT StepCode, Text FROM ProcessStepText WHERE ProcessCode = @Code",
            new { vProcess.Code },
            cancellationToken: aCt)).ConfigureAwait(false);
        var vTexts = vRows.ToDictionary(aRow => aRow.StepCode, aRow => aRow.Text);

        var vSteps = new List<ProcessStepInfo>(vProcess.Steps);
        for (var vPosition = 1; vPosition <= vProcess.Steps; vPosition++)
        {
            var vCode = "s" + vPosition.ToString(CultureInfo.InvariantCulture);
            vSteps.Add(new ProcessStepInfo(
                vPosition,
                vCode,
                vTexts.TryGetValue(vCode, out var vText) ? vText : string.Empty,
                vProcess.KindAt(vPosition),
                vProcess.HasCheckAt(vPosition)));
        }

        return vSteps;
    }

    /// <inheritdoc />
    public async Task<ProcessRunStarted> StartAsync(int aProjectId, string aProcessCode, CancellationToken aCt = default)
    {
        var vProcess = ProcessCatalogue.Find(aProcessCode)
            ?? throw new ArgumentException($"'{aProcessCode}' is not one of the TechieFlow commands.", nameof(aProcessCode));

        var vFirstWording = (await StepsAsync(vProcess.Code, aCt).ConfigureAwait(false)).FirstOrDefault()?.Wording ?? string.Empty;

        using var vConnection = objConnections.OpenConnection();
        var vProjectKnown = await vConnection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM Project WHERE ProjectId = @aProjectId",
            new { aProjectId },
            cancellationToken: aCt)).ConfigureAwait(false);
        if (vProjectKnown == 0)
        {
            throw new ArgumentException($"Project {aProjectId} is not known.", nameof(aProjectId));
        }

        var vExisting = await vConnection.QueryFirstOrDefaultAsync<RunRow>(new CommandDefinition(
            @"SELECT RunId, ProcessCode, State, StartedUtc, CurrentStep FROM ProcessRun
              WHERE ProjectId = @aProjectId AND ProcessCode = @Code AND State = @RunningState ORDER BY RunId DESC LIMIT 1",
            new { aProjectId, vProcess.Code, RunningState },
            cancellationToken: aCt)).ConfigureAwait(false);
        if (vExisting is not null)
        {
            return new ProcessRunStarted(
                vExisting.RunId,
                vProcess.Code,
                aProjectId,
                vExisting.State,
                vExisting.CurrentStep,
                vFirstWording,
                DateTime.Parse(vExisting.StartedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                AlreadyRunning: true);
        }

        var vNow = objClock.UtcNow;
        var vRunId = await vConnection.ExecuteScalarAsync<int>(new CommandDefinition(
            @"INSERT INTO ProcessRun (ProjectId, ProcessCode, State, CurrentStep, StartedUtc)
              VALUES (@aProjectId, @Code, @RunningState, 1, @StartedUtc);
              SELECT last_insert_rowid();",
            new { aProjectId, vProcess.Code, RunningState, StartedUtc = vNow.ToString("O", CultureInfo.InvariantCulture) },
            cancellationToken: aCt)).ConfigureAwait(false);

        return new ProcessRunStarted(vRunId, vProcess.Code, aProjectId, RunningState, 1, vFirstWording, vNow, AlreadyRunning: false);
    }

    /// <inheritdoc />
    public async Task<ProcessDocuments?> DocumentsAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vRoot = await ReadRootAsync(aProjectId, aCt).ConfigureAwait(false);
        if (vRoot is null)
        {
            return null;
        }

        var vDocs = Path.Combine(vRoot, "docs");
        return new ProcessDocuments(
            Newest(vDocs, "*-BRD.md"),
            Newest(vDocs, "*-Checklist.md"),
            ProcessDocumentGuard.StatusPath);
    }

    /// <inheritdoc />
    public async Task WriteDocumentAsync(int aProjectId, string aRelativePath, string aContent, CancellationToken aCt = default)
    {
        var vRoot = await ReadRootAsync(aProjectId, aCt).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Project {aProjectId} is not known.");

        if (!ProcessDocumentGuard.TryResolve(vRoot, aRelativePath, out var vFullPath))
        {
            objLogger.LogWarning("A process was refused a write to {Path} in project {ProjectId}.", aRelativePath, aProjectId);
            throw new InvalidOperationException(
                $"A process writes only the project's own BRD, checklist and status documents; '{aRelativePath}' is none of them.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(vFullPath)!);
        await File.WriteAllTextAsync(vFullPath, aContent, aCt).ConfigureAwait(false);
    }

    /// <summary>The last-named file matching a pattern directly under the docs folder, project-relative, or null.</summary>
    private static string? Newest(string aDocsFolder, string aPattern)
    {
        if (!Directory.Exists(aDocsFolder))
        {
            return null;
        }

        var vName = Directory.EnumerateFiles(aDocsFolder, aPattern)
            .Select(Path.GetFileName)
            .OfType<string>()
            .OrderByDescending(aName => aName, StringComparer.Ordinal)
            .FirstOrDefault();
        return vName is null ? null : "docs/" + vName;
    }

    /// <summary>The project's folder: its path, or the folder holding it when the path is a solution file.</summary>
    private async Task<string?> ReadRootAsync(int aProjectId, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        var vPath = await vConnection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "SELECT Path FROM Project WHERE ProjectId = @aProjectId",
            new { aProjectId },
            cancellationToken: aCt)).ConfigureAwait(false);
        if (vPath is null)
        {
            return null;
        }

        return File.Exists(vPath) ? Path.GetDirectoryName(vPath) : vPath;
    }

    /// <summary>A <c>ProcessRun</c> row, read by Dapper.</summary>
    private sealed class RunRow
    {
        public int RunId { get; set; }

        public string ProcessCode { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string StartedUtc { get; set; } = string.Empty;

        public int CurrentStep { get; set; }
    }

    /// <summary>A <c>ProcessStepText</c> row, read by Dapper.</summary>
    private sealed class StepTextRow
    {
        public string StepCode { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;
    }
}
