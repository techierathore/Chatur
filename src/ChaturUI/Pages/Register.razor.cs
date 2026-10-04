using Chatur.Core.Accounts;
using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Pages;

/// <summary>
/// Register (mockups/register.html). The live per-rule display (REQ-UI-003) and the specific
/// "already in use" wording (REQ-UI-004) are cluster D's work; this wires the real submit
/// (REQ-FN-004) and refuses to send a password that breaks <see cref="PasswordRule"/>
/// (REQ-FN-005).
/// </summary>
public partial class Register
{
    [Inject]
    private IAccountActions Accounts { get; set; } = null!;

    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    private string objFirstName = string.Empty;
    private string objLastName = string.Empty;
    private string objEmail = string.Empty;
    private string objPassword = string.Empty;
    private string objConfirmPassword = string.Empty;
    private bool objPasswordInvalid;
    private string? objErrorMessage;
    private bool objEmailInUse;
    private bool objIsSubmitting;

    /// <summary>Which parts of <see cref="PasswordRule"/> <see cref="objPassword"/> currently meets, for
    /// the live checklist while typing (REQ-UI-003).</summary>
    private PasswordRuleStatus RuleStatus => PasswordRule.Evaluate(objPassword);

    /// <summary>How many of the four parts of <see cref="PasswordRule"/> are currently met (REQ-UI-003).</summary>
    private int RuleMetCount =>
        (RuleStatus.HasLength ? 1 : 0) + (RuleStatus.HasCapital ? 1 : 0) + (RuleStatus.HasNumber ? 1 : 0) + (RuleStatus.HasSpecial ? 1 : 0);

    /// <summary>Whether the confirm-password field currently matches the password field, for the live
    /// "both fields match" indicator (mockups/register.html "confirm-ok").</summary>
    private bool PasswordsMatch => !string.IsNullOrEmpty(objConfirmPassword) && string.Equals(objPassword, objConfirmPassword, StringComparison.Ordinal);

    /// <summary>This machine's name, shown on the device note (mockups/register.html "device-note") so
    /// the owner knows which device the new account is being registered from (REQ-FN-002's device
    /// identity, read-only display here).</summary>
    private static string MachineName => Environment.MachineName;

    /// <summary>
    /// Runs when "Create account" is pressed. Checks <see cref="PasswordRule"/> first and marks the
    /// password field without sending anything when it fails (REQ-FN-005); otherwise registers the
    /// account and opens Projects on success (REQ-FN-004). A refusal because the email is already in
    /// use is flagged separately so the email field can show App Manager's own message and keep
    /// everything the owner typed (REQ-UI-004).
    /// </summary>
    private async Task HandleSubmitAsync()
    {
        objErrorMessage = null;
        objEmailInUse = false;

        if (!PasswordRule.Evaluate(objPassword).MeetsRule)
        {
            objPasswordInvalid = true;
            return;
        }

        objPasswordInvalid = false;

        if (!string.Equals(objPassword, objConfirmPassword, StringComparison.Ordinal))
        {
            objErrorMessage = "Password and confirm password do not match.";
            return;
        }

        objIsSubmitting = true;
        try
        {
            var vRequest = new RegisterRequest(objFirstName, objLastName, objEmail, objPassword);
            var vResult = await Accounts.RegisterAsync(vRequest);

            if (vResult.Succeeded)
            {
                Nav.NavigateTo("/start");
                return;
            }

            // App Manager's registration endpoint refuses at this stage only for an email already in
            // use (BRD-9); the client-side checks above already caught a weak or mismatched password.
            objErrorMessage = vResult.ErrorMessage;
            objEmailInUse = true;
        }
        finally
        {
            objIsSubmitting = false;
        }
    }
}
