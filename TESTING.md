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
- Local-host workstation operation source for geode opening and Earth refinement, including
  authority/replay/capacity admission, atomic inventory mutation, failure shard returns,
  and Crystal Shaping XP awards. This source still requires a rebuilt live-world retest.

This remains a vertical-slice test. Remote-client workstation operation RPC, socket-management
UI, and full persistence/multiplayer admission are not complete. Do not use a valuable world
for first-pass testing of newly rebuilt operation code.

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
