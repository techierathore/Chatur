-- [REQ-UI-086] [REQ-FN-108] Sister projects: folders the owner names on Settings > Measurements whose
-- docs/metrics/runs.jsonl the Estimator reads (never writes) when a project has fewer than three runs of its own.
CREATE TABLE SisterProject (
    SisterProjectId INTEGER CONSTRAINT PkSisterProject PRIMARY KEY,
    Path TEXT NOT NULL,
    AddedUtc TEXT NOT NULL,
    CONSTRAINT UqSisterProjectPath UNIQUE (Path)
);

-- [REQ-UI-083] [REQ-UI-084] [REQ-UI-085] [REQ-FN-112] The estimate a run was started or queued with, so a later
-- tier change does not move it. RunKind is process, auto or queue; RunId is that run's own id. BatchTs is the
-- ts of the batch written to the project's estimates.jsonl (empty when nothing was written: no checklist yet).
-- Snapshot is the estimate as it was shown, as JSON, with the labels the stream does not carry.
CREATE TABLE RunEstimate (
    RunEstimateId INTEGER CONSTRAINT PkRunEstimate PRIMARY KEY,
    RunKind TEXT NOT NULL,
    RunId INTEGER NOT NULL,
    ProjectId INTEGER NOT NULL,
    BatchTs TEXT NOT NULL,
    Snapshot TEXT NOT NULL,
    KeptUtc TEXT NOT NULL,
    CONSTRAINT UqRunEstimateRun UNIQUE (RunKind, RunId),
    CONSTRAINT FkRunEstimateProject FOREIGN KEY (ProjectId) REFERENCES Project (ProjectId)
);
