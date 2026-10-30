using Chatur.Core.Platform;

namespace Chatur.Platform;

/// <summary>
/// Roots Chatur's database and log files under <see cref="FileSystem.AppDataDirectory"/> — the real
/// machine's own application-data folder, which survives every update (Architecture §1 Q3, §5
/// "Logging"). A desktop head started by a test keeps them under the repository's
/// <c>tests/.artifacts/app-data/desktop</c> instead, never in the owner's folder (REQ-NFR-010).
/// </summary>
public sealed class MauiAppPaths : IAppPaths
{
    /// <summary>The folder this run keeps its data in, decided once per process.</summary>
    public static string DataFolder { get; } = TestDataGuard.ResolveDataFolder(
        FileSystem.AppDataDirectory, Environment.GetEnvironmentVariable, AppContext.BaseDirectory, "desktop");

    /// <inheritdoc />
    public string AppDataFolder { get; } = DataFolder;

    /// <inheritdoc />
    public string DatabasePath => Path.Combine(AppDataFolder, "chatur.db");

    /// <inheritdoc />
    public string LogFolder { get; } = EnsureFolder(Path.Combine(DataFolder, "logs"));

    private static string EnsureFolder(string aPath)
    {
        Directory.CreateDirectory(aPath);
        return aPath;
    }
}
