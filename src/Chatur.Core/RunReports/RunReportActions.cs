using System.Globalization;
using Chatur.Core.Actions;
using Chatur.Core.AutoRun;
using Chatur.Core.Data;
using Dapper;

namespace Chatur.Core.RunReports;

/// <summary>
/// <see cref="IRunReportActions"/> over the selected project's end-to-end runs (<c>AutoRun</c> rows, key
/// <c>auto-{id}</c>) and its <c>docs/metrics</c> files. A run that Chatur carried out is reported from its own
/// rows: the verdicts its process runs wrote, the waits and retries in its record, and the misses and the
/// tokens the project's measurement streams recorded while it ran. An end-to-end run recorded in
/// <c>runs.jsonl</c> only (command <c>auto</c>, e.g. by the framework outside Chatur) is listed beside them and
/// reported from the streams, as before. Read only; every call reads again.
/// </summary>
public sealed class RunReportActions : IRunReportActions
{
    /// <summary>The prefix of a run Chatur carried out itself; the rest of the key is the <c>AutoRun</c> id.</summary>
    public const string AutoKeyPrefix = "auto-";

    private const string AutoCommand = "auto";

    private readonly IProjectActions objProjects;
    private readonly IRequirementBoardActions objBoard;
    private readonly AutoRunStore objStore;
    private readonly IDbConnectionFactory objConnections;
    private readonly Platform.IClock objClock;

