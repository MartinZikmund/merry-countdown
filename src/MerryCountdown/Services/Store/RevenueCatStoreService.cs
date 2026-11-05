#if __ANDROID__ || __IOS__
using MerryCountdown.Core.Store;
using Uno.RevenueCat.Enums;
using Uno.RevenueCat.Models;
using Uno.RevenueCat.Services;

namespace MerryCountdown.Services.Store;

/// <summary>Google Play and App Store through RevenueCat. No account, no user id: purchases follow the store login.</summary>
public sealed class RevenueCatStoreService(IRevenueCatBilling billing) : IStoreService
{
    public bool HasBilling => true;

    private static string ApiKey =>
#if __ANDROID__
        StoreConfig.RevenueCatAndroidKey;
#else
        StoreConfig.RevenueCatIosKey;
#endif

    private bool EnsureInitialized()
    {
        if (string.IsNullOrEmpty(ApiKey))
        {
            return false;
        }

        if (!billing.IsInitialized)
        {
            billing.Initialize(ApiKey);
        }

        return true;
    }

    public async Task<string?> GetPriceAsync(CancellationToken ct = default) =>
        (await FindPackageAsync(ct))?.Product.Pricing.PriceLocalized;

    public async Task<bool> IsOwnedAsync(CancellationToken ct = default)
    {
        if (!EnsureInitialized())
        {
            throw new InvalidOperationException("RevenueCat isn't configured yet.");
        }

        var info = await billing.GetCustomerInfoAsync(cancellationToken: ct);
        return info.IsSuccess
            ? info.Value!.Entitlements.Any(e => e.IsActive && e.Identifier == StoreConfig.FullVersionProductId)
            : throw new InvalidOperationException("RevenueCat couldn't be reached.", info.ErrorException);
    }

    public async Task<PurchaseResult> PurchaseAsync(CancellationToken ct = default)
    {
        if (await FindPackageAsync(ct) is not { } package)
        {
            return PurchaseResult.NotAvailable;
        }

        var result = await billing.PurchaseProductAsync(package, ct);
        if (result.IsSuccess)
        {
            return PurchaseResult.Succeeded;
        }

        return result.Error switch
        {
            PurchaseErrorStatus.PurchaseCancelledError => PurchaseResult.Cancelled,
            PurchaseErrorStatus.NetworkError => PurchaseResult.NetworkError,
            _ => PurchaseResult.Failed,
        };
    }

    private async Task<PackageDto?> FindPackageAsync(CancellationToken ct)
    {
        if (!EnsureInitialized())
        {
            return null;
        }

        var offerings = await billing.GetOfferingsAsync(cancellationToken: ct);
        return offerings.IsSuccess ? offerings.Value!.GetCurrent()?.AvailablePackages.FirstOrDefault() : null;
    }
}
#endif
