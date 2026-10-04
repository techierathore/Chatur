namespace Chatur.Core.Roles;

/// <summary>
/// The shape of the file <see cref="RoleActions.ExportAsync"/> writes and
/// <see cref="RoleActions.ImportAsync"/> reads (REQ-FN-028, REQ-FN-029) — every role, right, command
/// and rule, with its version. Internal to this module: a view never sees this shape, only the
/// exported JSON text <see cref="Actions.IRoleActions.ExportAsync"/> returns.
/// </summary>
/// <param name="Schema">A constant tag identifying the file as a Chatur roles export.</param>
/// <param name="Version">The export format's own version — <c>1</c> today.</param>
/// <param name="ExportedUtc">When the file was written.</param>
/// <param name="Roles">Every role, at its current version.</param>
/// <param name="Rules">Every standing and per-role rule.</param>
internal sealed record RoleExportFile(
    string Schema,
    int Version,
    string ExportedUtc,
    IReadOnlyList<RoleExportRole> Roles,
    IReadOnlyList<RoleExportRule> Rules);

/// <summary>One role in an export file.</summary>
internal sealed record RoleExportRole(
    string Code,
    string Name,
    string Wording,
    int Version,
    string ValidFromUtc,
    int Tier,
    IReadOnlyList<RoleExportRight> Rights,
    IReadOnlyList<RoleExportCommand> Commands);

/// <summary>One <c>RoleRight</c> row in an export file.</summary>
internal sealed record RoleExportRight(string Action, bool Allowed);

/// <summary>One <c>RoleCommand</c> row in an export file.</summary>
internal sealed record RoleExportCommand(string Command, string Description);

/// <summary>One <c>Rule</c> row in an export file — <c>Scope</c> is <c>"global"</c> or a role's code.</summary>
internal sealed record RoleExportRule(string Scope, string Text, int Version);
