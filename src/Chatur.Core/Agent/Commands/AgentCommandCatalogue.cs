using System.Text;
using System.Text.RegularExpressions;

namespace Chatur.Core.Agent.Commands;

/// <summary>How Chatur carries a command out (BRD-249, REQ-FN-311).</summary>
public enum AgentCommandKind
{
    /// <summary>Chatur does it alone and answers at once; no model is asked.</summary>
    Chatur,

    /// <summary>Chatur starts a run of its own process engine (the steps, the checks on disk, the owner's stops).</summary>
    Process,

    /// <summary>Chatur gives the agent the command's steps, the agent does the work with Chatur's tools, and Chatur checks the result on disk.</summary>
    Guided
}

/// <summary>One command an agent has.</summary>
/// <param name="RoleCode">The agent's code: <c>analyst</c>, <c>architect</c>, <c>flow-master</c> or <c>verifier</c>.</param>
/// <param name="Code">The command without its star, e.g. <c>brainstorm</c>.</param>
/// <param name="Arguments">What follows the command, as the person types it, e.g. <c>{topic}</c>; empty when it takes nothing.</param>
/// <param name="Description">What it does, in one plain line.</param>
/// <param name="Kind">How Chatur carries it out.</param>
/// <param name="Target">The process it starts (<see cref="AgentCommandKind.Process"/>), or the file it writes (<see cref="AgentCommandKind.Guided"/>, with <c>{App}</c> for the project's name).</param>
public sealed record AgentCommand(string RoleCode, string Code, string Arguments, string Description, AgentCommandKind Kind, string? Target = null)
{
    /// <summary>The command as the person types it: <c>*brainstorm {topic}</c>.</summary>
    public string Usage => Arguments.Length == 0 ? "*" + Code : $"*{Code} {Arguments}";
}

/// <summary>
/// Every command of the four agents, in code (BRD-249, REQ-FN-311; Architecture §6, 2026-11-22). Each agent carries every
/// command its TechieFlow agent has (<c>.tfcore/agents/analyst.md</c>, <c>architect.md</c>, <c>flow-master.md</c>,
/// <c>verifier.md</c> of TechieFlow, read 22 Nov 2026), done by Chatur itself: no TechieFlow script is called. The greeting,
/// <c>*help</c>, the answer to "what commands do you have" and the agent's instructions are all built from this one list
/// (BRD-241, REQ-FN-308), so an agent never says it lacks a command it has.
/// </summary>
public static partial class AgentCommandCatalogue
{
    /// <summary>The four agents whose commands this list holds.</summary>
    public static readonly IReadOnlyList<string> RoleCodes = ["analyst", "architect", "flow-master", "verifier"];

