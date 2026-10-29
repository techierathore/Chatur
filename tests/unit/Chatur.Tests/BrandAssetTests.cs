using System.Buffers.Binary;
using System.Security.Cryptography;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Guards REQ-NFR-007, REQ-NFR-008 and REQ-NFR-009: the approved Chatur mark is copied byte for
/// byte from the owner's brand folder (never redrawn), the MAUI icon and splash colours are the
/// brand ones, and the web harness carries the icons its script renders from <c>Chatur.png</c>.
/// </summary>
public sealed class BrandAssetTests
{
    private const string AppIconHash = "84a8947e6be50647e2c08d44c3db336624619c171bd443d885343b6cde4c9e54";
    private const string GlyphHash = "dd405f7a28f201b0bc74052824387babe05d2d9d37e954188f7b14bfd89a3a66";
    private const string TileHash = "a9496ade44b52415c822ed91e7a2fab547685983c904da35bc171b0d76169b5f";

    /// <summary>
    /// When the three icon and splash SVGs under <c>Resources</c> are hashed, then each equals the
    /// approved brand file (<c>Chatur-appicon.svg</c> for the tile, <c>Chatur-appiconfg.svg</c> for
    /// the glyph and the splash), so none was redrawn.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-008 icon and splash SVGs are byte-identical to the brand files")]
    public void ResourceSvgsMatchBrandFiles()
    {
        var vRoot = FindRepositoryRoot();
        Assert.Equal(AppIconHash, HashOf(vRoot, "src/Chatur/Resources/AppIcon/appicon.svg"));
        Assert.Equal(GlyphHash, HashOf(vRoot, "src/Chatur/Resources/AppIcon/appiconfg.svg"));
        Assert.Equal(GlyphHash, HashOf(vRoot, "src/Chatur/Resources/Splash/splash.svg"));
    }

    /// <summary>
    /// When the MAUI project file is read, then the icon colour is the brand amber <c>#B45F14</c>
    /// and the splash colour stays <c>#1E1B16</c>.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-007 icon colour is brand amber and splash colour stays 1E1B16")]
    public void MauiColoursAreBrand()
    {
        var vProject = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src/Chatur/Chatur.csproj"));
        Assert.Contains("<MauiIcon Include=\"Resources\\AppIcon\\appicon.svg\" ForegroundFile=\"Resources\\AppIcon\\appiconfg.svg\" Color=\"#B45F14\"", vProject);
        Assert.Contains("<MauiSplashScreen Include=\"Resources\\Splash\\splash.svg\" Color=\"#1E1B16\"", vProject);
    }

    /// <summary>
    /// When the web harness wwwroot is read, then <c>favicon.ico</c> is a 32 px icon,
    /// <c>icon-192.png</c> is 192 px and <c>icon-512.png</c> is 512 px, the source tile
    /// <c>scripts/brand/Chatur.png</c> is the approved 1024 px file, and the script that renders
    /// them exists.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-009 web harness icons have the stated sizes and a script renders them")]
    public void WebIconsHaveStatedSizes()
    {
        var vRoot = FindRepositoryRoot();
        var vWww = Path.Combine(vRoot, "src/Chatur.WebHarness/wwwroot");

        var vIco = File.ReadAllBytes(Path.Combine(vWww, "favicon.ico"));
        Assert.Equal(1, BinaryPrimitives.ReadUInt16LittleEndian(vIco.AsSpan(2, 2)));
        Assert.Equal(32, vIco[6]);
        Assert.Equal(32, vIco[7]);

        Assert.Equal((192, 192), PngSize(Path.Combine(vWww, "icon-192.png")));
        Assert.Equal((512, 512), PngSize(Path.Combine(vWww, "icon-512.png")));
        Assert.Equal((1024, 1024), PngSize(Path.Combine(vRoot, "scripts/brand/Chatur.png")));
        Assert.Equal(TileHash, HashOf(vRoot, "scripts/brand/Chatur.png"));
        Assert.True(File.Exists(Path.Combine(vRoot, "scripts/make-web-icons.py")));
    }

    /// <summary>
    /// When the harness page shell is read, then it links all three icon files.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-009 App.razor links favicon.ico, icon-192.png and icon-512.png")]
    public void AppShellLinksIcons()
    {
        var vShell = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src/Chatur.WebHarness/Components/App.razor"));
        Assert.Contains("href=\"favicon.ico\"", vShell);
        Assert.Contains("href=\"icon-192.png\"", vShell);
        Assert.Contains("href=\"icon-512.png\"", vShell);
    }

    private static string HashOf(string aRoot, string aRelative) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(aRoot, aRelative)))).ToLowerInvariant();

    private static (int Width, int Height) PngSize(string aPath)
    {
        var vBytes = File.ReadAllBytes(aPath);
        return (BinaryPrimitives.ReadInt32BigEndian(vBytes.AsSpan(16, 4)), BinaryPrimitives.ReadInt32BigEndian(vBytes.AsSpan(20, 4)));
    }

    private static string FindRepositoryRoot()
    {
        var vFolder = new DirectoryInfo(AppContext.BaseDirectory);
        while (vFolder is not null)
        {
            if (File.Exists(Path.Combine(vFolder.FullName, "Chatur.sln")))
            {
                return vFolder.FullName;
            }

            vFolder = vFolder.Parent;
        }

        throw new DirectoryNotFoundException("Chatur.sln not found above " + AppContext.BaseDirectory);
    }
}
