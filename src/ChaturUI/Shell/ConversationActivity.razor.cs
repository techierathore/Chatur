using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Shell;

/// <summary>
/// The agent's steps and proposed changes under a reply in the conversation (REQ-UI-022, REQ-UI-032,
/// REQ-UI-033). Holds no state of its own; the conversation region owns the lists and the actions.
/// </summary>
/// <remarks>Owning cluster: A (Workbench conversation).</remarks>
public partial class ConversationActivity
{
    /// <summary>The steps taken so far, in order.</summary>
    [Parameter]
    public IReadOnlyList<ActivityEvent> Events { get; set; } = [];

    /// <summary>The changes still waiting for the owner.</summary>
    [Parameter]
    public IReadOnlyList<ProposedChange> Changes { get; set; } = [];

    /// <summary>Raised when the owner opens a change's file beside the conversation.</summary>
    [Parameter]
    public EventCallback<ProposedChange> OnOpen { get; set; }

    /// <summary>Raised when the owner approves a change.</summary>
    [Parameter]
    public EventCallback<ProposedChange> OnApprove { get; set; }

    /// <summary>Raised when the owner rejects a change.</summary>
    [Parameter]
    public EventCallback<ProposedChange> OnReject { get; set; }

    private static string ActivityIcon(ActivityEvent aEvent) => aEvent.Kind switch
    {
        "refusal" => "shield-off",
        "tool" => "terminal",
        "stopped" => "square",
        "user" => "user",
        "assistant" => "sparkles",
        _ => "circle"
    };
}
