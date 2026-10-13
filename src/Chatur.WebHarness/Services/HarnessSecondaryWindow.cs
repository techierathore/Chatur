using Chatur.Core.Platform;
using Microsoft.AspNetCore.Components;

namespace Chatur.WebHarness.Services;

/// <summary>
/// A browser tab cannot open a second desktop window, so the harness shows the secondary window's
/// page in the same tab, with the secondary layout around it (REQ-FN-106). Scoped, because the
/// <see cref="NavigationManager"/> it drives belongs to one circuit.
/// </summary>
public sealed class HarnessSecondaryWindow : ISecondaryWindow
{
    private readonly NavigationManager objNav;

    /// <summary>Creates the port.</summary>
    /// <param name="aNav">This circuit's navigation.</param>
    public HarnessSecondaryWindow(NavigationManager aNav)
    {
        objNav = aNav;
    }

    /// <inheritdoc />
    public void Open(string aRoute) => objNav.NavigateTo(aRoute);

    /// <inheritdoc />
    public void Close() => objNav.NavigateTo("/");
}
