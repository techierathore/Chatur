using System.Diagnostics;
using Chatur.Core.Platform;
using Microsoft.Extensions.Logging;

namespace Chatur.WebHarness.Services;

/// <summary>
/// Logs a request to open a file — the browser checks run headless, with no editor or default
/// application to open — but really runs a build or run command (REQ-UI-012 through REQ-UI-016 need
/// a genuine <c>dotnet build</c>/<c>dotnet run</c> to smoke-test against, not a logged pretend one;
/// this harness is an ordinary server process on the real machine and can start one).
/// </summary>
public sealed class HarnessProcessLauncher : IProcessLauncher
{
    private readonly ILogger<HarnessProcessLauncher> objLogger;

    /// <summary>
    /// Creates the launcher.
    /// </summary>
    /// <param name="aLogger">Where every request is logged.</param>
    public HarnessProcessLauncher(ILogger<HarnessProcessLauncher> aLogger)
    {
        objLogger = aLogger;
    }

    /// <inheritdoc />
    public Task OpenInEditorAsync(string aFilePath, CancellationToken aCt = default)
    {
        objLogger.LogInformation("Would open {FilePath} in the owner's editor.", aFilePath);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OpenWithDefaultAppAsync(string aFilePath, CancellationToken aCt = default)
    {
        objLogger.LogInformation("Would open {FilePath} with the machine's default application.", aFilePath);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<int> RunAsync(
        string aCommand,
        string aArguments,
        string aWorkingDirectory,
        Action<string> aOnOutputLine,
        CancellationToken aCt = default)
    {
        using var vProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = aCommand,
                Arguments = aArguments,
                WorkingDirectory = aWorkingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        vProcess.OutputDataReceived += (_, aArgs) =>
        {
            if (aArgs.Data is not null)
            {
                aOnOutputLine(aArgs.Data);
            }
        };
        vProcess.ErrorDataReceived += (_, aArgs) =>
        {
            if (aArgs.Data is not null)
            {
                aOnOutputLine(aArgs.Data);
            }
        };

        try
        {
            vProcess.Start();
            vProcess.BeginOutputReadLine();
            vProcess.BeginErrorReadLine();
            await vProcess.WaitForExitAsync(aCt).ConfigureAwait(false);
            return vProcess.ExitCode;
        }
        catch (OperationCanceledException)
        {
            if (!vProcess.HasExited)
            {
                vProcess.Kill(entireProcessTree: true);
            }

            throw;
        }
        catch (Exception aException)
        {
            objLogger.LogError(aException, "Failed to run {Command} {Arguments}", aCommand, aArguments);
            throw;
        }
    }
}
