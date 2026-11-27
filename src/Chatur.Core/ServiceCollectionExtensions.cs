using Chatur.Core.Accounts;
using Chatur.Core.Actions;
using Chatur.Core.Agent;
using Chatur.Core.BuildRun;
using Chatur.Core.Changes;
using Chatur.Core.Corrections;
using Chatur.Core.Data;
using Chatur.Core.Files;
using Chatur.Core.Guards;
using Chatur.Core.Licences;
using Chatur.Core.Measurements;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        // registration, refresh and sign-out call, against the production App Manager where Chatur
        // is application 1, configured from AppManager:BaseUrl / ApplicationId / ApiKey /
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
        .AddHttpMessageHandler<AppManagerDeviceIdHandler>();

        // Prerequisites (Architecture §7 "Doctor"; REQ-FN-011..013): a typed client for read-only,
        // unauthenticated calls to the public GitHub API, checking the Chatur repository's own
        // releases for a newer nightly — a second, unrelated client from App Manager's above, so
        // neither's base address or headers leak into the other.
        aServices.AddHttpClient<IGitHubReleaseClient, GitHubReleaseClient>((aProvider, aClient) =>
        {
            aClient.BaseAddress = ReleaseFeedAddress.Resolve(aProvider.GetRequiredService<IConfiguration>());
            // A feed that does not answer must not hold Prerequisites for HttpClient's default 100 seconds (verify 2026-11-13).
            aClient.Timeout = TimeSpan.FromSeconds(15);
            aClient.DefaultRequestHeaders.UserAgent.ParseAdd("Chatur-Doctor");
            aClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        });
        aServices.AddSingleton<IExternalCommandRunner, ProcessCommandRunner>();

        aServices.AddScoped<IAccountActions, AccountActions>();
        // Licence (REQ-UI-064 to REQ-UI-066, REQ-FN-075): App Manager's licence by default; the web harness may replace ILicenceSource.
        aServices.AddChaturLicences();
        aServices.AddScoped<IProjectActions, ProjectActions>();
        aServices.AddScoped<IProjectOpenActions, ProjectOpenActions>();
        aServices.AddScoped<IProjectTemplateActions, ProjectTemplateActions>();
        aServices.AddScoped<Agent.Commands.IAgentCommandRunner, Agent.Commands.AgentCommandRunner>();
        aServices.AddScoped<IFileActions, FileActions>();
        aServices.AddScoped<IFileSearchActions, FileSearchActions>();
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
        aServices.AddSingleton<IModelChooserFactory, TechieRagModelChooserFactory>();
        aServices.AddScoped<IAutoRouter, AutoRouter>();
        aServices.AddScoped<IAgentActions, AgentActions>();
        aServices.AddScoped<IChangeActions, ChangeActions>();
        aServices.AddScoped<IRoleActions, RoleActions>();
        // An agent's greeting and the system prompt its model receives, both read from its persona file (REQ-FN-308).
        aServices.AddScoped<IAgentPersonaActions, Roles.AgentPersonaActions>();
        // All projects (REQ-FN-076..078, cluster P4-C): the project list with where each stands, and the roles sync.
        // The sync store is the head's own; a head that names none has one that says App Manager keeps nothing yet.
        aServices.AddScoped<IAllProjectsActions, Projects.AllProjectsActions>();
        aServices.AddScoped<IRoleSyncActions, Roles.RoleSyncActions>();
        aServices.TryAddSingleton<Platform.IRoleSyncStore, Platform.UnavailableRoleSyncStore>();
        aServices.AddScoped<ICorrectionActions, CorrectionActions>();
        aServices.AddScoped<IMeasurementActions, MeasurementActions>();
        // Chatur's own measurements inside Chatur (REQ-UI-115, REQ-FN-313, REQ-FN-314): read from docs/metrics only, no network.
        aServices.AddScoped<IMetricsReportActions, MetricsReportActions>();
        // What is left on each key (REQ-UI-129 to REQ-UI-131, REQ-FN-326): a balance address, else priced calls counted
        // against a typed limit, else "not known". Every model call is recorded by the provider factory through the recorder.
        aServices.AddScoped<IKeyUsageRecorder, KeyUsageRecorder>();
        aServices.AddScoped<IKeyBalanceActions, KeyBalanceActions>();
        aServices.AddHttpClient(KeyBalanceActions.HttpClientName);
        aServices.AddScoped<IProcessActions, Processes.ProcessActions>();
        aServices.AddScoped<ISourceControlActions, SourceControlActions>();
        // Working copies (REQ-FN-079, REQ-FN-080, REQ-UI-068, cluster P4-D): the same class's git worktree half.
        aServices.AddScoped<IWorktreeActions, SourceControlActions>();
        aServices.AddScoped<IThemeActions, ThemeActions>();
        aServices.AddScoped<IRequirementBoardActions, Board.RequirementBoardActions>();
        // Run report (REQ-UI-058..061, cluster E): read-only, from the project's measurement streams.
        aServices.AddScoped<IRunReportActions, RunReports.RunReportActions>();
        // Process run (REQ-FN-052..055, cluster C): the executor does each step's real work; verify-phase
        // builds and tests the user's project and grades from the tests' own results (REQ-UI-048).
        aServices.AddSingleton(new ProcessRuns.ProcessRunOptions());
        aServices.AddSingleton<ProcessRuns.IProjectCommandRunner, ProcessRuns.ProjectCommandRunner>();
        aServices.AddScoped<ProcessRuns.ProjectFolders>();
        aServices.AddScoped<ProcessRuns.RunEvidenceStore>();
        // Step work (REQ-FN-084..087, REQ-FN-090): a work step goes to the agent that owns its process; checks judge the disk.
        aServices.AddScoped<ProcessRuns.ProcessRunBaseline>();
        aServices.AddScoped<IStepAgentActions, StepAgents.StepAgentActions>();
        aServices.AddScoped<ProcessRuns.IProcessStepExecutor, ProcessRuns.ProjectStepExecutor>();
        aServices.AddScoped<ProcessRuns.IRunVerdictWriter, ProcessRuns.RunVerdictWriter>();
        aServices.AddScoped<ProcessRuns.ProcessRunActions>();
        aServices.AddScoped<IProcessRunActions>(aProvider => aProvider.GetRequiredService<ProcessRuns.ProcessRunActions>());
        aServices.AddScoped<IAgentWorkspaceActions, AgentWorkspace.AgentWorkspaceActions>();
        // End-to-end run (REQ-FN-061..066, cluster A): the chain over the same engine, unattended.
        aServices.AddScoped<ProcessRuns.IUnattendedProcessRuns>(aProvider => aProvider.GetRequiredService<ProcessRuns.ProcessRunActions>());
        aServices.AddSingleton(new AutoRun.AutoRunOptions());
        aServices.AddSingleton<AutoRun.IAutoRunDelay, AutoRun.TaskAutoRunDelay>();
        aServices.AddSingleton<AutoRun.IAutoRunLauncher, AutoRun.BackgroundAutoRunLauncher>();
        aServices.AddScoped<AutoRun.AutoRunStore>();
        aServices.AddScoped<AutoRun.OpenRequirementReader>();
        aServices.AddScoped<AutoRun.AutoRunViewBuilder>();
        aServices.AddScoped<IAutoRunActions, AutoRun.AutoRunActions>();
        // Run queue (REQ-FN-067..070, cluster C): end-to-end runs taken one at a time through IAutoRunActions.
        aServices.AddScoped<IRunQueueActions, RunQueue.RunQueueActions>();
        // Estimate before, actual after (BRD-192 to BRD-209): worked out from run records, no model asked.
        aServices.AddScoped<IEstimateActions, Estimates.EstimateActions>();
        // Integrations (REQ-FN-081..083, REQ-UI-069): what Chatur offers to outside tools.
        aServices.AddScoped<IIntegrationActions, Integrations.IntegrationActions>();
        // Compare models (REQ-FN-327 to REQ-FN-330, REQ-UI-132, REQ-UI-133): one task to two to four models side by side.
        aServices.AddScoped<ICompareModelsActions, CompareModels.CompareModelsActions>();
        // Feedback and issues (REQ-UI-062, REQ-FN-073, REQ-UI-063, REQ-FN-074): raised through App Manager; a test host may swap the source.
        Feedback.FeedbackServiceCollectionExtensions.AddChaturFeedback(aServices);

        // GuardPipeline resolves every registered IToolGuard, in this order.
        aServices.AddScoped<IToolGuard, RoleRightsGuard>();
        aServices.AddScoped<IToolGuard, AskMeFirstGuard>();
        aServices.AddScoped<IToolGuard, SourceControlRefusalGuard>();
        aServices.AddScoped<GuardPipeline>();

        return aServices;
    }
}
