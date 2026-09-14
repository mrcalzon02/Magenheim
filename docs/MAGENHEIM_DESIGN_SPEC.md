# Magenheim Design Specification

## Core premise

Magenheim adds a geology-to-magic progression layer to Valheim. Magic begins with physical biome geodes, proceeds through a dedicated Crystal Shaping skill, and grows into equipment enhancement and later deliberate magical systems. It should feel like a survival-progression extension of Valheim rather than a disconnected spell menu.

## Permanent progression identities

Crystal tiers are ordered and stable:

1. Rough
2. Simple
3. Crystal
4. Advanced
5. Master

Refinement always advances exactly one tier. Ordinary refinement preserves elemental alignment.

Normal elemental alignments are:

- Earth
- Fire
- Frost
- Storm
- Venom
- Radiance
- Seidr
- Spirit

Fate crystals and boss resonance are later special systems, not ordinary geode alignments.

Permanent Crystal Shaping skill identifier: `magenheim.crystal_shaping`.

## Refinement

Canonical base failure and shard return:

- Rough -> Simple: 10% failure, 1 matching shard.
- Simple -> Crystal: 20% failure, 2 matching shards.
- Crystal -> Advanced: 30% failure, 3 matching shards.
- Advanced -> Master: 40% failure, 5 matching shards.

A valid failure destroys the source crystal. Failure is still practice, so a valid success or failure may award Crystal Shaping experience. Invalid attempts are non-mutating and award no experience.

Crystal Shaping reduces failure:

`skillReduction = (CrystalShapingSkill / 100) * MaximumFailureReduction`

`effectiveFailure = BaseFailure * (1 - skillReduction)`

Default maximum reduction is 75%, configurable from 50% through 100%. At 100% configured reduction and skill 100, effective failure reaches zero.

Station progression gates the four steps:

- `Magenheim_GeologistWorkstation`
- `Magenheim_StationUpgrade_FracturingBlock`
- `Magenheim_StationUpgrade_FacetingWheel`
- `Magenheim_StationUpgrade_ResonanceFrame`

## Geodes

Every terrestrial biome may define its own naturally occurring geode family. Geodes appear as physical world objects, visually distinct from ordinary stones, larger than common ground stones but smaller than the player. Mining produces the intact geode; mining does not directly emit finished crystals.

Geodes are opened at the Geologist's Workstation. The default opening transaction produces:

- one guaranteed Rough crystal;
- an independent 35% chance for a second crystal;
- an independent 10% chance for a third crystal.

Each produced crystal receives its elemental alignment from the geode/biome weighted table unless a definition explicitly specifies linked rolls. Weight tables must have a positive total and may be normalized mathematically rather than requiring an exact total of 100.

The first bounded content slice is Meadows/Earth. Additional biome tables are data additions and must not require a second refinement engine.

## World-generation invariants

Worldgen integration is additive and non-destructive. Magenheim may add its own prefabs, vegetation, locations, or clutter, but compatibility code must not delete, rewrite, disable, or reorder vanilla or third-party entries.

Every Magenheim registration uses a stable `magenheim.` key. Before registration, integration code observes existing registrations and creates a plan. An occupied key is skipped or rejected according to conservative policy; the existing entry survives unchanged.

Supported abstract spawn areas are Median, Edge, and All. None and unknown bits are invalid by default. Configuration may explicitly clamp known bits or fall back to All, but destructive foreign mutation can never be enabled through compatibility configuration.

The pure planner validates definitions and collisions. A thin runtime adapter translates approved entries to current Jötunn/Valheim APIs and records what it attempted. Repeated lifecycle calls must be idempotent.

## Adaptive equipment socketing

Socketing is per item instance and additive. It must never represent an individual socket by mutating a shared `ItemDrop` or prefab.

Eligible equipment is discovered from observable category/capabilities and configurable overrides rather than a fixed vanilla-only list. Supported effect profiles may include weapons, armor, shields, tools, and utility equipment. Each crystal element/tier can provide a different category-appropriate bonus.

Rules:

- namespace persistent custom data under Magenheim-owned keys;
- preserve unknown foreign metadata;
- allow item/prefab/category/mod-origin include and exclude configuration;
- do not replace another mod's prefab solely to add a socket;
- refuse unsafe classification with a diagnostic instead of cloning or mutating foreign content;
- apply effects through Magenheim-owned calculation/effect layers.

## Multiplayer and transactions

Server gameplay rules are authoritative. Gameplay-significant definitions must not silently diverge between peers.

Geode opening, refinement, socket changes, inventory consumption, item grants, and skill experience are authoritative transactions. Client UI may preview, but persisted mutation requires server approval. Operations must defend against replay, reconnect duplication, inventory-full cases, and partial consumption.

Before persistent socketing is production-admitted, metadata must survive save/load, drop/pickup, storage, repair, upgrade, player/world transfer, host/client, and dedicated-server flows.

## Validation ladder

A source file existing is not completion. Validate in increasing strength:

1. source/schema review;
2. deterministic pure-core tests;
3. compilation against intended targets;
4. plugin startup and dependency registration;
5. disposable-world generation and repeated-load idempotence;
6. server-authoritative multiplayer transactions;
7. persistence and mod-removal safety.

Do not use valuable worlds as first-test environments.

## Later systems and archived design

The earlier full design explored staves, runecarving, Galdr, Seidr rituals, wards, travel, ship attunement, elemental resonance, battlefield totems, spirit binding, Fate magic, and boss-resonant artifacts. That material remains useful and is preserved at `docs/archive/MAGENHEIM_DESIGN_SPEC_0.1.0_PRE_RECONCILIATION.md`.

Archived material is a design reservoir, not automatic implementation authority. Each recovered feature must be reconciled against this live specification, current source, current APIs, and current user direction before implementation.
