using Chatur.Core.Actions;

namespace Chatur.Core.Processes;

/// <summary>One TechieFlow command in code: its words, its agent and the shape of its rows.</summary>
/// <param name="Code">The command.</param>
/// <param name="Description">What it does, in one line.</param>
/// <param name="Agent">The agent that carries it out.</param>
/// <param name="Shape">One letter per row: <c>S</c> a step, <c>s</c> a step with a check attached, <c>C</c> a check, <c>P</c> a place it stops for the owner.</param>
public sealed record ProcessDefinition(string Code, string Description, string Agent, string Shape)
{
    /// <summary>The number of rows.</summary>
    public int Steps => Shape.Length;

    /// <summary>The checks: every check row and every step with a check attached.</summary>
    public int Checks => Shape.Count(aLetter => aLetter is 'C' or 's');

    /// <summary>The places it stops for the owner.</summary>
    public int OwnerStops => Shape.Count(aLetter => aLetter == 'P');

    /// <summary>
    /// The 1-based row at which a verifying process writes its verdicts into the project's checklist
    /// (REQ-UI-048), or 0 for a process that does not verify.
    /// </summary>
    public int VerdictStep { get; init; }

    /// <summary>Whether the process grades requirements and writes verdicts.</summary>
    public bool IsVerifying => VerdictStep > 0;

    /// <summary>The kind of a 1-based row.</summary>
    /// <param name="aPosition">The row's position.</param>
    public ProcessStepKind KindAt(int aPosition) => Shape[aPosition - 1] switch
    {
        'C' => ProcessStepKind.Check,
        'P' => ProcessStepKind.OwnerStop,
        _ => ProcessStepKind.Step
    };

    /// <summary>Whether a check is attached to the 1-based row.</summary>
    /// <param name="aPosition">The row's position.</param>
    public bool HasCheckAt(int aPosition) => Shape[aPosition - 1] == 's';
}

/// <summary>
/// The twelve TechieFlow commands. The steps and checks are code and do not change; only the wording is read from <c>ProcessStepText</c>.
/// Phase 3 attached a check to the last step of build-phase, handoff-phase, refresh-status, mockups and metrics-report, because
/// without it "done" cannot be judged on disk (BRD-171, BRD-173, BRD-174, BRD-176, BRD-179).
/// </summary>
public static class ProcessCatalogue
{
    /// <summary>Every command, in the order TechieFlow runs them.</summary>
    public static readonly IReadOnlyList<ProcessDefinition> All =
    [
        new("day1-greenfield", "Idea to brief, BRD and checklist", "Analyst", "SPsCsPSCP"),
        new("split-brd", "Cuts one BRD into a phase each", "Architect", "SsCSCP"),
        new("build-phase", "Builds a phase from its checklist", "Flow master", "SsCsCSsCPsP"),
        new("verify-phase", "Checks the phase against every requirement", "Verifier", "SsCsCPS") { VerdictStep = 7 },
        new("fix-issues", "Works through what verification raised", "Flow master", "SSsCsCCP"),
        new("handoff-phase", "Closes a phase, opens the next", "Flow master", "sCPsP"),
        new("refresh-status", "Rewrites the status from the checklists", "Flow master", "SsCs"),
        new("deploy-checklist", "Lists what a release needs", "Architect", "SsCSCP"),
        new("mockups", "Draws the screens a phase needs", "Architect", "SsCsP"),
        new("amend-docs", "Puts a late change through every document", "Analyst", "SsCPsP"),
        new("triage-issues", "Sorts reports into the right checklist", "Verifier", "SsCsP"),
        new("metrics-report", "Reads the run records and reports", "Verifier", "SSCs")
    ];

    /// <summary>The command with this code, or <see langword="null"/>.</summary>
    /// <param name="aCode">The command, compared without regard to case.</param>
    public static ProcessDefinition? Find(string aCode) =>
        All.FirstOrDefault(aProcess => string.Equals(aProcess.Code, aCode, StringComparison.OrdinalIgnoreCase));
}
