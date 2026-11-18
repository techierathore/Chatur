using System.Text.Json;
using Chatur.Core.Actions;
using Chatur.Core.Board;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Chatur.Core.ProcessRuns;
using Chatur.Core.Processes;
using Chatur.Core.Routing;
using Dapper;

namespace Chatur.Core.Estimates;

/// <summary>
/// The Estimator (Architecture §7): reads the project's checklist and run records and the sister projects' run
/// records, asks routing which model and tier each step is set to, and works the estimate out with
/// <see cref="EstimateCalculator"/>. It depends on no model provider, agent or router, so it cannot make a model
/// call (BRD-199). It writes only <c>estimates.jsonl</c>, and only on a confirmed start (BRD-205, BRD-206).
/// </summary>
public sealed class EstimateActions : IEstimateActions
{
    private const string RunsFile = "runs.jsonl";
    private const string EstimatesFile = "estimates.jsonl";

    /// <summary>
    /// The process whose owning agent runs each step, and the kind of work on Settings ▸ Routing that names the step's
    /// tier when the owner has set one (REQ-FN-019: a work tier wins over the role's own tier for that work).
    /// </summary>
    private static readonly IReadOnlyDictionary<string, (string Process, string WorkKind)> StepOwners =
        new Dictionary<string, (string Process, string WorkKind)>
        {
            [EstimateSteps.Build] = ("build-phase", "write-code"),
            [EstimateSteps.Verify] = ("verify-phase", "verify"),
            [EstimateSteps.Fix] = ("fix-issues", "write-code"),
            [EstimateSteps.Triage] = ("triage-issues", "review-code")
        };

    private readonly IDbConnectionFactory objConnections;
    private readonly IRoutingActions objRouting;
    private readonly ProjectFolders objFolders;
    private readonly IClock objClock;

    /// <summary>Creates the Estimator.</summary>
    /// <param name="aConnections">Opens connections to Chatur's database.</param>
    /// <param name="aRouting">The tiers, their chains and the work tiers; read only.</param>
    /// <param name="aFolders">Finds a project's folder.</param>
    /// <param name="aClock">Stamps a confirmed batch.</param>
    public EstimateActions(IDbConnectionFactory aConnections, IRoutingActions aRouting, ProjectFolders aFolders, IClock aClock)
    {
        objConnections = aConnections;
        objRouting = aRouting;
        objFolders = aFolders;
        objClock = aClock;
    }

    /// <inheritdoc />
    public async Task<PhaseEstimate?> EstimateAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vFolder = await objFolders.FolderAsync(aProjectId, aCt).ConfigureAwait(false);
        var vName = await ProjectNameAsync(aProjectId, aCt).ConfigureAwait(false);
        if (vFolder is null || vName is null)
        {
            return null;
        }

        var (vPhase, vRows) = await PhaseRowsAsync(vFolder, aCt).ConfigureAwait(false);
        var vOwnLines = await ReadLinesAsync(Path.Combine(vFolder, "docs", "metrics", RunsFile), aCt).ConfigureAwait(false);
        var (vOwn, vSkipped) = RunHistory.Read(vName, vOwnLines, vRows.ToHashSet(StringComparer.Ordinal));

        var vSisters = new List<SisterHistory>();
        foreach (var vSister in await SisterRowsAsync(aCt).ConfigureAwait(false))
        {
            if (SameFolder(vSister.Path, vFolder))
            {
                continue;
            }

            var vLines = await ReadLinesAsync(Path.Combine(vSister.Path, "docs", "metrics", RunsFile), aCt).ConfigureAwait(false);
            vSisters.Add(new SisterHistory(NameOf(vSister.Path), RunHistory.Read(NameOf(vSister.Path), vLines, new HashSet<string>()).Samples));
        }

