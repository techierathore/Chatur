namespace Chatur.Core.Actions;

/// <summary>
/// Compare models (UI Design phase 4 "Screen: Compare models", BRD-291 to BRD-297): one task given to two to four
/// connected models side by side on the open project, each measured and kept in Chatur's database and in the project's
/// own <c>docs/metrics</c> in TfLens's shape.
/// </summary>
public interface ICompareModelsActions
{
    /// <summary>The fewest models one comparison takes.</summary>
    const int MinModels = 2;

    /// <summary>The most models one comparison takes.</summary>
    const int MaxModels = 4;

    /// <summary>Every model of every connected provider, labelled with its provider (REQ-FN-327).</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<CompareModelOption>> ModelsAsync(CancellationToken aCt = default);

    /// <summary>
    /// Why a pick of <paramref name="aCount"/> models cannot run, in plain words, or <see langword="null"/> when it can
    /// (REQ-FN-327: two to four; one or five is refused with the reason).
    /// </summary>
    /// <param name="aCount">How many models are picked.</param>
    string? PickRefusal(int aCount);

    /// <summary>
    /// Runs every picked model on the task at the same time and keeps the result (REQ-FN-327 to REQ-FN-330). A question
    /// gives each model the project's read tool only, rooted at the project; a task that changes files gives each model
    /// its own copy under <c>&lt;project&gt;/bin/chatur-compare/&lt;comparison&gt;/&lt;model&gt;</c> and removes the whole
    /// <c>bin/chatur-compare/&lt;comparison&gt;</c> folder when the run ends, also when a model fails or the run is
    /// cancelled. A model that fails keeps its error in its own result; the others still finish.
    /// </summary>
    /// <param name="aRequest">The project, models, kind and task.</param>
    /// <param name="aOnChange">Called with the comparison as it starts and each time a model ends; may be called from any thread.</param>
    /// <param name="aCt">A token that stops every model.</param>
    /// <returns>The finished comparison.</returns>
    /// <exception cref="ArgumentException">The pick is not two to four connected models, the task is blank, or the project is not known.</exception>
    Task<Comparison> RunAsync(CompareRequest aRequest, Func<Comparison, Task>? aOnChange = null, CancellationToken aCt = default);

    /// <summary>Past comparisons on a project, newest first (REQ-FN-330).</summary>
    /// <param name="aProjectId">The project.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<ComparisonSummary>> HistoryAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>Opens a past comparison again, with its results and marks (REQ-FN-330, REQ-UI-133).</summary>
    /// <param name="aComparisonId">The comparison.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The comparison, or <see langword="null"/> when there is none by that id.</returns>
    Task<Comparison?> OpenAsync(int aComparisonId, CancellationToken aCt = default);

    /// <summary>
    /// Marks one model's result right or wrong (REQ-UI-133, REQ-FN-330): kept on the result, and written to the project's
    /// <c>docs/metrics/gates.jsonl</c> as a new record whose <c>run_id</c> is that model's run; a changed mark is a new
    /// record, never an edit, and the latest one stands.
    /// </summary>
    /// <param name="aResultId">The result.</param>
    /// <param name="aRight"><see langword="true"/> for right, <see langword="false"/> for wrong.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <returns>The comparison the result belongs to, with the new mark.</returns>
    /// <exception cref="InvalidOperationException">The result is not known or its model is still running.</exception>
    Task<Comparison> MarkAsync(int aResultId, bool aRight, CancellationToken aCt = default);
}
