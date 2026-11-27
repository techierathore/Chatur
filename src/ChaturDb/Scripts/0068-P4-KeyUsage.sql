-- [REQ-UI-131, REQ-FN-326] Chatur phase 4, 27 Nov 2026 (BRD-289, BRD-290): one row per model call Chatur made through a
-- provider, with the price TechieRag gave for it, so Measurements can count what a key has used since a date the person
-- typed. CostUsd is NULL when TechieRag had no price for the call; such a call makes the line "not known", never a guess.
-- A provider's rows go with it when it is removed. A shipped migration is never edited; this adds to the schema.

CREATE TABLE KeyUsage (
    KeyUsageId INTEGER CONSTRAINT PkKeyUsage PRIMARY KEY,
    ProviderId INTEGER NOT NULL,
    ModelIdentifier TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    TokensIn INTEGER NOT NULL,
    TokensOut INTEGER NOT NULL,
    CostUsd NUMERIC NULL,
    CONSTRAINT FkKeyUsageProvider FOREIGN KEY (ProviderId) REFERENCES Provider (ProviderId) ON DELETE CASCADE
);

CREATE INDEX IXKeyUsageProviderIdCreatedUtc ON KeyUsage (ProviderId, CreatedUtc);
