namespace Chatur.Core.Actions;

/// <summary>
/// Sign in, register, sign out and the current owner's identity (Architecture §7 "Accounts"; page
/// Sign in / Register / Workbench's sign-out). Backed by App Manager (Architecture §1 Q4).
/// </summary>
public interface IAccountActions
{
    /// <summary>
    /// Signs in with an email and password, sending this installation's device id (REQ-UI-001,
    /// REQ-FN-001, REQ-FN-002).
    /// </summary>
    /// <param name="aEmail">The email address typed on Sign in.</param>
    /// <param name="aPassword">The password typed on Sign in — encrypted before it leaves the machine.</param>
    /// <param name="aRemember">Whether the token should survive a restart (REQ-FN-003).</param>
    /// <param name="aCt">A token that cancels the sign-in.</param>
    /// <returns>The result, with App Manager's own message on refusal (REQ-UI-002).</returns>
    Task<SignInResult> SignInAsync(string aEmail, string aPassword, bool aRemember, CancellationToken aCt = default);

    /// <summary>
    /// Creates a new account (REQ-FN-004, REQ-FN-005, REQ-UI-004).
    /// </summary>
    /// <param name="aRequest">The name, email and password typed on Register.</param>
    /// <param name="aCt">A token that cancels the registration.</param>
    /// <returns>The result, with App Manager's own message when the email is already in use.</returns>
    Task<SignInResult> RegisterAsync(RegisterRequest aRequest, CancellationToken aCt = default);

    /// <summary>
    /// Signs out, so the stored token no longer opens the app (REQ-FN-009).
    /// </summary>
    /// <param name="aCt">A token that cancels the sign-out.</param>
    Task SignOutAsync(CancellationToken aCt = default);

    /// <summary>
    /// The owner signed in on this machine right now.
    /// </summary>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>
    /// The signed-in owner, or <see langword="null"/> when nobody is signed in. Every layout but
    /// <c>BareLayout</c> reads this to decide whether to redirect to <c>/sign-in</c>.
    /// </returns>
    Task<CurrentUser?> CurrentUserAsync(CancellationToken aCt = default);

    /// <summary>
    /// This installation's device identifier, generated once on first run and kept afterwards
    /// (REQ-FN-002, Architecture §5 "Identity").
    /// </summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<string> DeviceIdAsync(CancellationToken aCt = default);
}
