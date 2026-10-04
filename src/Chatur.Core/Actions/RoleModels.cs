namespace Chatur.Core.Actions;

/// <summary>One row of the Agents tab's role list (REQ-UI-025, REQ-UI-026).</summary>
/// <param name="RoleId">The row's identity.</param>
/// <param name="Code">The role's short code, e.g. <c>"analyst"</c>.</param>
/// <param name="Name">The role's display name.</param>
/// <param name="Tier">The model tier this role uses (REQ-UI-028).</param>
public sealed record RoleSummary(int RoleId, string Code, string Name, int Tier);

/// <summary>One past version of a role, for its history list (REQ-UI-029).</summary>
/// <param name="Version">The version number.</param>
/// <param name="ValidFromUtc">When this version took effect.</param>
/// <param name="WhatChanged">A short description of what changed in this version.</param>
/// <param name="Wording">The wording this version had, so the owner can bring it back.</param>
public sealed record RoleVersion(int Version, DateTime ValidFromUtc, string WhatChanged, string Wording);

/// <summary>A role's full detail — wording, rights, commands, rules and history (REQ-UI-026, REQ-UI-027).</summary>
/// <param name="Summary">The role's summary row.</param>
/// <param name="Wording">The role's prompt wording.</param>
/// <param name="Rights">What the role may do.</param>
/// <param name="Commands">The commands the role answers to.</param>
/// <param name="Rules">The rules that apply to the role.</param>
/// <param name="History">Every earlier version, newest first.</param>
public sealed record RoleDetail(
    RoleSummary Summary,
    string Wording,
    IReadOnlyList<string> Rights,
    IReadOnlyList<string> Commands,
    IReadOnlyList<string> Rules,
    IReadOnlyList<RoleVersion> History);
