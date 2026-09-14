# Magenheim

**Magic begins as geology.**

Development foundation, version **0.0.2**. This is the beginning of the Earth / Meadows vertical slice described in `MAGENHEIM_DESIGN_SPEC.md`. It is not the playable 0.1 release.

## Implemented

- BepInEx plugin with a hard Jötunn dependency and everyone-must-have-mod / minor-version compatibility declaration.
- Crystal Shaping registration with permanent ID `magenheim.crystal_shaping` and English localization.
- Static Earth refinement definitions, strict field/range/progression validation, immutable snapshots, and a deterministic SHA-256 fingerprint.
- All four refinement calculations, station-level gates, matched-element shard results, and experience on every valid attempt.
- Meadows cracking outcomes: one guaranteed Rough crystal, independent 35% and 10% bonuses, and a separate element roll per crystal. The shipped Earth-only slice yields only Earth; mixed-element tables are exercised in tests.
- Shard recombination outcomes: five matching shards yield one Simple crystal, with station, element and quantity validation. The cost is configurable in data.
- A repeatable local build and 118 automated assertions, including 100,000 weighted selections and 100,000 simulated cracks.

Cracking, refinement and recombination produce decisions only. They do not yet consume inventory, grant items or experience, or execute gameplay RPCs. The plugin registers the skill and loads data; it does not yet register world objects or equipment. Recombination reports a single batch even when more shards are available.

The development definition schema is now version 2, adding geode rules to the same validated, hashed snapshot. Old schema-1 definition files are rejected explicitly; use the new packaged defaults and reapply intended balance edits. This does not change the planned item metadata schema or the permanent skill ID.

## Build

From this directory, run `./build.ps1` in PowerShell. It defaults to the adjacent Valheim installation and the installed `Central Fuckery` r2modman profile. Override with `-GamePath` and `-ModProfile` on another machine.

The tested build uses Windows' installed C# compiler, local game/mod reference assemblies, and an installed .NET 8 or newer runtime for the pure-core tests. No SDK or dependency download is needed for this route. A conventional solution/project is included for machines with a .NET SDK; that alternate build route has not been tested here because this machine has runtimes only.

Output: `dist/BepInEx/plugins/Magenheim/`, containing `Magenheim.dll`, `default-data/foundation.json`, and `Translations/English.json`. Runtime libraries are supplied by Valheim, BepInEx, and Jötunn, not bundled into the plugin.

## Current validation boundary

Compilation and standalone tests pass. Valheim has not been launched with this plugin, so skill registration, localization, plugin startup, and dedicated-server behavior remain unverified in game. The network attribute checks mod compatibility; it does not synchronize content data. Gameplay must remain unregistered until server authority and transactions are implemented.

The active mod profile and game saves have not been modified. Use a disposable test profile and world for initial runtime validation. Once modded items exist, removing the mod before loading valuable saves can cause modded-content loss.

## Next playable milestone

Meadows geode -> intact geode -> Geologist's Workstation -> Earth crystal -> risky refinement -> persistent socket and Earth staff. See `IMPLEMENTATION_PLAN.md`, `BACKLOG.md`, and `VALIDATION.md` for the remaining work and acceptance checks.
