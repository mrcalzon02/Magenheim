# One-run Underworld production forge contract

The remote production run is intentionally broader than the six new stations. It regenerates the
shared Underworld material library, all forty raw/refined material item identities and their icons, all ten physical Crystal weapon chassis and their icons, all
twelve biome-specific Crystal/staff-chassis Underworld derivatives, Rootforged construction, six
biome stations, six progression tools and twenty-four rigged armour sources. The thirty-two elemental staff Blender sources are treated as authoritative manual source art: the forge re-exports all 32 runtime/GLB representations and regenerates all 32 staff icons without inventing a replacement geometry generator. Crystal-tier icons remain verification-only because they are not part of the weapon/placeable rebuild.

## Admission order

1. Pure-Python readiness checks and Core tests.
2. Provision the current free Valheim Dedicated Server managed assemblies plus pinned BepInEx
   5.4.2351 and compile Magenheim.Runtime. A C# API error therefore stops before Blender is
   downloaded or any asset run is spent.
3. Download Blender 5.0.0 from the official Blender host and verify it against the official
   SHA-256 list.
4. Generate and quantitatively gate 25 shared 512px Underworld material families.
5. Author the fourteen previously missing raw-resource inventory models plus all eighteen refined
   material models, export them with shared PBR maps, and regenerate icons for all forty raw/refined material identities.
6. Regenerate Crystal weapons first so derivative weapons inherit the current authoritative
   chassis, UVs and painted 512px atlases; re-export all 32 elemental staves from their existing authoritative Blender sources.
7. Regenerate all twelve Underworld weapon derivatives, Rootforged, stations, tools and armour in dependency order, then regenerate staff icons against the final model catalog.
8. Run global topology, winding/surface continuity, scale, held orientation/grip, icon, texture,
   authored-surface and attach_skin source-rig gates.
9. Render every admitted production model twice: neutral studio and approximate biome-context
   lighting. Render all 24 rigged armour pieces again under an exaggerated articulation stress pose, then build family/contact sheets for human acceptance.
10. Push generated assets to a dedicated production/blender-<run id> branch. The workflow never
   auto-merges generated binaries to main.

## Texture/material standard

Shared Underworld materials own deterministic, tile-safe 512px albedo, roughness and normal source maps.
Luminous families also own localized emission maps. Repeat continuity is machine-gated so projected/repeated
surfaces cannot hide a hard source-map seam, and long Rootforged members keep world-scale UV density instead
of stretching one map across their full 4m/8m length. Blender review/GLB sees the complete material;
the runtime payload now carries owned albedo, normal and packed metallic/gloss references (plus
localized emission where authored). ModelAssets explicitly clears every donor auxiliary map before
applying those exported maps, preventing donor UVs from contaminating the owned mesh. The source
maps remain generator-owned and export content-hashes the runtime copies, preserving the repository's
anti-overwrite invariant.

Crystal weapons remain on their stronger purpose-authored atlas path. Their authored UV unwrap,
occlusion, edge wear, timber/leather/hammered-metal/clouded-crystal painting is not replaced by the
shared Underworld material system.

## Environmental review

Context renders are an art-composition gate, not a claim of in-game acceptance. They approximate
Fungal, Blackwater, Sulfur, Frozen, Fracture and Decay lighting so material separation is examined
under the palette where it will actually live, paired beside a neutral render that exposes cases
where colored environment lighting merely hides a weak texture. Final world placement, real Valheim
shader behavior, particles, weather coupling, station environmental siting, interaction, networking
and save/reload still require the ordinary local acceptance pass.

## Why this is one expensive run

All source/generator/manifest dependencies are checked before Blender. The output is preserved on a
review branch and review plates are retained as a short-lived artifact, so rejecting or accepting
the visual result does not require regenerating it. A merge is a Git operation, not a second Blender
run.


## Wearable armour admission

The production exporter now emits true runtime skin streams for all twenty-four Underworld armour
pieces. Each source retains the canonical Valheim player bone order, individual mesh origins are not
migrated after skin authoring, and every exported vertex carries one to four normalized influences.
ModelAssets reconstructs those streams as SkinnedMeshRenderers under a cloned vanilla attach_skin
donor, while Jotunn BoneReorder performs the final VisEquipment remap when equipped.

The cheap pre-Blender ModelAssetTests include a synthetic attach_skin regression. The post-generation
admission gate requires the full 53-bone contract and weight-stream/vertex-count equality. Final
male/female animation, clipping, multiplayer and save/reload acceptance still require local Valheim
runtime verification and are not inferred from Blender output.


## Geothermal station dependency

The one-run scope includes three owned Sulfurous-Wastes geothermal vent models. They are reviewed as
production environment assets, registered as sparse native Underworld vegetation, and carry real
geothermal trigger/placement volumes. The Furnace Heart Forge placement rule therefore binds to a
loaded vent feature rather than treating the entire Sulfur biome as a free heat source.


## Expanded visual admission

The production review universe is now 150 models: 22 raw material items, 18 refined material items,
10 Crystal weapons, 12 Underworld weapon derivatives, 32 elemental staves, 3 geothermal vents,
17 Rootforged pieces, 6 stations, 6 tools and 24 armour pieces. Rootforged requires joinery revision 4 and the station family requires endgame
detail revision 2 before Blender provisioning. The twelve derivative weapon sources must preserve
their Crystal/staff ancestry and are bounded against their parent chassis before save, so biome
hardware cannot silently change the already-validated held envelope.


## Material-item admission

The eight previously authored Fungal/Blackwater raw material models remain authoritative. A new
32-model material-item author fills the fourteen missing Sulfur/Frozen/Fracture/Decay raw materials
and all eighteen refinement outputs. Processing state is visible in geometry—ore clusters become
bars, loose fibre becomes coils, timber becomes keyed laminates, ice becomes framed lenses,
fracture crystal becomes prisms, and amber/bone become sealed/composite components—rather than
being represented by donor recolours. The new models use shared PBR maps and carry an eight-part
minimum inventory-detail floor. Runtime mappings cover all 22 raw and 18 refined prefabs and fail
closed when an owned representation is missing.

These thirty-two new Blender outputs and their icons are not claimed generated until the manual
production forge is run and its review branch is accepted.
