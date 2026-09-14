# Magenheim Project State

## Authority

`INSTRUCTIONS.md`, committed source on the live `main` branch, and directly observed repository/runtime evidence are authoritative. Backlog, validation records, archived design, scheduled prompts, and conversation are subordinate when they disagree with verified live state.

## Current local delivery - 0.0.16

The local content set now contains eleven original models/texture atlases and
twelve icons: geode, five Earth crystal tiers, shards, Crystal Shaping badge,
Geologist's Workstation, and three station upgrades. Models have OBJ/MTL exports.
Workshop pieces have build costs, collision shapes, distinct wear variants, and
upgrade links to the Geologist's Workstation. Its crafting skill is Crystal Shaping.

The prior worldgen drop-table fix is installed. Valheim startup using the same
packaged assemblies now registers all seven inventory items, Crystal Shaping,
the four workshop pieces, and one Meadows geode vegetation addition without a
Magenheim registration error. The isolated check loaded no world and was closed.

0.0.16 is installed and hash-verified in the active Central Fuckery profile.
The previous installation was archived under backups/Local-Magenheim-20260914-075509.zip.
Build: zero errors/warnings. Core harness: 113 assertions passed. Eleven art
assets pass geometry, UV, atlas, and icon checks. Source changes remain local on
main and have not been committed or pushed during these content deliveries.

See TESTING.md and docs/validation/2026-09-14-workshop-content.md. Next playable
work is authoritative geode opening/refinement and earned Crystal Shaping XP.
The workshop is buildable but has no mineral recipes yet. Actual placement,
station leveling, natural mining, visual appearance, save/reload, and multiplayer
still require world tests.

The historical snapshot below predates the local content deliveries. Its
no-compiler/no-content statements are superseded by this section.

## Prior source-only snapshot — 2026-09-14

Repository: `mrcalzon02/Magenheim`.

GitHub reports `main` as the default branch and branch enumeration reports only `main`. The former `master` pointer is gone; there is no remaining parallel development branch to reconcile.

Current worldgen implementation sequence:

- `e1b530804591004717f20d6c361b7d150421eb9a` — one-time additive Jötunn geode vegetation registration, removal of the duplicate prefab-registration path, and runtime-safe `Everything`/`Everywhere` area resolution.
- `19e8d3eecef2cac4af2c55003d3638ec81cb827e` — schema-3 fingerprinted geode placement authority and validated server placement overrides.
- `1e41c263188599c5b1aad2c3be3f2f0c51e9b158` — removed the premature `ZoneSystem` vegetation lookup from prefab-availability collision observation and advanced runtime identity to 0.0.14.

## Stable domain authority

Crystal tiers remain `Rough -> Simple -> Crystal -> Advanced -> Master`. Normal elemental alignments remain Earth, Fire, Frost, Storm, Venom, Radiance, Seidr, and Spirit. Ordinary refinement preserves alignment. Base failure remains 10/20/30/40 percent with Crystal Shaping reduction and workstation/upgrade progression. Valid failed attempts destroy the source and return 1/2/3/5 matching shards by tier.

## Definition schema 3

Definition schema 3 owns refinement, geodes, worldgen compatibility, and each geode's complete Jötunn-facing placement behavior in one validated/fingerprinted snapshot.

Compatibility authority includes invalid-area behavior, duplicate behavior, prefab-collision detection, exact/case-insensitive identity comparison, and exclusions limited to Magenheim-owned registration/prefab namespaces. Additive-only behavior cannot be disabled.

Each geode placement record fingerprints block/force-placement flags, per-zone min/max, altitude and ocean-depth limits, terrain-delta range/radius, tilt range, forest thresholds, scale range, group size/radius, and ground offset. Values are validated in the pure core, including runtime-float representability.

BepInEx placement overrides are server-side inputs only. They are converted back into pure placement records, passed through `MagenheimDefinitionOverrideApplier`, fully revalidated, and included in the effective definition fingerprint before any worldgen registration consumes them. Runtime code no longer owns independent placement constants.

## Area-generation safeguards

Abstract spawn areas remain Median, Edge, and All. Negative numeric flags fail closed under Reject/Clamp behavior; unknown bits are governed by the fingerprinted invalid-area policy. Text configuration accepts `Everywhere` as an All alias.

