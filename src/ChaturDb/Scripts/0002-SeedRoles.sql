-- Chatur — seed data: the four roles with their rights, commands and tier; standing rules; routing
-- tiers 1..3 (empty chains); the four themes (Architecture §6, Coding Standards "Standards applied").

INSERT INTO Role (Code, Name, Wording, Version, ValidFromUtc, Tier) VALUES
    ('analyst', 'Analyst', 'Reads the project and the BRD, writes and clarifies requirements, and never changes code.', 1, '2026-09-22T00:00:00Z', 3),
    ('architect', 'Architect', 'Designs the solution structure and the data model, and writes the architecture and coding-standards documents.', 1, '2026-09-22T00:00:00Z', 2),
    ('flow-master', 'Flow master', 'Builds a requirement cluster to the coding standards, runs the build and the tests, and proposes changes for approval.', 1, '2026-09-22T00:00:00Z', 1),
    ('verifier', 'Verifier', 'Checks a built requirement against its acceptance criteria and is the only role that may mark one verified.', 1, '2026-09-22T00:00:00Z', 2);

-- Rights: nobody may run source control (Coding Standards "A model never runs a source-control
-- command"); only the Verifier may mark a requirement verified (REQ-FN-030); the Analyst cannot
-- change code (REQ-UI-030).
INSERT INTO RoleRight (RoleId, Action, Allowed)
SELECT RoleId, 'read-file', 1 FROM Role;
INSERT INTO RoleRight (RoleId, Action, Allowed)
SELECT RoleId, 'run-source-control', 0 FROM Role;
INSERT INTO RoleRight (RoleId, Action, Allowed)
SELECT RoleId, 'edit-file', CASE Code WHEN 'analyst' THEN 0 ELSE 1 END FROM Role;
INSERT INTO RoleRight (RoleId, Action, Allowed)
SELECT RoleId, 'run-build', CASE Code WHEN 'flow-master' THEN 1 WHEN 'verifier' THEN 1 ELSE 0 END FROM Role;
INSERT INTO RoleRight (RoleId, Action, Allowed)
SELECT RoleId, 'mark-verified', CASE Code WHEN 'verifier' THEN 1 ELSE 0 END FROM Role;

INSERT INTO RoleCommand (RoleId, Command, Description)
SELECT RoleId, '*create-project-brief', 'Writes the project brief from the owner''s answers.' FROM Role WHERE Code = 'analyst';
INSERT INTO RoleCommand (RoleId, Command, Description)
SELECT RoleId, '*split-brd', 'Splits the BRD into per-app requirement documents.' FROM Role WHERE Code = 'analyst';
INSERT INTO RoleCommand (RoleId, Command, Description)
SELECT RoleId, '*architect', 'Designs the solution structure and the data model.' FROM Role WHERE Code = 'architect';
INSERT INTO RoleCommand (RoleId, Command, Description)
SELECT RoleId, '*create-doc', 'Writes a document from a template.' FROM Role WHERE Code = 'architect';
INSERT INTO RoleCommand (RoleId, Command, Description)
SELECT RoleId, '*build-phase', 'Builds every requirement cluster of a phase.' FROM Role WHERE Code = 'flow-master';
INSERT INTO RoleCommand (RoleId, Command, Description)
SELECT RoleId, '*fix-issues', 'Fixes the issues a verify-phase run found.' FROM Role WHERE Code = 'flow-master';
INSERT INTO RoleCommand (RoleId, Command, Description)
SELECT RoleId, '*verify-phase', 'Checks every requirement of a phase against its acceptance criteria.' FROM Role WHERE Code = 'verifier';

INSERT INTO Rule (Scope, Text, Version) VALUES
    ('global', 'A model never runs a source-control command. The guard that refuses it is a class with its own unit tests.', 1),
    ('global', 'A file with a change waiting on it is read-only in the editor until the change is approved or rejected.', 1),
    ('global', 'Ask me first means no file is written until the owner has approved that change.', 1);

-- Escalation threshold and the three tiers' fallback chains — empty until Settings ▸ Routing (cluster
-- K) adds models to them; the rows exist so the routing screen and the routing action layer have
-- something to read from day one.
INSERT INTO RoutingSetting (Key, Value) VALUES
    ('EscalationThreshold', '3'),
    ('Tier1Chain', '[]'),
    ('Tier2Chain', '[]'),
    ('Tier3Chain', '[]');

