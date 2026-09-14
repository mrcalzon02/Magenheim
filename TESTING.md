# Magenheim 0.0.16 - Earth minerals and workshop

Launch **Modded** from r2modman's **Central Fuckery** profile. The installed plugin
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

This is a content test. Geode opening, mineral crafting recipes, refinement inventory
transactions, and earned skill XP are not connected yet. Do not consume geodes
expecting an opening action. All crystal tiers can be inspected using console spawns.

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
The workshop currently provides buildable content and station progression; its
crafting panel has no mineral recipes yet.

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

To inspect skill display and saving on the disposable character:

```text
raiseskill "Crystal Shaping" 1
```

If the game's command parser rejects the quoted display name, inspect the registered
numeric skill ID in `BepInEx/LogOutput.log` and use that ID with `raiseskill`.
This diagnostic is not an XP gameplay implementation.

## Build and install

Run `./build.ps1`, then `./install-local.ps1` with Valheim closed. Both accept
`-ProfileRoot` for another existing mod profile. Building requires .NET SDK 8,
the installed game, BepInEx, and NuGet restore. The local SDK is in the ignored
`dist/toolchain/dotnet` folder; system dotnet is the fallback.

The installer archives the previous Local-Magenheim folder under `backups`, refuses
duplicate Magenheim DLL installations, and verifies every copied file by SHA-256.
It does not install bundled Unity, framework, or third-party runtime DLLs.

## Evidence boundaries

See `docs/validation/2026-09-14-earth-content-package.md` for observed build/startup
results. Natural worldgen, actual mining drops, visual appearance under in-game
lighting, persistence, and multiplayer require separate observations.
