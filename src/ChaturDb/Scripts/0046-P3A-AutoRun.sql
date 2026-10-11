-- [REQ-FN-061] [REQ-FN-062] End-to-end run: one row per run started from a brief. CurrentProcess is the
-- place in the chain the run is on, so a run carries on from it after Chatur is closed and opened again.
-- ResumeAtUtc is set only while the run waits out a usage limit (REQ-FN-064); StopReason only when it stopped.
CREATE TABLE AutoRun (
    AutoRunId INTEGER CONSTRAINT PkAutoRun PRIMARY KEY,
    ProjectId INTEGER NOT NULL,
    Brief TEXT NOT NULL,
    State TEXT NOT NULL,
    CurrentProcess INTEGER NOT NULL,
    StartedUtc TEXT NOT NULL,
    EndedUtc TEXT NULL,
    StopReason TEXT NULL,
    ResumeAtUtc TEXT NULL,
    CONSTRAINT FkAutoRunProject FOREIGN KEY (ProjectId) REFERENCES Project (ProjectId)
);

CREATE INDEX IxAutoRunProject ON AutoRun (ProjectId, State);

-- [REQ-FN-061] [REQ-FN-065] The processes of one run's chain, each with the process run that carried it out
-- and how many times a step of it was tried again.
CREATE TABLE AutoRunProcess (
    AutoRunProcessId INTEGER CONSTRAINT PkAutoRunProcess PRIMARY KEY,
    AutoRunId INTEGER NOT NULL,
    Position INTEGER NOT NULL,
    ProcessCode TEXT NOT NULL,
    State TEXT NOT NULL,
    ProcessRunId INTEGER NULL,
    Retries INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT FkAutoRunProcessRun FOREIGN KEY (AutoRunId) REFERENCES AutoRun (AutoRunId),
    CONSTRAINT UcAutoRunProcessPosition UNIQUE (AutoRunId, Position)
);

-- [REQ-FN-064] [REQ-FN-065] The run's record: every usage-limit wait, every retry and every process started,
-- kept so the run report and the End-to-end run screen can show them.
CREATE TABLE AutoRunEvent (
    AutoRunEventId INTEGER CONSTRAINT PkAutoRunEvent PRIMARY KEY,
    AutoRunId INTEGER NOT NULL,
    AtUtc TEXT NOT NULL,
    Kind TEXT NOT NULL,
    ProcessPosition INTEGER NULL,
    ProcessCode TEXT NULL,
    Message TEXT NOT NULL,
    UntilUtc TEXT NULL,
    CONSTRAINT FkAutoRunEventRun FOREIGN KEY (AutoRunId) REFERENCES AutoRun (AutoRunId)
);

CREATE INDEX IxAutoRunEventRun ON AutoRunEvent (AutoRunId, AutoRunEventId);
