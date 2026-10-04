-- Chatur — the `correct-wording` tool (REQ-UI-034). Every role holds the right: a role that finds
-- its own wording, a rule or a step wrong during work corrects it on the spot, and the safety is the
-- Correction row each use writes, which the owner keeps or undoes under Settings > Corrections.
-- A shipped migration is never edited; this adds the right as a new one (REQ-NFR-003).

INSERT INTO RoleRight (RoleId, Action, Allowed)
SELECT RoleId, 'correct-wording', 1 FROM Role;
