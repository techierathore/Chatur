using System.ComponentModel;
using System.Diagnostics;
using Chatur.Core.Prerequisites;
using Microsoft.Extensions.Logging;

namespace Chatur.Core.ProcessRuns;

/// <summary>
/// <see cref="IProjectCommandRunner"/> over a real child process. Standard input is closed so a tool
/// that asks a question fails instead of waiting for an answer nobody can give; on a time limit or a
/// cancel the whole process tree is stopped, so a build server or browser the command started does not
/// outlive it.
/// </summary>
public sealed class ProjectCommandRunner : IProjectCommandRunner
{
    private readonly ILogger<ProjectCommandRunner> objLogger;

    /// <summary>Creates the runner.</summary>
    /// <param name="aLogger">Where a failure other than "program not found" is logged.</param>
    public ProjectCommandRunner(ILogger<ProjectCommandRunner> aLogger)
    {
        objLogger = aLogger;
    }

    /// <inheritdoc />
    public async Task<ProjectCommandResult> RunAsync(ProjectCommand aCommand, CancellationToken aCt = default)
    {
        var vWatch = Stopwatch.StartNew();
        var vTail = new OutputTail(aCommand.MaxOutputChars);
        Process? vProcess;
        try
        {
            vProcess = Process.Start(StartInfoFor(aCommand));
        }
        catch (Exception aEx) when (aEx is Win32Exception or InvalidOperationException or DirectoryNotFoundException)
        {
            objLogger.LogInformation(aEx, "{Command} could not be started.", aCommand.FileName);
            return new ProjectCommandResult(false, -1, false, string.Empty, $"{aCommand.FileName} could not be started: {aEx.Message}", vWatch.Elapsed);
        }

        if (vProcess is null)
        {
            return new ProjectCommandResult(false, -1, false, string.Empty, $"{aCommand.FileName} could not be started.", vWatch.Elapsed);
        }

        using (vProcess)
        {
            vProcess.OutputDataReceived += (aSender, aArgs) => AddLine(vTail, aArgs.Data);
            vProcess.ErrorDataReceived += (aSender, aArgs) => AddLine(vTail, aArgs.Data);
            vProcess.BeginOutputReadLine();
            vProcess.BeginErrorReadLine();
            vProcess.StandardInput.Close();
            return await WaitAsync(vProcess, aCommand, vTail, vWatch, aCt).ConfigureAwait(false);
        }
    }

    private static void AddLine(OutputTail aTail, string? aLine)
    {
        if (aLine is not null)
        {
            aTail.Add(aLine);
        }
    }

    private static ProcessStartInfo StartInfoFor(ProjectCommand aCommand)
    {
        var vInfo = new ProcessStartInfo(aCommand.FileName, aCommand.Arguments)
        {
            WorkingDirectory = aCommand.WorkingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        vInfo.Environment["PATH"] = HomebrewPath.Augment(Environment.GetEnvironmentVariable("PATH"), OperatingSystem.IsMacOS());
        // A run started from inside another test run must not inherit its test host's connection.
        foreach (var vKey in vInfo.Environment.Keys.Where(aKey => aKey.StartsWith("VSTEST_", StringComparison.OrdinalIgnoreCase) || aKey.StartsWith("TESTINGPLATFORM_", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            vInfo.Environment.Remove(vKey);
        }

        vInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        vInfo.Environment["DOTNET_NOLOGO"] = "1";
        vInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        foreach (var vPair in aCommand.Environment ?? new Dictionary<string, string>())
        {
            vInfo.Environment[vPair.Key] = vPair.Value;
        }

        return vInfo;
    }

    private static async Task<ProjectCommandResult> WaitAsync(Process aProcess, ProjectCommand aCommand, OutputTail aTail, Stopwatch aWatch, CancellationToken aCt)
    {
        using var vLimit = CancellationTokenSource.CreateLinkedTokenSource(aCt);
        vLimit.CancelAfter(aCommand.Timeout);
        try
        {
            await aProcess.WaitForExitAsync(vLimit.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            StopTree(aProcess);
            if (aCt.IsCancellationRequested)
            {
                throw;
            }

            return new ProjectCommandResult(true, -1, true, aTail.ToString(), null, aWatch.Elapsed);
        }

        return new ProjectCommandResult(true, aProcess.ExitCode, false, aTail.ToString(), null, aWatch.Elapsed);
    }

    private static void StopTree(Process aProcess)
    {
        try
        {
            if (!aProcess.HasExited)
            {
                aProcess.Kill(true);
            }
        }
        catch (Exception aEx) when (aEx is InvalidOperationException or Win32Exception)
        {
            // The process ended between the check and the kill; there is nothing left to stop.
        }
    }
}
