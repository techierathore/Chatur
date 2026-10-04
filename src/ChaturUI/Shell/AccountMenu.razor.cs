using Chatur.Core;
using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Shell;

/// <summary>
/// The signed-in user's menu in a window's top bar: who is signed in, Account and Settings, and
/// Sign out (REQ-FN-009). Sign out clears the stored token and sends every window back to sign-in.
/// </summary>
public partial class AccountMenu : IDisposable
{
    [Inject]
    private IAccountActions Accounts { get; set; } = default!;

    [Inject]
    private AppState AppStateService { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized() => AppStateService.Changed += OnAppStateChanged;

    /// <summary>The signed-in owner's initials, for the avatar fallback.</summary>
    private string Initials
    {
        get
        {
            var vName = AppStateService.CurrentUser?.DisplayName;
            if (string.IsNullOrWhiteSpace(vName))
            {
                return "?";
            }

            var vParts = vName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return vParts.Length switch
            {
                0 => "?",
                1 => vParts[0][..1].ToUpperInvariant(),
                _ => $"{vParts[0][0]}{vParts[^1][0]}".ToUpperInvariant()
            };
        }
    }

    /// <summary>
    /// Signs out (REQ-FN-009): the stored token is removed and <see cref="AppState.CurrentUser"/> is
    /// cleared, so the route guard on every window sends the owner back to <c>/sign-in</c> at once.
    /// </summary>
    private async Task SignOutAsync()
    {
        await Accounts.SignOutAsync();
        Nav.NavigateTo("/sign-in", forceLoad: true);
    }

    private void OnAppStateChanged() => _ = InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    public void Dispose() => AppStateService.Changed -= OnAppStateChanged;
}
