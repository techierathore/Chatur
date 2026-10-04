-- Chatur — initial schema (Architecture §4). Every entity the phase-1 checklist needs, seeded by
-- 0002-SeedRoles.sql. A shipped migration is never edited; a new one is added (REQ-NFR-003).

CREATE TABLE Installation (
    InstallationId INTEGER CONSTRAINT PkInstallation PRIMARY KEY,
    DeviceId TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    CONSTRAINT UcInstallationDeviceId UNIQUE (DeviceId)
);

CREATE TABLE Setting (
    SettingId INTEGER CONSTRAINT PkSetting PRIMARY KEY,
    Key TEXT NOT NULL,
    Value TEXT NULL,
    CONSTRAINT UcSettingKey UNIQUE (Key)
);

CREATE TABLE Theme (
    ThemeId INTEGER CONSTRAINT PkTheme PRIMARY KEY,
    Name TEXT NOT NULL,
    "Values" TEXT NOT NULL,
    Source TEXT NOT NULL,
    CONSTRAINT UcThemeName UNIQUE (Name)
);

CREATE TABLE ProjectFolder (
    ProjectFolderId INTEGER CONSTRAINT PkProjectFolder PRIMARY KEY,
    Path TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    CONSTRAINT UcProjectFolderPath UNIQUE (Path)
);

CREATE TABLE Project (
    ProjectId INTEGER CONSTRAINT PkProject PRIMARY KEY,
    ProjectFolderId INTEGER NULL,
    Name TEXT NOT NULL,
    Path TEXT NOT NULL,
    Kind TEXT NOT NULL,
    LastOpenedUtc TEXT NULL,
    CONSTRAINT UcProjectPath UNIQUE (Path),
    CONSTRAINT FkProjectProjectFolder FOREIGN KEY (ProjectFolderId) REFERENCES ProjectFolder (ProjectFolderId)
);

CREATE INDEX IXProjectProjectFolderId ON Project (ProjectFolderId);

CREATE TABLE RunTarget (
    RunTargetId INTEGER CONSTRAINT PkRunTarget PRIMARY KEY,
    ProjectId INTEGER NOT NULL,
    Name TEXT NOT NULL,
    Command TEXT NOT NULL,
    Platform TEXT NOT NULL,
    IsLastUsed INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT FkRunTargetProject FOREIGN KEY (ProjectId) REFERENCES Project (ProjectId)
);

CREATE INDEX IXRunTargetProjectId ON RunTarget (ProjectId);

CREATE TABLE Provider (
    ProviderId INTEGER CONSTRAINT PkProvider PRIMARY KEY,
    Name TEXT NOT NULL,
    Connector TEXT NOT NULL,
    SignInMethod TEXT NOT NULL,
    BaseUrl TEXT NOT NULL,
    SecretName TEXT NULL,
    State TEXT NOT NULL DEFAULT 'NotTestedYet',
    CONSTRAINT UcProviderName UNIQUE (Name)
);

CREATE TABLE Model (
    ModelId INTEGER CONSTRAINT PkModel PRIMARY KEY,
    ProviderId INTEGER NOT NULL,
    Tier INTEGER NOT NULL,
    Identifier TEXT NOT NULL,
    CONSTRAINT FkModelProvider FOREIGN KEY (ProviderId) REFERENCES Provider (ProviderId)
);

CREATE INDEX IXModelProviderId ON Model (ProviderId);

CREATE TABLE Role (
    RoleId INTEGER CONSTRAINT PkRole PRIMARY KEY,
    Code TEXT NOT NULL,
    Name TEXT NOT NULL,
    Wording TEXT NOT NULL,
    Version INTEGER NOT NULL,
    ValidFromUtc TEXT NOT NULL,
    Tier INTEGER NOT NULL DEFAULT 2,
    CONSTRAINT UcRoleCode UNIQUE (Code)
);

CREATE TABLE RoleRight (
    RoleRightId INTEGER CONSTRAINT PkRoleRight PRIMARY KEY,
    RoleId INTEGER NOT NULL,
    Action TEXT NOT NULL,
    Allowed INTEGER NOT NULL,
    CONSTRAINT FkRoleRightRole FOREIGN KEY (RoleId) REFERENCES Role (RoleId)
);

CREATE INDEX IXRoleRightRoleId ON RoleRight (RoleId);

