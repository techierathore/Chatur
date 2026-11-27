-- [REQ-FN-327] [REQ-FN-328] [REQ-FN-329] [REQ-UI-132] [REQ-UI-133] [REQ-FN-330] Compare models: one task given to two to
-- four connected models side by side on the open project, so a past comparison opens again from Past comparisons.
-- Kind is question (changes nothing) or change (each model works in its own copy under the project's bin folder).
-- State is running, done (at least one model answered) or failed (none did).
-- ProjectUnchanged: for a question, 1 when every file of the project was the same afterwards. CopiesRemoved: for a change,
-- 1 when bin/chatur-compare/<ComparisonId> was removed when the run ended.
CREATE TABLE Comparison (
    ComparisonId INTEGER CONSTRAINT PkComparison PRIMARY KEY,
    ProjectId INTEGER NOT NULL,
    Kind TEXT NOT NULL,
    Task TEXT NOT NULL,
    State TEXT NOT NULL,
    StartedUtc TEXT NOT NULL,
    EndedUtc TEXT NULL,
    ProjectUnchanged INTEGER NULL,
    CopiesRemoved INTEGER NULL,
    CONSTRAINT FkComparisonProject FOREIGN KEY (ProjectId) REFERENCES Project (ProjectId)
);

CREATE INDEX IxComparisonProject ON Comparison (ProjectId, ComparisonId);

-- One row per model. ModelId is kept without a foreign key, so a model removed from its provider later leaves its past
-- results readable; ModelIdentifier and ProviderName are the names as they were. StartedUtc carries milliseconds: it is
-- the run record's "started" in docs/metrics/runs.jsonl and a mark's "run_id" in gates.jsonl. CostUsd is TechieRag's own
-- price, NULL when it gave none ("no price"). ChangedFiles is a JSON list of {Path, Change, LinesAdded, LinesRemoved}.
-- Mark is right, wrong or NULL; the latest mark stands.
CREATE TABLE ComparisonResult (
    ComparisonResultId INTEGER CONSTRAINT PkComparisonResult PRIMARY KEY,
    ComparisonId INTEGER NOT NULL,
    Position INTEGER NOT NULL,
    ModelId INTEGER NULL,
    ModelIdentifier TEXT NOT NULL,
    ProviderName TEXT NOT NULL,
    Connector TEXT NOT NULL,
    State TEXT NOT NULL,
    StartedUtc TEXT NOT NULL,
    EndedUtc TEXT NULL,
    DurationMs INTEGER NULL,
    TokensIn INTEGER NULL,
    TokensOut INTEGER NULL,
    CostUsd REAL NULL,
    Answer TEXT NULL,
    Error TEXT NULL,
    ChangedFiles TEXT NULL,
    Mark TEXT NULL,
    MarkedUtc TEXT NULL,
    CONSTRAINT FkComparisonResultComparison FOREIGN KEY (ComparisonId) REFERENCES Comparison (ComparisonId)
);

CREATE INDEX IxComparisonResultComparison ON ComparisonResult (ComparisonId, Position);