    /// <summary>Creates the actions.</summary>
    /// <param name="aProjects">Says which project is selected.</param>
    /// <param name="aBoard">Gives the titles and owners of the project's requirements.</param>
    /// <param name="aStore">Reads the end-to-end runs.</param>
    /// <param name="aConnections">Opens connections to Chatur's database, for the verdicts a run's process runs wrote.</param>
    /// <param name="aClock">The time source, for the end of a run that is still going.</param>
    public RunReportActions(
        IProjectActions aProjects,
        IRequirementBoardActions aBoard,
        AutoRunStore aStore,
        IDbConnectionFactory aConnections,
        Platform.IClock aClock)
    {
        objProjects = aProjects;
        objBoard = aBoard;
        objStore = aStore;
        objConnections = aConnections;
        objClock = aClock;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RunReportEntry>> RunsAsync(CancellationToken aCt = default)
    {
        var vProject = await objProjects.SelectedProjectAsync(aCt).ConfigureAwait(false);
        if (vProject is null)
        {
            return [];
        }

        var vAuto = (await objStore.ListAsync(vProject.ProjectId, aCt).ConfigureAwait(false)).Select(aRow => EntryOf(aRow));
        var vRecorded = RunReportReader.Runs(vProject.Name, await ReadAsync(vProject, "runs.jsonl", aCt).ConfigureAwait(false))
            .Where(aEntry => aEntry.Command == AutoCommand);
        return [.. vAuto.Concat(vRecorded).OrderByDescending(aEntry => aEntry.StartedUtc)];
    }

    /// <inheritdoc />
    public async Task<RunReportView?> ReportAsync(string aRunKey, CancellationToken aCt = default)
    {
        var vProject = await objProjects.SelectedProjectAsync(aCt).ConfigureAwait(false);
        if (vProject is null)
        {
            return null;
        }

        var vRuns = await ReadAsync(vProject, "runs.jsonl", aCt).ConfigureAwait(false);
        var vMisses = await ReadAsync(vProject, "misses.jsonl", aCt).ConfigureAwait(false);
        var vBoard = await objBoard.LoadAsync(RequirementFilter.None, aCt).ConfigureAwait(false);

        if (aRunKey.StartsWith(AutoKeyPrefix, StringComparison.Ordinal))
        {
            return int.TryParse(aRunKey.AsSpan(AutoKeyPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var vId)
                && await objStore.RunAsync(vId, aCt).ConfigureAwait(false) is { } vRow
                && vRow.ProjectId == vProject.ProjectId
                    ? await AutoReportAsync(vRow, vRuns, vMisses, vBoard.Items, aCt).ConfigureAwait(false)
                    : null;
        }

        var vGates = await ReadAsync(vProject, "gates.jsonl", aCt).ConfigureAwait(false);
        return RunReportReader.Report(vProject.Name, aRunKey, vRuns, vGates, vMisses, vBoard.Items);
    }

    private static RunReportEntry EntryOf(AutoRunRow aRow) =>
        new(
            AutoKeyPrefix + aRow.AutoRunId.ToString(CultureInfo.InvariantCulture),
            aRow.ProjectName,
            AutoCommand,
            AutoRunStore.ParseStamp(aRow.StartedUtc),
            aRow.EndedUtc is null ? null : AutoRunStore.ParseStamp(aRow.EndedUtc));

    private async Task<RunReportView> AutoReportAsync(
        AutoRunRow aRow,
        IReadOnlyList<string> aRuns,
        IReadOnlyList<string> aMisses,
        IReadOnlyList<RequirementItem> aItems,
        CancellationToken aCt)
    {
        var vEntry = EntryOf(aRow);
        var vId = (int)aRow.AutoRunId;
        var vChain = await objStore.ProcessesAsync(vId, aCt).ConfigureAwait(false);
        var vEvents = await objStore.EventsAsync(vId, aCt).ConfigureAwait(false);
        var vVerdicts = await VerdictsAsync(vChain.Where(aProcess => aProcess.ProcessRunId is not null).Select(aProcess => aProcess.ProcessRunId!.Value).ToList(), aCt).ConfigureAwait(false);

        var vUntil = vEntry.EndedUtc ?? objClock.UtcNow;
        var vWaits = WaitsOf(vEvents, vUntil);
        var vWaited = vWaits.Count == 0 ? (long?)null : (long)vWaits.Sum(aWait => aWait.TotalSeconds);
        var vDuration = vEntry.EndedUtc is { } vEnded
            ? (long)Math.Max(0, (vEnded - vEntry.StartedUtc).TotalSeconds - (vWaited ?? 0))
            : (long?)null;
        var vCost = RunReportReader.CostBetween(aRuns, vEntry.StartedUtc, vUntil);
        long? vTotal = vCost.TokensIn is null && vCost.TokensOut is null ? null : (vCost.TokensIn ?? 0) + (vCost.TokensOut ?? 0);

        return new RunReportView(
            vEntry,
            vVerdicts.Select(aVerdict => Requirement(aVerdict, aItems)).ToList(),
            RunReportReader.MissesBetween(aMisses, vEntry.StartedUtc, vUntil),
            vCost.Rows,
            vTotal,
            vDuration,
            vWaited,
            vCost.TokensIn,
            vCost.TokensOut,
            vWaits.Count,
            vChain.Sum(aProcess => (int)aProcess.Retries));
    }

    /// <summary>The length of every usage-limit wait: from the wait to the entry that ended it, or to the planned end / the run's end.</summary>
    private static List<TimeSpan> WaitsOf(IReadOnlyList<AutoRunEvent> aEvents, DateTime aUntil)
    {
        var vWaits = new List<TimeSpan>();
        for (var vIndex = 0; vIndex < aEvents.Count; vIndex++)
        {
            if (aEvents[vIndex].Kind != AutoRunEventKind.LimitWait)
            {
                continue;
            }

            var vBegan = aEvents[vIndex].AtUtc;
            var vEnd = aEvents.Skip(vIndex + 1).FirstOrDefault(aEvent => aEvent.AtUtc >= vBegan && aEvent.Kind is AutoRunEventKind.LimitResumed or AutoRunEventKind.Stopped or AutoRunEventKind.Paused)?.AtUtc
                ?? Min(aEvents[vIndex].UntilUtc ?? aUntil, aUntil);
            vWaits.Add(vEnd > vBegan ? vEnd - vBegan : TimeSpan.Zero);
        }

        return vWaits;
    }

    private static DateTime Min(DateTime aFirst, DateTime aSecond) => aFirst < aSecond ? aFirst : aSecond;

    private static RunReportRequirement Requirement(VerdictRow aVerdict, IReadOnlyList<RequirementItem> aItems)
    {
        var vItem = aItems.FirstOrDefault(aCandidate => string.Equals(aCandidate.Id, aVerdict.RequirementId, StringComparison.OrdinalIgnoreCase));
        var vType = aVerdict.RequirementId.Split('-') is { Length: > 1 } vParts ? vParts[1] : string.Empty;
        return new RunReportRequirement(
            aVerdict.RequirementId,
            vItem?.Title ?? string.Empty,
            vItem?.AgentName ?? Board.RequirementAgents.ForType(vType).Name,
            aVerdict.Status,
            AutoRunStore.ParseStamp(aVerdict.WrittenUtc));
    }

    /// <summary>The last verdict each requirement was left at by the run's process runs, in the order they were first graded.</summary>
    private async Task<IReadOnlyList<VerdictRow>> VerdictsAsync(IReadOnlyList<long> aProcessRunIds, CancellationToken aCt)
    {
        if (aProcessRunIds.Count == 0)
        {
            return [];
        }

        using var vConnection = objConnections.OpenConnection();
        var vRows = (await vConnection.QueryAsync<VerdictRow>(new CommandDefinition(
            "SELECT RequirementId, Status, WrittenUtc FROM ProcessRunVerdict WHERE RunId IN @aProcessRunIds ORDER BY ProcessRunVerdictId",
            new { aProcessRunIds },
            cancellationToken: aCt)).ConfigureAwait(false)).ToList();
        return vRows
            .GroupBy(aRow => aRow.RequirementId, StringComparer.OrdinalIgnoreCase)
            .Select(aGroup => aGroup.Last())
            .ToList();
    }

    private static async Task<string[]> ReadAsync(ProjectSummary aProject, string aStream, CancellationToken aCt)
    {
        var vFolder = Directory.Exists(aProject.Path) ? aProject.Path : Path.GetDirectoryName(aProject.Path);
        var vFile = vFolder is null ? null : Path.Combine(vFolder, "docs", "metrics", aStream);
        return vFile is not null && File.Exists(vFile)
            ? await File.ReadAllLinesAsync(vFile, aCt).ConfigureAwait(false)
            : [];
    }

    private sealed class VerdictRow
    {
        public string RequirementId { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string WrittenUtc { get; set; } = string.Empty;
    }
}
