namespace Chatur.Core.Integrations;

/// <summary>One row of the catalogue: an action's fixed words and address, and whether it starts on.</summary>
/// <param name="Code">The short code.</param>
/// <param name="Name">The name on the screen.</param>
/// <param name="Does">What it does.</param>
/// <param name="StartedBy">What starts it.</param>
/// <param name="Method">The HTTP method.</param>
/// <param name="Path">The path.</param>
/// <param name="Built">Whether Chatur answers this address yet.</param>
/// <param name="OnByDefault">Whether it is on until the owner says otherwise.</param>
public sealed record CataloguedAction(string Code, string Name, string Does, string StartedBy, string Method, string Path, bool Built, bool OnByDefault);

/// <summary>
/// Every action Chatur offers to outside tools, in screen order (mockups/integrations.html). Only
/// <c>start</c> is answered today; the others are listed so the owner sees what is coming, and are never on.
/// </summary>
public static class PublishedActionCatalogue
{
    /// <summary>The code of "Start a process".</summary>
    public const string StartCode = "start";

    /// <summary>The path every address begins with.</summary>
    public const string Root = "/v1/chatur";

    /// <summary>All six actions.</summary>
    public static IReadOnlyList<CataloguedAction> All { get; } =
    [
        new(StartCode, "Start a process", "Begins a named process and gives back a run number", "Another tool, or a schedule", "POST", $"{Root}/process/start", true, true),
        new("board", "Read the board", "Gives back every card and its column", "Another tool", "GET", $"{Root}/board", false, true),
        new("change", "Propose a change", "Opens a change for you to accept — never applied on its own", "Another tool", "POST", $"{Root}/change", false, false),
        new("verify", "Run the verifier", "Checks a phase and gives back a verdict per requirement", "A process, or another tool", "POST", $"{Root}/verify", false, true),
        new("metrics", "Fetch the measurements", "Gives back runs, misses and token counts", "Another tool", "GET", $"{Root}/metrics", false, true),
        new("issue", "Raise an issue", "Sends an entry to App Manager, as the feedback screen does", "Chatur, or another tool", "POST", $"{Root}/issue", false, false),
    ];

    /// <summary>Finds an action by its code.</summary>
    /// <param name="aCode">The code.</param>
    /// <returns>The action, or <see langword="null"/> when none has that code.</returns>
    public static CataloguedAction? Find(string aCode) => All.FirstOrDefault(aAction => string.Equals(aAction.Code, aCode, StringComparison.OrdinalIgnoreCase));
}
