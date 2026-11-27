using System.Net;
using System.Text;
using Chatur.Core.Actions;
using Chatur.Core.Measurements;
using Chatur.Core.Platform;
using Chatur.Tests.Support;
using Microsoft.Data.Sqlite;
using TechieRag.Abstractions;
using TechieRag.Models;
using Xunit;

namespace Chatur.Tests.Measurements;

/// <summary>
/// What is left on each key (REQ-UI-129 to REQ-UI-131, REQ-FN-326) against a real, migrated, temporary database, a fixed
/// clock and a fake web handler for the balance addresses — no real network call is made.
/// </summary>
public sealed class KeyBalanceActionsTests : MigratedDatabaseFixture
{
    /// <summary>Noon on 27 Nov 2026, UTC; the actions read days in UTC here.</summary>
    private static readonly DateTime NowUtc = new(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc);

    private readonly InMemorySecretStore objSecrets = new();
    private readonly FakeHandler objHandler = new();

    /// <summary>When Measurements opens with four providers of different kinds, then each connected provider has exactly one line, in name order (REQ-UI-129).</summary>
    [Fact(DisplayName = "REQ-UI-129 every connected provider has one line")]
    public async Task EveryProviderHasOneLine()
    {
        await AddProviderAsync("My-Claude", "Anthropic", "Key", "https://api.anthropic.com", "sk-ant");
        await AddProviderAsync("Local", "Ollama", "None", "http://localhost:11434", null);
        await AddProviderAsync("OpenRouter", "OpenAICompatible", "Key", "https://openrouter.ai/api/v1", "sk-or");
        objHandler.Answer(HttpStatusCode.OK, """{"data":{"limit":20,"usage":11.79,"limit_remaining":8.21}}""");

        var vLines = await CreateActions().ListAsync();

        Assert.Equal(["Local", "My-Claude", "OpenRouter"], vLines.Select(aLine => aLine.ProviderName).ToArray());
        Assert.All(vLines, aLine => Assert.False(string.IsNullOrWhiteSpace(aLine.Note)));
    }

    /// <summary>When a local model is connected, then its line reads "not known" because a local model has no key, and no limit can be typed for it (REQ-UI-129, REQ-FN-326).</summary>
    [Fact(DisplayName = "REQ-UI-129 a local model has no key to count")]
    public async Task LocalModelHasNoKey()
    {
        await AddProviderAsync("Local", "Ollama", "None", "http://localhost:11434", null);

        var vLine = Assert.Single(await CreateActions().ListAsync());

        Assert.Equal(KeyBalanceKind.NotKnown, vLine.Kind);
        Assert.Contains("local model has no key", vLine.Note, StringComparison.Ordinal);
        Assert.False(vLine.CanSetLimit);
        Assert.Null(vLine.Left);
    }

    /// <summary>When an OpenRouter key with a limit is asked, then its line shows the limit, the usage and what remains as the provider gave them, with the time it answered, and the key is sent as a bearer token to openrouter.ai's key address (REQ-UI-130).</summary>
    [Fact(DisplayName = "REQ-UI-130 OpenRouter answers with its limit, usage and what remains")]
    public async Task OpenRouterAnswers()
    {
        await AddProviderAsync("OpenRouter", "OpenAICompatible", "Key", "https://openrouter.ai/api/v1", "sk-or-test");
        objHandler.Answer(HttpStatusCode.OK, """{"data":{"label":"x","limit":20,"usage":11.79,"limit_remaining":8.21}}""");

        var vLine = Assert.Single(await CreateActions().ListAsync());

        Assert.Equal(KeyBalanceKind.AskedOfProvider, vLine.Kind);
        Assert.Equal(20m, vLine.LimitUsd);
        Assert.Equal(11.79m, vLine.UsedUsd);
        Assert.Equal(8.21m, vLine.Left);
        Assert.Equal("openrouter.ai answered at 12:00 today", vLine.Note);
        Assert.False(vLine.CanSetLimit);
        Assert.Equal("https://openrouter.ai/api/v1/key", objHandler.LastUrl);
        Assert.Equal("Bearer sk-or-test", objHandler.LastAuthorization);
    }

