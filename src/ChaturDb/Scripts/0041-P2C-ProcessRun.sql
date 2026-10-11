-- [REQ-FN-052] [REQ-FN-053] [REQ-FN-055] Process run: what came of each step a run has finished, kept
-- beside the ProcessRun row that 0040 created (CurrentStep says where the run is). A run paused at an
-- owner stop is still at that step, with its finished steps' results, after Chatur is closed and opened again.
CREATE TABLE ProcessRunStep (
    ProcessRunStepId INTEGER CONSTRAINT PkProcessRunStep PRIMARY KEY,
    RunId INTEGER NOT NULL,
    Position INTEGER NOT NULL,
    State TEXT NOT NULL,
    Result TEXT NULL,
    Answer TEXT NULL,
    FinishedUtc TEXT NOT NULL,
    CONSTRAINT FkProcessRunStepRun FOREIGN KEY (RunId) REFERENCES ProcessRun (RunId),
    CONSTRAINT UcProcessRunStepPosition UNIQUE (RunId, Position)
);
