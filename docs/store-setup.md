# Store setup

The code for billing is done. What's left needs your accounts, and it's the part with waiting lists: get it moving well
before December.

Until then the app runs fine and shows the paywall, but **buying can't work**. Only web and desktop are fully free.

The whole app knows one product: **`dev.mzikmund.merrycountdown.pro`**, a one-time purchase (see
[`StoreConfig`](../src/MerryCountdown/Services/Store/StoreConfig.cs)).

## Microsoft Store (WinUI head)

1. Partner Center: the name *Merry Countdown* is reserved.
2. The Prod identity is set in `MerryCountdown.csproj` for the Windows head only: `ApplicationId` is the package name and
   `StorePackagePublisher` the `CN=` publisher. Uno writes the publisher as `O=<ApplicationPublisher>`, so the
   `SetStorePackagePublisher` target in `src/Directory.Build.targets` overwrites it in the generated manifest.
3. Add-ons: create a **Durable** add-on, lifetime forever, with product id `dev.mzikmund.merrycountdown.pro`.
4. Submit the app and add-on. Store licences only exist once certified, so this can't be tested earlier.

## Google Play and App Store (via RevenueCat)

1. Create the app in Play Console and App Store Connect, each with a non-consumable product (`dev.mzikmund.merrycountdown.pro`).
2. In RevenueCat: add both apps, create the entitlement **`dev.mzikmund.merrycountdown.pro`**, attach the products and put them in the
   *current* offering. The first package of that offering is what the app sells.
3. Put the public SDK keys in `StoreConfig.RevenueCatAndroidKey` and `RevenueCatIosKey`. These keys are designed to ship
   inside apps.

## Privacy policy

All three stores ask for a privacy policy URL, for the app and for the in-app product:
<https://mzikmund.dev/Apps/MerryCountdown/PrivacyPolicy> (English and Czech on one page).

## Kids' category notes

- Parent gate guards purchases, external links and sharing.
- No ads, no analytics, no accounts, no network calls of its own. Use that in the privacy declarations.
- The curated videos are on YouTube, so **watch all twelve yourself before shipping**. They came from the 2021 app's
  list and were only checked for being online and sounding kid-friendly by title. They're in
  `src/MerryCountdown.Core/Content/doors-*.json`.

## Testing the paywall without a store

Debug builds only: set `MERRY_SIMULATE_STORE=1`, or put an empty `simulate-store.txt` in the app's local folder. Web and
desktop then behave like a store platform, and buying succeeds at once. Put `owned` in the file to start with the purchase already made.

To test December in November, set `MERRY_FAKE_DATE=2026-12-05`, or put that date in `devdate.txt` in the same folder.
