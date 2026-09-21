# Local handoff — Underworld creature production

> 2026-09-21 scope correction: the current user requests broad rough prototypes using existing donor skeletons, not new animation systems. [The donor prototype roster](UNDERWORLD_CONTENT_PROTOTYPES.md) governs this pass. Bespoke rig/animation requirements and the earlier single-creature sequencing below remain final-art reference only; they do not block donor-based content expansion.

**Scope:** final local repair, Blender generation, Unity/Valheim installation and runtime testing.  
**Priority:** Fungal Forest Sporeling first. Do not expand to another creature until this benchmark passes.

## Current authoritative inputs

- `docs/UNDERWORLD_CREATURE_ART_RIG_STANDARD.md`
- `tools/generate-underworld-sporeling-textures.py`
- `tools/author-underworld-sporeling.py`
- `tools/verify-underworld-sporeling.py`
- standard Blender wrapper: `tools/blender.ps1`

The Sporeling author defines the geometry and HOST-SWARM-HEXAPOD armature contract. It does **not** yet claim skin weights, completed animation actions, runtime prefab integration, or in-game acceptance.

## Local execution order

1. Run `python tools/generate-underworld-sporeling-textures.py`.
2. Run `tools/blender.ps1 author-underworld-sporeling`.
3. Run `tools/blender.ps1 verify-underworld-sporeling`.
4. Open the generated source blend and visually inspect silhouette, normals, cap/gill intersections, leg clearance, mandible clearance, spore vents, bone placement and creature scale.
5. Repair source-authoring scripts, not only generated binaries.
6. Add authored UVs and wire the five texture families (albedo/normal/roughness plus gill/spore-sac emission) into the Blender materials. Do not use `smart_project`.
7. Skin meshes to the HOST-SWARM-HEXAPOD armature. Verify deformation at all six coxa/femur/tarsus chains, abdomen, head/jaw and fronds.
8. Author at minimum idle, walk/scuttle, turn, alert, bite, hit/stagger, death-spore-puff and emerge actions. Attack damage/release event must coincide with visible contact; SporeFX must fire from the authored socket.
9. Export through the existing Magenheim asset pipeline, create/register the runtime prefab additively, then test spawn/locomotion/combat/death in Fungal Forest.
10. Test multiplayer observation before marking accepted.

## Hard acceptance constraints

Target body envelope is approximately 0.50m long and 0.40m high. Runtime class target is 4k–9k triangles after visual acceptance; preserve the richer editable source. Texture source maps are 1024x1024. Major silhouette anatomy must remain geometry, not painted substitutes. Emission is localized to gills/spore organs. Six legs must visibly articulate; no rigid-body scuttle. Do not substitute a vanilla humanoid skeleton.

The creature is only **authored** until Blender generation and verification succeed. It is only **accepted** after import plus in-game locomotion, attack, death and multiplayer observation.

## Known repair targets

The current author creates the armature hierarchy but does not bind/weight the mesh. Materials currently use procedural colors rather than the generated texture maps. Primitive-generated UV availability is not sufficient evidence of the authored-UV standard and must be explicitly corrected/verified. Animation names are metadata contracts, not completed Blender actions. These are intentional handoff blockers, not completed work.

If a local Blender API incompatibility appears, repair the authoritative Python script and regenerate. Do not patch only the resulting `.blend`.
