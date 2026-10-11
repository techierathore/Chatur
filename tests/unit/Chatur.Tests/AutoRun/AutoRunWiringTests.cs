using Chatur.Core;
using Chatur.Core.Actions;
using Chatur.Core.AutoRun;
using Chatur.Core.Platform;
using Chatur.Core.ProcessRuns;
using Chatur.Tests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Chatur.Tests.AutoRun;

/// <summary>
/// The real service registration, as the heads make it: the end-to-end run resolves with the real
/// launcher, the real engine behind both interfaces, and runs a chain against a real database
/// (REQ-FN-061, REQ-FN-062).
/// </summary>
public sealed class AutoRunWiringTests : MigratedDatabaseFixture
{
    /// <summary>
    /// When the heads register Chatur.Core, then <see cref="IAutoRunActions"/> resolves, the unattended
    /// engine is the same object as <see cref="IProcessRunActions"/>, and the real background launcher
    /// carries a started run on to the acceptance test on its own (REQ-FN-061, REQ-FN-062).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-061 the registered end-to-end run starts and the background launcher carries it to acceptance")]
    public async Task RegisteredRunIsCarriedOnInTheBackground()
    {
        var vFolder = Path.Combine(Path.GetTempPath(), $"chatur-wire-{Guid.NewGuid():N}");
        Directory.CreateDirectory(vFolder);
        try
        {
            var vServices = new ServiceCollection();
            vServices.AddLogging();
            vServices.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            vServices.AddSingleton<IClock, SystemClock>();
            vServices.AddSingleton<IAppPaths>(new WiringPaths(vFolder, ConnectionString));
            vServices.AddChaturCore();
            // A handed step now needs a model and the checks need documents; this test is about the wiring, so the steps are stood in for.
            vServices.AddSingleton<ISecretStore, InMemorySecretStore>();
            vServices.AddSingleton<IProcessLauncher, NoLauncher>();
            vServices.AddScoped<IProcessStepExecutor, PassingExecutor>();
            vServices.AddSingleton<Chatur.Core.Data.IDbConnectionFactory>(CreateFactory());
            vServices.AddSingleton(new AutoRunOptions { StallTimeout = TimeSpan.FromSeconds(30) });
            await using var vProvider = vServices.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = false });
            var vProject = CreateProject("Wired", vFolder);

            using var vScope = vProvider.CreateScope();
            var vActions = vScope.ServiceProvider.GetRequiredService<IAutoRunActions>();
            Assert.Same(
                vScope.ServiceProvider.GetRequiredService<IProcessRunActions>(),
                vScope.ServiceProvider.GetRequiredService<IUnattendedProcessRuns>());
            var vStarted = await vActions.StartAsync(vProject, "Finish the wiring check and stop at the acceptance test, please.");

            AutoRunView? vNow = vStarted;
            for (var vTry = 0; vTry < 600 && vNow!.State != AutoRunState.ReadyForAcceptance; vTry++)
            {
                await Task.Delay(100);
                vNow = await vActions.GetAsync(vStarted.AutoRunId);
            }

            Assert.Equal(AutoRunState.ReadyForAcceptance, vNow!.State);
            Assert.All(vNow.Chain, aProcess => Assert.Equal(AutoRunProcessState.Finished, aProcess.State));
        }
        finally
        {
            Directory.Delete(vFolder, true);
        }
    }

    private sealed class NoLauncher : IProcessLauncher
    {
        public Task OpenInEditorAsync(string aFilePath, CancellationToken aCt = default) => Task.CompletedTask;

        public Task OpenWithDefaultAppAsync(string aFilePath, CancellationToken aCt = default) => Task.CompletedTask;

        public Task<int> RunAsync(string aCommand, string aArguments, string aWorkingDirectory, Action<string> aOnOutputLine, CancellationToken aCt = default) => Task.FromResult(0);
    }

    private sealed class PassingExecutor : IProcessStepExecutor
    {
        public Task<ProcessStepOutcome> ExecuteAsync(int aRunId, int aProjectId, string aProcessCode, ProcessRunStepView aStep, CancellationToken aCt = default) =>
            Task.FromResult(new ProcessStepOutcome(true, "Done"));
    }

    private sealed class WiringPaths : IAppPaths
    {
        public WiringPaths(string aFolder, string aConnectionString)
        {
            AppDataFolder = aFolder;
            DatabasePath = aConnectionString;
        }

        public string AppDataFolder { get; }

        public string DatabasePath { get; }

        public string LogFolder => AppDataFolder;
    }
}
