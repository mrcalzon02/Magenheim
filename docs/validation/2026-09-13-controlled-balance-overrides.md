# Controlled Balance Override Validation — 2026-09-13

## Scope

This record covers runtime version 0.0.5 and the new controlled definition-override layer.

## Implemented path

1. `default-data/foundation.json` remains the immutable shipped baseline.
2. `MagenheimDefinitionLoader` parses and validates that baseline into `MagenheimDefinitionSet`.
3. `MagenheimBalanceConfig` binds BepInEx configuration entries only for permitted balance fields already represented by the baseline.
4. `MagenheimDefinitionOverrideApplier` copies the baseline with those permitted balance values.
5. Elemental override sets must exactly match the baseline elemental identity set for each geode; adding, removing, or replacing an elemental identity rejects the override.
6. Unknown refinement source tiers and unknown geode IDs reject.
7. The effective snapshot is passed through `MagenheimDefinitionValidator.ValidateAndFreeze`, so normal range, progression, namespace, biome, area, probability, shard, and elemental-weight validation is reapplied.
8. A new deterministic fingerprint is generated from the effective normalized snapshot before `RuntimeServices` is created.

## Preserved invariants

Configuration cannot change crystal tier topology, refinement destinations, required stations, geode identity, biome ownership, prefab identity, guaranteed crystal count, or the set of elemental identities assigned to a geode. The override layer therefore changes balance without becoming a second content-authority mechanism.

## Static review

Source readback verified the core override applier, runtime BepInEx binding layer, plugin bootstrap integration, runtime version 0.0.5, backlog update, and project-state update are committed on `main`.

## Execution boundary

No .NET compiler or Valheim runtime is available in the active execution environment. Compilation, BepInEx configuration binding at runtime, Jötunn startup, and deterministic test execution are therefore not claimed.

## Required next verification

Run the standalone pure-core test harness, compile both core and runtime with warnings as errors, then add deterministic override tests covering unchanged-default fingerprints, changed-balance fingerprints, invalid numeric values, unknown targets, and forbidden elemental-set changes. Server/client fingerprint synchronization must be implemented before any gameplay mutation is enabled.
