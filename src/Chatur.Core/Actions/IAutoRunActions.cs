namespace Chatur.Core.Actions;

/// <summary>
/// The end-to-end run (page End-to-end run, <c>/auto</c>; BRD-118 to BRD-125): one brief in, the whole
/// chain of processes run on its own through <see cref="IProcessActions"/> and
/// <see cref="IProcessRunActions"/>, until the work is ready for the owner's acceptance test. Every fact
/// about a run is in the database, so a run survives Chatur being closed. A run is started with
/// <see cref="StartAsync"/>, which returns at once and carries the chain on in the background; the screen
/// polls <see cref="GetAsync"/> or <see cref="CurrentAsync"/>. Step-by-step working through
/// <see cref="IProcessActions.StartAsync"/> is untouched (REQ-FN-063).
/// </summary>
public interface IAutoRunActions
{
    /// <summary>
    /// Starts an end-to-end run on a project with a brief and begins carrying the chain on in the
    /// background without being told each process (REQ-FN-061).
    /// </summary>
    /// <param name="aProjectId">The project to run on.</param>
    /// <param name="aBrief">What should be true when the run finishes; at least <see cref="AutoRunBrief.MinLength"/> characters once trimmed.</param>
    /// <param name="aCt">A token that cancels the start (not the run).</param>
    /// <returns>The new run, in state <see cref="AutoRunState.Running"/>.</returns>
    /// <exception cref="ArgumentException">The project is not known, or the brief is shorter than <see cref="AutoRunBrief.MinLength"/>.</exception>
    /// <exception cref="InvalidOperationException">A run is already going on the project.</exception>
    Task<AutoRunView> StartAsync(int aProjectId, string aBrief, CancellationToken aCt = default);

    /// <summary>
    /// Carries a run on in the foreground until it ends, is paused or stopped, or is ready for the
    /// owner's acceptance test, waiting out a usage limit and retrying a stall or a crash on the way
    /// (REQ-FN-061, REQ-FN-064, REQ-FN-065). A run that is paused, stopped or finished is returned
    /// unchanged. Safe to call after a restart: it continues from the saved process and step.
    /// </summary>
    /// <param name="aAutoRunId">The run to carry on.</param>
    /// <param name="aCt">A token that cancels the call; the run keeps its place.</param>
    /// <returns>The run as it stands afterwards, or <see langword="null"/> when there is no such run.</returns>
    Task<AutoRunView?> AdvanceAsync(int aAutoRunId, CancellationToken aCt = default);

    /// <summary>The run as it stands: the chain, what is happening now, the open requirements and the events so far (REQ-FN-061, REQ-FN-062).</summary>
    /// <param name="aAutoRunId">The run to read.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The run, or <see langword="null"/> when there is no such run.</returns>
    Task<AutoRunView?> GetAsync(int aAutoRunId, CancellationToken aCt = default);

    /// <summary>
    /// The chain a run would go through, with no run started: the eight processes in order and the agent
    /// that runs each (REQ-UI-055). The same chain <see cref="StartAsync"/> writes.
    /// </summary>
    IReadOnlyList<AutoRunChainStep> PlannedChain();

    /// <summary>The project's run that is going now (running, waiting out a limit or paused), or <see langword="null"/> when none is.</summary>
    /// <param name="aProjectId">The project to look at.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<AutoRunView?> CurrentAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>The project's end-to-end runs, newest first.</summary>
    /// <param name="aProjectId">The project to list.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<AutoRunSummary>> ListAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>Pauses a going run at the end of the step it is on; <see cref="ResumeAsync"/> carries on from that step.</summary>
    /// <param name="aAutoRunId">The run to pause.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <returns>The run afterwards (unchanged when it is not going), or <see langword="null"/> when there is no such run.</returns>
    Task<AutoRunView?> PauseAsync(int aAutoRunId, CancellationToken aCt = default);

    /// <summary>Carries a paused run on from the step it was paused on, in the background.</summary>
    /// <param name="aAutoRunId">The run to resume.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <returns>The run afterwards (unchanged when it is not paused), or <see langword="null"/> when there is no such run.</returns>
    Task<AutoRunView?> ResumeAsync(int aAutoRunId, CancellationToken aCt = default);

    /// <summary>Stops a run that is not over, as the owner asked; what it did stays in its record.</summary>
    /// <param name="aAutoRunId">The run to stop.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <returns>The run afterwards, or <see langword="null"/> when there is no such run.</returns>
    Task<AutoRunView?> StopAsync(int aAutoRunId, CancellationToken aCt = default);

    /// <summary>
    /// Marks a run complete after the owner's acceptance test (REQ-FN-066). Refused while any
    /// requirement of the project is still open, and while the run has not reached
    /// <see cref="AutoRunState.ReadyForAcceptance"/>.
    /// </summary>
    /// <param name="aAutoRunId">The run to mark.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <returns>The run, now <see cref="AutoRunState.Complete"/>, or <see langword="null"/> when there is no such run.</returns>
    /// <exception cref="InvalidOperationException">A requirement is still open (the message says how many), or the run is not ready for acceptance.</exception>
    Task<AutoRunView?> MarkCompleteAsync(int aAutoRunId, CancellationToken aCt = default);
}
