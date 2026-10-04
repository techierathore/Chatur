using Microsoft.AspNetCore.Components;

namespace ChaturUI.Layouts;

/// <summary>
/// The route guard every layout but <see cref="BareLayout"/> wraps its body in. Reads
/// <see cref="IAccountActions.CurrentUserAsync"/>; while the stub returns <see langword="null"/>
/// (cluster B has not wired real sign-in yet), every window correctly redirects to
/// <c>/sign-in</c> rather than showing content with no owner behind it — this is the intended
/// behaviour of an unfinished sign-in, never a bypass to route around.
/// </summary>
public partial class AuthGate
{
    private bool objIsSignedIn;

    /// <summary>The window content to show once an owner is confirmed signed in.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var vUser = await Accounts.CurrentUserAsync();
        if (vUser is null)
        {
            Nav.NavigateTo("/sign-in");
            return;
        }

        objIsSignedIn = true;
    }
}
