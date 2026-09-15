# 2026-09-15 — Underworld Composite Authority Slice

Status: **source-complete; compile/test execution not claimed in connector environment**

## Intent

Advance the highest-priority dependency-valid Underworld framework slice after the Rootforged architecture A0 catalog: prevent biome/boss/Deepstone authority and architecture authority from becoming independent multiplayer/gameplay truth streams.

## Change

Added `src/Magenheim.Core/Underworld/UnderworldAuthorityComposition.cs`.

`UnderworldAuthorityComposer` now combines a validated `UnderworldDefinitionSet` and validated `UnderworldArchitectureDefinitionSet` into one `UnderworldAuthoritySnapshot` with one deterministic SHA-256 fingerprint. The composite fingerprint includes both component fingerprints and a composite schema identity. Component fingerprints must be canonical lowercase SHA-256 hex and component schema versions must match their current validators.

This is the canonical envelope runtime Underworld mutation should consume when Underworld multiplayer/runtime admission begins. It does not replace the component validators; it composes them and prevents a future runtime from synchronizing construction separately from biome/boss/Deepstone progression.

Added `tests/Magenheim.Core.Tests/UnderworldAuthorityCompositionTests.cs` and wired it into `DefinitionAuthorityTests.Run()`. Source tests cover deterministic composition, architecture-cost drift changing the composite fingerprint, content/Deep-Boon drift changing the composite fingerprint, and null-component fail-closed behavior.

## Authority preservation

- `Magenheim.Core` remains free of Unity/Valheim/BepInEx/Jötunn dependencies.
- No second network handshake, persistence engine, worldgen authority or runtime registrar was introduced.
- Existing `UnderworldDefinitionSet` and `UnderworldArchitectureDefinitionSet` remain independently validated component authorities.
- Runtime mutation remains unimplemented and therefore unclaimed.

## Validation boundary

GitHub source read/write confirms the files are present on authoritative `main`. This execution environment does not provide the normal local Magenheim/Valheim build toolchain, so compilation and deterministic test execution remain required before runtime work may depend on this slice.

## Next dependency-valid slice

1. Compile `Magenheim.Core` and execute the full deterministic harness in the normal development environment; repair any defect at source.
2. Feed `UnderworldAuthoritySnapshot.Fingerprint` into the existing Magenheim gameplay-authority composition/handshake rather than creating an Underworld-specific network handshake.
3. Define and register real `Understone` and `Worldroot Timber` resources through the Underworld resource framework.
4. Begin A1 procedural visuals, explicit snap layouts, colliders and additive Hammer registration for the eleven Rootforged A0 pieces.
