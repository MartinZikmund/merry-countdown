# Android packaging

How `.github/workflows/package-android.yml` builds the Google Play bundle, and what Play needs before it takes one.

## Building an upload

Actions → **Build Android Packages** → Run workflow, pick the branch, a **channel** and a **Play track**:

- **Prod** — package `dev.mzikmund.merrycountdown`, the one Play knows.
- **Dev** — the side-by-side `…merrycountdown.dev` package, for sideloading only. It is never uploaded.
- **Play track** — `none` builds the `.aab` as an artifact only; `internal`, `alpha`, `beta` or `production` also upload it.

Pushes pick for you: `release/v*` builds Prod and uploads to production, `main` builds Dev and uploads nothing.

The artifact `Android_AAB_<channel>` holds the signed bundle, `dev.mzikmund.merrycountdown-Signed.aab`.

## The first upload is manual

Play's API can't create the first release of a new app: run the workflow with **Prod** and track **none**, download
the artifact and upload the `.aab` in Play Console (Test and release → Production or Internal testing → Create new
release). Play App Signing is set up at that point, and the key that signed this first bundle becomes the **upload
key**: every later upload must be signed with it.

Until the app is published, Play only accepts **draft** releases through the API, so the workflow uploads drafts. Once
the app is live, set the repository variable `PLAY_RELEASE_STATUS` to `completed` (Settings → Secrets and variables →
Actions → Variables) and releases roll out without a click.

## Signing and secrets

| Secret | What it is |
| --- | --- |
| `ANDROID_KEYSTORE_BASE64` | The upload keystore, base64 encoded |
| `ANDROID_KEY_ALIAS` | The key's alias in that keystore |
| `ANDROID_KEY_PASSWORD`, `ANDROID_STORE_PASSWORD` | The key and keystore passwords |
| `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` | A Google Cloud service account key with release rights on the app in Play Console (Users and permissions) |

Dev builds are signed with the same upload key, so `main` builds fail if the first four are missing.

## Version code

Nerdbank.GitVersioning sets `versionCode` itself (`NBGV_SetVersionForMauiAndroid`): major × 2²⁴ + minor × 2¹⁶ + git
height, so `0.3` with height 5 is `196613`. It only goes up as long as releases come from `release/v*` branches with a
rising `version.json`; see [versioning.md](./versioning.md).

## Prod guard

An upload to the production track fails if `StoreConfig.RevenueCatAndroidKey` is still empty: the app would build and
run, but its paywall couldn't sell Pro. Artifact-only and testing-track builds go through, so the app can be tested
before RevenueCat is set up. See [store-setup.md](./store-setup.md).

## Store listing

The text and graphics are in [store-listing.md](./store-listing.md#google-play).
