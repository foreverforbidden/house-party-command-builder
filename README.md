# HP Commander

A Windows desktop tool for building [House Party](https://store.steampowered.com/app/611790/House_Party/) V2 console commands through a GUI instead of typing them by hand.

House Party's built-in developer console supports both a legacy space-separated syntax (`Item condom setenabled true`) and the newer dot-chained V2 syntax (`Rachael.change(top)`, `Rachael.values.set(trait:Exhibitionism)=100`). Several of these commands are case-sensitive in ways that silently fail instead of erroring, which makes hand-typing them error-prone. This app builds the exact command string from structured controls (dropdowns, checkboxes, steppers) and copies it to the clipboard for pasting into the in-game console.

## Features

- Character/target picker with search, multi-select, and an "All Characters" mode
- Builders for Change, Outfit, Inventory, Values (traits/relationships/object values), States, Properties, Run, Addforce, Size, Time, Misc, Intimacy, and legacy V1 commands
- All game-specific data (characters, items, traits, states, etc.) lives in an editable `Data/commands.json` — extend it as you discover more without recompiling
- Clipboard copy with a short history of recently built commands

## Console reference

[`docs/console-reference.md`](docs/console-reference.md) documents the **v1.5.2** console —
all 39 V1 commands with syntax confirmed against the game's own `help`/`example` output, plus
corrections to the community guide (`Social` and `Door` are not obsolete; there is no
`personality` command). [`docs/item-functions.json`](docs/item-functions.json) lists all 133
items and their 176 available item functions.

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

`Data/commands.json` is copied next to the executable and can be hand-edited afterward.

## Notes

- Some commands (e.g. the `Intimacy` reset subcommand, `Size`/`Time` edge cases) aren't fully confirmed against the game's actual behavior and are labeled as such in the UI. Contributions with confirmed syntax are welcome.
- This is an unofficial, community-built tool and is not affiliated with or endorsed by Eek! Games.
