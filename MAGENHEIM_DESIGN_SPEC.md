# Magenheim Design Specification

## Core fantasy

Magenheim makes magic begin as geology. Biomes surface geodes as discoverable world objects. Geodes crack into elemental Rough Crystals, which are shaped through Simple, Crystal, Advanced, and Master grades. Crystal Shaping is a permanent player skill and refinement becomes progressively more difficult. Equipment can accept crystals through a socket system whose bonuses adapt to the item category rather than requiring destructive replacement of foreign items.

## World-generation invariants

Worldgen integration is additive. Magenheim may add its own locations, vegetation, clutter, prefabs, and metadata, but must not delete, rewrite, disable, reorder, or otherwise mutate vanilla or third-party worldgen entries merely to obtain compatibility.

Every Magenheim worldgen registration uses a stable namespaced key beginning with `magenheim.`. Before an addition is executed, integration code must observe existing registrations and produce a plan. A conflicting key is skipped by default and diagnosed. The planner operates on snapshots and never mutates observed collections.

Biome area is treated as an explicit flags contract. Supported values are `Median`, `Edge`, and `All` (`Median | Edge`). `None`, negative/unknown bits, and values outside the known mask are invalid. Configuration determines whether invalid values are rejected, clamped to known bits, or replaced by `All`; the default is reject. A clamp that produces `None` is still invalid.

Registration timing follows the host API. Additions must be performed once per registration lifecycle and guarded against duplicate injection. Observing a new world load does not grant permission to duplicate already-added custom content. Any future behavior that intentionally modifies vanilla content requires a separately documented feature and must never be smuggled through the compatibility layer.

## Compatibility architecture

Compatibility has three layers. The pure planning layer validates Magenheim definitions and compares them with observed registrations. The adapter layer translates approved additions to Jötunn/Valheim types. The execution layer performs only approved Magenheim additions and records what it attempted. This separation makes conflicts inspectable and keeps foreign data out of mutation paths.

Configuration is conservative by default:

- mode: `additiveOnly`
- duplicate registration: `skip`
- invalid area: `reject`
- mutate vanilla entries: `false`
- mutate foreign entries: `false`
- unknown integration owner: treat as foreign
- diagnostics: enabled

Compatibility configuration may make Magenheim less intrusive, but cannot enable destructive foreign mutation. A future explicit interoperability module may read another mod's public API and add Magenheim data through that API, provided the other mod owns the mutation and the integration remains opt-in.

## Geodes and crystal progression

Each biome can define one or more geode families and weighted elemental results. Meadows begins with an Earth-dominant geode slice. Each cracked geode produces at least one Rough Crystal; additional drops are independent rolls so quantity and element selection remain separable. Weighted tables must sum to a positive total and may be normalized mathematically rather than requiring exactly 100.

Crystal grades are Rough, Simple, Crystal, Advanced, and Master. Refinement attempts use Crystal Shaping skill, station requirements, recipe inputs, and a grade-specific failure chance. Failure increases cumulatively by 10 percentage points for each higher refinement step unless a data definition explicitly supplies a validated replacement curve. Master is the most failure-prone grade and grants the strongest elemental contribution.

Failure behavior must be data-driven and explicit about whether input is destroyed, downgraded, or converted to shards. No integration may silently duplicate output after a failed transaction.

## Adaptive equipment socketing

Sockets are attached through Magenheim-owned metadata or sidecar state rather than by replacing the underlying foreign item definition. The compatibility system classifies an item by observable capabilities and category, then selects an elemental effect profile appropriate to weapons, armor, shields, tools, utility items, or other supported equipment.

Unknown equipment remains usable. If classification cannot be made safely, socketing is refused with a diagnostic rather than mutating or cloning the foreign item. Existing item stats remain authoritative; Magenheim adds calculated modifiers through its own effect layer.

## Networking and authority

Worldgen definitions, refinement outcomes, socket changes, item consumption, item grants, and skill experience are authoritative transactions. Client presentation may predict or preview, but persisted changes require server-approved state. Compatibility fingerprints should cover gameplay-significant definitions so mismatched peers can be rejected or warned before world state diverges.

## Acceptance principle

No feature is complete because a source file exists. Completion requires observed evidence appropriate to the layer: deterministic core tests for pure logic, compilation for adapters, controlled runtime loading for registration, and disposable-world generation checks for worldgen. Valuable saves are never the first validation target.
