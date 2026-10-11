-- [REQ-FN-104] Chatur phase 3, document rights (owner decision 2026-10-09, option A): a "write-docs" right, shown to the
-- owner as "writes documents", lets a role write files under the project's docs/ folder and nothing else. The Analyst,
-- the Architect and the Verifier hold it. The Flow master does not: its "edit-file" right already covers docs/.
-- Every other right is left as it was. A shipped migration is never edited; this adds to it.
INSERT INTO RoleRight (RoleId, Action, Allowed)
SELECT RoleId, 'write-docs', CASE WHEN Code IN ('analyst', 'architect', 'verifier') THEN 1 ELSE 0 END FROM Role;
