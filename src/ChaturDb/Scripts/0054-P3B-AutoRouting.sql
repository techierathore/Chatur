-- Cluster B (Settings), REQ-FN-071 / REQ-FN-072: automatic routing.
--   The on/off switch is a RoutingSetting row (Key 'AutoRouting'); no row, or '0', means off, so a
--   database that has never seen the switch behaves exactly as before.
--   RoutingChoice: one row for each piece of work the small model was asked to route, so the owner can
--   see which model was chosen, how the choice was reached and why. Source is SmallModel,
--   OnlyCandidate or Fallback (TechieRag's own words), or TiersAndChains when the small model failed
--   and the owner's tiers and chains were used instead.

CREATE TABLE RoutingChoice (
    RoutingChoiceId INTEGER CONSTRAINT PkRoutingChoice PRIMARY KEY,
    SessionId INTEGER NULL,
    RoleId INTEGER NULL,
    ModelIdentifier TEXT NOT NULL,
    Source TEXT NOT NULL,
    Reason TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL
);

CREATE INDEX IXRoutingChoiceSessionId ON RoutingChoice (SessionId);
