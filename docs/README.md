# Docs

One page per topic. Keep it that way — see [`.claude/rules/docs.md`](../.claude/rules/docs.md).

## Building & tooling

- [building.md](./building.md) — target frameworks, per-platform prerequisites, build and run commands.
- [worktree-identity.md](./worktree-identity.md) — per-worktree package identity, so two git worktrees can be installed and run side by side.
- [xaml-styler.md](./xaml-styler.md) — XAML formatting rules and how CI enforces them.

## Architecture

- [app-bar.md](./app-bar.md) — the sticker tabs at the top of the app, how they share the title bar on Windows, the customize button, and the badge that says a new door is waiting.
- [compact-overlay.md](./compact-overlay.md) — the small always-on-top countdown window, and why only WinUI offers it.
- [countdown-sign.md](./countdown-sign.md) — the choosable signs and countdown styles on Home and Family Display, and how to add a sign.
- [dialogs.md](./dialogs.md) — how every dialog gets the app look and the chosen theme, and why its template is a copy.
- [door-wall.md](./door-wall.md) — how the 24 doors are drawn, opened and fitted to the screen.
- [json-aot-serialization.md](./json-aot-serialization.md) — `JsonSerializerContext` conventions for AOT-safe, trim-safe JSON serialization.
- [keep-screen-on.md](./keep-screen-on.md) — the setting that stops the screen sleeping, how it combines with Family Display and window visibility, and what each head uses.
- [localization.md](./localization.md) — adding strings, plural forms for counted things, names in Czech, and the Czech house style.
- [scene-shake.md](./scene-shake.md) — what each scene does when the device or the window is shaken, and how a shake is told apart from a drag.
- [scene-taps.md](./scene-taps.md) — what each scene does when tapped, and how to make something tappable.
- [sound-effects.md](./sound-effects.md) — which taps make which sounds, what plays them on each head, how to add one, and who made them.
- [views.md](./views.md) — `ViewBase<TViewModel>`, `IViewBase`, and how views resolve their view models.
- [wish-letter.md](./wish-letter.md) — the Wishes page: the pinned add bar, picture-first wishes, undo, and what sending needs.
- [wish-pictures.md](./wish-pictures.md) — the picture picker on the Wishes page, and how to add a picture to it.

## Release

- [store-setup.md](./store-setup.md) — what to create in each store before the full version can be sold, and how to test the paywall without one.
- [versioning.md](./versioning.md) — the Dev/Prod channel model, git-height versions, side-by-side identity.
- [versioning-migration.md](./versioning-migration.md) — applying that model to an existing app.
- [windows-packaging.md](./windows-packaging.md) — building the Store `.msixupload`, and what keeps the Windows package small.

## Design history

- [superpowers/](./superpowers/) — specs and implementation plans kept for context on why things are shaped the way they are. Not reference documentation.