CREATE TABLE RoleCommand (
    RoleCommandId INTEGER CONSTRAINT PkRoleCommand PRIMARY KEY,
    RoleId INTEGER NOT NULL,
    Command TEXT NOT NULL,
    Description TEXT NOT NULL,
    CONSTRAINT FkRoleCommandRole FOREIGN KEY (RoleId) REFERENCES Role (RoleId)
);

CREATE INDEX IXRoleCommandRoleId ON RoleCommand (RoleId);

CREATE TABLE Rule (
    RuleId INTEGER CONSTRAINT PkRule PRIMARY KEY,
    Scope TEXT NOT NULL,
    Text TEXT NOT NULL,
    Version INTEGER NOT NULL
);

CREATE TABLE ProcessStepText (
    StepTextId INTEGER CONSTRAINT PkProcessStepText PRIMARY KEY,
    ProcessCode TEXT NOT NULL,
    StepCode TEXT NOT NULL,
    Text TEXT NOT NULL,
    Version INTEGER NOT NULL
);

CREATE TABLE RoutingEntry (
    RoutingEntryId INTEGER CONSTRAINT PkRoutingEntry PRIMARY KEY,
    RoleId INTEGER NULL,
    WorkKind TEXT NULL,
    Tier INTEGER NOT NULL,
    SortOrder INTEGER NOT NULL,
    CONSTRAINT FkRoutingEntryRole FOREIGN KEY (RoleId) REFERENCES Role (RoleId)
);

CREATE INDEX IXRoutingEntryRoleId ON RoutingEntry (RoleId);

CREATE TABLE RoutingSetting (
    RoutingSettingId INTEGER CONSTRAINT PkRoutingSetting PRIMARY KEY,
    Key TEXT NOT NULL,
    Value TEXT NOT NULL,
    CONSTRAINT UcRoutingSettingKey UNIQUE (Key)
);

CREATE TABLE Session (
    SessionId INTEGER CONSTRAINT PkSession PRIMARY KEY,
    ProjectId INTEGER NOT NULL,
    RoleId INTEGER NOT NULL,
    Mode TEXT NOT NULL,
    State TEXT NOT NULL,
    StartedUtc TEXT NOT NULL,
    CONSTRAINT FkSessionProject FOREIGN KEY (ProjectId) REFERENCES Project (ProjectId),
    CONSTRAINT FkSessionRole FOREIGN KEY (RoleId) REFERENCES Role (RoleId)
);

CREATE INDEX IXSessionProjectId ON Session (ProjectId);
CREATE INDEX IXSessionRoleId ON Session (RoleId);

CREATE TABLE SessionEvent (
    SessionEventId INTEGER CONSTRAINT PkSessionEvent PRIMARY KEY,
    SessionId INTEGER NOT NULL,
    Seq INTEGER NOT NULL,
    Kind TEXT NOT NULL,
    Payload TEXT NOT NULL,
    Model TEXT NULL,
    Tokens INTEGER NULL,
    CreatedUtc TEXT NOT NULL,
    CONSTRAINT FkSessionEventSession FOREIGN KEY (SessionId) REFERENCES Session (SessionId)
);

CREATE INDEX IXSessionEventSessionId ON SessionEvent (SessionId);

CREATE TABLE Change (
    ChangeId INTEGER CONSTRAINT PkChange PRIMARY KEY,
    SessionId INTEGER NOT NULL,
    FilePath TEXT NOT NULL,
    Before TEXT NULL,
    After TEXT NULL,
    Status TEXT NOT NULL,
    CONSTRAINT FkChangeSession FOREIGN KEY (SessionId) REFERENCES Session (SessionId)
);

CREATE INDEX IXChangeSessionId ON Change (SessionId);

CREATE TABLE Correction (
    CorrectionId INTEGER CONSTRAINT PkCorrection PRIMARY KEY,
    Target TEXT NOT NULL,
    Before TEXT NULL,
    After TEXT NULL,
    Why TEXT NOT NULL,
    Status TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL
);

CREATE TABLE RefusedWrite (
    RefusedWriteId INTEGER CONSTRAINT PkRefusedWrite PRIMARY KEY,
    ProjectId INTEGER NULL,
    StreamName TEXT NOT NULL,
    Reason TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    CONSTRAINT FkRefusedWriteProject FOREIGN KEY (ProjectId) REFERENCES Project (ProjectId)
);

CREATE INDEX IXRefusedWriteProjectId ON RefusedWrite (ProjectId);
