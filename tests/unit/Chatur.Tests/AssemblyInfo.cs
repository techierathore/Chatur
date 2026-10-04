using Xunit;

// Test classes run one at a time. Fourteen database-backed classes release their SQLite file with
// SqliteConnection.ClearAllPools(), which is process-wide: run in parallel, one class's clean-up
// disposed another class's open connection ("Cannot access a disposed object. SQLitePCL.sqlite3"),
// failing whichever test happened to be mid-query (seen 2026-10-01 in CorrectionActionsTests).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
