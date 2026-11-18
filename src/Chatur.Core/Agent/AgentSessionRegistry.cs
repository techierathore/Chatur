using System.Collections.Concurrent;
using Chatur.Core.Data;

namespace Chatur.Core.Agent;

/// <summary>
/// Tracks a session's own cancellation, shared across every scoped <see cref="AgentActions"/>
/// instance that touches it (REQ-UI-023). One request's <c>StopAsync</c> call and a different
/// request's in-flight <c>SendAsync</c> loop are different service instances in different scopes, so
/// the token they share has to live somewhere both can reach — this class, registered as a singleton,
/// is that somewhere. It holds no session data of its own; that stays in the <c>Session</c> and
/// <c>SessionEvent</c> tables.
/// </summary>
/// <remarks>
/// A future <c>SendAsync</c> (cluster G) links its own work to <see cref="Token"/> so a call to
/// <see cref="Cancel"/> stops it; until then this class only backs the cluster H methods that already
/// use it (<c>AgentActions.StopAsync</c>).
/// </remarks>
public sealed class AgentSessionRegistry
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> objSources = new();
    private readonly AppState? objAppState;

    /// <summary>Creates a registry that is not split by account (a test with one account).</summary>
    public AgentSessionRegistry()
    {
    }

    /// <summary>
    /// Creates a registry that keeps each account's sessions apart: two accounts' databases number
    /// their sessions from 1 each, so a stop asked for one account's session never reaches the
    /// other's (BRD-211).
    /// </summary>
    /// <param name="aAppState">Names who is signed in.</param>
    public AgentSessionRegistry(AppState aAppState)
    {
        objAppState = aAppState;
    }

    private string KeyOf(int aSessionId) => $"{AccountScope.Key(objAppState)}|{aSessionId}";

    /// <summary>
    /// The cancellation token for a session's current work. Calling this creates the source the first
    /// time a session is touched; the same token keeps being returned until <see cref="Cancel"/> fires it.
    /// </summary>
    /// <param name="aSessionId">The session to get a token for.</param>
    public CancellationToken Token(int aSessionId) =>
        objSources.GetOrAdd(KeyOf(aSessionId), _ => new CancellationTokenSource()).Token;

    /// <summary>
    /// Cancels a session's current work and drops its token, so the next call to <see cref="Token"/>
    /// starts a fresh one.
    /// </summary>
    /// <param name="aSessionId">The session to stop.</param>
    public void Cancel(int aSessionId)
    {
        if (objSources.TryRemove(KeyOf(aSessionId), out var vSource))
        {
            vSource.Cancel();
            vSource.Dispose();
        }
    }
}
