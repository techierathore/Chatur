namespace Chatur.Core.Actions;

/// <summary>
/// Holding an agent's proposed edits until the owner approves or rejects them (Architecture §7
/// "Changes"; page Workbench's proposed-change card).
/// </summary>
public interface IChangeActions
{
    /// <summary>Every change proposed in a session, waiting or settled (REQ-UI-032).</summary>
    /// <param name="aSessionId">The session to list changes for.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<ProposedChange>> ProposedAsync(int aSessionId, CancellationToken aCt = default);

    /// <summary>
    /// Approves a change: the file on disk gets the new text (REQ-FN-033).
    /// </summary>
    /// <param name="aChangeId">The change to approve.</param>
    /// <param name="aCt">A token that cancels the approval.</param>
    Task ApproveAsync(int aChangeId, CancellationToken aCt = default);

    /// <summary>
    /// Rejects a change: the file on disk is left untouched (REQ-FN-034).
    /// </summary>
    /// <param name="aChangeId">The change to reject.</param>
    /// <param name="aCt">A token that cancels the rejection.</param>
    Task RejectAsync(int aChangeId, CancellationToken aCt = default);
}
