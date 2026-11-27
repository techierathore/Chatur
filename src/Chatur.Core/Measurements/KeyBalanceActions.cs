using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;

namespace Chatur.Core.Measurements;

/// <summary>
/// <see cref="IKeyBalanceActions"/> over the <c>Provider</c>, <c>Setting</c> and <c>KeyUsage</c> tables, the secret store
/// and the two balance addresses Chatur knows (Architecture decisions log 2026-11-27): OpenRouter's
/// <c>GET https://openrouter.ai/api/v1/key</c> and DeepSeek's <c>GET https://api.deepseek.com/user/balance</c>. The key
/// is read from the secret store for one question and is never logged, shown or kept.
/// </summary>
public sealed partial class KeyBalanceActions : IKeyBalanceActions
{
    /// <summary>The name of the <see cref="IHttpClientFactory"/> client the balance questions use.</summary>
    public const string HttpClientName = "KeyBalance";

    /// <summary>The <c>Setting</c> key a typed limit is filed under, followed by the provider's id.</summary>
    public const string LimitSettingPrefix = "KeyLimit:";

    /// <summary>How long a balance address has to answer before its line reads "not known".</summary>
    public static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(15);

    private readonly IDbConnectionFactory objDb;
    private readonly ISecretStore objSecrets;
    private readonly IHttpClientFactory objHttp;
    private readonly IClock objClock;
    private readonly TimeZoneInfo objTimeZone;

    /// <summary>Creates the action set for the signed-in account, in this computer's time zone.</summary>
    /// <param name="aDb">Opens connections to the signed-in account's database.</param>
    /// <param name="aSecrets">The operating system's secret store, where each key is filed.</param>
    /// <param name="aHttp">Makes the client the balance questions are sent with.</param>
    /// <param name="aClock">The current time.</param>
    public KeyBalanceActions(IDbConnectionFactory aDb, ISecretStore aSecrets, IHttpClientFactory aHttp, IClock aClock)
        : this(aDb, aSecrets, aHttp, aClock, TimeZoneInfo.Local)
    {
    }

    /// <summary>Creates the action set in a given time zone, so a test knows what "today" is.</summary>
    /// <param name="aDb">Opens connections to the signed-in account's database.</param>
    /// <param name="aSecrets">The operating system's secret store, where each key is filed.</param>
    /// <param name="aHttp">Makes the client the balance questions are sent with.</param>
    /// <param name="aClock">The current time.</param>
    /// <param name="aTimeZone">The time zone a day and an answer's time are read in.</param>
    public KeyBalanceActions(IDbConnectionFactory aDb, ISecretStore aSecrets, IHttpClientFactory aHttp, IClock aClock, TimeZoneInfo aTimeZone)
    {
        objDb = aDb;
        objSecrets = aSecrets;
        objHttp = aHttp;
        objClock = aClock;
        objTimeZone = aTimeZone;
    }

    /// <summary>The balance addresses Chatur knows, by the provider's web address.</summary>
    public enum BalanceAddress
    {
        /// <summary>No address Chatur knows tells this provider's balance or limit.</summary>
        None,

        /// <summary>OpenRouter: the key's own limit, usage and what remains.</summary>
        OpenRouter,

        /// <summary>DeepSeek: the account's balance.</summary>
        DeepSeek
    }

