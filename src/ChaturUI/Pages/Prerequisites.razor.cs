using System.Runtime.InteropServices;
using Chatur.Core.Actions;
using TrBlazeUI.Components.Badge;

namespace ChaturUI.Pages;

/// <summary>
/// Prerequisites (mockups/prerequisites.html). Owns the whole page shell; wires the ready-to-copy
/// fix command (REQ-UI-017) and the "This Chatur" version/commit card (REQ-UI-018). The tools list
/// and the newer-build offer come from <see cref="IPrerequisiteActions.ProbeAsync"/> and
/// <see cref="IPrerequisiteActions.NewerBuildAsync"/> (cluster I, REQ-FN-011 through REQ-FN-013);
/// while either still throws <see cref="NotImplementedException"/> this page shows an honest empty
/// state instead of crashing.
/// </summary>
public partial class Prerequisites
{
    private bool objIsLoading = true;
    private IReadOnlyList<ToolCheck>? objTools;
    private ChaturBuildInfo? objBuildInfo;
    private ChaturBuildInfo? objNewerBuild;
    private string objMachine = string.Empty;
    private string objCheckedAt = string.Empty;

    private int ReadyCount => objTools?.Count(aTool => aTool.State == ToolCheckState.Ready) ?? 0;

    private int AttentionCount => objTools is null ? 0 : objTools.Count - ReadyCount;

    private string Subtitle => State.SelectedProject is null
        ? "No project is open, so only Chatur's own version can be checked."
        : $"Every tool {State.SelectedProject.Name} needs before it will build, and what Chatur itself is.";

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        objMachine = $"{RuntimeInformation.OSDescription} · {PlatformName()}";
        await LoadAsync();
    }

    /// <summary>Runs every probe again — the mockup's "Check again" button.</summary>
    private Task CheckAgainAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        objIsLoading = true;
        StateHasChanged();

        objTools = await ProbeToolsAsync();
        objBuildInfo = await ReadOwnVersionAsync();
        objNewerBuild = await ReadNewerBuildAsync();
        objCheckedAt = "just now";

        objIsLoading = false;
    }

    /// <summary>
    /// Wraps <see cref="IPrerequisiteActions.ProbeAsync"/>: <see langword="null"/> instead of a crash
    /// while cluster I's probe still throws <see cref="NotImplementedException"/>.
    /// </summary>
    private async Task<IReadOnlyList<ToolCheck>?> ProbeToolsAsync()
    {
        try
        {
            return await PrerequisiteActions.ProbeAsync(State.SelectedProject?.ProjectId);
        }
        catch (NotImplementedException)
        {
            return null;
        }
    }

    /// <summary>Wraps <see cref="IPrerequisiteActions.OwnVersionAsync"/> (REQ-UI-018).</summary>
    private async Task<ChaturBuildInfo?> ReadOwnVersionAsync()
    {
        try
        {
            return await PrerequisiteActions.OwnVersionAsync();
        }
        catch (NotImplementedException)
        {
            return null;
        }
    }

    /// <summary>
    /// Wraps <see cref="IPrerequisiteActions.NewerBuildAsync"/>: <see langword="null"/> instead of a
    /// crash while cluster I's check still throws <see cref="NotImplementedException"/>.
    /// </summary>
    private async Task<ChaturBuildInfo?> ReadNewerBuildAsync()
    {
        try
        {
            return await PrerequisiteActions.NewerBuildAsync();
        }
        catch (NotImplementedException)
        {
            return null;
        }
    }

    private static string PlatformName() =>
        OperatingSystem.IsMacCatalyst() ? "Mac Catalyst" :
        OperatingSystem.IsWindows() ? "Windows" : "Web";

    /// <summary>Turns a tool name into the stable slug its copy button's <c>data-testid</c> uses.</summary>
    private static string Slug(string aName) =>
        aName.ToLowerInvariant().Replace(" ", "-").Replace(".", string.Empty);

    /// <summary>Splits a semver prerelease tag onto its own side of a middle dot for display, e.g. <c>"0.1.0-nightly"</c> → <c>"0.1.0 · nightly"</c>.</summary>
    private static string DisplayVersion(string aVersion)
    {
        var vDashIndex = aVersion.IndexOf('-');
        return vDashIndex < 0 ? aVersion : $"{aVersion[..vDashIndex]} · {aVersion[(vDashIndex + 1)..]}";
    }

    private static string ShortCommit(string aCommit) => aCommit.Length > 7 ? aCommit[..7] : aCommit;

    private static BadgeVariant StateVariant(ToolCheckState aState) => aState switch
    {
        ToolCheckState.Ready => BadgeVariant.Success,
        ToolCheckState.Missing => BadgeVariant.Warning,
        ToolCheckState.NotRunning => BadgeVariant.Warning,
        _ => BadgeVariant.Outline
    };

    /// <summary>The pill look the mockup gives a tool's state: no border, a check before "ready", and the success colour at full strength.</summary>
    /// <param name="aState">The tool's state.</param>
    private static string StateClass(ToolCheckState aState) => aState == ToolCheckState.Ready
        ? "gap-1 rounded-full border-transparent bg-success/15 text-success"
        : "gap-1 rounded-full border-transparent";

    private static string StateLabel(ToolCheckState aState) => aState switch
    {
        ToolCheckState.Ready => "ready",
        ToolCheckState.Missing => "missing",
        ToolCheckState.NotRunning => "not running",
        _ => "unknown"
    };
}
