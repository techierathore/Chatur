using Chatur.Core;
using Chatur.Core.Platform;
using Chatur.WebHarness.Components;
using Chatur.WebHarness.Services;
using ChaturDb;
using ChaturUI;
using ChaturUI.Services;
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

    vApp.MapHealthChecks("/healthz");

    if (!vApp.Environment.IsDevelopment())
    {
        vApp.UseExceptionHandler("/Error", createScopeForErrors: true);
        vApp.UseHsts();
    }

    vApp.UseStaticFiles();
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
