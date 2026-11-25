using Chatur.Core.Accounts;
using Chatur.Core.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chatur.Core.Licences;

/// <summary>Registers the licence module: the real App Manager source by default (a head may replace <see cref="ILicenceSource"/>).</summary>
public static class LicenceServiceCollectionExtensions
{
    /// <summary>Adds <see cref="ILicenceActions"/>, the App Manager licence client and <see cref="AppManagerLicenceSource"/>.</summary>
    /// <param name="aServices">The service collection to add to.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddChaturLicences(this IServiceCollection aServices)
    {
        aServices.AddHttpClient<IAppManagerLicenceClient, AppManagerLicenceClient>((aProvider, aClient) =>
        {
            var vConfiguration = aProvider.GetRequiredService<IConfiguration>();
            var vBaseUrl = vConfiguration["AppManager:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(vBaseUrl))
            {
                aClient.BaseAddress = new Uri(vBaseUrl.EndsWith('/') ? vBaseUrl : vBaseUrl + "/");
            }

            var vApiKey = vConfiguration["AppManager:ApiKey"];
            if (!string.IsNullOrWhiteSpace(vApiKey))
            {
                aClient.DefaultRequestHeaders.Add("X-Api-Key", vApiKey);
            }

            var vApiSecret = vConfiguration["AppManager:ApiSecret"];
            if (!string.IsNullOrWhiteSpace(vApiSecret))
            {
                aClient.DefaultRequestHeaders.Add("X-Api-Secret", vApiSecret);
            }
        })
        .AddHttpMessageHandler<AppManagerDeviceIdHandler>();
        aServices.AddScoped<ILicenceSource, AppManagerLicenceSource>();
        aServices.AddScoped<ILicenceActions, LicenceActions>();
        return aServices;
    }
}
