using Chatur.Core.Corrections;
using Chatur.Core.Roles;
using Chatur.Tests.Support;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Chatur.Tests.Corrections;

/// <summary>Tests for cluster K's and cluster L's methods on <see cref="CorrectionActions"/> against a real, migrated, temporary database.</summary>
public sealed class CorrectionActionsTests : MigratedDatabaseFixture
{
    /// <summary>Every correction is listed, newest first, with when it was made (REQ-UI-034).</summary>
    [Fact]
    public async Task ListAsyncReturnsEveryCorrectionNewestFirst()
    {
        var vOlderId = InsertCorrection("analyst", "Old A.", "New A.", "First.");
        var vNewerId = InsertCorrection("architect", "Old B.", "New B.", "Second.");
        var vActions = new CorrectionActions(CreateFactory());

        var vCorrections = (await vActions.ListAsync()).ToList();

        Assert.True(vCorrections.Count >= 2);
        var vNewer = vCorrections.Single(c => c.CorrectionId == vNewerId);
        var vOlder = vCorrections.Single(c => c.CorrectionId == vOlderId);
        Assert.True(vCorrections.IndexOf(vNewer) < vCorrections.IndexOf(vOlder));
        Assert.Equal("Proposed", vNewer.Status);
        Assert.True(vNewer.CreatedUtc > default(DateTime));
    }

    /// <summary>When a correction is kept, then its row moves to kept (REQ-FN-037).</summary>
    [Fact]
    public async Task KeepAsyncMovesTheRowToKept()
    {
        var vCorrectionId = InsertCorrection("analyst", "Old wording.", "New wording.", "Clarity.");
        var vActions = new CorrectionActions(CreateFactory());

        await vActions.KeepAsync(vCorrectionId);

        Assert.Equal("Kept", ReadStatus(vCorrectionId));
    }

    /// <summary>
    /// When a correction targeting a role is undone, then the role's wording before it returns and
    /// the row moves to undone (REQ-FN-038).
    /// </summary>
    [Fact]
    public async Task UndoAsyncRevertsTheRoleWordingAndMovesToUndone()
    {
        var vOriginalWording = ReadRoleWording("analyst");
        var vNewWording = "A wording Chatur corrected on its own, at least forty characters long.";
        await new RoleActions(CreateFactory()).SaveAsync(RoleIdFor("analyst"), vNewWording, Array.Empty<string>(), Array.Empty<string>());
        var vCorrectionId = InsertCorrection("analyst", vOriginalWording, vNewWording, "Self-correction.");

        var vActions = new CorrectionActions(CreateFactory());
        await vActions.UndoAsync(vCorrectionId);

        Assert.Equal("Undone", ReadStatus(vCorrectionId));
        Assert.Equal(vOriginalWording, ReadRoleWording("analyst"));
    }

    /// <summary>
    /// When the seed is exported, then every kept correction is in it and every undone one is not
    /// (REQ-FN-039).
    /// </summary>
    [Fact]
    public async Task ExportSeedAsyncIncludesOnlyKeptCorrections()
    {
        var vKeptId = InsertCorrection("architect", "Before A.", "After A.", "Kept one.");
        var vUndoneId = InsertCorrection("architect", "Before B.", "After B.", "Undone one.");
        var vActions = new CorrectionActions(CreateFactory());
        await vActions.KeepAsync(vKeptId);
        await vActions.UndoAsync(vUndoneId);

        var vSeed = await vActions.ExportSeedAsync();

        Assert.Contains("After A.", vSeed);
        Assert.DoesNotContain("After B.", vSeed);
    }

    private int InsertCorrection(string aTarget, string aBefore, string aAfter, string aWhy)
    {
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText =
            "INSERT INTO Correction (Target, Before, After, Why, Status, CreatedUtc) VALUES (@Target, @Before, @After, @Why, 'Proposed', @CreatedUtc); SELECT last_insert_rowid();";
        vCommand.Parameters.AddWithValue("@Target", aTarget);
        vCommand.Parameters.AddWithValue("@Before", aBefore);
        vCommand.Parameters.AddWithValue("@After", aAfter);
        vCommand.Parameters.AddWithValue("@Why", aWhy);
        vCommand.Parameters.AddWithValue("@CreatedUtc", DateTime.UtcNow.ToString("O"));
        return (int)(long)vCommand.ExecuteScalar()!;
    }

    private string ReadStatus(int aCorrectionId)
    {
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText = "SELECT Status FROM Correction WHERE CorrectionId = @Id;";
        vCommand.Parameters.AddWithValue("@Id", aCorrectionId);
        return (string)vCommand.ExecuteScalar()!;
    }

    private string ReadRoleWording(string aCode)
    {
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText = "SELECT Wording FROM Role WHERE Code = @Code;";
        vCommand.Parameters.AddWithValue("@Code", aCode);
        return (string)vCommand.ExecuteScalar()!;
    }
}
