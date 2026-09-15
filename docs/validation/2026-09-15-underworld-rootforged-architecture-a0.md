# 2026-09-15 — Underworld Rootforged Architecture A0

Status: **static source validation only; compile/runtime admission deferred**

## Intent

Begin the parallel Underworld architecture/build-system workstream without creating a second runtime authority. The bounded A0 slice establishes a durable execution plan plus a pure Core catalog for the first eleven Understone, Worldroot and Rootforged structural pieces.

## Starting authority

- Repository: `mrcalzon02/Magenheim`
- Branch: `main`
- Reconciled remote baseline before mutation: `0d5ea597c40fb820f6d54c8f1668c16a898e5609`
- Existing Underworld core authority from that baseline remains untouched and authoritative.
- Existing `CrystalArchitectureRegistrar` / visuals / icons are the runtime implementation reference; A0 does not register new runtime pieces.

## Durable planning authority

`docs/UNDERWORLD_ARCHITECTURE_IMPLEMENTATION_PLAN.md` defines the parallel Rootforged construction track:

- Understone anchors;
- Worldroot carries;
- iron spans;
- silver stabilizes;
- large-format 2m/4m/8m/12m+ construction;
- explicit snap/collider requirements;
- phased A0-A7 implementation and validation;
- a reference-hall acceptance test for monumental root/iron/stone architecture.

`IMPLEMENTATION_PLAN.md` links this workstream into the main project plan without making it a second Underworld implementation stack.

## A0 Core authority

`src/Magenheim.Core/Underworld/UnderworldArchitectureDefinitions.cs` adds:

- build-piece kind and tier identities;
- validated dimensions, station and recoverable resource costs;
- Magenheim-owned build IDs and runtime prefab names;
- stable resource identities for future Understone and Worldroot Timber registration;
- duplicate/invalid-data rejection;
- order-independent SHA-256 architecture fingerprinting;
- an initial eleven-piece structural catalog:
  - Understone Foundation 2x2m;
  - Great Understone Column Plinth;
  - Worldroot Beams 2m / 4m / 8m;
  - Worldroot Pillars 2m / 4m / 8m;
  - Iron-Banded Worldroot Beam 4m;
  - Rootforged Arch Ribs 4m / 8m.

No Silverbound pieces are admitted in A0; that tier remains deliberately deferred.

## Deterministic test source

`tests/Magenheim.Core.Tests/UnderworldArchitectureTests.cs` covers:

- canonical eleven-piece count and required scale families;
- deferred Silverbound tier;
- order-independent fingerprints;
- fingerprint drift when a gameplay-significant cost changes;
- duplicate build ID rejection;
- foreign ID/prefab rejection;
- invalid dimension rejection;
- invalid/duplicate resource-cost rejection.

`DefinitionAuthorityTests.cs` invokes the architecture suite through the existing deterministic Core harness.

## Static verification

Repository-source inspection establishes that:

- the new Core authority imports no Unity, Valheim, BepInEx or Jötunn APIs;
- every initial build ID is under `magenheim.underworld.build.*`;
- every planned runtime prefab is under `Magenheim_Underworld_*`;
- resource identities are stable and no temporary runtime item is registered to fake a recipe dependency;
- catalog inputs are frozen after validation;
- fingerprint inputs are sorted before hashing;
- A0 does not mutate runtime registration, worldgen, progression state or foreign content.

## Deferred validation

This connector execution does not provide the normal local Valheim/.NET build environment, so compilation and test execution are not claimed here.

Required next gates:

1. compile `Magenheim.Core` with warnings as errors in the normal development environment;
2. run the full `Magenheim.Core.Tests` harness and record the new assertion count;
3. repair any compile/test defect at the authoritative Core source;
4. integrate the architecture catalog/fingerprint into the canonical Magenheim/Underworld definition authority before gameplay-significant runtime progression depends on it;
5. implement real Understone and Worldroot resource registration through the Underworld resource framework;
6. begin A1 owned procedural visuals, icons, colliders, snap layouts and additive Hammer registration;
7. runtime-validate the minimum monumental hall frame before expanding to A2.

## Admission

- Durable implementation plan: prepared for commit.
- A0 pure source: prepared for commit.
- Static validation: passed by source inspection.
- Compile validation: deferred.
- Deterministic test execution: deferred.
- Runtime registration: intentionally absent in A0.
- Runtime validation: not applicable yet.
- Production admission: **not admitted**.
