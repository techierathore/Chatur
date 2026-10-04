using System.Text.RegularExpressions;
using Chatur.Core.Actions;

namespace Chatur.Core.Prerequisites;

/// <summary>One tool Doctor knows how to look for, and how to read its version from raw command output (REQ-FN-011).</summary>
/// <param name="ToolName">The name shown on the Prerequisites tools table.</param>
/// <param name="Command">The executable to run.</param>
/// <param name="VersionArguments">The arguments that print the tool's version.</param>
/// <param name="NeededVersion">The version the row says is needed, or <see langword="null"/> for any version.</param>
/// <param name="ParseVersion">Pulls the version number out of the command's raw output, or <see langword="null"/> when it cannot be read.</param>
/// <param name="MacFixCommand">The Homebrew command that installs the tool.</param>
/// <param name="WindowsFixCommand">The winget command that installs the tool.</param>
public sealed record ToolProbeDefinition(
    string ToolName,
    string Command,
    string VersionArguments,
    string? NeededVersion,
    Func<string, string?> ParseVersion,
    string MacFixCommand,
    string WindowsFixCommand);

/// <summary>
/// The tools Chatur's own build and test pipeline needs (Architecture §7 "Doctor"). No project yet
/// records its own list of required tools in the data model, so this catalog is probed for every
/// project until that schema exists — a limitation, not a per-project detector.
/// </summary>
public static class ToolProbeCatalog
{
    /// <summary>The .NET SDK — needed by any project with a <c>.sln</c> or <c>.csproj</c> (REQ-FN-011).</summary>
    public static readonly ToolProbeDefinition DotNetSdk = new(
        ".NET SDK", "dotnet", "--version", null,
        FirstLine,
        "brew install --cask dotnet-sdk", "winget install Microsoft.DotNet.SDK.10");

    /// <summary>git — needed by every project (REQ-FN-011's own words: "always git").</summary>
    public static readonly ToolProbeDefinition Git = new(
        "git", "git", "--version", null,
        aOutput => Match(aOutput, @"git version (\S+)"),
        "brew install git", "winget install Git.Git");

    /// <summary>Node — needed by any project with a <c>package.json</c> (REQ-FN-011).</summary>
    public static readonly ToolProbeDefinition Node = new(
        "Node", "node", "--version", null,
        aOutput => FirstLine(aOutput)?.TrimStart('v'),
        "brew install node", "winget install OpenJS.NodeJS");

    /// <summary>npm — needed alongside Node (REQ-FN-011).</summary>
    public static readonly ToolProbeDefinition Npm = new(
        "npm", "npm", "--version", null,
        FirstLine,
        "brew install node", "winget install OpenJS.NodeJS");

    /// <summary>
    /// The MAUI workload — needed only when a <c>.csproj</c> sets <c>&lt;UseMaui&gt;true&lt;/UseMaui&gt;</c>
    /// (REQ-FN-011). Probed with <c>dotnet workload list</c> rather than a version flag, since a
    /// workload has no single version of its own; "found" means a workload that builds the desktop
    /// heads (<c>maui</c>, <c>maui-windows</c> or <c>maui-maccatalyst</c>) is in the installed list, not a
    /// parsed number. <c>maui-android</c> alone does not count: it cannot build this app's heads.
    /// </summary>
    public static readonly ToolProbeDefinition MauiWorkload = new(
        "MAUI workload", "dotnet", "workload list", null,
        aOutput => MauiWorkloadInstalled(aOutput) ? "installed" : null,
        "brew install --cask dotnet-sdk && dotnet workload install maui",
        "winget install Microsoft.DotNet.SDK.10 && dotnet workload install maui");

    /// <summary>Docker — needed only when a <c>Dockerfile</c> or <c>docker-compose.y[a]ml</c> exists (REQ-FN-011).</summary>
    public static readonly ToolProbeDefinition Docker = new(
        "Docker", "docker", "--version", null,
        aOutput => Match(aOutput, @"Docker version (\S+),"),
        "brew install --cask docker", "winget install Docker.DockerDesktop");

    /// <summary>The base set probed when no project is selected (REQ-FN-011) — unchanged from before this fix.</summary>
    public static IReadOnlyList<ToolProbeDefinition> Default { get; } = [DotNetSdk, Git, Node, Npm];

    /// <summary>
    /// Derives the tools the selected project needs from what its own files say (REQ-FN-011's
    /// acceptance: "each tool it needs is listed") — a pure function over <see cref="ProjectToolSignals"/>
    /// so the derivation itself is unit testable with no real folder involved;
    /// <see cref="PrerequisiteActions"/> is the one place that reads the disk to build the signals.
    /// </summary>
    /// <param name="aSignals">What <see cref="PrerequisiteActions"/> found on disk for the selected project.</param>
    public static IReadOnlyList<ToolProbeDefinition> ForProject(ProjectToolSignals aSignals)
    {
        var vTools = new List<ToolProbeDefinition> { Git };

        if (aSignals.HasDotNetProject)
        {
            vTools.Add(DotNetSdk);
            if (aSignals.UsesMaui)
            {
                vTools.Add(MauiWorkload);
            }
        }

        if (aSignals.HasNodePackage)
        {
            vTools.Add(Node);
            vTools.Add(Npm);
        }

        if (aSignals.HasDocker)
        {
            vTools.Add(Docker);
        }

        return vTools;
    }

    private static bool MauiWorkloadInstalled(string aOutput) =>
        Regex.IsMatch(aOutput, @"^\s*(maui|maui-windows|maui-maccatalyst)\s", RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static string? FirstLine(string aOutput)
    {
        var vLine = aOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(vLine) ? null : vLine;
    }

    private static string? Match(string aOutput, string aPattern)
    {
        var vMatch = Regex.Match(aOutput, aPattern);
        return vMatch.Success ? vMatch.Groups[1].Value : null;
    }
}
