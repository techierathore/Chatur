using Chatur.Core.Accounts;
using Chatur.Core.Actions;
using Chatur.Core.Agent;
using Chatur.Core.BuildRun;
using Chatur.Core.Changes;
using Chatur.Core.Corrections;
using Chatur.Core.Data;
using Chatur.Core.Files;
using Chatur.Core.Guards;
using Chatur.Core.Measurements;
using Chatur.Core.Models;
using Chatur.Core.Prerequisites;
using Chatur.Core.Projects;
using Chatur.Core.Roles;
using Chatur.Core.Routing;
using Chatur.Core.SourceControl;
using Chatur.Core.Themes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;

namespace Chatur.Core;

/// <summary>
/// Registers every module of the action layer, the guard pipeline and the data-access helper. Every
/// head — <c>Chatur</c> and <c>Chatur.WebHarness</c> — calls this once, then registers only its own
/// <c>Chatur.Core.Platform</c> implementations on top. No cluster ever edits this file: a new action
/// method on an existing interface needs no registration change, and a whole new module is added
/// here by the foundation only.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds every <c>Chatur.Core</c> service: the action layer, the guard pipeline and
    /// <see cref="IDbConnectionFactory"/>. Does not register a <c>Chatur.Core.Platform</c> port —
    /// each head supplies its own.
    /// </summary>
    /// <param name="aServices">The service collection to add to.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddChaturCore(this IServiceCollection aServices)
    {
        aServices.AddSingleton<AppState>();
        aServices.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();

        // App Manager (Architecture §1 Q4, §5 "Identity"): one HttpClient for every sign-in,
        // registration, refresh and sign-out call, configured from AppManager:BaseUrl / ApiKey /
        // ApiSecret (docs/AppManager-api-usage-guide.md §1 "Option A: API Key Headers"). Cluster C's
        // Register reuses this same client rather than opening a second one. A downloaded Chatur
        // carries an installed-app key with no secret (§2.1.1), so every call also sends this
        // installation's X-Device-Id (AppManagerDeviceIdHandler).
        aServices.AddTransient<AppManagerDeviceIdHandler>();
        aServices.AddHttpClient<IAppManagerClient, AppManagerClient>((aProvider, aClient) =>
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
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            // The local development App Manager (docs/Chatur-UsageGuide.md "Test users") serves
            // HTTPS on localhost with a self-signed certificate; every other host is validated
            // normally — this never relaxes trust for a real deployment.
            ServerCertificateCustomValidationCallback = (aRequest, aCertificate, aChain, aErrors) =>
                aErrors == System.Net.Security.SslPolicyErrors.None || IsLocalDevelopmentHost(aRequest?.RequestUri)
        })
        .AddHttpMessageHandler<AppManagerDeviceIdHandler>();

        // Prerequisites (Architecture §7 "Doctor"; REQ-FN-011..013): a typed client for read-only,
        // unauthenticated calls to the public GitHub API, checking the Chatur repository's own
        // releases for a newer nightly — a second, unrelated client from App Manager's above, so
        // neither's base address or headers leak into the other.
        aServices.AddHttpClient<IGitHubReleaseClient, GitHubReleaseClient>((aProvider, aClient) =>
        {
            aClient.BaseAddress = ReleaseFeedAddress.Resolve(aProvider.GetRequiredService<IConfiguration>());
            aClient.DefaultRequestHeaders.UserAgent.ParseAdd("Chatur-Doctor");
            aClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        });
        aServices.AddSingleton<IExternalCommandRunner, ProcessCommandRunner>();

        aServices.AddScoped<IAccountActions, AccountActions>();
        aServices.AddScoped<IProjectActions, ProjectActions>();
        aServices.AddScoped<IFileActions, FileActions>();
        aServices.AddScoped<IBuildRunActions, BuildRunActions>();
        aServices.AddScoped<IPrerequisiteActions, PrerequisiteActions>();
        aServices.AddScoped<ILlmConnectionProbe, TechieRagConnectionProbe>();
        // A subscription sign-in's session lives in the operating system's secret store (REQ-FN-016).
        aServices.AddScoped<TechieRag.Abstractions.ISubscriptionSessionStore, SecretSubscriptionSessionStore>();
        aServices.AddScoped<ISubscriptionSignIn, TechieRagSubscriptionSignIn>();
        aServices.AddScoped<IProviderActions, ProviderActions>();
        aServices.AddScoped<IRoutingActions, RoutingActions>();
        // Shared across scopes so a stop request (one scope) can cancel a send in flight in another —
        // see Agent.AgentSessionRegistry's own doc comment (REQ-UI-023).
        aServices.AddSingleton<Agent.AgentSessionRegistry>();
        aServices.AddScoped<Agent.IAgentLlmProviderFactory, Agent.TechieRagLlmProviderFactory>();
        aServices.AddScoped<IAgentActions, AgentActions>();
        aServices.AddScoped<IChangeActions, ChangeActions>();
        aServices.AddScoped<IRoleActions, RoleActions>();
        aServices.AddScoped<ICorrectionActions, CorrectionActions>();
        aServices.AddScoped<IMeasurementActions, MeasurementActions>();
        aServices.AddScoped<ISourceControlActions, SourceControlActions>();
        aServices.AddScoped<IThemeActions, ThemeActions>();

        // GuardPipeline resolves every registered IToolGuard, in this order.
        aServices.AddScoped<IToolGuard, RoleRightsGuard>();
        aServices.AddScoped<IToolGuard, AskMeFirstGuard>();
        aServices.AddScoped<IToolGuard, SourceControlRefusalGuard>();
        aServices.AddScoped<GuardPipeline>();

        return aServices;
    }

    /// <summary>
    /// Whether a request is going to the local development App Manager rather than a real deployment
    /// (docs/Chatur-UsageGuide.md "Test users" — <c>https://localhost:32769/</c>).
    /// </summary>
    /// <param name="aUri">The request's target address.</param>
    private static bool IsLocalDevelopmentHost(Uri? aUri) =>
        aUri is not null && (aUri.Host == "localhost" || aUri.Host == "127.0.0.1");
}
