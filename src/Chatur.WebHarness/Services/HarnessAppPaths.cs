using Chatur.Core.Platform;

namespace Chatur.WebHarness.Services;

/// <summary>
/// Roots Chatur's database and log files under the harness's own <c>bin</c> output, in a folder
/// named honestly for what it is — never the real machine's application-data folder (Foundation
/// brief "src/Chatur.WebHarness"). <see cref="TestDataGuard.DataFolderVariable"/> names another test
/// folder; either way the desktop app's own folder is refused (REQ-NFR-010).
/// </summary>
public sealed class HarnessAppPaths : IAppPaths
{
    /// <summary>Where the desktop app keeps the owner's data on Windows, which the harness never opens.</summary>
    private static readonly string OwnerFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Techie Rathore", "com.techierathore.chatur");

    /// <inheritdoc />
    public string AppDataFolder { get; } = EnsureFolder(Guarded(
        Environment.GetEnvironmentVariable(TestDataGuard.DataFolderVariable) is { Length: > 0 } vNamed
            ? vNamed
            : Path.Combine(AppContext.BaseDirectory, "harness-data")));

    /// <inheritdoc />
    public string DatabasePath => Path.Combine(AppDataFolder, "chatur.db");

    /// <inheritdoc />
    public string LogFolder => EnsureFolder(Path.Combine(AppDataFolder, "logs"));

    private static string Guarded(string aFolder)
    {
        TestDataGuard.EnsureNotOwnerFolder(aFolder, OwnerFolder);
        return aFolder;
    }

    private static string EnsureFolder(string aPath)
    {
        Directory.CreateDirectory(aPath);
        return aPath;
    }
}
