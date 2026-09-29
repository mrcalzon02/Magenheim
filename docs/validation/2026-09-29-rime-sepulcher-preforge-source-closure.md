# Rime Sepulcher pre-forge source closure — 2026-09-29

## Scope

This record closes the dependency-valid **source/runtime architecture** for the Frozen Caverns
Rime Sepulcher without claiming forged assets or live Valheim acceptance.

## Implemented authority

- UnderworldFrozenRimeSepulcherCatalog owns sixteen stable room/model families and route semantics:
  Shelter, ClearGallery, FrostField, WhiteoutChoke and IceShear.
- UnderworldRimeSepulcherExposurePolicy maps room and passage routes onto the existing Frozen
  Underworld atmosphere authority. Shelter/clear routes can locally suppress obscuration while
  pressure routes reuse Deep Fog/Whiteout and the ordinary resistance pipeline.
- RimeSepulcherRoomRegistrar registers the authored theme only when the Frozen dungeon catalog row
  is RuntimeReady and all seventeen runtime model payloads exist.
- RimeSepulcherInteriorRuntime builds deterministic topology plus the dimension-aware spatial plan,
  instantiates the authored rooms and then assembles physical passage modules between them.
- RimeSepulcherPassageAssembler derives each passage exposure state from its two endpoint room
  definitions and attaches the normal local weather override; there is no parallel dungeon-only cold
  damage system.
- RimeSepulcherEncounterAuthority owns deterministic server-side creature/resource population,
  cleared encounter identities and harvested pickup identities through ZDO persistence.
- Encounter dressing is room-role aware. Frost Tick Niche, Iceblind Hunt, Cryolith Guard,
  White Silence Antechamber and pressure routes use Frozen Caverns fauna deliberately; ordinary
  chambers are not filled with random mobs.
- Resource rooms use only canonical Frozen Caverns resources: Rimewood, Clear Ice and Rimesilver.
- The entrance shell, buried same-instance interior, entrance/return portal pairing and Frozen-only
  location registrar are source-complete.
- RimeSepulcherPickablePersistencePatch records one-shot pickup harvests on the owning server.
- Plugin lifecycle wiring registers/disposes the room and location registrars and installs the
  persistence patch.

## Admission gate

Rime Sepulcher remains **Planned**.

verify-rime-sepulcher-production-contract.py accepts only a complete generated family state:
0/0/0 while waiting for the forge, or 17/17/17 Blender source / GLB / runtime model payloads.
A partial family fails closed. RuntimeReady with anything less than 17/17/17 also fails.

The production path is:

1. rebuild-rime-sepulcher-dungeon.ps1
2. Blender source verification
3. model export and shared model verification
4. verify-rime-sepulcher-production-contract.py
5. record-rime-sepulcher-generated-assets.py
6. generated freshness verification
7. promote-rime-sepulcher-runtime.py
8. repeat the production contract after promotion

The promotion script changes only the Frozen Caverns dungeon row and refuses to run unless all
seventeen source, GLB and runtime payloads exist.

## Current evidence boundary

This execution did **not** run Blender and does not claim that any Rime Sepulcher .blend, .glb
or .model.json payload was generated. It also does not claim live placement, collision,
whiteout readability, multiplayer persistence or return-path acceptance.

Therefore the correct current state is:

**SOURCE/RUNTIME ARCHITECTURE COMPLETE — PLANNED / FORGE PENDING.**

Live acceptance remains in TESTING.md and may begin only after the complete asset family is forged,
verified and the catalog has legitimately promoted to RuntimeReady.
