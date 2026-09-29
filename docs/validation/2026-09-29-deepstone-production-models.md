# Deepstone Conclave production model pass — 2026-09-29

Status: **runtime model assets implemented and remote-read-back verified on main; live visual acceptance still required.**

## Defect

The Conclave previously instantiated the same `underworld-standing-stone` mesh and the same
single material six times. The earlier carving pass improved that shared slab, but the architecture
made genuine biome-specific Deepstones impossible. Live testing also reported flat/untextured grey
stonework at the centre.

The centre additionally loaded the generic standing-stone mesh for the Descent Monolith even though
`underworld-descent-monolith` already existed as a separate authored asset.

## Runtime model replacement

The six canonical stones now own separate multi-part runtime model assets:

- `underworld-deepstone-bloom`
- `underworld-deepstone-tide`
- `underworld-deepstone-cinder`
- `underworld-deepstone-rime`
- `underworld-deepstone-fracture`
- `underworld-deepstone-decay`

Each keeps one collision-bearing stone core and uses non-colliding detail parts for silhouette and
surface identity. Every material explicitly references an existing Magenheim-authored UV texture
family rather than relying on stripped donor projection.

### Bloom

Weathered stone, timber-textured root tendrils, crystal-textured glowcap growths, green emissive
glyph and restrained green local light.

### Tide

Wet dark stone, bone/shell-textured barnacle growth, crystal pearl nodules, liquid-textured drips,
cyan emissive glyph and cyan local light.

### Cinder

Dark basalt, jagged basalt projections, separate ember-fissure geometry, orange-red emissive glyph
and stronger hot local light.

### Rime

Pale stone, separate ice/crystal edge growth and base icicles, cold blue-white emissive glyph and
cold local light.

### Fracture

Dark slate stone, detached/floating crystal shard geometry, explicit rift-seam geometry, violet
emissive glyph and violet local light.

### Decay

Bruised/dirty stone, timber-textured invasive roots, amber crystal sacs, bone-textured ribs/spurs,
yellow-green emissive glyph and sickly local light.

## Asset complexity

Remote read-back currently reports:

| Deepstone | Parts | Triangles | Collider parts |
|---|---:|---:|---:|
| Bloom | 4 | 808 | 1 |
| Tide | 5 | 1048 | 1 |
| Cinder | 4 | 584 | 1 |
| Rime | 4 | 584 | 1 |
| Fracture | 4 | 604 | 1 |
| Decay | 5 | 824 | 1 |

The old shared standing stone was approximately 250 triangles with one material.

All referenced `surface-*-authored.png` texture files are present, all vertex/normal/UV streams
match, triangle indices are valid, and every new model has one local light plus at least one
emissive material.

## Runtime binding

`UnderworldWorldCenterRegistrar` now maps each canonical Deepstone id to its own model id and world
scale, and loads the model through the full multi-part `ModelAssets.Load` path. It no longer loads
one shared Deepstone mesh/material.

The centre Descent Monolith now explicitly loads `underworld-descent-monolith`.

## Visible progression state

`UnderworldDeepstonePresentationRuntime` projects—but never mutates—the authoritative persistent
Deepstone state. It clones the model materials before changing emission so cached materials cannot
leak state between stones.

Unawakened stones retain a restrained fraction of their authored emission/light. A stone with both
its trophy mounted and Deep Boon unlocked brightens its emission and local light, with only a small
light pulse. The state is reconstructed from `UnderworldDeepstoneRuntime`; presentation does not
grant progression.

## Editable-source recovery

`tools/materialize-underworld-deepstone-sources.py` reconstructs exact editable Blender scenes from
the six shipped runtime payloads, including UVs, material-family textures, collider metadata and
runtime-light metadata. The current execution environment does not contain Blender, so this pass
does **not** claim the six new .blend files have been materialized or re-exported here. The
game-consumed runtime 3D model assets themselves are committed and bound.

## Validation boundary

Static verification proves model structure, texture references and runtime binding. It does not
prove final appearance under Valheim's shaders, Underworld fog, LOD/camera distance, save/reload or
multiplayer. Complete `TESTING.md -> Deepstone Conclave model / material acceptance` before
claiming live visual completion.
