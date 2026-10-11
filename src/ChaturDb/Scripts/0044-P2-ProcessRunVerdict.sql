-- [REQ-UI-048] Process run: each verdict a verifying run wrote into the project's checklist table,
-- kept so Process run can list what the run wrote after Chatur is closed and opened again.
CREATE TABLE ProcessRunVerdict (
    ProcessRunVerdictId INTEGER CONSTRAINT PkProcessRunVerdict PRIMARY KEY,
    RunId INTEGER NOT NULL,
    RequirementId TEXT NOT NULL,
    ChecklistFile TEXT NOT NULL,
    Status TEXT NOT NULL,
    Percent INTEGER NOT NULL,
    Remark TEXT NOT NULL,
    WrittenUtc TEXT NOT NULL,
    CONSTRAINT FkProcessRunVerdictRun FOREIGN KEY (RunId) REFERENCES ProcessRun (RunId),
    CONSTRAINT UcProcessRunVerdictRequirement UNIQUE (RunId, RequirementId)
);
