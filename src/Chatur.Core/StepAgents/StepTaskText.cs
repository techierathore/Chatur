using System.Globalization;
using Chatur.Core.Processes;

namespace Chatur.Core.StepAgents;

/// <summary>
/// The words a handed step gives its agent (REQ-FN-084): the step's wording, the run, the project, and what the
/// process's checks will read on disk afterwards, so the agent knows what "done" is before it starts (REQ-FN-088).
/// </summary>
public static class StepTaskText
{
    /// <summary>The description of the tool call that builds the project, as the session records it; a command the step ran starts with this.</summary>
    public const string BuildCommand = "Build the project.";

    /// <summary>The task text for a step.</summary>
    /// <param name="aDefinition">The process.</param>
    /// <param name="aRunId">The run.</param>
    /// <param name="aPosition">The step's 1-based position.</param>
    /// <param name="aWording">The step's wording as the database holds it now.</param>
    /// <param name="aProjectName">The project's name.</param>
    /// <param name="aProjectPath">The project's folder.</param>
    public static string Build(ProcessDefinition aDefinition, int aRunId, int aPosition, string aWording, string aProjectName, string aProjectPath)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"""
            You have been handed one step of a Chatur process run.

            Process: {aDefinition.Code} (run {aRunId}) — {aDefinition.Description}
            Step: {aPosition} of {aDefinition.Steps} — {aWording}
            Project: {aProjectName}{(aProjectPath.Length == 0 ? string.Empty : " at " + aProjectPath)}

            Do this step in the project: read what you need with read-file, write files with edit-file (paths relative to the project folder), and build with run-build when the step asks for it. Source-control commands are refused. When the step is done, end with a short message saying what you did and which files you wrote.

            When the process's checks run they read the project's files on disk, not your message. {Expectation(aDefinition.Code)}
            """);
    }

    /// <summary>The message that carries an interrupted step on in the same session.</summary>
    public static string Resume() =>
        "The step was interrupted (a usage limit, a stall, or Chatur closing). Carry on where you left off and finish it; do not start over.";

    /// <summary>What the checks of a process read on disk, in the words the agent needs to satisfy them.</summary>
    /// <param name="aProcessCode">The process.</param>
    public static string Expectation(string aProcessCode) => aProcessCode.ToLowerInvariant() switch
    {
        "day1-greenfield" => "They look for: a brief in docs/ (a file name containing \"Brief\") that names an audience, a problem and a measure of success; a BRD (\"-BRD.md\") whose every BRD-n item has an *Acceptance:* line and whose ids are not repeated; an Architecture document, a UI design document (\"UIDesign\") with a \"### Screen:\" section per screen linking its mockup, an HTML mockup per screen in docs/mockups/ with data-testid anchors; and a checklist (\"-Checklist.md\") with a REQ row for every BRD id (the row's detail line names \"BRD-n\") and a mockup that exists for every REQ-UI row.",
        "split-brd" => "They look for: the Phases file (\"Phases\") giving each phase a row with a BRD range such as \"BRD-1 to BRD-95, BRD-156 to BRD-158\"; one BRD (\"-Pn-BRD.md\", or \"-BRD.md\" for phase 1) and one checklist per phase; every BRD id in exactly one phase's range and defined in that phase's BRD.",
        "build-phase" => "They look for: the newest checklist's open rows (Not Started, In Progress, Partial) all built and recorded Implemented; Chatur's own build of the project with no error and no warning; Chatur's own run of the tests with every test passing; and a test under tests/ that carries the id of each Implemented row.",
        "fix-issues" => "They look for: Chatur's own build of the project with no error and no warning; Chatur's own run of the tests, all passing; and no row left FAIL or Needs re-verify in the checklists — each fixed row recorded Implemented again.",
        "handoff-phase" => "They look for: no open row in the phase's checklist (only Verified, Done (pre-existing), N/A, or Blocked with a reason in Remarks); PROJECT-STATUS.md carrying a hand-off entry that mentions \"hand-off\" and today's date (yyyy-MM-dd); and the Phases file marking the phase \"done\".",
        "refresh-status" => "They look for: PROJECT-STATUS.md written during this run, with a count for each status in a table row such as \"| Implemented | 7 |\" (or \"Implemented: 7\"), equal to the number of checklist rows with that status across all the checklists.",
        "deploy-checklist" => "They look for: a deployment checklist written during this run (docs/ file name containing \"Deploy\" and \"Checklist\") with a heading for each of: Before, Deploy, After, Rollback, Secrets, Proven; every item a checkbox line (\"- [ ] ...\") that names who does it with \"Who: <name>\"; and mentions of the pipeline, secrets and versions.",
        "mockups" => "They look for: every \"### Screen:\" section of the phase's UI design linking a mockup in docs/mockups/ that exists and carries data-testid anchors; each section describing its regions and its empty, loading and error states; and every REQ-UI row of the checklist naming a mockup that exists.",
        "amend-docs" => "They look for: step 2's final message naming each document the change touches by its project-relative path (for example docs/Project-BRD.md); each named document changed during the run; no BRD id renumbered or deleted (a removed one is struck through, its id kept); and every new BRD id given a checklist row whose detail line names \"BRD-n\" and carries an *Acceptance:* line.",
        "triage-issues" => "They look for: every report in docs/issues/ (.md or .txt) matched to a checklist row — a report that names a REQ id reopens that row (status Not Started, In Progress, FAIL or Needs re-verify), and any other report becomes a new Not Started row whose requirement text contains the report's title and which has an *Acceptance:* line; and no file under src/ or tests/ changed.",
        "metrics-report" => "They look for: a report written during this run (docs/ file name containing \"Metrics\" and \"Report\") with lines \"Runs: N\", \"Tokens: N\" and \"Failures: N\" equal to the totals added up from docs/metrics/*.jsonl — runs is the number of records in runs.jsonl, tokens is the sum of tokens_in and tokens_out over runs.jsonl, failures is the number of runs.jsonl records with build_result \"fail\" plus gates.jsonl records with verdict \"FAIL\".",
        _ => "They judge the result on disk."
    };
}
