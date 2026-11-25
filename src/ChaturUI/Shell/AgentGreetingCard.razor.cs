using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Shell;

/// <summary>
/// One agent's greeting in the conversation: its name and title, what it is for, and its commands (REQ-FN-308).
/// Holds no state of its own; the conversation region decides when and where the greeting appears.
/// </summary>
/// <remarks>Owning cluster: G (the conversation).</remarks>
public partial class AgentGreetingCard
{
    /// <summary>What the agent says about itself.</summary>
    [Parameter]
    [EditorRequired]
    public AgentGreeting Greeting { get; set; } = default!;

    /// <summary>The first line: the agent's own name and title, or the title alone when the agent has no name of its own.</summary>
    private string WhoLine => string.Equals(Greeting.Name, Greeting.Title, StringComparison.OrdinalIgnoreCase)
        ? $"Hello, I am the {Greeting.Title}."
        : $"Hello, I am {Greeting.Name}, the {Greeting.Title}.";
}
