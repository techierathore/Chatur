using System.Diagnostics;
using Chatur.Core.Platform;
using Microsoft.Extensions.Logging;

namespace Chatur.Platform;

/// <summary>
/// Opens addresses with the machine's default application and runs build/run commands, on
/// whichever real machine this is (Architecture §7 "Build and run").
/// </summary>
public sealed class MauiProcessLauncher : IProcessLauncher
{
    private readonly ILogger<MauiProcessLauncher> objLogger;

    /// <summary>
    /// Creates the launcher.
    /// </summary>
    /// <param name="aLogger">Where a failed launch is logged.</param>
    public MauiProcessLauncher(ILogger<MauiProcessLauncher> aLogger)
    {
        objLogger = aLogger;
    }

    /// <inheritdoc />
    public Task OpenWithDefaultAppAsync(string aFilePath, CancellationToken aCt = default)
    {
        // The machine's own association — real on both platforms, not stubbed, since it needs no
        // REQ-specific data (no "chosen editor" to read first).
#if WINDOWS
        Process.Start(new ProcessStartInfo(aFilePath) { UseShellExecute = true });
#elif MACCATALYST
        Process.Start("open", aFilePath);
#endif
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OpenBrowserAsync(string aUrl, CancellationToken aCt = default)
    {
        try
        {
#if WINDOWS
            Process.Start(new ProcessStartInfo(aUrl) { UseShellExecute = true });
#elif MACCATALYST
            Process.Start("open", aUrl);
#endif
        }
        catch (Exception vException) when (vException is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            objLogger.LogWarning(vException, "Could not open {Url} in the browser", aUrl);
        }

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
