using Chatur.Core.Actions;
using Chatur.Core.Agent;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;
using TechieRag.Llm;

namespace Chatur.Core.Routing;

/// <summary>
/// <see cref="IAutoRouter"/> over TechieRag's <see cref="TechieRag.Abstractions.IModelChooser"/>
/// (REQ-FN-071, REQ-FN-072). The candidates are the connected models in the owner's tier chains; the
/// small model that does the choosing is the first model of the lowest tier (the highest tier number,
/// "on this machine" in the shipped setup) that has one. Nothing here reads the request itself: the
/// reading is the small model's job. When routing is off the chooser is never built or called, so the
/// owner's tiers and chains decide exactly as before.
/// </summary>
public sealed class AutoRouter : IAutoRouter
{
    /// <summary>The Source written to the log when the small model failed and the tiers and chains decided.</summary>
    public const string TiersAndChainsSource = "TiersAndChains";

    private readonly IDbConnectionFactory objDb;
    private readonly IRoutingActions objRouting;
    private readonly IAgentLlmProviderFactory objProviders;
    private readonly IModelChooserFactory objChoosers;
    private readonly IClock objClock;

    /// <summary>Creates the router.</summary>
    /// <param name="aDb">Opens connections to Chatur's own database.</param>
    /// <param name="aRouting">Says whether automatic routing is on and what the owner's chains are.</param>
    /// <param name="aProviders">Builds the TechieRag provider for the small model.</param>
    /// <param name="aChoosers">Builds the chooser around the small model.</param>
    /// <param name="aClock">Timestamps each logged choice.</param>
    public AutoRouter(
        IDbConnectionFactory aDb,
        IRoutingActions aRouting,
        IAgentLlmProviderFactory aProviders,
        IModelChooserFactory aChoosers,
        IClock aClock)
    {
        objDb = aDb;
        objRouting = aRouting;
        objProviders = aProviders;
        objChoosers = aChoosers;
        objClock = aClock;
    }

    /// <inheritdoc />
    public async Task<ConnectedModel?> ChooseAsync(string aRequest, int aSessionId, int aRoleId, CancellationToken aCt = default)
    {
        if (!await objRouting.AutoRoutingAsync(aCt).ConfigureAwait(false))
        {
            return null;
        }

        var vCandidates = await CandidatesAsync(aCt).ConfigureAwait(false);
        if (vCandidates.Count == 0)
        {
            return null;
        }

        try
        {
            var vLowestTier = vCandidates.Max(aCandidate => aCandidate.Tier);
            var vSmallModel = vCandidates.First(aCandidate => aCandidate.Tier == vLowestTier).Model;
            var vProvider = await objProviders.CreateAsync(vSmallModel, aCt).ConfigureAwait(false);
            var vChooser = objChoosers.Create(vProvider);

            var vChoice = await vChooser
                .ChooseAsync(aRequest, vCandidates.Select(aCandidate => aCandidate.Candidate).ToList(), aCt)
                .ConfigureAwait(false);
            var vChosen = vCandidates.FirstOrDefault(aCandidate => aCandidate.Candidate.ModelId == vChoice.ModelId)
                ?? throw new InvalidOperationException($"The chooser answered \"{vChoice.ModelId}\", which is not one of the candidates.");

            await LogAsync(aSessionId, aRoleId, vChosen.Model.ModelIdentifier, vChoice.Source.ToString(), vChoice.Reason, aCt).ConfigureAwait(false);
            return vChosen.Model;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception vException)
        {
            // The small model itself failed: the owner's tiers and chains decide, and the log says so.
            await LogAsync(aSessionId, aRoleId, "-", TiersAndChainsSource, $"The small model could not choose: {vException.Message}", aCt).ConfigureAwait(false);
            return null;
        }
    }

