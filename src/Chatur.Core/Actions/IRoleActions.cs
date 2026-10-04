namespace Chatur.Core.Actions;

/// <summary>
/// Reading, editing, versioning, exporting and importing roles, rights, commands and rules
/// (Architecture §7 "Roles and rules"; page Settings ▸ Agents).
/// </summary>
public interface IRoleActions
{
    /// <summary>Every seeded role (REQ-UI-025, REQ-UI-026).</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<RoleSummary>> RolesAsync(CancellationToken aCt = default);

    /// <summary>A role's full detail (REQ-UI-026).</summary>
    /// <param name="aRoleId">The role to read.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<RoleDetail> RoleAsync(int aRoleId, CancellationToken aCt = default);

    /// <summary>
    /// Saves a new version of a role's wording, commands and rules (REQ-UI-027, REQ-UI-029).
    /// </summary>
    /// <param name="aRoleId">The role to change.</param>
    /// <param name="aWording">The new wording — at least 40 characters.</param>
    /// <param name="aCommands">The role's commands after the save.</param>
    /// <param name="aRules">The role's rules after the save.</param>
    /// <param name="aCt">A token that cancels the save.</param>
    Task<RoleDetail> SaveAsync(int aRoleId, string aWording, IReadOnlyList<string> aCommands, IReadOnlyList<string> aRules, CancellationToken aCt = default);

    /// <summary>
    /// Adds a new agent as version 1 — a name, some wording and the rights it is given (Settings ▸
    /// Agents, "Add an agent"). A right not named in <paramref name="aAllowedActions"/> is stored as
    /// withheld, so the guards read a complete answer for the new role.
    /// </summary>
    /// <param name="aName">The agent's display name; its code is made from it.</param>
    /// <param name="aWording">The agent's wording — at least 40 characters.</param>
    /// <param name="aTier">The model tier its work uses — 1 through 3.</param>
    /// <param name="aAllowedActions">The actions it may do, e.g. <c>"read-file"</c>.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <returns>The new role's detail.</returns>
    /// <exception cref="ArgumentException">The name is empty or already used, or the wording is under 40 characters.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="aTier"/> is not 1, 2 or 3.</exception>
    Task<RoleDetail> AddAsync(string aName, string aWording, int aTier, IReadOnlyCollection<string> aAllowedActions, CancellationToken aCt = default);

    /// <summary>
    /// Sets the model tier a role's work uses (REQ-UI-028). Settings ▸ Routing reads the same
    /// column through <see cref="RolesAsync"/>'s <see cref="RoleSummary.Tier"/>, so this one write
    /// is what makes the two tabs agree.
    /// </summary>
    /// <param name="aRoleId">The role to change.</param>
    /// <param name="aTier">The tier to use — 1 (strongest) through 3 (on this machine).</param>
    /// <param name="aCt">A token that cancels the save.</param>
    Task<RoleDetail> SetTierAsync(int aRoleId, int aTier, CancellationToken aCt = default);

    /// <summary>
    /// Writes every role, right, command and rule to one exportable file (REQ-FN-028).
    /// </summary>
    /// <param name="aCt">A token that cancels the export.</param>
    /// <returns>The exported file's content.</returns>
    Task<string> ExportAsync(CancellationToken aCt = default);

    /// <summary>
    /// Brings in roles, rights, commands and rules from an exported file (REQ-FN-029).
    /// </summary>
    /// <param name="aJson">The exported file's content.</param>
    /// <param name="aCt">A token that cancels the import.</param>
    Task ImportAsync(string aJson, CancellationToken aCt = default);

    /// <summary>
    /// Marks a requirement verified. Refused for every role but the Verifier (REQ-FN-030).
    /// </summary>
    /// <param name="aRequirementId">The requirement to mark.</param>
    /// <param name="aActingRoleId">The role asking to mark it.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <exception cref="InvalidOperationException"><paramref name="aActingRoleId"/> is not the Verifier.</exception>
    Task MarkVerifiedAsync(int aRequirementId, int aActingRoleId, CancellationToken aCt = default);
}
