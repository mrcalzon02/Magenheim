# Magenheim Design Specification

## Core premise

Magenheim adds a geology-to-magic progression layer to Valheim. Geodes appear as biome-specific world resources. Geodes are opened into elemental crystals. Crystal Shaping is its own progression skill. Crystals are refined through increasingly valuable tiers and later socketed into weapons, armor, tools, and equipment through adaptive, non-destructive compatibility rules.

## Permanent progression model

Crystal tiers are ordered and stable:

1. Rough
2. Simple
3. Crystal
4. Advanced
5. Master

Refinement always advances exactly one tier. A rule must explicitly define source tier, destination tier, success chance, skill requirement, station requirement, and failure result. Runtime code must not infer hidden tier jumps.

The intended risk curve increases by ten percentage points of failure per refinement step. For the current foundation the canonical default success rates are therefore 90%, 80%, 70%, and 60% for Rough→Simple, Simple→Crystal, Crystal→Advanced, and Advanced→Master. Balance values remain data/config driven so later tuning does not require rewriting progression code.

## Elemental alignment

Every crystal carries exactly one elemental alignment. Alignment is selected when the crystal is produced from a geode and remains stable through refinement unless a future explicitly defined transmutation mechanic says otherwise. Refinement never silently changes element.

The current bounded implementation restores the Meadows/Earth vertical slice only. Meadows geodes may therefore produce Earth crystals in the initial playable path. Additional biome tables are additive data definitions and must not require changing the refinement engine.

## Geodes

Each biome owns one or more geode definitions. A geode definition contains its biome, weighted crystal outcome table, world-spawn parameters, item identity, and opening requirements. Outcome selection must use independent weighted rolls where multiple crystals may be produced; one crystal's element roll must not implicitly determine every other crystal unless the definition explicitly requests linked rolls.

World-generation integration must be additive and non-destructive: register Magenheim locations/resources without replacing biome vegetation tables, location lists, or third-party spawn definitions. Compatibility should prefer append/patch registration APIs and config-driven enable/disable controls.

## Crystal Shaping skill

Crystal Shaping is a dedicated skill. Valid geode opening and valid refinement attempts can award experience even when the refinement result fails, provided the attempt passed all preconditions. Invalid requests do not award experience.

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

## Current implementation boundary

The first restored code slice is pure domain logic only. It owns crystal identities, tier ordering, validation, and refinement outcomes. It deliberately does not depend on Unity, Valheim, BepInEx, or Jötunn. Runtime adapters will consume this layer later.
