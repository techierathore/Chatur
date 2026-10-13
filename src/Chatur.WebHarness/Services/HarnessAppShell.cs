using Chatur.Core.Platform;
using Microsoft.JSInterop;

namespace Chatur.WebHarness.Services;

/// <summary>
/// A browser tab has no application to quit or window to zoom, so the harness does what is closest:
/// Exit stops the harness server, and Zoom sets the page's zoom from 50% to 300% in 10% steps, as the
/// desktop heads do (REQ-UI-073). Scoped, because the <see cref="IJSRuntime"/> belongs to one circuit.
/// </summary>
public sealed class HarnessAppShell : IAppShell
{
    private readonly IHostApplicationLifetime objLifetime;
    private readonly IJSRuntime objJs;
    private double objLevel = 1.0;

    /// <summary>Creates the port.</summary>
    /// <param name="aLifetime">The server's lifetime, stopped by Exit.</param>
    /// <param name="aJs">This circuit's script runtime.</param>
    public HarnessAppShell(IHostApplicationLifetime aLifetime, IJSRuntime aJs)
    {
        objLifetime = aLifetime;
        objJs = aJs;
    }

    /// <inheritdoc />
    public void Exit() => objLifetime.StopApplication();

    /// <inheritdoc />
    public void ZoomBy(int aSteps) => Apply(objLevel + (aSteps * 0.1));

    /// <inheritdoc />
    public void ResetZoom() => Apply(1.0);

    private void Apply(double aLevel)
    {
        objLevel = Math.Round(Math.Clamp(aLevel, 0.5, 3.0) * 10) / 10;
        _ = objJs.InvokeVoidAsync("eval", $"document.documentElement.style.zoom={objLevel.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }
}
