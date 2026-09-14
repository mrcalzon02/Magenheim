# Magenheim Design Specification

## Core premise

Magenheim adds a geology-to-magic progression layer to Valheim. Geodes appear as biome-specific world resources. Geodes are opened into elemental crystals. Crystal Shaping is its own progression skill. Crystals are refined through increasingly valuable tiers and later socketed into weapons, armor, tools, and equipment through adaptive, non-destructive compatibility rules.

## Permanent progression model

Crystal tiers are ordered and stable:

1. Rough
2. Simple
3. Refined
4. Advanced
5. Master

Refinement always advances exactly one tier. A rule explicitly defines source tier, destination tier, base failure chance, station requirement, and failure shard return. Runtime code must not infer hidden tier jumps.

The canonical failure curve is:

- Rough -> Simple: 10% base failure, 1 matching shard on failure.
- Simple -> Refined: 20% base failure, 2 matching shards on failure.
- Refined -> Advanced: 30% base failure, 3 matching shards on failure.
- Advanced -> Master: 40% base failure, 5 matching shards on failure.

A failed valid refinement destroys the source crystal and returns element-matched shards. Failure is still practice, so a valid refinement attempt is experience-eligible whether it succeeds or fails. Invalid requests award no experience.

Crystal Shaping reduces failure rather than imposing invented tier-level gates. The default maximum reduction is 75%, configurable from 50% through 100%. The authoritative calculation is:

`skillReduction = (CrystalShapingSkill / 100) * MaximumFailureReduction`

`effectiveFailure = BaseFailure * (1 - skillReduction)`

At 100% configured reduction and skill 100, effective failure is zero.

Station progression gates refinement:

- Rough -> Simple: `Magenheim_GeologistWorkstation`
- Simple -> Refined: `Magenheim_StationUpgrade_FracturingBlock`
- Refined -> Advanced: `Magenheim_StationUpgrade_FacetingWheel`
- Advanced -> Master: `Magenheim_StationUpgrade_ResonanceFrame`

## Elemental alignment

Every normal crystal carries exactly one stable elemental alignment. Canonical persistent identities are:

- Earth
- Fire
- Frost
- Storm
- Venom
- Radiance
- Seidr
- Spirit

Fate and boss resonance are later special systems, not ordinary geode elemental alignments. Refinement preserves elemental alignment unless a future explicitly defined transmutation mechanic states otherwise.

The current bounded implementation remains the Meadows/Earth vertical slice. Additional biome tables are additive data definitions and must not require changing the refinement engine.

## Geodes

Each biome owns one or more geode definitions. A geode definition contains its biome, weighted crystal outcome table, world-spawn parameters, item identity, and opening requirements. Outcome selection uses independent weighted rolls where multiple crystals may be produced unless the definition explicitly requests linked rolls.

World-generation integration must be additive and non-destructive: register Magenheim resources without replacing biome vegetation tables, location lists, or third-party spawn definitions. Compatibility should prefer append/registration APIs and config-driven enable/disable controls.

## Crystal Shaping skill

Crystal Shaping is a dedicated Valheim skill. Valid geode opening and valid refinement attempts can award experience even when refinement fails, provided all preconditions passed.

Permanent skill identifier: `magenheim.crystal_shaping`.

## Equipment slotting

Socketing is a later milestone and must be adaptive rather than based on hard-coded vanilla item lists. Eligible equipment is discovered from item metadata/category plus configurable inclusion and exclusion rules. Magenheim must attach socket data without overwriting another mod's item definition. Crystal effects are calculated from equipment category, crystal element, crystal tier, and configured scaling.

Compatibility rules:

- never replace another mod's prefab solely to add a socket;
- never rewrite an external recipe when an additive recipe/upgrade path can be used;
- preserve unknown metadata and custom-data keys;
- namespace all persistent custom data under Magenheim-owned keys;
- expose category, item, prefab, and mod-origin allow/deny configuration;
- server-authoritative mutations must be designed before persistent sockets are enabled in multiplayer.

## Multiplayer and persistence boundary

Server gameplay rules are authoritative. Refinement chances, element definitions, geode weights, socket rules, and other gameplay data must not silently diverge between peers. The pure-domain engine makes deterministic decisions from explicit inputs; later runtime code owns inventory transactions, RPC authority, synchronized configuration, and persistence.

Per-item state must never be represented by mutating a shared `ItemDrop` or prefab definition. Persistent item metadata uses Magenheim-owned keys and must survive save/load, drop/pickup, repair, chest storage, world transfer, and multiplayer transfer before socketing is admitted as production behavior.

## Current implementation boundary

The current source slice is pure domain logic only. It owns canonical crystal identities, tier ordering, rule validation, skill-scaled refinement probability, destructive failure/shard outcomes, and deterministic results. It deliberately does not depend on Unity, Valheim, BepInEx, or Jötunn. Runtime adapters consume this layer later.