-- Four themes, each a light and a dark set of OKLCH values in the shape TrBlazeUI reads (Architecture
-- §1 Q8). Amber ships as the default (Settings row below); Indigo, Teal and Slate are the others.
INSERT INTO Theme (Name, "Values", Source) VALUES
    ('amber', '{"light":{"bg":"oklch(0.975 0.008 65)","card":"oklch(1 0 0)","fg":"oklch(0.24 0.018 65)","dim":"oklch(0.51 0.018 65)","faint":"oklch(0.66 0.016 65)","line":"oklch(0.90 0.016 65)","line2":"oklch(0.947 0.011 65)","soft":"oklch(0.958 0.014 65)","hover":"oklch(0.925 0.022 65)","accent":"oklch(0.575 0.135 55)","accentFg":"oklch(0.99 0 0)","accentSoft":"oklch(0.935 0.05 65)"},"dark":{"bg":"oklch(0.182 0.010 65)","card":"oklch(0.222 0.012 65)","fg":"oklch(0.93 0.009 65)","dim":"oklch(0.715 0.014 65)","faint":"oklch(0.58 0.014 65)","line":"oklch(0.308 0.016 65)","line2":"oklch(0.265 0.013 65)","soft":"oklch(0.262 0.015 65)","hover":"oklch(0.318 0.020 65)","accent":"oklch(0.795 0.135 70)","accentFg":"oklch(0.20 0.05 70)","accentSoft":"oklch(0.325 0.055 65)"}}', 'build'),
    ('indigo', '{"light":{"bg":"oklch(0.972 0.006 275)","card":"oklch(1 0 0)","fg":"oklch(0.23 0.022 275)","dim":"oklch(0.52 0.02 275)","faint":"oklch(0.66 0.018 275)","line":"oklch(0.90 0.014 275)","line2":"oklch(0.945 0.01 275)","soft":"oklch(0.958 0.012 275)","hover":"oklch(0.925 0.018 275)","accent":"oklch(0.525 0.19 275)","accentFg":"oklch(0.99 0 0)","accentSoft":"oklch(0.935 0.04 275)"},"dark":{"bg":"oklch(0.178 0.014 275)","card":"oklch(0.218 0.016 275)","fg":"oklch(0.93 0.01 275)","dim":"oklch(0.71 0.017 275)","faint":"oklch(0.575 0.017 275)","line":"oklch(0.305 0.022 275)","line2":"oklch(0.262 0.018 275)","soft":"oklch(0.258 0.020 275)","hover":"oklch(0.315 0.026 275)","accent":"oklch(0.675 0.165 275)","accentFg":"oklch(0.16 0.04 275)","accentSoft":"oklch(0.315 0.075 275)"}}', 'build'),
    ('teal', '{"light":{"bg":"oklch(0.972 0.008 195)","card":"oklch(1 0 0)","fg":"oklch(0.23 0.022 195)","dim":"oklch(0.50 0.022 195)","faint":"oklch(0.65 0.02 195)","line":"oklch(0.895 0.018 195)","line2":"oklch(0.945 0.012 195)","soft":"oklch(0.955 0.015 195)","hover":"oklch(0.92 0.024 195)","accent":"oklch(0.51 0.11 195)","accentFg":"oklch(0.99 0 0)","accentSoft":"oklch(0.93 0.045 195)"},"dark":{"bg":"oklch(0.176 0.016 195)","card":"oklch(0.216 0.019 195)","fg":"oklch(0.93 0.012 195)","dim":"oklch(0.71 0.02 195)","faint":"oklch(0.575 0.02 195)","line":"oklch(0.30 0.026 195)","line2":"oklch(0.26 0.021 195)","soft":"oklch(0.256 0.024 195)","hover":"oklch(0.312 0.030 195)","accent":"oklch(0.755 0.115 195)","accentFg":"oklch(0.16 0.04 195)","accentSoft":"oklch(0.315 0.065 195)"}}', 'build'),
    ('slate', '{"light":{"bg":"oklch(0.968 0.004 250)","card":"oklch(1 0 0)","fg":"oklch(0.22 0.012 250)","dim":"oklch(0.51 0.012 250)","faint":"oklch(0.66 0.010 250)","line":"oklch(0.898 0.008 250)","line2":"oklch(0.944 0.006 250)","soft":"oklch(0.956 0.007 250)","hover":"oklch(0.924 0.010 250)","accent":"oklch(0.455 0.085 250)","accentFg":"oklch(0.99 0 0)","accentSoft":"oklch(0.93 0.025 250)"},"dark":{"bg":"oklch(0.172 0.008 250)","card":"oklch(0.212 0.010 250)","fg":"oklch(0.925 0.006 250)","dim":"oklch(0.705 0.010 250)","faint":"oklch(0.57 0.010 250)","line":"oklch(0.30 0.013 250)","line2":"oklch(0.258 0.011 250)","soft":"oklch(0.254 0.012 250)","hover":"oklch(0.31 0.016 250)","accent":"oklch(0.72 0.085 250)","accentFg":"oklch(0.16 0.03 250)","accentSoft":"oklch(0.31 0.045 250)"}}', 'build');

INSERT INTO Setting (Key, Value) VALUES
    ('Appearance.ThemeName', 'amber'),
    ('Appearance.IsDark', '1');
