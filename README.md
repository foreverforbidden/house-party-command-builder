# HP Commander

A Windows desktop tool for building [House Party](https://store.steampowered.com/app/611790/House_Party/) V2 console commands through a GUI instead of typing them by hand.

House Party's built-in developer console supports both a legacy space-separated syntax (`Item condom setenabled true`) and the newer dot-chained V2 syntax (`Rachael.change(top)`, `Rachael.values.set(trait:Exhibitionism)=100`). Several of these commands are case-sensitive in ways that silently fail instead of erroring, which makes hand-typing them error-prone. This app builds the exact command string from structured controls (dropdowns, checkboxes, steppers) and copies it to the clipboard for pasting into the in-game console.

## Features

- Character/target picker with search, multi-select, and an "All Characters" mode
- Builders for Change, Outfit, Inventory, Values (traits/relationships/object values), States, Properties, Run, Addforce, Size, Time, Misc, Intimacy, and legacy V1 commands
- All game-specific data (characters, clothing, values, items, world, social, quests) lives in editable `Data/*.json` files — extend it as you discover more without recompiling
- Dark mode with live switching, no restart required
- Optional update check: asks GitHub whether there is a newer release and, if you say so, downloads and installs it in place (opt-in, off by default — see [Updating](#updating))
- Optional auto-copy: builds a command and copies it to the clipboard automatically once you stop typing (opt-in, asks for consent first)
- Clipboard copy with a short history of recently built commands

## Console reference

[`docs/console-reference.md`](docs/console-reference.md) documents the **v1.5.2** console —
all 39 V1 commands with syntax confirmed against the game's own `help`/`example` output, plus
corrections to the community guide (`Social` and `Door` are not obsolete; there is no
`personality` command).

Reference data ripped from the game's own files:

- [`docs/console-examples.json`](docs/console-examples.json) — **155 worked examples across 47 commands**, extracted from the game binary
- [`docs/clothing-ids.json`](docs/clothing-ids.json) — 2,053 clothing IDs across 9 content packs
- [`docs/locations.json`](docs/locations.json) — 270 walk/warp destinations
- [`docs/cutscenes.json`](docs/cutscenes.json) — 137 cutscenes with zone and cast info
- [`docs/items-from-story.json`](docs/items-from-story.json) — 353 items with display names and actions
- [`docs/item-functions.json`](docs/item-functions.json) — 133 items, 176 item functions
- [`docs/story-values.json`](docs/story-values.json) / [`docs/player-values.json`](docs/player-values.json) — `values` command targets
- [`docs/achievements.json`](docs/achievements.json) — 91 achievements

## Requirements

- Windows
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) to build (the app itself targets `net10.0-windows` with WPF)

## Building

```
dotnet build
```

## Running

```
dotnet run --project HpCommander/HpCommander.csproj
```

## Publishing a portable build

Produces a single self-contained `.exe` (no .NET runtime required on the target machine) at `publish/HpCommander.exe`:

```
./publish.ps1
```

Which is just this, if you'd rather run it yourself:

```
dotnet publish HpCommander/HpCommander.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish
```

`publish/` is gitignored — the exe is a build output, not a checked-in artifact.

`Data/*.json` is copied next to the executable and can be hand-edited afterward. Each file covers one domain (`characters.json`, `clothing.json`, `values.json`, `items.json`, `world.json`, `social.json`, `quests.json`); `tools/import-data.cs` regenerates them from the `docs/` reference dumps if you want to refresh from source instead (`dotnet run tools/import-data.cs`).

## Cutting a release

Pushing a `v*` tag runs [`.github/workflows/release.yml`](.github/workflows/release.yml), which tests,
publishes, and attaches `HpCommander-v<x.y.z>-win-x64.zip` (the exe plus its `Data` folder) to a new
[GitHub release](https://github.com/foreverforbidden/house-party-command-builder/releases):

```
git tag v1.8.0 && git push origin v1.8.0
```

The tag is passed to `publish.ps1 -Version`, so the binary always reports the version it was tagged
with. Keep `<Version>` in `HpCommander.csproj` in step for local builds. The asset name matters: the
in-app updater looks for exactly that pattern, so run the workflow rather than uploading by hand.

## Updating

Off by default. Turn on "Check for updates on startup" in the **Info** section, or press "Check for
updates" there at any time. The check reads the public releases API and sends nothing about you.

When a newer release exists you are offered the choice to install it, skip that version, or be
reminded later. Installing downloads the release zip, verifies it against the checksum GitHub
publishes, then replaces the executable **and** the `Data` folder together and restarts — they are a
matched pair, and an app whose `Data` came from a different version refuses to load it.

> Because the whole `Data` folder is replaced, any hand-edits you have made to `Data/*.json` are
> lost on update. Keep a copy if you have customised them.

If the program lives somewhere it cannot write to itself (for example under `Program Files`), it
says so and opens the releases page instead of downloading anything.

## Help wanted: character run functions

The Run tab knows 133 items and the ~176 functions between them, because
`item itemfunction list` dumps all of it at once. There is no equivalent for characters, so
`runFunctions` in `values.json` is down to the two that have been confirmed by hand.

`<character>.run.list` reports them one character at a time. If you collect any, they go in
[`docs/character-run-functions.json`](docs/character-run-functions.json) keyed by character name:

```json
{
  "Ashley": ["EnableFaceAndChestSpermEffect", "SwitchToAlternateTexture"],
  "Leah":   ["SwitchToAlternateTexture"]
}
```

`dotnet run tools/import-data.cs` merges that into `values.json` as `characterRunFunctions`; the Run
tab then shows a character's own list when exactly one character is selected, falling back to the
shared list for everyone else. The file is merged rather than copied over, so a partial dump never
deletes characters someone else already confirmed.

## Notes

- Some commands (e.g. the `Intimacy` reset subcommand, `Size`/`Time` edge cases) aren't fully confirmed against the game's actual behavior and are labeled as such in the UI. Contributions with confirmed syntax are welcome.
- This is an unofficial, community-built tool and is not affiliated with or endorsed by Eek! Games.
