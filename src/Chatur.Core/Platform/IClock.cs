namespace Chatur.Core.Platform;

/// <summary>
/// Platform port over the current time, so a test can supply a fixed clock instead of the real one
/// (Coding Standards §Testability).
/// </summary>
public interface IClock
{
    /// <summary>The current instant, in UTC.</summary>
    DateTime UtcNow { get; }
}
