namespace Chatur.Core.Actions;

/// <summary>Where a whole run stands. A process is started by <c>IProcessActions.StartAsync</c>, which writes <see cref="Running"/>.</summary>
public enum ProcessRunState
{
    /// <summary>The run has a step to carry out.</summary>
    Running,

    /// <summary>The run is at an owner stop and does nothing until the owner answers.</summary>
    Waiting,

    /// <summary>A check failed, or the owner stopped it; the run ended.</summary>
    Stopped,

    /// <summary>Every step is done.</summary>
    Finished
}

/// <summary>Where one step of a run stands.</summary>
public enum ProcessStepState
{
    /// <summary>Not reached yet.</summary>
    ToCome,

    /// <summary>The step the run is on.</summary>
    Running,

    /// <summary>The step is an owner stop, waiting for the answer.</summary>
    Waiting,

    /// <summary>The step finished and carries its result.</summary>
    Done,

    /// <summary>The step is a check that failed.</summary>
    Failed
}

/// <summary>The owner's answer to an owner stop.</summary>
public enum ProcessAnswer
{
    /// <summary>Agreed; the run carries on.</summary>
    Yes,

    /// <summary>Refused; the run carries on and the step records the refusal.</summary>
    No,

    /// <summary>Not now; the run stays at the stop.</summary>
    Later
}

/// <summary>One step of a run as Process run shows it (REQ-FN-052).</summary>
/// <param name="Position">The step's place in the process, from 1.</param>
/// <param name="StepCode">The code the wording is filed under, <c>"s1"</c>, <c>"s2"</c> …</param>
/// <param name="Wording">The step's wording as the database holds it now.</param>
/// <param name="Kind">Whether the row is a step, a check or an owner stop.</param>
/// <param name="State">Where the step stands.</param>
/// <param name="Result">What came of the step; the reason, for a failed check. <see langword="null"/> until the step has finished.</param>
/// <param name="Answer">For an owner stop, the owner's answer once given.</param>
/// <param name="Work">For a step handed to an agent (REQ-FN-084), what the agent is doing or did; <see langword="null"/> for a step nobody was handed (a check, an owner stop, or a step not reached).</param>
public sealed record ProcessRunStepView(
    int Position,
    string StepCode,
    string Wording,
    ProcessStepKind Kind,
    ProcessStepState State,
    string? Result,
    string? Answer,
    ProcessStepWork? Work = null);

/// <summary>One run of one process with its steps (REQ-FN-052).</summary>
/// <param name="RunId">The run's id.</param>
/// <param name="ProjectId">The project the run works on.</param>
/// <param name="ProcessCode">The process being run, e.g. <c>verify-phase</c>.</param>
/// <param name="State">Where the run stands.</param>
/// <param name="StartedUtc">When the run began.</param>
/// <param name="EndedUtc">When the run finished or stopped; <see langword="null"/> while it has not.</param>
/// <param name="CurrentPosition">The step the run is on: the running or waiting one, or the one a check failed on; <see langword="null"/> when the run finished.</param>
/// <param name="Steps">Every step, in order.</param>
public sealed record ProcessRunView(
    int RunId,
    int ProjectId,
    string ProcessCode,
    ProcessRunState State,
    DateTime StartedUtc,
    DateTime? EndedUtc,
    int? CurrentPosition,
    IReadOnlyList<ProcessRunStepView> Steps);

/// <summary>One line of a project's run history, for the "Earlier on this project" list.</summary>
/// <param name="RunId">The run's id.</param>
/// <param name="ProcessCode">The process that was run.</param>
/// <param name="State">Where the run stands.</param>
/// <param name="StartedUtc">When the run began.</param>
/// <param name="CurrentPosition">The step the run is on or ended on; <see langword="null"/> when it finished.</param>
public sealed record ProcessRunSummary(
    int RunId,
    string ProcessCode,
    ProcessRunState State,
    DateTime StartedUtc,
    int? CurrentPosition);

/// <summary>What carrying out a step came to.</summary>
/// <param name="Passed"><see langword="false"/> only for a check that failed.</param>
/// <param name="Result">What to show against the step; for a failed check, which check failed and why.</param>
/// <param name="StoppedByOwner"><see langword="true"/> when the owner pressed Stop while the step's agent was working (REQ-FN-090); the run ends there.</param>
public sealed record ProcessStepOutcome(bool Passed, string Result, bool StoppedByOwner = false);

/// <summary>How one test came out when the run ran the project's tests (REQ-UI-048).</summary>
public enum ProcessTestOutcome
{
    /// <summary>The test ran and passed.</summary>
    Passed,

    /// <summary>The test ran and failed, or errored.</summary>
    Failed,

