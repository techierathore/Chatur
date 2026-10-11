-- Chatur phase 2, Agent workspace (REQ-FN-058, REQ-FN-059): what each agent may do beyond the
-- phase 1 tools, and which right each of its commands needs. A command whose right the agent lacks
-- is not offered in its workspace. A shipped migration is never edited; this adds to it.

-- A command names the right it needs; NULL means it needs none beyond reading.
ALTER TABLE RoleCommand ADD COLUMN RequiredAction TEXT NULL;

UPDATE RoleCommand SET RequiredAction = 'mark-verified' WHERE Command = '*verify-phase';
UPDATE RoleCommand SET RequiredAction = 'edit-file' WHERE Command IN ('*build-phase', '*fix-issues', '*create-doc');

-- The Verifier checks and never writes source (the Agent workspace mockup lists it as withheld).
UPDATE RoleRight SET Allowed = 0 WHERE Action = 'edit-file' AND RoleId IN (SELECT RoleId FROM Role WHERE Code = 'verifier');

-- The extra rights the workspace lists: run the tests, drive the screens, open a feedback file,
-- log a miss, and close a phase (nobody but the owner's handoff-phase may).
INSERT INTO RoleRight (RoleId, Action, Allowed)
SELECT RoleId, 'run-tests', CASE Code WHEN 'flow-master' THEN 1 WHEN 'verifier' THEN 1 ELSE 0 END FROM Role;
INSERT INTO RoleRight (RoleId, Action, Allowed)
SELECT RoleId, 'drive-screens', CASE Code WHEN 'verifier' THEN 1 ELSE 0 END FROM Role;
INSERT INTO RoleRight (RoleId, Action, Allowed)
SELECT RoleId, 'open-feedback', CASE Code WHEN 'analyst' THEN 0 ELSE 1 END FROM Role;
INSERT INTO RoleRight (RoleId, Action, Allowed)
SELECT RoleId, 'log-miss', 1 FROM Role;
INSERT INTO RoleRight (RoleId, Action, Allowed)
SELECT RoleId, 'close-phase', 0 FROM Role;

-- The Verifier's other commands, as the Agent workspace mockup lists them.
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*triage-issues', 'Sorts what has been reported into the checklist it belongs to.', NULL FROM Role WHERE Code = 'verifier';
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*metrics-report', 'Reads the run records and says what they add up to.', NULL FROM Role WHERE Code = 'verifier';
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*log-miss', 'Records one thing that went wrong, so the next run can be measured against it.', 'log-miss' FROM Role WHERE Code = 'verifier';
