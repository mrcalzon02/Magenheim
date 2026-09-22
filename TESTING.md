# Magenheim 0.0.92 - test acceptance

## Required delivery check before launching

Run `./closeout.ps1` (`-Offline` when using cached dependencies) with Valheim and
r2modman closed. Reopen r2modman and select Central Fuckery. The enabled entry must
show **Magenheim v0.0.92 by Local** and start its description with
**0.0.92: Adds Overworld/Underworld tabs to the single vanilla Minimap**.

The closeout verifies the catalog version/description and the installed DLL hashes.
If either differs, installation is incomplete. After Launch Modded, confirm the
BepInEx startup log says `Loading [Magenheim 0.0.52]` before reporting game results.
Future versions must update `release.json` and this expected version together.

Install the candidate ZIP into a disposable r2modman profile, then launch **Modded**. The plugin
is under `BepInEx/plugins/Local-Magenheim/Magenheim`. Launching vanilla Steam
does not use this profile. Exit the game before rebuilding or installing.

## What is present

- Crystal Shaping, permanent identifier `magenheim.crystal_shaping`, with its own icon.
- An original Earth geode model, texture, and inventory icon; a larger mineable world nodule.
- Rough, Simple, Crystal, Advanced, and Master Earth crystals and Earth Crystal Shards,
  each with its own model, texture atlas, and icon.
- Definition-driven Meadows geode world generation and intact-geode destruction drops.
- Geologist's Workstation plus Fracturing Block, Faceting Wheel, and Resonance Frame,
  all with original models, icons, build costs, collision shapes, and wear variants.
- Local-host workstation operation source for geode opening and Earth refinement, including
  authority/replay/capacity admission, atomic inventory mutation, failure shard returns,
  and Crystal Shaping XP awards. This source still requires a rebuilt live-world retest.

Remote-client workstation RPC and socket-management UI/transport are source-implemented.
Full persistence and multiplayer acceptance remain open. Use a disposable world and character.


## Crystal Shaping visibility regression

Open Skills immediately after loading an existing or new character, before using
the workstation. Crystal Shaping must appear at level 0 if untrained, with its
custom icon. Reopen the panel and reload the character: no duplicate entry or free
XP should appear. Previously earned Crystal Shaping level/XP must stay unchanged.

## Candidate acceptance matrix

Confirm the startup log reports **0.0.92** and has no Magenheim bootstrap or registration
errors. Keep logs and screenshots with each result. Mark checks PASS / FAIL / NOT RUN;
record game version, mod list, host/client role, seed, steps, expected/actual result.

| Area | Required acceptance |
| --- | --- |
| Geology | Mine each biome geode; exactly one intact drop; opening output follows definitions; no mutation of vanilla rocks; reload without duplicate registrations. |
| Deep Fracture caverns | 0.0.52. Enter a Deep Fracture district. It must read as an enclosed cavern: a vault overhead, terraced walkable floor, dripstone. Walk the full floor and confirm no step blocks movement; check passage mouths connect and the tunnel between districts matches them. Where a district has a surface fissure, confirm the shaft lights the floor below. |
| Geode and crystals | 0.0.52. Open a geode item and the world nodule: the exterior must read as fractured plates and the exposed face as a banded, druzy-lined cavity, tinted to its biome with no see-through faces. Check all five crystal tiers and shards read as a progression in hand and on the ground. |
| Surface fidelity | 0.0.52. Every model now carries a generated albedo. Look for surfaces that read as flat colour, unexpectedly dark, or tiled at the wrong scale. There are no normal maps; judge albedo only. |
| Inventory notification | 0.0.49 repair. Force a workshop transaction to fail (full inventory) and confirm the rollback completes with no `TargetParameterCountException` and no lost or duplicated items. Install and extract a socket, host and remote client, and confirm the inventory view refreshes immediately after each mutation. |
| Workshop | Place all upgrades; verify station levels, all eight refinement families, success/failure shard returns, XP, full-inventory rejection and repaired iron straps. |
| Sockets | Host and remote client open/install/extract; Dais permission boundary; stale item, duplicate response, reconnect, mismatch and spoofed descriptor rejection without loss/duplication. |
| Persistence | Save/reload, drop/pickup, chest, repair, upgrade, transfer, death and dedicated server for socketed items and new pieces. |
| Staffs/weapons | All eight four-tier staff families: recipe, icon, held appearance, projectile, hit/status effect and damage; all ten physical crystal weapons. |
| Construction | Furniture/decor, hearth, beams/foundations, 24 banners, eight beds, Dais, Ice Box: placement, collisions, wear, removal/refunds, comfort, interaction and persisted state. |
| Sentinel/alchemy | Sentinel targeting and all eight munition types; grinding returns; Dust recipes; Eitrwine fermentation and effects. |
| Compatibility | Representative third-party equipment and exclusions; unknown-item rejection; identical host/client authority; unchanged foreign prefab behavior. |
| Underworld maps | Large map exposes Overworld/Underworld tabs on one vanilla Minimap; small map follows the physical layer; movement reveals only the physical layer; saved pins/fog persist independently; shared-map/discovery writes stay on the physical layer; async/map-size mods do not corrupt Surface textures. |
| Disabled scope | No surface Deep Fracture locations or model-only artifact recipes should appear. |

