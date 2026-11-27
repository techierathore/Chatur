namespace Chatur.Core.Actions;

/// <summary>How what is left on a key is known: the "How it is known" pill of <c>keys-panel</c> (REQ-UI-129).</summary>
public enum KeyBalanceKind
{
    /// <summary>The provider's own balance or limit address answered (REQ-UI-130).</summary>
    AskedOfProvider,

    /// <summary>Chatur's own count of the priced calls it made through the key, against a limit the person typed (REQ-UI-131).</summary>
    OwnCount,

    /// <summary>No number is known; the note says why (REQ-FN-326).</summary>
    NotKnown
}

/// <summary>A limit the person typed for one key, counted from a date (REQ-UI-131).</summary>
/// <param name="LimitUsd">The limit in US dollars, above 0 with at most two decimals.</param>
/// <param name="CountingFrom">The first day whose calls count against it, not after today.</param>
public sealed record KeyLimit(decimal LimitUsd, DateOnly CountingFrom);

/// <summary>
/// One line of <c>keys-panel</c> on Measurements and Measurements across projects: what is left on one connected
/// provider's key and how that is known (REQ-UI-129 to REQ-UI-131, REQ-FN-326). Every amount the line has no basis for
/// is <see langword="null"/> and reads "—" or "not known"; none is ever guessed.
/// </summary>
/// <param name="ProviderId">The provider's row.</param>
/// <param name="ProviderName">The name the person gave the provider.</param>
/// <param name="Connector">The provider's connector, e.g. <c>"Anthropic"</c>.</param>
/// <param name="SignInMethod">How it signs in: <c>Key</c>, <c>Subscription</c> or <c>None</c>.</param>
/// <param name="Kind">How what is left is known.</param>
/// <param name="Note">The plain words under the pill: who answered and when, the date counted from and the calls, or why nothing is known.</param>
/// <param name="LimitUsd">The key's limit, from the provider or typed; <see langword="null"/> when there is none.</param>
/// <param name="UsedUsd">What has been used against it; <see langword="null"/> when not known.</param>
/// <param name="Left">What is left, in <paramref name="Currency"/>; <see langword="null"/> means "not known".</param>
/// <param name="Currency">The currency of the amounts; <c>USD</c> unless the provider answered in another.</param>
/// <param name="Limit">The limit typed for this key, when there is one.</param>
/// <param name="CanSetLimit">Whether a limit can be typed for this key: not for a local model, nor for one the provider answers for.</param>
public sealed record KeyBalanceLine(
    int ProviderId,
    string ProviderName,
    string Connector,
    string SignInMethod,
    KeyBalanceKind Kind,
    string Note,
    decimal? LimitUsd,
    decimal? UsedUsd,
    decimal? Left,
    string Currency,
    KeyLimit? Limit,
    bool CanSetLimit);

/// <summary>What the Limit dialog's two fields say, read by <see cref="IKeyBalanceActions.CheckLimit"/> (REQ-UI-131).</summary>
/// <param name="LimitUsd">The amount, when it is valid.</param>
/// <param name="AmountError">Why the amount cannot be saved, or <see langword="null"/>.</param>
/// <param name="FromError">Why the date cannot be saved, or <see langword="null"/>.</param>
public sealed record KeyLimitCheck(decimal? LimitUsd, string? AmountError, string? FromError)
{
    /// <summary>Whether both fields can be saved; Save stays inert otherwise.</summary>
    public bool IsValid => LimitUsd is not null && AmountError is null && FromError is null;
}
