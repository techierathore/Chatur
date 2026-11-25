using Chatur.Core.Accounts;
using Chatur.Core.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chatur.Core.Feedback;

/// <summary>Registers Feedback and issues: the action and, by default, App Manager as the place entries go.</summary>
public static class FeedbackServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IFeedbackActions"/> and <see cref="IFeedbackSource"/>. The source is App Manager
    /// (<see cref="AppManagerFeedbackSource"/>); a test host registers its own <see cref="IFeedbackSource"/>
    /// after this call, which wins. Called by <c>AddChaturCore</c>.
    /// </summary>
    /// <param name="aServices">The service collection to add to.</param>
    public static IServiceCollection AddChaturFeedback(this IServiceCollection aServices)
    {
        aServices.AddHttpClient<AppManagerFeedbackSource>((aProvider, aClient) =>
        {
            var vConfiguration = aProvider.GetRequiredService<IConfiguration>();
            var vBaseUrl = vConfiguration["AppManager:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(vBaseUrl))
            {
                aClient.BaseAddress = new Uri(vBaseUrl.EndsWith('/') ? vBaseUrl : vBaseUrl + "/");
            }

            // A server that does not answer must not hold Send for HttpClient's default 100 seconds.
            aClient.Timeout = TimeSpan.FromSeconds(20);
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
        aServices.AddScoped<IFeedbackSource>(aProvider => aProvider.GetRequiredService<AppManagerFeedbackSource>());
        aServices.AddScoped<IFeedbackActions, FeedbackActions>();
        return aServices;
    }
}
