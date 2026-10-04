using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Chatur.Core.Prerequisites;

/// <summary>
/// <see cref="IExternalCommandRunner"/> over a real child process, with Homebrew's own directories
/// added to <c>PATH</c> on macOS so a probe started from Finder still finds a Homebrew-installed tool
/// (REQ-FN-012).
/// </summary>
public sealed class ProcessCommandRunner : IExternalCommandRunner
{
    private readonly ILogger<ProcessCommandRunner> objLogger;

    /// <summary>Creates the runner.</summary>
    /// <param name="aLogger">Where a probe failure other than "not found" is logged.</param>
    public ProcessCommandRunner(ILogger<ProcessCommandRunner> aLogger)
    {
        objLogger = aLogger;
    }

    /// <inheritdoc />
    public async Task<ExternalCommandResult> RunAsync(string aCommand, string aArguments, CancellationToken aCt = default)
    {
        var vStartInfo = new ProcessStartInfo(aCommand, aArguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        vStartInfo.Environment["PATH"] = HomebrewPath.Augment(
            Environment.GetEnvironmentVariable("PATH"), OperatingSystem.IsMacOS());

        try
        {
            using var vProcess = new Process { StartInfo = vStartInfo };
            vProcess.Start();

            var vOutputTask = vProcess.StandardOutput.ReadToEndAsync(aCt);
            var vErrorTask = vProcess.StandardError.ReadToEndAsync(aCt);
            await vProcess.WaitForExitAsync(aCt).ConfigureAwait(false);
            var vOutput = await vOutputTask.ConfigureAwait(false);
            var vError = await vErrorTask.ConfigureAwait(false);

            return new ExternalCommandResult(
                true, vProcess.ExitCode, string.IsNullOrWhiteSpace(vOutput) ? vError : vOutput);
        }
        catch (Win32Exception)
        {
            // The executable is not on PATH at all — an honest "not found" tool, not a fault to log.
            return new ExternalCommandResult(false, -1, string.Empty);
        }
        catch (Exception vEx) when (vEx is not OperationCanceledException)
        {
            objLogger.LogWarning(vEx, "Probing {Command} failed", aCommand);
            return new ExternalCommandResult(false, -1, string.Empty);
        }
    }
}
