using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;
using TrBlazeUI.Components.Toast;

namespace ChaturUI.Pages;

/// <summary>
/// Sign in (mockups/sign-in.html). Submits against <c>IAccountActions.SignInAsync</c> and shows
/// App Manager's own error on refusal (REQ-UI-001, REQ-UI-002).
/// </summary>
public partial class SignIn
{
    private string objEmail = string.Empty;
    private string objPassword = string.Empty;
    private bool objRemember = true;
    private bool objIsSigningIn;
    private string? objErrorMessage;

    /// <summary>This machine's own name, shown beside the lock note (mockups/sign-in.html "device-note") — not the App Manager device id, which is REQ-FN-002 (cluster B).</summary>
    private readonly string objDeviceName = Environment.MachineName;

    [Inject]
    private IAccountActions Accounts { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    [Inject]
    private ToastService Toast { get; set; } = default!;

    /// <summary>
    /// Sends the typed email and password to App Manager. On success, moves on to Projects
    /// (REQ-UI-001); on refusal, shows the server's own message and stays on the screen
    /// (REQ-UI-002).
    /// </summary>
    private async Task HandleSignInAsync()
    {
        if (objIsSigningIn)
        {
            return;
        }

        objIsSigningIn = true;
        objErrorMessage = null;

        try
        {
            var vResult = await Accounts.SignInAsync(objEmail, objPassword, objRemember);
            if (vResult.Succeeded)
            {
                Nav.NavigateTo("/start");
                return;
            }

            objErrorMessage = vResult.ErrorMessage ?? "Sign-in failed.";
        }
        finally
        {
            objIsSigningIn = false;
        }
    }

    /// <summary>
    /// Password reset is not a row in this phase's checklist — the mockup names what App Manager
    /// will do, shown as a toast rather than the mockup's own <c>alert()</c> (Coding Standards
    /// "Use ToastService for user feedback ... never JavaScript alert()").
    /// </summary>
    private void HandleForgotPassword() =>
        Toast.Show($"App Manager sends a reset link to {objEmail}. The link is good for one hour and can only be used once.", "Forgotten your password?");
}
