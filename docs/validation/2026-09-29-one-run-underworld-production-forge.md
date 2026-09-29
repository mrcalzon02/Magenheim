# One-run Underworld production forge contract

The remote production run is intentionally broader than the six new stations. It regenerates the
shared Underworld material library, all ten physical Crystal weapon chassis and their icons, the
Worldroot Crystal-derived weapons, Rootforged construction, six biome stations, six progression
tools and twenty-four rigged armour sources. The thirty-two elemental staff Blender sources are treated as authoritative manual source art: the forge re-exports all 32 runtime/GLB representations and regenerates all 32 staff icons without inventing a replacement geometry generator. Crystal-tier icons remain verification-only because they are not part of the weapon/placeable rebuild.

## Admission order

1. Pure-Python readiness checks and Core tests.
2. Provision the current free Valheim Dedicated Server managed assemblies plus pinned BepInEx
   5.4.2350 and compile Magenheim.Runtime. A C# API error therefore stops before Blender is
   downloaded or any asset run is spent.
3. Download Blender 5.0.0 from the official Blender host and verify it against the official
   SHA-256 list.
4. Generate and quantitatively gate 23 shared 512px Underworld material families.
5. Regenerate Crystal weapons first so derivative weapons inherit the current authoritative
   chassis, UVs and painted 512px atlases; re-export all 32 elemental staves from their existing authoritative Blender sources.
6. Regenerate Worldroot derivatives, Rootforged, stations, tools and armour in dependency order, then regenerate staff icons against the final model catalog.
7. Run global topology, winding/surface continuity, scale, held orientation/grip, icon, texture,
   authored-surface and attach_skin source-rig gates.
8. Render every admitted production model twice: neutral studio and approximate biome-context
   lighting. Build family contact sheets for human acceptance.
9. Push generated assets to a dedicated production/blender-<run id> branch. The workflow never
   auto-merges generated binaries to main.

## Texture/material standard

Shared Underworld materials own deterministic 512px albedo, roughness and normal source maps.
Luminous families also own localized emission maps. Blender review/GLB sees the complete material;
the current lightweight runtime model payload retains the authored albedo plus metallic/roughness/
emission scalar data, because ModelAssets intentionally does not yet carry normal/roughness texture
slots. The source maps are kept outside the packaged runtime texture directory; export packs them
and content-hashes the runtime copies, preserving the repository's anti-overwrite invariant.

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
