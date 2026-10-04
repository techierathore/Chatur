using System.Reflection;
using DbUp;
using DbUp.Engine;
using DbUp.Sqlite;

namespace ChaturDb;

/// <summary>
/// Runs every pending migration embedded in this assembly against Chatur's own SQLite database. The
/// app (<c>Chatur</c> and <c>Chatur.WebHarness</c>) calls <see cref="Migrate"/> once at startup,
/// before any other component touches the database (Architecture §2 "ChaturDb", §5 "Database").
/// </summary>
/// <remarks>
/// <para><b>A shipped migration is never edited; a new one is added</b> (Coding Standards, REQ-NFR-003).
/// Numbers 0001 and 0002 are this foundation's own <c>InitialSchema</c> and <c>SeedRoles</c> scripts.
/// Numbers 0003–0009 are reserved for the foundation. A cluster adding its own migration names it
/// <c>00NN-&lt;Letter&gt;-&lt;What&gt;.sql</c> starting at <c>0010 + (cluster index - 1)</c>, using the
/// cluster letters <c>bash .tfcore/utils/tf-build-list.sh Chatur</c> prints (A is index 1):
/// A=0010, B=0011, C=0012, D=0013, E=0014, F=0015, G=0016, H=0017, I=0018, J=0019, K=0020, L=0021,
/// M=0022, N=0023, O=0024. DbUp orders scripts by name, so the number is what matters — a cluster
/// with nothing to migrate simply never claims its number.</para>
/// </remarks>
public static class ChaturDbMigrator
{
    /// <summary>
    /// Applies every embedded migration that has not yet run against <paramref name="aConnectionString"/>.
    /// </summary>
    /// <param name="aConnectionString">A SQLite connection string, e.g. <c>Data Source=/path/chatur.db</c>.</param>
    /// <returns>The result DbUp reports; <see cref="DatabaseUpgradeResult.Successful"/> when every script ran.</returns>
    public static DatabaseUpgradeResult Migrate(string aConnectionString)
    {
        var vUpgrader = DeployChanges.To
            .SqliteDatabase(aConnectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogToConsole()
            .Build();

        return vUpgrader.PerformUpgrade();
    }
}
