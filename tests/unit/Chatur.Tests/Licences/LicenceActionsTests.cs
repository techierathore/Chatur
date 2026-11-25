using Chatur.Core;
using Chatur.Core.Actions;
using Chatur.Core.Licences;
using Chatur.Core.Platform;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chatur.Tests.Licences;

/// <summary>
/// Tests for the licence module over the in-memory test licence: REQ-UI-064 (licence shown), REQ-UI-065
/// (the server's warning), REQ-UI-066 (forget a machine) and REQ-FN-075 (a feature that is off is not offered).
/// </summary>
public sealed class LicenceActionsTests
{
    private const string Warning = "Forget one machine to carry on.";

    /// <summary>
    /// When the licence is read, then its name, state and days remaining are in the view (REQ-UI-064).
    /// </summary>
    [Fact]
    public async Task GetAsyncShowsNameStateAndDaysRemaining()
    {
        var (vActions, _, _) = Create();

        var vView = await vActions.GetAsync();

        Assert.True(vView.Available);
        Assert.Equal("Professional", vView.Licence!.Name);
        Assert.Equal("Active", vView.Licence.State);
        Assert.Equal(287, vView.Licence.DaysRemaining);
    }

    /// <summary>
    /// When two machines are signed in against a cap of one, then the view carries the server's warning verbatim,
    /// and when a machine is forgotten the warning goes (REQ-UI-065).
    /// </summary>
    [Fact]
    public async Task OverTheCapShowsTheServersWarningUntilBackWithinIt()
    {
        var (vActions, _, _) = Create();

        var vOver = await vActions.GetAsync();
        Assert.Equal(Warning, vOver.Warning!.Message);
        Assert.Contains("2 devices against a cap of 1", vOver.Warning.Title);

        await vActions.ForgetDeviceAsync("win");
        var vWithin = await vActions.GetAsync();
        Assert.Null(vWithin.Warning);
    }

    /// <summary>
    /// When a machine is forgotten, then it leaves the list and can no longer sign in (REQ-UI-066).
    /// </summary>
    [Fact]
    public async Task ForgetDeviceRemovesTheMachineAndEndsItsSignIn()
    {
        var (vActions, vSource, _) = Create();
        Assert.True(vSource.CanSignIn("win"));

        var vResult = await vActions.ForgetDeviceAsync("win");
        var vView = await vActions.GetAsync();

        Assert.True(vResult.Succeeded);
        Assert.DoesNotContain(vView.Devices, aDevice => aDevice.DeviceId == "win");
        Assert.False(vSource.CanSignIn("win"));
        Assert.True(vSource.CanSignIn("mac"));
    }

    /// <summary>
    /// When a machine that is not on the licence is forgotten, then nothing changes and the answer says so.
    /// </summary>
    [Fact]
    public async Task ForgetDeviceRefusesAMachineThatIsNotThere()
    {
        var (vActions, _, _) = Create();

        var vResult = await vActions.ForgetDeviceAsync("nope");

        Assert.False(vResult.Succeeded);
        Assert.NotNull(vResult.Message);
    }

    /// <summary>
    /// When a feature is switched off, then it is recorded in the shared state as off and the others stay on (REQ-FN-075).
    /// </summary>
    [Fact]
    public async Task SwitchedOffFeatureIsOffInTheSharedState()
    {
        var (vActions, _, vState) = Create();

        await vActions.SetFeatureAsync(ChaturFeatures.Measurements, false);

        Assert.False(vState.IsFeatureOn(ChaturFeatures.Measurements));
        Assert.True(vState.IsFeatureOn(ChaturFeatures.Integrations));

        await vActions.SetFeatureAsync(ChaturFeatures.Measurements, true);
        Assert.True(vState.IsFeatureOn(ChaturFeatures.Measurements));
    }

    /// <summary>
    /// When the licence source cannot be reached, then the view says it is unavailable and every feature stays on.
    /// </summary>
    [Fact]
    public async Task UnreachableSourceLeavesEveryFeatureOn()
    {
        var vState = new AppState();
        vState.SetCurrentUser(new CurrentUser("1", "a@b.c", "A B"));
        var vActions = new LicenceActions(vState, new ThrowingSource(), new FixedClock(), NullLogger<LicenceActions>.Instance);

        await vActions.EnsureFeatureSwitchesAsync();
        var vView = await vActions.GetAsync();

        Assert.False(vView.Available);
        Assert.Empty(vState.OffFeatures);
    }

