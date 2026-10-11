-- [REQ-FN-084] [REQ-FN-086] [REQ-FN-087] [REQ-FN-090] A work step handed to the agent that owns its process: which
-- session it was handed to, the model that session uses, and where the hand-over stands. The files written, the
-- commands run and the refusals are not copied here; they are read from the session's own SessionEvent and Change
-- rows, so there is one record of what the agent did.
CREATE TABLE ProcessRunStepWork (
    ProcessRunStepWorkId INTEGER CONSTRAINT PkProcessRunStepWork PRIMARY KEY,
    RunId INTEGER NOT NULL,
    Position INTEGER NOT NULL,
    SessionId INTEGER NOT NULL,
    AgentName TEXT NOT NULL,
    ModelName TEXT NULL,
    Tier INTEGER NOT NULL,
    State TEXT NOT NULL,
    Task TEXT NOT NULL,
    FinalMessage TEXT NULL,
    StartedUtc TEXT NOT NULL,
    FinishedUtc TEXT NULL,
    CONSTRAINT FkProcessRunStepWorkRun FOREIGN KEY (RunId) REFERENCES ProcessRun (RunId),
    CONSTRAINT FkProcessRunStepWorkSession FOREIGN KEY (SessionId) REFERENCES Session (SessionId),
    CONSTRAINT UcProcessRunStepWorkPosition UNIQUE (RunId, Position)
);

CREATE INDEX IXProcessRunStepWorkSessionId ON ProcessRunStepWork (SessionId);

-- [REQ-FN-088] [REQ-FN-097] [REQ-FN-098] What the project's files looked like when a run began handing steps out, so a
-- check can say what changed during the run. Path is project-relative; Fingerprint is the file's length and
-- last-write time, or, for a requirement document, the list of its ids. The row with an empty Path only says the
-- snapshot was taken.
CREATE TABLE ProcessRunFile (
    ProcessRunFileId INTEGER CONSTRAINT PkProcessRunFile PRIMARY KEY,
    RunId INTEGER NOT NULL,
    Path TEXT NOT NULL,
    Fingerprint TEXT NOT NULL,
    CONSTRAINT FkProcessRunFileRun FOREIGN KEY (RunId) REFERENCES ProcessRun (RunId),
    CONSTRAINT UcProcessRunFilePath UNIQUE (RunId, Path)
);
