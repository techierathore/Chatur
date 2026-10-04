using Chatur.Core.Actions;
using Chatur.Core.Models;
using TechieRag.Abstractions;
using TechieRag.Models;

namespace Chatur.Tests.Support;

/// <summary>
/// A scripted <see cref="ISubscriptionSignIn"/> — the real one asks OpenAI for a device code and waits
/// for a person to authorise it, which a unit test never does (Coding Standards §Testability). It hands
/// the callback a fixed code, then either fails or, like TechieRag after a real sign-in, saves a session.
/// </summary>
public sealed class FakeSubscriptionSignIn : ISubscriptionSignIn
{
    /// <summary>The prompt the fake hands the callback.</summary>
    public static readonly SignInCodePrompt Prompt = new("https://auth.example/codex/device", "ABCD-1234", new DateTimeOffset(2026, 9, 30, 12, 15, 0, TimeSpan.Zero));

    /// <summary>The failure to throw once the code has been shown, or <see langword="null"/> to succeed.</summary>
    public Exception? Failure { get; set; }

    /// <summary>Where a successful sign-in saves its session, as TechieRag would; <see langword="null"/> saves nothing.</summary>
    public ISubscriptionSessionStore? SessionStore { get; set; }

    /// <summary>How many times <see cref="SignInAsync"/> was called.</summary>
    public int CallCount { get; private set; }

    /// <summary>The model identifier the last sign-in was for.</summary>
    public string? LastModelIdentifier { get; private set; }

    /// <inheritdoc />
    public async Task SignInAsync(string aModelIdentifier, Func<SignInCodePrompt, CancellationToken, Task> aOnCode, CancellationToken aCt = default)
    {
        CallCount++;
        LastModelIdentifier = aModelIdentifier;
        await aOnCode(Prompt, aCt);

        if (Failure is not null)
        {
            throw Failure;
        }

        if (SessionStore is not null)
        {
            await SessionStore.SaveAsync(new SubscriptionSession { ConnectorName = "chatgpt-subscription", AccessToken = "access-1", RefreshToken = "refresh-1" }, aCt);
        }
    }
}
