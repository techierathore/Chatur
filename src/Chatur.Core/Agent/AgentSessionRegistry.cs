using System.Collections.Concurrent;

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
    private readonly ConcurrentDictionary<int, CancellationTokenSource> objSources = new();

    /// <summary>
    /// The cancellation token for a session's current work. Calling this creates the source the first
    /// time a session is touched; the same token keeps being returned until <see cref="Cancel"/> fires it.
    /// </summary>
    /// <param name="aSessionId">The session to get a token for.</param>
    public CancellationToken Token(int aSessionId) =>
        objSources.GetOrAdd(aSessionId, _ => new CancellationTokenSource()).Token;

    /// <summary>
    /// Cancels a session's current work and drops its token, so the next call to <see cref="Token"/>
    /// starts a fresh one.
    /// </summary>
    /// <param name="aSessionId">The session to stop.</param>
    public void Cancel(int aSessionId)
    {
        if (objSources.TryRemove(aSessionId, out var vSource))
        {
            vSource.Cancel();
            vSource.Dispose();
        }
    }
}
