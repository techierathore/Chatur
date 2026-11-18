-- [REQ-UI-087] The tokens a session has spent so far, counted after every model call from the usage the call
-- returns, so a running process can show what it has spent beside its estimate (MISS-Chatur-20261110-07).
-- A process run's tokens so far are the sum over the sessions of its handed steps (ProcessRunStepWork.SessionId).
ALTER TABLE Session ADD COLUMN TokensIn INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Session ADD COLUMN TokensOut INTEGER NOT NULL DEFAULT 0;
