# Underworld content prototype pass — 0.0.89

Request: expand the six-biome content roster using native donors; avoid new animation, save or map systems.

## Delivered source

- 42 non-boss creature prototypes cover all seven canonical entries in each biome. Cloning preserves native skeletons, animation controllers, clips/events, attack items, AI, faction, drops and ZNetView. Registration checks creature components and controller availability independently per donor. No automatic encounters or final-art claim.
- 18 infrastructure prototypes provide a walkway/support/footing palette per biome using native building pieces. Original dimensions, colliders, snap points, support, wear and demolition remain intact. Console review only; no new Hammer recipes.
- 36 named small foliage/stone/root variants extend the existing disposable ecology preview. Dense biomes get four fill objects per support, other biomes two (at most 224 new fill objects in a local patch). No fill colliders or custom lights are added; any donor-authored lights are inherited. Cover rejects submerged terrain, biome mismatches and slopes steeper than approximately 35 degrees. Existing large-landmark palettes and their indices remain unchanged.
- Biome tints use renderer property blocks; shared donor materials and donor prefabs are not modified. Models/textures remain Valheim-owned runtime references.
- Prototype limitations, donor mapping and spawn instructions: `docs/UNDERWORLD_CONTENT_PROTOTYPES.md`. Prior art standards now explicitly defer bespoke rig work during this prototype pass.

## Existing build breaks repaired

The starting tree failed before compiling this content: `UnderworldLayer` had been removed while terrain/context consumers still referenced it; the world-center registrar still used the deleted `UnderworldAnchor`; the split-pillar landmark multiplied two Vector3 values using an unsupported operator. Restored only the two-value layer label, used Unity's Vector3 for the fixed center and Vector3.Scale for the landmark. No transition state or persistence system restored.

The model gate refreshed the two generated catalog files to match the three already-existing Conclave model payloads. No model geometry was authored by this pass.

## Verification

- Canonical roster comparison: all 42 creatures, seven per biome, 60 unique creature/infrastructure prefab names and bounded uniform scales.
- Runtime Release build: zero warnings/errors.
- Full closeout gates: 283 model payloads, 117 icons, existing creature source/texture/review gates, 38,265 Core assertions plus separately reported modules; native patch/reflection/type checks passed on the initial complete build. Model-importer compilation reports existing nullable warnings; runtime compilation is clean.
- Live game startup, actual donor registration counts, inherited animation/socket alignment after scaling, visual color/readability, building-piece placement, multiplayer and world persistence are **not tested**. Generated Sporeling review plates concern existing art, not acceptance of these donor prototypes.

## Remaining content work

Distinct replacement anatomy and authored flora meshes, biome-balanced damage/loot, ambient temperament, final aquatic and rooted-body choices, native spawn integration, and assembled ruin/settlement layouts remain outstanding. The prototype pass intentionally does not implement burrowing, wall adhesion, custom latch/pull fields, new rigs, independent floating-body physics or new save/map systems.

## Installation closeout

Final `closeout.ps1 -Offline` completed successfully after obtaining access to the external mod profile. Installed 0.0.89 and verified all payload hashes and the enabled launcher entry. DLL SHA-256: `DBFAE3B561C293B4E8F87EBB0CB6F9C40FCADE263A97E372F2B0850FF0ADF177`. Original payload backup: `backups/Local-Magenheim-20260921-100206.zip`; final catalog backup: `backups/mods-20260921-100508-198.yml`. Launcher catalog was read back, not visually observed. No game was launched.
