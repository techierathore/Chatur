using Chatur.Core.ProcessRuns;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chatur.Tests.ProcessRuns;

/// <summary>Tests for <see cref="ProjectCommandRunner"/> against real child processes (REQ-FN-052).</summary>
public sealed class ProjectCommandRunnerTests
{
    private readonly ProjectCommandRunner objRunner = new(NullLogger<ProjectCommandRunner>.Instance);

    /// <summary>When a command runs past its limit, then its process is stopped, the result says it timed out, and the call returns promptly.</summary>
    [Fact(DisplayName = "REQ-FN-052 a command past its time limit is stopped")]
    public async Task CommandPastItsLimitIsStopped()
    {
        var vCommand = new ProjectCommand(SleepProgram, SleepArguments, Path.GetTempPath(), TimeSpan.FromSeconds(1), 1000);

        var vResult = await objRunner.RunAsync(vCommand);

        Assert.True(vResult.Started);
        Assert.True(vResult.TimedOut);
        Assert.True(vResult.Elapsed < TimeSpan.FromSeconds(20));
    }

    /// <summary>When the caller cancels, then the run is cancelled and the process is stopped.</summary>
    [Fact(DisplayName = "REQ-FN-052 cancelling stops the command")]
    public async Task CancellingStopsTheCommand()
    {
        using var vSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        var vCommand = new ProjectCommand(SleepProgram, SleepArguments, Path.GetTempPath(), TimeSpan.FromMinutes(5), 1000);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => objRunner.RunAsync(vCommand, vSource.Token));
    }

    /// <summary>When a program does not exist, then the result says it did not start rather than throwing.</summary>
    [Fact(DisplayName = "REQ-FN-052 a missing program is reported not started")]
    public async Task MissingProgramIsNotStarted()
    {
        var vResult = await objRunner.RunAsync(new ProjectCommand("chatur-no-such-program", string.Empty, Path.GetTempPath(), TimeSpan.FromSeconds(5), 1000));

        Assert.False(vResult.Started);
        Assert.Contains("could not be started", vResult.Error, StringComparison.Ordinal);
    }

    /// <summary>When a command writes more than the cap, then only the end of its output is kept.</summary>
    [Fact(DisplayName = "REQ-FN-052 output is capped to its end")]
    public void OutputIsCappedToItsEnd()
    {
        var vTail = new OutputTail(50);
        for (var vLine = 1; vLine <= 100; vLine++)
        {
            vTail.Add($"line {vLine}");
        }

        var vText = vTail.ToString();

        Assert.True(vText.Length <= 50);
        Assert.EndsWith("line 100", vText, StringComparison.Ordinal);
        Assert.DoesNotContain("line 1\n", vText, StringComparison.Ordinal);
    }

    private static string SleepProgram => OperatingSystem.IsWindows() ? "ping" : "sleep";

    private static string SleepArguments => OperatingSystem.IsWindows() ? "-n 60 127.0.0.1" : "60";
}