    private async Task<List<RoutedCandidate>> CandidatesAsync(CancellationToken aCt)
    {
        List<ModelRow> vConnected;
        List<RoleRow> vRoles;
        using (var vConnection = objDb.OpenConnection())
        {
            vConnected = (await vConnection.QueryAsync<ModelRow>(new CommandDefinition(
                """
                SELECT m.ModelId AS ModelId, m.ProviderId AS ProviderId, p.Name AS ProviderName, p.Connector AS Connector,
                       p.BaseUrl AS BaseUrl, p.SecretName AS SecretName, m.Identifier AS Identifier, m.Tier AS Tier
                FROM Model m JOIN Provider p ON p.ProviderId = m.ProviderId
                WHERE p.State = 'Connected' ORDER BY m.Tier, m.ModelId;
                """,
                cancellationToken: aCt)).ConfigureAwait(false)).ToList();
            vRoles = (await vConnection.QueryAsync<RoleRow>(new CommandDefinition(
                "SELECT Name, Tier FROM Role ORDER BY RoleId;",
                cancellationToken: aCt)).ConfigureAwait(false)).ToList();
        }

        var vOrdered = new List<(ModelRow Row, int Tier)>();
        foreach (var vChain in await objRouting.TiersAsync(aCt).ConfigureAwait(false))
        {
            foreach (var vModelId in vChain.ModelIdsInOrder)
            {
                var vRow = vConnected.FirstOrDefault(aRow => aRow.ModelId == vModelId);
                if (vRow is not null && vOrdered.All(aPair => aPair.Row.ModelId != vRow.ModelId))
                {
                    vOrdered.Add((vRow, vChain.Tier));
                }
            }
        }

        if (vOrdered.Count == 0)
        {
            vOrdered.AddRange(vConnected.Select(aRow => (aRow, (int)aRow.Tier)));
        }

        var vResult = new List<RoutedCandidate>();
        foreach (var (vRow, vTier) in vOrdered)
        {
            if (vResult.Any(aCandidate => aCandidate.Candidate.ModelId == vRow.Identifier))
            {
                continue;
            }

            var vModel = new ConnectedModel((int)vRow.ModelId, (int)vRow.ProviderId, vRow.ProviderName, vRow.Connector, vRow.BaseUrl, vRow.SecretName, vRow.Identifier);
            var vCandidate = new ModelCandidate { ModelId = vRow.Identifier, Tier = $"Tier {vTier}", Notes = NotesFor(vTier, vRoles) };
            vResult.Add(new RoutedCandidate(vModel, vTier, vCandidate));
        }

        return vResult;
    }

    private static string NotesFor(int aTier, List<RoleRow> aRoles)
    {
        var vDescription = aTier switch
        {
            1 => "The strongest tier, for work that must not go wrong.",
            2 => "The everyday tier, for ordinary work.",
            _ => "The tier on this machine, cheap and fast, for small jobs."
        };
        var vNames = aRoles.Where(aRole => aRole.Tier == aTier).Select(aRole => aRole.Name).ToList();
        return vNames.Count == 0
            ? vDescription
            : $"{vDescription} The owner set it for: {string.Join(", ", vNames)}.";
    }

    private async Task LogAsync(int aSessionId, int aRoleId, string aModelIdentifier, string aSource, string aReason, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO RoutingChoice (SessionId, RoleId, ModelIdentifier, Source, Reason, CreatedUtc)
            VALUES (@aSessionId, @aRoleId, @aModelIdentifier, @aSource, @aReason, @vNowUtc);
            """,
            new { aSessionId, aRoleId, aModelIdentifier, aSource, aReason, vNowUtc = objClock.UtcNow.ToString("O") },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    private sealed record RoutedCandidate(ConnectedModel Model, int Tier, ModelCandidate Candidate);

    private sealed record ModelRow(long ModelId, long ProviderId, string ProviderName, string Connector, string BaseUrl, string? SecretName, string Identifier, long Tier);

    private sealed record RoleRow(string Name, long Tier);
}
