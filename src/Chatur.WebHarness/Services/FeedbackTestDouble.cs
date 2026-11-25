using System.Globalization;
using Chatur.Core.Feedback;

namespace Chatur.WebHarness.Services;

/// <summary>
/// A stand-in for App Manager's Issue service, held in memory, for the browser checks (REQ-UI-062,
/// REQ-FN-073, REQ-UI-063, REQ-FN-074). The web harness, a test-only host, uses it only when its own
/// <c>appsettings.json</c> says <c>Feedback:Source</c> is <c>TestDouble</c>; the real app never does.
/// The test routes in <c>Program.cs</c> (Development only) switch the network off, answer an entry and
/// read what the "App Manager" holds, so a test can drive what a real server would not let it.
/// </summary>
public sealed class FeedbackTestDouble : IFeedbackSource
{
    private readonly object objLock = new();
    private readonly List<Issue> objIssues = [];
    private bool objNetworkDown;

    /// <summary>Whether the pretend network is down: every call then fails as if unreachable.</summary>
    public bool NetworkDown
    {
        get { lock (objLock) { return objNetworkDown; } }
        set { lock (objLock) { objNetworkDown = value; } }
    }

    /// <summary>What the pretend server holds, oldest first.</summary>
    public IReadOnlyList<IssueSnapshot> Issues()
    {
        lock (objLock)
        {
            return [.. objIssues.Select(aIssue => new IssueSnapshot(aIssue.Id, aIssue.Number, aIssue.Request.About, aIssue.Request.Severity.ToString(), aIssue.Request.Title, aIssue.Request.Detail, aIssue.Request.Environment, aIssue.Finished, aIssue.Answer))];
        }
    }

    /// <summary>Writes an answer on the issue whose title holds the text; marks it finished when asked. Returns whether one was found.</summary>
    /// <param name="aTitleContains">Text in the title.</param>
    /// <param name="aAnswer">The answer to show.</param>
    /// <param name="aFinished">Whether the issue is also resolved.</param>
    public bool Answer(string aTitleContains, string aAnswer, bool aFinished)
    {
        lock (objLock)
        {
            var vIssue = objIssues.LastOrDefault(aIssue => aIssue.Request.Title.Contains(aTitleContains, StringComparison.Ordinal));
            if (vIssue is null)
            {
                return false;
            }

            vIssue.Answer = aAnswer;
            vIssue.Finished = aFinished;
            return true;
        }
    }

    /// <summary>Forgets every issue and brings the pretend network back.</summary>
    public void Reset()
    {
        lock (objLock)
        {
            objIssues.Clear();
            objNetworkDown = false;
        }
    }

    /// <inheritdoc />
    public Task<FeedbackRemoteRaised> RaiseAsync(FeedbackRemoteRequest aRequest, CancellationToken aCt = default)
    {
        lock (objLock)
        {
            if (objNetworkDown)
            {
                throw new FeedbackUnreachableException("The test network is down.");
            }

            var vId = objIssues.Count + 1;
            var vIssue = new Issue(vId.ToString(CultureInfo.InvariantCulture), "test-" + vId.ToString("000", CultureInfo.InvariantCulture), aRequest);
            objIssues.Add(vIssue);
            return Task.FromResult(new FeedbackRemoteRaised(vIssue.Id, vIssue.Number));
        }
    }

    /// <inheritdoc />
    public Task<FeedbackRemoteState?> ReadAsync(string aRemoteId, CancellationToken aCt = default)
    {
        lock (objLock)
        {
            if (objNetworkDown)
            {
                throw new FeedbackUnreachableException("The test network is down.");
            }

            var vIssue = objIssues.FirstOrDefault(aIssue => aIssue.Id == aRemoteId);
            return Task.FromResult(vIssue is null ? null : new FeedbackRemoteState(vIssue.Finished, vIssue.Answer));
        }
    }

    /// <summary>One issue as a test reads it.</summary>
    public sealed record IssueSnapshot(string Id, string Number, string About, string Severity, string Title, string Detail, string? Environment, bool Finished, string? Answer);

    private sealed class Issue(string aId, string aNumber, FeedbackRemoteRequest aRequest)
    {
        public string Id { get; } = aId;

        public string Number { get; } = aNumber;

        public FeedbackRemoteRequest Request { get; } = aRequest;

        public bool Finished { get; set; }

        public string? Answer { get; set; }
    }
}
