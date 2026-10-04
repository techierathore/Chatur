using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;
using TechieRag.Llm;

namespace Chatur.Core.Models;

/// <summary>
/// <see cref="IProviderActions"/> over the <c>Provider</c> and <c>Model</c> tables and
/// <see cref="ISecretStore"/> (Architecture §7 "Models"). Every secret is written to the operating
/// system's own store; the database keeps only the name it is filed under (REQ-FN-017).
/// </summary>
public sealed class ProviderActions : IProviderActions
{
    /// <summary>The connectors that sign in with a pasted key, and the models Chatur offers for each,
    /// with a default tier (1 = strongest). A day-1 catalog, not a live fetch — TechieRag has no
    /// "list the provider's models" call (<c>.techierag/TechieRag-AI-Reference.md</c>), so Chatur
    /// seeds the models its own routing already knows how to use.</summary>
    private static readonly IReadOnlyDictionary<string, (string Identifier, int Tier)[]> ModelCatalogByConnector =
        new Dictionary<string, (string, int)[]>
        {
            ["Anthropic"] = new[] { ("claude-opus-5", 1), ("claude-sonnet-5", 2), ("claude-haiku-5", 3) },
            ["OpenAI"] = new[] { ("gpt-5", 1), ("gpt-5-mini", 2), ("gpt-5-nano", 3) },
            ["Google"] = new[] { ("gemini-2.5-pro", 1), ("gemini-2.5-flash", 2), ("gemini-2.5-flash-lite", 3) }
        };

    /// <summary>The connector for any service that speaks the OpenAI shape at its own web address.</summary>
    private const string CompatibleConnector = "OpenAICompatible";

    /// <summary>The connector name of a ChatGPT subscription provider (REQ-FN-016).</summary>
    public const string ChatGptSubscriptionConnector = "ChatGptSubscription";

    /// <summary>
    /// TechieRag's catalog key for the ChatGPT subscription connector (<c>LlmConnectorCatalog</c>) — what
    /// its sign-in session is filed under. Public since TechieRag 1.0.9 (TR-RAG-004 closed).
    /// </summary>
    internal const string ChatGptCatalogName = LlmConnectorCatalog.ChatGptSubscriptionName;

    /// <summary>The <c>SignInMethod</c> of a provider that signs in through a vendor's subscription flow.</summary>
    private const string SubscriptionSignInMethod = "Subscription";

    private readonly IDbConnectionFactory objDb;
    private readonly ISecretStore objSecrets;
    private readonly ILlmConnectionProbe objProbe;
    private readonly ISubscriptionSignIn objSubscriptionSignIn;

