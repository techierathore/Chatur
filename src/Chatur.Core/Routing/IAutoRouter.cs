using Chatur.Core.Agent;
using TechieRag.Abstractions;

namespace Chatur.Core.Routing;

/// <summary>
/// Automatic routing (REQ-FN-071, REQ-FN-072): when the owner has switched it on, a small model inside
/// TechieRag reads a request and chooses which configured model does the work.
/// </summary>
public interface IAutoRouter
{
    /// <summary>
    /// Chooses the model for one piece of work, or answers <see langword="null"/> when the work is to be
    /// routed by the owner's tiers and chains as before: automatic routing is off (the small model is
    /// then never asked), no model is connected, or the small model itself failed. Each choice is
    /// logged (model, source, reason).
    /// </summary>
    /// <param name="aRequest">What the piece of work asks for.</param>
    /// <param name="aSessionId">The session the work belongs to, for the log.</param>
    /// <param name="aRoleId">The role doing the work, for the log.</param>
    /// <param name="aCt">A token that cancels the choice.</param>
    Task<ConnectedModel?> ChooseAsync(string aRequest, int aSessionId, int aRoleId, CancellationToken aCt = default);

    /// <summary>
    /// Like <see cref="ChooseAsync"/>, and also says in a few words why (REQ-UI-111), so the reason can be stored with
    /// the reply. Answers <see langword="null"/> when routing is off or no model is connected, so nothing is said about
    /// a choice that was not made; when the small model failed it answers a decision with no model and the reason
    /// the tiers and chains decided. The default reads <see cref="ChooseAsync"/> and gives a plain reason.
    /// </summary>
    /// <param name="aRequest">What the piece of work asks for.</param>
    /// <param name="aSessionId">The session the work belongs to, for the log.</param>
    /// <param name="aRoleId">The role doing the work, for the log.</param>
    /// <param name="aCt">A token that cancels the choice.</param>
    async Task<RoutingDecision?> DecideAsync(string aRequest, int aSessionId, int aRoleId, CancellationToken aCt = default)
    {
        var vModel = await ChooseAsync(aRequest, aSessionId, aRoleId, aCt).ConfigureAwait(false);
        return vModel is null ? null : new RoutingDecision(vModel, "Automatic: chosen for this work");
    }
}

/// <summary>What Automatic routing decided for one piece of work (REQ-UI-111).</summary>
/// <param name="Model">The model it picked; <see langword="null"/> when the small model failed and the owner's tiers and chains decide.</param>
/// <param name="Reason">Why, in a few words, starting with <c>Automatic:</c>.</param>
public sealed record RoutingDecision(ConnectedModel? Model, string Reason);

/// <summary>
/// Builds TechieRag's <see cref="IModelChooser"/> around a small model, a seam so a test can supply a
/// fake chooser instead of making a model call (Coding Standards §Testability).
/// </summary>
public interface IModelChooserFactory
{
    /// <summary>Creates a chooser that asks <paramref name="aSmallModel"/>.</summary>
    /// <param name="aSmallModel">The small model that does the choosing.</param>
    IModelChooser Create(ILlmProvider aSmallModel);
}
