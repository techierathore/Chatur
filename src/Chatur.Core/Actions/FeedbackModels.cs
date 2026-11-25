namespace Chatur.Core.Actions;

/// <summary>Where an entry is on its way (REQ-UI-062, REQ-UI-063, REQ-FN-074).</summary>
public enum FeedbackState
{
    /// <summary>Kept on this machine; App Manager has not taken it yet, and it goes as soon as it can (REQ-FN-074).</summary>
    Waiting,

    /// <summary>App Manager has taken it (REQ-UI-062).</summary>
    Raised,

    /// <summary>Someone has written back; the answer is in the row (REQ-UI-063).</summary>
    Answered,

    /// <summary>The thing is dealt with.</summary>
    Fixed,
}

/// <summary>How bad the owner says it is.</summary>
public enum FeedbackSeverity
{
    /// <summary>Blocker — the owner cannot carry on.</summary>
    Blocker,

    /// <summary>Major — it costs time.</summary>
    Major,

    /// <summary>Minor — it is untidy.</summary>
    Minor,
}

/// <summary>What an entry can be about, and the one name each carries (REQ-FN-073).</summary>
public static class FeedbackAbout
{
    /// <summary>Chatur itself.</summary>
    public const string Chatur = "Chatur";

    /// <summary>The TrBlazeUI library.</summary>
    public const string TrBlazeUI = "TrBlazeUI";

    /// <summary>The TechieRag library.</summary>
    public const string TechieRag = "TechieRag";

    /// <summary>App Manager.</summary>
    public const string AppManager = "App Manager";

    /// <summary>Every name an entry may carry, in the order the picker shows them.</summary>
    public static IReadOnlyList<string> All { get; } = [Chatur, TrBlazeUI, TechieRag, AppManager];

    /// <summary>Whether the name is one of <see cref="All"/>.</summary>
    /// <param name="aName">The name to check.</param>
    public static bool IsKnown(string? aName) => All.Contains(aName ?? string.Empty, StringComparer.Ordinal);
}

/// <summary>The lengths the form and <see cref="Actions.IFeedbackActions.SendAsync"/> both hold to.</summary>
public static class FeedbackLimits
{
    /// <summary>Fewest characters in a title.</summary>
    public const int TitleMin = 4;

    /// <summary>Most characters in a title.</summary>
    public const int TitleMax = 80;

    /// <summary>Fewest characters in the description.</summary>
    public const int DetailMin = 20;

    /// <summary>Most characters in the description.</summary>
    public const int DetailMax = 4000;
}

/// <summary>What the owner typed on the form.</summary>
/// <param name="About">One of <see cref="FeedbackAbout.All"/> (REQ-FN-073).</param>
/// <param name="Severity">How bad it is.</param>
/// <param name="Title">One line, <see cref="FeedbackLimits.TitleMin"/> to <see cref="FeedbackLimits.TitleMax"/> characters.</param>
/// <param name="Detail">What happened, at least <see cref="FeedbackLimits.DetailMin"/> characters.</param>
/// <param name="AttachMachineDetails">Whether Chatur's version and the operating system go with it.</param>
public sealed record FeedbackDraft(string About, FeedbackSeverity Severity, string Title, string Detail, bool AttachMachineDetails);

/// <summary>One entry as the list shows it.</summary>
/// <param name="FeedbackEntryId">This machine's number for it; shown as <c>FB-n</c>.</param>
/// <param name="About">The name the owner picked (REQ-FN-073).</param>
/// <param name="Severity">How bad the owner said it was.</param>
/// <param name="Title">The one-line title.</param>
/// <param name="Detail">The description.</param>
/// <param name="CreatedUtc">When the owner pressed Send.</param>
/// <param name="State">Where it is on its way.</param>
/// <param name="RemoteNumber">App Manager's issue number once it has taken the entry.</param>
/// <param name="Answer">The latest answer (REQ-UI-063), or the reason App Manager refused it while it is waiting; otherwise <see langword="null"/>.</param>
public sealed record FeedbackEntry(
    int FeedbackEntryId,
    string About,
    FeedbackSeverity Severity,
    string Title,
    string Detail,
    DateTime CreatedUtc,
    FeedbackState State,
    string? RemoteNumber,
    string? Answer);

/// <summary>The list, newest first.</summary>
/// <param name="Entries">Every entry the account has raised.</param>
/// <param name="WaitingCount">How many are still kept on this machine (REQ-FN-074).</param>
public sealed record FeedbackView(IReadOnlyList<FeedbackEntry> Entries, int WaitingCount);
