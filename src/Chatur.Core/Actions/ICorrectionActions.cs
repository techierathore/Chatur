namespace Chatur.Core.Actions;

/// <summary>
/// Listing, keeping and undoing corrections Chatur made about itself, and exporting the kept ones as
/// seed data (Architecture §7 "Roles and rules"; page Settings ▸ Corrections).
/// </summary>
public interface ICorrectionActions
{
    /// <summary>Every correction, proposed or settled (REQ-UI-034).</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<Correction>> ListAsync(CancellationToken aCt = default);

    /// <summary>
    /// Changes the wording of a role, a rule or a process step as a versioned save and writes the
    /// correction down — what changed, why and when — for the owner to keep or undo (REQ-UI-034).
    /// </summary>
    /// <param name="aKind"><c>"role"</c>, <c>"rule"</c> or <c>"step"</c>.</param>
    /// <param name="aTarget">A role's code, a rule's id, or <c>"process/step"</c> for a step.</param>
    /// <param name="aNewWording">The wording that replaces the current one.</param>
    /// <param name="aReason">Why the current wording was wrong.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <returns>The correction row, status <c>"Proposed"</c>.</returns>
    /// <exception cref="ArgumentException">The kind is unknown, the reason or wording is empty, or a role's wording is under 40 characters.</exception>
    /// <exception cref="KeyNotFoundException">Nothing has that target.</exception>
    Task<Correction> CorrectWordingAsync(string aKind, string aTarget, string aNewWording, string aReason, CancellationToken aCt = default);

    /// <summary>
    /// Keeps a correction in force (REQ-FN-037).
    /// </summary>
    /// <param name="aCorrectionId">The correction to keep.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    Task KeepAsync(int aCorrectionId, CancellationToken aCt = default);

    /// <summary>
    /// Undoes a correction: the wording before it returns (REQ-FN-038).
    /// </summary>
    /// <param name="aCorrectionId">The correction to undo.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    Task UndoAsync(int aCorrectionId, CancellationToken aCt = default);

    /// <summary>
    /// Exports the seed data for the next build: every kept correction, none that were undone
    /// (REQ-FN-039).
    /// </summary>
    /// <param name="aCt">A token that cancels the export.</param>
    /// <returns>The exported seed data's content.</returns>
    Task<string> ExportSeedAsync(CancellationToken aCt = default);
}
