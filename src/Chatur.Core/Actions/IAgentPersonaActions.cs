namespace Chatur.Core.Actions;

/// <summary>
/// What an agent is, read from the persona file its role points at (for a TechieFlow project,
/// <c>.tfcore/agents/&lt;code&gt;.md</c>) and from what the role itself stores (REQ-FN-308). The greeting shown
/// when the person chooses an agent and the system prompt the model receives for that agent's turns both come
/// from here, so what the agent says it is and what the model was told it is cannot differ.
/// </summary>
public interface IAgentPersonaActions
{
    /// <summary>
    /// The greeting for an agent in a project: who it is, what it is for and the commands it has. Built from the
    /// persona file when the project has one, otherwise from the role's own wording and stored commands. No model
    /// is asked, so it works without a provider.
    /// </summary>
    /// <param name="aProjectId">The project the person is working in.</param>
    /// <param name="aRoleId">The agent chosen.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The greeting, or <see langword="null"/> when there is no such role.</returns>
    Task<AgentGreeting?> GreetingAsync(int aProjectId, int aRoleId, CancellationToken aCt = default);

    /// <summary>
    /// The system prompt a model receives for an agent's turns: the role's wording, the commands it has and the
    /// persona file's whole text.
    /// </summary>
    /// <param name="aProjectId">The project the person is working in.</param>
    /// <param name="aRoleId">The agent acting.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The prompt, or <see langword="null"/> when the role has nothing to say.</returns>
    Task<string?> SystemPromptAsync(int aProjectId, int aRoleId, CancellationToken aCt = default);
}
