using Chatur.Core.Agent;
using Chatur.Core.Platform;
using Chatur.Core.Routing;
using Chatur.Tests.Support;
using Dapper;
using TechieRag.Abstractions;
using TechieRag.Llm;
using TechieRag.Models;
using Xunit;

namespace Chatur.Tests.Routing;

/// <summary>Tests for <see cref="AutoRouter"/> and the automatic-routing switch, against a real, migrated, temporary database and a fake chooser.</summary>
public sealed class AutoRouterTests : MigratedDatabaseFixture
{
    /// <summary>
    /// REQ-FN-071: when automatic routing is on, then the model the small model chose is the model given to the work,
    /// the chooser is offered every connected model in the tier chains with a note, and the choice (model, source, reason) is logged.
    /// </summary>
    [Fact]
    public async Task RoutingOnUsesTheChoosersPickAndLogsIt()
    {
        var vDeep = AddModel(1, "deep-model");
        AddModel(3, "fast-model");
        var vChooser = new FakeChooser(aCandidates => new ModelChoice(aCandidates.Single(aC => aC.ModelId == "deep-model"), "A hard question.", ModelChoiceSource.SmallModel, null));
        var vRouting = NewRouting();
        await vRouting.SetAutoRoutingAsync(true);
        var vSut = NewSut(vRouting, vChooser);

        var vPicked = await vSut.ChooseAsync("Refactor the whole module.", 7, 1);

        Assert.NotNull(vPicked);
        Assert.Equal(vDeep, vPicked.ModelId);
        Assert.Equal(1, vChooser.Calls);
        Assert.Equal(["deep-model", "fast-model"], vChooser.Offered.Select(aC => aC.ModelId).ToArray());
        Assert.All(vChooser.Offered, aC => Assert.False(string.IsNullOrWhiteSpace(aC.Notes)));
        var vLog = ReadLog().Single();
        Assert.Equal(("deep-model", "SmallModel", "A hard question.", 7L), (vLog.ModelIdentifier, vLog.Source, vLog.Reason, vLog.SessionId));
    }

    /// <summary>
    /// REQ-UI-111: when automatic routing is on, then the decision carries the model and, in a few words, why — what the
    /// small model said, cut to its first sentence and led by "Automatic:" — so the reason can be stored with the reply.
    /// </summary>
    [Fact]
    public async Task DecisionCarriesTheModelAndAShortReason()
    {
        AddModel(1, "deep-model");
        AddModel(3, "fast-model");
        var vChooser = new FakeChooser(aCandidates => new ModelChoice(aCandidates.Single(aC => aC.ModelId == "fast-model"), "A short question needs the smallest model. Nothing more is needed.", ModelChoiceSource.SmallModel, null));
        var vRouting = NewRouting();
        await vRouting.SetAutoRoutingAsync(true);
        var vSut = NewSut(vRouting, vChooser);

        var vDecision = await vSut.DecideAsync("What is this?", 5, 1);

        Assert.NotNull(vDecision);
        Assert.Equal("fast-model", vDecision.Model!.ModelIdentifier);
        Assert.Equal("Automatic: A short question needs the smallest model", vDecision.Reason);
    }

    /// <summary>REQ-UI-111: when automatic routing is off, then there is no decision and so no reason to show.</summary>
    [Fact]
    public async Task NoDecisionWhenRoutingIsOff()
    {
        AddModel(1, "deep-model");
        AddModel(3, "fast-model");
        var vSut = NewSut(NewRouting(), new FakeChooser(aCandidates => OnlyChoice(aCandidates[0])));

        Assert.Null(await vSut.DecideAsync("Hello", 1, 1));
    }

    /// <summary>
    /// REQ-UI-111: when the small model fails, then the decision has no model and says the tiers decided, so a reply
    /// routed that way still says why.
    /// </summary>
    [Fact]
    public async Task DecisionSaysTheTiersDecidedWhenTheSmallModelFails()
    {
        AddModel(1, "deep-model");
        AddModel(3, "fast-model");
        var vRouting = NewRouting();
        await vRouting.SetAutoRoutingAsync(true);
        var vSut = NewSut(vRouting, new FakeChooser(_ => throw new HttpRequestException("down")));

        var vDecision = await vSut.DecideAsync("Hello", 1, 1);

        Assert.NotNull(vDecision);
        Assert.Null(vDecision.Model);
        Assert.Equal(AutoRouter.TiersDecidedReason, vDecision.Reason);
    }

