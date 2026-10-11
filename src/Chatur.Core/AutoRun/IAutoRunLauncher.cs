namespace Chatur.Core.AutoRun;

/// <summary>Carries a run on in the background after it was started or resumed, so the screen does not wait for it (REQ-FN-061).</summary>
public interface IAutoRunLauncher
{
    /// <summary>Starts carrying the run on; does nothing when it is already being carried on.</summary>
    /// <param name="aAutoRunId">The run to carry on.</param>
    void Launch(int aAutoRunId);
}
