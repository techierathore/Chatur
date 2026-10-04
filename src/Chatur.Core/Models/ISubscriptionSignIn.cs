using Chatur.Core.Actions;

namespace Chatur.Core.Models;

/// <summary>
/// Runs a ChatGPT subscription's device-code sign-in through TechieRag (REQ-FN-016). A seam so
/// <see cref="ProviderActions"/> can be unit-tested without a real sign-in (Coding Standards
/// §Testability). The session it obtains is kept by TechieRag in the
/// <see cref="TechieRag.Abstractions.ISubscriptionSessionStore"/> it was built with — in the app,
/// <see cref="SecretSubscriptionSessionStore"/>.
/// </summary>
public interface ISubscriptionSignIn
{
    /// <summary>
    /// Signs in and completes only once the owner has authorised the code (or the code expires).
    /// </summary>
    /// <param name="aModelIdentifier">The model the sign-in is for.</param>
    /// <param name="aOnCode">Called once with the page to open and the code to enter; the caller shows the code and opens the browser, then returns while the sign-in keeps waiting.</param>
    /// <param name="aCt">A token that cancels the sign-in; cancelling abandons a sign-in in progress.</param>
    /// <exception cref="TechieRag.Llm.SubscriptionSignInException">The vendor refused, or the code expired before it was authorised.</exception>
    Task SignInAsync(string aModelIdentifier, Func<SignInCodePrompt, CancellationToken, Task> aOnCode, CancellationToken aCt = default);
}
