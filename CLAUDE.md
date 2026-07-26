# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repository is

This is the source for **SCP Mod Manager**, a RimWorld mod (Steam Workshop item, packageId
`scplegion.scpmodmanager`) that replaces the vanilla mod-management screen. It is a fork/continuation of
Fluffy's original `ModManager` (`Source/modinfo.json` / `Source/ModConfig.json` / `Source/About.xml`
deliberately still carry that upstream project's *original* identity — packageId `fluffy.modmanager`,
name "Mod Manager" — as historical fork-tracking metadata; do not "fix" those to match this project's
identity, see the note in Repository layout below). This fork was deliberately renamed — namespace,
assembly, Harmony instance ID, packageId, and mod name are all distinct from the upstream project — so it
is never mistaken for, or resolved as a dependency match against, the original `ModManager`/`fluffy.modmanager`
mod. The fork also adds a dark-theme UI redesign and a Linux asset-loading fix on top. It is a C# game
mod, not a standalone application — it only runs embedded inside RimWorld via the game's mod-loading
system.

## Repository layout

- `Source/` — all C# source, split into three projects (see below). Also carries mod-publishing
  artifacts inherited from the original repo (`modinfo.json`, `ModConfig.json`, `metadata.json`,
  `PublishedFileId.txt`, `News/`, `About.xml`, `Manifest.xml`, `ModConfig.json`) — these are
  Workshop-publishing/fork-tracking metadata for the *upstream* Fluffy project, not build inputs, and
  intentionally still reference the original `fluffy.modmanager` identity. Leave them alone; the live,
  in-game identity lives in `About/`, not `Source/`.
- `1.0/` … `1.6/` — per-RimWorld-version folders, each with an `Assemblies/` directory. These hold the
  **built** DLLs that RimWorld actually loads at runtime for that game version (see Multi-version
  support below). They are build output, not source.
- `About/` — Workshop listing metadata (`About.xml`, `Manifest.xml`, preview images, changelog). RimWorld
  reads `About/About.xml` to know the mod's name, supported versions, and dependencies.
- `Assets/AssetBundles/` — the 1.6 Unity AssetBundle containing UI textures.
- `LegacyAssets/` — loose PNG textures, used as a fallback loader path (see the Linux fix below).
- `Languages/` — Def-injected and Keyed translations for 9 languages.
- `LoadFolders.xml` — tells RimWorld which folders to mount for each supported game version.

## Building

There is no CI workflow and no build script in this repo — building is done locally with the .NET SDK
against a RimWorld install.

- Solution file: `Source/SCPModManager.slnx` (a newer XML solution format; `Source/ModManager.sln.old` and
  the `*.csproj.oldversioncscproj` files are legacy artifacts from before the rename, not used by current
  tooling).
- Three projects, all under `Source/`: `FluffyUI/FluffyUI.csproj`, `ColourPicker/ColourPicker.csproj`,
  `SCPModManager/SCPModManager.csproj` (depends on the other two via `ProjectReference`). The project,
  its output assembly, and its C# namespace are all `SCPModManager` — this is the renamed
  `Source/ModManager/` from the original project.
- All three target `net48` and all three write their output to the **same** path:
  `../../1.6/Assemblies` — i.e. everything currently builds only for the 1.6 folder, regardless of the
  version folders present at the repo root. There is no separate build per RimWorld version at present.
- Game references come from the `Krafs.Rimworld.Ref` NuGet package (pinned to `1.6.4535`), which resolves
  `Assembly-CSharp.dll` etc. from a local RimWorld install. `Lib.Harmony`, `Mlie_Rimworld_VersionFromManifest`,
  `SemanticVersioning`, and `YamlDotNet` are pulled from NuGet as well.
- Typical commands, run from `Source/`:
  ```
  dotnet restore SCPModManager.slnx
  dotnet build SCPModManager.slnx -c Release
  ```
  (`Release` is the only configured `BuildType` in the `.slnx`.) A working build requires a RimWorld
  installation reachable by `Krafs.Rimworld.Ref` for the game-assembly references — this sandbox has no
  RimWorld install and no `dotnet` SDK, so builds can't be verified here; treat compilation as best-effort
  from reading the code, and flag anywhere you're unsure a change compiles.
- There are no automated tests in this repository. Verification is manual (in-game), and "passed autotests"
  in past commit messages refers to the mod author's own external RimWorld regression pass, not anything
  runnable from this repo.

## Multi-version support

RimWorld mods must support multiple past game versions simultaneously. This repo does that with the
classic per-version-folder convention: `LoadFolders.xml` maps each supported version (`1.0`–`1.6`) to the
root, a version-numbered folder, and an assets folder, and `About/About.xml` lists the same versions
under `supportedVersions`. When touching anything version-specific, check `LoadFolders.xml` and
`About/About.xml` together — they need to stay in sync.

The `1.6` entry is the only one with a documented deliberate quirk: the shipped 1.6 AssetBundle
(`Assets/AssetBundles`) is built with a newer Unity editor than the 1.6 game runtime and silently fails
to load textures on some platforms (observed on Linux), which crashes the UI when the textures come back
null. The fix was to also mount `LegacyAssets` (loose PNGs) after `Assets` on 1.6, and to make texture
lookups in `Utilities/Resources.cs` degrade gracefully (e.g. `Spinner` falls back to `Warning` /
`BaseContent.BadTex` instead of throwing) rather than assume the bundle always loads.

## Architecture (`Source/SCPModManager/`)

