using Chatur.Core.Actions;

namespace Chatur.Core.Estimates;

/// <summary>The model and tier routing gives one line of work.</summary>
/// <param name="Model">The model identifier, e.g. <c>claude-opus-5-5</c>, or <see langword="null"/> when routing names none.</param>
/// <param name="Tier">The tier, 1 to 3, or <see langword="null"/> when not known.</param>
public sealed record StepRoute(string? Model, int? Tier);

/// <summary>A sister project's usable runs.</summary>
/// <param name="Name">The folder's name, as the estimate names it.</param>
/// <param name="Samples">Its usable runs.</param>
public sealed record SisterHistory(string Name, IReadOnlyList<RunSample> Samples);

/// <summary>Everything an estimate is worked out from. No model, no network, no file: the same input gives the same output.</summary>
/// <param name="App">The project's name.</param>
/// <param name="Phase">The phase whose checklist is estimated; 0 when there is none.</param>
/// <param name="Rows">The phase's checklist rows.</param>
/// <param name="Own">This project's usable runs (<see cref="RunHistory.Read"/> with the phase's rows excluded).</param>
/// <param name="Skipped">What this project's history left out.</param>
/// <param name="Sisters">The sister projects' usable runs.</param>
/// <param name="Routes">The model and tier for each of <see cref="EstimateSteps.All"/>; a missing step has neither.</param>
/// <param name="AutoRouting">Whether automatic routing is on.</param>
public sealed record EstimateInput(
    string App,
    int Phase,
    IReadOnlyList<string> Rows,
    IReadOnlyList<RunSample> Own,
    EstimateSkipped Skipped,
    IReadOnlyList<SisterHistory> Sisters,
    IReadOnlyDictionary<string, StepRoute> Routes,
    bool AutoRouting);

/// <summary>
/// The estimate, in code, with no model asked (BRD-195 to BRD-200, BRD-208, BRD-209). The arithmetic is
/// <c>tf-estimate.sh</c>'s: per row kind and line of work, the first quartile, median and third quartile of
/// each run's output tokens and seconds per row, interpolated between neighbours and rounded half to even;
/// fewer than three runs is no precedent. Which runs count is the owner's order of 10 Nov 2026: the step's
/// model here, then with the sister projects, then every model here, then every model with the sister projects.
/// </summary>
public static class EstimateCalculator
{
    /// <summary>The fewest runs a figure may rest on.</summary>
    public const int MinRuns = 3;

    /// <summary>Works out the phase's estimate.</summary>
    /// <param name="aInput">The rows, the runs and the routes.</param>
    public static PhaseEstimate Estimate(EstimateInput aInput)
    {
        var vRows = aInput.Rows.Where(aRow => RunHistory.PrefixOf(aRow) is not null).Distinct(StringComparer.Ordinal).ToList();
        var vCache = new Dictionary<(string Step, string Prefix), (RowEstimate Figure, IReadOnlySet<string> Keys)>();
        var vFigures = new List<RowEstimate>();
        var vKeys = new Dictionary<(string Step, string ReqId), IReadOnlySet<string>>();

        foreach (var vReqId in vRows)
        {
            var vPrefix = RunHistory.PrefixOf(vReqId)!;
            foreach (var vStep in EstimateSteps.All)
            {
                if (!vCache.TryGetValue((vStep, vPrefix), out var vCached))
                {
                    vCached = ForKind(aInput, vStep, vPrefix);
                    vCache[(vStep, vPrefix)] = vCached;
                }

                vFigures.Add(vCached.Figure with { ReqId = vReqId });
                vKeys[(vStep, vReqId)] = vCached.Keys;
            }
        }

        var vTotals = EstimateSteps.All.Select(aStep => Total(aInput, aStep, vRows, vFigures, vKeys)).ToList();
        return new PhaseEstimate(aInput.App, aInput.Phase, vRows, vFigures, vTotals, aInput.Skipped.ThisPhase > 0, aInput.AutoRouting, aInput.Skipped);
    }

    /// <summary>
    /// First quartile, median and third quartile by linear interpolation between neighbours, each rounded to a
    /// whole number half to even — exactly the framework's <c>quartiles</c> and <c>rnd</c>.
    /// </summary>
    /// <param name="aValues">At least one value.</param>
    public static EstimateFigure Quartiles(IReadOnlyList<double> aValues)
    {
        if (aValues.Count == 0)
        {
            throw new ArgumentException("Quartiles need at least one value.", nameof(aValues));
        }

        var vSorted = aValues.OrderBy(aValue => aValue).ToArray();
        double At(double aP)
        {
            var vK = (vSorted.Length - 1) * aP;
            var vLo = (int)vK;
            var vHi = Math.Min(vLo + 1, vSorted.Length - 1);
            return vSorted[vLo] + ((vSorted[vHi] - vSorted[vLo]) * (vK - vLo));
        }

        return new EstimateFigure(
            (long)Math.Round(At(0.25), MidpointRounding.ToEven),
            (long)Math.Round(At(0.5), MidpointRounding.ToEven),
            (long)Math.Round(At(0.75), MidpointRounding.ToEven));
    }

