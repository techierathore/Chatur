namespace Chatur.Core.Data;

/// <summary>
/// Brings one SQLite file up to the current schema (Architecture §2 "ChaturDb"). The heads implement it
/// over <c>ChaturDbMigrator</c>, which <c>Chatur.Core</c> does not reference; the connection factory calls
/// it the first time an account's own file is opened (Architecture §6, 2026-11-17).
/// </summary>
public interface IDatabaseMigrator
{
    /// <summary>Applies every migration the file has not had yet.</summary>
    /// <param name="aDatabasePath">The SQLite file's full path; it is created when it does not exist.</param>
    /// <returns><see langword="true"/> when every migration ran.</returns>
    bool Migrate(string aDatabasePath);
}

/// <summary>An <see cref="IDatabaseMigrator"/> over a function the head supplies.</summary>
public sealed class DelegateDatabaseMigrator : IDatabaseMigrator
{
    private readonly Func<string, bool> objMigrate;

    /// <summary>Creates the migrator.</summary>
    /// <param name="aMigrate">Migrates the file at the given path and says whether it succeeded.</param>
    public DelegateDatabaseMigrator(Func<string, bool> aMigrate)
    {
        objMigrate = aMigrate;
    }

    /// <inheritdoc />
    public bool Migrate(string aDatabasePath) => objMigrate(aDatabasePath);
}
