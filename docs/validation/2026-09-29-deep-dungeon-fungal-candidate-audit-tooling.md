# Deep Dungeon Expansion — Fungal first-candidate audit tooling

**Date:** 2026-09-29  
**Source head at checkpoint:** `514f48dabf948c360d167fe54042ceda7db042fd`  
**Authority:** `docs/DEEP_DUNGEON_EXPANSION.md`

## Scope

This checkpoint advances the first ordinary-dungeon admission lane:

**Fungal Forest / Rootwarren identity -> Burial Chambers donor -> `DG_ForestCrypt`**

No dungeon is promoted by this work.

## Implemented

The expanded-vanilla room clone now carries serialized donor provenance captured **before** scaling:

- donor room name;
- donor `Room.m_size`;
- donor `RoomConnection.localPosition` values;
- configured linear scale.

The provenance is stored on `UnderworldVanillaDungeonRoomAuditMetadata` so it survives Unity room
instantiation and can be compared against the actual generated private room.

The developer command now supports:

`magenheim_underworld dde audit`

The live candidate audit writes evidence under:

`BepInEx/config/Magenheim/validation/deep-dungeon-expansion/audits/`

For a loaded expanded-vanilla dungeon it fails closed on:

- non-private room identities;
- a scaled `Room` root;
- missing or incorrectly scaled `Magenheim_DDE_ScaleRoot`;
- `Room.m_size` that does not equal the donor size expanded by the configured scale;
- changed/missing donor connection count;
- `RoomConnection` objects that are no longer direct `Room` children;
- connection-local positions that do not equal the donor positions expanded by the configured scale;
- non-private required-room identities;
- non-Magenheim door prefabs;
- vanilla/non-owning-biome creature bindings;
- vanilla/non-owning-biome reward bindings;
- retained Vegvisirs or Runestones;
- wrong owning Underworld biome registration.

The source prerequisite verifier now requires the donor-provenance and live candidate-audit seams.

The durable plan was also corrected to reflect Valheim's actual connection math: the `Room` root
must remain scale 1.0 while structural child content is uniformly enlarged and raw
`RoomConnection.localPosition` values are expanded by the same factor.

## Claim boundary

This checkpoint establishes **source implementation and read-back presence only**.

It does not claim that:

- DDE-00 executed successfully against the installed Valheim/Jotunn environment;
- DDE-01 donor census passed in-game;
- the Fungal/Burial candidate generated successfully;
- `magenheim_underworld dde audit` has produced a PASS artifact;
- room joins, doors, stairs or AI pathing passed visual/live inspection;
- persistence, multiplayer, economy or performance passed;
- Fungal Forest is RuntimeReady.

The container available to this session could not resolve `github.com`, so the repository Python
verifiers could not be executed locally. No verifier PASS is claimed.

## Next installed-game execution

1. Run the normal Magenheim build and confirm DDE-00 source gate execution.
2. Load Valheim and run `magenheim_underworld donors`; review all five DDE-01 census files.
3. Enable only `CandidateDungeon = fungal_forest` in a disposable validation world.
4. Generate and enter a Burial Chamber derivative.
5. Run `magenheim_underworld dde audit`.
6. Retain the audit artifact and automatic generation artifact.
7. Visually inspect joins, doors, stairs, vertical traversal and AI navigation.
8. Repeat across multiple seeds.
9. Continue with `magenheim_underworld dde snapshot` for save/reload and two-peer evidence.

Only after DDE-00 through DDE-12 have live evidence should the Fungal family be considered for
DDE-13 promotion.
