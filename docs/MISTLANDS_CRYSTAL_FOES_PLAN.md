# Magenheim — Mistlands Crystal Foes Implementation Plan

Status: Durable implementation authority
Scope: Mistlands crystal ecology and reusable surface creature spawners only
Excludes: The Dark Throne structure, the Nowhere King, his encounter, equipment, trophy, and rewards. Those are governed by `NOWHERE_KING_PLAN.md`.

## Intent

Extend Magenheim's existing crystal-creature ecosystem into the Mistlands through rare, additive, non-destructive surface spawning. The system must reuse Magenheim creature authorities and avoid replacing vanilla Mistlands biomes, locations, spawn tables, or foreign prefabs. It must also provide a reusable spawner architecture that can later be consumed by special Magenheim locations without coupling ordinary Mistlands ecology to those locations.

## Design invariants

Magenheim owns every spawner prefab and every spawn definition introduced by this plan. Vanilla and third-party spawn authorities remain untouched. Placement is additive. Existing crystal creature prefabs are referenced rather than cloned unless a genuinely different creature variant requires its own authoritative prefab. The spawn system must be configurable, deterministic where worldgen requires determinism, multiplayer-safe, and able to survive save/load without multiplying persistent spawners.

The ordinary Mistlands crystal ecology remains independent of the Dark Throne. A world may contain ordinary Magenheim crystal spawners whether or not a Dark Throne has generated. No boss encounter state may be required for these spawners to function.

## Authoritative implementation structure

Create a compact definition-driven system rather than one class per creature or one worldgen hook per variant.

- `CrystalCreatureSpawnDefinition.cs`: authoritative weighted spawn-entry and spawner-profile definitions.
- `CrystalCreatureSpawnerRegistrar.cs`: creates/registers Magenheim-owned spawner prefabs from profiles.
- `MistlandsCrystalSpawnConfig.cs`: frequency, limits, creature weights, respawn timing, activation radius, group size, star-level policy, and enable/disable controls.
- `MistlandsCrystalWorldgenRegistrar.cs`: additive Mistlands placement authority for persistent surface spawners.
- `CrystalCreatureSpawnerRuntime.cs`: server-authoritative runtime population and respawn behavior where vanilla spawner behavior is insufficient.
- `CrystalCreatureSpawnerPersistence.cs`: only if vanilla/ZDO persistence cannot express the required durable state without duplication.

Preferred prefab families:

- `Magenheim_CrystalSpawner_Lesser`
- `Magenheim_CrystalSpawner_Mixed`
- `Magenheim_CrystalSpawner_Guardian`
- `Magenheim_CrystalSpawner_DarkThrone`

The Dark Throne profile exists as a reusable profile boundary, but its placement and encounter suspension rules belong to `NOWHERE_KING_PLAN.md`.

## Creature distribution

`Lesser` is the normal rare Mistlands intrusion profile. It selects from Magenheim's smaller crystal creatures with weighted probabilities and conservative group sizes.

`Mixed` is substantially rarer and may combine lesser creatures with mid-tier crystal threats. It should communicate that the crystal ecosystem is becoming established without turning ordinary Mistlands exploration into a replacement Magenheim biome.

`Guardian` is reserved for exceptional placements and high-threat contexts. It must not become common random surface clutter.

Creature lists must resolve from registered Magenheim creature identities at runtime. Missing optional creature definitions fail locally for that entry and are logged; they must not invalidate the entire Mistlands spawn system.

## Placement

Placement is Mistlands-only by default and should be rare enough that discovering a crystal nest remains notable. The initial implementation should expose frequency and world-count/density controls rather than baking final balance into code.

Placement validation must reject unsuitable terrain, submerged placement, severe geometry intersection, protected/no-build situations where applicable, and positions too close to incompatible Magenheim special locations. It should prefer natural clearings, rock shelves, ruined margins, and other spaces where a crystal infestation can be visually understood.

Do not edit vanilla Mistlands location definitions or biome spawn arrays. Register Magenheim-owned worldgen content through supported Jotunn/Valheim boundaries.

## Runtime and multiplayer authority

Spawner decisions are authoritative on the server/owning simulation. Clients render creatures and effects but do not independently roll population outcomes. The implementation must avoid each peer creating its own wave.

Spawner state must tolerate players entering and leaving activation range, server restart, world save/load, and creature cleanup. Respawn timers must not reset into immediate duplication after reload. Persistent creature identities should use normal Valheim networking/persistence wherever possible rather than inventing a second save format.

## Visual language

Spawner sites should read as geological crystal intrusions rather than magical portals. Use small fractured basalt/stone formations, embedded crystal growth, shards, and alignment-colored emission derived from Magenheim's existing elemental palette. Effects must remain restrained enough to coexist with Mistlands fog and existing visual density.

The site itself may hint at the elemental family likely to emerge, but the definition layer controls whether profiles are single-alignment, weighted mixed-alignment, or unrestricted.

## Compatibility boundaries

Never mutate shared foreign creature prefabs. Never replace Mistlands biome data. Never require Jewelcrafting. Jewelcrafting equipment integration is irrelevant to creature spawning and must remain outside this dependency tree.

Creature references must come from Magenheim's authoritative creature registry/registrars. If future compatibility allows third-party creatures in a profile, those entries must be optional, detected at runtime, and additive.

## Implementation sequence

1. Reconcile the current crystal-creature registry and identify the authoritative prefab names, tiers, AI assumptions, drops, and network components of all eligible lesser/mid-tier creatures.
2. Implement `CrystalCreatureSpawnDefinition` and collapse creature selection into weighted profiles.
3. Implement and register the reusable spawner prefabs.
4. Add server-authoritative spawn execution, activation limits, living-creature accounting, and respawn timing.
5. Add Mistlands-only additive worldgen placement and config.
6. Add geological/crystal visual dressing without cloning creature logic.
7. Verify save/load and dedicated-server behavior, especially duplicate-spawner and duplicate-wave failure modes.
8. Expose the `DarkThrone` profile for consumption by the separate Nowhere King location authority.

## Acceptance criteria

A fresh world can generate rare Magenheim crystal spawner sites in Mistlands without removing or modifying vanilla locations. The same world seed produces stable worldgen placement where the underlying API supports deterministic placement. A dedicated server produces one authoritative population rather than one roll per peer. Leaving and returning does not duplicate creatures. Saving/restarting does not duplicate persistent spawners or reset every respawn timer. Missing optional creature entries do not break other entries. Config can disable the feature completely and can independently tune frequency and profile weights.

The implementation is accepted only after source/runtime registration verification and, when the environment permits, a disposable-world Mistlands traversal plus save/reload and multiplayer test.

## Relationship to the Dark Throne

The Nowhere King plan may consume `Magenheim_CrystalSpawner_DarkThrone` and the same definition/runtime machinery. It must not fork or copy this implementation. Special encounter behavior such as suspending throne-area spawners when the boss engages belongs to the encounter authority, not to the general Mistlands spawner system.
