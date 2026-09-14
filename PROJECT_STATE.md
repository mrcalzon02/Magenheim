# Magenheim Project State

## Authority

`INSTRUCTIONS.md`, committed source on the live `main` branch, and directly observed repository/runtime evidence are authoritative. Backlog, validation records, archived design, scheduled prompts, and conversation are subordinate when they disagree with verified live state.

## Current delivery - 0.0.16 plus post-test source repair

The 0.0.16 content set contains eleven original models/texture atlases and twelve
icons: geode, five Earth crystal tiers, shards, Crystal Shaping badge, Geologist's
Workstation, and three station upgrades. Models have OBJ/MTL exports. Workshop pieces
have build costs, collision shapes, distinct wear variants, and upgrade links to the
Geologist's Workstation. Its crafting skill is Crystal Shaping.

Valheim startup using the packaged assemblies registered all seven inventory items,
Crystal Shaping, the four workshop pieces, and one Meadows geode vegetation addition
without a Magenheim registration exception. The local 0.0.16 package was built with
zero errors/warnings, passed 113 core assertions, and was installed/hash-verified in
the active Central Fuckery profile before the first live world test.

### Live world test 1 - 2026-09-14

Direct in-game testing materially advanced the evidence boundary. The Geologist's
Workstation exists, opens its station UI, and all directly spawned Earth inventory
items appeared correctly. The dedicated Meadows Earth world-geode prefab also spawned
with the custom Magenheim geometry.

The same test exposed four distinct conditions:

- the world geode custom mesh rendered translucent;
- workstation tabletop/iron-band surface fighting is visible;
- the workstation Craft panel has no mineral operations;
- no adaptive equipment-slotting operation exists.

The last two are confirmed implementation gaps, not hidden/discovery failures. Current
runtime source registers the content but does not bind authoritative geode opening or
crystal refinement inventory transactions to the station UI, and P2 socket metadata,
eligibility, transaction, and persistence work is not implemented yet.

The translucent geode was traced to inherited render state on cloned source materials,
not texture transparency: Magenheim atlases are opaque RGB images. `EarthAssets` now
normalizes Magenheim-owned cloned materials to Opaque RenderType, opaque blend factors,
ZWrite, non-alpha shader keywords, and geometry render queue. That source repair is not
accepted as a live fix until a rebuilt package is installed and observed in Valheim.

The workstation iron-band issue is traced to asset geometry: the generator places thick
iron band cuboids through the tabletop boards and shares visible surface planes. That
repair remains open because the generator and checked-in derived mesh must be changed
together rather than patched at runtime.

The attempted console diagnostic `raiseskill Crystal Shaping 1` was rejected by the
vanilla command parser. The permanent Jotunn custom-skill identifier is the required
single-token diagnostic argument: `raiseskill magenheim.crystal_shaping 1`. That exact
command still requires live confirmation and is not a substitute for earned gameplay XP.

See `TESTING.md` and `docs/validation/2026-09-14-live-world-test-1.md`. The highest-priority
next work is to rebuild/retest the opaque material repair, then repair/bake the workstation
band geometry, then bind authority-gated geode opening/refinement transactions and earned
Crystal Shaping XP to the Geologist's Workstation. Adaptive socketing remains P2 and must
use per-item persistent metadata rather than shared prefab mutation.

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

Installed/tested package identity remains 0.0.16. Target framework remains .NET Framework 4.6.2 with `JotunnLib` 2.30.0. BepInEx plugin GUID is `mrcalzon02.magenheim`; network compatibility remains `EveryoneMustHaveMod` with patch strictness. Definition-authority synchronization and session-scoped geode-operation replay protection remain in source.

## Validation boundary

The 0.0.16 build/startup path has already been compiled and exercised locally, while the post-test material-state source repair has not yet been rebuilt in this execution context. Do not conflate source inspection with a compiled/runtime acceptance result.

Natural geode generation, exact mining/drop behavior, repeated-load idempotence, save/reload persistence, host/client replication, dedicated-server behavior, authority-gated geode opening/refinement, earned Crystal Shaping XP, and socket persistence remain runtime gates.

## Next exact action

1. build the current `main` source and rerun the deterministic core harness;
2. install the rebuilt package and spawn `Magenheim_Geode_Meadows_Earth_World` to confirm opaque rendering;
3. test `raiseskill magenheim.crystal_shaping 1` and verify the skill value changes;
4. repair the workstation iron-band generator geometry and checked-in generated mesh together, then retest placement/lighting;
5. implement the Geologist's Workstation geode-opening/refinement transaction binding with atomic inventory mutation, station/upgrade gating, failure shard returns, and earned Crystal Shaping XP;
6. only after that P1 transaction path is validated, begin adaptive per-item socket metadata and the station slot-management operation.
