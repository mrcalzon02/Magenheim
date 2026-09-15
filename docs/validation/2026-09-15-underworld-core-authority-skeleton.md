# 2026-09-15 — Underworld Core Authority Skeleton

Status: **static source validation only; compile/runtime admission deferred**

## Intent

Begin the first bounded implementation slice from `docs/UNDERWORLD_IMPLEMENTATION_PLAN.md` without creating a parallel runtime authority. The slice establishes pure Core models for deterministic Underworld world identity, Nowhere King unlock eligibility, biome/boss/Deepstone relationships, progression prerequisites and a deterministic Underworld component fingerprint.

## Starting authority

- Repository: `mrcalzon02/Magenheim`
- Branch: `main`
- Reconciled implementation baseline before mutation: `aa30cf91444f07c4f6198cc221998f3372a790b3`
- Concurrent advancement from the prior planning commit affected only `src/Magenheim.Runtime/DeepFractureCreatureRegistrar.cs` and `src/Magenheim.Runtime/DeepFractureLegacyCoreBinder.cs`; those changes are preserved and outside this slice.

## Authoritative changes

- `src/Magenheim.Core/Underworld/UnderworldDefinitionSet.cs`
  - adds schema-versioned pure Underworld biome, boss and Deepstone definitions;
  - validates Magenheim-owned namespaces;
  - freezes input collections;
  - validates one primary boss per biome and one matching Deepstone per boss;
  - validates boss prerequisites against known identities;
  - rejects self-dependencies and progression cycles;
  - derives a deterministic SHA-256 component fingerprint independent of source collection order.
- `src/Magenheim.Core/Underworld/UnderworldWorldIdentity.cs`
  - derives a deterministic logical Underworld identity and seed fingerprint from parent world identity plus parent seed;
  - exposes a deterministic 32-bit derived seed for a future runtime adapter;
  - consumes `DarkThroneEncounterSnapshot` directly for post-Nowhere-King unlock eligibility instead of introducing a duplicate completion flag.
- `tests/Magenheim.Core.Tests/UnderworldDefinitionTests.cs`
  - defines the six-biome/six-boss/six-Deepstone progression fixture from the durable plan;
  - covers order-independent fingerprints, gameplay-significant fingerprint changes, namespace rejection, duplicate rejection, invalid references, progression-cycle rejection, deterministic derived world identity and Nowhere King unlock behavior.
- `tests/Magenheim.Core.Tests/DefinitionAuthorityTests.cs`
  - adds the Underworld test suite to the existing deterministic Core harness.

## Authority boundary

This slice intentionally does **not** register an Underworld biome, world, boss, Deepstone, item or runtime prefab. It also does not create an independent JSON loader.

The new `UnderworldDefinitionSet` is a component authority intended to be embedded into Magenheim's existing `MagenheimDefinitionSet` / `MagenheimDefinitionValidator` fingerprint path. Until that integration occurs, the Underworld component fingerprint is not by itself sufficient to authorize multiplayer runtime mutations.

## Static verification

Verified by source inspection in the repository connector:

- new Core files contain no Unity, Valheim, BepInEx or Jötunn dependencies;
- unlock eligibility consumes the existing durable Dark Throne lifecycle and requires `Defeated`;
- identifiers are constrained to `magenheim.underworld.*` / `Magenheim_Underworld_Trophy_*` ownership;
- progression references are validated before cycle traversal;
- fingerprint inputs are sorted before hashing so collection ordering cannot change authority;
- no runtime registration or foreign worldgen mutation is introduced.

## Deferred validation

The available execution container has no `dotnet` or `csc` compiler, and the project does not use GitHub Actions. Therefore compilation and deterministic test execution are **not claimed** in this record.

Required next gates:

1. compile `Magenheim.Core` with warnings as errors in the normal development environment;
2. run `tests/Magenheim.Core.Tests` and record the new assertion total;
3. repair any compile/test defect at the authoritative source before schema integration;
4. embed the validated Underworld component into the canonical Magenheim definition loader/fingerprint rather than introducing a second authority;
5. only after authority integration begin runtime world-transition experiments.

## Admission

- Source design: implemented.
- Static validation: passed by inspection.
- Compile validation: deferred.
- Deterministic test validation: deferred.
- Runtime validation: not applicable to this slice.
- Production admission: **not admitted**.