    /// <summary>Creates the action implementation.</summary>
    /// <param name="aDb">Opens connections to Chatur's own database.</param>
    /// <param name="aSecrets">The operating system's own secret store.</param>
    /// <param name="aProbe">Calls a provider once, through TechieRag.</param>
    /// <param name="aSubscriptionSignIn">Runs a subscription's device-code sign-in through TechieRag.</param>
    public ProviderActions(IDbConnectionFactory aDb, ISecretStore aSecrets, ILlmConnectionProbe aProbe, ISubscriptionSignIn aSubscriptionSignIn)
    {
        objDb = aDb;
        objSecrets = aSecrets;
        objProbe = aProbe;
        objSubscriptionSignIn = aSubscriptionSignIn;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModelProvider>> ListAsync(CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        var vCommand = new CommandDefinition(
            "SELECT ProviderId, Name, Connector, SignInMethod, BaseUrl, State FROM Provider ORDER BY Name;",
            cancellationToken: aCt);
        var vRows = await vConnection.QueryAsync<ProviderRow>(vCommand).ConfigureAwait(false);
        return vRows.Select(ToModelProvider).ToList();
    }

    /// <inheritdoc />
    public async Task<ModelProvider> AddWithKeyAsync(string aName, string aConnector, string aBaseUrl, string aKey, CancellationToken aCt = default)
    {
        if (string.IsNullOrWhiteSpace(aName))
        {
            throw new ArgumentException("A provider needs a name.", nameof(aName));
        }

        var vIsCompatible = aConnector == CompatibleConnector;
        if (!vIsCompatible && !ModelCatalogByConnector.ContainsKey(aConnector))
        {
            throw new ArgumentException($"Chatur does not know the connector \"{aConnector}\".", nameof(aConnector));
        }

        if (string.IsNullOrWhiteSpace(aKey))
        {
            throw new ArgumentException("A pasted key is required.", nameof(aKey));
        }

        // An OpenAI-compatible service has no catalog Chatur could know in advance: its own model
        // list is read first, and the add fails — before anything is saved — when it cannot be.
        var vCatalog = vIsCompatible
            ? await ReadCompatibleCatalogAsync(aBaseUrl, aKey, aCt).ConfigureAwait(false)
            : ModelCatalogByConnector[aConnector];

        using var vConnection = objDb.OpenConnection();
        var vInsert = new CommandDefinition(
            """
            INSERT INTO Provider (Name, Connector, SignInMethod, BaseUrl, SecretName, State)
            VALUES (@aName, @aConnector, 'Key', @aBaseUrl, NULL, 'Connected');
            SELECT last_insert_rowid();
            """,
            new { aName, aConnector, aBaseUrl },
            cancellationToken: aCt);
        var vProviderId = await vConnection.ExecuteScalarAsync<long>(vInsert).ConfigureAwait(false);

        var vSecretName = $"chatur.provider.{vProviderId}";
        await objSecrets.SaveAsync(vSecretName, aKey, aCt).ConfigureAwait(false);

        var vUpdate = new CommandDefinition(
            "UPDATE Provider SET SecretName = @vSecretName WHERE ProviderId = @vProviderId;",
            new { vSecretName, vProviderId },
            cancellationToken: aCt);
        await vConnection.ExecuteAsync(vUpdate).ConfigureAwait(false);

        await SeedCatalogModelsAsync(vConnection, vProviderId, vCatalog, aCt).ConfigureAwait(false);

        return new ModelProvider((int)vProviderId, aName, aConnector, "Key", aBaseUrl, ProviderState.Connected);
    }

    /// <inheritdoc />
    public async Task<ModelProvider> AddLocalAsync(string aName, string aBaseUrl, CancellationToken aCt = default)
    {
        if (string.IsNullOrWhiteSpace(aName))
        {
            throw new ArgumentException("A local model needs a name.", nameof(aName));
        }

        if (string.IsNullOrWhiteSpace(aBaseUrl))
        {
            throw new ArgumentException("A local model needs its server's web address.", nameof(aBaseUrl));
        }

        using var vConnection = objDb.OpenConnection();
        var vInsert = new CommandDefinition(
            """
            INSERT INTO Provider (Name, Connector, SignInMethod, BaseUrl, SecretName, State)
            VALUES (@aName, 'Ollama', 'None', @aBaseUrl, NULL, 'Connected');
            SELECT last_insert_rowid();
            """,
            new { aName, aBaseUrl },
            cancellationToken: aCt);
        var vProviderId = await vConnection.ExecuteScalarAsync<long>(vInsert).ConfigureAwait(false);

        await SeedCatalogModelsAsync(vConnection, vProviderId, new[] { (aName, 3) }, aCt).ConfigureAwait(false);

        return new ModelProvider((int)vProviderId, aName, "Ollama", "None", aBaseUrl, ProviderState.Connected);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Signs in first and writes the <c>Provider</c> row only once the sign-in has succeeded, so a
    /// cancelled, expired or refused sign-in leaves nothing behind. The session itself is filed by
    /// TechieRag through <see cref="SecretSubscriptionSessionStore"/> under
    /// <see cref="SecretSubscriptionSessionStore.SecretNameFor"/>; the row keeps only that name.
    /// </remarks>
    public async Task<ModelProvider> AddSubscriptionAsync(
        string aName,
        string aConnector,
        Func<SignInCodePrompt, CancellationToken, Task> aOnSignInCode,
        CancellationToken aCt = default)
    {
        if (string.IsNullOrWhiteSpace(aName))
        {
            throw new ArgumentException("A provider needs a name.", nameof(aName));
        }

        if (aConnector != ChatGptSubscriptionConnector)
        {
            throw new ArgumentException($"Chatur has no subscription sign-in for \"{aConnector}\".", nameof(aConnector));
        }

        ArgumentNullException.ThrowIfNull(aOnSignInCode);

        var vModelIdentifier = new ChatGptSubscriptionOptions().Model;
        await objSubscriptionSignIn.SignInAsync(vModelIdentifier, aOnSignInCode, aCt).ConfigureAwait(false);

        var vSecretName = SecretSubscriptionSessionStore.SecretNameFor(ChatGptCatalogName);
        using var vConnection = objDb.OpenConnection();

        var vExisting = await vConnection.QuerySingleOrDefaultAsync<ProviderRow>(
            new CommandDefinition(
                "SELECT ProviderId, Name, Connector, SignInMethod, BaseUrl, SecretName, State FROM Provider WHERE Connector = @aConnector AND SignInMethod = @SubscriptionSignInMethod;",
                new { aConnector, SubscriptionSignInMethod },
                cancellationToken: aCt)).ConfigureAwait(false);
        if (vExisting is not null)
        {
            await vConnection.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE Provider SET State = 'Connected' WHERE ProviderId = @ProviderId;",
                    new { vExisting.ProviderId },
                    cancellationToken: aCt)).ConfigureAwait(false);
            return new ModelProvider((int)vExisting.ProviderId, vExisting.Name, aConnector, SubscriptionSignInMethod, vExisting.BaseUrl, ProviderState.Connected);
        }

        var vInsert = new CommandDefinition(
            """
            INSERT INTO Provider (Name, Connector, SignInMethod, BaseUrl, SecretName, State)
            VALUES (@aName, @aConnector, @SubscriptionSignInMethod, '', @vSecretName, 'Connected');
            SELECT last_insert_rowid();
            """,
            new { aName, aConnector, SubscriptionSignInMethod, vSecretName },
            cancellationToken: aCt);
        var vProviderId = await vConnection.ExecuteScalarAsync<long>(vInsert).ConfigureAwait(false);

        await SeedCatalogModelsAsync(vConnection, vProviderId, new[] { (vModelIdentifier, 2) }, aCt).ConfigureAwait(false);

        return new ModelProvider((int)vProviderId, aName, aConnector, SubscriptionSignInMethod, string.Empty, ProviderState.Connected);
    }

