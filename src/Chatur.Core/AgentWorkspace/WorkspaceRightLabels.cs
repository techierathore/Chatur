namespace Chatur.Core.AgentWorkspace;

/// <summary>
/// What each right reads as on the Agent workspace screen: a sentence for the right held, and a
/// sentence saying where the action went for the right withheld (REQ-FN-058). An action with no entry
/// reads as its own name, so a right an owner adds still shows.
/// </summary>
internal static class WorkspaceRightLabels
{
    private static readonly Dictionary<string, string> AllowedLabels = new()
    {
        ["read-file"] = "read every file",
        ["edit-file"] = "write to a source file",
        ["write-docs"] = "write a document under docs/",
        ["run-build"] = "run the build",
        ["run-tests"] = "run the tests",
        ["drive-screens"] = "drive the screens",
        ["mark-verified"] = "write verdicts to the checklist",
        ["open-feedback"] = "open a feedback file",
        ["log-miss"] = "log a miss",
        ["correct-wording"] = "correct its own wording",
        ["run-source-control"] = "run source control",
        ["close-phase"] = "close a phase or open the next one"
    };

    private static readonly Dictionary<string, string> DeniedLabels = new()
    {
        ["read-file"] = "read a file",
        ["edit-file"] = "write to a source file — that belongs to the Flow master",
        ["write-docs"] = "write a document under docs/ — already covered by writing to a source file",
        ["run-build"] = "run the build — that belongs to the Flow master and the Verifier",
        ["run-tests"] = "run the tests — that belongs to the Flow master and the Verifier",
        ["drive-screens"] = "drive the screens — that belongs to the Verifier",
        ["mark-verified"] = "write verdicts to the checklist — that belongs to the Verifier",
        ["open-feedback"] = "open a feedback file",
        ["log-miss"] = "log a miss",
        ["correct-wording"] = "correct its own wording",
        ["run-source-control"] = "run source control — you check in yourself, in your own terminal",
        ["close-phase"] = "close a phase or open the next one — that is handoff-phase"
    };

    /// <summary>The sentence for a right the agent holds.</summary>
    /// <param name="aAction">The right's action name.</param>
    public static string Allowed(string aAction) => AllowedLabels.GetValueOrDefault(aAction, aAction);

    /// <summary>The sentence for a right the agent lacks.</summary>
    /// <param name="aAction">The right's action name.</param>
    public static string Denied(string aAction) => DeniedLabels.GetValueOrDefault(aAction, aAction);
}