    private static (RowEstimate Figure, IReadOnlySet<string> Keys) ForKind(EstimateInput aInput, string aStep, string aPrefix)
    {
        var vRoute = aInput.Routes.GetValueOrDefault(aStep) ?? new StepRoute(null, null);
        var vOwn = aInput.Own.Where(aSample => aSample.Step == aStep && aSample.Prefixes.Contains(aPrefix)).ToList();
        var vSisters = aInput.Sisters
            .SelectMany(aSister => aSister.Samples
                .Where(aSample => aSample.Step == aStep && aSample.Prefixes.Contains(aPrefix))
                .Select(aSample => (aSister.Name, Sample: aSample)))
            .ToList();
        bool OnModel(RunSample aSample) => string.Equals(aSample.Model, vRoute.Model, StringComparison.OrdinalIgnoreCase);

        // The owner's order (10 Nov 2026): the step's model first, here and then with the sister projects; then
        // every model, here and then with the sister projects. With no model named there is no "same model" pool.
        var vPools = new List<(bool AllModels, bool WithSisters)>();
        if (vRoute.Model is { Length: > 0 })
        {
            vPools.Add((false, false));
            if (aInput.Sisters.Count > 0)
            {
                vPools.Add((false, true));
            }
        }

        vPools.Add((true, false));
        if (aInput.Sisters.Count > 0)
        {
            vPools.Add((true, true));
        }

        List<RunSample> vPool = [];
        var vNames = new List<string>();
        var vAllModels = false;
        foreach (var (vAll, vWithSisters) in vPools)
        {
            vAllModels = vAll;
            vPool = vOwn.Where(aSample => vAll || OnModel(aSample)).ToList();
            vNames = [];
            if (vWithSisters)
            {
                var vFromSisters = vSisters.Where(aEntry => vAll || OnModel(aEntry.Sample)).ToList();
                vPool.AddRange(vFromSisters.Select(aEntry => aEntry.Sample));
                vNames = vFromSisters.Select(aEntry => aEntry.Name).Distinct(StringComparer.Ordinal).ToList();
            }

            if (vPool.Count >= MinRuns)
            {
                var vFigure = new RowEstimate(
                    string.Empty,
                    aStep,
                    Quartiles(vPool.Select(aSample => aSample.TokensPerRow).ToList()),
                    Quartiles(vPool.Select(aSample => aSample.SecondsPerRow).ToList()),
                    vWithSisters ? EstimateBasis.Family : EstimateBasis.Repo,
                    vPool.Count,
                    vAll && vRoute.Model is { Length: > 0 },
                    vNames);
                return (vFigure, vPool.Select(aSample => aSample.Key).ToHashSet(StringComparer.Ordinal));
            }
        }

        return (new RowEstimate(string.Empty, aStep, null, null, EstimateBasis.None, vPool.Count, false, []), new HashSet<string>());
    }

    private static StepTotal Total(
        EstimateInput aInput,
        string aStep,
        List<string> aRows,
        List<RowEstimate> aFigures,
        Dictionary<(string Step, string ReqId), IReadOnlySet<string>> aKeys)
    {
        var vRoute = aInput.Routes.GetValueOrDefault(aStep) ?? new StepRoute(null, null);
        var vHave = aFigures.Where(aFigure => aFigure.Step == aStep && aFigure.Basis != EstimateBasis.None).ToList();
        var vRuns = vHave.SelectMany(aFigure => aKeys[(aStep, aFigure.ReqId)]).Distinct(StringComparer.Ordinal).Count();
        var vSisters = vHave.SelectMany(aFigure => aFigure.SisterProjects).Distinct(StringComparer.Ordinal).ToList();
        if (vHave.Count == 0)
        {
            // REQ-FN-109: "no precedent" still names the runs it found, fewer than three; the rows' pools are the ones counted.
            var vFound = aFigures.Where(aFigure => aFigure.Step == aStep).Select(aFigure => aFigure.BasisRuns).DefaultIfEmpty(0).Max();
            return new StepTotal(aStep, vRoute.Model, vRoute.Tier, null, null, EstimateBasis.None, vFound, aRows.Count, aRows.Count, false, []);
        }

        return new StepTotal(
            aStep,
            vRoute.Model,
            vRoute.Tier,
            vHave.Select(aFigure => aFigure.TokensOut).Aggregate(EstimateFigure.Add),
            vHave.Select(aFigure => aFigure.DurationS).Aggregate(EstimateFigure.Add),
            vHave.Any(aFigure => aFigure.Basis == EstimateBasis.Family) ? EstimateBasis.Family : EstimateBasis.Repo,
            vRuns,
            aRows.Count,
            aRows.Count - vHave.Count,
            vHave.Any(aFigure => aFigure.FromAllModels),
            vSisters);
    }
}
