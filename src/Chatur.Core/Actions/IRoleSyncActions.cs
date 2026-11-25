namespace Chatur.Core.Actions;

/// <summary>
/// Keeping a developer's roles and rules the same on every machine, through App Manager
/// (page All projects, the sync panel; BRD-148).
/// </summary>
public interface IRoleSyncActions
{
    /// <summary>
    /// Reads what the sync panel shows, without sending or taking anything (REQ-FN-078).
    /// </summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<RoleSyncStatus> StatusAsync(CancellationToken aCt = default);

    /// <summary>
    /// Sends what changed here and takes what changed elsewhere (REQ-FN-078). When only this machine
    /// changed since the last sync its roles and rules go to the store; when only another machine did, they
    /// are brought in here; when both did, this machine's are kept and sent (the later sync wins).
    /// </summary>
    /// <param name="aCt">A token that cancels the sync.</param>
    Task<RoleSyncStatus> SyncNowAsync(CancellationToken aCt = default);
}
