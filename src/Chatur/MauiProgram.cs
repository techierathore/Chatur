using System.Reflection;
using Chatur.Core;
using Chatur.Core.Platform;
using Chatur.Platform;
using Chatur.Services;
using ChaturDb;
using ChaturUI;
using ChaturUI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using TrBlazeUI.Components.Toast;
using TrBlazeUI.Primitives.Extensions;

namespace Chatur;

/// <summary>
/// Builds the desktop app: Serilog first, then the same action layer every head registers, then
/// this head's own platform services and windows (Coding Standards §Logging, REQ-NFR-002).
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Creates and configures the <see cref="MauiApp"/>.
    /// </summary>
    /// <returns>The built app, ready for the platform launcher.</returns>
    public static MauiApp CreateMauiApp()
    {
        ConfigureLogging();

        try
        {
            var vBuilder = MauiApp.CreateBuilder();
            vBuilder.UseMauiApp<App>();

            vBuilder.Logging.ClearProviders();
            vBuilder.Logging.AddSerilog(dispose: false);
#if DEBUG
            vBuilder.Logging.AddDebug();
#endif

            AddEmbeddedConfiguration(vBuilder.Configuration);

            vBuilder.Services.AddMauiBlazorWebView();
#if DEBUG
            vBuilder.Services.AddBlazorWebViewDeveloperTools();
#endif

            RegisterServices(vBuilder.Services);

            var vApp = vBuilder.Build();

            var vPaths = vApp.Services.GetRequiredService<IAppPaths>();
            var vMigrationResult = ChaturDbMigrator.Migrate($"Data Source={vPaths.DatabasePath}");
            if (!vMigrationResult.Successful)
            {
                Log.Fatal(vMigrationResult.Error, "Chatur database migration failed");
            }

            Log.Information("Chatur composed and migrated");
            return vApp;
        }
        catch (Exception vException)
        {
            Log.Fatal(vException, "Chatur failed to start");
            Log.CloseAndFlush();
            throw;
        }
    }

    /// <summary>
    /// Registers the action layer, this head's own platform services, and the windows.
    /// </summary>
    /// <param name="aServices">The service collection to add to.</param>
    private static void RegisterServices(IServiceCollection aServices)
    {
        // TrBlazeUI setup (.trblazeui/TrBlazeUI-AI-Reference.md §1 "Program.cs Service Registration").
        aServices.AddTrBlazeUIPrimitives();
        aServices.AddScoped<ToastService>();

        aServices.AddChaturCore();
        aServices.AddChaturUI();

        aServices.AddSingleton<IAppPaths, MauiAppPaths>();
        aServices.AddSingleton<IClock, SystemClock>();
        aServices.AddSingleton<ISecretStore, MauiSecretStore>();
        aServices.AddSingleton<IFolderPicker, MauiFolderPicker>();
        aServices.AddSingleton<IProcessLauncher, MauiProcessLauncher>();

        // The window pages take no constructor dependencies yet, so WindowService creates them
        // directly rather than resolving them here; nothing to register for them today.
        aServices.AddSingleton<IWindowService, WindowService>();
    }

    /// <summary>
    /// Writes a daily rolling log file under the application-data folder, wired before anything
    /// else can fail (Coding Standards §Logging, REQ-NFR-002).
    /// </summary>
    private static void ConfigureLogging()
    {
        var vLogFolder = Path.Combine(FileSystem.AppDataDirectory, "logs");
        Directory.CreateDirectory(vLogFolder);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(vLogFolder, "chatur-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true)
            .CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += (_, aArgs) =>
        {
            Log.Fatal(aArgs.ExceptionObject as Exception, "Unhandled exception (terminating: {IsTerminating})", aArgs.IsTerminating);
            if (aArgs.IsTerminating)
            {
                Log.CloseAndFlush();
            }
        };

        TaskScheduler.UnobservedTaskException += (_, aArgs) =>
        {
            Log.Error(aArgs.Exception, "Unobserved task exception");
            aArgs.SetObserved();
        };
    }

    /// <summary>
    /// Adds the embedded <c>appsettings.json</c> — non-secret configuration only (Architecture §1 Q2).
    /// </summary>
    /// <param name="aConfiguration">The configuration builder.</param>
    private static void AddEmbeddedConfiguration(IConfigurationBuilder aConfiguration)
    {
        using var vStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Chatur.appsettings.json");
        if (vStream is null)
        {
            Log.Warning("Embedded appsettings.json was not found; continuing with built-in defaults");
            return;
        }

        aConfiguration.AddJsonStream(vStream);

#if DEBUG
        // Local development only (REQ-FN-001/002/003, REQ-FN-014/015/017 all read AppManager:*/
        // provider keys from here): `MauiApp.CreateBuilder`, unlike `WebApplication.CreateBuilder`,
        // never loads user secrets on its own, so without this call every AppManager:* value stayed
        // empty in the real Windows/Mac Catalyst head even though `dotnet user-secrets` had written
        // them — sign-in always failed with "Could not reach App Manager", the WebHarness's own
        // config path (ASP.NET Core's Development-only auto-load) never having exercised this gap.
        // Found booting the real Windows head for REQ-NFR-004 (2026-09-23); guarded to DEBUG so a
        // Release build never reads or ships a secrets file.
        aConfiguration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);
#endif
    }
}
