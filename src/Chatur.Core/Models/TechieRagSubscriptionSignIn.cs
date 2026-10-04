using Chatur.Core.Actions;
using Microsoft.Extensions.Logging;
using TechieRag.Abstractions;
using TechieRag.Llm;

namespace Chatur.Core.Models;

/// <summary>
/// <see cref="ISubscriptionSignIn"/> over TechieRag's <see cref="ChatGptSubscriptionLlmProvider"/>
/// (REQ-FN-016; "Every model call goes through TechieRag"). Chatur runs no OAuth of its own: TechieRag
/// asks OpenAI for the device code and polls for the authorisation; this class only relays the code to
/// the caller and gives TechieRag the secret-store-backed session store.
/// </summary>
public sealed class TechieRagSubscriptionSignIn : ISubscriptionSignIn
{
    private readonly ISubscriptionSessionStore objSessions;
    private readonly ILoggerFactory objLoggerFactory;

    /// <summary>Creates the sign-in.</summary>
    /// <param name="aSessions">Where TechieRag keeps the signed-in session.</param>
    /// <param name="aLoggerFactory">Gives TechieRag's provider its logger; it never receives a token.</param>
    public TechieRagSubscriptionSignIn(ISubscriptionSessionStore aSessions, ILoggerFactory aLoggerFactory)
    {
        objSessions = aSessions;
        objLoggerFactory = aLoggerFactory;
    }

    /// <inheritdoc />
    public async Task SignInAsync(string aModelIdentifier, Func<SignInCodePrompt, CancellationToken, Task> aOnCode, CancellationToken aCt = default)
    {
        // The provider itself, not TechieRagBuilder.GetLlmProvider(): the builder hands back its retry
        // wrapper, which has no SignInAsync, so the sign-in is started on TechieRag's own public
        // ChatGptSubscriptionLlmProvider directly.
        var vOptions = new ChatGptSubscriptionOptions { Model = aModelIdentifier, SessionStore = objSessions };
        using var vProvider = new ChatGptSubscriptionLlmProvider(
            (aPrompt, aToken) => aOnCode(new SignInCodePrompt(aPrompt.VerificationUri.AbsoluteUri, aPrompt.UserCode, aPrompt.ExpiresAt), aToken),
            vOptions,
            objLoggerFactory.CreateLogger<ChatGptSubscriptionLlmProvider>());

        await vProvider.SignInAsync(aCt).ConfigureAwait(false);
    }
}
