using System.Globalization;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Chatur.Core.Processes;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Chatur.Core.Integrations;

/// <summary>
/// <see cref="IIntegrationActions"/> over the <c>IntegrationAction</c>, <c>IntegrationToken</c>,
/// <c>IntegrationCall</c>, <c>VerifierWindowCheck</c> and <c>ReleaseDownload</c> rows. An outside call is
/// started through <see cref="IProcessActions"/>, the same way the Processes screen starts one.
/// </summary>
public sealed class IntegrationActions : IIntegrationActions
{
    private const string SelectedProjectSettingKey = "SelectedProjectId";

    private readonly IDbConnectionFactory objConnections;
    private readonly IProcessActions objProcesses;
    private readonly IClock objClock;
    private readonly AppState objAppState;
    private readonly ILogger<IntegrationActions> objLogger;

    /// <summary>Creates the action implementation.</summary>
    /// <param name="aConnections">Opens connections to the signed-in account's database.</param>
    /// <param name="aProcesses">Starts the process an outside tool asked for.</param>
    /// <param name="aClock">The time source.</param>
    /// <param name="aAppState">Names the signed-in account a new token is made for.</param>
    /// <param name="aLogger">Where a refused call is logged (never the token).</param>
    public IntegrationActions(IDbConnectionFactory aConnections, IProcessActions aProcesses, IClock aClock, AppState aAppState, ILogger<IntegrationActions> aLogger)
    {
        objConnections = aConnections;
        objProcesses = aProcesses;
        objClock = aClock;
        objAppState = aAppState;
        objLogger = aLogger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishedAction>> ListActionsAsync(CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vChosen = (await vConnection.QueryAsync<ChosenRow>(new CommandDefinition("SELECT Code, Enabled FROM IntegrationAction", cancellationToken: aCt)).ConfigureAwait(false))
            .ToDictionary(aRow => aRow.Code, aRow => aRow.Enabled != 0, StringComparer.OrdinalIgnoreCase);

        return [.. PublishedActionCatalogue.All.Select(aAction => new PublishedAction(
            aAction.Code,
            aAction.Name,
            aAction.Does,
            aAction.StartedBy,
            aAction.Method,
            aAction.Path,
            aAction.Built,
            aAction.Built && (vChosen.TryGetValue(aAction.Code, out var vOn) ? vOn : aAction.OnByDefault)))];
    }

    /// <inheritdoc />
    public async Task SetActionEnabledAsync(string aCode, bool aEnabled, CancellationToken aCt = default)
    {
        var vAction = PublishedActionCatalogue.Find(aCode ?? string.Empty)
            ?? throw new ArgumentException($"'{aCode}' is not an action Chatur offers.", nameof(aCode));
        if (aEnabled && !vAction.Built)
        {
            throw new ArgumentException($"'{vAction.Name}' is not built yet, so it cannot be turned on.", nameof(aCode));
        }

        using var vConnection = objConnections.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO IntegrationAction (Code, Enabled, ChangedUtc) VALUES (@Code, @Enabled, @Now)
              ON CONFLICT (Code) DO UPDATE SET Enabled = @Enabled, ChangedUtc = @Now",
            new { vAction.Code, Enabled = aEnabled ? 1 : 0, Now = Stamp(objClock.UtcNow) },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<NewIntegrationToken> CreateTokenAsync(CancellationToken aCt = default)
    {
        var vToken = IntegrationToken.Make(AccountScope.CurrentUserId(objAppState));
        using var vConnection = objConnections.OpenConnection();
        var vId = await vConnection.ExecuteScalarAsync<int>(new CommandDefinition(
            @"INSERT INTO IntegrationToken (TokenHash, Hint, CreatedUtc) VALUES (@Hash, @Hint, @Now);
              SELECT last_insert_rowid();",
            new { Hash = IntegrationToken.Hash(vToken), Hint = IntegrationToken.Hint(vToken), Now = Stamp(objClock.UtcNow) },
            cancellationToken: aCt)).ConfigureAwait(false);
        return new NewIntegrationToken(vId, vToken);
    }

    /// <inheritdoc />
    public async Task<OutsideCallResult> StartFromOutsideAsync(string? aToken, string? aProject, string? aProcess, CancellationToken aCt = default)
    {
        if (aToken is null || !IntegrationToken.TryReadAccount(aToken, out _))
        {
            return Refuse(OutsideCallOutcome.Unauthorized, "The token is missing or not one Chatur made.");
        }

        using var vConnection = objConnections.OpenConnection();
        var vTokenId = await vConnection.ExecuteScalarAsync<int?>(new CommandDefinition(
            "SELECT IntegrationTokenId FROM IntegrationToken WHERE TokenHash = @Hash AND RevokedUtc IS NULL",
            new { Hash = IntegrationToken.Hash(aToken) },
            cancellationToken: aCt)).ConfigureAwait(false);
        if (vTokenId is null)
        {
            return Refuse(OutsideCallOutcome.Unauthorized, "The token is not known or was withdrawn.");
        }

        var vStart = (await ListActionsAsync(aCt).ConfigureAwait(false)).First(aAction => aAction.Code == PublishedActionCatalogue.StartCode);
        if (!vStart.Enabled)
        {
            return Refuse(OutsideCallOutcome.NotThere, "Not found.");
        }

        var vProcessCode = (aProcess ?? string.Empty).Trim();
        if (ProcessCatalogue.Find(vProcessCode) is null)
        {
            return Refuse(OutsideCallOutcome.Refused, $"'{vProcessCode}' is not one of the TechieFlow commands.");
        }

        var vProject = await FindProjectAsync(vConnection, aProject, aCt).ConfigureAwait(false);
        if (vProject is null)
        {
            return Refuse(OutsideCallOutcome.Refused, string.IsNullOrWhiteSpace(aProject) ? "No project is open in Chatur, so name one." : $"'{aProject}' is not a project Chatur knows.");
        }

        ProcessRunStarted vStarted;
        try
        {
            vStarted = await objProcesses.StartAsync(vProject.ProjectId, vProcessCode, aCt).ConfigureAwait(false);
        }
        catch (ArgumentException vException)
        {
            objLogger.LogInformation("An outside call to start {Process} on {Project} was refused: {Reason}", vProcessCode, vProject.Name, vException.Message);
            return Refuse(OutsideCallOutcome.Refused, vException.Message);
        }

        await vConnection.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO IntegrationCall (IntegrationTokenId, ActionCode, RunId, ProjectName, ProcessCode, CalledUtc)
              VALUES (@TokenId, @Action, @RunId, @Project, @Process, @Now)",
            new { TokenId = vTokenId.Value, Action = PublishedActionCatalogue.StartCode, vStarted.RunId, Project = vProject.Name, Process = vProcessCode, Now = Stamp(objClock.UtcNow) },
            cancellationToken: aCt)).ConfigureAwait(false);
        objLogger.LogInformation("An outside tool started {Process} on {Project} as run {RunId}", vProcessCode, vProject.Name, vStarted.RunId);
        return new OutsideCallResult(OutsideCallOutcome.Started, vStarted.RunId, vStarted.State, vProject.Name, vProcessCode, string.Empty);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OutsideRun>> ListOutsideRunsAsync(CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRows = await vConnection.QueryAsync<CallRow>(new CommandDefinition(
            @"SELECT RunId, ProcessCode, ProjectName, CalledUtc FROM IntegrationCall
              WHERE RunId IS NOT NULL ORDER BY IntegrationCallId DESC LIMIT 20",
            cancellationToken: aCt)).ConfigureAwait(false);
        return [.. vRows.Select(aRow => new OutsideRun(aRow.RunId, aRow.ProcessCode, aRow.ProjectName, Parse(aRow.CalledUtc)))];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WindowCheck>> ListWindowChecksAsync(CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRows = await vConnection.QueryAsync<CheckRow>(new CommandDefinition(
            @"SELECT Application, Platform, Machine, ScreenshotsKept, CheckedUtc, Result FROM VerifierWindowCheck
              ORDER BY CheckedUtc DESC, VerifierWindowCheckId DESC LIMIT 20",
            cancellationToken: aCt)).ConfigureAwait(false);
        return [.. vRows.Select(aRow => new WindowCheck(aRow.Application, aRow.Platform, aRow.Machine, aRow.ScreenshotsKept, Parse(aRow.CheckedUtc), aRow.Result))];
    }

    /// <inheritdoc />
    public async Task RecordWindowCheckAsync(WindowCheck aCheck, CancellationToken aCt = default)
    {
        ArgumentNullException.ThrowIfNull(aCheck);
        if (aCheck.Platform is not ("Mac Catalyst" or "Windows"))
        {
            throw new ArgumentException("The verifier drives a Mac Catalyst or a Windows application's own window.", nameof(aCheck));
        }

        if (aCheck.ScreenshotsKept < 1)
        {
            throw new ArgumentException("A check that drove a window keeps at least one screenshot.", nameof(aCheck));
        }

        using var vConnection = objConnections.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO VerifierWindowCheck (Application, Platform, Machine, ScreenshotsKept, CheckedUtc, Result)
              VALUES (@Application, @Platform, @Machine, @ScreenshotsKept, @CheckedUtc, @Result)",
            new { aCheck.Application, aCheck.Platform, aCheck.Machine, aCheck.ScreenshotsKept, CheckedUtc = Stamp(aCheck.CheckedUtc), aCheck.Result },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SignedDownload>> ListDownloadsAsync(CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRows = (await vConnection.QueryAsync<DownloadRow>(new CommandDefinition(
            @"SELECT Version, Platform, FileName, SignedBy, Notarisation, PublishedUtc FROM ReleaseDownload
              WHERE Version = (SELECT Version FROM ReleaseDownload ORDER BY PublishedUtc DESC, ReleaseDownloadId DESC LIMIT 1)
              ORDER BY CASE Platform WHEN 'Mac' THEN 0 ELSE 1 END, ReleaseDownloadId DESC",
            cancellationToken: aCt)).ConfigureAwait(false)).ToList();
        return [.. vRows.GroupBy(aRow => aRow.Platform).Select(aGroup => aGroup.First())
            .Select(aRow => new SignedDownload(aRow.Version, aRow.Platform, aRow.FileName, aRow.SignedBy, Enum.Parse<NotarisationState>(aRow.Notarisation), Parse(aRow.PublishedUtc)))];
    }

    /// <inheritdoc />
    public async Task RecordDownloadAsync(SignedDownload aDownload, CancellationToken aCt = default)
    {
        ArgumentNullException.ThrowIfNull(aDownload);
        if (aDownload.Platform is not ("Mac" or "Windows"))
        {
            throw new ArgumentException("A release has a Mac download and a Windows download.", nameof(aDownload));
        }

        if (string.IsNullOrWhiteSpace(aDownload.SignedBy))
        {
            throw new ArgumentException("A download is shown only when it says who signed it.", nameof(aDownload));
        }

        using var vConnection = objConnections.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO ReleaseDownload (Version, Platform, FileName, SignedBy, Notarisation, PublishedUtc)
              VALUES (@Version, @Platform, @FileName, @SignedBy, @Notarisation, @PublishedUtc)",
            new { aDownload.Version, aDownload.Platform, aDownload.FileName, aDownload.SignedBy, Notarisation = aDownload.Notarisation.ToString(), PublishedUtc = Stamp(aDownload.PublishedUtc) },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    private static OutsideCallResult Refuse(OutsideCallOutcome aOutcome, string aMessage) =>
        new(aOutcome, 0, string.Empty, string.Empty, string.Empty, aMessage);

    private static string Stamp(DateTime aUtc) => aUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static DateTime Parse(string aStamp) => DateTime.Parse(aStamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static async Task<ProjectRow?> FindProjectAsync(System.Data.IDbConnection aConnection, string? aName, CancellationToken aCt)
    {
        if (!string.IsNullOrWhiteSpace(aName))
        {
            return await aConnection.QueryFirstOrDefaultAsync<ProjectRow>(new CommandDefinition(
                "SELECT ProjectId, Name FROM Project WHERE Name = @Name COLLATE NOCASE AND RemovedUtc IS NULL LIMIT 1",
                new { Name = aName.Trim() },
                cancellationToken: aCt)).ConfigureAwait(false);
        }

        return await aConnection.QueryFirstOrDefaultAsync<ProjectRow>(new CommandDefinition(
            @"SELECT p.ProjectId, p.Name FROM Setting s JOIN Project p ON p.ProjectId = CAST(s.Value AS INTEGER)
              WHERE s.Key = @Key AND p.RemovedUtc IS NULL",
            new { Key = SelectedProjectSettingKey },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    private sealed class ChosenRow
    {
        public string Code { get; set; } = string.Empty;

        public int Enabled { get; set; }
    }

    private sealed class ProjectRow
    {
        public int ProjectId { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class CallRow
    {
        public int RunId { get; set; }

        public string ProcessCode { get; set; } = string.Empty;

        public string ProjectName { get; set; } = string.Empty;

        public string CalledUtc { get; set; } = string.Empty;
    }

    private sealed class CheckRow
    {
        public string Application { get; set; } = string.Empty;

        public string Platform { get; set; } = string.Empty;

        public string Machine { get; set; } = string.Empty;

        public int ScreenshotsKept { get; set; }

        public string CheckedUtc { get; set; } = string.Empty;

        public string Result { get; set; } = string.Empty;
    }

    private sealed class DownloadRow
    {
        public string Version { get; set; } = string.Empty;

        public string Platform { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public string SignedBy { get; set; } = string.Empty;

        public string Notarisation { get; set; } = string.Empty;

        public string PublishedUtc { get; set; } = string.Empty;
    }
}