    /// <summary>REQ-UI-111: the reason is plain words for each way a choice is reached, and long answers are cut short.</summary>
    [Fact]
    public void ReasonForReadsInPlainWords()
    {
        Assert.Equal("Automatic: only one model is connected", AutoRouter.ReasonFor(ModelChoiceSource.OnlyCandidate, "Only one.", 1));
        Assert.Equal("Automatic: the small model gave no clear answer, so the first model was used", AutoRouter.ReasonFor(ModelChoiceSource.Fallback, "?", 3));
        Assert.Equal("Automatic: chosen by the small model", AutoRouter.ReasonFor(ModelChoiceSource.SmallModel, "  ", 3));
        var vLong = AutoRouter.ReasonFor(ModelChoiceSource.SmallModel, new string('x', 400), 3);
        Assert.True(vLong.Length <= "Automatic: ".Length + 90);
        Assert.EndsWith("…", vLong);
    }

    /// <summary>The small model that does the choosing is a model in the lowest tier, not the strongest one.</summary>
    [Fact]
    public async Task TheSmallModelIsTheFirstModelOfTheLowestTier()
    {
        AddModel(1, "deep-model");
        AddModel(3, "fast-model");
        var vFactory = new RecordingProviderFactory();
        var vRouting = NewRouting();
        await vRouting.SetAutoRoutingAsync(true);
        var vSut = new AutoRouter(CreateFactory(), vRouting, vFactory, new FakeChooser(aCandidates => OnlyChoice(aCandidates[0])), new SystemClock());

        await vSut.ChooseAsync("Hello", 1, 1);

        Assert.Equal(["fast-model"], vFactory.Built.ToArray());
    }

    /// <summary>
    /// REQ-FN-072: when automatic routing is off, then the chooser is never called, the small model is never built,
    /// nothing is logged, and no pick is returned so the tiers and chains decide.
    /// </summary>
    [Fact]
    public async Task RoutingOffNeverCallsTheChooser()
    {
        AddModel(1, "deep-model");
        AddModel(3, "fast-model");
        var vChooser = new FakeChooser(aCandidates => OnlyChoice(aCandidates[0]));
        var vFactory = new RecordingProviderFactory();
        var vSut = new AutoRouter(CreateFactory(), NewRouting(), vFactory, vChooser, new SystemClock());

        var vPicked = await vSut.ChooseAsync("Hello", 1, 1);

        Assert.Null(vPicked);
        Assert.Equal(0, vChooser.Calls);
        Assert.Empty(vFactory.Built);
        Assert.Empty(ReadLog());
    }

    /// <summary>REQ-FN-072: turning the switch off after it was on puts the tiers and chains back in charge, and leaves the chains exactly as they were.</summary>
    [Fact]
    public async Task TurningRoutingOffRestoresTheTiersAndChains()
    {
        var vFirst = AddModel(2, "first");
        var vSecond = AddModel(2, "second");
        var vRouting = NewRouting();
        await vRouting.ReorderChainAsync(2, [vSecond, vFirst]);
        var vChooser = new FakeChooser(aCandidates => OnlyChoice(aCandidates[0]));
        var vSut = NewSut(vRouting, vChooser);

        await vRouting.SetAutoRoutingAsync(true);
        Assert.True(await vRouting.AutoRoutingAsync());
        Assert.NotNull(await vSut.ChooseAsync("Hello", 1, 1));
        await vRouting.SetAutoRoutingAsync(false);

        Assert.False(await vRouting.AutoRoutingAsync());
        Assert.Null(await vSut.ChooseAsync("Hello again", 1, 1));
        Assert.Equal(1, vChooser.Calls);
        Assert.Equal([vSecond, vFirst], (await vRouting.TiersAsync()).Single(aT => aT.Tier == 2).ModelIdsInOrder.ToArray());
    }

    /// <summary>A database that has never seen the switch has automatic routing off.</summary>
    [Fact]
    public async Task RoutingIsOffUntilTheOwnerTurnsItOn()
    {
        Assert.False(await NewRouting().AutoRoutingAsync());
    }

