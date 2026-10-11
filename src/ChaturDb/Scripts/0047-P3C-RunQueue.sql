-- [REQ-FN-067] [REQ-FN-068] [REQ-FN-069] [REQ-FN-070] Run queue: one row per run the owner queued.
-- Status is Queued (waiting its turn, or a paused run waiting to carry on), Started (its end-to-end run,
-- AutoRunId, was begun; where it stands is read from that run) or Removed (taken out, never started again).
-- Queue order is the row id. ResumeFocus is the open requirement the last resume took first.
CREATE TABLE RunQueueItem (
    RunQueueItemId INTEGER CONSTRAINT PkRunQueueItem PRIMARY KEY,
    ProjectId INTEGER NOT NULL,
    Brief TEXT NOT NULL,
    Status TEXT NOT NULL,
    AutoRunId INTEGER NULL,
    ResumeFocus TEXT NULL,
    QueuedUtc TEXT NOT NULL,
    RemovedUtc TEXT NULL,
    CONSTRAINT FkRunQueueItemProject FOREIGN KEY (ProjectId) REFERENCES Project (ProjectId)
);

CREATE INDEX IxRunQueueItemStatus ON RunQueueItem (Status, RunQueueItemId);
