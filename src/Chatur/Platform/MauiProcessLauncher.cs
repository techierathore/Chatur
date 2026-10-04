using System.Data.Common;
using System.Diagnostics;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Microsoft.Extensions.Logging;

namespace Chatur.Platform;

/// <summary>
/// Launches the owner's editor, the machine's default application, and build/run commands, on
/// whichever real machine this is (REQ-FN-045, REQ-FN-046, Architecture §7 "Build and run").
/// </summary>
public sealed class MauiProcessLauncher : IProcessLauncher
{
    /// <summary>
    /// The <c>Setting.Key</c> the owner's chosen editor command is filed under. No Settings tab in
    /// this phase 1 checklist offers a control for it yet, so it is read straight from the generic
    /// <c>Setting</c> table — the same table an eventual Settings row would write to — with the
    /// <c>VISUAL</c>/<c>EDITOR</c> environment variables and then the machine's own default
    /// application as graceful fallbacks, so the request still succeeds today.
    /// </summary>
    public const string EditorCommandSettingKey = "Editor.Command";

    private readonly ILogger<MauiProcessLauncher> objLogger;
    private readonly IDbConnectionFactory objDb;

    /// <summary>
    /// Creates the launcher.
    /// </summary>
    /// <param name="aLogger">Where a failed launch is logged.</param>
    /// <param name="aDb">Opens connections to Chatur's own database, to read the owner's chosen editor.</param>
    public MauiProcessLauncher(ILogger<MauiProcessLauncher> aLogger, IDbConnectionFactory aDb)
    {
        objLogger = aLogger;
        objDb = aDb;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reads <see cref="EditorCommandSettingKey"/> from the <c>Setting</c> table; when nothing is
    /// there, falls back to the <c>VISUAL</c>/<c>EDITOR</c> environment variables, and finally to the
    /// machine's own default application, so the owner always gets a program rather than a refusal
    /// (REQ-FN-045).
    /// </remarks>
    public async Task OpenInEditorAsync(string aFilePath, CancellationToken aCt = default)
    {
        var vEditorCommand = await ReadEditorCommandAsync(aCt).ConfigureAwait(false)
            ?? Environment.GetEnvironmentVariable("VISUAL")
            ?? Environment.GetEnvironmentVariable("EDITOR");

        if (string.IsNullOrWhiteSpace(vEditorCommand))
        {
            await OpenWithDefaultAppAsync(aFilePath, aCt).ConfigureAwait(false);
            return;
        }

        try
        {
            using var vProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = vEditorCommand,
                    Arguments = $"\"{aFilePath}\"",
                    UseShellExecute = true
                }
            };
            vProcess.Start();
        }
        catch (Exception vException)
        {
            objLogger.LogError(vException, "Failed to open {FilePath} in the chosen editor {Editor}", aFilePath, vEditorCommand);
            throw;
        }
    }

    private async Task<string?> ReadEditorCommandAsync(CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText = "SELECT Value FROM Setting WHERE Key = @Key;";
        var vParameter = vCommand.CreateParameter();
        vParameter.ParameterName = "@Key";
        vParameter.Value = EditorCommandSettingKey;
        vCommand.Parameters.Add(vParameter);

        var vResult = vCommand is DbCommand vDbCommand
            ? await vDbCommand.ExecuteScalarAsync(aCt).ConfigureAwait(false)
            : vCommand.ExecuteScalar();

        return vResult as string;
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
