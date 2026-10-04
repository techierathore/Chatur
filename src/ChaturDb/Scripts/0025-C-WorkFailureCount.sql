-- Fix-pass addition, cluster C (REQ-FN-022 "repeated failed fixes climb a tier"). A shipped
-- migration is never edited; this is a new one. Counts, per kind of work, how many times in a row
-- the project's build has failed right after the agent loop wrote an approved change — the honest
-- "same fix failing the set number of times" trigger AgentActions.RunBuildAsync reads before calling
-- RoutingActions.RecordEscalationAsync (0020-K). A successful build, or the count reaching
-- RoutingSetting's own EscalationThreshold and actually escalating, resets it to zero — this table
-- only ever holds the CURRENT streak, never a historical total (RoutingEscalation is the audit trail
-- for that).
CREATE TABLE WorkFailureCount (
    WorkKind TEXT NOT NULL CONSTRAINT PkWorkFailureCount PRIMARY KEY,
    FailureCount INTEGER NOT NULL DEFAULT 0
);