    /// <summary>
    /// REQ-FN-071: when the small model itself fails, then no pick is returned so the tiers and chains decide,
    /// and the log says the small model could not choose.
    /// </summary>
    [Fact]
    public async Task ChooserThrowingFallsBackToTiersAndChains()
    {
        AddModel(1, "deep-model");
        AddModel(3, "fast-model");
        var vRouting = NewRouting();
        await vRouting.SetAutoRoutingAsync(true);
        var vSut = NewSut(vRouting, new FakeChooser(_ => throw new HttpRequestException("The small model is down.")));

        var vPicked = await vSut.ChooseAsync("Hello", 3, 1);

        Assert.Null(vPicked);
        var vLog = ReadLog().Single();
        Assert.Equal(AutoRouter.TiersAndChainsSource, vLog.Source);
        Assert.Contains("The small model is down.", vLog.Reason);
    }

    /// <summary>A cancelled choice is cancelled, not mistaken for a small-model failure.</summary>
    [Fact]
    public async Task CancellationIsNotSwallowed()
    {
        AddModel(1, "deep-model");
        AddModel(3, "fast-model");
        var vRouting = NewRouting();
        await vRouting.SetAutoRoutingAsync(true);
        var vSut = NewSut(vRouting, new FakeChooser(_ => throw new OperationCanceledException()));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => vSut.ChooseAsync("Hello", 1, 1));
    }

    private RoutingActions NewRouting() => new(CreateFactory());

    private AutoRouter NewSut(RoutingActions aRouting, FakeChooser aChooser) =>
        new(CreateFactory(), aRouting, new RecordingProviderFactory(), aChooser, new SystemClock());

    private static ModelChoice OnlyChoice(ModelCandidate aCandidate) => new(aCandidate, "Only one.", ModelChoiceSource.OnlyCandidate, null);

    private int AddModel(int aTier, string aIdentifier)
    {
        using var vConnection = CreateFactory().OpenConnection();
        var vProviderId = vConnection.ExecuteScalar<long>(
            "INSERT INTO Provider (Name, Connector, SignInMethod, BaseUrl, SecretName, State) VALUES (@aIdentifier, 'Fake', 'Key', 'http://localhost', NULL, 'Connected'); SELECT last_insert_rowid();",
            new { aIdentifier });
        var vModelId = (int)vConnection.ExecuteScalar<long>(
            "INSERT INTO Model (ProviderId, Tier, Identifier) VALUES (@vProviderId, @aTier, @aIdentifier); SELECT last_insert_rowid();",
            new { vProviderId, aTier, aIdentifier });
        var vChain = vConnection.ExecuteScalar<string>("SELECT Value FROM RoutingSetting WHERE Key = @vKey;", new { vKey = $"Tier{aTier}Chain" });
        var vIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(string.IsNullOrWhiteSpace(vChain) ? "[]" : vChain)!;
        vIds.Add(vModelId);
        vConnection.Execute("UPDATE RoutingSetting SET Value = @vJson WHERE Key = @vKey;", new { vJson = System.Text.Json.JsonSerializer.Serialize(vIds), vKey = $"Tier{aTier}Chain" });
        return vModelId;
    }

    private List<LogRow> ReadLog()
    {
        using var vConnection = CreateFactory().OpenConnection();
        return vConnection.Query<LogRow>("SELECT SessionId, ModelIdentifier, Source, Reason FROM RoutingChoice ORDER BY RoutingChoiceId;").ToList();
    }

    private sealed record LogRow(long? SessionId, string ModelIdentifier, string Source, string Reason);

    private sealed class FakeChooser : IModelChooser, IModelChooserFactory
    {
        private readonly Func<IReadOnlyList<ModelCandidate>, ModelChoice> objChoose;

        public FakeChooser(Func<IReadOnlyList<ModelCandidate>, ModelChoice> aChoose) => objChoose = aChoose;

        public int Calls { get; private set; }

        public IReadOnlyList<ModelCandidate> Offered { get; private set; } = [];

        public IModelChooser Create(ILlmProvider aSmallModel) => this;

        public Task<ModelChoice> ChooseAsync(string request, IReadOnlyList<ModelCandidate> candidates, CancellationToken cancellationToken = default)
        {
            Calls++;
            Offered = candidates;
            return Task.FromResult(objChoose(candidates));
        }
    }

    private sealed class RecordingProviderFactory : IAgentLlmProviderFactory
    {
        public List<string> Built { get; } = [];

        public Task<ILlmProvider> CreateAsync(ConnectedModel aModel, CancellationToken aCt = default)
        {
            Built.Add(aModel.ModelIdentifier);
            // The fake chooser never talks to the provider, so none needs to exist.
            return Task.FromResult<ILlmProvider>(null!);
        }
    }
}
