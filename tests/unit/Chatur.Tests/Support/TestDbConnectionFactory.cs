using System.Data;
using Chatur.Core.Data;
using Microsoft.Data.Sqlite;

namespace Chatur.Tests.Support;

/// <summary>
/// Opens connections to one temporary SQLite file, exactly as <c>SqliteConnectionFactory</c> does for
/// the real heads (Chatur.Core.Data.SqliteConnectionFactory) — a test supplies a connection string to
/// a temporary file instead of reading <c>IAppPaths.DatabasePath</c> (Coding Standards §Testability).
/// </summary>
public sealed class TestDbConnectionFactory : IDbConnectionFactory
{
    private readonly string objConnectionString;

    /// <summary>Creates the factory over an already-migrated database file.</summary>
    /// <param name="aConnectionString">A SQLite connection string, e.g. <c>Data Source=/tmp/x.db</c>.</param>
    public TestDbConnectionFactory(string aConnectionString)
    {
        objConnectionString = aConnectionString;
    }

    /// <inheritdoc />
    public IDbConnection OpenConnection()
    {
        var vConnection = new SqliteConnection(objConnectionString);
        vConnection.Open();

        using var vPragma = vConnection.CreateCommand();
        vPragma.CommandText = "PRAGMA foreign_keys = ON;";
        vPragma.ExecuteNonQuery();

        return vConnection;
    }
}
