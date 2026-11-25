using ChaturUI.Services;
using Microsoft.AspNetCore.Components;

namespace ChaturUI;

/// <summary>
/// The root component: the router, the portal and toast hosts and the theme host. As its window closes
/// (<see cref="WindowLifetime"/>) it draws nothing, so everything under it is taken down while the page is still there
/// (REQ-NFR-011).
/// </summary>
public partial class ChaturRoutes : IDisposable
{
    [Inject]
    private WindowLifetime Lifetime { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized() => Lifetime.Closing += OnClosing;

    private void OnClosing() => _ = InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    public void Dispose() => Lifetime.Closing -= OnClosing;
}
