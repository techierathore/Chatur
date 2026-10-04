using ChaturDb;
using Serilog;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Proves REQ-NFR-002: Serilog file logging is wired at startup before anything else can fail. This
/// reproduces the exact shape <c>Chatur.WebHarness/Program.cs</c> and <c>Chatur/MauiProgram.cs</c>
/// both use — a file-sink logger created first, then a startup step wrapped so a failure is
/// <c>Log.Fatal</c>'d and the logger is always flushed — and deliberately breaks startup with a real
/// failure (<see cref="ChaturDbMigrator.Migrate"/> against a database path whose folder does not
/// exist) rather than a synthetic exception, then checks the log file the failure left behind.
/// </summary>
public sealed class SerilogStartupTests : IDisposable
{
    private readonly string objLogFolder = Path.Combine(Path.GetTempPath(), $"chatur-nfr002-{Guid.NewGuid():N}");

    public SerilogStartupTests()
    {
        Directory.CreateDirectory(objLogFolder);
    }

    /// <summary>
    /// When startup is broken on purpose — a migration against a folder that does not exist — then
    /// the log file exists on disk afterwards and holds the failure.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-002 a broken startup still leaves a log file holding the failure")]
    public void ABrokenStartupStillLeavesALogFileHoldingTheFailure()
    {
        // Wired first, exactly as both heads' startup does (Coding Standards §Logging).
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(
                Path.Combine(objLogFolder, "startup-.log"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            var vBadDatabasePath = Path.Combine(objLogFolder, "does-not-exist", "chatur.db");
            var vMigrationResult = ChaturDbMigrator.Migrate($"Data Source={vBadDatabasePath}");

            if (!vMigrationResult.Successful)
            {
                // The exact call both heads make when their own migration step fails.
                Log.Fatal(vMigrationResult.Error, "Chatur database migration failed (REQ-NFR-002 deliberate break)");
            }

            Assert.False(vMigrationResult.Successful, "the deliberately broken path was expected to fail the migration");
        }
        finally
        {
            Log.CloseAndFlush();
        }

        var vLogFiles = Directory.GetFiles(objLogFolder, "startup-*.log");
        Assert.NotEmpty(vLogFiles);

        var vLogText = File.ReadAllText(vLogFiles.Single());
        Assert.Contains("REQ-NFR-002 deliberate break", vLogText, StringComparison.Ordinal);
        // Serilog's default file template abbreviates the level to "FTL" (e.g. "[FTL]").
        Assert.Contains("FTL", vLogText, StringComparison.Ordinal);
    }

    /// <summary>Deletes the temporary log folder this test created.</summary>
    public void Dispose()
    {
        Log.CloseAndFlush();
        if (Directory.Exists(objLogFolder))
        {
            Directory.Delete(objLogFolder, recursive: true);
        }
    }
}
