using Chatur.Core.Platform;

namespace Chatur.WebHarness.Services;

/// <summary>
/// Roots Chatur's database and log files under the harness's own <c>bin</c> output, in a folder
/// named honestly for what it is — never the real machine's application-data folder (Foundation
/// brief "src/Chatur.WebHarness").
/// </summary>
public sealed class HarnessAppPaths : IAppPaths
{
    /// <inheritdoc />
    public string AppDataFolder { get; } = EnsureFolder(Path.Combine(AppContext.BaseDirectory, "harness-data"));

    /// <inheritdoc />
    public string DatabasePath => Path.Combine(AppDataFolder, "chatur.db");

    /// <inheritdoc />
    public string LogFolder { get; } = EnsureFolder(Path.Combine(AppContext.BaseDirectory, "harness-data", "logs"));

    private static string EnsureFolder(string aPath)
    {
        Directory.CreateDirectory(aPath);
        return aPath;
    }
}
