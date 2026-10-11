using System.Text.Json;
using System.Text.RegularExpressions;

namespace Chatur.Core.BuildRun;

/// <summary>
/// Finds, from a project's own files, the commands that build it and test it: <c>dotnet build</c> /
/// <c>dotnet test</c> for a .NET project, <c>npm run build</c> for a node project that has a build
/// script, <c>npx playwright test</c> for a project with a Playwright config. The same reading of the
/// project's files that <see cref="BuildRunActions"/> uses to offer its run targets, so the owner's
/// Build &amp; run and a verifying process never disagree about what the project is.
/// </summary>
public static class ProjectToolchain
{
    private static readonly string[] PlaywrightConfigs =
    [
        "playwright.config.ts", "playwright.config.js", "playwright.config.mjs", "playwright.config.cjs"
    ];

    private static readonly HashSet<string> ExcludedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", "node_modules", ".vs", ".idea"
    };

    /// <summary>The file or folder to build for a .NET project: its solution, else its first non-test project, else its first test project; <see langword="null"/> when it has none.</summary>
    /// <param name="aFolder">The project's folder.</param>
    public static string? DotNetBuildTarget(string aFolder) =>
        FirstOf(aFolder, "*.sln")
        ?? FirstOf(aFolder, "*.slnx")
        ?? FirstOf(aFolder, "*.csproj", aPath => !IsTestProject(aPath))
        ?? FirstOf(aFolder, "*.csproj");

    /// <summary>Every .NET test project in the folder (a project that references <c>Microsoft.NET.Test.Sdk</c>).</summary>
    /// <param name="aFolder">The project's folder.</param>
    public static IReadOnlyList<string> DotNetTestProjects(string aFolder) =>
        EnumerateFiles(aFolder, "*.csproj").Where(IsTestProject).OrderBy(aPath => aPath, StringComparer.Ordinal).ToList();

    /// <summary>Whether the project's <c>package.json</c> has a <c>build</c> script.</summary>
    /// <param name="aFolder">The project's folder.</param>
    public static bool HasNodeBuildScript(string aFolder)
    {
        var vPath = Path.Combine(aFolder, "package.json");
        if (!File.Exists(vPath))
        {
            return false;
        }

        try
        {
            using var vDocument = JsonDocument.Parse(File.ReadAllText(vPath), new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            return vDocument.RootElement.TryGetProperty("scripts", out var vScripts)
                && vScripts.ValueKind == JsonValueKind.Object
                && vScripts.TryGetProperty("build", out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>Whether the project has a Playwright config at its root.</summary>
    /// <param name="aFolder">The project's folder.</param>
    public static bool HasPlaywrightConfig(string aFolder) =>
        PlaywrightConfigs.Any(aName => File.Exists(Path.Combine(aFolder, aName)));

    /// <summary>The program that runs <c>dotnet</c> for this process: the host that started it when known, else <c>dotnet</c> on the path.</summary>
    public static string DotNetExecutable()
    {
        var vHost = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(vHost) && File.Exists(vHost))
        {
            return vHost;
        }

        var vFile = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        var vRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrWhiteSpace(vRoot) && File.Exists(Path.Combine(vRoot, vFile)))
        {
            return Path.Combine(vRoot, vFile);
        }

        return Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet" && Environment.ProcessPath is { } vSelf
            ? vSelf
            : "dotnet";
    }

    /// <summary>The program that runs <c>npm</c> on this machine.</summary>
    public static string NpmExecutable() => OperatingSystem.IsWindows() ? "npm.cmd" : "npm";

    /// <summary>The program that runs <c>npx</c> on this machine.</summary>
    public static string NpxExecutable() => OperatingSystem.IsWindows() ? "npx.cmd" : "npx";

    /// <summary>
    /// Every file matching a pattern under a folder, skipping build output, source-control and package
    /// folders.
    /// </summary>
    /// <param name="aRoot">The folder to search.</param>
    /// <param name="aPattern">The file pattern, e.g. <c>*.csproj</c>.</param>
    public static IEnumerable<string> EnumerateFiles(string aRoot, string aPattern)
    {
        var vStack = new Stack<string>();
        vStack.Push(aRoot);

        while (vStack.Count > 0)
        {
            var vDirectory = vStack.Pop();
            IEnumerable<string> vFiles;
            IEnumerable<string> vSubdirectories;
            try
            {
                vFiles = Directory.EnumerateFiles(vDirectory, aPattern);
                vSubdirectories = Directory.EnumerateDirectories(vDirectory).Where(aDirectory => !ExcludedFolders.Contains(Path.GetFileName(aDirectory)));
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var vFile in vFiles)
            {
                yield return vFile;
            }

            foreach (var vSubdirectory in vSubdirectories)
            {
                vStack.Push(vSubdirectory);
            }
        }
    }

    private static string? FirstOf(string aFolder, string aPattern, Func<string, bool>? aWhere = null)
    {
        var vMatches = EnumerateFiles(aFolder, aPattern).Where(aPath => aWhere?.Invoke(aPath) ?? true).ToList();
        var vTopLevel = vMatches.Where(aPath => string.Equals(Path.GetDirectoryName(aPath), aFolder.TrimEnd(Path.DirectorySeparatorChar), StringComparison.Ordinal)).OrderBy(aPath => aPath, StringComparer.Ordinal).FirstOrDefault();
        return vTopLevel ?? vMatches.OrderBy(aPath => aPath.Length).ThenBy(aPath => aPath, StringComparer.Ordinal).FirstOrDefault();
    }

    private static bool IsTestProject(string aPath)
    {
        try
        {
            return Regex.IsMatch(File.ReadAllText(aPath), "Microsoft\\.NET\\.Test\\.Sdk", RegexOptions.IgnoreCase);
        }
        catch (IOException)
        {
            return false;
        }
    }
}
