# Docs

One page per topic. Keep it that way — see [`.claude/rules/docs.md`](../.claude/rules/docs.md).

## Building & tooling

- [building.md](./building.md) — target frameworks, per-platform prerequisites, build and run commands.
- [worktree-identity.md](./worktree-identity.md) — per-worktree package identity, so two git worktrees can be installed and run side by side.
- [xaml-styler.md](./xaml-styler.md) — XAML formatting rules and how CI enforces them.

## Architecture

- [countdown-sign.md](./countdown-sign.md) — the choosable signs and countdown styles on Home and Family Display, and how to add a sign.
- [door-wall.md](./door-wall.md) — how the 24 doors are drawn, opened and fitted to the screen.
- [json-aot-serialization.md](./json-aot-serialization.md) — `JsonSerializerContext` conventions for AOT-safe, trim-safe JSON serialization.
- [scene-taps.md](./scene-taps.md) — what each scene does when tapped, and how to make something tappable.
- [sound-effects.md](./sound-effects.md) — which taps make which sounds, how to add one, and who made them.
- [views.md](./views.md) — `ViewBase<TViewModel>`, `IViewBase`, and how views resolve their view models.

## Release

- [store-setup.md](./store-setup.md) — what to create in each store before the full version can be sold, and how to test the paywall without one.
- [versioning.md](./versioning.md) — the Dev/Prod channel model, git-height versions, side-by-side identity.
- [versioning-migration.md](./versioning-migration.md) — applying that model to an existing app.

## Design history

- [superpowers/](./superpowers/) — specs and implementation plans kept for context on why things are shaped the way they are. Not reference documentation.
