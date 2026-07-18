# HP Commander

A Windows desktop tool for building [House Party](https://store.steampowered.com/app/611790/House_Party/) V2 console commands through a GUI instead of typing them by hand.

House Party's built-in developer console supports both a legacy space-separated syntax (`Item condom setenabled true`) and the newer dot-chained V2 syntax (`Rachael.change(top)`, `Rachael.values.set(trait:Exhibitionism)=100`). Several of these commands are case-sensitive in ways that silently fail instead of erroring, which makes hand-typing them error-prone. This app builds the exact command string from structured controls (dropdowns, checkboxes, steppers) and copies it to the clipboard for pasting into the in-game console.

## Features

- Character/target picker with search, multi-select, and an "All Characters" mode
- Builders for Change, Outfit, Inventory, Values (traits/relationships/object values), States, Properties, Run, Addforce, Size, Time, Misc, Intimacy, and legacy V1 commands
- All game-specific data (characters, clothing, values, items, world, social, quests) lives in editable `Data/*.json` files — extend it as you discover more without recompiling
- Dark mode with live switching, no restart required
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

Produces a single self-contained `.exe` (no .NET runtime required on the target machine):

```
dotnet publish HpCommander/HpCommander.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

`Data/*.json` is copied next to the executable and can be hand-edited afterward. Each file covers one domain (`characters.json`, `clothing.json`, `values.json`, `items.json`, `world.json`, `social.json`, `quests.json`); `tools/import-data.cs` regenerates them from the `docs/` reference dumps if you want to refresh from source instead (`dotnet run tools/import-data.cs`).

## Notes

- Some commands (e.g. the `Intimacy` reset subcommand, `Size`/`Time` edge cases) aren't fully confirmed against the game's actual behavior and are labeled as such in the UI. Contributions with confirmed syntax are welcome.
- This is an unofficial, community-built tool and is not affiliated with or endorsed by Eek! Games.
