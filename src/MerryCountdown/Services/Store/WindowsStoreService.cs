#if WINAPPSDK
using MerryCountdown.Core.Store;
using Windows.Services.Store;

namespace MerryCountdown.Services.Store;

/// <summary>Microsoft Store add-on. A desktop app has no implicit window, so the context must be tied to one.</summary>
public sealed class WindowsStoreService(Func<nint> windowHandle) : IStoreService
{
    private StoreContext? _context;

    public bool HasBilling => true;

    private StoreContext Context
    {
        get
        {
            if (_context is null)
            {
                _context = StoreContext.GetDefault();
                WinRT.Interop.InitializeWithWindow.Initialize(_context, windowHandle());
            }

            return _context;
        }
    }

    public async Task<string?> GetPriceAsync(CancellationToken ct = default) =>
        (await FindAddOnAsync(ct))?.Price.FormattedPrice;

    public async Task<bool> IsOwnedAsync(CancellationToken ct = default)
    {
        var license = await Context.GetAppLicenseAsync().AsTask(ct);
        return license.AddOnLicenses.Values.Any(a => a.IsActive && a.InAppOfferToken == StoreConfig.FullVersionProductId);
    }

    public async Task<PurchaseResult> PurchaseAsync(CancellationToken ct = default)
    {
        if (await FindAddOnAsync(ct) is not { } addOn)
        {
            return PurchaseResult.NotAvailable;
        }

        var result = await Context.RequestPurchaseAsync(addOn.StoreId).AsTask(ct);
        return result.Status switch
        {
            StorePurchaseStatus.Succeeded => PurchaseResult.Succeeded,
            StorePurchaseStatus.AlreadyPurchased => PurchaseResult.AlreadyOwned,
            StorePurchaseStatus.NotPurchased => PurchaseResult.Cancelled,
            StorePurchaseStatus.NetworkError => PurchaseResult.NetworkError,
            _ => PurchaseResult.Failed,
        };
    }

    private async Task<StoreProduct?> FindAddOnAsync(CancellationToken ct)
    {
        var query = await Context.GetAssociatedStoreProductsAsync(["Durable"]).AsTask(ct);
        return query.ExtendedError is null
            ? query.Products.Values.FirstOrDefault(p => p.InAppOfferToken == StoreConfig.FullVersionProductId)
            : throw new InvalidOperationException("The Microsoft Store query failed.", query.ExtendedError);
    }
}
#endif