    /// <summary>Every command, agent by agent, in the order the TechieFlow agent lists them.</summary>
    public static readonly IReadOnlyList<AgentCommand> All =
    [
        // The analyst (TechieFlow analyst.md).
        new("analyst", "help", string.Empty, "Lists these commands, numbered.", AgentCommandKind.Chatur),
        new("analyst", "day1-brownfield", "{App}", "Day 1 for a project that already has code: reads the code and its documents and writes the Architecture, BRD, Coding Standards, checklist, status, UsageGuide and DevGuide.", AgentCommandKind.Guided, "docs/{App}-BRD.md"),
        new("analyst", "day1-greenfield", "{App}", "Day 1 for a new project: brief, BRD, Architecture, mockups, Coding Standards, checklist and status, as a process run with its checks.", AgentCommandKind.Process, "day1-greenfield"),
        new("analyst", "mockups", "{App} [--update]", "Draws the UI design and one HTML mockup per screen, as a process run with its checks.", AgentCommandKind.Process, "mockups"),
        new("analyst", "split-brd", "{App}", "Splits the BRD into the checklist (and into phases when the project is large), as a process run with its checks.", AgentCommandKind.Process, "split-brd"),
        new("analyst", "brainstorm", "{topic}", "Holds a brainstorming session with you, one technique at a time, and saves it to docs/brainstorming-session-results.md.", AgentCommandKind.Guided, "docs/brainstorming-session-results.md"),
        new("analyst", "amend-docs", "{App} {change}", "Folds a change into the BRD, Architecture, mockups and checklist in place, as a process run with its checks.", AgentCommandKind.Process, "amend-docs"),
        new("analyst", "create-project-brief", "{App}", "Writes docs/{App}-Brief.md, the one page day 1 reads: what it is, who it is for, must do, out of scope, open questions.", AgentCommandKind.Guided, "docs/{App}-Brief.md"),
        new("analyst", "doc-out", string.Empty, "Writes the whole document drafted in this conversation to its file.", AgentCommandKind.Guided),
        new("analyst", "research-prompt", "{topic}", "Writes a deep research prompt for a topic to docs/{App}-Research-Prompt.md.", AgentCommandKind.Guided, "docs/{App}-Research-Prompt.md"),
        new("analyst", "yolo", string.Empty, "Turns Go ahead on or off for this conversation: on, nothing waits for your approval.", AgentCommandKind.Chatur),
        new("analyst", "exit", string.Empty, "Says goodbye and closes this conversation.", AgentCommandKind.Chatur),

        // The architect (TechieFlow architect.md).
        new("architect", "help", string.Empty, "Lists these commands, numbered.", AgentCommandKind.Chatur),
        new("architect", "doc-out", string.Empty, "Writes the whole document drafted in this conversation to its file.", AgentCommandKind.Guided),
        new("architect", "research", "{topic}", "Writes a deep research prompt for a topic to docs/{App}-Research-Prompt.md.", AgentCommandKind.Guided, "docs/{App}-Research-Prompt.md"),
        new("architect", "yolo", string.Empty, "Turns Go ahead on or off for this conversation: on, nothing waits for your approval.", AgentCommandKind.Chatur),
        new("architect", "exit", string.Empty, "Says goodbye and closes this conversation.", AgentCommandKind.Chatur),
        new("architect", "architect", "{App}", "Designs the solution structure and the data model and writes docs/{App}-Architecture.md (Chatur's own, kept from phase 1).", AgentCommandKind.Guided, "docs/{App}-Architecture.md"),
        new("architect", "create-doc", "{document}", "Writes a document from a template (Chatur's own, kept from phase 1).", AgentCommandKind.Guided),

        // The flow master (TechieFlow flow-master.md).
        new("flow-master", "develop-end-to-end", "{App} [{brief}] [--no-git] [--model <id>] | --follow | --stop | --resume", "Takes the project to the owner's acceptance test unattended, step after step; --follow says where it is, --stop ends it, --resume carries it on.", AgentCommandKind.Chatur),
        new("flow-master", "develop-status", "{App}", "Says whether an end-to-end run is going, its step, its requirements and its tokens.", AgentCommandKind.Chatur),
        new("flow-master", "estimate-phase", "{App}", "Estimates the phase's tokens and time from the runs so far.", AgentCommandKind.Chatur),
        new("flow-master", "build-phase", "{App}", "Builds every open row of the phase's checklist, as a process run with its checks.", AgentCommandKind.Process, "build-phase"),
        new("flow-master", "fix-issues", "{App} {folder}", "Works through reported bugs, fixes them and re-checks, as a process run with its checks.", AgentCommandKind.Process, "fix-issues"),
        new("flow-master", "triage-issues", "{App} {evidence} [verify]", "Logs reported bugs against the checklist without changing code, as a process run with its checks.", AgentCommandKind.Process, "triage-issues"),
        new("flow-master", "log-miss", "{App} \"sentence\" [--fixed]", "Records one thing that was missed, in your words, in docs/metrics/misses.jsonl and docs/{App}-Misses.md.", AgentCommandKind.Chatur),
        new("flow-master", "triage-and-fix", "{App} {evidence}", "Logs the reported bugs, then fixes them: a triage run first, the fix run queued after it.", AgentCommandKind.Process, "triage-issues"),
        new("flow-master", "amend-docs", "{App} {change}", "Folds a change into the BRD, Architecture, mockups and checklist in place, as a process run with its checks.", AgentCommandKind.Process, "amend-docs"),
        new("flow-master", "devguide", "{App} [--update]", "Writes the developer's map from each screen to the code behind it.", AgentCommandKind.Guided, "docs/{App}-DevGuide.md"),
        new("flow-master", "productguide", "{App}", "Writes the end user's manual, screen by screen.", AgentCommandKind.Guided, "docs/{App}-ProductGuide.md"),
        new("flow-master", "handoff-phase", "{App}", "Finishes the UsageGuide and DevGuide and closes the phase, as a process run with its checks.", AgentCommandKind.Process, "handoff-phase"),
        new("flow-master", "deploy-checklist", "{App} {pipeline-document}", "Writes the deployment checklist for one hosting target, as a process run with its checks.", AgentCommandKind.Process, "deploy-checklist"),
        new("flow-master", "refresh-status", "{App} [verify]", "Rewrites PROJECT-STATUS.md from the checklists, as a process run with its checks.", AgentCommandKind.Process, "refresh-status"),
        new("flow-master", "metrics", "{App}", "Writes the metrics report from docs/metrics, as a process run with its checks.", AgentCommandKind.Process, "metrics-report"),
        new("flow-master", "generate-html", "<path>", "Turns one markdown file into an HTML page beside it.", AgentCommandKind.Chatur),
        new("flow-master", "render-workflow-docs", "{App}", "Turns the BRDs, the Architecture and PROJECT-STATUS.md into HTML pages beside them.", AgentCommandKind.Chatur),
        new("flow-master", "yolo", string.Empty, "Turns Go ahead on or off for this conversation: on, nothing waits for your approval.", AgentCommandKind.Chatur),
        new("flow-master", "help", string.Empty, "Lists these commands, numbered.", AgentCommandKind.Chatur),
        new("flow-master", "exit", string.Empty, "Says goodbye and closes this conversation.", AgentCommandKind.Chatur),

        // The verifier (TechieFlow verifier.md).
        new("verifier", "verify", "{scope} {App} [--phase N|all]", "Checks every row in scope against the running application and writes the verdicts, as a process run with its checks.", AgentCommandKind.Process, "verify-phase"),
        new("verifier", "report", "{scope} {App}", "Shows the status and remarks of the rows in scope (ui, functional, all, or ids) from the checklists, running nothing.", AgentCommandKind.Chatur),
        new("verifier", "help", string.Empty, "Lists these commands, numbered.", AgentCommandKind.Chatur),
        new("verifier", "exit", string.Empty, "Says goodbye and closes this conversation.", AgentCommandKind.Chatur),
        new("verifier", "triage-issues", "{evidence}", "Sorts what has been reported into the checklist it belongs to, as a process run (Chatur's own, kept from phase 2).", AgentCommandKind.Process, "triage-issues"),
        new("verifier", "metrics-report", string.Empty, "Reads the run records and says what they add up to, as a process run (Chatur's own, kept from phase 2).", AgentCommandKind.Process, "metrics-report"),
        new("verifier", "log-miss", "\"sentence\" [--fixed]", "Records one thing that went wrong, in your words (Chatur's own, kept from phase 2).", AgentCommandKind.Chatur)
    ];

