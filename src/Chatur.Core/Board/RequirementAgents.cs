using Chatur.Core.Actions;

namespace Chatur.Core.Board;

/// <summary>
/// Which agent owns a requirement, decided by the type in its id (REQ-UI-050). The builder agent
/// (Flow master) builds functional, UI and RAG requirements — the UI and RAG personas run beneath it —
/// and the Verifier owns the non-functional ones, which only a measurement can settle. The four roles
/// match the seeded <c>Role</c> rows.
/// </summary>
public static class RequirementAgents
{
    private static readonly BoardAgent Analyst = new("analyst", "Analyst");
    private static readonly BoardAgent Architect = new("architect", "Architect");
    private static readonly BoardAgent FlowMaster = new("flow-master", "Flow master");
    private static readonly BoardAgent Verifier = new("verifier", "Verifier");

    /// <summary>The four agents, in the order the board's filter lists them.</summary>
    public static IReadOnlyList<BoardAgent> All { get; } = [Analyst, Architect, FlowMaster, Verifier];

    /// <summary>The agent that owns a requirement type.</summary>
    /// <param name="aType">The id's type letters, e.g. <c>UI</c>, <c>FN</c>, <c>RAG</c> or <c>NFR</c>.</param>
    public static BoardAgent ForType(string aType) => aType.ToUpperInvariant() switch
    {
        "NFR" => Verifier,
        _ => FlowMaster,
    };
}
