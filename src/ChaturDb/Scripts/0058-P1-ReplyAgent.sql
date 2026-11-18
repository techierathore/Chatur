-- [REQ-FN-024] A reply keeps the agent that wrote it (MISS-Chatur-20261115-03). Until now a conversation held one
-- agent (Session.RoleId) and every reply was named from it, so changing the agent renamed the earlier replies.
-- NULL on a step that is not a reply. A reply recorded before this script takes the agent its conversation has
-- now: who wrote it was never stored, and this stops it changing again.
ALTER TABLE SessionEvent ADD COLUMN RoleId INTEGER NULL CONSTRAINT FkSessionEventRole REFERENCES Role (RoleId);

UPDATE SessionEvent
SET RoleId = (SELECT s.RoleId FROM Session s WHERE s.SessionId = SessionEvent.SessionId)
WHERE Kind = 'assistant';