    /// <summary>The test did not run (skipped, not executed or inconclusive); it does not count as passed.</summary>
    Skipped
}

/// <summary>One test the run saw that carries a requirement's id, with how it came out (REQ-UI-048).</summary>
/// <param name="Name">The test's name as its tool reported it (display name or title).</param>
/// <param name="Outcome">How it came out.</param>
/// <param name="Message">The failure message, or <see langword="null"/> when it did not fail.</param>
public sealed record ProcessRunTestResult(string Name, ProcessTestOutcome Outcome, string? Message);

/// <summary>
/// The test evidence behind one verdict: what the UI shows beside it (REQ-UI-048).
/// </summary>
/// <param name="TestsRan"><see langword="true"/> when the project's tests ran far enough to report results; <see langword="false"/> when they could not run or the project has none.</param>
/// <param name="TestsFound">How many tests carrying the requirement's id the run saw.</param>
/// <param name="TestsPassed">How many of them passed.</param>
/// <param name="TestsFailed">How many of them failed; a skipped test is neither passed nor failed.</param>
/// <param name="Tests">Each of those tests, in the order the tools reported them.</param>
public sealed record ProcessRunVerdictEvidence(
    bool TestsRan,
    int TestsFound,
    int TestsPassed,
    int TestsFailed,
    IReadOnlyList<ProcessRunTestResult> Tests);

/// <summary>One verdict a verifying run wrote into the project's checklist table (REQ-UI-048).</summary>
/// <param name="RequirementId">The requirement graded, e.g. <c>REQ-FN-034</c>.</param>
/// <param name="ChecklistFile">The checklist the verdict is in, project-relative, e.g. <c>docs/App-P2-Checklist.md</c>.</param>
/// <param name="Status">The status written into the row, in the checklist's own word.</param>
/// <param name="Percent">The percentage written into the row.</param>
/// <param name="Remark">The dated remark appended to the row.</param>
/// <param name="Evidence">The tests behind the verdict; <see langword="null"/> only for a verdict recorded before test evidence was kept.</param>
public sealed record ProcessRunVerdict(
    string RequirementId,
    string ChecklistFile,
    string Status,
    int Percent,
    string Remark,
    ProcessRunVerdictEvidence? Evidence = null);

/// <summary>Where a step handed to an agent stands (REQ-FN-084, REQ-FN-087, REQ-FN-090).</summary>
public enum ProcessStepWorkState
{
    /// <summary>The agent's session is open and it is working.</summary>
    Working,

    /// <summary>The agent finished; its final message is recorded and the step has ended.</summary>
    Finished,

    /// <summary>The owner pressed Stop; the agent's session was ended and the step reads "stopped by the owner". A step in this state is shown as <see cref="ProcessStepState.Failed"/> with that result.</summary>
    Stopped
}

/// <summary>
/// What an agent did on one step handed to it, kept per run and step (REQ-FN-084..087, REQ-FN-090). The lists grow
/// while <see cref="State"/> is <see cref="ProcessStepWorkState.Working"/>, so Process run can show them as they happen (BRD-167).
/// </summary>
/// <param name="SessionId">The agent session the step was handed to; the conversation panel shows it under <paramref name="AgentName"/>.</param>
/// <param name="AgentName">The process's agent, e.g. <c>Flow master</c>.</param>
/// <param name="ModelName">The model the agent's session uses, named when the step starts and replaced by the one that answered; <see langword="null"/> when no model is connected.</param>
/// <param name="Tier">The tier of the agent's role, 1 to 3, that the model was routed from.</param>
/// <param name="State">Where the step stands.</param>
/// <param name="Task">The text the agent was given: the step's wording, the run and the project.</param>
/// <param name="FilesWritten">Project-relative paths the agent wrote, in order.</param>
/// <param name="FilesProposed">Paths the agent proposed that wait for the owner's approval (session mode ask-first); not written.</param>
/// <param name="CommandsRun">Commands the agent ran, e.g. <c>Build the project.</c>.</param>
/// <param name="Refusals">Each tool the agent asked for that its rights or the source-control refusal withheld, with the reason.</param>
/// <param name="FinalMessage">The agent's last message; <see langword="null"/> until it finishes.</param>
public sealed record ProcessStepWork(
    int SessionId,
    string AgentName,
    string? ModelName,
    int Tier,
    ProcessStepWorkState State,
    string Task,
    IReadOnlyList<string> FilesWritten,
    IReadOnlyList<string> FilesProposed,
    IReadOnlyList<string> CommandsRun,
    IReadOnlyList<string> Refusals,
    string? FinalMessage)
{
    /// <summary>Whether the owner stopped the agent (REQ-FN-090).</summary>
    public bool StoppedByOwner => State == ProcessStepWorkState.Stopped;
}