    /// <summary>Whether Chatur's own command list covers an agent.</summary>
    /// <param name="aRoleCode">The agent's code.</param>
    public static bool Covers(string? aRoleCode) =>
        aRoleCode is not null && RoleCodes.Contains(aRoleCode, StringComparer.OrdinalIgnoreCase);

    /// <summary>An agent's commands, in order; empty for an agent the owner added.</summary>
    /// <param name="aRoleCode">The agent's code.</param>
    public static IReadOnlyList<AgentCommand> ForRole(string? aRoleCode) =>
        All.Where(aCommand => string.Equals(aCommand.RoleCode, aRoleCode, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>
    /// The agent's command with this name, or <see langword="null"/>. A star in front is ignored, and a name written
    /// with spaces or without its hyphens (<c>brain-storm</c>, <c>brainstorm</c>) still finds it.
    /// </summary>
    /// <param name="aRoleCode">The agent's code.</param>
    /// <param name="aCode">The command as typed.</param>
    public static AgentCommand? Find(string? aRoleCode, string? aCode)
    {
        var vWanted = Normalise(aCode);
        if (vWanted.Length == 0)
        {
            return null;
        }

        var vCommands = ForRole(aRoleCode);
        var vTyped = (aCode ?? string.Empty).TrimStart('*').Trim();
        return vCommands.FirstOrDefault(c => string.Equals(c.Code, vTyped, StringComparison.OrdinalIgnoreCase))
            ?? vCommands.FirstOrDefault(c => Normalise(c.Code) == vWanted)
            ?? (objAliases.TryGetValue(vTyped, out var vAlias) ? vCommands.FirstOrDefault(c => c.Code == vAlias) : null);
    }

    /// <summary>Names Chatur listed before 22 Nov 2026 for a command TechieFlow names otherwise; both find the command.</summary>
    private static readonly IReadOnlyDictionary<string, string> objAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["verify-phase"] = "verify",
        ["metrics-report"] = "metrics",
        ["research"] = "research-prompt",
        ["research-prompt"] = "research"
    };

    /// <summary>The heading of the UsageGuide section the tables go under, and the anchor the README links to (BRD-263, BRD-264).</summary>
    public const string GuideHeading = "The agents and their commands";

    /// <summary>The link from the README to the guide's section.</summary>
    public const string GuideLink = "docs/Chatur-UsageGuide.md#the-agents-and-their-commands";

    /// <summary>The name a person sees for an agent: <c>Flow master</c> for <c>flow-master</c>.</summary>
    /// <param name="aRoleCode">The agent's code.</param>
    public static string DisplayNameOf(string aRoleCode) => aRoleCode.ToLowerInvariant() switch
    {
        "analyst" => "Analyst",
        "architect" => "Architect",
        "flow-master" => "Flow master",
        "verifier" => "Verifier",
        _ => aRoleCode
    };

    /// <summary>The command with sample values, as a person could type it: <c>*day1-brownfield MyApp</c>. Optional parts are left out.</summary>
    /// <param name="aCommand">The command.</param>
    public static string ExampleOf(AgentCommand aCommand)
    {
        var vArguments = aCommand.Arguments;
        var vAlternative = vArguments.IndexOf(" | ", StringComparison.Ordinal);
        if (vAlternative >= 0)
        {
            vArguments = vArguments[..vAlternative];
        }

        vArguments = OptionalPart().Replace(vArguments, string.Empty).Trim();
        foreach (var (vPlaceholder, vSample) in objSamples)
        {
            vArguments = vArguments.Replace(vPlaceholder, vSample, StringComparison.Ordinal);
        }

        return vArguments.Length == 0 ? "*" + aCommand.Code : $"*{aCommand.Code} {vArguments}";
    }

    /// <summary>Where a command's result appears, in plain words: in the chat, a file, or a run on a page.</summary>
    /// <param name="aCommand">The command.</param>
    public static string WhereOf(AgentCommand aCommand)
    {
        switch (aCommand.Code)
        {
            case "develop-end-to-end":
                return "A run on the End-to-end run page";
            case "log-miss":
                return "A file: `docs/metrics/misses.jsonl`";
            case "generate-html":
                return "A file: `docs/…`, an HTML page beside the markdown";
            case "render-workflow-docs":
                return "A file: `docs/…`, an HTML page beside each document";
        }

        return aCommand.Kind switch
        {
            AgentCommandKind.Process => "A run on the Processes page",
            AgentCommandKind.Guided => aCommand.Target is { Length: > 0 } vTarget ? $"A file: `{vTarget}`" : "A file: `docs/…`",
            _ => "In the chat"
        };
    }

    /// <summary>The UsageGuide section: one heading and one table per agent, built from <see cref="All"/> so the guide cannot drift from the code.</summary>
    public static string UsageGuideMarkdown()
    {
        var vText = new StringBuilder();
        // A sub-section of the guide's "Execution guide": the UsageGuide template allows no section of its own for it.
        vText.Append("### ").Append(GuideHeading).Append("\n\n");
        vText.Append("Type a command with its star in the agent's conversation. In the examples `MyApp` stands for your project's name, and `{App}` in a file name is replaced by it.\n\n");
        foreach (var vRole in RoleCodes)
        {
            vText.Append("#### ").Append(DisplayNameOf(vRole)).Append("\n\n");
            vText.Append("| Command | What it does | Example to type | Where the result appears |\n|---|---|---|---|\n");
            foreach (var vCommand in ForRole(vRole))
            {
                vText.Append($"| `*{vCommand.Code}` | {vCommand.Description} | `{ExampleOf(vCommand)}` | {WhereOf(vCommand)} |\n");
            }

            vText.Append('\n');
        }

        return vText.ToString();
    }

    /// <summary>The README section: each agent with its commands on one line, and the link to the guide.</summary>
    public static string ReadmeMarkdown()
    {
        var vText = new StringBuilder();
        vText.Append("## ").Append(GuideHeading).Append("\n\n");
        vText.Append("Chatur has four agents. Type a command with its star in the agent's conversation.\n\n");
        foreach (var vRole in RoleCodes)
        {
            vText.Append("- **").Append(DisplayNameOf(vRole)).Append("**: ")
                .Append(string.Join(" · ", ForRole(vRole).Select(c => $"`{c.Usage}`"))).Append('\n');
        }

        vText.Append($"\nWhat each command does, an example to type and where the result appears: [{GuideHeading}]({GuideLink}).\n");
        return vText.ToString();
    }

    /// <summary>Sample values for the placeholders in a command's arguments.</summary>
    private static readonly (string Placeholder, string Sample)[] objSamples =
    [
        ("{App}", "MyApp"),
        ("{topic}", "a habit tracker"),
        ("{change}", "add a search box"),
        ("{folder}", "docs/issues"),
        ("{evidence}", "docs/issues/bug-1.md"),
        ("{document}", "architecture"),
        ("{scope}", "all"),
        ("{pipeline-document}", "azure-pipelines.yml"),
        ("<path>", "docs/MyApp-BRD.md"),
        ("\"sentence\"", "\"the Run button stayed grey\"")
    ];

    [GeneratedRegex(@"\s*\[[^\]]*\]")]
    private static partial Regex OptionalPart();

    /// <summary>A command name with its star, hyphens, underscores and spaces taken out, in lower case.</summary>
    /// <param name="aCode">The name.</param>
    public static string Normalise(string? aCode) =>
        new((aCode ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
