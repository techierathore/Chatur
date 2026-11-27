using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TechieRag.Abstractions;
using TechieRag.Models;
using TechieRag.Services;

namespace Chatur.Core.Measurements;

/// <summary>
/// <see cref="IKeyUsageRecorder"/> over the <c>KeyUsage</c> table (migration 0068). The price of a call is the one
/// TechieRag's own <see cref="TokenUsageTracker"/> gives for the model; Chatur keeps no rate card of its own.
/// </summary>
public sealed class KeyUsageRecorder : IKeyUsageRecorder
{
    private readonly IDbConnectionFactory objDb;
    private readonly IClock objClock;
    private readonly ILogger<KeyUsageRecorder> objLogger;

    /// <summary>Creates the recorder.</summary>
    /// <param name="aDb">Opens connections to the signed-in account's database.</param>
    /// <param name="aClock">The current time, for each row's <c>CreatedUtc</c>.</param>
    /// <param name="aLogger">Where a recording that failed is noted; the call itself is never failed by it.</param>
    public KeyUsageRecorder(IDbConnectionFactory aDb, IClock aClock, ILogger<KeyUsageRecorder>? aLogger = null)
    {
        objDb = aDb;
        objClock = aClock;
        objLogger = aLogger ?? NullLogger<KeyUsageRecorder>.Instance;
    }

    /// <inheritdoc />
    public void Watch(ILlmProvider aProvider, int aProviderId, string aModelIdentifier)
    {
        ArgumentNullException.ThrowIfNull(aProvider);
        aProvider.OnCompletionCompleted += (_, aArgs) =>
            Record(aProviderId, aModelIdentifier, aArgs.InputTokens, aArgs.OutputTokens, aArgs.IsEstimated);
    }

    /// <inheritdoc />
    public void Record(int aProviderId, string aModelIdentifier, int aTokensIn, int aTokensOut, bool aIsEstimated)
    {
        try
        {
            var vCost = PriceOf(aModelIdentifier, aTokensIn, aTokensOut, aIsEstimated);
            using var vConnection = objDb.OpenConnection();
            vConnection.Execute(
                """
                INSERT INTO KeyUsage (ProviderId, ModelIdentifier, CreatedUtc, TokensIn, TokensOut, CostUsd)
                VALUES (@aProviderId, @aModelIdentifier, @CreatedUtc, @aTokensIn, @aTokensOut, @vCost);
                """,
                new { aProviderId, aModelIdentifier, CreatedUtc = KeyUsageTime.ToText(objClock.UtcNow), aTokensIn, aTokensOut, vCost });
        }
        catch (Exception vException) when (vException is not OutOfMemoryException)
        {
            // A model call that succeeded is never failed by its bookkeeping; the line then counts one call fewer.
            objLogger.LogWarning(vException, "A model call through provider {ProviderId} could not be recorded for its key's count.", aProviderId);
        }
    }

    /// <summary>
    /// TechieRag's price for one call, or <see langword="null"/> when it has none: the tokens were estimated, or the
    /// model is not in TechieRag's price table (which prices such a model at 0), so a call with tokens costing 0 is
    /// "no price", never free.
    /// </summary>
    /// <param name="aModelIdentifier">The model's identifier, as TechieRag's table matches it.</param>
    /// <param name="aTokensIn">Input tokens.</param>
    /// <param name="aTokensOut">Output tokens.</param>
    /// <param name="aIsEstimated">Whether the counts were estimated because the provider sent none.</param>
    public static decimal? PriceOf(string aModelIdentifier, int aTokensIn, int aTokensOut, bool aIsEstimated)
    {
        if (aIsEstimated)
        {
            return null;
        }

        if (aTokensIn + aTokensOut == 0)
        {
            return 0m;
        }

        var vUsage = new TokenUsage { InputTokens = aTokensIn, OutputTokens = aTokensOut, ModelName = aModelIdentifier };
        new TokenUsageTracker().RecordUsage(vUsage);
        return vUsage.EstimatedCostUsd > 0 ? vUsage.EstimatedCostUsd : null;
    }
}

/// <summary>How <c>KeyUsage.CreatedUtc</c> is written, so a range read compares like with like.</summary>
public static class KeyUsageTime
{
    /// <summary>The text form of a UTC instant: <c>yyyy-MM-dd HH:mm:ss</c>, which sorts as it compares.</summary>
    /// <param name="aUtc">The instant, in UTC.</param>
    public static string ToText(DateTime aUtc) =>
        aUtc.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
}
