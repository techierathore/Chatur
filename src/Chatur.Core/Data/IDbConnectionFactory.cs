using System.Data;

namespace Chatur.Core.Data;

/// <summary>
/// Opens connections to Chatur's own SQLite database. The data access layer (Dapper) asks for a
/// connection here rather than building one itself, so where the file lives stays a platform decision.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Opens a connection to the signed-in account's own database (BRD-210): everything a person makes
    /// or opens lives there. With nobody signed in it is the install's database.
    /// </summary>
    /// <returns>An open connection the caller disposes.</returns>
    IDbConnection OpenConnection();

    /// <summary>
    /// Opens a connection to the install's own database, whoever is signed in: the device identifier App
    /// Manager knows this machine by (BRD-4) lives there, not with any account.
    /// </summary>
    /// <returns>An open connection the caller disposes.</returns>
    IDbConnection OpenInstallConnection() => OpenConnection();

    /// <summary>
    /// The App Manager user id of the account <see cref="OpenConnection"/> opens the data of, or
    /// <see langword="null"/> when it opens the install's. Work that goes on in the background captures
    /// it and pins itself with <see cref="AccountScope.Enter"/> (BRD-216).
    /// </summary>
    string? CurrentUserId => null;

    /// <summary>A key that keeps one account's in-memory state apart from another's (<c>"install"</c> with no account).</summary>
    string AccountKey => CurrentUserId ?? "install";
}
