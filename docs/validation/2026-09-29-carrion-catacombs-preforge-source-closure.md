# Carrion Catacombs pre-forge source closure — 2026-09-29

## Scope

This record closes the dependency-valid source/runtime architecture for the Great Decay Carrion
Catacombs without claiming forged Blender payloads or live Valheim acceptance.

## Implemented authority

- UnderworldGreatDecayCarrionCatacombsCatalog owns sixteen stable room/model families spanning
  Sanctuary, PreservedRuin, TaintedRuin, RootIngress and BlackBloom route states.
- The room program visibly progresses from recognizable funerary construction into rotwood,
  bone, spores and Carrion Amber occupation rather than treating Great Decay as generic caves.
- UnderworldCarrionCatacombsContaminationPolicy maps rooms and passages onto the existing Great
  Decay atmosphere: ordinary decay miasma, hazard floors and Black Bloom where authored.
- Defiant Flesh and Defiant armour remain normal atmosphere resistance sources. The runtime
  Defiant Censer remains the existing 16 m local suppression source; Carrion does not create a
  second contamination meter or special-case immunity.
- Two Sanctuary rooms deliberately provide low-pressure work/recovery geometry where a carried
  Censer can establish a materially clearer local worksite through the normal suppression path.
- CarrionCatacombsRoomRegistrar and CarrionCatacombsLocationRegistrar refuse registration unless
  the Great Decay catalog row is RuntimeReady and all seventeen runtime model payloads exist.
- CarrionCatacombsInteriorRuntime builds deterministic generic topology, dimension-aware spatial
  embedding, authored room instances and physically connected adaptive passage modules.
- CarrionCatacombsEncounterAuthority owns deterministic server-side encounter/resource state.
  Rotling Warrens, Spore Husk Cloister, Graft Warden Hall, Miasma Nave, Carrion Sluice and the
  Corpse Orchard Antechamber use deliberate Great Decay fauna instead of random room flooding.
- Resource rooms use the canonical Great Decay resource vocabulary: Rotwood, Decay Spore,
  Carrion Amber and Bone Gravel.
- The reduced Ossuary Gate exterior, buried same-instance interior and exact Great Decay return
  path are source-complete.
- Plugin lifecycle wiring registers/disposes Carrion room/location registrars and installs the
  Pickable harvest-persistence patch.

## Admission gate

Carrion Catacombs remains **Planned**.

verify-carrion-catacombs-production-contract.py accepts only 0/0/0 generated assets while waiting
for the forge, or a complete 17/17/17 Blender source / GLB / runtime model family. Partial
families fail closed, and RuntimeReady with fewer than seventeen payloads is rejected.

The production path is:

1. rebuild-carrion-catacombs-dungeon.ps1
2. Blender source verification
3. shared model export/verification
4. verify-carrion-catacombs-production-contract.py
5. record-carrion-catacombs-generated-assets.py
6. generated freshness verification
7. promote-carrion-catacombs-runtime.py
8. repeat the production contract after promotion

The promotion script changes only the GreatDecay dungeon row and refuses to run unless the full
seventeen-file family exists in source, GLB and runtime form.

## Current evidence boundary

This execution did not run Blender. No Carrion Catacombs .blend, .glb or .model.json payload is
claimed generated, and no live collision, atmosphere, Defiant/Censer balance, multiplayer
persistence or return-path acceptance is claimed.

Correct current state:

**SOURCE/RUNTIME ARCHITECTURE COMPLETE — PLANNED / FORGE PENDING.**

Live acceptance remains in TESTING.md after legitimate asset forge and RuntimeReady promotion.
