using Microsoft.Extensions.Configuration;

namespace Chatur.Core.Prerequisites;

/// <summary>
/// Where Doctor looks for a newer nightly (REQ-FN-013). The address is configuration —
/// <c>Chatur:Releases:ApiBaseAddress</c> — and defaults to the public GitHub API, so a deployment
/// that sets nothing checks the real repository; a mirror or a local feed can be pointed at instead.
/// </summary>
public static class ReleaseFeedAddress
{
    /// <summary>The configuration key that overrides the release feed's base address.</summary>
    public const string ConfigurationKey = "Chatur:Releases:ApiBaseAddress";

    /// <summary>The address used when nothing is configured.</summary>
    public const string DefaultAddress = "https://api.github.com/";

    /// <summary>Reads the base address from configuration, falling back to <see cref="DefaultAddress"/>.</summary>
    /// <param name="aConfiguration">The application configuration.</param>
    /// <returns>An absolute address that ends with a slash; the default when the value is missing or not an absolute URL.</returns>
    public static Uri Resolve(IConfiguration aConfiguration)
    {
        var vValue = aConfiguration[ConfigurationKey];
        if (string.IsNullOrWhiteSpace(vValue))
        {
            return new Uri(DefaultAddress);
        }

        var vText = vValue.Trim();
        return Uri.TryCreate(vText.EndsWith('/') ? vText : vText + "/", UriKind.Absolute, out var vUri)
            ? vUri
            : new Uri(DefaultAddress);
    }
}
