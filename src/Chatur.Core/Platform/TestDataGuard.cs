namespace Chatur.Core.Platform;

/// <summary>
/// Keeps a test run away from the owner's own data (REQ-NFR-010). A desktop head started for a test
/// — by <c>tf-verify-boot.sh</c>, which opens the web view's DevTools port, or by anyone who sets
/// <see cref="DataFolderVariable"/> — keeps its database, logs and secrets under the repository's
/// <c>tests/.artifacts/app-data/</c> folder, and refuses to start rather than open the owner's
/// application-data folder.
/// </summary>
public static class TestDataGuard
{
    /// <summary>Names the folder a test run keeps its data in; setting it marks the run as a test.</summary>
    public const string DataFolderVariable = "CHATUR_TEST_DATA";

    /// <summary>The variable <c>tf-verify-boot.sh</c> sets to open the web view's DevTools port for the tests.</summary>
    public const string WebViewArgumentsVariable = "WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS";

    /// <summary>What a test run's secret names start with, so they never replace the owner's.</summary>
    public const string SecretPrefix = "test.";

    private const string DevToolsSwitch = "--remote-debugging-port";
    private const string SolutionFile = "Chatur.sln";

    /// <summary>Whether this process was started by a test.</summary>
    /// <param name="aEnvironment">Reads an environment variable, e.g. <see cref="Environment.GetEnvironmentVariable(string)"/>.</param>
    public static bool IsUnderTest(Func<string, string?> aEnvironment) =>
        !string.IsNullOrWhiteSpace(aEnvironment(DataFolderVariable))
        || (aEnvironment(WebViewArgumentsVariable)?.Contains(DevToolsSwitch, StringComparison.OrdinalIgnoreCase) ?? false);

    /// <summary>
    /// The folder this process keeps its data in: the owner's own folder for an ordinary start, and for a
    /// test the folder <see cref="DataFolderVariable"/> names, or else <c>tests/.artifacts/app-data/{head}</c>
    /// in the repository the app was built from.
    /// </summary>
    /// <param name="aOwnerFolder">The owner's application-data folder.</param>
    /// <param name="aEnvironment">Reads an environment variable.</param>
    /// <param name="aBaseDirectory">Where the running app's files are, inside the repository when it was built there.</param>
    /// <param name="aHead">The head's name, e.g. <c>desktop</c>.</param>
    /// <exception cref="InvalidOperationException">A test run with no test folder, or one that resolves to the owner's folder.</exception>
    public static string ResolveDataFolder(string aOwnerFolder, Func<string, string?> aEnvironment, string aBaseDirectory, string aHead)
    {
        if (!IsUnderTest(aEnvironment))
        {
            return aOwnerFolder;
        }

        var vFolder = aEnvironment(DataFolderVariable) is { Length: > 0 } vNamed
            ? Path.GetFullPath(vNamed)
            : FindRepositoryRoot(aBaseDirectory) is { } vRoot
                ? Path.Combine(vRoot, "tests", ".artifacts", "app-data", aHead)
                : throw new InvalidOperationException(
                    $"This is a test run, but the app was not started from the repository and {DataFolderVariable} is not set; it will not open the owner's database.");

        EnsureNotOwnerFolder(vFolder, aOwnerFolder);
        Directory.CreateDirectory(vFolder);
        return vFolder;
    }

    /// <summary>Refuses a test folder that is the owner's application-data folder or inside it.</summary>
    /// <param name="aFolder">The folder a test run is about to use.</param>
    /// <param name="aOwnerFolder">The owner's application-data folder.</param>
    /// <exception cref="InvalidOperationException">The folder is the owner's, or inside it.</exception>
    public static void EnsureNotOwnerFolder(string aFolder, string aOwnerFolder)
    {
        var vFolder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(aFolder));
        var vOwner = Path.TrimEndingDirectorySeparator(Path.GetFullPath(aOwnerFolder));
        if (vFolder.Equals(vOwner, StringComparison.OrdinalIgnoreCase)
            || vFolder.StartsWith(vOwner + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"A test run refuses the owner's application-data folder ({vOwner}); its data stays under tests/.artifacts.");
        }
    }

    private static string? FindRepositoryRoot(string aStart)
    {
        for (var vDirectory = new DirectoryInfo(aStart); vDirectory is not null; vDirectory = vDirectory.Parent)
        {
            if (File.Exists(Path.Combine(vDirectory.FullName, SolutionFile)))
            {
                return vDirectory.FullName;
            }
        }

        return null;
    }
}
