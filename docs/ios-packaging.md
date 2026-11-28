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

Each iOS workload builds with exactly one Xcode version, and App Store Connect warns (ITMS-90725) about uploads built
with an iOS SDK older than the current one, so the workflow pins both:

- `WORKLOAD_SET` in the workflow (`10.0.115` carries iOS workload 27.2) goes to `dotnet workload restore --version`.
- The job runs on the `xcode-27` image, and `.github/actions/select-xcode` selects the Xcode the installed workload
  asks for. If the image doesn't have it, the step fails and lists the Xcode versions it has.

To move to a newer iOS SDK, bump `WORKLOAD_SET`, which is easy to look up: `dotnet workload search version <set>` lists
the workloads in a set.

Locally, build with a matching Xcode: `DEVELOPER_DIR=/Applications/Xcode-27.2.0.app/Contents/Developer`, with the
same workload set installed (`dotnet workload install ios --version 10.0.115`).

## Export compliance

`Info.plist` sets `ITSAppUsesNonExemptEncryption` to `false`: the app uses only the system's HTTPS (RevenueCat), so
App Store Connect doesn't ask about encryption on every upload.

## Store listing

The text and screenshots are in [store-listing.md](./store-listing.md#app-store).
