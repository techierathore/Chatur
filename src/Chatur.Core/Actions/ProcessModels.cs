namespace Chatur.Core.Actions;

/// <summary>What a row of a process is: an ordinary step, a check that can stop the run, or a place it waits for the owner.</summary>
public enum ProcessStepKind
{
    /// <summary>A step the process does by itself.</summary>
    Step,

    /// <summary>A check; a failed one stops the run on that step.</summary>
    Check,

    /// <summary>A place the run stops until the owner answers.</summary>
    OwnerStop
}

/// <summary>One TechieFlow command as Processes lists it (REQ-FN-049).</summary>
/// <param name="Code">The command, e.g. <c>"verify-phase"</c>.</param>
/// <param name="Description">What it does, in one line.</param>
/// <param name="Agent">The agent that carries it out, e.g. <c>"Verifier"</c>.</param>
/// <param name="Steps">The number of steps, every row of the process counted.</param>
/// <param name="Checks">The number of checks, including a check attached to a step.</param>
/// <param name="OwnerStops">The number of places it stops for the owner.</param>
/// <param name="LastRunUtc">When it was last started on this project, or <see langword="null"/> when never.</param>
/// <param name="IsRunning">Whether a run of it on this project is running now.</param>
/// <param name="RunningRunId">The running run's id when <paramref name="IsRunning"/>, otherwise <see langword="null"/>.</param>
public sealed record ProcessSummary(
    string Code,
    string Description,
    string Agent,
    int Steps,
    int Checks,
    int OwnerStops,
    DateTime? LastRunUtc,
    bool IsRunning,
    int? RunningRunId);

/// <summary>One row of a process, with its wording read from the database (REQ-UI-045, REQ-UI-046).</summary>
/// <param name="Position">The 1-based position in the process.</param>
/// <param name="StepCode">The code the wording is filed under, <c>"s1"</c>, <c>"s2"</c> …</param>
/// <param name="Wording">The step's wording as the database holds it now.</param>
/// <param name="Kind">Whether this row is a step, a check or an owner stop.</param>
/// <param name="HasCheck">Whether a check is attached to this step (counted in <see cref="ProcessSummary.Checks"/>).</param>
public sealed record ProcessStepInfo(int Position, string StepCode, string Wording, ProcessStepKind Kind, bool HasCheck);

/// <summary>A process that has been started on a project (REQ-FN-050).</summary>
/// <param name="RunId">The run's id; Process run opens at <c>/processes/{RunId}/run</c>.</param>
/// <param name="ProcessCode">The process started.</param>
/// <param name="ProjectId">The project it runs on.</param>
/// <param name="State">The run's state; <c>"running"</c> from the moment it starts.</param>
/// <param name="CurrentStep">The 1-based step the run is on; 1 at the start.</param>
/// <param name="FirstStepWording">The first step's wording, so the screen can show it at once.</param>
/// <param name="StartedUtc">When the run started.</param>
/// <param name="AlreadyRunning"><see langword="true"/> when the process was already running on this project and that run is returned instead of a second one.</param>
public sealed record ProcessRunStarted(
    int RunId,
    string ProcessCode,
    int ProjectId,
    string State,
    int CurrentStep,
    string FirstStepWording,
    DateTime StartedUtc,
    bool AlreadyRunning);

/// <summary>The three documents a process works on in a project (REQ-FN-051).</summary>
/// <param name="Brd">Project-relative path of the BRD, or <see langword="null"/> when the project has none yet.</param>
/// <param name="Checklist">Project-relative path of the checklist, or <see langword="null"/> when none yet.</param>
/// <param name="Status">Project-relative path of the status document.</param>
public sealed record ProcessDocuments(string? Brd, string? Checklist, string Status);
