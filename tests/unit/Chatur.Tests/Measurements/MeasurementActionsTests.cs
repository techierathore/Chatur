using System.Text.Json;
using System.Linq;
using Chatur.Core.Measurements;
using Chatur.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chatur.Tests.Measurements;

/// <summary>Tests for cluster K's <see cref="MeasurementActions.AppendAsync"/> and cluster L's <see cref="MeasurementActions.StreamsAsync"/>, against a real, migrated, temporary database and a real temp folder.</summary>
public sealed class MeasurementActionsTests : MigratedDatabaseFixture
{
    private readonly string objProjectPath = Path.Combine(Chatur.Tests.Support.TestScratch.Root, $"chatur-project-{Guid.NewGuid():N}");

    /// <summary>
    /// When Measurements opens for a project with no streams written yet, then all five are listed at
    /// zero records rather than the page crashing (REQ-UI-036).
    /// </summary>
    [Fact]
    public async Task StreamsAsyncReturnsAllSixStreamsEvenWithNothingWritten()
    {
        var vProjectId = CreateProject("Empty", objProjectPath);
        var vActions = new MeasurementActions(CreateFactory(), NullLogger<MeasurementActions>.Instance);

        var vStreams = await vActions.StreamsAsync(vProjectId);

        Assert.Equal(6, vStreams.Count);
        Assert.Contains(vStreams, s => s.Name == "estimates.jsonl");
        Assert.All(vStreams, s => Assert.Equal(0, s.RecordCount));
        Assert.All(vStreams, s => Assert.Null(s.NewestUtc));
        Assert.All(vStreams, s => Assert.False(s.LastWriteFailed));
    }

    /// <summary>
    /// When records have been appended, then the stream's count and newest time reflect them
    /// (REQ-UI-036).
    /// </summary>
    [Fact]
    public async Task StreamsAsyncCountsAppendedRecordsAndFindsTheNewest()
    {
        var vProjectId = CreateProject("Busy", objProjectPath);
        var vAppendActions = new MeasurementActions(CreateFactory(), NullLogger<MeasurementActions>.Instance);
        await vAppendActions.AppendAsync(vProjectId, "runs", new Dictionary<string, string?> { ["cmd"] = "build-phase" });
        await vAppendActions.AppendAsync(vProjectId, "runs", new Dictionary<string, string?> { ["cmd"] = "verify-phase" });

        var vStreams = await vAppendActions.StreamsAsync(vProjectId);

        var vRuns = vStreams.Single(s => s.Name == "runs.jsonl");
        Assert.Equal(2, vRuns.RecordCount);
        Assert.NotNull(vRuns.NewestUtc);
    }

    /// <summary>
    /// When a stream's most recent write failed, then Measurements says so and the session's earlier
    /// records are still counted — a failed write never stops the work (REQ-UI-035).
    /// </summary>
    [Fact]
    public async Task StreamsAsyncMarksTheStreamWhenItsNewestWriteFailed()
    {
        var vProjectId = CreateProject("Flaky", objProjectPath);
        InsertRefusedWrite(vProjectId, "misses.jsonl", "The disk was full.");
        var vActions = new MeasurementActions(CreateFactory(), NullLogger<MeasurementActions>.Instance);

        var vStreams = await vActions.StreamsAsync(vProjectId);

        var vMisses = vStreams.Single(s => s.Name == "misses.jsonl");
        Assert.True(vMisses.LastWriteFailed);
        Assert.All(vStreams.Where(s => s.Name != "misses.jsonl"), s => Assert.False(s.LastWriteFailed));
    }

