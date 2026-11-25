using Chatur.Core.Actions;
using Chatur.Core.Feedback;
using Chatur.Core.Platform;
using Chatur.Tests.Support;
using ChaturDb;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chatur.Tests.Feedback;

/// <summary>
/// Tests for <see cref="FeedbackActions"/> against a real, migrated database kept under this test
/// project's own bin folder and removed when the test ends, with a fake <see cref="IFeedbackSource"/>
/// (REQ-UI-062, REQ-FN-073, REQ-UI-063, REQ-FN-074).
/// </summary>
public sealed class FeedbackActionsTests : IDisposable
{
    private const string Detail = "Queued three runs, restarted Chatur, and they came back in a different order.";

    private readonly string objDatabasePath = Path.Combine(AppContext.BaseDirectory, $"feedback-tests-{Guid.NewGuid():N}.db");
    private readonly FakeSource objSource = new();

    /// <summary>When the owner sends an entry, then App Manager takes it and the list shows it as raised (REQ-UI-062).</summary>
    [Fact(DisplayName = "REQ-UI-062 a sent entry is raised and listed as raised")]
    public async Task SentEntryIsRaisedAndListed()
    {
        var vActions = Create();

        var vEntry = await vActions.SendAsync(new FeedbackDraft(FeedbackAbout.Chatur, FeedbackSeverity.Major, "The queue forgets its order", Detail, true));
        var vView = await vActions.ListAsync();

        Assert.Equal(FeedbackState.Raised, vEntry.State);
        Assert.Single(objSource.Raised);
        Assert.Equal(FeedbackState.Raised, vView.Entries.Single().State);
        Assert.Equal("test-001", vView.Entries.Single().RemoteNumber);
        Assert.Equal(0, vView.WaitingCount);
        Assert.NotNull(objSource.Raised[0].Environment);
    }

    /// <summary>When the owner picks a library, then the entry carries its name to the source and into the list (REQ-FN-073).</summary>
    [Fact(DisplayName = "REQ-FN-073 an entry carries the library name the owner picked")]
    public async Task EntryCarriesTheLibraryName()
    {
        var vActions = Create();

        await vActions.SendAsync(new FeedbackDraft(FeedbackAbout.TrBlazeUI, FeedbackSeverity.Minor, "A header covers the first row", Detail, false));

        Assert.Equal(FeedbackAbout.TrBlazeUI, objSource.Raised.Single().About);
        Assert.Null(objSource.Raised.Single().Environment);
        Assert.Equal(FeedbackAbout.TrBlazeUI, (await vActions.ListAsync()).Entries.Single().About);
    }

    /// <summary>When the name is unknown or the wording is too short, then nothing is kept or sent.</summary>
    [Theory(DisplayName = "REQ-FN-073 an unknown name or short wording is refused")]
    [InlineData("Somebody else", "A fine title", Detail)]
    [InlineData(FeedbackAbout.Chatur, "abc", Detail)]
    [InlineData(FeedbackAbout.Chatur, "A fine title", "too short")]
    public async Task BadDraftIsRefused(string aAbout, string aTitle, string aDetail)
    {
        var vActions = Create();

        await Assert.ThrowsAsync<ArgumentException>(() => vActions.SendAsync(new FeedbackDraft(aAbout, FeedbackSeverity.Major, aTitle, aDetail, false)));

        Assert.Empty(objSource.Raised);
        Assert.Empty((await vActions.ListAsync()).Entries);
    }

    /// <summary>When the network is down, then the entry is kept as waiting and sent once, without another Send, when it returns (REQ-FN-074).</summary>
    [Fact(DisplayName = "REQ-FN-074 an entry kept while the network is down is sent when it returns")]
    public async Task EntryIsKeptThenSentWhenNetworkReturns()
    {
        var vActions = Create();
        objSource.NetworkDown = true;

        var vKept = await vActions.SendAsync(new FeedbackDraft(FeedbackAbout.TechieRag, FeedbackSeverity.Major, "Routing picks the costly model", Detail, false));
        var vStillDown = await vActions.ListAsync();
        objSource.NetworkDown = false;
        var vBack = await vActions.ListAsync();
        var vAgain = await vActions.ListAsync();

        Assert.Equal(FeedbackState.Waiting, vKept.State);
        Assert.Equal(1, vStillDown.WaitingCount);
        Assert.Equal(FeedbackState.Raised, vBack.Entries.Single().State);
        Assert.Equal(0, vBack.WaitingCount);
        Assert.Single(objSource.Raised);
        Assert.Single(vAgain.Entries);
    }

