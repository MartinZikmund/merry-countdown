namespace MerryCountdown.Core.Store;

public enum PurchaseResult
{
    Succeeded,
    AlreadyOwned,
    Cancelled,
    NetworkError,
    NotAvailable,
    Failed,
}

/// <summary>One platform's billing for the single "full version" product.</summary>
public interface IStoreService
{
    /// <summary>False on web and desktop, where there's no billing and everything is simply free.</summary>
    bool HasBilling { get; }

    Task<string?> GetPriceAsync(CancellationToken ct = default);

    /// <summary>Throws when the store can't be reached. False means genuinely not owned.</summary>
    Task<bool> IsOwnedAsync(CancellationToken ct = default);

    Task<PurchaseResult> PurchaseAsync(CancellationToken ct = default);
}

public sealed class NoBillingStoreService : IStoreService
{
    public bool HasBilling => false;

    public Task<string?> GetPriceAsync(CancellationToken ct = default) => Task.FromResult<string?>(null);

    public Task<bool> IsOwnedAsync(CancellationToken ct = default) => Task.FromResult(true);

    public Task<PurchaseResult> PurchaseAsync(CancellationToken ct = default) => Task.FromResult(PurchaseResult.NotAvailable);
}

public interface IEntitlementCache
{
    bool IsFull { get; set; }
}

public interface IFullVersionService
{
    bool IsFull { get; }

    bool CanPurchase { get; }

    event EventHandler? Changed;

    Task<string?> GetPriceAsync();

    Task RefreshAsync(CancellationToken ct = default);

    Task<PurchaseResult> PurchaseAsync(CancellationToken ct = default);
}

public sealed class FullVersionService(IStoreService store, IEntitlementCache cache) : IFullVersionService
{
    public bool IsFull => !store.HasBilling || cache.IsFull;

    public bool CanPurchase => store.HasBilling && !cache.IsFull;

    public event EventHandler? Changed;

    public async Task<string?> GetPriceAsync()
    {
        try
        {
            return await store.GetPriceAsync();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return null;
        }
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        if (!store.HasBilling)
        {
            return;
        }

        try
        {
            Apply(await store.IsOwnedAsync(ct));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Offline on a train: a family who paid keeps what they paid for.
        }
    }

    public async Task<PurchaseResult> PurchaseAsync(CancellationToken ct = default)
    {
        var result = await store.PurchaseAsync(ct);
        if (result is PurchaseResult.Succeeded or PurchaseResult.AlreadyOwned)
        {
            Apply(true);
        }

        return result;
    }

    private void Apply(bool owned)
    {
        if (cache.IsFull == owned)
        {
            return;
        }

        cache.IsFull = owned;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
