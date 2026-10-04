using System.Text.RegularExpressions;
using Chatur.Core;
using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;
using TrBlazeUI.Components.Badge;
using TrBlazeUI.Components.LogView;

namespace ChaturUI.Shell;

/// <summary>
/// The Workbench's output strip: the build/run's live output (REQ-UI-014) and, when a build fails,
/// its errors with file and line, each one opening that file (REQ-UI-016).
/// </summary>
/// <remarks>Owning cluster: H (REQ-UI-012 through REQ-UI-016).</remarks>
public partial class OutputStrip : IDisposable
{
    // Mirrors BuildRunActions' own error line format exactly, so an error opened here is the same
    // one Build reported (REQ-UI-016) without OutputStrip and AppToolbar needing a shared result type.
    private static readonly Regex ErrorLineRegex = new(
        @"^\s*(?<file>[^()\r\n]+?)\((?<line>\d+)(,\d+)?\)\s*:\s*error\s+\S+\s*:\s*(?<message>.+?)(\s*\[.*\])?\s*$",
        RegexOptions.Compiled);

    private readonly List<LogLine> objLines = [];
    private readonly List<BuildError> objErrors = [];
    private CancellationTokenSource? objStreamCts;
    private int? objStreamingForProjectId;
    private string objStatusText = "nothing run yet";

    /// <summary>The result pill's colour: green once a build passed, red once it failed, neutral otherwise (mockup `pill ok`).</summary>
    private BadgeVariant StatusVariant => objStatusText switch
    {
        _ when objStatusText.Contains("failed", StringComparison.Ordinal) => BadgeVariant.Destructive,
        _ when objStatusText.Contains("passed", StringComparison.Ordinal) => BadgeVariant.Success,
        _ => BadgeVariant.Secondary,
    };

    [Inject]
    private IBuildRunActions BuildRun { get; set; } = default!;

    [Inject]
    private IFileActions Files { get; set; } = default!;

    [Inject]
    private AppState AppStateService { get; set; } = default!;

    [Inject]
    private WorkbenchState Workbench { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        AppStateService.Changed += OnAppStateChanged;
        Workbench.BuildOrRunStarted += OnBuildOrRunStarted;
        EnsureStreaming();
    }

    private void EnsureStreaming()
    {
        var vProjectId = AppStateService.SelectedProject?.ProjectId;
        if (vProjectId == objStreamingForProjectId)
        {
            return;
        }

        objStreamingForProjectId = vProjectId;
        RestartStreaming(vProjectId);
    }

    /// <summary>
    /// Clears what is on screen and opens a fresh subscription, whether because the selected project
    /// changed or because a new build or run just started for the same one (REQ-UI-014) — Build and
    /// Run share one growing output buffer per project (<c>BuildRunActions.RunState</c>), so a display
    /// that only ever appended would show every past build and run stacked on top of each other.
    /// </summary>
    private void RestartStreaming(int? aProjectId)
    {
        objStreamCts?.Cancel();
        objLines.Clear();
        objErrors.Clear();
        objStatusText = "nothing run yet";

        if (aProjectId is { } vId)
        {
            objStreamCts = new CancellationTokenSource();
            _ = ConsumeOutputAsync(vId, objStreamCts.Token);
        }
    }

    /// <summary>
    /// Runs synchronously, inline within the click handler that raised
    /// <see cref="WorkbenchState.BuildOrRunStarted"/> — never through <c>InvokeAsync</c> — so the old
    /// subscription is cancelled and the display cleared <em>before</em> control returns to
    /// <c>AppToolbar</c> and it calls <c>IBuildRunActions.BuildAsync</c>/<c>RunAsync</c>, which clears
    /// the server buffer a moment later. Deferring this through <c>InvokeAsync</c> left a real gap in
    /// which the previous, not-yet-cancelled subscription's own 150 ms poll could land inside that
    /// gap, wake up, see the server buffer already cleared for the new run, and start replaying it
    /// too — duplicating every line the new subscription was also reading.
    /// </summary>
    private void OnBuildOrRunStarted()
    {
        RestartStreaming(objStreamingForProjectId);
        StateHasChanged();
    }

    private async Task ConsumeOutputAsync(int aProjectId, CancellationToken aCt)
    {
        try
        {
            await foreach (var vLine in BuildRun.StreamOutputAsync(aProjectId, aCt))
            {
                objLines.Add(new LogLine(vLine.Text, vLine.IsError ? LogLineKind.Failure : LogLineKind.Ordinary));
                UpdateStatus(vLine);

                if (vLine.IsError)
                {
                    var vError = ParseError(vLine.Text, AppStateService.SelectedProject?.Path);

                    // dotnet build prints each error at least twice (once as it happens, again in the
                    // "Build FAILED" summary) — list it once, not once per line it appeared on.
                    if (vError is not null && !objErrors.Contains(vError))
                    {
                        objErrors.Add(vError);
                    }
                }

                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the project changes or the component is disposed.
        }
    }

    private void UpdateStatus(OutputLine aLine)
    {
        if (aLine.Text.StartsWith("Build succeeded", StringComparison.Ordinal))
        {
            objStatusText = "build passed";
        }
        else if (aLine.Text.StartsWith("Build FAILED", StringComparison.Ordinal))
        {
            objStatusText = "build failed";
        }
        else if (aLine.Text.Contains("Process exited", StringComparison.Ordinal)
                 || aLine.Text.StartsWith("Run stopped", StringComparison.Ordinal)
                 || aLine.Text.StartsWith("Build stopped", StringComparison.Ordinal))
        {
            objStatusText = "stopped";
        }
        else if (aLine.Text.StartsWith("> dotnet run", StringComparison.Ordinal))
        {
            objStatusText = "running";
        }
    }

    /// <summary>
    /// Opens a build error's file in a tab, so the owner lands on the file the compiler is pointing
    /// at (REQ-UI-016). The line number itself is shown beside the error; <c>CodeEditor</c> has no
    /// jump-to-line yet, so this opens the file rather than also scrolling to it.
    /// </summary>
    private async Task OpenErrorFileAsync(BuildError aError)
    {
        if (AppStateService.SelectedProject is not { } vProject)
        {
            return;
        }

        try
        {
            var vContent = await Files.ReadAsync(vProject.ProjectId, aError.FilePath);
            Workbench.Open(aError.FilePath, vContent);
        }
        catch (IOException)
        {
            // The file named by the compiler no longer exists on disk — nothing to open.
        }
    }

    private static BuildError? ParseError(string aLine, string? aProjectPath)
    {
        var vMatch = ErrorLineRegex.Match(aLine);
        if (!vMatch.Success)
        {
            return null;
        }

        var vFile = vMatch.Groups["file"].Value.Trim();
        if (!string.IsNullOrEmpty(aProjectPath) && Path.IsPathRooted(vFile))
        {
            try
            {
                vFile = Path.GetRelativePath(aProjectPath, vFile);
            }
            catch (ArgumentException)
            {
                // Keep the rooted path when it is not under the project.
            }
        }

        return new BuildError(vFile.Replace('\\', '/'), int.Parse(vMatch.Groups["line"].Value), vMatch.Groups["message"].Value.Trim());
    }

    private void OnAppStateChanged() => InvokeAsync(() =>
    {
        EnsureStreaming();
        StateHasChanged();
    });

    /// <inheritdoc />
    public void Dispose()
    {
        AppStateService.Changed -= OnAppStateChanged;
        objStreamCts?.Cancel();
    }
}
