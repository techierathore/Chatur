namespace Chatur.Core.Platform;

/// <summary>
/// Platform port over starting another program: the owner's chosen editor, the machine's default
/// application for a file, or a build/run command whose output streams back line by line
/// (Architecture §2, §7 "Build and run").
/// </summary>
public interface IProcessLauncher
{
    /// <summary>
    /// Opens a file in the program the owner chose as their editor.
    /// </summary>
    /// <param name="aFilePath">The absolute path of the file to open.</param>
    /// <param name="aCt">A token that cancels the launch.</param>
    Task OpenInEditorAsync(string aFilePath, CancellationToken aCt = default);

    /// <summary>
    /// Opens a file with whatever program the machine associates with it.
    /// </summary>
    /// <param name="aFilePath">The absolute path of the file to open.</param>
    /// <param name="aCt">A token that cancels the launch.</param>
    Task OpenWithDefaultAppAsync(string aFilePath, CancellationToken aCt = default);

    /// <summary>
    /// Runs a command to completion, streaming each output line as it is written.
    /// </summary>
    /// <param name="aCommand">The program to start.</param>
    /// <param name="aArguments">The arguments to pass it.</param>
    /// <param name="aWorkingDirectory">The directory the command runs in.</param>
    /// <param name="aOnOutputLine">Called once per line of standard output or error, as it arrives.</param>
    /// <param name="aCt">A token that stops the process and ends the run early.</param>
    /// <returns>The process's exit code.</returns>
    Task<int> RunAsync(
        string aCommand,
        string aArguments,
        string aWorkingDirectory,
        Action<string> aOnOutputLine,
        CancellationToken aCt = default);
}
