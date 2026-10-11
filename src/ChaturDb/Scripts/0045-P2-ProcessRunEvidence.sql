-- [REQ-UI-048, REQ-FN-052] Process run: what a verifying run really did to a project and what it saw.
-- ProcessRunCommand is one command the run ran on the project's own tools (its build, each of its
-- test commands) with the exit code, a short log tail and the limit it ran under. ProcessRunTest is the
-- outcome of every single test those commands reported; the verdict step grades a requirement only
-- from these rows (Verified only when a test carrying its id ran and every such test passed).
CREATE TABLE ProcessRunCommand (
    ProcessRunCommandId INTEGER CONSTRAINT PkProcessRunCommand PRIMARY KEY,
    RunId INTEGER NOT NULL,
    Position INTEGER NOT NULL,
    Kind TEXT NOT NULL,
    Command TEXT NOT NULL,
    Outcome TEXT NOT NULL,
    ExitCode INTEGER NULL,
    Warnings INTEGER NULL,
    DurationMs INTEGER NOT NULL,
    TimeoutMs INTEGER NOT NULL,
    LogTail TEXT NOT NULL,
    Reason TEXT NULL,
    FinishedUtc TEXT NOT NULL,
    CONSTRAINT FkProcessRunCommandRun FOREIGN KEY (RunId) REFERENCES ProcessRun (RunId)
);

CREATE INDEX IxProcessRunCommandRun ON ProcessRunCommand (RunId, Kind);

CREATE TABLE ProcessRunTest (
    ProcessRunTestId INTEGER CONSTRAINT PkProcessRunTest PRIMARY KEY,
    RunId INTEGER NOT NULL,
    Source TEXT NOT NULL,
    Name TEXT NOT NULL,
    Outcome TEXT NOT NULL,
    Requirements TEXT NOT NULL,
    Message TEXT NULL,
    DurationMs INTEGER NOT NULL,
    CONSTRAINT FkProcessRunTestRun FOREIGN KEY (RunId) REFERENCES ProcessRun (RunId)
);

CREATE INDEX IxProcessRunTestRun ON ProcessRunTest (RunId);
