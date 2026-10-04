-- Chatur — cluster L: a versioned history for each role's wording, so Settings ▸ Agents' History
-- panel (REQ-UI-029) has something honest to read even before cluster K's SaveAsync has written a
-- second version. A shipped migration is never edited; this adds a new table instead (REQ-NFR-003).

CREATE TABLE RoleVersionHistory (
    RoleVersionHistoryId INTEGER CONSTRAINT PkRoleVersionHistory PRIMARY KEY,
    RoleId INTEGER NOT NULL,
    Version INTEGER NOT NULL,
    Wording TEXT NOT NULL,
    ValidFromUtc TEXT NOT NULL,
    WhatChanged TEXT NOT NULL,
    CONSTRAINT FkRoleVersionHistoryRole FOREIGN KEY (RoleId) REFERENCES Role (RoleId)
);

CREATE INDEX IXRoleVersionHistoryRoleId ON RoleVersionHistory (RoleId);

-- Every role's current, seeded version is its own first history row.
INSERT INTO RoleVersionHistory (RoleId, Version, Wording, ValidFromUtc, WhatChanged)
SELECT RoleId, Version, Wording, ValidFromUtc, 'Seeded with the build'
FROM Role;