    /// <summary>When the source refuses an entry, then it is kept and the refusal's words are shown with it.</summary>
    [Fact(DisplayName = "REQ-FN-074 an entry the source refuses is kept with the reason")]
    public async Task RefusedEntryIsKeptWithTheReason()
    {
        var vActions = Create();
        objSource.Refusal = "Sign in again to send feedback.";

        var vEntry = await vActions.SendAsync(new FeedbackDraft(FeedbackAbout.Chatur, FeedbackSeverity.Blocker, "It will not start", Detail, false));

        Assert.Equal(FeedbackState.Waiting, vEntry.State);
        Assert.Equal("Not sent: Sign in again to send feedback.", vEntry.Answer);
    }

    /// <summary>When an entry has been answered, then its row shows the state and the answer; once resolved it is fixed (REQ-UI-063).</summary>
    [Fact(DisplayName = "REQ-UI-063 an answered entry shows its state and its answer")]
    public async Task AnsweredEntryShowsStateAndAnswer()
    {
        var vActions = Create();
        await vActions.SendAsync(new FeedbackDraft(FeedbackAbout.TrBlazeUI, FeedbackSeverity.Major, "The switch is not read out", Detail, false));

        objSource.State = new FeedbackRemoteState(false, "Fixed in 2.1.4, out this week");
        var vAnswered = (await vActions.ListAsync()).Entries.Single();
        objSource.State = new FeedbackRemoteState(true, "Released on 20 Sep");
        var vFixed = (await vActions.ListAsync()).Entries.Single();

        Assert.Equal(FeedbackState.Answered, vAnswered.State);
        Assert.Equal("Fixed in 2.1.4, out this week", vAnswered.Answer);
        Assert.Equal(FeedbackState.Fixed, vFixed.State);
        Assert.Equal("Released on 20 Sep", vFixed.Answer);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(objDatabasePath))
        {
            File.Delete(objDatabasePath);
        }
    }

    private FeedbackActions Create()
    {
        var vConnectionString = $"Data Source={objDatabasePath}";
        Assert.True(ChaturDbMigrator.Migrate(vConnectionString).Successful);
        return new FeedbackActions(new TestDbConnectionFactory(vConnectionString), objSource, new FixedClock(), NullLogger<FeedbackActions>.Instance);
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow { get; } = new(2026, 11, 20, 9, 12, 0, DateTimeKind.Utc);
    }

    private sealed class FakeSource : IFeedbackSource
    {
        public List<FeedbackRemoteRequest> Raised { get; } = [];

        public bool NetworkDown { get; set; }

        public string? Refusal { get; set; }

        public FeedbackRemoteState State { get; set; } = new(false, null);

        public Task<FeedbackRemoteRaised> RaiseAsync(FeedbackRemoteRequest aRequest, CancellationToken aCt = default)
        {
            if (NetworkDown)
            {
                throw new FeedbackUnreachableException("down");
            }

            if (Refusal is not null)
            {
                throw new FeedbackRefusedException(Refusal);
            }

            Raised.Add(aRequest);
            return Task.FromResult(new FeedbackRemoteRaised(Raised.Count.ToString(), "test-" + Raised.Count.ToString("000")));
        }

        public Task<FeedbackRemoteState?> ReadAsync(string aRemoteId, CancellationToken aCt = default)
        {
            if (NetworkDown)
            {
                throw new FeedbackUnreachableException("down");
            }

            return Task.FromResult<FeedbackRemoteState?>(State);
        }
    }
}
