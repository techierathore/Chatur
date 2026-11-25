using System.IO.Compression;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;

namespace Chatur.Core.Measurements;

/// <summary>
/// <see cref="IMetricsReportActions"/> over the project's own <c>docs/metrics</c> files. Reads files only: it holds no
/// <c>HttpClient</c>, calls no App Manager and uploads nothing, so a private project on a machine with no way out still
/// gets its figures (BRD-253, REQ-UI-115). The adding up is <see cref="MetricsCalculator"/>.
/// </summary>
public sealed class MetricsReportActions : IMetricsReportActions
{
    /// <summary>The most TfLens takes in one zip (UploadBounds): 25 MB.</summary>
    public const long MaxZipBytes = 25L * 1024 * 1024;

    /// <summary>The most TfLens takes from one entry (UploadBounds): 50 MB.</summary>
    public const long MaxEntryBytes = 50L * 1024 * 1024;

    private static readonly string[] ExportFiles = ["runs.jsonl", "gates.jsonl", "misses.jsonl", "sessions.jsonl", "commits.jsonl", "estimates.jsonl"];

    private readonly IDbConnectionFactory objConnections;
    private readonly IProjectActions objProjects;
    private readonly IClock objClock;
    private readonly TimeZoneInfo objZone;

    /// <summary>Creates the actions.</summary>
    /// <param name="aConnections">Opens connections to Chatur's database, for the project's name and path.</param>
    /// <param name="aProjects">Lists the projects, for the across-projects figures.</param>
    /// <param name="aClock">The current time.</param>
    /// <param name="aZone">The time zone day boundaries are taken in; this machine's when not given.</param>
    public MetricsReportActions(IDbConnectionFactory aConnections, IProjectActions aProjects, IClock aClock, TimeZoneInfo? aZone = null)
    {
        objConnections = aConnections;
        objProjects = aProjects;
        objClock = aClock;
        objZone = aZone ?? TimeZoneInfo.Local;
    }

    /// <inheritdoc />
    public async Task<MetricsReport?> ReportAsync(int aProjectId, MetricsPeriod aPeriod, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRow = await vConnection.QuerySingleOrDefaultAsync<ProjectRow>(new CommandDefinition(
            "SELECT Name, Path FROM Project WHERE ProjectId = @aProjectId",
            new { aProjectId },
            cancellationToken: aCt)).ConfigureAwait(false);
        if (vRow is null)
        {
            return null;
        }

        var vFolder = MetricsFolderOf(vRow.Path);
        var vFound = Directory.Exists(vFolder);
        var vComputed = await ComputeAsync(vFolder, aPeriod, aCt).ConfigureAwait(false);
        return new MetricsReport(
            aProjectId,
            vRow.Name,
            vFolder,
            vFound,
            aPeriod,
            vComputed.Totals,
            vComputed.ByModel,
            vComputed.ByAgent,
            vComputed.ByCommand,
            vComputed.Misses,
            vComputed.Runs,
            vComputed.UnreadableLines);
    }

