using Chatur.Core;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Chatur.WebHarness;
using Chatur.WebHarness.Components;
using Chatur.WebHarness.Services;
using ChaturDb;
using ChaturUI;
using ChaturUI.Services;
using Dapper;
using Microsoft.AspNetCore.StaticFiles;
using Serilog;
using TrBlazeUI.Components.Toast;
using TrBlazeUI.Primitives.Extensions;

// Serilog wired before anything else can fail (Coding Standards §Logging, REQ-NFR-002).
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting Chatur.WebHarness...");

try
{
    var vBuilder = WebApplication.CreateBuilder(args);

    vBuilder.Host.UseSerilog((aContext, aServices, aConfiguration) =>
    {
        var vHarnessPaths = new HarnessAppPaths();
        aConfiguration
            .ReadFrom.Configuration(aContext.Configuration)
            .ReadFrom.Services(aServices)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(vHarnessPaths.LogFolder, "harness-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14);
    });

    vBuilder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // TrBlazeUI setup (.trblazeui/TrBlazeUI-AI-Reference.md §1 "Program.cs Service Registration").
    vBuilder.Services.AddTrBlazeUIPrimitives();
    vBuilder.Services.AddScoped<ToastService>();

    // The action layer, then this harness's own platform services — never the reverse, so a
    // cluster reading this file sees the whole shape in one place.
    vBuilder.Services.AddChaturCore();
    vBuilder.Services.AddChaturUI();
    vBuilder.Services.AddSingleton<IAppPaths, HarnessAppPaths>();
    vBuilder.Services.AddSingleton<IClock, SystemClock>();
    vBuilder.Services.AddSingleton<ISecretStore, HarnessSecretStore>();
    vBuilder.Services.AddSingleton<IFolderPicker, HarnessFolderPicker>();
    vBuilder.Services.AddSingleton<IProcessLauncher, HarnessProcessLauncher>();
    vBuilder.Services.AddScoped<ISecondaryWindow, HarnessSecondaryWindow>();
    vBuilder.Services.AddScoped<IAppShell, HarnessAppShell>();

    vBuilder.Services.AddHealthChecks();

    var vApp = vBuilder.Build();

    // Migrations run at startup against this harness's own database, under its own bin output —
    // never the real machine's application-data folder (Architecture §2 "Chatur.WebHarness").
    var vPaths = vApp.Services.GetRequiredService<IAppPaths>();
    var vMigrationResult = ChaturDbMigrator.Migrate($"Data Source={vPaths.DatabasePath}");
    if (!vMigrationResult.Successful)
    {
        Log.Fatal(vMigrationResult.Error, "Chatur.WebHarness database migration failed");
    }

    // The web harness is driven headless for verification; a freshly started copy has no project
    // selected, so Workbench and every project-scoped screen would render empty. Ensure a fixture
    // project is registered and selected. The fixture lives under tests/.artifacts/, never in the
    // real source tree, and carries a maccatalyst TFM so the Project menu discovers the
    // "Mac Catalyst · Debug" target on macOS (REQ-FN-006, REQ-FN-007, REQ-UI-011).
    await EnsureDefaultProjectAsync(vApp.Services);

    // Routing and several acceptance tests need at least one connected provider with models in the
    // harness database. The harness never invents a real subscription, but a connected row with
    // sample models is enough for the UI to render the mockup's tier chains.
    await EnsureDefaultProviderAsync(vApp.Services);

    vApp.MapHealthChecks("/healthz");

    if (!vApp.Environment.IsDevelopment())
    {
        vApp.UseExceptionHandler("/Error", createScopeForErrors: true);
        vApp.UseHsts();
    }

    vApp.UseStaticFiles();
    vApp.UsePrecompressedStaticFiles();
    vApp.UseAntiforgery();

    vApp.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .AddAdditionalAssemblies(typeof(ChaturRoutes).Assembly);

    Log.Information("Chatur.WebHarness started ({Environment})", vApp.Environment.EnvironmentName);
    vApp.Run();
}
catch (Exception vException)
{
    Log.Fatal(vException, "Chatur.WebHarness terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static async Task EnsureDefaultProjectAsync(IServiceProvider aServices)
{
    try
    {
        await using var vScope = aServices.CreateAsyncScope();
        var vProjectActions = vScope.ServiceProvider.GetRequiredService<IProjectActions>();
        var vSelected = await vProjectActions.SelectedProjectAsync().ConfigureAwait(false);
        if (vSelected is not null)
        {
            return;
        }

        var vRepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var vFixtureFolder = Path.Combine(vRepoRoot, "tests", ".artifacts", "verify", "fixtures", "web-harness-default");
        var vFixtureProject = Path.Combine(vFixtureFolder, "DefaultApp");
        Directory.CreateDirectory(vFixtureProject);

        // The Workbench mockup shows three run targets: Mac Catalyst, Web harness and Tests. A single
        // folder with one multi-target MAUI project plus a web project and a test project produces
        // exactly those labels when BuildRunActions discovers targets.
        var vMauiCsproj = Path.Combine(vFixtureProject, "DefaultApp.csproj");
        if (!File.Exists(vMauiCsproj))
        {
            await File.WriteAllTextAsync(
                vMauiCsproj,
                "<Project Sdk=\"Microsoft.NET.Sdk\">" +
                "<PropertyGroup>" +
                "<TargetFrameworks>net10.0-maccatalyst</TargetFrameworks>" +
                "<UseMaui>true</UseMaui>" +
                "</PropertyGroup>" +
                "</Project>").ConfigureAwait(false);
        }

        var vWebCsproj = Path.Combine(vFixtureProject, "Web harness.csproj");
        if (!File.Exists(vWebCsproj))
        {
            await File.WriteAllTextAsync(
                vWebCsproj,
                "<Project Sdk=\"Microsoft.NET.Sdk.Web\">" +
                "<PropertyGroup>" +
                "<TargetFramework>net10.0</TargetFramework>" +
                "</PropertyGroup>" +
                "</Project>").ConfigureAwait(false);
        }

        var vTestCsproj = Path.Combine(vFixtureProject, "Tests.csproj");
        if (!File.Exists(vTestCsproj))
        {
            await File.WriteAllTextAsync(
                vTestCsproj,
                "<Project Sdk=\"Microsoft.NET.Sdk\">" +
                "<PropertyGroup>" +
                "<TargetFramework>net10.0</TargetFramework>" +
                "<IsPackable>false</IsPackable>" +
                "</PropertyGroup>" +
                "<ItemGroup>" +
                "<PackageReference Include=\"Microsoft.NET.Test.Sdk\" Version=\"17.14.0\" />" +
                "</ItemGroup>" +
                "</Project>").ConfigureAwait(false);
        }

        await vProjectActions.AddFolderAsync(vFixtureFolder).ConfigureAwait(false);
        var vProjects = await vProjectActions.ScanAsync().ConfigureAwait(false);
        var vDefault = vProjects.FirstOrDefault(aProject => aProject.Path == vFixtureProject);
        if (vDefault is null)
        {
            Log.Warning("Web harness default fixture project was not found after scanning {Folder}", vFixtureFolder);
            return;
        }

        await vProjectActions.SelectProjectAsync(vDefault.ProjectId).ConfigureAwait(false);

        // If an earlier harness run discovered targets before the fixture files were complete, clear
        // them so the next menu open rediscovers whatever is on disk now.
        var vConnectionFactory = vScope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        using (var vDb = vConnectionFactory.OpenConnection())
        {
            vDb.Execute("DELETE FROM RunTarget WHERE ProjectId = @ProjectId;", new { ProjectId = vDefault.ProjectId });
        }

        Log.Information("Web harness selected default fixture project {ProjectId} at {Path}", vDefault.ProjectId, vDefault.Path);
    }
    catch (Exception vException)
    {
        Log.Warning(vException, "Could not ensure a default fixture project for the web harness");
    }
}

static async Task EnsureDefaultProviderAsync(IServiceProvider aServices)
{
    try
    {
        await using var vScope = aServices.CreateAsyncScope();
        var vConnection = vScope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        using var vDb = vConnection.OpenConnection();

        var vHasConnected = vDb.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM Provider WHERE State = 'Connected';") > 0;
        if (vHasConnected)
        {
            return;
        }

        const string vProviderName = "OpenCode Go";
        const string vConnector = "OpenAICompatible";
        const string vBaseUrl = "https://opencode.ai/zen/go/v1";
        const string vSecretName = "Chatur:TestProvider:Key";

        var vProviderId = vDb.ExecuteScalar<int>(
            "INSERT INTO Provider (Name, Connector, SignInMethod, BaseUrl, SecretName, State) " +
            "VALUES (@Name, @Connector, 'key', @BaseUrl, @SecretName, 'Connected'); " +
            "SELECT last_insert_rowid();",
            new { Name = vProviderName, Connector = vConnector, BaseUrl = vBaseUrl, SecretName = vSecretName });

        var vModels = new[] { "minimax-m3", "kimi-k2.7-code", "kimi-k2.5-code" };
        for (var vIndex = 0; vIndex < vModels.Length; vIndex++)
        {
            // Tier 2 is the shipped "everyday work" tier; Routing's reset-tiers puts these in Tier 2.
            vDb.Execute(
                "INSERT INTO Model (ProviderId, Tier, Identifier) VALUES (@ProviderId, 2, @Identifier);",
                new { ProviderId = vProviderId, Identifier = vModels[vIndex] });
        }

        var vSecretStore = vScope.ServiceProvider.GetRequiredService<ISecretStore>();
        await vSecretStore.SaveAsync(vSecretName, "oc_sk_a75a9826cc0e_bPnUmaAdxt2ZdkZnveNQe4aZUGHsEQf9").ConfigureAwait(false);

        Log.Information("Web harness seeded connected provider {ProviderId} with {ModelCount} model(s)", vProviderId, vModels.Length);
    }
    catch (Exception vException)
    {
        Log.Warning(vException, "Could not ensure a default provider for the web harness");
    }
}
