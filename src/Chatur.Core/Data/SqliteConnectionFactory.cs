using System.Collections.Concurrent;
using System.Data;
using Chatur.Core.Platform;
using Microsoft.Data.Sqlite;

namespace Chatur.Core.Data;

/// <summary>
/// Opens connections to Chatur's SQLite files (Architecture §6, 2026-11-17). Each account has its own
/// file, <c>accounts/&lt;App Manager user id&gt;/chatur.db</c> under <see cref="IAppPaths.AppDataFolder"/>,
/// so nothing one account makes or opens can be read by another (BRD-210 to BRD-213). The install's own
/// file, <see cref="IAppPaths.DatabasePath"/>, holds the device identifier and — untouched — whatever was
/// kept before accounts had files of their own; no account is ever shown it (BRD-214).
/// </summary>
public sealed class SqliteConnectionFactory : IDbConnectionFactory
{
    /// <summary>The folder under <see cref="IAppPaths.AppDataFolder"/> that holds one folder per account.</summary>
    public const string AccountsFolderName = "accounts";

    /// <summary>The file name of every Chatur database, the install's and each account's.</summary>
    public const string DatabaseFileName = "chatur.db";

    private readonly IAppPaths objPaths;
    private readonly AppState? objAppState;
    private readonly IDatabaseMigrator? objMigrator;
    private readonly ConcurrentDictionary<string, Lazy<bool>> objMigrated = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a factory that always opens the install's file — for a tool or a test with no accounts.
    /// </summary>
    /// <param name="aPaths">Where the database file lives on this machine.</param>
    public SqliteConnectionFactory(IAppPaths aPaths)
    {
        objPaths = aPaths;
    }

    /// <summary>
    /// Creates a factory that opens the signed-in account's own file, migrating it the first time.
    /// </summary>
    /// <param name="aPaths">Where Chatur's files live on this machine.</param>
    /// <param name="aAppState">Names who is signed in.</param>
    /// <param name="aMigrator">Brings an account's file up to the current schema the first time it is opened.</param>
    public SqliteConnectionFactory(IAppPaths aPaths, AppState aAppState, IDatabaseMigrator aMigrator)
    {
        objPaths = aPaths;
        objAppState = aAppState;
        objMigrator = aMigrator;
    }

    /// <summary>
    /// The file the signed-in account's data lives in, or the install's file when nobody is signed in.
    /// </summary>
    public string CurrentDatabasePath
    {
        get
        {
            var vUserId = CurrentUserId;
            return vUserId is null ? objPaths.DatabasePath : AccountDatabasePath(objPaths.AppDataFolder, vUserId);
        }
    }

    /// <summary>
    /// The file one account's data lives in. The id is App Manager's; anything in it that cannot be part
    /// of a folder name is replaced, so no id can reach outside the accounts folder.
    /// </summary>
    /// <param name="aAppDataFolder">Chatur's own folder on this machine.</param>
    /// <param name="aUserId">The account's App Manager user id.</param>
    public static string AccountDatabasePath(string aAppDataFolder, string aUserId) =>
        Path.Combine(aAppDataFolder, AccountsFolderName, AccountFolderName(aUserId), DatabaseFileName);

    /// <summary>The account's folder name: its id, with anything a folder name cannot hold replaced by <c>_</c>.</summary>
    /// <param name="aUserId">The account's App Manager user id.</param>
    public static string AccountFolderName(string aUserId)
    {
        var vInvalid = Path.GetInvalidFileNameChars();
        var vName = new string(aUserId.Trim().Select(c => vInvalid.Contains(c) || c == '.' ? '_' : c).ToArray());
        return vName.Length == 0 ? "_" : vName;
    }

    /// <inheritdoc />
    public IDbConnection OpenConnection()
    {
        var vPath = CurrentDatabasePath;
        if (!string.Equals(vPath, objPaths.DatabasePath, StringComparison.OrdinalIgnoreCase))
        {
            EnsureMigrated(vPath);
        }

        return Open(vPath);
    }

    /// <inheritdoc />
    public IDbConnection OpenInstallConnection() => Open(objPaths.DatabasePath);

    /// <inheritdoc />
    public string? CurrentUserId => objAppState is null ? null : AccountScope.CurrentUserId(objAppState);

    /// <summary>Creates the account's folder and brings its file to the current schema, once per file per process.</summary>
    private void EnsureMigrated(string aPath)
    {
        if (objMigrator is null)
        {
            return;
        }

        var vDone = objMigrated.GetOrAdd(aPath, aKey => new Lazy<bool>(() =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(aKey)!);
            return objMigrator.Migrate(aKey);
        }, LazyThreadSafetyMode.ExecutionAndPublication));

        if (!vDone.Value)
        {
            objMigrated.TryRemove(aPath, out _);
            throw new InvalidOperationException("The signed-in account's database could not be brought up to date; see the log.");
        }
    }

    private static IDbConnection Open(string aPath)
    {
        var vConnection = new SqliteConnection($"Data Source={aPath}");
        vConnection.Open();

        // SQLite defaults foreign-key enforcement to off per connection; every table declares a
        // foreign key expecting it to be enforced.
        using (var vPragma = vConnection.CreateCommand())
        {
            vPragma.CommandText = "PRAGMA foreign_keys = ON;";
            vPragma.ExecuteNonQuery();
        }

        return vConnection;
    }
}
