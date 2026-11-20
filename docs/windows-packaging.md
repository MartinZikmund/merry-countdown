# Windows packaging

How `.github/workflows/package-windows.yml` turns the WinAppSDK head into a Store `.msixupload`, and what keeps that
package small.

## Building a Store upload

Actions → **Build Windows Packages** → Run workflow, pick the branch and a **channel**:

- **Prod** — Store identity (`5143MartinZikmund.MerryCountdown`, publisher `CN=5EDFBB6C-AD5D-4B76-B479-1C5F44017714`).
  This is the one to upload to Partner Center.
- **Dev** — the side-by-side Dev identity, for testing.

Pushes still pick the channel from the branch: `release/v*` builds Prod, `main` builds Dev. The repo is private, so
pushes are skipped and only manual runs build (see [versioning.md](./versioning.md#workflows)).

The `merge` job's artifact, `MerryCountdown_<version>_<channel>.msixupload`, is what you upload. It holds one
`.msixbundle` (x86, x64, ARM64) plus the symbol files.

## What the workflow does

1. Restores and builds **the project file**, not the `.slnx`, once per platform. The solution defines no platforms, so
   `/p:Platform=ARM64` on it fails with MSB4126.
2. Passes `/p:Platform` to the restore too. The platform picks `Properties/PublishProfiles/win-<platform>.pubxml`, which
   turns trimming and ReadyToRun on; without it the trimmer is never restored and the build silently ships an untrimmed
   package. A step fails the job if no ILLink output exists. That restore's global `TargetFramework` also reaches the
   `net10.0`-only Scenes project, so the workflow restores Scenes again on its own.
3. Checks the built package's manifest: the version must be `X.Y.Z.0` (the Store rejects a non-zero revision), and a
   Prod package must carry the Store name and publisher above.
4. On x64, signs a throwaway copy with a generated certificate, installs it next to the Windows App Runtime it depends
   on, launches it, and fails if the app isn't running 15 seconds later. The screenshot is uploaded as
   `Smoke_Screenshot_<channel>`.
5. Bundles only the `MerryCountdown_*.msix` packages. The runtime packages under `Dependencies\` are skipped: the Store
   installs the runtime itself, and `makeappx` refuses to bundle them.
6. Keeps only the app's own PDBs in the symbol packages. The Skia and ANGLE native PDBs are ~150 MB of the upload and
   only symbolicate crashes inside those libraries.

The packages are **unsigned**. The Store re-signs every package, so the Prod path needs no certificate. The
`BASE64_ENCODED_WINDOWS_PFX*` secrets are optional and the build doesn't use them yet; a Dev package for sideloading
needs signing first (`winapp cert generate` + `winapp sign`).

## What keeps the package small

All in `src/MerryCountdown/MerryCountdown.csproj`, for the WinAppSDK head only:

| Setting | Why |
|---|---|
| `WindowsAppSDKSelfContained=false` | The Windows App Runtime is a framework package the Store installs once, not a copy in every app. |
| `TrimMode=partial` | Full trimming strips what XAML reaches through WinRT: `{markup:Localize}`, converters, `{Binding}` sources. |
| `TrimmerRoots.xml` | Keeps `Microsoft.UI.Xaml.Markup`. Without it the trimmed package crashes at launch with `0xc000027b`. |
| `PrunePackageReference` for Windows ML | `Microsoft.WindowsAppSDK.ML` and `Microsoft.Windows.AI.MachineLearning` ship ~40 MB of unused natives. Remove the prunes if the app ever uses Windows ML. |

Measured on CI (November 2026), per-architecture `.msix`:

| | x64 | ARM64 | x86 | `.msixupload` |
|---|---|---|---|---|
| Before | 70.9 MB | 68.3 MB | 44.1 MB | — (never built) |
| After | 28.3 MB | 26.8 MB | 25.7 MB | 80.7 MB |

The SkiaSharp natives are **not** pruned: `Controls/SceneCanvas.cs` draws the scenes with `SkiaSharp.Views.WinUI` on
Windows.

## Trimming and JSON

A trimmed build switches reflection-based `System.Text.Json` off. Anything stored with `IPreferences.SetComplex` must be
registered in `src/MerryCountdown/Services/Settings/PreferencesJsonContext.cs`, or saving it throws
`NotSupportedException`. Core models go through `MerryCountdownJsonContext`, see
[json-aot-serialization.md](./json-aot-serialization.md).

Debug builds don't trim, so a missing registration or trimmer root only shows up in a Release package. The smoke launch
catches startup crashes; it doesn't exercise every screen.
