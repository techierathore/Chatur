namespace Chatur.Core.Platform;

/// <summary>
/// Platform port over where Chatur keeps its own files on this machine (Architecture §1 Q3, Q5 —
/// the database and the log file live in the application-data folder on a real machine, and the
/// web harness roots them under its own <c>bin</c> output instead).
/// </summary>
public interface IAppPaths
{
    /// <summary>The folder Chatur's own files live under on this machine.</summary>
    string AppDataFolder { get; }

    /// <summary>The full path of the SQLite database file.</summary>
    string DatabasePath { get; }

    /// <summary>The folder Serilog writes its rolling log files to.</summary>
    string LogFolder { get; }
}
