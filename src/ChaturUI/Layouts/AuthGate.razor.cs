using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace ChaturUI.Layouts;

/// <summary>
/// The route guard every layout but <see cref="BareLayout"/> wraps its body in. Reads
/// <see cref="IAccountActions.CurrentUserAsync"/>; when nobody is signed in, every window redirects to
/// <c>/sign-in</c> rather than showing content with no owner behind it. When the check itself fails —
/// a build with no App Manager address, or a server that cannot be reached — it shows the reason with
/// "Try again" and "Sign in again" instead of an endless blank window (REQ-NFR-006, 2026-10-06).
/// </summary>
public partial class AuthGate
{
    private bool objIsSignedIn;
    private bool objIsChecking;
    private string? objCheckError;

    [Inject]
    private ILogger<AuthGate> Logger { get; set; } = default!;

    /// <summary>The window content to show once an owner is confirmed signed in.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override Task OnInitializedAsync() => CheckAsync();

    /// <summary>Checks the sign-in once; on a failure keeps the reason so the window can show it.</summary>
    private async Task CheckAsync()
    {
        objIsChecking = true;
        objCheckError = null;
        try
        {
            var vUser = await Accounts.CurrentUserAsync();
            if (vUser is null)
            {
                Nav.NavigateTo("/sign-in");
                return;
            }

            objIsSignedIn = true;
        }
        catch (Exception vException)
        {
            Logger.LogError(vException, "Checking the signed-in owner failed");
            objCheckError = vException is InvalidOperationException
                ? "This build of Chatur has no sign-in service address, so it cannot reach App Manager."
                : $"The sign-in service could not be reached: {vException.Message}";
        }
        finally
        {
            objIsChecking = false;
        }
    }

    /// <summary>Goes to Sign in, so the owner can start a fresh session.</summary>
    private void SignInAgain() => Nav.NavigateTo("/sign-in");
}
