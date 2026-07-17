# House Party console reference (v1.5.2)

Every command below was confirmed against the game's own `help` / `example` output on
version **1.5.2**, captured from the debug log rather than transcribed by hand.

This corrects the community documentation in several places — see [Corrections](#corrections).

Token order is **not** significant for most commands: the game's own examples deliberately
vary it (`combat frank passout` and `frank combat wakeup` are both valid). The forms below
are the ones the game itself documents.

## The full command list

The console reports 39 valid primary commands:

```
CharInfo   Clear      Combat     CombineValue  CritGrp   Crosshair  CustomFOV
Cutscene   Dance      DebugMode  Dialogue      DisableNPC Door      Emote
Events     Example    EnableNPC  Fade          GameMsg   Help       IKReach
InfoDump   Intimacy   Item       LockFPS       LookAt    Values     MatchValue
Pose       Quest      Roaming    SendEvent     SetInspectText  Social  TriggerBGC
Turn       WalkTo     WarpOverTime  WarpTo
```

`Values` is the V2 system bridged into this list; the rest are V1-style (space-separated).
The V2 commands (`change`, `outfit`, `size`, `time`, `states`, `properties`, `run`,
`addforce`, `unstuck`, `achievements`, `inventory`, `characters`) use dotted syntax and are
documented separately by the community guide.

`Achievements` is **V2-only** — `help achievements` returns "not a valid command name".
Its only function is `Achievements.clear`.

## Movement

### WarpTo — instant teleport
```
warpto vickie player          # warp a character to another character
frank warpto bedleft          # warp to a named location
warpto player 11 0 12         # warp to x y z coordinates
warpto player 0 0 0           # attempt to fix being stuck on solid ground
warpto player kitchen         # warp back if stranded out of bounds
warpto all 20 0 24            # warp everyone
```

### WarpOverTime — teleport with a duration
```
warpovertime vickie player 3          # over 3 seconds
warpovertime patrick roofneargutter 0.5
```

### WalkTo — walk (pathfind) to a destination
```
frank walkto bed
walkto brittney hottubseat1
walkto all outside
walkto all outside cancel     # cancel the movement
```

### Turn
```
derek turn around
turn rachael toward player
katherine turn toward toaster true    # true = instantly
```

### Roaming
```
roaming derek list                        # roaming state + allowed/prohibited locations
roaming patrick allow false               # disable roaming
patrick roaming true allow                # enable roaming
roaming all allow false
all changelocation roaming                # force a location change
roaming derek allowlocation hottub
derek prohibitlocation roaming stephanie
stopallcurrentroamingmotionto patrick roaming
vickie roaming clearlists
```
Requires a character or `all`.

## Social — drunk, mood, friendship, romance

```
20 drunk add madison social               # add 20 to Madison's drunk value
social -30 mood derek add                 # reduce Derek's mood by 30
social rachael friendship player 8 add    # add 8 friendship toward the player
social rachael player romance equals 10   # set romance to 10
social all drunk 25 equals                # make everyone drunk
social amy sendtext                       # pretend to text
social madison derek talkto               # talk to, if close enough
```
Modifiers: `add`, `equals`. Values: `drunk`, `mood`, `friendship`, `romance`.

## Door

```
frontdoor door open
door close "Slider Door"                  # quote names containing spaces
lock door masterbedroomdoor
door "master bedroom door" unlock
```

## CharInfo

```
CharInfo derek
```
Reports enabled state, closest move target, exact 3D world position, distances, which
characters they can see, current/queued move targets, zone, combat state, pose state, and
intimacy state. Pair the position with `warpto <char> x y z`.

## Item

```
item itemfunction list                    # every item and its available functions
item list mount                           # valid body parts for 'mount'
item setenabled frank false               # disable an item/character object
katherine item "Pretentious Goddamn Nerd" rename
ashley mount item toaster head true       # mount an item to a body part
frank triggerusewithmenu item
item "vickie's panties" itemfunction ChangeVickiePanties
item DLiciousOutfit setinventoryicon 2
item pizzabox warpitemto nearfrontdoor
```

Valid `mount` body parts (24):
```
Head, RightEye, LeftEye, LeftHand, RightHand, LeftShin, RightShin,
Spine1, Spine2, Spine3, Spine4, Neck1, Neck2, LeftFoot, RightFoot,
LeftBreast, RightBreast, Pelvis, Chest, Hip, LeftButt, RightButt,
LeftForeArm, RightForeArm
```

`item itemfunction list` reports **133 items** with **176 distinct functions**. 14 functions
are universal (`DestroyItem`, `PlaySoundEffect1`, `ResetToOriginalPosition`,
`SwitchToAlternateTexture1`, `UnMountFromObject`, …); the rest are item-specific
(`Coffee: EnableSteam`, `Fridge: EnableBloodyFridgeDentedMat`, `Forest: DestroyForest`).

## Combat

```
combat frank passout
frank combat wakeup
amy combat cancel
amy patrick combat fight        # attacker, target
combat all fight                # free-for-all
combat all fight frank          # everyone attacks Frank
combat all passout
```
Subcommands: `fight`, `passout`, `wakeup`, `cancel`. `fight` only works between **enabled**
characters. `cancel` also cancels combat for the target if they've already been struck.

## Quest

```
quest start "Hunt for Red's Thermos"
complete quest "Hunt for Red's Thermos"
quest list ashley
```
Subcommands: `list`, `start`, `fail`, `increment`, `complete`. A quest must be **started**
before it can be incremented, completed, or failed. Names are **not** case-sensitive but have
very limited typo protection, and must come from the story currently being played. `list`
takes only a character name.

## Emote

```
emote derek ecstatic 100
emote happy derek 10
emote all 2
brittney emote closedeyes       # toggles
puckeredlips emote frank 100
emote 14 frank 0                # emotes also have numeric IDs
```

## Pose

```
brittney pose test              # cycle available poses
pose rachael modelpose2 true
modelpose2 rachael false pose   # release
```

## Cutscene

```
cutscene playscene PlayerMasterBedroomSex1 player amy
cutscene endscene PlayerMasterBedroomSex1
cutscene EndAnySceneWithPlayer
cutscene playscene threesomeffm_masterbedroom player amy katherine
rachael cutscene player PlayRandomSceneFromCurrentLocation
playrandomscenefromlocation masterbedroomzone cutscene player rachael
```

## Intimacy

Requires the Explicit Content DLC.

```
sexualact intimacy player madison startdoggiestyle    # two characters
sexualact vickie 10051 intimacy                       # one character (masturbation)
0 intimacy vickie 10060                               # end
intimacy madison increaseactionspeed
```
Subcommands: `SexualAct` (0), `IncreaseActionSpeed` (10), `DecreaseActionSpeed` (11), `reset`.
IntimacyEvents accept a name or numeric ID (`StartFingering` 7000, `StartMasturbation` 10051,
`End` 10060, …).

## EnableNPC / DisableNPC

```
EnableNPC Gisella
DisableNPC Gisella
```
Useful for bringing characters into a scene. `fight` requires enabled characters.

## InfoDump

```
infodump
```
Dumps every event trigger for every character, with its trigger type and enabled state.
Large output — read it from the debug log rather than the console.

## The console's own example text is in the binary

Every `example <command>` string the console prints is stored as plain ASCII in
`HouseParty_Data/il2cpp_data/Metadata/global-metadata.dat`. [`console-examples.json`](console-examples.json)
holds **155 worked examples across 47 command verbs**, extracted directly — no need to run
anything in-game.

The IL2CPP string-literal table stores offset+length externally, so entries sit *directly
concatenated with no separator*:

```
...frank warpto bedleftTo warp Patrick to the roof area near the gutter over .5 seconds:...
```

Splitting therefore keys on the `To `/`If `/`And ` that begins the next description. The
extraction was validated against 21 examples independently captured from the game's debug
log — all 21 recovered verbatim.

This is how the reference below was completed for commands never run manually, including
`events`, `sendevent`, `dialogue`, `gamemsg`, `lookat`, `triggerbgc`, `crosshair`, `lockfps`,
`critgrp`, `matchvalue`, `ikreach`, `combinevalue` and `dance`.

## Traits (17)

`values.set(trait:X)` accepts these, from each character's `Personality.Values` in the
`.character` files:

```
Aggressive, Charismatic, Creative, Energetic, Exhibitionism, Happy, Humerous,
Intelligent, Jealous, LikesMen, LikesWomen, Nice, Optimistic, Perverse,
Serious, Shy, Sociable
```

Four non-NPCs (Babs, Murray, Podcast, Tater) have no traits — the same four that have no
quests.

## Value namespaces

Story values use colon-namespacing, matching the documented console syntax verbatim:

```
Relationship:<Character>:Friendship      Attribute:Strength | Health | Speed | Stamina
Relationship:<Character>:Romance         Combat:Skill        Dancing:Skill
CanSee:Count                             InVicinity:Count    InOutfit:<Outfit>
```

[`story-values.json`](story-values.json) has the per-character sets (Gisella 124, Liz Katz 139,
Rachael 57, …). [`player-values.json`](player-values.json) has the Player's own — 529 for
Original Story, including `OrgasmSensitivity`, `Orgasm` and `OrgasmRechargeRate`.

## Locations

[`locations.json`](locations.json) — **270 destinations** for `walkto` / `warpto` /
`warpovertime` / `item warpitemto`, harvested from movement events (`EventType` 100 = walk,
210 = warp) in the story and character files.

**The console normalises these**: strip everything non-alphanumeric and lowercase, so data's
`"HotTub Seat 1"` is typed `hottubseat1`, and `"Bed (Left)"` is `bedleft`. All eight
destinations used in the game's own examples round-trip correctly through this rule.

Note the list is mixed — walk/warp targets can also be characters (`Frank`, `Player`) and
items (`Armchair`, `Katana`), so those appear too.

## Quests are per-story

Four stories ship under `StreamingAssets/Mods/Stories/`:

| Story | Quests |
|---|---|
| Original Story | 125 across 18 characters |
| Date Night With Brittney | 11 (Brittney 9, Patrick 2) |
| A Vickie Vixen Valentine | 3 (all Vickie) |
| Combat Training | 0 |

A quest name only works in the story you're currently playing. Note that **only Original
Story populates the `CharacterName` field** inside quest entries; the other stories leave it
empty, so the owning character must be taken from the `.character` filename.

## Ripped reference data

`StreamingAssets` ships its game data as **plain UTF-16LE JSON** — no asset ripping needed.
Data lives per content pack (`Base` plus one folder per DLC), under `<pack>/Data/`.

| File | Source | Contents |
|---|---|---|
| [`clothing-ids.json`](clothing-ids.json) | `*/Data/ClothingData.json` | 2,053 clothing IDs across 9 packs, with friendly name + type |
| [`cutscenes.json`](cutscenes.json) | `*/Data/CutSceneData.json` | 137 cutscenes across 6 packs, with zone / NPC count / sex-scene flag |
| [`item-functions.json`](item-functions.json) | `item itemfunction list` | 133 items, 176 distinct functions |

Outfits (`*/Data/OutfitData.json`) carry an explicit `WearableBy`, so they're mapped per
character and shipped in the app's own `Data/commands.json` — 253 entries across 21
characters.

**Clothing is not reliably mappable to characters from static data.** `SelectedCharacter` is
`Any` for 395/400 base entries, and only ~30% of IDs carry a character-name prefix (and even
those lie — `arin_hair_amy` is Arin's hair *for Amy*). The authoritative per-character list
comes from `<Character>.change.list` in-game.

Quest names come from the Custom Story Creator files at
`StreamingAssets/Mods/Stories/<Story>/*.character` (also UTF-16LE JSON), under each
character's `Quests` array — `Name` is what the `quest` command takes.

## Corrections

The pinned Steam guide ("House Party Version 1.4.2.13172 V2 Console Commands Documentation")
is the community's canonical reference. As of 1.5.2 it is wrong in these places:

- **`Social` is not obsolete.** The guide's author explicitly told a user that
  `social character player friendship 10 equals` was "the old command" and incorrect. The
  game's own `example social` documents this exact form.
- **`Door` is a first-class command.** The guide says door locking "was moved to the Values
  section" (`values.spareroomdoor.set(IsLocked)=1`). `door`/`lock`/`unlock` exist and are
  documented in-game.
- **There is no `personality` command**, despite guides listing
  `[character] personality [trait] [value]`. Traits live at `values.set(trait:X)`.
- **`mount` is not a standalone command** — it is an `Item` subcommand.
- The guide covers ~14 V2 roots plus a handful of V1 commands. The V1 console alone has 39.

## How this was captured

The game writes every console command **and its full output** to a debug log:

```
%USERPROFILE%\Documents\Eek\House Party\DebugLogs\
```

Entries look like `Log: CmdConsole Success: <command> - <output>`. The file is flushed when
the session ends (exit to menu), or on demand via **F5 → Save to Log**. Running a batch of
`example <command>` calls and then reading the log is far faster than transcribing console
output by hand.

Other useful locations (per Eek! Games' official forum post):

- `%USERPROFILE%\AppData\LocalLow\Eek\House Party\player.log` — Unity engine log (no console output)
- `%USERPROFILE%\AppData\LocalLow\Eek\House Party\Saves` — save files (JSON behind a short binary header)
