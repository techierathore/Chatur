using Chatur.Core.Data;
using Chatur.Core.Measurements;
using Chatur.Core.Platform;
using Chatur.Core.ProcessRuns;
using Chatur.Core.Processes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Chatur.Tests.Support;

/// <summary>Builds the real run engine over a test database, with whichever executor a test brings.</summary>
public static class ProcessRunWiring
{
    /// <summary>The run engine with its real verdict writer and evidence store.</summary>
    /// <param name="aConnections">The test database.</param>
    /// <param name="aExecutor">Carries out the steps.</param>
    public static ProcessRunActions Actions(IDbConnectionFactory aConnections, IProcessStepExecutor aExecutor)
    {
        var vClock = new SystemClock();
        var vEvidence = new RunEvidenceStore(aConnections, vClock);
        return new ProcessRunActions(
            aConnections,
            aExecutor,
            new MeasurementActions(aConnections, NullLogger<MeasurementActions>.Instance),
            new RunVerdictWriter(
                aConnections,
                new ProcessActions(aConnections, vClock, NullLogger<ProcessActions>.Instance),
                vClock,
                new ProjectFolders(aConnections),
                vEvidence),
            vEvidence,
            vClock,
            NullLogger<ProcessRunActions>.Instance);
    }

    /// <summary>The real executor over a test database, with run folders under <paramref name="aAppData"/>.</summary>
    /// <param name="aConnections">The test database.</param>
    /// <param name="aAppData">Where Chatur's own files go.</param>
    /// <param name="aOptions">The limits to run under.</param>
    public static ProjectStepExecutor Executor(IDbConnectionFactory aConnections, string aAppData, ProcessRunOptions aOptions)
    {
        var vClock = new SystemClock();
        return new ProjectStepExecutor(
            new ProjectFolders(aConnections),
            new RunEvidenceStore(aConnections, vClock),
            new ProjectCommandRunner(NullLogger<ProjectCommandRunner>.Instance),
            aOptions,
            new FixedAppPaths(aAppData),
            vClock);
    }

    private sealed class FixedAppPaths : IAppPaths
    {
        public FixedAppPaths(string aFolder)
        {
            AppDataFolder = aFolder;
        }

        public string AppDataFolder { get; }

        public string DatabasePath => Path.Combine(AppDataFolder, "chatur.db");

        public string LogFolder => Path.Combine(AppDataFolder, "logs");
    }
}
