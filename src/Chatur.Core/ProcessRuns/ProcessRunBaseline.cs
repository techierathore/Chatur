using System.Globalization;
using Chatur.Core.Data;
using Chatur.Core.ProcessRuns.Checks;
using Dapper;

namespace Chatur.Core.ProcessRuns;

/// <summary>
/// What a project's files looked like when a run began handing steps out, kept in <c>ProcessRunFile</c>, so a check
/// can say what changed during the run (REQ-FN-088, REQ-FN-097, REQ-FN-098). A file is fingerprinted by its length
/// and last-write time; a requirement document also by the list of its BRD numbers.
/// </summary>
public sealed class ProcessRunBaseline
{
    /// <summary>The key prefix of a requirement document's id list.</summary>
    public const string BrdIdsPrefix = "brd-ids:";

    private static readonly string[] SkippedFolders = ["bin", "obj", "node_modules", ".git", ".artifacts", ".vs", ".tfcore", ".claude", ".opencode"];

    private readonly IDbConnectionFactory objConnections;

    /// <summary>Creates the baseline store.</summary>
    /// <param name="aConnections">Opens connections to Chatur's database.</param>
    public ProcessRunBaseline(IDbConnectionFactory aConnections)
    {
        objConnections = aConnections;
    }

    /// <summary>Takes the snapshot of the project's files for the run unless the run already has one.</summary>
    /// <param name="aRunId">The run.</param>
    /// <param name="aFolder">The project's folder.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    public async Task EnsureAsync(int aRunId, string aFolder, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vTaken = await vConnection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT COUNT(*) FROM ProcessRunFile WHERE RunId = @aRunId",
            new { aRunId },
            cancellationToken: aCt)).ConfigureAwait(false);
        if (vTaken > 0)
        {
            return;
        }

        var vSnapshot = Snapshot(aFolder);
        vSnapshot[string.Empty] = "taken";
        using var vTransaction = vConnection.BeginTransaction();
        foreach (var vEntry in vSnapshot)
        {
            await vConnection.ExecuteAsync(new CommandDefinition(
                "INSERT OR IGNORE INTO ProcessRunFile (RunId, Path, Fingerprint) VALUES (@aRunId, @Path, @Fingerprint)",
                new { aRunId, Path = vEntry.Key, Fingerprint = vEntry.Value },
                vTransaction,
                cancellationToken: aCt)).ConfigureAwait(false);
        }

        vTransaction.Commit();
    }

    /// <summary>The snapshot the run took, or an empty dictionary when it took none.</summary>
    /// <param name="aRunId">The run.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    public async Task<IReadOnlyDictionary<string, string>> LoadAsync(int aRunId, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRows = await vConnection.QueryAsync<(string Path, string Fingerprint)>(new CommandDefinition(
            "SELECT Path, Fingerprint FROM ProcessRunFile WHERE RunId = @aRunId",
            new { aRunId },
            cancellationToken: aCt)).ConfigureAwait(false);
        return vRows.ToDictionary(aRow => aRow.Path, aRow => aRow.Fingerprint, StringComparer.Ordinal);
    }

    /// <summary>When the run began.</summary>
    /// <param name="aRunId">The run.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    public async Task<DateTime?> StartedUtcAsync(int aRunId, CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vStamp = await vConnection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "SELECT StartedUtc FROM ProcessRun WHERE RunId = @aRunId",
            new { aRunId },
            cancellationToken: aCt)).ConfigureAwait(false);
        return vStamp is null ? null : DateTime.Parse(vStamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();
    }

    /// <summary>The fingerprints of the project's documents, sources and tests as they are now.</summary>
    /// <param name="aFolder">The project's folder.</param>
    public static Dictionary<string, string> Snapshot(string aFolder)
    {
        var vSnapshot = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var vTop in new[] { "docs", "src", "tests" })
        {
            var vFolder = Path.Combine(aFolder, vTop);
            if (Directory.Exists(vFolder))
            {
                Walk(aFolder, vFolder, vSnapshot);
            }
        }

        var vStatus = Path.Combine(aFolder, ProjectDocuments.StatusPath);
        if (File.Exists(vStatus))
        {
            vSnapshot[ProjectDocuments.StatusPath] = Fingerprint(vStatus);
        }

        var vDocuments = new ProjectDocuments(aFolder);
        foreach (var vBrd in vDocuments.Brds())
        {
            var vNumbers = ProjectDocuments.ParseBrd(vDocuments.Read(vBrd)).Select(aItem => aItem.Number).Distinct().Order();
            vSnapshot[BrdIdsPrefix + vBrd] = string.Join(',', vNumbers);
        }

        return vSnapshot;
    }

    /// <summary>The fingerprint of one file.</summary>
    /// <param name="aFullPath">The file's full path.</param>
    public static string Fingerprint(string aFullPath)
    {
        var vInfo = new FileInfo(aFullPath);
        return string.Create(CultureInfo.InvariantCulture, $"{vInfo.Length}:{vInfo.LastWriteTimeUtc.Ticks}");
    }

    private static void Walk(string aRoot, string aFolder, Dictionary<string, string> aSnapshot)
    {
        foreach (var vFile in Directory.EnumerateFiles(aFolder))
        {
            aSnapshot[Path.GetRelativePath(aRoot, vFile).Replace('\\', '/')] = Fingerprint(vFile);
        }

        foreach (var vSub in Directory.EnumerateDirectories(aFolder))
        {
            if (!SkippedFolders.Contains(Path.GetFileName(vSub), StringComparer.OrdinalIgnoreCase))
            {
                Walk(aRoot, vSub, aSnapshot);
            }
        }
    }
}
