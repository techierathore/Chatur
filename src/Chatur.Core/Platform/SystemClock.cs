namespace Chatur.Core.Platform;

/// <summary>
/// The real clock. Nothing about "now" differs between Mac Catalyst, Windows or the web harness, so
/// this is the one <see cref="Platform"/> port every head registers the same way, unlike
/// <see cref="ISecretStore"/>, <see cref="IFolderPicker"/>, <see cref="IProcessLauncher"/> and
/// <see cref="IAppPaths"/>, which are genuinely different per host.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}