    /// <summary>When a DeepSeek key is asked, then its line shows the account's balance in US dollars, picked from the balances it lists (REQ-UI-130).</summary>
    [Fact(DisplayName = "REQ-UI-130 DeepSeek answers with the account balance")]
    public async Task DeepSeekAnswers()
    {
        await AddProviderAsync("DeepSeek", "OpenAICompatible", "Key", "https://api.deepseek.com/v1", "sk-ds");
        objHandler.Answer(HttpStatusCode.OK, """{"is_available":true,"balance_infos":[{"currency":"CNY","total_balance":"70.00"},{"currency":"USD","total_balance":"9.50","granted_balance":"0.00","topped_up_balance":"9.50"}]}""");

        var vLine = Assert.Single(await CreateActions().ListAsync());

        Assert.Equal(KeyBalanceKind.AskedOfProvider, vLine.Kind);
        Assert.Equal(9.50m, vLine.Left);
        Assert.Equal("USD", vLine.Currency);
        Assert.Null(vLine.LimitUsd);
        Assert.StartsWith("api.deepseek.com answered at 12:00 today", vLine.Note, StringComparison.Ordinal);
        Assert.Equal("https://api.deepseek.com/user/balance", objHandler.LastUrl);
    }

    /// <summary>When the balance address is recognised by the provider's web address, then OpenRouter and DeepSeek have one and any other address has none (REQ-UI-130).</summary>
    [Fact(DisplayName = "REQ-UI-130 only OpenRouter and DeepSeek have a balance address")]
    public void BalanceAddressByHost()
    {
        Assert.Equal(KeyBalanceActions.BalanceAddress.OpenRouter, KeyBalanceActions.AddressFor("https://openrouter.ai/api/v1"));
        Assert.Equal(KeyBalanceActions.BalanceAddress.DeepSeek, KeyBalanceActions.AddressFor("https://api.deepseek.com"));
        Assert.Equal(KeyBalanceActions.BalanceAddress.None, KeyBalanceActions.AddressFor("https://api.anthropic.com"));
        Assert.Equal(KeyBalanceActions.BalanceAddress.None, KeyBalanceActions.AddressFor("https://opencode.ai/zen/go/v1"));
        Assert.Equal(KeyBalanceActions.BalanceAddress.None, KeyBalanceActions.AddressFor(""));
    }

    /// <summary>When a limit of $50.00 is typed from 1 Nov 2026 and Chatur made two priced calls since then (and one before), then the line is Chatur's own count: limit $50.00, used $12.40, left $37.60, "since 1 Nov 2026 · 2 calls" (REQ-UI-131).</summary>
    [Fact(DisplayName = "REQ-UI-131 a typed limit is counted against Chatur's own priced calls")]
    public async Task TypedLimitCountsOwnCalls()
    {
        var vId = await AddProviderAsync("My-Claude", "Anthropic", "Key", "https://api.anthropic.com", "sk-ant");
        AddUsage(vId, new DateTime(2026, 10, 31, 23, 0, 0, DateTimeKind.Utc), 99.00m);
        AddUsage(vId, new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), 10.00m);
        AddUsage(vId, new DateTime(2026, 11, 20, 9, 0, 0, DateTimeKind.Utc), 2.40m);
        var vActions = CreateActions();

        await vActions.SetLimitAsync(vId, new KeyLimit(50.00m, new DateOnly(2026, 11, 1)));
        var vLine = Assert.Single(await vActions.ListAsync());

