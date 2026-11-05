using MerryCountdown.Core.Store;

namespace MerryCountdown.Services.Store;

/// <summary>Debug only (MERRY_SIMULATE_STORE=1): lets the paywall be exercised where there's no billing at all.</summary>
public sealed class SimulatedStoreService(bool startsOwned = false) : IStoreService
{
    private bool _owned = startsOwned;

    public bool HasBilling => true;

    public Task<string?> GetPriceAsync(CancellationToken ct = default) => Task.FromResult<string?>("3,99 €");

    public Task<bool> IsOwnedAsync(CancellationToken ct = default) => Task.FromResult(_owned);

    public Task<PurchaseResult> PurchaseAsync(CancellationToken ct = default)
    {
        _owned = true;
        return Task.FromResult(PurchaseResult.Succeeded);
    }
}
