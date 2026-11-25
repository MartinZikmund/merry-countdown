# The app's name

English: **Merry Countdown**. Czech: **Čekání na Vánoce**. A Czech user should see the same name in the
title bar, under the icon, in the Start menu and in the share sheet. This page says where each one lives.

Display text only. The package identity, the product id and the reserved Store name do not change.

## Where the name lives

| Place | Source | Notes |
|---|---|---|
| In-app title, Settings card, full-version title | `ApplicationName` and `FullVersionTitle` in `Strings/{en,cs}/Resources.resw` | The window title and title bar use `ApplicationName`. |
| Windows: Start, taskbar, Settings > Apps | `ApplicationName` in the same resw files | Copied into the package at build time, see below. |
| Android launcher label | `Platforms/Android/Resources/values{,-cs}/Strings.xml` | `Label = "@string/ApplicationName"` in `Main.Android.cs`. |
| iOS home screen | `CFBundleDisplayName` in `Platforms/iOS/Resources/{en,cs}.lproj/InfoPlist.strings` | `CFBundleName` is the same name. Apple suggests under 16 characters; `Čekání na Vánoce` is exactly 16. |
| `ApplicationTitle` in `MerryCountdown.csproj` | English only | Desktop and WebAssembly use it. They have no per-language display name. |
| Store titles | Partner Center, Play Console, App Store Connect | Not in this repo, see the last section. |

Change the Czech name in all of the first four rows. `AppNameTests` fails if they drift apart.

## How Windows gets a localized name

The manifest source (`Package.appxmanifest`) has no name in it: Uno fills it from `ApplicationTitle`. Two targets in
`src/Directory.Build.targets` replace that with a resource:

1. `GenerateWindowsAppNames` reads `ApplicationName` from each `Strings/<lang>/Resources.resw`, adds the channel and
   worktree suffix, and writes `obj/.../appname/<lang>/AppName.resw` with one key, `AppDisplayName`. Those files go
   into the package's `resources.pri`.
2. `LocalizeWindowsAppName` runs after `UnoGeneratePackageAppxManifest` and sets `Properties/DisplayName`,
   `VisualElements/@DisplayName` and `DefaultTile/@ShortName` to `ms-resource:AppName/AppDisplayName`.

The suffix is the same as before, so Dev and worktree builds keep working in both languages:

| Build | English | Czech |
|---|---|---|
| Prod | `Merry Countdown` | `Čekání na Vánoce` |
| Dev | `Merry Countdown Dev` | `Čekání na Vánoce Dev` |
| Dev, worktree `issue-39` | `Merry Countdown Dev (issue-39)` | `Čekání na Vánoce Dev (issue-39)` |

The 40-character limit on `ShortName` (see [worktree-identity.md](./worktree-identity.md)) is checked by
`scripts/verify-worktree-identity.ps1` against the English `ApplicationTitle`. A Czech worktree name with a long tag
can pass 40 characters because the Czech text is only resolved at run time. That only affects Dev builds.

To see what the package really contains:

```powershell
makepri dump /if <out>\resources.pri /of pri.xml /o    # look for AppDisplayName: one EN and one CS candidate
```

## Android and iOS

Both were already localized folders, so only the Czech text changed. The Dev suffix and worktree tag are added by
`ApplyMobileLabelAndroid` and `ApplyMobileLabelIos` in `WorktreeIdentity.targets` and do not need changing.

The Czech iOS file is UTF-8, not ASCII, because of the accented letters.

## Length

`Čekání na Vánoce` is 16 characters. Under a phone icon the launcher shortens it (about 12 characters show). It is
shorter than the earlier names, which fitted the Windows title bar at 560 DIPs wide next to the DEV badge.

## Manual follow-ups (outside the repo)

- **Microsoft Store:** add the Czech listing title. A localized title must be reserved in Partner Center before it can
  be used, the reserved name today is only *Merry Countdown*.
- **Google Play:** set the Czech title under Store presence > Main store listing > Translations.
- **App Store Connect:** set the Czech name on the Czech localization of the app. Names must be unique across the store.
- The Czech store descriptions and the privacy policy text should say *Čekání na Vánoce* too.
