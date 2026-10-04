using System.Data;

namespace Chatur.Core.Data;

/// <summary>
/// Opens connections to Chatur's own SQLite database. Every action implementation that reads or
/// writes data takes this rather than a connection string, so a test can supply a connection to a
/// temporary file (Coding Standards §Testability; Architecture §1 Q3).
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Opens a new, already-open connection to Chatur's database at <see cref="Platform.IAppPaths.DatabasePath"/>.
    /// </summary>
    /// <returns>An open <see cref="IDbConnection"/>. The caller disposes it.</returns>
    IDbConnection OpenConnection();
}
