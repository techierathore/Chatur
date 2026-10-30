using Chatur.Core.Platform;
using Xunit;

namespace Chatur.Tests;

/// <summary>Tests for <see cref="TestDataGuard"/> (REQ-NFR-010).</summary>
public sealed class TestDataGuardTests : IDisposable
{
    private readonly string objRoot = Path.Combine(Path.GetTempPath(), "chatur-guard-" + Guid.NewGuid().ToString("N"));
    private readonly string objOwner;
    private readonly string objBin;

    /// <summary>Lays out a fake repository (with <c>Chatur.sln</c> and a <c>bin</c> folder) and a fake owner's folder.</summary>
    public TestDataGuardTests()
    {
        objOwner = Path.Combine(objRoot, "owner", "Data");
        objBin = Path.Combine(objRoot, "repo", "src", "Chatur", "bin", "Debug");
        Directory.CreateDirectory(objOwner);
        Directory.CreateDirectory(objBin);
        File.WriteAllText(Path.Combine(objRoot, "repo", "Chatur.sln"), string.Empty);
    }

    /// <summary>
    /// When the desktop head is started by the test boot (the DevTools port is open), then its data folder is
    /// the repository's tests/.artifacts/app-data/desktop, not the owner's application-data folder.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-010 a desktop head started by the test boot keeps its data under tests/.artifacts/app-data/desktop")]
    public void TestBootUsesArtifactsFolder()
    {
        var vFolder = TestDataGuard.ResolveDataFolder(objOwner, Env(("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--remote-debugging-port=9222")), objBin, "desktop");

        Assert.Equal(Path.Combine(objRoot, "repo", "tests", ".artifacts", "app-data", "desktop"), vFolder);
        Assert.True(Directory.Exists(vFolder));
    }

    /// <summary>When the owner starts the app normally, then it uses the owner's own folder.</summary>
    [Fact(DisplayName = "REQ-NFR-010 an ordinary start uses the owner application-data folder")]
    public void OrdinaryStartUsesOwnerFolder()
    {
        Assert.Equal(objOwner, TestDataGuard.ResolveDataFolder(objOwner, Env(), objBin, "desktop"));
    }

    /// <summary>When a test run is pointed at the owner's application-data database, then it refuses to start.</summary>
    [Fact(DisplayName = "REQ-NFR-010 a test run pointed at the owner application-data database refuses to start")]
    public void TestRunRefusesOwnerFolder()
    {
        var vError = Assert.Throws<InvalidOperationException>(() =>
            TestDataGuard.ResolveDataFolder(objOwner, Env(("CHATUR_TEST_DATA", objOwner)), objBin, "desktop"));

        Assert.Contains("refuses the owner's application-data folder", vError.Message);
    }

    /// <summary>When a test run is pointed at a folder inside the owner's, then it refuses that too.</summary>
    [Fact(DisplayName = "REQ-NFR-010 a test run pointed inside the owner folder refuses to start")]
    public void TestRunRefusesFolderInsideOwner()
    {
        Assert.Throws<InvalidOperationException>(() =>
            TestDataGuard.ResolveDataFolder(objOwner, Env(("CHATUR_TEST_DATA", Path.Combine(objOwner, "sub"))), objBin, "desktop"));
    }

    /// <summary>When a test run started outside the repository names no folder, then it refuses rather than fall back to the owner's.</summary>
    [Fact(DisplayName = "REQ-NFR-010 a test run outside the repository with no folder named refuses to start")]
    public void TestRunOutsideRepositoryRefuses()
    {
        Assert.Throws<InvalidOperationException>(() =>
            TestDataGuard.ResolveDataFolder(objOwner, Env(("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--remote-debugging-port=9222")), objOwner, "desktop"));
    }

    /// <summary>When other web view arguments are set without the DevTools port, then it is not a test run.</summary>
    [Fact(DisplayName = "REQ-NFR-010 other web view arguments do not mark a test run")]
    public void OtherWebViewArgumentsAreNotATest()
    {
        Assert.False(TestDataGuard.IsUnderTest(Env(("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--disable-gpu"))));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(objRoot, true);
    }

    private static Func<string, string?> Env(params (string Name, string Value)[] aVariables) =>
        aName => aVariables.FirstOrDefault(v => v.Name == aName).Value;
}