- **`SCPModManager.cs`** — the `Mod` entry point (class `SCPModManager : Mod`, namespace `SCPModManager`).
  Constructs the `Harmony` instance (id `scplegion.scpmodmanager`) and calls `PatchAll`, initializes
  `UserData` and `SCPModManagerSettings` singletons exposed as static properties on `SCPModManager`
  (`SCPModManager.Instance`, `SCPModManager.UserData`, `SCPModManager.Settings`).
- **`Page_BetterModConfig.cs`** — the actual mod-selection window (extends vanilla's
  `Page_ModsConfig`); this is the largest and most central file. It owns the two-list UI (available vs.
  active mods), search filters, keyboard navigation/focus state (`FocusArea`), drag-and-drop reordering,
  and viewport-culled rendering of both lists for performance.
- **`ModButton/`** — wraps a `ModMetaData` in a richer model: `ModButton` (base), with
  `ModButton_Installed`, `ModButton_Missing`, `ModButton_Downloading` subclasses for the different states
  a mod entry can be in. `ModButtonManager` indexes/caches all known buttons and resolves mod identifiers
  (see identifier-resolution rules in `Source/ForModders.md`).
- **`ModList/`** — save/load of named mod-list snapshots (backups), import/export as a shareable string,
  and matching a list against currently installed mods. `ModIdentifier` is the serializable
  reference-by-identifier type used inside saved lists.
- **`Dependencies/`** — parses and evaluates the modder-facing `Manifest.xml` contract (dependencies,
  incompatibilities, `loadBefore`/`loadAfter` hints, version checks). `SourceSync.cs` and
  `VersionCheck.cs` do the online-manifest comparison for update checks. The full manifest schema and
  identifier-resolution order are documented in `Source/ForModders.md` — read that file before changing
  parsing behavior here, since it's a public contract for other mod authors.
- **`Manifest/`** — the `Manifest.cs` model itself plus extension helpers, loaded via
  `LoadDataFromXmlCustom`.
- **`CrossPromotion/`** — "mods by the same author" discovery/promotion feature; queries the Steam
  Workshop for other items by an author and filters out mods the user already has.
- **`Patches/`** — Harmony patch classes, one file per patched method (naming convention:
  `<Type>_<Method>.cs`). These alter vanilla behavior (e.g. main menu controls, Workshop
  subscribe/unsubscribe notifications, window resizing) without editing game code.
- **`Utilities/`**:
  - `Resources.cs` — the single source of truth for all icon/texture lookups and the dark-theme
    palette (`Resources.DarkTheme`). **Never inline a new `Color(...)` for UI chrome** — add it to
    `DarkTheme` and reference it, so the whole palette stays retunable from one place.
  - `Workshop.cs` — Steam Workshop subscribe/query helpers.
  - `UserData.cs` / `IUserData.cs` — per-user persisted settings/state (implements `IExposable` for
    RimWorld's save/scribe system).
  - `IO.cs` — filesystem helpers (local mod copies, mod-list file storage next to save games).
  - `I18n.cs` — translation-key wrappers; all keys are prefixed `SCPLegion.SCPModManager.` (see
    `PREFIX`), matching the `Languages/*/Keyed/*.xml` tag names — keep both in sync when adding keys.
  - `Constants.cs` — shared layout constants (e.g. `StandardSize` used by `Page_BetterModConfig`).
- **`SCPModManagerSettings.cs`** — the in-game mod settings page (background color, cross-promotion
  toggle, etc.), rendered via `DoSettingsWindowContents`.

`Source/FluffyUI/` and `Source/ColourPicker/` are separate, smaller projects consumed as library
references (not copied locally — `CopyLocal`/`Private` are `false` in the `.csproj`, since their DLLs
already ship in the main assemblies folder). `ColourPicker` implements the colour-selection dialog used
for mod/mod-list colouring; `FluffyUI` provides shared custom widgets (grids, float menus) beyond
vanilla `Verse.Widgets`.

## Conventions

- No external UI libraries — all rendering goes through `Verse.Widgets` / `UnityEngine.GUI`. Keep new UI
  code in that style rather than introducing a UI framework.
- One class per file, file named after the class/patched member.
- Harmony patches live under `Patches/`, one file per patch, named `<PatchedType>_<PatchedMethod>.cs`.
- UI colors, especially anything touching the dark theme, must go through `Resources.DarkTheme` /
  `Resources`, not ad-hoc `Color` literals.
- Changing `Manifest.xml` parsing, dependency resolution, or identifier matching is a public-contract
  change for other mod authors — cross-check against `Source/ForModders.md` and update it if behavior
  changes.
- `About/About.xml`'s `<description>` is a duplicate (BBCode-formatted) of `README.md`'s prose sections —
  when updating the feature list or changelog bullets in one, mirror the change in the other (and
  `Source/description.md` / `Source/Readme.md`, which are older copies of the same text).
- This fork's identity (namespace `SCPModManager`, packageId `scplegion.scpmodmanager`, Harmony id
  `scplegion.scpmodmanager`, translation prefix `SCPLegion.SCPModManager`) is intentionally distinct from
  upstream's `ModManager`/`fluffy.modmanager`/`Fluffy.ModManager`, so this mod is never conflated with, or
  matched as, the original by other mods' dependency/compatibility checks. Don't reintroduce bare
  `ModManager`/`fluffy.modmanager` naming in live code, `About/`, `README.md`, or `Languages/` — only the
  legacy `Source/` upstream-tracking files (see above) and historical `Source/News/` entries are exempt.