    /// <summary>Which balance address, if any, a provider at <paramref name="aBaseUrl"/> has.</summary>
    /// <param name="aBaseUrl">The provider's web address as stored.</param>
    public static BalanceAddress AddressFor(string? aBaseUrl)
    {
        var vHost = HostOf(aBaseUrl);
        return vHost switch
        {
            "openrouter.ai" => BalanceAddress.OpenRouter,
            "api.deepseek.com" => BalanceAddress.DeepSeek,
            _ when vHost.EndsWith(".openrouter.ai", StringComparison.Ordinal) => BalanceAddress.OpenRouter,
            _ => BalanceAddress.None,
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KeyBalanceLine>> ListAsync(CancellationToken aCt = default)
    {
        List<ProviderRow> vProviders;
        Dictionary<string, string?> vLimits;
        using (var vConnection = objDb.OpenConnection())
        {
            vProviders = (await vConnection.QueryAsync<ProviderRow>(new CommandDefinition(
                "SELECT ProviderId, Name, Connector, SignInMethod, BaseUrl, SecretName FROM Provider ORDER BY Name;",
                cancellationToken: aCt)).ConfigureAwait(false)).ToList();
            vLimits = (await vConnection.QueryAsync<(string Key, string? Value)>(new CommandDefinition(
                "SELECT Key, Value FROM Setting WHERE Key LIKE @Prefix;",
                new { Prefix = LimitSettingPrefix + "%" },
                cancellationToken: aCt)).ConfigureAwait(false)).ToDictionary(aRow => aRow.Key, aRow => aRow.Value, StringComparer.Ordinal);
        }

        var vLines = new List<KeyBalanceLine>(vProviders.Count);
        foreach (var vProvider in vProviders)
        {
            vLimits.TryGetValue(LimitSettingPrefix + vProvider.ProviderId.ToString(CultureInfo.InvariantCulture), out var vLimitText);
            vLines.Add(await LineForAsync(vProvider, ParseLimit(vLimitText), aCt).ConfigureAwait(false));
        }

        return vLines;
    }

    /// <inheritdoc />
    public async Task SetLimitAsync(int aProviderId, KeyLimit aLimit, CancellationToken aCt = default)
    {
        ArgumentNullException.ThrowIfNull(aLimit);
        var vCheck = CheckLimit(aLimit.LimitUsd.ToString(CultureInfo.InvariantCulture), aLimit.CountingFrom);
        if (!vCheck.IsValid)
        {
            throw new ArgumentException(vCheck.AmountError ?? vCheck.FromError, nameof(aLimit));
        }

        var vValue = JsonSerializer.Serialize(new StoredLimit(aLimit.LimitUsd, aLimit.CountingFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        using var vConnection = objDb.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO Setting (Key, Value) VALUES (@Key, @vValue) ON CONFLICT (Key) DO UPDATE SET Value = excluded.Value;",
            new { Key = LimitKey(aProviderId), vValue },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ClearLimitAsync(int aProviderId, CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Setting WHERE Key = @Key;",
            new { Key = LimitKey(aProviderId) },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public KeyLimitCheck CheckLimit(string? aAmountText, DateOnly? aCountingFrom)
    {
        decimal? vAmount = null;
        string? vAmountError = null;
        var vText = aAmountText?.Trim() ?? string.Empty;
        if (vText.Length == 0)
        {
            vAmountError = "Type the limit in US dollars, like 50.00.";
        }
        else if (!AmountPattern().IsMatch(vText) || !decimal.TryParse(vText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var vParsed))
        {
            vAmountError = "Type a number with at most two decimals, like 50.00.";
        }
        else if (vParsed <= 0m)
        {
            vAmountError = "The limit has to be above 0.";
        }
        else
        {
            vAmount = vParsed;
        }

        string? vFromError = null;
        if (aCountingFrom is not { } vFrom)
        {
            vFromError = "Choose the day to count from.";
        }
        else if (vFrom > Today())
        {
            vFromError = "The day to count from cannot be after today.";
        }

        return new KeyLimitCheck(vAmountError is null ? vAmount : null, vAmountError, vFromError);
    }

    private async Task<KeyBalanceLine> LineForAsync(ProviderRow aProvider, KeyLimit? aLimit, CancellationToken aCt)
    {
        var vLine = new KeyBalanceLine(aProvider.ProviderId, aProvider.Name, aProvider.Connector, aProvider.SignInMethod,
            KeyBalanceKind.NotKnown, string.Empty, null, null, null, "USD", aLimit, true);

        if (IsLocal(aProvider))
        {
            return vLine with { Note = "a local model has no key, so nothing is spent on it", Limit = null, CanSetLimit = false };
        }

        var vAddress = AddressFor(aProvider.BaseUrl);
        if (vAddress != BalanceAddress.None)
        {
            return await AskAsync(vLine with { CanSetLimit = false, Limit = null }, aProvider, vAddress, aCt).ConfigureAwait(false);
        }

        if (aLimit is not null)
        {
            return await CountAsync(vLine, aLimit, aCt).ConfigureAwait(false);
        }

        return vLine with { Note = $"{NoBalanceReason(aProvider)}, and no limit is typed" };
    }

    /// <summary>Asks the provider's balance address; anything but a readable answer is "not known" with the reason.</summary>
    private async Task<KeyBalanceLine> AskAsync(KeyBalanceLine aLine, ProviderRow aProvider, BalanceAddress aAddress, CancellationToken aCt)
    {
        var (vHost, vUrl) = aAddress == BalanceAddress.OpenRouter
            ? ("openrouter.ai", "https://openrouter.ai/api/v1/key")
            : ("api.deepseek.com", "https://api.deepseek.com/user/balance");

        var vKey = aProvider.SecretName is null ? null : await objSecrets.ReadAsync(aProvider.SecretName, aCt).ConfigureAwait(false);
        if (string.IsNullOrEmpty(vKey))
        {
            return aLine with { Note = $"its key is not in this computer's secret store, so {vHost} was not asked" };
        }

        string vBody;
        using (var vTimeout = CancellationTokenSource.CreateLinkedTokenSource(aCt))
        {
            vTimeout.CancelAfter(AskTimeout);
            try
            {
                using var vRequest = new HttpRequestMessage(HttpMethod.Get, vUrl);
                vRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", vKey);
                vRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                using var vClient = objHttp.CreateClient(HttpClientName);
                using var vResponse = await vClient.SendAsync(vRequest, vTimeout.Token).ConfigureAwait(false);
                if (!vResponse.IsSuccessStatusCode)
                {
                    var vReason = string.IsNullOrWhiteSpace(vResponse.ReasonPhrase) ? string.Empty : " " + vResponse.ReasonPhrase;
                    return aLine with { Note = $"{vHost} did not answer: it said {(int)vResponse.StatusCode}{vReason}" };
                }

                vBody = await vResponse.Content.ReadAsStringAsync(vTimeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!aCt.IsCancellationRequested)
            {
                return aLine with { Note = $"{vHost} did not answer within {AskTimeout.TotalSeconds:0} seconds" };
            }
            catch (HttpRequestException)
            {
                return aLine with { Note = $"{vHost} did not answer: it could not be reached" };
            }
        }

        var vAnswered = $"{vHost} answered at {When(objClock.UtcNow)}";
        try
        {
            return aAddress == BalanceAddress.OpenRouter
                ? ReadOpenRouter(aLine, vBody, vAnswered)
                : ReadDeepSeek(aLine, vBody, vAnswered);
        }
        catch (Exception vException) when (vException is JsonException or InvalidOperationException or FormatException or KeyNotFoundException)
        {
            return aLine with { Note = $"{vHost} did not answer: its answer could not be read" };
        }
    }

    /// <summary>OpenRouter's <c>/key</c>: <c>data.limit</c> (null for none), <c>data.limit_remaining</c>, <c>data.usage</c>.</summary>
    private static KeyBalanceLine ReadOpenRouter(KeyBalanceLine aLine, string aBody, string aAnswered)
    {
        using var vDocument = JsonDocument.Parse(aBody);
        var vData = vDocument.RootElement.GetProperty("data");
        var vLimit = Number(vData, "limit");
        var vUsed = Number(vData, "usage");
        var vRemaining = Number(vData, "limit_remaining");
        if (vLimit is null)
        {
            return aLine with { Note = $"{aAnswered}: this key has no limit, and that address gives no account balance", UsedUsd = vUsed };
        }

        var vLeft = vRemaining ?? (vUsed is { } vU ? vLimit - vU : null);
        if (vLeft is null)
        {
            throw new FormatException("No amount left in the answer.");
        }

        return aLine with { Kind = KeyBalanceKind.AskedOfProvider, Note = aAnswered, LimitUsd = vLimit, UsedUsd = vUsed, Left = vLeft };
    }

    /// <summary>DeepSeek's <c>/user/balance</c>: <c>balance_infos[]</c> with <c>currency</c> and <c>total_balance</c>; US dollars first.</summary>
    private static KeyBalanceLine ReadDeepSeek(KeyBalanceLine aLine, string aBody, string aAnswered)
    {
        using var vDocument = JsonDocument.Parse(aBody);
        var vInfos = vDocument.RootElement.GetProperty("balance_infos").EnumerateArray().ToList();
        if (vInfos.Count == 0)
        {
            throw new FormatException("No balance in the answer.");
        }

        var vInfo = vInfos.FirstOrDefault(aInfo => string.Equals(Text(aInfo, "currency"), "USD", StringComparison.OrdinalIgnoreCase));
        if (vInfo.ValueKind == JsonValueKind.Undefined)
        {
            vInfo = vInfos[0];
        }

        var vBalance = Number(vInfo, "total_balance") ?? throw new FormatException("No total_balance in the answer.");
        var vCurrency = Text(vInfo, "currency") is { Length: > 0 } vC ? vC.ToUpperInvariant() : "USD";
        return aLine with
        {
            Kind = KeyBalanceKind.AskedOfProvider,
            Note = $"{aAnswered} · the account's balance",
            Left = vBalance,
            Currency = vCurrency,
        };
    }

    /// <summary>Chatur's own count: the priced calls since the day typed, against the limit; any unpriced call makes it "not known".</summary>
    private async Task<KeyBalanceLine> CountAsync(KeyBalanceLine aLine, KeyLimit aLimit, CancellationToken aCt)
    {
        var vFromUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(aLimit.CountingFrom.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), objTimeZone);
        using var vConnection = objDb.OpenConnection();
        var vCount = await vConnection.QuerySingleAsync<CountRow>(new CommandDefinition(
            """
            SELECT COUNT(*) AS Calls,
                   COALESCE(SUM(CASE WHEN CostUsd IS NULL THEN 1 ELSE 0 END), 0) AS Unpriced,
                   CAST(COALESCE(SUM(CostUsd), 0) AS REAL) AS Cost
            FROM KeyUsage WHERE ProviderId = @ProviderId AND CreatedUtc >= @FromUtc;
            """,
            new { aLine.ProviderId, FromUtc = KeyUsageTime.ToText(vFromUtc) },
            cancellationToken: aCt)).ConfigureAwait(false);

        var vSince = aLimit.CountingFrom.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
        var vCalls = $"{vCount.Calls} call{(vCount.Calls == 1 ? string.Empty : "s")}";
        if (vCount.Unpriced > 0)
        {
            return aLine with
            {
                Note = $"{vCount.Unpriced} of {vCalls} since {vSince} had no price from TechieRag, so what is used is not known",
                LimitUsd = aLimit.LimitUsd,
            };
        }

        var vUsed = Math.Round(Convert.ToDecimal(vCount.Cost, CultureInfo.InvariantCulture), 2);
        return aLine with
        {
            Kind = KeyBalanceKind.OwnCount,
            Note = $"against your limit, since {vSince} · {vCalls}",
            LimitUsd = aLimit.LimitUsd,
            UsedUsd = vUsed,
            Left = aLimit.LimitUsd - vUsed,
        };
    }

    /// <summary>Why a key with no balance address has no balance, in the provider's terms.</summary>
    private static string NoBalanceReason(ProviderRow aProvider)
    {
        if (string.Equals(aProvider.SignInMethod, "Subscription", StringComparison.OrdinalIgnoreCase))
        {
            return "a subscription is paid by the month and has no balance on a key";
        }

        return aProvider.Connector.ToLowerInvariant() switch
        {
            "anthropic" => "Anthropic gives no balance to a normal key",
            "openai" => "OpenAI gives no balance to a normal key",
            "google" or "gemini" or "googlegemini" => "Google gives no balance to a key",
            _ => $"{(HostOf(aProvider.BaseUrl) is { Length: > 0 } vHost ? vHost : aProvider.Name)} has no address Chatur knows for its balance",
        };
    }

    private static bool IsLocal(ProviderRow aProvider) =>
        string.Equals(aProvider.SignInMethod, "None", StringComparison.OrdinalIgnoreCase)
        || aProvider.Connector.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
        || aProvider.Connector.Equals("LmStudio", StringComparison.OrdinalIgnoreCase);

    private static string HostOf(string? aBaseUrl) =>
        Uri.TryCreate(aBaseUrl?.Trim(), UriKind.Absolute, out var vUri) ? vUri.Host.ToLowerInvariant() : string.Empty;

    private static decimal? Number(JsonElement aElement, string aName)
    {
        if (!aElement.TryGetProperty(aName, out var vValue))
        {
            return null;
        }

        return vValue.ValueKind switch
        {
            JsonValueKind.Number => vValue.GetDecimal(),
            JsonValueKind.String => decimal.Parse(vValue.GetString()!, NumberStyles.Number, CultureInfo.InvariantCulture),
            JsonValueKind.Null => null,
            _ => throw new FormatException($"{aName} is not a number."),
        };
    }

    private static string? Text(JsonElement aElement, string aName) =>
        aElement.TryGetProperty(aName, out var vValue) && vValue.ValueKind == JsonValueKind.String ? vValue.GetString() : null;

    private DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(objClock.UtcNow, DateTimeKind.Utc), objTimeZone));

    /// <summary>"09:14 today", or "09:14 on 3 Nov 2026" for another day.</summary>
    private string When(DateTime aUtc)
    {
        var vLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(aUtc, DateTimeKind.Utc), objTimeZone);
        var vTime = vLocal.ToString("HH:mm", CultureInfo.InvariantCulture);
        return DateOnly.FromDateTime(vLocal) == Today() ? $"{vTime} today" : $"{vTime} on {vLocal.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}";
    }

    private static string LimitKey(int aProviderId) => LimitSettingPrefix + aProviderId.ToString(CultureInfo.InvariantCulture);

    private static KeyLimit? ParseLimit(string? aValue)
    {
        if (string.IsNullOrWhiteSpace(aValue))
        {
            return null;
        }

        try
        {
            var vStored = JsonSerializer.Deserialize<StoredLimit>(aValue);
            return vStored is not null && vStored.LimitUsd > 0m
                && DateOnly.TryParseExact(vStored.CountingFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var vFrom)
                ? new KeyLimit(vStored.LimitUsd, vFrom)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [GeneratedRegex(@"^\d+(\.\d{1,2})?$", RegexOptions.CultureInvariant)]
    private static partial Regex AmountPattern();

    /// <summary>A typed limit as the <c>Setting</c> row keeps it.</summary>
    private sealed record StoredLimit(decimal LimitUsd, string CountingFrom);

    private sealed class CountRow
    {
        public long Calls { get; set; }

        public long Unpriced { get; set; }

        public double Cost { get; set; }
    }

    private sealed class ProviderRow
    {
        public int ProviderId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Connector { get; set; } = string.Empty;

        public string SignInMethod { get; set; } = string.Empty;

        public string BaseUrl { get; set; } = string.Empty;

        public string? SecretName { get; set; }
    }
}