At the runtime boundary, Magenheim resolves the current `Heightmap.BiomeArea` enum by defined name rather than integer cast. The combined value is resolved from `Everything` or `Everywhere`, covering the naming difference visible across current Jötunn source/documentation. Unsupported runtime enum values fail closed.

## Additive worldgen execution

`GeodeWorldgenRegistrar` is the single runtime mutation path for natural geodes. It waits for Jötunn vanilla prefabs, derives desired additions from `DefinitionWorldgenPlanner`, performs read-only host collision observation, reruns pure Add/Skip/Error planning, preflights every approved addition, clones/configures the new Magenheim-owned world object, and registers it once through `ZoneManager.AddCustomVegetation`.

The registrar never edits or removes host worldgen. Existing prefab identities are observations. Error decisions abort before mutation; Skip decisions leave the host entry untouched. Execution preflight also refuses an occupied prefab identity even if proactive prefab-collision reporting is disabled, so configurability cannot weaken the non-destructive invariant.

Host collision observation intentionally uses `PrefabManager.GetPrefab` during `OnVanillaPrefabsAvailable`. It does not call `ZoneManager.GetZoneVegetation` at that lifecycle point because current Jötunn source directly dereferences `ZoneSystem.instance` in that method, while a live ZoneSystem is not guaranteed by the prefab/ObjectDB availability event. Prefab occupancy remains the decisive collision boundary because Jötunn `AddCustomVegetation` itself claims the prefab through `PrefabManager.AddPrefab`.

The prior separate `GeodeWorldPrefabRegistrar` was removed because Jötunn `AddCustomVegetation` registers its prefab through `PrefabManager`; pre-registering the same world prefab created a self-collision risk.

The dedicated natural world object remains `<item prefab>_World` and is separate from the temporary Stone-derived intact inventory-item visual. It is configured for persistent network state, destructibility, and exactly one intact-geode destruction drop. Vanilla `Rock_4` is used only as a clone source and is not intentionally modified.

## Meadows/Earth placement authority

The shipped schema-3 Meadows definition currently specifies: block checking enabled, force placement disabled, `MinPerZone=0`, `MaxPerZone=0.35`, altitude 1–1000, ocean depth 0–0, terrain delta 0–2 measured over radius 2, tilt 0–35 degrees, forest filtering disabled with threshold range 0–1, scale 0.85–1.15, group size 1–1, group radius 0, and ground offset -0.10.

These fields are legitimate server configuration because any effective change is revalidated and changes definition authority/fingerprint.

## Runtime identity

Runtime source/package version is 0.0.14. Target framework remains .NET Framework 4.6.2 with `JotunnLib` 2.30.0. BepInEx plugin GUID is `mrcalzon02.magenheim`; network compatibility remains `EveryoneMustHaveMod` with patch strictness. Definition-authority synchronization and session-scoped geode-operation replay protection remain in source.

## Validation boundary

This execution host still exposes no `dotnet`, `csc`, or `mcs`, so compilation and deterministic test execution are not claimed. `GeodePlacementDefinitionTests` exists for placement preservation, fingerprint sensitivity, invalid tilt, runtime-float representability, and validated override propagation; the source is committed but not executed here.

Valheim startup, schema-3 JSON loading in-game, BepInEx config binding, natural geode generation, repeated-load idempotence, host/client replication, exact destruction/drop behavior, and proof that vanilla `Rock_4` remains unchanged remain runtime gates.

## Next exact action

In a current .NET/Valheim development environment:

1. run the pure-core test harness and repair any schema-3 defect;
2. compile `Magenheim.Runtime` against Jötunn 2.30.0/current Valheim assemblies;
3. launch a disposable world and verify one Magenheim custom vegetation registration per geode identity with no duplication across actual menu/world transitions;
4. verify server placement overrides change the effective fingerprint and produce matching host/client authority;
5. verify occupied foreign prefab identities are skipped/errored by policy without mutation;
6. mine a naturally generated Meadows geode and verify persistent network behavior plus exactly one intact-geode drop;
7. verify vanilla `Rock_4` remains unchanged;
8. then continue the Meadows loop with permanent Crystal Shaping registration, Geologist's Workstation, Earth crystal/shard item registration, and actual authority-gated inventory transactions.
