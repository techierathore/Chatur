using Chatur.Core.Actions;

namespace Chatur.Core.Feedback;

/// <summary>What is sent to the source when an entry is raised.</summary>
/// <param name="About">The name the owner picked (REQ-FN-073).</param>
/// <param name="Severity">How bad the owner said it was.</param>
/// <param name="Title">The one-line title, without the name.</param>
/// <param name="Detail">The description.</param>
/// <param name="Environment">Machine details, when the owner attached them.</param>
public sealed record FeedbackRemoteRequest(string About, FeedbackSeverity Severity, string Title, string Detail, string? Environment);

/// <summary>What the source answers once it has taken an entry.</summary>
/// <param name="RemoteId">The source's own id, used to ask for the entry again.</param>
/// <param name="RemoteNumber">The number the owner can quote, e.g. <c>chatur-005</c>.</param>
public sealed record FeedbackRemoteRaised(string RemoteId, string RemoteNumber);

/// <summary>What the source says about an entry it holds.</summary>
/// <param name="IsFinished">Whether the entry is resolved or closed.</param>
/// <param name="Answer">The latest public answer, or <see langword="null"/> when nobody has written back.</param>
public sealed record FeedbackRemoteState(bool IsFinished, string? Answer);

/// <summary>The source cannot be reached: no network, a timeout or a server error. The entry is kept and tried again (REQ-FN-074).</summary>
public sealed class FeedbackUnreachableException : Exception
{
    /// <summary>Creates the exception.</summary>
    public FeedbackUnreachableException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="aMessage">What went wrong.</param>
    public FeedbackUnreachableException(string aMessage)
        : base(aMessage)
    {
    }

    /// <summary>Creates the exception with a message and its cause.</summary>
    /// <param name="aMessage">What went wrong.</param>
    /// <param name="aInner">The cause.</param>
    public FeedbackUnreachableException(string aMessage, Exception aInner)
        : base(aMessage, aInner)
    {
    }
}

/// <summary>The source answered and said no: the entry is not acceptable as it stands. The message is the source's own words.</summary>
public sealed class FeedbackRefusedException : Exception
{
    /// <summary>Creates the exception.</summary>
    public FeedbackRefusedException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="aMessage">The source's own words.</param>
    public FeedbackRefusedException(string aMessage)
        : base(aMessage)
    {
    }

    /// <summary>Creates the exception with a message and its cause.</summary>
    /// <param name="aMessage">The source's own words.</param>
    /// <param name="aInner">The cause.</param>
    public FeedbackRefusedException(string aMessage, Exception aInner)
        : base(aMessage, aInner)
    {
    }
}

/// <summary>
/// Where feedback goes. In the real app it is App Manager (<see cref="AppManagerFeedbackSource"/>);
/// the web harness, a test-only host, can switch to a test double from its own configuration file.
/// </summary>
public interface IFeedbackSource
{
    /// <summary>Raises one entry.</summary>
    /// <param name="aRequest">The entry.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    /// <exception cref="FeedbackUnreachableException">The source cannot be reached.</exception>
    /// <exception cref="FeedbackRefusedException">The source refused the entry.</exception>
    Task<FeedbackRemoteRaised> RaiseAsync(FeedbackRemoteRequest aRequest, CancellationToken aCt = default);

    /// <summary>Reads where an entry the source holds now stands.</summary>
    /// <param name="aRemoteId">The id <see cref="RaiseAsync"/> returned.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    /// <returns>The state, or <see langword="null"/> when the source no longer knows the entry.</returns>
    /// <exception cref="FeedbackUnreachableException">The source cannot be reached.</exception>
    Task<FeedbackRemoteState?> ReadAsync(string aRemoteId, CancellationToken aCt = default);
}
