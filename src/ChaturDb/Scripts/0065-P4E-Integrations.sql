-- [REQ-FN-081] [REQ-FN-082] [REQ-FN-083] [REQ-UI-069] Integrations: what Chatur offers to outside tools.
-- IntegrationAction holds only the owner's own on/off choices; an action with no row keeps its default.
-- IntegrationToken holds the hash of each token made for another tool; the token itself is shown once and never kept.
-- IntegrationCall records each process an outside tool started, so the run can be listed.
-- VerifierWindowCheck is written by the verifier after it drives a built Mac or Windows application's own window.
-- ReleaseDownload is written when a release is published with signed downloads.
CREATE TABLE IntegrationAction (
    Code TEXT CONSTRAINT PkIntegrationAction PRIMARY KEY,
    Enabled INTEGER NOT NULL,
    ChangedUtc TEXT NOT NULL
);

CREATE TABLE IntegrationToken (
    IntegrationTokenId INTEGER CONSTRAINT PkIntegrationToken PRIMARY KEY,
    TokenHash TEXT NOT NULL,
    Hint TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    RevokedUtc TEXT NULL,
    CONSTRAINT UcIntegrationTokenHash UNIQUE (TokenHash)
);

CREATE TABLE IntegrationCall (
    IntegrationCallId INTEGER CONSTRAINT PkIntegrationCall PRIMARY KEY,
    IntegrationTokenId INTEGER NOT NULL,
    ActionCode TEXT NOT NULL,
    RunId INTEGER NULL,
    ProjectName TEXT NOT NULL,
    ProcessCode TEXT NOT NULL,
    CalledUtc TEXT NOT NULL,
    CONSTRAINT FkIntegrationCallToken FOREIGN KEY (IntegrationTokenId) REFERENCES IntegrationToken (IntegrationTokenId)
);

CREATE TABLE VerifierWindowCheck (
    VerifierWindowCheckId INTEGER CONSTRAINT PkVerifierWindowCheck PRIMARY KEY,
    Application TEXT NOT NULL,
    Platform TEXT NOT NULL,
    Machine TEXT NOT NULL,
    ScreenshotsKept INTEGER NOT NULL,
    CheckedUtc TEXT NOT NULL,
    Result TEXT NOT NULL
);

CREATE TABLE ReleaseDownload (
    ReleaseDownloadId INTEGER CONSTRAINT PkReleaseDownload PRIMARY KEY,
    Version TEXT NOT NULL,
    Platform TEXT NOT NULL,
    FileName TEXT NOT NULL,
    SignedBy TEXT NOT NULL,
    Notarisation TEXT NOT NULL,
    PublishedUtc TEXT NOT NULL
);
