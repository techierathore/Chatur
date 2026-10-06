using Chatur.Core.Data;
using Chatur.Core.Platform;

namespace Chatur.Core.Accounts;

/// <summary>
/// Adds this installation's device id as <c>X-Device-Id</c> to every App Manager call. A downloaded
/// Chatur uses an installed-app key (<c>pk_live_…</c>, no secret), and App Manager refuses such a call
/// without the header (<c>400 DEVICE_ID_REQUIRED</c>) and rate-limits per device
/// (AppManager API usage guide §2.1.1; decision 2026-10-06).
/// </summary>
public sealed class AppManagerDeviceIdHandler : DelegatingHandler
{
    /// <summary>The header App Manager reads the installation's id from.</summary>
    public const string HeaderName = "X-Device-Id";

    private readonly IDbConnectionFactory objDb;
    private readonly IClock objClock;

    /// <summary>Creates the handler over Chatur's database, where the installation's id is kept.</summary>
    public AppManagerDeviceIdHandler(IDbConnectionFactory aDb, IClock aClock)
    {
        objDb = aDb;
        objClock = aClock;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage aRequest, CancellationToken aCt)
    {
        if (!aRequest.Headers.Contains(HeaderName))
        {
            var vDeviceId = await InstallationId.GetOrCreateAsync(objDb, objClock, aCt).ConfigureAwait(false);
            aRequest.Headers.Add(HeaderName, vDeviceId);
        }

        return await base.SendAsync(aRequest, aCt).ConfigureAwait(false);
    }
}