        Assert.Equal(KeyBalanceKind.OwnCount, vLine.Kind);
        Assert.Equal(50.00m, vLine.LimitUsd);
        Assert.Equal(12.40m, vLine.UsedUsd);
        Assert.Equal(37.60m, vLine.Left);
        Assert.Equal("against your limit, since 1 Nov 2026 · 2 calls", vLine.Note);
        Assert.Equal(new KeyLimit(50.00m, new DateOnly(2026, 11, 1)), vLine.Limit);
        Assert.True(vLine.CanSetLimit);
    }

    /// <summary>When the typed limit is removed, then the line falls back to "not known" with the provider's reason (REQ-UI-131, REQ-FN-326).</summary>
    [Fact(DisplayName = "REQ-UI-131 removing the limit leaves the line not known")]
    public async Task ClearingTheLimit()
    {
        var vId = await AddProviderAsync("My-Claude", "Anthropic", "Key", "https://api.anthropic.com", "sk-ant");
        var vActions = CreateActions();
        await vActions.SetLimitAsync(vId, new KeyLimit(50m, new DateOnly(2026, 11, 1)));

        await vActions.ClearLimitAsync(vId);
        var vLine = Assert.Single(await vActions.ListAsync());

        Assert.Equal(KeyBalanceKind.NotKnown, vLine.Kind);
        Assert.Null(vLine.Limit);
    }

    /// <summary>When the Limit dialog's fields are checked, then an amount above 0 with up to two decimals and a day not after today pass, and anything else says why (REQ-UI-131).</summary>
    [Fact(DisplayName = "REQ-UI-131 the limit must be above 0 with two decimals and not start after today")]
    public void LimitFieldsAreChecked()
    {
        var vActions = CreateActions();
        var vToday = new DateOnly(2026, 11, 27);

        Assert.True(vActions.CheckLimit("50.00", vToday).IsValid);
        Assert.Equal(12.5m, vActions.CheckLimit(" 12.5 ", new DateOnly(2026, 11, 1)).LimitUsd);
        Assert.NotNull(vActions.CheckLimit("0", vToday).AmountError);
        Assert.NotNull(vActions.CheckLimit("0.00", vToday).AmountError);
        Assert.NotNull(vActions.CheckLimit("1.234", vToday).AmountError);
        Assert.NotNull(vActions.CheckLimit("-5", vToday).AmountError);
        Assert.NotNull(vActions.CheckLimit("ten", vToday).AmountError);
        Assert.NotNull(vActions.CheckLimit("", vToday).AmountError);
        Assert.NotNull(vActions.CheckLimit("50", new DateOnly(2026, 11, 28)).FromError);
        Assert.NotNull(vActions.CheckLimit("50", null).FromError);
        Assert.False(vActions.CheckLimit("50", new DateOnly(2026, 11, 28)).IsValid);
    }

    /// <summary>When a limit that fails the check is saved anyway, then the action refuses it and nothing is stored (REQ-UI-131).</summary>
    [Fact(DisplayName = "REQ-UI-131 a limit starting after today is refused")]
    public async Task FutureLimitRefused()
    {
        var vId = await AddProviderAsync("My-Claude", "Anthropic", "Key", "https://api.anthropic.com", "sk-ant");
        var vActions = CreateActions();

        await Assert.ThrowsAsync<ArgumentException>(() => vActions.SetLimitAsync(vId, new KeyLimit(50m, new DateOnly(2026, 12, 1))));
        await Assert.ThrowsAsync<ArgumentException>(() => vActions.SetLimitAsync(vId, new KeyLimit(0m, new DateOnly(2026, 11, 1))));

        Assert.Null(Assert.Single(await vActions.ListAsync()).Limit);
    }

    /// <summary>When a call Chatur made is recorded, then its row carries the provider, model, tokens and TechieRag's price, or no price when TechieRag has none or the tokens were estimated (REQ-UI-131).</summary>
    [Fact(DisplayName = "REQ-UI-131 each call is recorded with TechieRag's price or none")]
    public async Task CallsAreRecordedWithTechieRagsPrice()
    {
        var vId = await AddProviderAsync("My-OpenAI", "OpenAI", "Key", "https://api.openai.com/v1", "sk-oa");
        var vRecorder = new KeyUsageRecorder(CreateFactory(), new FixedClock(NowUtc));

        vRecorder.Record(vId, "gpt-4o", 1_000_000, 0, false);
        vRecorder.Record(vId, "a-model-techierag-does-not-know", 1000, 500, false);
        vRecorder.Record(vId, "gpt-4o", 1000, 500, true);

        var vRows = ReadUsage(vId);
        Assert.Equal(3, vRows.Count);
        Assert.True(vRows[0].Cost is > 0d, "TechieRag prices gpt-4o");
        Assert.Equal(1_000_000, vRows[0].TokensIn);
        Assert.Equal("2026-11-27 12:00:00", vRows[0].CreatedUtc);
        Assert.Null(vRows[1].Cost);
        Assert.Null(vRows[2].Cost);
    }

    /// <summary>When a TechieRag provider Chatur built completes a call, then the watch records it against the provider without the caller doing anything more (REQ-UI-131).</summary>
    [Fact(DisplayName = "REQ-UI-131 a watched provider's completed call is recorded")]
    public async Task WatchedProviderIsRecorded()
    {
        var vId = await AddProviderAsync("My-OpenAI", "OpenAI", "Key", "https://api.openai.com/v1", "sk-oa");
        var vRecorder = new KeyUsageRecorder(CreateFactory(), new FixedClock(NowUtc));
        var vProvider = new RaisingProvider();

        vRecorder.Watch(vProvider, vId, "gpt-4o");
        vProvider.Complete(120, 30);

        var vRow = Assert.Single(ReadUsage(vId));
        Assert.Equal("gpt-4o", vRow.ModelIdentifier);
        Assert.Equal(120, vRow.TokensIn);
        Assert.Equal(30, vRow.TokensOut);
    }

    /// <summary>When a key has no balance address and no limit is typed, then its line reads "not known" and says why in the provider's terms (REQ-FN-326).</summary>
    [Fact(DisplayName = "REQ-FN-326 no address and no limit reads not known and says why")]
    public async Task NoAddressNoLimitIsNotKnown()
    {
        await AddProviderAsync("My-OpenAI", "OpenAI", "Key", "https://api.openai.com/v1", "sk-oa");

        var vLine = Assert.Single(await CreateActions().ListAsync());

        Assert.Equal(KeyBalanceKind.NotKnown, vLine.Kind);
        Assert.Null(vLine.Left);
        Assert.Null(vLine.UsedUsd);
        Assert.Equal("OpenAI gives no balance to a normal key, and no limit is typed", vLine.Note);
        Assert.Null(objHandler.LastUrl);
    }

    /// <summary>When the balance address answers 401, then the line reads "not known" with the status, never a number, and never names the key (REQ-FN-326).</summary>
    [Fact(DisplayName = "REQ-FN-326 an address that refuses reads not known with the reason")]
    public async Task RefusedAddressIsNotKnown()
    {
        await AddProviderAsync("OpenRouter", "OpenAICompatible", "Key", "https://openrouter.ai/api/v1", "sk-or-secret");
        objHandler.Answer(HttpStatusCode.Unauthorized, """{"error":{"message":"No auth credentials found"}}""");

        var vLine = Assert.Single(await CreateActions().ListAsync());

        Assert.Equal(KeyBalanceKind.NotKnown, vLine.Kind);
        Assert.Null(vLine.Left);
        Assert.Equal("openrouter.ai did not answer: it said 401 Unauthorized", vLine.Note);
        Assert.DoesNotContain("sk-or-secret", vLine.Note, StringComparison.Ordinal);
    }

    /// <summary>When the balance address answers with something that is not its JSON, then the line reads "not known" because the answer could not be read (REQ-FN-326).</summary>
    [Fact(DisplayName = "REQ-FN-326 an unreadable answer reads not known")]
    public async Task UnreadableAnswerIsNotKnown()
    {
        await AddProviderAsync("DeepSeek", "OpenAICompatible", "Key", "https://api.deepseek.com", "sk-ds");
        objHandler.Answer(HttpStatusCode.OK, "<html>maintenance</html>");

        var vLine = Assert.Single(await CreateActions().ListAsync());

        Assert.Equal(KeyBalanceKind.NotKnown, vLine.Kind);
        Assert.Equal("api.deepseek.com did not answer: its answer could not be read", vLine.Note);
    }

    /// <summary>When the balance address cannot be reached, then the line reads "not known" and says so (REQ-FN-326).</summary>
    [Fact(DisplayName = "REQ-FN-326 an address that cannot be reached reads not known")]
    public async Task UnreachableAddressIsNotKnown()
    {
        await AddProviderAsync("OpenRouter", "OpenAICompatible", "Key", "https://openrouter.ai/api/v1", "sk-or");
        objHandler.Fail(new HttpRequestException("No such host is known."));

        var vLine = Assert.Single(await CreateActions().ListAsync());

        Assert.Equal(KeyBalanceKind.NotKnown, vLine.Kind);
        Assert.Equal("openrouter.ai did not answer: it could not be reached", vLine.Note);
    }

    /// <summary>When the balance address does not answer in time, then the line reads "not known" and says it did not answer within the time allowed (REQ-FN-326).</summary>
    [Fact(DisplayName = "REQ-FN-326 an address that times out reads not known")]
    public async Task TimedOutAddressIsNotKnown()
    {
        await AddProviderAsync("OpenRouter", "OpenAICompatible", "Key", "https://openrouter.ai/api/v1", "sk-or");
        objHandler.Fail(new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout."));

        var vLine = Assert.Single(await CreateActions().ListAsync());

        Assert.Equal(KeyBalanceKind.NotKnown, vLine.Kind);
        Assert.Equal("openrouter.ai did not answer within 15 seconds", vLine.Note);
    }

    /// <summary>When an OpenRouter key has no limit of its own, then what is left is "not known" and the note says the key has no limit, while the usage it gave is shown (REQ-FN-326).</summary>
    [Fact(DisplayName = "REQ-FN-326 an OpenRouter key with no limit leaves what is left not known")]
    public async Task OpenRouterKeyWithoutLimit()
    {
        await AddProviderAsync("OpenRouter", "OpenAICompatible", "Key", "https://openrouter.ai/api/v1", "sk-or");
        objHandler.Answer(HttpStatusCode.OK, """{"data":{"limit":null,"usage":3.5,"limit_remaining":null}}""");

        var vLine = Assert.Single(await CreateActions().ListAsync());

        Assert.Equal(KeyBalanceKind.NotKnown, vLine.Kind);
        Assert.Null(vLine.Left);
        Assert.Equal(3.5m, vLine.UsedUsd);
        Assert.Contains("this key has no limit", vLine.Note, StringComparison.Ordinal);
    }

    /// <summary>When a call since the day counted from had no price, then the line reads "not known" and says how many calls had no price, with the limit still shown (REQ-FN-326, REQ-UI-131).</summary>
    [Fact(DisplayName = "REQ-FN-326 calls with no price make the own count not known")]
    public async Task UnpricedCallsMakeItNotKnown()
    {
        var vId = await AddProviderAsync("My-Claude", "Anthropic", "Key", "https://api.anthropic.com", "sk-ant");
        AddUsage(vId, new DateTime(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc), 1.00m);
        AddUsage(vId, new DateTime(2026, 11, 3, 9, 0, 0, DateTimeKind.Utc), null);
        var vActions = CreateActions();
        await vActions.SetLimitAsync(vId, new KeyLimit(50m, new DateOnly(2026, 11, 1)));

        var vLine = Assert.Single(await vActions.ListAsync());

        Assert.Equal(KeyBalanceKind.NotKnown, vLine.Kind);
        Assert.Equal(50m, vLine.LimitUsd);
        Assert.Null(vLine.UsedUsd);
        Assert.Null(vLine.Left);
        Assert.Equal("1 of 2 calls since 1 Nov 2026 had no price from TechieRag, so what is used is not known", vLine.Note);
    }

    /// <summary>When a provider's key is not in the secret store, then its balance address is not asked and the line says why (REQ-FN-326).</summary>
    [Fact(DisplayName = "REQ-FN-326 a key missing from the secret store is not asked")]
    public async Task MissingKeyIsNotAsked()
    {
        await AddProviderAsync("OpenRouter", "OpenAICompatible", "Key", "https://openrouter.ai/api/v1", null);

        var vLine = Assert.Single(await CreateActions().ListAsync());

        Assert.Equal(KeyBalanceKind.NotKnown, vLine.Kind);
        Assert.Contains("not in this computer's secret store", vLine.Note, StringComparison.Ordinal);
        Assert.Null(objHandler.LastUrl);
    }

    private KeyBalanceActions CreateActions() =>
        new(CreateFactory(), objSecrets, new FakeHttpClientFactory(objHandler), new FixedClock(NowUtc), TimeZoneInfo.Utc);

    private async Task<int> AddProviderAsync(string aName, string aConnector, string aSignIn, string aBaseUrl, string? aKey)
    {
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText = "INSERT INTO Provider (Name, Connector, SignInMethod, BaseUrl, SecretName, State) VALUES (@Name, @Connector, @SignIn, @BaseUrl, @SecretName, 'Connected'); SELECT last_insert_rowid();";
        vCommand.Parameters.AddWithValue("@Name", aName);
        vCommand.Parameters.AddWithValue("@Connector", aConnector);
        vCommand.Parameters.AddWithValue("@SignIn", aSignIn);
        vCommand.Parameters.AddWithValue("@BaseUrl", aBaseUrl);
        vCommand.Parameters.AddWithValue("@SecretName", aKey is null ? (object)$"missing.{aName}" : $"test.{aName}");
        var vId = (int)(long)vCommand.ExecuteScalar()!;
        if (aKey is not null)
        {
            await objSecrets.SaveAsync($"test.{aName}", aKey);
        }

        return vId;
    }

    private void AddUsage(int aProviderId, DateTime aUtc, decimal? aCost)
    {
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText = "INSERT INTO KeyUsage (ProviderId, ModelIdentifier, CreatedUtc, TokensIn, TokensOut, CostUsd) VALUES (@P, 'claude-sonnet-5-5', @At, 100, 50, @Cost);";
        vCommand.Parameters.AddWithValue("@P", aProviderId);
        vCommand.Parameters.AddWithValue("@At", KeyUsageTime.ToText(aUtc));
        vCommand.Parameters.AddWithValue("@Cost", aCost is { } vCost ? vCost : DBNull.Value);
        vCommand.ExecuteNonQuery();
    }

    private List<(string ModelIdentifier, string CreatedUtc, long TokensIn, long TokensOut, double? Cost)> ReadUsage(int aProviderId)
    {
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText = "SELECT ModelIdentifier, CreatedUtc, TokensIn, TokensOut, CostUsd FROM KeyUsage WHERE ProviderId = @P ORDER BY KeyUsageId;";
        vCommand.Parameters.AddWithValue("@P", aProviderId);
        using var vReader = vCommand.ExecuteReader();
        var vRows = new List<(string, string, long, long, double?)>();
        while (vReader.Read())
        {
            vRows.Add((vReader.GetString(0), vReader.GetString(1), vReader.GetInt64(2), vReader.GetInt64(3), vReader.IsDBNull(4) ? null : vReader.GetDouble(4)));
        }

        return vRows;
    }

    private sealed class FixedClock(DateTime aUtcNow) : IClock
    {
        public DateTime UtcNow { get; } = aUtcNow;
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler aHandler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(aHandler, disposeHandler: false);
    }

    /// <summary>Answers every request with one canned answer, or fails it, and remembers the last address and authorization.</summary>
    private sealed class FakeHandler : HttpMessageHandler
    {
        private HttpStatusCode objStatus = HttpStatusCode.OK;
        private string objBody = "{}";
        private Exception? objFailure;

        public string? LastUrl { get; private set; }

        public string? LastAuthorization { get; private set; }

        public void Answer(HttpStatusCode aStatus, string aBody)
        {
            objStatus = aStatus;
            objBody = aBody;
            objFailure = null;
        }

        public void Fail(Exception aFailure) => objFailure = aFailure;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage aRequest, CancellationToken aCt)
        {
            LastUrl = aRequest.RequestUri?.ToString();
            LastAuthorization = aRequest.Headers.Authorization?.ToString();
            if (objFailure is not null)
            {
                return Task.FromException<HttpResponseMessage>(objFailure);
            }

            return Task.FromResult(new HttpResponseMessage(objStatus) { Content = new StringContent(objBody, Encoding.UTF8, "application/json") });
        }
    }

    /// <summary>A TechieRag provider that only raises its completion event, as a real one does after each call.</summary>
    private sealed class RaisingProvider : ILlmProvider
    {
        public string Name => "Raising";

        public string ModelName => "gpt-4o";

        public bool SupportsToolCalling => false;

        public bool SupportsStreaming => false;

        public event EventHandler<LlmCompletionEventArgs>? OnCompletionCompleted;

        public void Complete(int aIn, int aOut) =>
            OnCompletionCompleted?.Invoke(this, new LlmCompletionEventArgs { InputTokens = aIn, OutputTokens = aOut, Duration = TimeSpan.FromSeconds(1), ModelName = "gpt-4o", ProviderName = "OpenAI" });

        public Task<LlmResponse> CompleteAsync(string prompt, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<string> CompleteStreamAsync(string prompt, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<LlmResponse> ChatAsync(IReadOnlyList<ChatMessage> messages, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<string> ChatStreamAsync(IReadOnlyList<ChatMessage> messages, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<T> CompleteAsync<T>(string prompt, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();

        public int EstimateTokenCount(string text) => text.Length / 4;
    }
}