        var vRoutes = await RoutesAsync(aCt).ConfigureAwait(false);
        var vAuto = await objRouting.AutoRoutingAsync(aCt).ConfigureAwait(false);
        return EstimateCalculator.Estimate(new EstimateInput(vName, vPhase, vRows, vOwn, vSkipped, vSisters, vRoutes, vAuto));
    }

    /// <inheritdoc />
    public async Task<KeptEstimate> ConfirmAsync(int aProjectId, PhaseEstimate aEstimate, string aRunKind, int aRunId, CancellationToken aCt = default)
    {
        RequireRunKind(aRunKind);
        if (await KeptAsync(aRunKind, aRunId, aCt).ConfigureAwait(false) is { Estimate.NoChecklist: false } vAlready)
        {
            return vAlready;
        }

        var vTs = string.Empty;
        if (!aEstimate.NoChecklist)
        {
            var vFolder = await objFolders.FolderAsync(aProjectId, aCt).ConfigureAwait(false)
                ?? throw new ArgumentException("The project's folder is not on disk.", nameof(aProjectId));
            var vPath = Path.Combine(vFolder, "docs", "metrics", EstimatesFile);
            var vExisting = await ReadLinesAsync(vPath, aCt).ConfigureAwait(false);
            var vAt = objClock.UtcNow;
            vTs = EstimateRecords.Stamp(vAt);
            while (EstimateRecords.Batch(vExisting, vTs) is not null)
            {
                vAt = vAt.AddSeconds(1);
                vTs = EstimateRecords.Stamp(vAt);
            }

            var vLines = EstimateRecords.ToLines(aEstimate, vTs);
            Directory.CreateDirectory(Path.GetDirectoryName(vPath)!);
            await File.AppendAllTextAsync(vPath, string.Concat(vLines.Select(aLine => aLine + "\n")), aCt).ConfigureAwait(false);
        }

        var vKept = new KeptEstimate(vTs, aEstimate);
        await KeepAsync(aProjectId, vKept, aRunKind, aRunId, aCt).ConfigureAwait(false);
        return vKept;
    }

    /// <inheritdoc />
    public async Task KeepAsync(int aProjectId, KeptEstimate aKept, string aRunKind, int aRunId, CancellationToken aCt = default)
    {
        RequireRunKind(aRunKind);
        using var vConnection = objConnections.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO RunEstimate (RunKind, RunId, ProjectId, BatchTs, Snapshot, KeptUtc)
            VALUES (@aRunKind, @aRunId, @aProjectId, @BatchTs, @vSnapshot, @vNow)
            ON CONFLICT (RunKind, RunId) DO UPDATE SET ProjectId = excluded.ProjectId, BatchTs = excluded.BatchTs,
                Snapshot = excluded.Snapshot, KeptUtc = excluded.KeptUtc;
            """,
            new { aRunKind, aRunId, aProjectId, aKept.BatchTs, vSnapshot = JsonSerializer.Serialize(aKept.Estimate), vNow = objClock.UtcNow.ToString("O") },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<KeptEstimate?> KeptAsync(string aRunKind, int aRunId, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRow = await vConnection.QuerySingleOrDefaultAsync<KeptRow>(new CommandDefinition(
            "SELECT BatchTs, Snapshot FROM RunEstimate WHERE RunKind = @aRunKind AND RunId = @aRunId;",
            new { aRunKind, aRunId },
            cancellationToken: aCt)).ConfigureAwait(false);
        var vEstimate = vRow is null ? null : JsonSerializer.Deserialize<PhaseEstimate>(vRow.Snapshot);
        return vRow is null || vEstimate is null ? null : new KeptEstimate(vRow.BatchTs, vEstimate);
    }

    /// <inheritdoc />
    public async Task<EstimateAgainstActual?> AgainstActualAsync(int aProjectId, string aBatchTs, CancellationToken aCt = default)
    {
        if (await objFolders.FolderAsync(aProjectId, aCt).ConfigureAwait(false) is not { } vFolder)
        {
            return null;
        }

        var vMetrics = Path.Combine(vFolder, "docs", "metrics");
        var vBatch = EstimateRecords.Batch(await ReadLinesAsync(Path.Combine(vMetrics, EstimatesFile), aCt).ConfigureAwait(false), aBatchTs);
        return vBatch is null
            ? null
            : EstimateReconciler.Reconcile(vBatch, await ReadLinesAsync(Path.Combine(vMetrics, RunsFile), aCt).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<EstimateAgainstActual?> PhaseAgainstActualAsync(int aProjectId, CancellationToken aCt = default)
    {
        if (await objFolders.FolderAsync(aProjectId, aCt).ConfigureAwait(false) is not { } vFolder)
        {
            return null;
        }

        var (vPhase, _) = await PhaseRowsAsync(vFolder, aCt).ConfigureAwait(false);
        var vLines = await ReadLinesAsync(Path.Combine(vFolder, "docs", "metrics", EstimatesFile), aCt).ConfigureAwait(false);
        return EstimateRecords.LatestBatchTs(vLines, vPhase) is { } vTs
            ? await AgainstActualAsync(aProjectId, vTs, aCt).ConfigureAwait(false)
            : null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SisterProject>> SistersAsync(CancellationToken aCt = default)
    {
        var vList = new List<SisterProject>();
        foreach (var vRow in await SisterRowsAsync(aCt).ConfigureAwait(false))
        {
            var vLines = await ReadLinesAsync(Path.Combine(vRow.Path, "docs", "metrics", RunsFile), aCt).ConfigureAwait(false);
            vList.Add(new SisterProject((int)vRow.SisterProjectId, vRow.Path, NameOf(vRow.Path), RunHistory.LiveRunCount(vLines)));
        }

        return vList;
    }

    /// <inheritdoc />
    public async Task<SisterProject> AddSisterAsync(string aFolder, CancellationToken aCt = default)
    {
        var vFolder = (aFolder ?? string.Empty).Trim().Trim('"');
        if (vFolder.Length == 0)
        {
            throw new ArgumentException("Name a folder to add.", nameof(aFolder));
        }

        vFolder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(vFolder));
        if (!Directory.Exists(vFolder))
        {
            throw new ArgumentException($"{vFolder} is not a folder on this computer.", nameof(aFolder));
        }

        if (!File.Exists(Path.Combine(vFolder, "docs", "metrics", RunsFile)))
        {
            throw new ArgumentException($"{NameOf(vFolder)} has no docs/metrics/runs.jsonl, so it has no run records to add.", nameof(aFolder));
        }

        if ((await SisterRowsAsync(aCt).ConfigureAwait(false)).Any(aRow => SameFolder(aRow.Path, vFolder)))
        {
            throw new ArgumentException($"{NameOf(vFolder)} is already a sister project.", nameof(aFolder));
        }

        using (var vConnection = objConnections.OpenConnection())
        {
            await vConnection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO SisterProject (Path, AddedUtc) VALUES (@vFolder, @vNow);",
                new { vFolder, vNow = objClock.UtcNow.ToString("O") },
                cancellationToken: aCt)).ConfigureAwait(false);
        }

        return (await SistersAsync(aCt).ConfigureAwait(false)).Single(aSister => SameFolder(aSister.Path, vFolder));
    }

    /// <inheritdoc />
    public async Task RemoveSisterAsync(int aSisterProjectId, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM SisterProject WHERE SisterProjectId = @aSisterProjectId;",
            new { aSisterProjectId },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <summary>
    /// The model and tier each step will run on, by the same rule the step hand-over runs it with
    /// (<see cref="StepRoutes"/>, MISS-Chatur-20261110-06).
    /// </summary>
    private async Task<IReadOnlyDictionary<string, StepRoute>> RoutesAsync(CancellationToken aCt)
    {
        var vRules = new StepRoutes(objConnections, objRouting);
        var vRoutes = new Dictionary<string, StepRoute>();
        foreach (var (vStep, (vProcess, _)) in StepOwners)
        {
            var vRoute = await vRules.ForProcessAsync(vProcess, aCt).ConfigureAwait(false);
            vRoutes[vStep] = new StepRoute(vRoute.Model, vRoute.Tier);
        }

        return vRoutes;
    }

    private static async Task<(int Phase, List<string> Rows)> PhaseRowsAsync(string aFolder, CancellationToken aCt)
    {
        var vChecklists = ProjectFolders.NewestPhaseChecklists(aFolder);
        if (vChecklists.Count == 0)
        {
            return (0, []);
        }

        var vPhase = ChecklistDocument.PhaseOf(vChecklists[0]);
        var vRows = new List<string>();
        foreach (var vRelative in vChecklists)
        {
            var vText = await File.ReadAllTextAsync(Path.Combine(aFolder, vRelative.Replace('/', Path.DirectorySeparatorChar)), aCt).ConfigureAwait(false);
            vRows.AddRange(ChecklistDocument.Parse(vText, vRelative, vPhase).Select(aItem => aItem.Id));
        }

        return (vPhase, vRows.Distinct(StringComparer.Ordinal).ToList());
    }

    private async Task<string?> ProjectNameAsync(int aProjectId, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        return await vConnection.QuerySingleOrDefaultAsync<string?>(new CommandDefinition(
            "SELECT Name FROM Project WHERE ProjectId = @aProjectId;",
            new { aProjectId },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    private async Task<List<SisterRow>> SisterRowsAsync(CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        return (await vConnection.QueryAsync<SisterRow>(new CommandDefinition(
            "SELECT SisterProjectId, Path FROM SisterProject ORDER BY SisterProjectId;",
            cancellationToken: aCt)).ConfigureAwait(false)).ToList();
    }

    private static async Task<string[]> ReadLinesAsync(string aPath, CancellationToken aCt) =>
        File.Exists(aPath) ? await File.ReadAllLinesAsync(aPath, aCt).ConfigureAwait(false) : [];

    private static string NameOf(string aFolder) => Path.GetFileName(Path.TrimEndingDirectorySeparator(aFolder));

    private static bool SameFolder(string aLeft, string aRight) =>
        string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(aLeft)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(aRight)),
            OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static void RequireRunKind(string aRunKind)
    {
        if (aRunKind is not (EstimateRunKinds.Process or EstimateRunKinds.Auto or EstimateRunKinds.Queue))
        {
            throw new ArgumentException($"\"{aRunKind}\" is not a kind of run an estimate is kept for.", nameof(aRunKind));
        }
    }

    private sealed class SisterRow
    {
        public long SisterProjectId { get; set; }

        public string Path { get; set; } = string.Empty;
    }

    private sealed class KeptRow
    {
        public string BatchTs { get; set; } = string.Empty;

        public string Snapshot { get; set; } = string.Empty;
    }
}
