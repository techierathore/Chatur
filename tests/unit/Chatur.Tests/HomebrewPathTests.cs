using Chatur.Core.Prerequisites;
using Xunit;

namespace Chatur.Tests;

/// <summary>Tests for <see cref="HomebrewPath"/> (REQ-FN-012).</summary>
public sealed class HomebrewPathTests
{
    /// <summary>
    /// When probing on macOS, then both Homebrew directories are appended to an existing <c>PATH</c>.
    /// </summary>
    [Fact]
    public void AugmentAddsBothHomebrewDirectoriesOnMac()
    {
        var vResult = HomebrewPath.Augment("/usr/bin:/bin", aIsMacOs: true);

        Assert.Contains("/opt/homebrew/bin", vResult.Split(':'));
        Assert.Contains("/usr/local/bin", vResult.Split(':'));
        Assert.Contains("/usr/bin", vResult.Split(':'));
    }

    /// <summary>
    /// When a Homebrew directory is already on <c>PATH</c>, then it is not duplicated.
    /// </summary>
    [Fact]
    public void AugmentDoesNotDuplicateAnExistingHomebrewDirectory()
    {
        var vResult = HomebrewPath.Augment("/opt/homebrew/bin:/usr/bin", aIsMacOs: true);

        Assert.Equal(1, vResult.Split(':').Count(aEntry => aEntry == "/opt/homebrew/bin"));
    }

    /// <summary>
    /// When probing on a platform other than macOS, then the <c>PATH</c> comes back unchanged.
    /// </summary>
    [Fact]
    public void AugmentLeavesPathAloneOffMac()
    {
        var vResult = HomebrewPath.Augment("/usr/bin:/bin", aIsMacOs: false);

        Assert.Equal("/usr/bin:/bin", vResult);
    }

    /// <summary>
    /// When the existing <c>PATH</c> is <see langword="null"/>, then the Mac branch still returns a
    /// usable <c>PATH</c> made only of the Homebrew directories, rather than throwing.
    /// </summary>
    [Fact]
    public void AugmentTreatsANullPathAsEmptyOnMac()
    {
        var vResult = HomebrewPath.Augment(null, aIsMacOs: true);

        Assert.Equal("/opt/homebrew/bin:/usr/local/bin", vResult);
    }
}
