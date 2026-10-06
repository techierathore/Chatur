using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;

namespace Chatur.Core.Accounts;

/// <summary>
/// This installation's own device id: generated once, kept in the <c>Installation</c> table and the
/// same for the life of the installation (REQ-FN-002). Device sign-in sends it in its body, and every
/// App Manager call sends it as <c>X-Device-Id</c>, which an installed-app key requires
/// (AppManager API usage guide §2.1.1).
/// </summary>
public static class InstallationId
{
    /// <summary>Reads the installation's device id, creating and storing it on first use.</summary>
    /// <param name="aDb">Opens connections to Chatur's database.</param>
    /// <param name="aClock">Stamps the row when the id is first created.</param>
    /// <param name="aCt">Cancels the read or write.</param>
    public static async Task<string> GetOrCreateAsync(IDbConnectionFactory aDb, IClock aClock, CancellationToken aCt = default)
    {
        using var vConnection = aDb.OpenConnection();

        var vExisting = await vConnection.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition("SELECT DeviceId FROM Installation ORDER BY InstallationId LIMIT 1", cancellationToken: aCt)).ConfigureAwait(false);
        if (vExisting is not null)
        {
            return vExisting;
        }

        var vDeviceId = Guid.NewGuid().ToString("N");
        await vConnection.ExecuteAsync(
            new CommandDefinition(
                "INSERT INTO Installation (DeviceId, CreatedUtc) VALUES (@DeviceId, @CreatedUtc)",
                new { DeviceId = vDeviceId, CreatedUtc = aClock.UtcNow.ToString("O") },
                cancellationToken: aCt)).ConfigureAwait(false);

        return vDeviceId;
    }
}
