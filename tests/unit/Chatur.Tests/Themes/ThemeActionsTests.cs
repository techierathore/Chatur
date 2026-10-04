using Chatur.Core;
using Chatur.Core.Themes;
using Chatur.Tests.Support;
using Xunit;

namespace Chatur.Tests.Themes;

/// <summary>Tests for cluster L's <see cref="ThemeActions"/> against a real, migrated, temporary database.</summary>
public sealed class ThemeActionsTests : MigratedDatabaseFixture
{
    private const string ValidThemeJson =
        """
        {
          "light": {"bg":"oklch(0.97 0 0)","card":"oklch(1 0 0)","fg":"oklch(0.2 0 0)","dim":"oklch(0.5 0 0)","faint":"oklch(0.6 0 0)","line":"oklch(0.9 0 0)","line2":"oklch(0.94 0 0)","soft":"oklch(0.95 0 0)","hover":"oklch(0.92 0 0)","accent":"oklch(0.5 0.2 300)","accentFg":"oklch(0.99 0 0)","accentSoft":"oklch(0.9 0.05 300)"},
          "dark": {"bg":"oklch(0.18 0 0)","card":"oklch(0.22 0 0)","fg":"oklch(0.93 0 0)","dim":"oklch(0.7 0 0)","faint":"oklch(0.58 0 0)","line":"oklch(0.3 0 0)","line2":"oklch(0.26 0 0)","soft":"oklch(0.25 0 0)","hover":"oklch(0.31 0 0)","accent":"oklch(0.7 0.2 300)","accentFg":"oklch(0.16 0 0)","accentSoft":"oklch(0.3 0.08 300)"}
        }
        """;

    /// <summary>Every built-in theme is listed, seeded with Amber as the build default (REQ-UI-037).</summary>
    [Fact]
    public async Task ListAsyncReturnsTheFourBuiltInThemes()
    {
        var vActions = new ThemeActions(CreateFactory(), new AppState());

        var vThemes = await vActions.ListAsync();

        Assert.Equal(4, vThemes.Count);
        Assert.Contains(vThemes, t => t.Name == "amber" && t.Source == "build");
        Assert.Contains(vThemes, t => t.Name == "indigo");
        Assert.Contains(vThemes, t => t.Name == "teal");
        Assert.Contains(vThemes, t => t.Name == "slate");
    }

    /// <summary>The shipped default is Amber, dark, before anyone has chosen anything (REQ-UI-037).</summary>
    [Fact]
    public async Task CurrentAsyncDefaultsToAmberDark()
    {
        var vActions = new ThemeActions(CreateFactory(), new AppState());

        var vChoice = await vActions.CurrentAsync();

        Assert.Equal("amber", vChoice.ThemeName);
        Assert.True(vChoice.IsDark);
    }

    /// <summary>
    /// When a theme is chosen, then it is persisted, read back by <see cref="ThemeActions.CurrentAsync"/>,
    /// and every window's shared state repaints (REQ-UI-037).
    /// </summary>
    [Fact]
    public async Task ChooseAsyncPersistsAndUpdatesAppState()
    {
        var vAppState = new AppState();
        var vRepainted = false;
        vAppState.Changed += () => vRepainted = true;
        var vActions = new ThemeActions(CreateFactory(), vAppState);

        await vActions.ChooseAsync("teal", false);

        var vChoice = await vActions.CurrentAsync();
        Assert.Equal("teal", vChoice.ThemeName);
        Assert.False(vChoice.IsDark);
        Assert.Equal("teal", vAppState.ThemeName);
        Assert.False(vAppState.IsDark);
        Assert.True(vRepainted);
    }

    /// <summary>Choosing a theme that was never added is refused (REQ-UI-037).</summary>
    [Fact]
    public async Task ChooseAsyncRefusesAnUnknownTheme()
    {
        var vActions = new ThemeActions(CreateFactory(), new AppState());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => vActions.ChooseAsync("no-such-theme", true));
    }

    /// <summary>
    /// When a theme file carrying every required token is added, then it joins the list with no new
    /// build of Chatur (REQ-UI-038).
    /// </summary>
    [Fact]
    public async Task AddFromFileAsyncAddsAThemeWithAFullShape()
    {
        var vPath = Path.Combine(Path.GetTempPath(), $"sunset-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(vPath, ValidThemeJson);
        try
        {
            var vActions = new ThemeActions(CreateFactory(), new AppState());

            var vAdded = await vActions.AddFromFileAsync(vPath);

            Assert.StartsWith("sunset-", vAdded.Name);
            Assert.Equal("file", vAdded.Source);
            Assert.Equal(5, (await vActions.ListAsync()).Count);
        }
        finally
        {
            File.Delete(vPath);
        }
    }

    /// <summary>A file missing a required token is refused, and nothing is added (REQ-UI-038).</summary>
    [Fact]
    public async Task AddFromFileAsyncRefusesAFileMissingATokenAndAddsNothing()
    {
        var vPath = Path.Combine(Path.GetTempPath(), $"incomplete-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(vPath, """{"light":{"bg":"oklch(1 0 0)"},"dark":{"bg":"oklch(0 0 0)"}}""");
        try
        {
            var vActions = new ThemeActions(CreateFactory(), new AppState());

            await Assert.ThrowsAsync<ArgumentException>(() => vActions.AddFromFileAsync(vPath));

            Assert.Equal(4, (await vActions.ListAsync()).Count);
        }
        finally
        {
            File.Delete(vPath);
        }
    }

    /// <summary>Adding a theme file whose name is already taken is refused (REQ-UI-038).</summary>
    [Fact]
    public async Task AddFromFileAsyncRefusesADuplicateName()
    {
        var vPath = Path.Combine(Path.GetTempPath(), $"amber-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(vPath, ValidThemeJson);
        try
        {
            var vActions = new ThemeActions(CreateFactory(), new AppState());
            var vFirst = await vActions.AddFromFileAsync(vPath);

            var vSecondPath = Path.Combine(Path.GetTempPath(), $"{vFirst.Name}.json");
            await File.WriteAllTextAsync(vSecondPath, ValidThemeJson);
            try
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => vActions.AddFromFileAsync(vSecondPath));
            }
            finally
            {
                File.Delete(vSecondPath);
            }
        }
        finally
        {
            File.Delete(vPath);
        }
    }
}
