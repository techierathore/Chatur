-- [REQ-FN-311, REQ-FN-308] Chatur phase 4, 22 Nov 2026 (BRD-249, BRD-241): every agent carries every command its
-- TechieFlow agent has, listed in code by AgentCommandCatalogue. The greeting, *help and the runner read that list; this
-- adds every command it holds that the stored list lacks, so Settings > Agents and the Agent workspace show the same
-- commands. Rows already there (and any an owner added) are kept. A shipped migration is never edited; this adds to it.

INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*help', 'Lists these commands, numbered.', NULL FROM Role
WHERE Code = 'analyst' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*help');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*day1-brownfield', 'Day 1 for a project that already has code: reads the code and its documents and writes the Architecture, BRD, Coding Standards, checklist, status, UsageGuide and DevGuide.', NULL FROM Role
WHERE Code = 'analyst' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*day1-brownfield');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*day1-greenfield', 'Day 1 for a new project: brief, BRD, Architecture, mockups, Coding Standards, checklist and status, as a process run with its checks.', NULL FROM Role
WHERE Code = 'analyst' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*day1-greenfield');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*mockups', 'Draws the UI design and one HTML mockup per screen, as a process run with its checks.', NULL FROM Role
WHERE Code = 'analyst' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*mockups');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*split-brd', 'Splits the BRD into the checklist (and into phases when the project is large), as a process run with its checks.', NULL FROM Role
WHERE Code = 'analyst' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*split-brd');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*brainstorm', 'Holds a brainstorming session with you, one technique at a time, and saves it to docs/brainstorming-session-results.md.', NULL FROM Role
WHERE Code = 'analyst' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*brainstorm');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*amend-docs', 'Folds a change into the BRD, Architecture, mockups and checklist in place, as a process run with its checks.', NULL FROM Role
WHERE Code = 'analyst' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*amend-docs');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*create-project-brief', 'Writes docs/{App}-Brief.md, the one page day 1 reads: what it is, who it is for, must do, out of scope, open questions.', NULL FROM Role
WHERE Code = 'analyst' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*create-project-brief');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*doc-out', 'Writes the whole document drafted in this conversation to its file.', NULL FROM Role
WHERE Code = 'analyst' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*doc-out');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*research-prompt', 'Writes a deep research prompt for a topic to docs/{App}-Research-Prompt.md.', NULL FROM Role
WHERE Code = 'analyst' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*research-prompt');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*yolo', 'Turns Go ahead on or off for this conversation: on, nothing waits for your approval.', NULL FROM Role
WHERE Code = 'analyst' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*yolo');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*exit', 'Says goodbye and closes this conversation.', NULL FROM Role
WHERE Code = 'analyst' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*exit');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*help', 'Lists these commands, numbered.', NULL FROM Role
WHERE Code = 'architect' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*help');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*doc-out', 'Writes the whole document drafted in this conversation to its file.', NULL FROM Role
WHERE Code = 'architect' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*doc-out');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*research', 'Writes a deep research prompt for a topic to docs/{App}-Research-Prompt.md.', NULL FROM Role
WHERE Code = 'architect' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*research');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*yolo', 'Turns Go ahead on or off for this conversation: on, nothing waits for your approval.', NULL FROM Role
WHERE Code = 'architect' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*yolo');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*exit', 'Says goodbye and closes this conversation.', NULL FROM Role
WHERE Code = 'architect' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*exit');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*architect', 'Designs the solution structure and the data model and writes docs/{App}-Architecture.md (Chatur''s own, kept from phase 1).', NULL FROM Role
WHERE Code = 'architect' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*architect');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*create-doc', 'Writes a document from a template (Chatur''s own, kept from phase 1).', NULL FROM Role
WHERE Code = 'architect' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*create-doc');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*develop-end-to-end', 'Takes the project to the owner''s acceptance test unattended, step after step; --follow says where it is, --stop ends it, --resume carries it on.', NULL FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*develop-end-to-end');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*develop-status', 'Says whether an end-to-end run is going, its step, its requirements and its tokens.', NULL FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*develop-status');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*estimate-phase', 'Estimates the phase''s tokens and time from the runs so far.', NULL FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*estimate-phase');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*build-phase', 'Builds every open row of the phase''s checklist, as a process run with its checks.', 'edit-file' FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*build-phase');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*fix-issues', 'Works through reported bugs, fixes them and re-checks, as a process run with its checks.', 'edit-file' FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*fix-issues');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*triage-issues', 'Logs reported bugs against the checklist without changing code, as a process run with its checks.', NULL FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*triage-issues');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*log-miss', 'Records one thing that was missed, in your words, in docs/metrics/misses.jsonl and docs/{App}-Misses.md.', 'log-miss' FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*log-miss');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*triage-and-fix', 'Logs the reported bugs, then fixes them: a triage run first, the fix run queued after it.', 'edit-file' FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*triage-and-fix');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*amend-docs', 'Folds a change into the BRD, Architecture, mockups and checklist in place, as a process run with its checks.', NULL FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*amend-docs');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*devguide', 'Writes the developer''s map from each screen to the code behind it.', NULL FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*devguide');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*productguide', 'Writes the end user''s manual, screen by screen.', NULL FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*productguide');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*handoff-phase', 'Finishes the UsageGuide and DevGuide and closes the phase, as a process run with its checks.', NULL FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*handoff-phase');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*deploy-checklist', 'Writes the deployment checklist for one hosting target, as a process run with its checks.', NULL FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*deploy-checklist');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*refresh-status', 'Rewrites PROJECT-STATUS.md from the checklists, as a process run with its checks.', NULL FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*refresh-status');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*metrics', 'Writes the metrics report from docs/metrics, as a process run with its checks.', NULL FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*metrics');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*generate-html', 'Turns one markdown file into an HTML page beside it.', NULL FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*generate-html');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*render-workflow-docs', 'Turns the BRDs, the Architecture and PROJECT-STATUS.md into HTML pages beside them.', NULL FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*render-workflow-docs');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*yolo', 'Turns Go ahead on or off for this conversation: on, nothing waits for your approval.', NULL FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*yolo');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*help', 'Lists these commands, numbered.', NULL FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*help');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*exit', 'Says goodbye and closes this conversation.', NULL FROM Role
WHERE Code = 'flow-master' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*exit');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*verify', 'Checks every row in scope against the running application and writes the verdicts, as a process run with its checks.', 'mark-verified' FROM Role
WHERE Code = 'verifier' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*verify');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*report', 'Shows the status and remarks of the rows in scope (ui, functional, all, or ids) from the checklists, running nothing.', NULL FROM Role
WHERE Code = 'verifier' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*report');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*help', 'Lists these commands, numbered.', NULL FROM Role
WHERE Code = 'verifier' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*help');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*exit', 'Says goodbye and closes this conversation.', NULL FROM Role
WHERE Code = 'verifier' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*exit');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*triage-issues', 'Sorts what has been reported into the checklist it belongs to, as a process run (Chatur''s own, kept from phase 2).', NULL FROM Role
WHERE Code = 'verifier' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*triage-issues');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*metrics-report', 'Reads the run records and says what they add up to, as a process run (Chatur''s own, kept from phase 2).', NULL FROM Role
WHERE Code = 'verifier' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*metrics-report');
INSERT INTO RoleCommand (RoleId, Command, Description, RequiredAction)
SELECT RoleId, '*log-miss', 'Records one thing that went wrong, in your words (Chatur''s own, kept from phase 2).', 'log-miss' FROM Role
WHERE Code = 'verifier' AND NOT EXISTS (SELECT 1 FROM RoleCommand c WHERE c.RoleId = Role.RoleId AND c.Command = '*log-miss');
