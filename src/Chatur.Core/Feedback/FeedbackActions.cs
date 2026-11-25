using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Chatur.Core.Feedback;

/// <summary>
/// <see cref="IFeedbackActions"/> over the <c>FeedbackEntry</c> rows. Every entry is written to the
/// account's own database before anything is sent, so nothing is lost when the network is down
/// (REQ-FN-074); a later list sends what is waiting and asks for the newest state of the rest.
/// </summary>
public sealed class FeedbackActions : IFeedbackActions
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private readonly IDbConnectionFactory objConnections;
    private readonly IFeedbackSource objSource;
    private readonly IClock objClock;
    private readonly ILogger<FeedbackActions> objLogger;

    /// <summary>Creates the action implementation.</summary>
    /// <param name="aConnections">Opens connections to the account's database.</param>
    /// <param name="aSource">Where entries go: App Manager in the real app.</param>
    /// <param name="aClock">The time source.</param>
    /// <param name="aLogger">Where an entry that could not be sent is logged.</param>
    public FeedbackActions(IDbConnectionFactory aConnections, IFeedbackSource aSource, IClock aClock, ILogger<FeedbackActions> aLogger)
    {
        objConnections = aConnections;
        objSource = aSource;
        objClock = aClock;
        objLogger = aLogger;
    }

    /// <inheritdoc />
    public async Task<FeedbackEntry> SendAsync(FeedbackDraft aDraft, CancellationToken aCt = default)
    {
        ArgumentNullException.ThrowIfNull(aDraft);
        var vTitle = (aDraft.Title ?? string.Empty).Trim();
        var vDetail = (aDraft.Detail ?? string.Empty).Trim();
        if (!FeedbackAbout.IsKnown(aDraft.About))
        {
            throw new ArgumentException("Pick what the entry is about.", nameof(aDraft));
        }

        if (vTitle.Length < FeedbackLimits.TitleMin || vTitle.Length > FeedbackLimits.TitleMax)
        {
            throw new ArgumentException($"The title needs {FeedbackLimits.TitleMin} to {FeedbackLimits.TitleMax} characters.", nameof(aDraft));
        }

        if (vDetail.Length < FeedbackLimits.DetailMin || vDetail.Length > FeedbackLimits.DetailMax)
        {
            throw new ArgumentException($"The description needs {FeedbackLimits.DetailMin} to {FeedbackLimits.DetailMax} characters.", nameof(aDraft));
        }

        var vEnvironment = aDraft.AttachMachineDetails ? MachineDetails() : null;
        int vId;
        using (var vDb = objConnections.OpenConnection())
        {
            vId = vDb.ExecuteScalar<int>(
                "INSERT INTO FeedbackEntry (About, Severity, Title, Detail, Environment, State, CreatedUtc) " +
                "VALUES (@About, @Severity, @Title, @Detail, @Environment, 'Waiting', @CreatedUtc); SELECT last_insert_rowid();",
                new { aDraft.About, Severity = aDraft.Severity.ToString(), Title = vTitle, Detail = vDetail, Environment = vEnvironment, CreatedUtc = Stamp(objClock.UtcNow) });
        }

        await Gate.WaitAsync(aCt).ConfigureAwait(false);
        try
        {
            await TryRaiseAsync(vId, aCt).ConfigureAwait(false);
        }
        finally
        {
            Gate.Release();
        }

        return (await ReadAllAsync().ConfigureAwait(false)).First(aEntry => aEntry.FeedbackEntryId == vId);
    }

    /// <inheritdoc />
    public async Task<FeedbackView> ListAsync(CancellationToken aCt = default)
    {
        await Gate.WaitAsync(aCt).ConfigureAwait(false);
        try
        {
            var vNetworkUp = true;
            foreach (var vEntry in (await ReadAllAsync().ConfigureAwait(false)).Where(aEntry => aEntry.State == FeedbackState.Waiting).OrderBy(aEntry => aEntry.FeedbackEntryId))
            {
                vNetworkUp = await TryRaiseAsync(vEntry.FeedbackEntryId, aCt).ConfigureAwait(false);
                if (!vNetworkUp)
                {
                    break;
                }
            }

            if (vNetworkUp)
            {
                await RefreshRaisedAsync(aCt).ConfigureAwait(false);
            }
        }
        finally
        {
            Gate.Release();
        }

        var vAll = await ReadAllAsync().ConfigureAwait(false);
        return new FeedbackView(vAll, vAll.Count(aEntry => aEntry.State == FeedbackState.Waiting));
    }

    /// <summary>Sends one waiting entry. Returns <see langword="false"/> when the source could not be reached, so the caller stops trying the rest.</summary>
    private async Task<bool> TryRaiseAsync(int aId, CancellationToken aCt)
    {
        using var vDb = objConnections.OpenConnection();
        var vRow = vDb.QuerySingleOrDefault<Row>(
            "SELECT FeedbackEntryId, About, Severity, Title, Detail, Environment FROM FeedbackEntry WHERE FeedbackEntryId = @Id AND State = 'Waiting';",
            new { Id = aId });
        if (vRow is null)
        {
            return true;
        }

        try
        {
            var vRaised = await objSource.RaiseAsync(
                new FeedbackRemoteRequest(vRow.About, Enum.Parse<FeedbackSeverity>(vRow.Severity), vRow.Title, vRow.Detail, vRow.Environment),
                aCt).ConfigureAwait(false);
            vDb.Execute(
                "UPDATE FeedbackEntry SET State = 'Raised', RemoteId = @RemoteId, RemoteNumber = @RemoteNumber, LastError = NULL WHERE FeedbackEntryId = @Id;",
                new { vRaised.RemoteId, vRaised.RemoteNumber, Id = aId });
            return true;
        }
        catch (FeedbackUnreachableException vException)
        {
            objLogger.LogInformation("Feedback entry {Id} is kept: {Reason}", aId, vException.Message);
            return false;
        }
        catch (FeedbackRefusedException vException)
        {
            objLogger.LogWarning("Feedback entry {Id} was refused: {Reason}", aId, vException.Message);
            vDb.Execute("UPDATE FeedbackEntry SET LastError = @Reason WHERE FeedbackEntryId = @Id;", new { Reason = vException.Message, Id = aId });
            return true;
        }
    }

    /// <summary>Asks for the newest state of every entry the source has taken and that is not yet fixed.</summary>
    private async Task RefreshRaisedAsync(CancellationToken aCt)
    {
        var vOpen = (await ReadAllAsync().ConfigureAwait(false)).Where(aEntry => aEntry.State is FeedbackState.Raised or FeedbackState.Answered).ToList();
        using var vDb = objConnections.OpenConnection();
        foreach (var vEntry in vOpen)
        {
            var vRemoteId = vDb.ExecuteScalar<string?>("SELECT RemoteId FROM FeedbackEntry WHERE FeedbackEntryId = @Id;", new { Id = vEntry.FeedbackEntryId });
            if (string.IsNullOrEmpty(vRemoteId))
            {
                continue;
            }

            FeedbackRemoteState? vState;
            try
            {
                vState = await objSource.ReadAsync(vRemoteId, aCt).ConfigureAwait(false);
            }
            catch (FeedbackUnreachableException vException)
            {
                objLogger.LogInformation("Feedback state not read: {Reason}", vException.Message);
                return;
            }
            catch (FeedbackRefusedException vException)
            {
                objLogger.LogWarning("Feedback state refused: {Reason}", vException.Message);
                continue;
            }

            if (vState is null)
            {
                continue;
            }

            var vNew = vState.IsFinished ? FeedbackState.Fixed : string.IsNullOrWhiteSpace(vState.Answer) ? FeedbackState.Raised : FeedbackState.Answered;
            vDb.Execute(
                "UPDATE FeedbackEntry SET State = @State, Answer = @Answer WHERE FeedbackEntryId = @Id;",
                new { State = vNew.ToString(), Answer = string.IsNullOrWhiteSpace(vState.Answer) ? null : vState.Answer.Trim(), Id = vEntry.FeedbackEntryId });
        }
    }

    private async Task<IReadOnlyList<FeedbackEntry>> ReadAllAsync()
    {
        using var vDb = objConnections.OpenConnection();
        var vRows = await vDb.QueryAsync<EntryRow>(
            "SELECT FeedbackEntryId, About, Severity, Title, Detail, CreatedUtc, State, RemoteNumber, Answer, LastError " +
            "FROM FeedbackEntry ORDER BY FeedbackEntryId DESC;").ConfigureAwait(false);
        return
        [
            .. vRows.Select(aRow => new FeedbackEntry(
                aRow.FeedbackEntryId,
                aRow.About,
                Enum.Parse<FeedbackSeverity>(aRow.Severity),
                aRow.Title,
                aRow.Detail,
                DateTime.Parse(aRow.CreatedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                Enum.Parse<FeedbackState>(aRow.State),
                aRow.RemoteNumber,
                aRow.Answer ?? (aRow.State == nameof(FeedbackState.Waiting) && aRow.LastError is { } vError ? "Not sent: " + vError : null)))
        ];
    }

    private static string MachineDetails()
    {
        var vVersion = typeof(FeedbackActions).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(FeedbackActions).Assembly.GetName().Version?.ToString()
            ?? "unknown";
        return $"Chatur {vVersion.Split('+')[0]}; {RuntimeInformation.OSDescription}; {RuntimeInformation.OSArchitecture}";
    }

    private static string Stamp(DateTime aUtc) => aUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private sealed class Row
    {
        public string About { get; set; } = string.Empty;

        public string Severity { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Detail { get; set; } = string.Empty;

        public string? Environment { get; set; }
    }

    private sealed class EntryRow
    {
        public int FeedbackEntryId { get; set; }

        public string About { get; set; } = string.Empty;

        public string Severity { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Detail { get; set; } = string.Empty;

        public string CreatedUtc { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string? RemoteNumber { get; set; }

        public string? Answer { get; set; }

        public string? LastError { get; set; }
    }
}
