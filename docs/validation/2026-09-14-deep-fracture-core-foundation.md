# Deep Fracture pure-core foundation — 2026-09-14

## Scope

This record admits the first implementation slice for Magenheim Deep Fractures at the pure-core source level only. It does not admit Valheim runtime generation, prefabs, locations, enemies, networking, persistence, or fresh-world behavior.

## Authority implemented

The source model follows the Deep Fracture design invariants supplied for this development cycle:

- major exploration remains underground and is expressed as large district-scale modules;
- the canonical environment vocabulary contains DF-01 through DF-20, ending in DF-20 The Confluence Heart;
- major piece reuse is bounded to the intended one-to-three-instance range rather than unconstrained procedural repetition;
- scale rules are Small 12-18, Full 24-36, and Grand 40-50 major modules;
- generated module state is separated into structural piece, elemental state, structural damage, occupation, resource state, and connection state;
- enemy composition is separated into creature chassis, elemental alignment, crystal components, encounter role, and territory;
- the principal enemy chassis catalog contains Annoyance Wisp, Geode Crawler, Shardling, Crystal Parasite, Crystal Revenant, Facet Sentry, Stone Sentinel, Crystal Hound, Burrower, Stone Guardian, Crystal Golem, Obelisk Warden, and Deep Colossus.

The implementation reuses the existing `ElementalAlignment` authority rather than creating a second element enum.

## Changed source

- `src/Magenheim.Core/DeepFractures/DeepFractureModel.cs`
- `src/Magenheim.Core/DeepFractures/DeepFractureCatalog.cs`
- `tests/Magenheim.Core.Tests/DeepFractureCatalogTests.cs`
- `tests/Magenheim.Core.Tests/DefinitionAuthorityTests.cs`

## Validation performed

Static source review confirms the catalog contains exactly twenty piece-family definitions and thirteen creature chassis definitions, the scale ranges match the design, DF-01 is constrained to Upper Fracture, DF-20 is constrained to Heart and owns Heart encounter capability, and the plan validator fails closed on unknown pieces, invalid depth placement, duplicate instance identities, excessive reuse, missing entrance, and missing Heart.

The deterministic test source is wired into the existing executable suite through `DefinitionAuthorityTests.Run()`. It includes a valid twelve-module Small fracture, an invalid piece/depth mutation, an excessive-reuse case, and a componentized Storm Crystal Golem encounter.

The current connector execution environment has no .NET SDK and local GitHub resolution is unavailable, so compilation and deterministic test execution are deferred rather than claimed.

## Admission

**Static source admission only.** Runtime, compile, deterministic-suite, worldgen, multiplayer, persistence, and performance acceptance remain open.

## Next exact action

Build current source in the normal .NET/Valheim development profile and execute the deterministic suite. Then implement the deterministic dungeon-layout planner against this pure-core grammar before adding Jotunn/Unity location prefabs.