    private void InsertRefusedWrite(int aProjectId, string aStreamName, string aReason)
    {
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText =
            "INSERT INTO RefusedWrite (ProjectId, StreamName, Reason, CreatedUtc) VALUES (@ProjectId, @StreamName, @Reason, @CreatedUtc);";
        vCommand.Parameters.AddWithValue("@ProjectId", aProjectId);
        vCommand.Parameters.AddWithValue("@StreamName", aStreamName);
        vCommand.Parameters.AddWithValue("@Reason", aReason);
        vCommand.Parameters.AddWithValue("@CreatedUtc", DateTime.UtcNow.ToString("O"));
        vCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// When a run record a command needs is appended, then <c>docs/metrics</c> holds that one stream's file and no
    /// other, never an empty one (REQ-FN-040; BRD-74 as changed 23 Nov 2026), the record is signed
    /// <c>harness: "chatur"</c> in TechieFlow's shape (REQ-FN-041), and a field the caller did not supply is written
    /// null rather than guessed (REQ-FN-042).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-040 a record a command needs makes its own stream file and no other")]
    public async Task AppendAsyncMakesOnlyItsOwnStreamAndSignsTheRecord()
    {
        var vProjectId = CreateProject("Sample", objProjectPath);
        var vActions = new MeasurementActions(CreateFactory(), NullLogger<MeasurementActions>.Instance);

        await vActions.AppendAsync(vProjectId, "runs", new Dictionary<string, string?> { ["cmd"] = "build-phase" });

        var vMetricsFolder = Path.Combine(objProjectPath, "docs", "metrics");
        Assert.Equal(["runs.jsonl"], new DirectoryInfo(vMetricsFolder).GetFiles().Select(f => f.Name).ToArray());

        var vLine = (await File.ReadAllLinesAsync(Path.Combine(vMetricsFolder, "runs.jsonl"))).Single(aLine => !string.IsNullOrWhiteSpace(aLine));
        Assert.Contains("\"harness\":\"chatur\"", vLine);
        Assert.Contains("\"v\":1,", vLine);
        Assert.Contains("\"ts\":\"", vLine);
        Assert.DoesNotContain("\"at\":", vLine);
        Assert.Contains("\"kind\":\"run\"", vLine);
        Assert.Contains("\"cmd\":\"build-phase\"", vLine);
        Assert.Contains("\"mode\":null", vLine);
    }

    /// <summary>
    /// When a record carries typed fields, then they are written as TechieFlow types them (a count a number, a list an
    /// array, a cost a number or null), so TfLens reads them unchanged; a count that is not a number is refused
    /// (REQ-FN-040, REQ-FN-043; owner's test of 25 Nov 2026).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-040 a record is typed the way TechieFlow types it")]
    public async Task AppendAsyncTypesTheFields()
    {
        var vProjectId = CreateProject("Typed", objProjectPath);
        var vActions = new MeasurementActions(CreateFactory(), NullLogger<MeasurementActions>.Instance);

        await vActions.AppendAsync(vProjectId, "runs", new Dictionary<string, string?>
        {
            ["cmd"] = "build-phase", ["duration_s"] = "12.4", ["tokens_in"] = "5", ["reqs_touched"] = "REQ-FN-001, REQ-UI-002", ["cost_usd"] = "0.25"
        });
        await vActions.AppendAsync(vProjectId, "runs", new Dictionary<string, string?> { ["cmd"] = "x", ["tokens_in"] = "many" });

        var vLines = await File.ReadAllLinesAsync(Path.Combine(objProjectPath, "docs", "metrics", "runs.jsonl"));
        var vLine = Assert.Single(vLines, l => !string.IsNullOrWhiteSpace(l));
        using var vDoc = JsonDocument.Parse(vLine);
        Assert.Equal(12, vDoc.RootElement.GetProperty("duration_s").GetInt64());
        Assert.Equal(5, vDoc.RootElement.GetProperty("tokens_in").GetInt64());
        Assert.Equal(0.25, vDoc.RootElement.GetProperty("cost_usd").GetDouble());
        Assert.Equal(["REQ-FN-001", "REQ-UI-002"], vDoc.RootElement.GetProperty("reqs_touched").EnumerateArray().Select(e => e.GetString()!).ToArray());
        Assert.Contains(await vActions.StreamsAsync(vProjectId), s => s.Name == "runs.jsonl" && s.Refused == 1);
    }

    /// <summary>
    /// When a record carries a field the schema does not know, then the write is refused, nothing is
    /// appended, and the reason is logged to <c>RefusedWrite</c> (REQ-FN-043).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-043 a record with an unknown field is refused and the reason is logged")]
    public async Task AppendAsyncRefusesAnUnknownField()
    {
        var vProjectId = CreateProject("Sample", objProjectPath);
        var vActions = new MeasurementActions(CreateFactory(), NullLogger<MeasurementActions>.Instance);

        await vActions.AppendAsync(vProjectId, "runs", new Dictionary<string, string?> { ["not_a_real_field"] = "x" });

        // Nothing is appended, and a refused write makes no file and no folder either.
        Assert.False(Directory.Exists(Path.Combine(objProjectPath, "docs")));
        Assert.Equal(1, CountRefusedWrites());
    }

    /// <summary>An unknown value for a closed field is refused the same way (REQ-FN-043).</summary>
    [Fact(DisplayName = "REQ-FN-043 a record with an unknown value is refused and the reason is logged")]
    public async Task AppendAsyncRefusesAnUnknownEnumValue()
    {
        var vProjectId = CreateProject("Sample", objProjectPath);
        var vActions = new MeasurementActions(CreateFactory(), NullLogger<MeasurementActions>.Instance);

        await vActions.AppendAsync(vProjectId, "runs", new Dictionary<string, string?> { ["build_result"] = "sort-of" });

        Assert.Equal(1, CountRefusedWrites());
    }

    /// <summary>
    /// When a record carries one unknown field beside known ones, then the whole write is refused
    /// (nothing partial is appended) and the stored reason names the unknown field (REQ-FN-043).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-043 one unknown field refuses the whole record and the reason names it")]
    public async Task AppendAsyncRefusesTheWholeRecordAndNamesTheField()
    {
        var vProjectId = CreateProject("Sample", objProjectPath);
        var vActions = new MeasurementActions(CreateFactory(), NullLogger<MeasurementActions>.Instance);

        await vActions.AppendAsync(vProjectId, "runs", new Dictionary<string, string?> { ["cmd"] = "build-phase", ["mystery"] = "x" });

        // Nothing is appended, and a refused write makes no file and no folder either.
        Assert.False(Directory.Exists(Path.Combine(objProjectPath, "docs")));
        Assert.Contains("mystery", ReadLastRefusalReason());
    }

    /// <summary>When the stream itself is not one of the five, then the write is refused and recorded (REQ-FN-043).</summary>
    [Fact(DisplayName = "REQ-FN-043 a write to an unknown stream is refused and recorded")]
    public async Task AppendAsyncRefusesAnUnknownStream()
    {
        var vProjectId = CreateProject("Sample", objProjectPath);
        var vActions = new MeasurementActions(CreateFactory(), NullLogger<MeasurementActions>.Instance);

        await vActions.AppendAsync(vProjectId, "invoices", new Dictionary<string, string?> { ["cmd"] = "x" });

        Assert.Equal(1, CountRefusedWrites());
        Assert.False(File.Exists(Path.Combine(objProjectPath, "docs", "metrics", "invoices.jsonl")));
    }

    private string ReadLastRefusalReason()
    {
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText = "SELECT Reason FROM RefusedWrite ORDER BY RefusedWriteId DESC LIMIT 1;";
        return (string)vCommand.ExecuteScalar()!;
    }

    private int CountRefusedWrites()
    {
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText = "SELECT COUNT(*) FROM RefusedWrite;";
        return (int)(long)vCommand.ExecuteScalar()!;
    }
}