    /// <inheritdoc />
    public SubscriptionTermsInfo? SubscriptionTerms(string aConnector)
    {
        if (aConnector != ChatGptSubscriptionConnector)
        {
            return null;
        }

        var vTerms = LlmConnectorCatalog.Find(ChatGptCatalogName)?.Subscription;
        return vTerms is null
            ? null
            : new SubscriptionTermsInfo(vTerms.Permitted, vTerms.Terms, vTerms.AppliesTo, vTerms.CheckedOn);
    }

    /// <inheritdoc />
    public async Task<ProviderTestResult> TestAsync(int aProviderId, CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        var vRowCommand = new CommandDefinition(
            "SELECT ProviderId, Name, Connector, SignInMethod, BaseUrl, SecretName, State FROM Provider WHERE ProviderId = @aProviderId;",
            new { aProviderId },
            cancellationToken: aCt);
        var vRow = await vConnection.QuerySingleOrDefaultAsync<ProviderRow>(vRowCommand).ConfigureAwait(false);
        if (vRow is null)
        {
            throw new KeyNotFoundException($"Provider {aProviderId} does not exist.");
        }

        string? vSecret = vRow.SecretName is null
            ? null
            : await objSecrets.ReadAsync(vRow.SecretName, aCt).ConfigureAwait(false);

        // A subscription's "secret" is the signed-in session, not a key a probe could send: it is
        // tested by whether the owner is still signed in, without spending a model call.
        var vResult = vRow.SignInMethod == SubscriptionSignInMethod
            ? (string.IsNullOrEmpty(vSecret)
                ? new ProviderTestResult(false, "Not signed in. Sign in again.", Array.Empty<string>())
                : new ProviderTestResult(true, null, Array.Empty<string>()))
            : await objProbe.ProbeAsync(vRow.Connector, vRow.BaseUrl, vSecret, aCt).ConfigureAwait(false);

        var vUpdate = new CommandDefinition(
            "UPDATE Provider SET State = @vState WHERE ProviderId = @aProviderId;",
            new { vState = vResult.Succeeded ? "Connected" : "Failed", aProviderId },
            cancellationToken: aCt);
        await vConnection.ExecuteAsync(vUpdate).ConfigureAwait(false);

        return vResult;
    }

    /// <inheritdoc />
    public Task<ProviderTestResult> TestUnsavedAsync(string aConnector, string aBaseUrl, string? aKey, CancellationToken aCt = default) =>
        objProbe.ProbeAsync(aConnector, aBaseUrl, aKey, aCt);

