-- [REQ-UI-062] [REQ-FN-073] [REQ-UI-063] [REQ-FN-074] Feedback and issues: one row per entry the owner raised.
-- State is Waiting (kept on this machine, not yet raised through App Manager), Raised, Answered or Fixed.
-- RemoteId and RemoteNumber are App Manager's own id and issue number once it has taken the entry.
-- About is the name the owner picked: Chatur, TrBlazeUI, TechieRag or App Manager. Each account has its own file.
CREATE TABLE FeedbackEntry (
    FeedbackEntryId INTEGER CONSTRAINT PkFeedbackEntry PRIMARY KEY,
    About TEXT NOT NULL,
    Severity TEXT NOT NULL,
    Title TEXT NOT NULL,
    Detail TEXT NOT NULL,
    Environment TEXT NULL,
    State TEXT NOT NULL,
    RemoteId TEXT NULL,
    RemoteNumber TEXT NULL,
    Answer TEXT NULL,
    LastError TEXT NULL,
    CreatedUtc TEXT NOT NULL
);

CREATE INDEX IxFeedbackEntryState ON FeedbackEntry (State, FeedbackEntryId);
