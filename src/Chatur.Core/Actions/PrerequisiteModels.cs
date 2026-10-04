namespace Chatur.Core.Actions;

/// <summary>How a tool probe on Prerequisites came out (REQ-FN-011).</summary>
public enum ToolCheckState
{
    /// <summary>The tool was found at or above the needed version.</summary>
    Ready,

    /// <summary>The tool was not found on this machine.</summary>
    Missing,

    /// <summary>The tool was found but is not currently running, e.g. Docker Desktop.</summary>
    NotRunning,

    /// <summary>The probe itself failed, so readiness could not be determined.</summary>
    Unknown
}

/// <summary>One row of the Prerequisites tools table (REQ-FN-011, REQ-UI-017).</summary>
/// <param name="ToolName">The tool's name.</param>
/// <param name="NeededVersion">The version the project needs, or <see langword="null"/> for any version.</param>
/// <param name="FoundVersion">The version found on this machine, or <see langword="null"/> when not found.</param>
/// <param name="State">Whether the tool is ready to use.</param>
/// <param name="FixCommand">The command that installs or starts the tool, ready to copy.</param>
public sealed record ToolCheck(
    string ToolName,
    string? NeededVersion,
    string? FoundVersion,
    ToolCheckState State,
    string? FixCommand);

/// <summary>
/// What the selected project's own files say it needs probed (REQ-FN-011) — read once from disk by
/// <see cref="Prerequisites.PrerequisiteActions"/>, then handed to
/// <see cref="Prerequisites.ToolProbeCatalog.ForProject"/>, a pure function kept unit testable
/// without a real folder (the same split <see cref="Prerequisites.NightlyRelease.EvaluateOffer"/>
/// uses to keep a network-free decision separate from the network call that feeds it).
/// </summary>
/// <param name="HasDotNetProject">A <c>.sln</c> or <c>.csproj</c> file exists (needs the .NET SDK).</param>
/// <param name="UsesMaui">A <c>.csproj</c> sets <c>&lt;UseMaui&gt;true&lt;/UseMaui&gt;</c> (also needs the MAUI workload).</param>
/// <param name="HasNodePackage">A <c>package.json</c> file exists (needs Node and npm).</param>
/// <param name="HasDocker">A <c>Dockerfile</c> or a <c>docker-compose.yml</c>/<c>.yaml</c> file exists (needs Docker).</param>
public sealed record ProjectToolSignals(bool HasDotNetProject, bool UsesMaui, bool HasNodePackage, bool HasDocker);

/// <summary>This Chatur's own build identity (REQ-UI-018), or a newer one found on GitHub (REQ-FN-013).</summary>
/// <param name="Version">The version string, e.g. <c>"0.1.0"</c>.</param>
/// <param name="Commit">The commit the build came from.</param>
/// <param name="BuiltUtc">When this build was produced.</param>
/// <param name="DownloadUrl">
/// The download for this machine, when this identity came from <c>NewerBuildAsync</c> and a matching
/// release asset exists; <see langword="null"/> for the running build's own identity.
/// </param>
public sealed record ChaturBuildInfo(string Version, string Commit, DateTime BuiltUtc, string? DownloadUrl = null);
