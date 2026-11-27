namespace Chatur.Core.Actions;

/// <summary>
/// What is left on each connected provider's key, for <c>keys-panel</c> on Measurements and Measurements across
/// projects (REQ-UI-129 to REQ-UI-131, REQ-FN-326; Architecture decisions log 2026-11-27). Three ways, in this order:
/// the provider's own balance or limit address (OpenRouter, DeepSeek), else Chatur's own count of priced calls against a
/// limit the person typed, else "not known" with the reason. Never a guess.
/// </summary>
public interface IKeyBalanceActions
{
    /// <summary>
    /// One line per connected provider, in the order Settings ▸ Model providers lists them. A provider with a balance
    /// address is asked on every call, so "Ask again" is this call again.
    /// </summary>
    /// <param name="aCt">A token that cancels the read and any question in flight.</param>
    Task<IReadOnlyList<KeyBalanceLine>> ListAsync(CancellationToken aCt = default);

    /// <summary>Saves the limit typed for one key and the date it counts from (REQ-UI-131).</summary>
    /// <param name="aProviderId">The provider whose key it is.</param>
    /// <param name="aLimit">The limit; checked again here as <see cref="CheckLimit"/> does.</param>
    /// <param name="aCt">A token that cancels the save.</param>
    /// <exception cref="ArgumentException">The amount is not above 0 with at most two decimals, or the date is after today.</exception>
    Task SetLimitAsync(int aProviderId, KeyLimit aLimit, CancellationToken aCt = default);

    /// <summary>Removes the limit typed for one key; its line then reads "not known" unless the provider answers.</summary>
    /// <param name="aProviderId">The provider whose key it is.</param>
    /// <param name="aCt">A token that cancels the removal.</param>
    Task ClearLimitAsync(int aProviderId, CancellationToken aCt = default);

    /// <summary>
    /// Checks the Limit dialog's two fields: an amount above 0 with at most two decimals, and a date not after today.
    /// Each field that fails says why.
    /// </summary>
    /// <param name="aAmountText">The amount as typed, e.g. <c>50.00</c>.</param>
    /// <param name="aCountingFrom">The date chosen, or <see langword="null"/> when none is.</param>
    KeyLimitCheck CheckLimit(string? aAmountText, DateOnly? aCountingFrom);
}