Do not promote this candidate to production until the multiplayer and persistence
checks pass. Detailed unfinished scope is in [CLOSEOUT.md](CLOSEOUT.md).

## Underworld dual-map acceptance — 0.0.92

Use one disposable character and world. Reveal a recognizable patch of Overworld terrain and add a
saved pin. Open the large map: **Overworld** must be selected. Select **Underworld** without using
the Deep Gate. A different terrain map and independent fog state must appear; Surface transient
pins must not leak onto it. Add an Underworld saved pin, switch back and forth, and verify each pin
belongs only to its own tab.

Enter the Underworld through the Deep Gate. The small minimap must switch to the Underworld
automatically. Walk far enough to cross the normal Valheim reveal interval/radius and verify fog
opens along the route using ordinary movement. While physically below, browse the Overworld tab and
back; browsing must not move the player or change the active world layer. Repeat the inverse after
returning to Surface.

Save, quit, and reload. Both fog states and both saved-pin sets must survive independently. Exercise
cartography/shared-map data and a discovered location on each physical layer; those writes must go
to the physical layer even if the other tab was being browsed immediately beforehand.

If a map-size or asynchronous map-generation mod is installed, repeat the first Underworld-tab
generation. The mod must still run through its ordinary Valheim Minimap hooks, Underworld
generation must not write into the Overworld texture set, and an autosave during generation must
not replace the Surface profile map with the Underworld payload.

Record the result separately from the still-open Underworld world-instance isolation acceptance:
map correctness does not prove ZDO/terrain layer isolation, and layer isolation does not prove map
persistence.

## Build the workshop

Use **Hammer > Crafting**. The workstation requires a nearby vanilla Workbench.
Its three upgrades require the Geologist's Workstation. The menu entries become
known as you discover their materials. Place upgrades within five meters of the
workstation; three different upgrades can raise its displayed station level to four.
Duplicate copies of one upgrade do not stack.

| Piece | Build materials |
| --- | --- |
| Geologist's Workstation | 10 Wood, 10 Stone, 2 Flint |
| Fracturing Block | 10 Wood, 8 Stone, 4 Flint |
| Faceting Wheel | 10 Fine Wood, 10 Stone, 4 Bronze |
| Resonance Frame | 10 Fine Wood, 4 Iron, 5 vanilla Crystal |

All listed resources are recoverable on removal. Check placement preview, collision,
repair/removal, upgrade connection effects, station level, and save/reload in a
disposable world. The main bench can be used outdoors without a roof or fire.

Direct prefab inspection commands, after `devcommands` in the local test world:

```text
spawn Magenheim_GeologistWorkstation 1
spawn Magenheim_StationUpgrade_FracturingBlock 1
spawn Magenheim_StationUpgrade_FacetingWheel 1
spawn Magenheim_StationUpgrade_ResonanceFrame 1
```

Use the Hammer for placement/removal tests: console spawning alone does not verify
build-resource consumption or return behavior.

## Disposable character/world test

