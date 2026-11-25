namespace Chatur.Core.Actions;

/// <summary>
/// Feedback and issues (page <c>/feedback</c>; BRD-137 to BRD-140): an entry is raised through App
/// Manager, carries the name of what it is about, comes back with its state and answer, and is kept
/// on this machine until App Manager has taken it. Every entry is the signed-in account's own.
/// </summary>
public interface IFeedbackActions
{
    /// <summary>
    /// Raises the entry through App Manager and lists it (REQ-UI-062), carrying the name the owner
    /// picked (REQ-FN-073). When App Manager cannot be reached the entry is still kept, as
    /// <see cref="FeedbackState.Waiting"/>, and sent by a later <see cref="ListAsync"/> (REQ-FN-074).
    /// </summary>
    /// <param name="aDraft">What the owner typed.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    /// <returns>The entry as it stands after the first try.</returns>
    /// <exception cref="ArgumentException">The name is not one of <see cref="FeedbackAbout.All"/>, or the title or description is outside <see cref="FeedbackLimits"/>.</exception>
    Task<FeedbackEntry> SendAsync(FeedbackDraft aDraft, CancellationToken aCt = default);

    /// <summary>
    /// Sends every entry still waiting, asks App Manager for the newest state and answer of every entry
    /// it has taken (REQ-UI-063), and returns the list. A network that is down leaves each entry as it
    /// was and is not an error (REQ-FN-074).
    /// </summary>
    /// <param name="aCt">A token that cancels the call.</param>
    Task<FeedbackView> ListAsync(CancellationToken aCt = default);
}
