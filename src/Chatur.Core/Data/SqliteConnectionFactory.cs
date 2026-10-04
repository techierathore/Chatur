using System.Data;
using Chatur.Core.Platform;
using Microsoft.Data.Sqlite;

namespace Chatur.Core.Data;

/// <summary>
/// Opens connections to the SQLite file named by <see cref="IAppPaths.DatabasePath"/>.
/// </summary>
public sealed class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly IAppPaths objPaths;

    /// <summary>
    /// Creates the factory.
    /// </summary>
    /// <param name="aPaths">Where the database file lives on this machine.</param>
    public SqliteConnectionFactory(IAppPaths aPaths)
    {
        objPaths = aPaths;
    }

    /// <inheritdoc />
    public IDbConnection OpenConnection()
    {
        var vConnection = new SqliteConnection($"Data Source={objPaths.DatabasePath}");
        vConnection.Open();

        // SQLite defaults foreign-key enforcement to off per connection; every table below declares
        // a foreign key expecting it to be enforced.
        using (var vPragma = vConnection.CreateCommand())
        {
            vPragma.CommandText = "PRAGMA foreign_keys = ON;";
            vPragma.ExecuteNonQuery();
        }

        return vConnection;
    }
}
