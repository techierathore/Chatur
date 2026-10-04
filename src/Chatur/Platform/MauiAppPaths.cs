using Chatur.Core.Platform;

namespace Chatur.Platform;

/// <summary>
/// Roots Chatur's database and log files under <see cref="FileSystem.AppDataDirectory"/> — the real
/// machine's own application-data folder, which survives every update (Architecture §1 Q3, §5
/// "Logging").
/// </summary>
public sealed class MauiAppPaths : IAppPaths
{
    /// <inheritdoc />
    public string AppDataFolder { get; } = FileSystem.AppDataDirectory;

    /// <inheritdoc />
    public string DatabasePath => Path.Combine(AppDataFolder, "chatur.db");

    /// <inheritdoc />
    public string LogFolder { get; } = EnsureFolder(Path.Combine(FileSystem.AppDataDirectory, "logs"));

    private static string EnsureFolder(string aPath)
    {
        Directory.CreateDirectory(aPath);
        return aPath;
    }
}
