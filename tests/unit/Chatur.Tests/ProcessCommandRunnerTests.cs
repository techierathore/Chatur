using System.Text.RegularExpressions;
using Chatur.Core.Prerequisites;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Tests for <see cref="ProcessCommandRunner"/> against a real child process on this machine — the
/// actual probe REQ-FN-011 runs, not a fake, so a real "dotnet" and a real "not found" tool are both
/// exercised once.
/// </summary>
public sealed class ProcessCommandRunnerTests
{
    /// <summary>
    /// When probing the real <c>dotnet</c> on this build machine, then it starts, exits cleanly and
    /// prints a version string — this test project could not itself run without a real .NET SDK.
    /// </summary>
    [Fact]
    public async Task RunAsyncCapturesTheRealDotnetVersion()
    {
        var vRunner = new ProcessCommandRunner(NullLogger<ProcessCommandRunner>.Instance);

        var vResult = await vRunner.RunAsync("dotnet", "--version", default);

        Assert.True(vResult.Started);
        Assert.Equal(0, vResult.ExitCode);
        Assert.Matches(new Regex(@"^\d+\.\d+\.\d+"), vResult.StandardOutput.Trim());
    }

    /// <summary>
    /// When the named executable does not exist on this machine at all, then the runner reports it as
    /// not started rather than throwing — the "missing tool" path REQ-FN-011's table relies on.
    /// </summary>
    [Fact]
    public async Task RunAsyncReportsNotStartedForAMissingExecutable()
    {
        var vRunner = new ProcessCommandRunner(NullLogger<ProcessCommandRunner>.Instance);

        var vResult = await vRunner.RunAsync("chatur-tool-that-does-not-exist", "--version", default);

        Assert.False(vResult.Started);
    }
}