    /// <inheritdoc />
    public async Task RemoveAsync(int aProviderId, CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        var vRowCommand = new CommandDefinition(
            "SELECT SecretName FROM Provider WHERE ProviderId = @aProviderId;",
            new { aProviderId },
            cancellationToken: aCt);
        var vSecretName = await vConnection.QuerySingleOrDefaultAsync<string?>(vRowCommand).ConfigureAwait(false);

        var vDeleteModels = new CommandDefinition(
            "DELETE FROM Model WHERE ProviderId = @aProviderId;",
            new { aProviderId },
            cancellationToken: aCt);
        await vConnection.ExecuteAsync(vDeleteModels).ConfigureAwait(false);

        var vDeleteProvider = new CommandDefinition(
            "DELETE FROM Provider WHERE ProviderId = @aProviderId;",
            new { aProviderId },
            cancellationToken: aCt);
        await vConnection.ExecuteAsync(vDeleteProvider).ConfigureAwait(false);

        if (vSecretName is not null)
        {
            await objSecrets.DeleteAsync(vSecretName, aCt).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModelSummary>> ListModelsAsync(CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        var vCommand = new CommandDefinition(
            """
            SELECT m.ModelId AS ModelId, m.ProviderId AS ProviderId, p.Name AS ProviderName,
                   m.Tier AS Tier, m.Identifier AS Identifier
            FROM Model m
            JOIN Provider p ON p.ProviderId = m.ProviderId
            ORDER BY p.Name, m.Tier, m.ModelId;
            """,
            cancellationToken: aCt);
        var vRows = await vConnection.QueryAsync<ModelRow>(vCommand).ConfigureAwait(false);
        return vRows.Select(aRow => new ModelSummary((int)aRow.ModelId, (int)aRow.ProviderId, aRow.ProviderName, (int)aRow.Tier, aRow.Identifier)).ToList();
    }

    /// <summary>
    /// Reads an OpenAI-compatible service's models through the probe; each is seeded at the middle
    /// tier, and the owner places them in Settings ▸ Routing.
    /// </summary>
    /// <exception cref="ArgumentException">The service did not answer or listed no models.</exception>
    private async Task<(string Identifier, int Tier)[]> ReadCompatibleCatalogAsync(string aBaseUrl, string aKey, CancellationToken aCt)
    {
        var vResult = await objProbe.ProbeAsync(CompatibleConnector, aBaseUrl, aKey, aCt).ConfigureAwait(false);
        if (!vResult.Succeeded)
        {
            throw new ArgumentException($"The provider did not answer: {vResult.Message}", nameof(aBaseUrl));
        }

        return vResult.Models.Select(aId => (aId, 2)).ToArray();
    }

    private static async Task SeedCatalogModelsAsync(
        System.Data.IDbConnection aConnection, long aProviderId, (string Identifier, int Tier)[] aCatalog, CancellationToken aCt)
    {
        var vParams = aCatalog.Select(aModel => new { aProviderId, aModel.Tier, aModel.Identifier });
        var vCommand = new CommandDefinition(
            "INSERT INTO Model (ProviderId, Tier, Identifier) VALUES (@aProviderId, @Tier, @Identifier);",
            vParams,
            cancellationToken: aCt);
        await aConnection.ExecuteAsync(vCommand).ConfigureAwait(false);
    }

    private static ModelProvider ToModelProvider(ProviderRow aRow) =>
        new((int)aRow.ProviderId, aRow.Name, aRow.Connector, aRow.SignInMethod, aRow.BaseUrl, Enum.Parse<ProviderState>(aRow.State));

    /// <summary>
    /// The <c>Provider</c> table's own shape, read by Dapper before it becomes a
    /// <see cref="ModelProvider"/>. <c>ProviderId</c> is <see cref="long"/>, not <see cref="int"/>:
    /// Microsoft.Data.Sqlite always returns an INTEGER column as <see cref="long"/>, and Dapper
    /// narrows it exactly, so an <see cref="int"/> property here throws at read time.
    /// </summary>
    private sealed class ProviderRow
    {
        public long ProviderId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Connector { get; set; } = string.Empty;

        public string SignInMethod { get; set; } = string.Empty;

        public string BaseUrl { get; set; } = string.Empty;

        public string? SecretName { get; set; }

        public string State { get; set; } = string.Empty;
    }

    /// <summary>
    /// A joined <c>Model</c>/<c>Provider</c> row exactly as SQLite returns it: an INTEGER column
    /// always comes back as <see cref="long"/> from Microsoft.Data.Sqlite, and Dapper's
    /// constructor-based materialization for a record with no parameterless constructor — which
    /// <see cref="ModelSummary"/> is — demands an exact type match, so it is read into this
    /// <see cref="long"/>-typed shape first and narrowed to <see cref="int"/> on purpose afterwards.
    /// </summary>
    private sealed record ModelRow(long ModelId, long ProviderId, string ProviderName, long Tier, string Identifier);
}
