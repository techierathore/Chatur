namespace Chatur.Core.Data;

/// <summary>
/// Which account the current piece of work belongs to (BRD-210 to BRD-216, Architecture §6
/// 2026-11-17). Ordinarily that is whoever <see cref="AppState.CurrentUser"/> names. Work that outlives
/// the screen that started it — an end-to-end run, a build or a run in the background — is pinned with
/// <see cref="Enter"/> to the account that started it, so a sign-out and another account's sign-in
/// while it is still going never make it read or write the second account's data.
/// </summary>
public static class AccountScope
{
    private static readonly AsyncLocal<string?> objPinned = new();

    /// <summary>The account pinned to this flow of work, or <see langword="null"/> when none is.</summary>
    public static string? Pinned => objPinned.Value;

    /// <summary>
    /// The account the current work belongs to: the pinned one when there is one, otherwise the one
    /// signed in, otherwise <see langword="null"/> (nobody is signed in).
    /// </summary>
    /// <param name="aAppState">The shared state that names who is signed in.</param>
    public static string? CurrentUserId(AppState? aAppState) => objPinned.Value ?? aAppState?.CurrentUser?.UserId;

    /// <summary>
    /// Pins every database read and write in this flow, and in every task it starts, to
    /// <paramref name="aUserId"/> until the returned handle is disposed.
    /// </summary>
    /// <param name="aUserId">The App Manager user id of the account the work belongs to; <see langword="null"/> pins nothing.</param>
    /// <returns>A handle that restores the previous pin when disposed.</returns>
    public static IDisposable Enter(string? aUserId)
    {
        var vPrevious = objPinned.Value;
        objPinned.Value = aUserId;
        return new Restore(vPrevious);
    }

    /// <summary>
    /// A key that keeps one account's in-memory state apart from another's: the account's id, or
    /// <c>"install"</c> when nobody is signed in.
    /// </summary>
    /// <param name="aAppState">The shared state that names who is signed in.</param>
    public static string Key(AppState? aAppState) => CurrentUserId(aAppState) ?? "install";

    private sealed class Restore : IDisposable
    {
        private readonly string? objPrevious;

        public Restore(string? aPrevious)
        {
            objPrevious = aPrevious;
        }

        public void Dispose() => objPinned.Value = objPrevious;
    }
}