    /// <summary>
    /// When a Chatur feature is not named by the licence source, then it is on; only a feature the licence names as off is off.
    /// </summary>
    [Fact]
    public async Task FeatureTheSourceDoesNotNameIsOn()
    {
        var vState = new AppState();
        vState.SetCurrentUser(new CurrentUser("1", "a@b.c", "A B"));
        var vSource = new FixedSource(new LicenceSnapshot(null, null, [], [new FeatureAccess(ChaturFeatures.Board, false)], false));
        var vActions = new LicenceActions(vState, vSource, new FixedClock(), NullLogger<LicenceActions>.Instance);

        var vView = await vActions.GetAsync();

        Assert.False(vView.Features.Single(aFeature => aFeature.Code == ChaturFeatures.Board).IsOn);
        Assert.All(vView.Features.Where(aFeature => aFeature.Code != ChaturFeatures.Board), aFeature => Assert.True(aFeature.IsOn));
    }

    /// <summary>
    /// When the account changes, then the previous account's switched-off features are forgotten.
    /// </summary>
    [Fact]
    public async Task AnotherAccountDoesNotInheritSwitchedOffFeatures()
    {
        var (vActions, _, vState) = Create();
        await vActions.SetFeatureAsync(ChaturFeatures.Worktrees, false);

        vState.SetCurrentUser(new CurrentUser("2", "x@y.z", "X Y"));

        Assert.True(vState.IsFeatureOn(ChaturFeatures.Worktrees));
    }

    /// <summary>
    /// When the test licence is reset, then every machine and feature is back as configured.
    /// </summary>
    [Fact]
    public async Task ResetPutsTheTestLicenceBack()
    {
        var (vActions, vSource, _) = Create();
        await vActions.ForgetDeviceAsync("win");
        await vActions.SetFeatureAsync(ChaturFeatures.Board, false);

        vSource.Reset();
        var vView = await vActions.GetAsync();

        Assert.Equal(3, vView.Devices.Count);
        Assert.All(vView.Features, aFeature => Assert.True(aFeature.IsOn));
    }

    private static (LicenceActions Actions, InMemoryLicenceSource Source, AppState State) Create()
    {
        var vState = new AppState();
        vState.SetCurrentUser(new CurrentUser("1", "owner@example.com", "The Owner"));
        var vOptions = new TestLicenceOptions(
            "Professional", "Active", "KEY", 287, 365, 1, Warning,
            [
                new TestLicenceDevice("mac", "Mac", "Mac Catalyst", "0.1.0", 0, 128, true, false),
                new TestLicenceDevice("win", "Win", "Windows", "0.1.0", 1, 41, false, false),
                new TestLicenceDevice("ci", "CI", "Mac Catalyst", "0.0.9", 80, 6, false, true)
            ],
            []);
        var vClock = new FixedClock();
        var vSource = new InMemoryLicenceSource(vOptions, vClock);
        return (new LicenceActions(vState, vSource, vClock, NullLogger<LicenceActions>.Instance), vSource, vState);
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FixedSource : ILicenceSource
    {
        private readonly LicenceSnapshot objSnapshot;

        public FixedSource(LicenceSnapshot aSnapshot) => objSnapshot = aSnapshot;

        public Task<LicenceSnapshot> ReadAsync(CancellationToken aCt = default) => Task.FromResult(objSnapshot);

        public Task<LicenceActionResult> ForgetDeviceAsync(string aDeviceId, CancellationToken aCt = default) => Task.FromResult(new LicenceActionResult(true, null));

        public Task<LicenceActionResult> SetBlockedAsync(string aDeviceId, bool aBlocked, CancellationToken aCt = default) => Task.FromResult(new LicenceActionResult(true, null));

        public Task<LicenceActionResult> SetFeatureAsync(string aCode, bool aOn, CancellationToken aCt = default) => Task.FromResult(new LicenceActionResult(true, null));
    }

    private sealed class ThrowingSource : ILicenceSource
    {
        public Task<LicenceSnapshot> ReadAsync(CancellationToken aCt = default) => throw new HttpRequestException("down");

        public Task<LicenceActionResult> ForgetDeviceAsync(string aDeviceId, CancellationToken aCt = default) => throw new HttpRequestException("down");

        public Task<LicenceActionResult> SetBlockedAsync(string aDeviceId, bool aBlocked, CancellationToken aCt = default) => throw new HttpRequestException("down");

        public Task<LicenceActionResult> SetFeatureAsync(string aCode, bool aOn, CancellationToken aCt = default) => throw new HttpRequestException("down");
    }
}
