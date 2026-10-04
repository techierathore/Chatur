-- REQ-UI-009 (fix 2026-09-30): removing a named folder must not fail on, or destroy, the history of a
-- project in it that has been worked on (Session, RunTarget, RefusedWrite all reference Project).
-- Such a project is hidden instead of deleted, and comes back if its folder is named again.
ALTER TABLE Project ADD COLUMN RemovedUtc TEXT NULL;
