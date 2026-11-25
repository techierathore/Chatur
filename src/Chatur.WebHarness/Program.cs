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

    // A page that goes away without saying goodbye (a test closing its browser) is let go at once, not kept
    // for three minutes in case it comes back. AppState is one object for the whole harness, so every page
    // kept this way went on answering its Changed event: after a test run, one project change made dozens
    // of closed pages re-read their files, run targets, branch and conversation together, and the page that
    // was really open stood with an empty work area for 3.7 to 6.9 s (REQ-UI-055, 2026-11-15;
    // tests/verify/deep-link-after-closed-pages.spec.ts). The MAUI head has no such pages: a window that
    // closes takes its components with it.
    vBuilder.Services.AddRazorComponents()
        .AddInteractiveServerComponents(aOptions => aOptions.DisconnectedCircuitMaxRetained = 0);

    // TrBlazeUI setup (.trblazeui/TrBlazeUI-AI-Reference.md §1 "Program.cs Service Registration").
    vBuilder.Services.AddTrBlazeUIPrimitives();
    vBuilder.Services.AddScoped<ToastService>();

    // The action layer, then this harness's own platform services — never the reverse, so a
    // cluster reading this file sees the whole shape in one place.
    vBuilder.Services.AddChaturCore();
    vBuilder.Services.AddChaturUI();
    vBuilder.Services.AddSingleton<IAppPaths, HarnessAppPaths>();
    // Each account's own database file is migrated the first time that account signs in (Architecture §6, 2026-11-17).
    vBuilder.Services.AddSingleton<IDatabaseMigrator>(new DelegateDatabaseMigrator(aPath => ChaturDbMigrator.Migrate($"Data Source={aPath}").Successful));
    vBuilder.Services.AddSingleton<IClock, SystemClock>();
    vBuilder.Services.AddSingleton<ISecretStore, HarnessSecretStore>();
    vBuilder.Services.AddSingleton<IFolderPicker, HarnessFolderPicker>();
    vBuilder.Services.AddSingleton<IProcessLauncher, HarnessProcessLauncher>();
    vBuilder.Services.AddScoped<ISecondaryWindow, HarnessSecondaryWindow>();
    vBuilder.Services.AddScoped<IAppShell, HarnessAppShell>();

    // The test licence: when tests/verify/test-settings.json has an enabled testLicence block, the licence is held
    // in memory here and never asked of the shared App Manager account. Without the block the real App Manager
    // source stays in place, exactly as in the app.
    var vTestLicence = HarnessTestLicence.Load(vBuilder.Configuration, new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() });
    if (vTestLicence is not null)
    {
        vBuilder.Services.AddSingleton(aProvider => new Chatur.Core.Licences.InMemoryLicenceSource(vTestLicence, aProvider.GetRequiredService<IClock>()));
        vBuilder.Services.AddScoped<Chatur.Core.Licences.ILicenceSource>(aProvider => aProvider.GetRequiredService<Chatur.Core.Licences.InMemoryLicenceSource>());
    }

    // Feedback and issues (REQ-UI-062..063, REQ-FN-073..074): this test-only host can swap App Manager for an in-memory
    // double, switched on by Feedback:Source in its own appsettings.json — never by an environment variable. The
    // real app has no such key and keeps App Manager.
    var vFeedbackDouble = string.Equals(vBuilder.Configuration["Feedback:Source"], "TestDouble", StringComparison.OrdinalIgnoreCase)
        ? new FeedbackTestDouble()
        : null;
    if (vFeedbackDouble is not null)
    {
        vBuilder.Services.AddSingleton(vFeedbackDouble);
        vBuilder.Services.AddSingleton<Chatur.Core.Feedback.IFeedbackSource>(vFeedbackDouble);
    }

    // Roles sync (REQ-FN-078): this test-only host can stand a file in for App Manager's store, named by
    // RoleSync:StoreFile in its own appsettings.json (relative to the harness data folder) — never by an
    // environment variable. The real app has no such key and keeps the store that says App Manager has none yet.
    var vRoleSyncFile = vBuilder.Configuration["RoleSync:StoreFile"];
    HarnessRoleSyncStore? vRoleSyncStore = null;
    if (!string.IsNullOrWhiteSpace(vRoleSyncFile))
    {
        vRoleSyncStore = new HarnessRoleSyncStore(Path.Combine(new HarnessAppPaths().AppDataFolder, vRoleSyncFile));
        vBuilder.Services.AddSingleton(vRoleSyncStore);
        vBuilder.Services.AddSingleton<IRoleSyncStore>(vRoleSyncStore);
    }

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

    // The web harness is driven headless for verification. Everything a person makes or opens lives in
    // the signed-in account's own database (BRD-210), so the fixture project the acceptance tests work
    // in, and the one connected provider routing needs, are put into the OWNER test account's database
    // the first time it signs in in this process — never into the install's file, and never into any
    // other account's: a second account starts empty, exactly as it does in the app. The fixture lives
    // under tests/.artifacts/, never in the real source tree, and carries a maccatalyst TFM so the
    // Project menu discovers the "Mac Catalyst · Debug" target on macOS (REQ-FN-006, REQ-FN-007,
    // REQ-UI-011). Which provider is the owner's choice, made in the testProvider block of
    // tests/verify/test-settings.json; the key falls back to Chatur:TestProvider:Key in configuration
    // (the project's user secrets in Development) when the file leaves it empty.
    var vTestProvider = HarnessTestProvider.Load(
        vBuilder.Configuration,
        new[] { AppContext.BaseDirectory, vApp.Environment.ContentRootPath },
        out var vTestProviderReason);
    var vOwnerEmail = HarnessTestProvider.OwnerEmail(vBuilder.Configuration, new[] { AppContext.BaseDirectory, vApp.Environment.ContentRootPath });
    AttachAccountSeeding(vApp.Services, vPaths, vOwnerEmail, vTestProvider, vTestProviderReason);

    vApp.MapHealthChecks("/healthz");
    vApp.MapIntegrationEndpoints();

    if (vApp.Environment.IsDevelopment())
    {
        // Test only, Development only (REQ-FN-043): every record the app writes is built by its own code from
        // known fields, so no screen can offer the measurement writer an unknown field. This hands the real
        // writer, as the app wires it, one record carrying a field the schema does not know, for the selected
        // project, so a browser check can see the refusal on Settings ▸ Measurements and the stream file stay
        // as it was. The MAUI head has no such route.
        vApp.MapPost("/test/measurements/unknown-field", async (IProjectActions aProjects, IMeasurementActions aMeasurements) =>
        {
            var vProject = await aProjects.SelectedProjectAsync().ConfigureAwait(false);
            if (vProject is null)
            {
                return Results.Conflict("No project is selected.");
            }

            await aMeasurements.AppendAsync(
                vProject.ProjectId,
                "runs",
                new Dictionary<string, string?> { ["kind"] = "run", ["not_in_the_schema"] = "x" }).ConfigureAwait(false);
            return Results.Ok(vProject.Name);
        });
    }

    if (vApp.Environment.IsDevelopment() && vFeedbackDouble is not null)
    {
        // Test only (REQ-FN-074, REQ-UI-063): the pretend App Manager's network, its answers and what it holds.
        vApp.MapPost("/test/feedback/network", (bool down) =>
        {
            vFeedbackDouble.NetworkDown = down;
            return Results.Ok(down);
        });
        vApp.MapPost("/test/feedback/answer", (string title, string answer, bool finished) =>
            vFeedbackDouble.Answer(title, answer, finished) ? Results.Ok() : Results.NotFound());
        vApp.MapGet("/test/feedback/issues", () => Results.Ok(vFeedbackDouble.Issues()));
        vApp.MapPost("/test/feedback/reset", () =>
        {
            vFeedbackDouble.Reset();
            return Results.Ok();
        });
    }

    if (vApp.Environment.IsDevelopment() && vRoleSyncStore is not null)
    {
        // Test only (REQ-FN-078): the pretend App Manager's store, and a second machine under the same account.
        vApp.MapGet("/test/role-sync", () => Results.Ok(vRoleSyncStore.Snapshot()));
        vApp.MapPost("/test/role-sync/peer", (string? addRule, string? removeRule) =>
            vRoleSyncStore.PeerChange("TR-WIN-DESK", addRule, removeRule) ? Results.Ok() : Results.Conflict("Nothing has synced yet."));
        vApp.MapDelete("/test/role-sync", () =>
        {
            vRoleSyncStore.Clear();
            return Results.Ok();
        });
    }

    if (vApp.Environment.IsDevelopment() && vTestLicence is not null)
    {
        // Test only, Development only, and only with a test licence (REQ-UI-066): puts the test licence back as
        // configured, and says whether a machine could still sign in, so a browser check can see that a forgotten
        // machine's sign-in no longer works. The MAUI head has no such route.
        vApp.MapPost("/test/licence/reset", (Chatur.Core.Licences.InMemoryLicenceSource aSource, AppState aState) =>
        {
            aSource.Reset();
            // The shared state held the switches the test left; it is put back with the licence.
            if (aState.CurrentUser is { } vUser)
            {
                aState.SetOffFeatures(vUser.UserId, vTestLicence.FeaturesOff);
            }

            return Results.Ok();
        });
        vApp.MapGet("/test/licence/sign-in/{aDeviceId}", (string aDeviceId, Chatur.Core.Licences.InMemoryLicenceSource aSource) =>
            aSource.CanSignIn(aDeviceId) ? Results.Ok() : Results.Unauthorized());
    }

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

