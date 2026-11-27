namespace Chatur.Core.Actions;

/// <summary>What a comparison asks of its models (REQ-FN-328, REQ-FN-329).</summary>
public enum CompareKind
{
    /// <summary>"Question, changes nothing": each model reads the open project and answers; it has no tool that writes.</summary>
    Question,

    /// <summary>"Task that changes files": each model works in its own copy under the project's bin folder.</summary>
    Change
}

/// <summary>One model Compare models offers: a model of a connected provider (REQ-FN-327).</summary>
/// <param name="ModelId">The row's identity in the <c>Model</c> table.</param>
/// <param name="Identifier">The model's own name at the provider, e.g. <c>claude-sonnet-5-5</c>.</param>
/// <param name="ProviderName">The name the owner gave the provider.</param>
/// <param name="Connector">The provider's connector, e.g. <c>Anthropic</c>.</param>
/// <param name="Slug">The model's name made safe for a test id and a folder name; unique in the list.</param>
public sealed record CompareModelOption(int ModelId, string Identifier, string ProviderName, string Connector, string Slug);

/// <summary>What the person asked for on Compare models.</summary>
/// <param name="ProjectId">The open project.</param>
/// <param name="ModelIds">The models picked, two to four, in the order picked.</param>
/// <param name="Kind">Question or a task that changes files.</param>
/// <param name="Task">The task, the same for every model.</param>
public sealed record CompareRequest(int ProjectId, IReadOnlyList<int> ModelIds, CompareKind Kind, string Task);

/// <summary>One file a model changed in its own copy (REQ-FN-329, REQ-UI-132).</summary>
/// <param name="Path">The path relative to the project, with forward slashes.</param>
/// <param name="Change"><c>A</c> added, <c>M</c> modified or <c>D</c> deleted.</param>
/// <param name="LinesAdded">Lines in the copy that are not in the project's file.</param>
/// <param name="LinesRemoved">Lines in the project's file that are not in the copy.</param>
public sealed record CompareChangedFile(string Path, string Change, int LinesAdded, int LinesRemoved);

/// <summary>One model's part of a comparison (REQ-UI-132, REQ-UI-133).</summary>
/// <param name="ResultId">The row's identity in <c>ComparisonResult</c>.</param>
/// <param name="Position">1 for the first model picked, 2 for the next, and so on.</param>
/// <param name="ModelId">The model's row, or <see langword="null"/> when it has since been removed.</param>
/// <param name="Model">The model's name at the provider.</param>
/// <param name="ProviderName">The provider's name.</param>
/// <param name="Connector">The provider's connector.</param>
/// <param name="State"><c>running</c>, <c>done</c> or <c>failed</c>.</param>
/// <param name="StartedUtc">When this model started; written with milliseconds as its run record's <c>started</c>.</param>
/// <param name="DurationMs">Wall-clock time, once it has ended.</param>
/// <param name="TokensIn">Tokens in, as TechieRag reported them over every call.</param>
/// <param name="TokensOut">Tokens out, as TechieRag reported them over every call.</param>
/// <param name="CostUsd">TechieRag's own price of the calls, or <see langword="null"/> when it gave none ("no price").</param>
/// <param name="Answer">The model's answer, as Markdown.</param>
/// <param name="Error">Why the model failed, in plain words, when it did.</param>
/// <param name="Files">The files it changed in its copy; empty for a question.</param>
/// <param name="Mark"><c>right</c>, <c>wrong</c>, or <see langword="null"/> when not marked.</param>
public sealed record CompareResult(
    int ResultId,
    int Position,
    int? ModelId,
    string Model,
    string ProviderName,
    string Connector,
    string State,
    DateTime StartedUtc,
    long? DurationMs,
    long? TokensIn,
    long? TokensOut,
    decimal? CostUsd,
    string? Answer,
    string? Error,
    IReadOnlyList<CompareChangedFile> Files,
    string? Mark);

/// <summary>One comparison, with each model's result (REQ-FN-327 to REQ-FN-330).</summary>
/// <param name="ComparisonId">The row's identity in <c>Comparison</c>.</param>
/// <param name="ProjectId">The project it ran on.</param>
/// <param name="Kind">Question or a task that changes files.</param>
/// <param name="Task">The task.</param>
/// <param name="State"><c>running</c>, <c>done</c> (at least one model answered) or <c>failed</c> (none did).</param>
/// <param name="StartedUtc">When Run was pressed.</param>
/// <param name="EndedUtc">When the last model ended.</param>
/// <param name="ProjectUnchanged">For a question: whether every file of the project was the same afterwards; <see langword="null"/> while running or for a task.</param>
/// <param name="CopiesRemoved">For a task that changes files: whether every copy was removed when the run ended.</param>
/// <param name="Results">One per model, in the order picked.</param>
public sealed record Comparison(
    int ComparisonId,
    int ProjectId,
    CompareKind Kind,
    string Task,
    string State,
    DateTime StartedUtc,
    DateTime? EndedUtc,
    bool? ProjectUnchanged,
    bool? CopiesRemoved,
    IReadOnlyList<CompareResult> Results);

/// <summary>One line of Past comparisons (REQ-FN-330).</summary>
/// <param name="ComparisonId">The comparison to open again.</param>
/// <param name="StartedUtc">When it ran.</param>
/// <param name="Kind">Question or a task that changes files.</param>
/// <param name="Task">The task.</param>
/// <param name="Models">The models, in the order picked.</param>
/// <param name="Right">How many were marked right.</param>
/// <param name="Wrong">How many were marked wrong.</param>
/// <param name="NotMarked">How many are not marked.</param>
public sealed record ComparisonSummary(int ComparisonId, DateTime StartedUtc, CompareKind Kind, string Task, IReadOnlyList<string> Models, int Right, int Wrong, int NotMarked);
