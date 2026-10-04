-- Cluster K additions (Architecture §7 "Roles and rules", "Models"). A shipped migration is never
-- edited; this is a new one (REQ-NFR-003). Two tables neither 0001 nor 0002 carries:
--   RequirementVerification: the durable trace of REQ-FN-030's one allowed write (only the Verifier
--   may mark a requirement verified; the refusal path writes nothing at all).
--   RoutingEscalation: the audit trail REQ-FN-022 asks for — "records the move" when a kind of work
--   climbs to a stronger tier after repeated failures.

CREATE TABLE RequirementVerification (
    RequirementVerificationId INTEGER CONSTRAINT PkRequirementVerification PRIMARY KEY,
    RequirementId INTEGER NOT NULL,
    VerifiedByRoleId INTEGER NOT NULL,
    VerifiedUtc TEXT NOT NULL,
    CONSTRAINT FkRequirementVerificationRole FOREIGN KEY (VerifiedByRoleId) REFERENCES Role (RoleId)
);

CREATE INDEX IXRequirementVerificationRequirementId ON RequirementVerification (RequirementId);

CREATE TABLE RoutingEscalation (
    RoutingEscalationId INTEGER CONSTRAINT PkRoutingEscalation PRIMARY KEY,
    WorkKind TEXT NOT NULL,
    FromTier INTEGER NOT NULL,
    ToTier INTEGER NOT NULL,
    Reason TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL
);

CREATE INDEX IXRoutingEscalationWorkKind ON RoutingEscalation (WorkKind);