// Seeds the owner test account's own database at its first sign-in in this process, and keeps
// harness-data/accounts/current.txt naming the folder of whoever signed in last, so the test seeds that
// write the database file directly (tests/verify/support/db.ts, tests/verify/seed/*.sh) find the
// signed-in account's file. Seeding runs on its own task and is waited for, so Start never reads the
// account's database half-seeded.
static void AttachAccountSeeding(IServiceProvider aServices, IAppPaths aPaths, string? aOwnerEmail, HarnessTestProvider? aProvider, string aProviderReason)
{
    var vAppState = aServices.GetRequiredService<AppState>();
    var vSeeded = new System.Collections.Concurrent.ConcurrentDictionary<string, bool>(StringComparer.Ordinal);
    string? vLastPointer = null;
    vAppState.Changed += () =>
    {
        var vUser = vAppState.CurrentUser;
        if (vUser is null)
        {
            return;
        }

        var vFolder = SqliteConnectionFactory.AccountFolderName(vUser.UserId);
        if (vFolder != vLastPointer)
        {
            var vAccounts = Path.Combine(aPaths.AppDataFolder, SqliteConnectionFactory.AccountsFolderName);
            Directory.CreateDirectory(vAccounts);
            File.WriteAllText(Path.Combine(vAccounts, "current.txt"), vFolder);
            vLastPointer = vFolder;
        }

        if (!vSeeded.TryAdd(vUser.UserId, true)
            || aOwnerEmail is null
            || !string.Equals(vUser.Email, aOwnerEmail, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Task.Run(async () =>
        {
            using var vPin = AccountScope.Enter(vUser.UserId);
            // The provider first: selecting the fixture project raises AppState.Changed, and the screens
            // that answer it read routing, which settles its tiers on the models there are at that moment.
            await EnsureTestProviderAsync(aServices, aProvider, aProviderReason).ConfigureAwait(false);
            await EnsureDefaultProjectAsync(aServices).ConfigureAwait(false);
        }).GetAwaiter().GetResult();
    };
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

static async Task EnsureTestProviderAsync(IServiceProvider aServices, HarnessTestProvider? aProvider, string aReason)
{
    if (aProvider is null)
    {
        Log.Warning("Web harness seeds no test provider: {Reason}", aReason);
        return;
    }

    // Filed under the account's own name, like every key a person pastes (BRD-212).
    var vSecretName = Chatur.Core.Accounts.AccountSecretNames.ForAccount(AccountScope.Pinned, HarnessTestProvider.SecretName);
    try
    {
        await using var vScope = aServices.CreateAsyncScope();
        var vConnection = vScope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        using var vDb = vConnection.OpenConnection();
        using var vTransaction = vDb.BeginTransaction();

        // The harness owns exactly one provider row: the one filed under the test secret name. An
        // earlier run may have seeded a different provider there; it is brought in line with the file
        // every start, so changing test-settings.json is enough to change the provider.
        var vProviderId = vDb.ExecuteScalar<int?>(
            "SELECT ProviderId FROM Provider WHERE SecretName = @SecretName OR Name = @Name ORDER BY SecretName = @SecretName DESC LIMIT 1;",
            new { SecretName = vSecretName, aProvider.Name },
            vTransaction);

        // A provider of the same name that a test added by hand keeps its row id but becomes the seed.
        vDb.Execute(
            "DELETE FROM Model WHERE ProviderId IN (SELECT ProviderId FROM Provider WHERE SecretName = @SecretName AND ProviderId <> @ProviderId); " +
            "DELETE FROM Provider WHERE SecretName = @SecretName AND ProviderId <> @ProviderId;",
            new { SecretName = vSecretName, ProviderId = vProviderId ?? -1 },
            vTransaction);

        var vRow = new
        {
            ProviderId = vProviderId,
            aProvider.Name,
            aProvider.Connector,
            aProvider.BaseUrl,
            SecretName = vSecretName,
        };
        if (vProviderId is null)
        {
            vProviderId = vDb.ExecuteScalar<int>(
                "INSERT INTO Provider (Name, Connector, SignInMethod, BaseUrl, SecretName, State) " +
                "VALUES (@Name, @Connector, 'key', @BaseUrl, @SecretName, 'Connected'); SELECT last_insert_rowid();",
                vRow,
                vTransaction);
        }
        else
        {
            // A restart keeps the row's state (a test that paused the provider to isolate a stub model keeps it
            // paused across the restart it makes). It is connected again only when the file now names another
            // provider, connector, address or model, or when the row had failed (an earlier, refused key).
            var vChanged = vDb.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM Provider p WHERE p.ProviderId = @ProviderId AND (p.Name <> @Name OR p.Connector <> @Connector " +
                "OR p.BaseUrl <> @BaseUrl OR p.State = 'Failed' OR NOT EXISTS (SELECT 1 FROM Model m WHERE m.ProviderId = p.ProviderId AND m.Identifier = @Model));",
                new { ProviderId = vProviderId, aProvider.Name, aProvider.Connector, aProvider.BaseUrl, aProvider.Model },
                vTransaction) > 0;
            vDb.Execute(
                "UPDATE Provider SET Name = @Name, Connector = @Connector, SignInMethod = 'key', BaseUrl = @BaseUrl, " +
                "SecretName = @SecretName" + (vChanged ? ", State = 'Connected'" : string.Empty) + " WHERE ProviderId = @ProviderId;",
                vRow,
                vTransaction);
        }

        // Tier 2 is the shipped "everyday work" tier; Routing's reset-tiers puts the model there.
        var vHasModel = vDb.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM Model WHERE ProviderId = @ProviderId AND Identifier = @Model;",
            new { ProviderId = vProviderId, aProvider.Model },
            vTransaction) > 0;
        vDb.Execute(
            "DELETE FROM Model WHERE ProviderId = @ProviderId AND Identifier <> @Model;",
            new { ProviderId = vProviderId, aProvider.Model },
            vTransaction);
        if (!vHasModel)
        {
            vDb.Execute(
                "INSERT INTO Model (ProviderId, Tier, Identifier) VALUES (@ProviderId, 2, @Model);",
                new { ProviderId = vProviderId, aProvider.Model },
                vTransaction);
        }

        vTransaction.Commit();

        if (aProvider.Key is null)
        {
            // One warning that names the setting, never its value (REQ-NFR-001). The harness still
            // renders every provider-dependent screen; only a live model call would fail.
            Log.Warning(
                "Web harness test provider {Provider} has no key in test-settings.json or {SettingName}; live model calls will fail",
                aProvider.Name,
                HarnessTestProvider.SecretName);
        }
        else
        {
            var vSecretStore = vScope.ServiceProvider.GetRequiredService<ISecretStore>();
            await vSecretStore.SaveAsync(vSecretName, aProvider.Key).ConfigureAwait(false);
        }

        Log.Information(
            "Web harness seeded test provider {ProviderId} {Provider} ({Connector}) with model {Model}",
            vProviderId,
            aProvider.Name,
            aProvider.Connector,
            aProvider.Model);
    }
    catch (Exception vException)
    {
        Log.Warning(vException, "Could not seed the test provider for the web harness");
    }
}