Enable the game's console with `-console` in the profile launch arguments, enter a
new disposable world locally, press F5, then enter `devcommands`.

```text
spawn Magenheim_Geode_Meadows_Earth 1
spawn Magenheim_Crystal_Earth_Rough 1
spawn Magenheim_Crystal_Earth_Simple 1
spawn Magenheim_Crystal_Earth_Crystal 1
spawn Magenheim_Crystal_Earth_Advanced 1
spawn Magenheim_Crystal_Earth_Master 1
spawn Magenheim_Shard_Earth 5
spawn Magenheim_Geode_Meadows_Earth_World 1
```

Inspect ground models and inventory icons. Pick up and drop each item; verify stack
names, sizes, and visuals. Mine the world nodule and verify exactly one intact geode.
Save, quit to menu, and reload to check item/nodule persistence and no duplicate registrations.
Natural geodes appear in newly generated Meadows zones; existing explored terrain
is not retroactively regenerated. A console-spawned nodule does not validate natural placement.

To inspect skill display and saving on the disposable character, use the permanent
single-token skill identifier. Valheim's `raiseskill` parser treats the display name's
space as another argument before Jotunn can resolve it, so `raiseskill Crystal Shaping 1`
is not a valid diagnostic command.

```text
raiseskill magenheim.crystal_shaping 1
```

After rebuilding/installing current source, use the Geologist's Workstation Craft panel
on a local-host world to test the Magenheim operation recipes. Opening a Meadows Earth
geode should consume exactly one intact geode, grant one to three Rough Earth crystals,
and award Crystal Shaping XP. Earth refinement recipes should consume exactly one source
crystal; success grants the next tier, while a destructive failure returns the configured
matching shards. These operation paths are source-implemented but remain unaccepted until
observed in the live disposable world.

## Live world test 1 - 2026-09-14

Observed in the disposable world before the later workstation-operation implementation:

- the Geologist's Workstation exists and opens its Craft panel;
- the Craft panel contained no Magenheim mineral operations in that build;
- no equipment-slotting operation was exposed;
- all directly spawned Earth inventory items appeared correctly;
- the spawned world geode used the custom mesh but rendered translucent;
- visible workstation surface fighting was observed around the iron tabletop bands;
- `raiseskill Crystal Shaping 1` was rejected by the command parser.

The translucent geode was traced to cloned source-material render state rather than
texture alpha: Magenheim's checked-in atlases are opaque RGB images. Source now normalizes
Magenheim custom materials to opaque blend/depth state; that repair requires a rebuild/install
and live retest before it is accepted. The workstation banding issue and socketing operation
remain open. Geode-opening/refinement operations have since been implemented for local-host
execution and now also require the rebuilt live retest.

## Build and install

With Valheim closed, run one command from the repository root:

```powershell
./install-local.ps1
```

The installer now runs `build.ps1` itself before touching the active profile. The build runs
the core test harness, compiles the runtime against the selected BepInEx/Valheim installation,
derives the package version directly from `MagenheimPlugin.PluginVersion`, creates a clean
versioned package, and only then installs it. Use `-SkipBuild` only when deliberately reinstalling
a package that was already built from the same current source.

Both scripts accept `-ProfileRoot`; the installer also forwards `-GameRoot` to the build.
Building requires .NET SDK 8, the installed game, BepInEx, and NuGet restore. The local SDK is
in the ignored `dist/toolchain/dotnet` folder; system dotnet is the fallback.

The installer archives the previous `Local-Magenheim` folder under `backups`, refuses duplicate
Magenheim DLL installations, validates package/source version agreement, verifies every copied
file by SHA-256, prints the installed `Magenheim.dll` hash, and prints the exact plugin version
expected in the BepInEx startup diagnostic. This closes the old failure mode where Git source
was current while the active profile silently continued loading an obsolete DLL.

## Evidence boundaries

See `docs/validation/2026-09-14-earth-content-package.md`,
`docs/validation/2026-09-14-workshop-content.md`, and
`docs/validation/2026-09-14-live-world-test-1.md` for observed results and current
acceptance boundaries. Natural worldgen, persistence, and multiplayer still require
separate observations.
