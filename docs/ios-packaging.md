# iOS packaging

How `.github/workflows/package-ios.yml` builds the App Store `.ipa` and gets it to TestFlight.

## Building an upload

Actions → **Build iOS Packages** → Run workflow, pick the branch, a **channel** and whether to upload to **TestFlight**:

- **Prod**: bundle id `dev.mzikmund.merrycountdown`, signed for the App Store. With *TestFlight* ticked, the `.ipa` goes
  to App Store Connect; it never goes to review by itself. Submitting is a click in App Store Connect.
- **Dev**: the side-by-side `…merrycountdown.dev` app, built unsigned to check that iOS still compiles.

Pushes pick for you: `release/v*` builds Prod and uploads to TestFlight, `main` builds Dev.

The artifact `iOS_IPA` holds the signed `MerryCountdown.ipa`.

## Signing and secrets

| Secret | What it is |
| --- | --- |
| `APPLE_DISTRIBUTION_P12_BASE64`, `APPLE_P12_PASSWORD` | The Apple Distribution certificate with its private key, exported from Keychain Access as `.p12` |
| `APPLE_CODESIGN_KEY` | The certificate's name, `Apple Distribution: Martin Zikmund (X8D8TDGSDY)` |
| `APPLE_TEAM_ID` | `X8D8TDGSDY` |
| `APPLE_PROVISIONING_PROFILE_BASE64`, `APPLE_PROVISIONING_PROFILE_UUID` | The *Merry Countdown App Store* provisioning profile and its UUID |
| `APPSTORE_ISSUER_ID`, `APPSTORE_API_KEY_ID`, `APPSTORE_API_PRIVATE_KEY` | An App Store Connect API key (App Manager) and its raw `.p8`, for the upload |

The certificate expires on 2027-09-25. When it's renewed, the profile has to be regenerated with it, and both
secrets updated.

## Version

The build is named `major.minor.height` (`BuildVersionSimple` from Nerdbank.GitVersioning), for example `0.3.12`, and
the same number goes in `CFBundleShortVersionString` and `CFBundleVersion`. The App Store Connect version must match
the short version of the build you submit. See [versioning.md](./versioning.md).

## Xcode

The iOS workload pins one exact Xcode version, and the runner image usually has a newer one. The workflow builds with
`-p:ValidateXcodeVersion=false` (the `XCODE_CHECK` variable), which works as long as the Xcode is newer. Locally, do
the same, or point `DEVELOPER_DIR` at a matching Xcode.

## Export compliance

`Info.plist` sets `ITSAppUsesNonExemptEncryption` to `false`: the app uses only the system's HTTPS (RevenueCat), so
App Store Connect doesn't ask about encryption on every upload.

## Store listing

The text and screenshots are in [store-listing.md](./store-listing.md#app-store).