    /// <inheritdoc />
    public async Task<MetricsAcrossProjects> AcrossProjectsAsync(MetricsPeriod aPeriod, CancellationToken aCt = default)
    {
        var vProjects = await objProjects.ScanAsync(aCt).ConfigureAwait(false);
        var vLines = new List<ProjectMetricsLine>(vProjects.Count);
        foreach (var vProject in vProjects)
        {
            var vFolder = MetricsFolderOf(vProject.Path);
            var vComputed = await ComputeAsync(vFolder, aPeriod, aCt).ConfigureAwait(false);
            vLines.Add(new ProjectMetricsLine(vProject.ProjectId, vProject.Name, vProject.Path, Directory.Exists(vFolder), vComputed.Totals, vComputed.Misses.Misses.Count));
        }

        var vOrdered = vLines.OrderByDescending(aLine => aLine.Totals.TokensOut.Value ?? 0)
            .ThenByDescending(aLine => aLine.Totals.Runs)
            .ThenBy(aLine => aLine.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new MetricsAcrossProjects(aPeriod, vOrdered, AddUp(vLines.Select(aLine => aLine.Totals).ToList()), vLines.Sum(aLine => aLine.Misses));
    }

    /// <inheritdoc />
    /// <remarks>Throws <see cref="InvalidOperationException"/>, with a message that says which limit, when the files are past what TfLens takes (25 MB zip, 512 entries, 50 MB an entry). An empty file is left out: it has nothing to import.</remarks>
    public async Task<MetricsExport?> ExportAsync(int aProjectId, string? aFolder, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRow = await vConnection.QuerySingleOrDefaultAsync<ProjectRow>(new CommandDefinition(
            "SELECT Name, Path FROM Project WHERE ProjectId = @aProjectId",
            new { aProjectId },
            cancellationToken: aCt)).ConfigureAwait(false);
        if (vRow is null)
        {
            return null;
        }

        var vFolder = MetricsFolderOf(vRow.Path);
        var vFiles = ExportFiles.Select(aName => new FileInfo(Path.Combine(vFolder, aName))).Where(aFile => aFile.Exists && aFile.Length > 0).ToList();
        if (vFiles.Count == 0)
        {
            return null;
        }

        var vTooBig = vFiles.FirstOrDefault(aFile => aFile.Length > MaxEntryBytes);
        if (vTooBig is not null)
        {
            throw new InvalidOperationException($"{vTooBig.Name} is larger than 50 MB, which is the most TfLens takes from one file. Nothing was written.");
        }

        var vTarget = aFolder ?? ProjectRootOf(vRow.Path);
        Directory.CreateDirectory(vTarget);
        var vStamp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(objClock.UtcNow, DateTimeKind.Utc), objZone);
        var vZipPath = Path.Combine(vTarget, $"{SafeName(vRow.Name)}-metrics-{vStamp:yyyyMMdd-HHmmss}.zip");

        var vEntries = new List<string>(vFiles.Count);
        try
        {
            await using (var vZipStream = new FileStream(vZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var vArchive = new ZipArchive(vZipStream, ZipArchiveMode.Create))
            {
                foreach (var vFile in vFiles)
                {
                    var vName = "docs/metrics/" + vFile.Name;
                    var vEntry = vArchive.CreateEntry(vName, CompressionLevel.Optimal);
                    await using var vEntryStream = vEntry.Open();
                    // The files are appended to while Chatur runs, so they are read shared.
                    await using var vSource = new FileStream(vFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    await vSource.CopyToAsync(vEntryStream, aCt).ConfigureAwait(false);
                    vEntries.Add(vName);
                }
            }

            var vBytes = new FileInfo(vZipPath).Length;
            if (vBytes > MaxZipBytes)
            {
                throw new InvalidOperationException($"The zip would be {vBytes / (1024 * 1024)} MB, and TfLens takes at most 25 MB in one zip. Nothing was kept.");
            }

            return new MetricsExport(vZipPath, vEntries, vBytes);
        }
        catch
        {
            if (File.Exists(vZipPath))
            {
                File.Delete(vZipPath);
            }

            throw;
        }
    }

    private async Task<MetricsComputation> ComputeAsync(string aMetricsFolder, MetricsPeriod aPeriod, CancellationToken aCt)
    {
        var vRuns = await ReadOrNullAsync(Path.Combine(aMetricsFolder, "runs.jsonl"), aCt).ConfigureAwait(false);
        var vGates = await ReadOrNullAsync(Path.Combine(aMetricsFolder, "gates.jsonl"), aCt).ConfigureAwait(false);
        var vMisses = await ReadOrNullAsync(Path.Combine(aMetricsFolder, "misses.jsonl"), aCt).ConfigureAwait(false);
        return MetricsCalculator.Compute(vRuns, vGates, vMisses, aPeriod, objClock.UtcNow, objZone);
    }

    private static async Task<string?> ReadOrNullAsync(string aPath, CancellationToken aCt)
    {
        if (!File.Exists(aPath))
        {
            return null;
        }

        // The files are appended to while Chatur runs, so they are read shared.
        await using var vStream = new FileStream(aPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var vReader = new StreamReader(vStream);
        return await vReader.ReadToEndAsync(aCt).ConfigureAwait(false);
    }

    private static RunTotals AddUp(IReadOnlyList<RunTotals> aTotals)
    {
        MeasuredSum Sum(Func<RunTotals, MeasuredSum> aPick)
        {
            var vParts = aTotals.Select(aPick).ToList();
            var vMeasured = vParts.Sum(aPart => aPart.Measured);
            return new MeasuredSum(vMeasured == 0 ? null : vParts.Sum(aPart => aPart.Value ?? 0), vMeasured, vParts.Sum(aPart => aPart.NotMeasured));
        }

        var vPriced = aTotals.Sum(aTotal => aTotal.Cost.Priced);
        return new RunTotals(
            aTotals.Sum(aTotal => aTotal.Runs),
            Sum(aTotal => aTotal.TokensIn),
            Sum(aTotal => aTotal.TokensOut),
            Sum(aTotal => aTotal.CacheRead),
            Sum(aTotal => aTotal.CacheWrite),
            new MeasuredCost(vPriced == 0 ? null : aTotals.Sum(aTotal => aTotal.Cost.Usd ?? 0m), vPriced, aTotals.Sum(aTotal => aTotal.Cost.NoPrice)),
            Sum(aTotal => aTotal.WallClockS));
    }

    private static string SafeName(string aName)
    {
        var vInvalid = Path.GetInvalidFileNameChars();
        var vClean = new string(aName.Select(aChar => vInvalid.Contains(aChar) ? '_' : aChar).ToArray()).Trim();
        return vClean.Length == 0 ? "project" : vClean;
    }

    /// <summary>The folder around a solution file, or the folder itself.</summary>
    private static string ProjectRootOf(string aProjectPath) =>
        File.Exists(aProjectPath) ? Path.GetDirectoryName(aProjectPath) ?? aProjectPath : aProjectPath;

    /// <summary>A project's <c>docs/metrics</c> folder; a solution's path is its .sln or .slnx file, so its folder is the one around it (as <see cref="MeasurementActions"/> does).</summary>
    private static string MetricsFolderOf(string aProjectPath) => Path.Combine(ProjectRootOf(aProjectPath), "docs", "metrics");

    /// <summary>The <c>Project</c> table's own shape, read by Dapper.</summary>
    private sealed class ProjectRow
    {
        public string Name { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;
    }
}
