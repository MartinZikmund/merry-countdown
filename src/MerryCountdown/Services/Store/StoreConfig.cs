namespace MerryCountdown.Services.Store;

/// <summary>Store identifiers. See docs/store-setup.md for what to create in each console.</summary>
public static class StoreConfig
{
    /// <summary>Microsoft Store add-on token, and the RevenueCat entitlement and product id.</summary>
    public const string FullVersionProductId = "dev.mzikmund.merrycountdown.pro";

    // RevenueCat's public SDK keys are designed to ship inside apps. iOS is empty until its app exists in RevenueCat.
    public const string RevenueCatAndroidKey = "goog_fZVItMOpixvXZXNYqsfQKHaPMdZ";
    public const string RevenueCatIosKey = "";
}
