# Installed vanilla stations versus Magenheim 0.0.168

The current benches explain their crafting processes more clearly than the
previous orb/hoop assemblies. They still do not match vanilla's material and
shape language. The comparison identifies why: their detail is concentrated in
many small separate meshes and procedural surface variation, while vanilla
concentrates it in deliberately shaped silhouettes and painted, object-specific
texture atlases. More components and larger textures have not closed that gap.

## Evidence and limits

Five prefabs were read from this installation's SoftRef manifest and native
bundles: Forge, Stonecutter, Artisan Table, Black Forge and Galdr Table. Highest
LOD renderers in the active new/intact state were isolated; inactive worn/broken
states and lower LODs were excluded. The Artisan model uses a skinned renderer
and is shown in its extracted bind pose. These are original installed assets,
not replicas or images found online. Native copies remain development-only in
`dist/vanilla-station-comparison` and are not included in the mod package.

Native and current authored models were rendered at 768px with the same camera
angle, frame fitting, neutral background and review lights. Native materials were
approximated from their diffuse/tint, metallic/smoothness, emission and normal
maps. Native shader vertex noise, weather, animated liquids, particles and game
lighting were not reproduced. The plates compare authored shapes and texture
content; they are not proof of final in-game shading or gameplay acceptance.
Models are fitted individually to each image frame. Equal image size does not
mean equal world size. The measured world dimensions below preserve that distinction.

## Actual geometry and footprint

Dimensions are width × depth × height in metres. Mesh count refers to visible
MeshRenderer/SkinnedMeshRenderer objects, not physical parts modeled inside them.
One renderer may use several material slots and require multiple rendering passes.
These counts alone are not an FPS or draw-call measurement.

| Station | Triangles | Mesh renderers | Material slots / distinct authored materials | Dimensions (m) |
|---|---:|---:|---:|---|
| Vanilla Forge | 1,748 | 1 | 2 | 1.87 × 1.17 × 1.25 |
| Vanilla Stonecutter | 2,652 | 1 | 1 | 3.28 × 1.63 × 2.08 |
| Vanilla Artisan Table | 7,589 | 1 (skinned) | 2 | 1.90 × 2.76 × 2.59 |
| Vanilla Black Forge | 4,203 | 1 | 2 | 3.66 × 1.78 × 1.16 |
| Vanilla Galdr Table | 2,305 | 1 | 1 | 3.32 × 3.56 × 2.73 |
| Mycelial Bench | 10,008 | 94 | 4 | 3.22 × 1.87 × 1.99 |
| Tidal Basin | 4,372 | 76 | 5 | 3.04 × 2.69 × 1.75 |
| Furnace Heart Forge | 9,860 | 95 | 4 | 3.64 × 2.60 × 3.04 |
| Silence Table | 6,952 | 78 | 4 | 3.03 × 1.77 × 2.19 |
| Anchor Forge | 5,208 | 64 | 4 | 3.80 × 2.56 × 3.08 |
| Crown Reliquary | 9,724 | 97 | 4 | 3.60 × 2.84 × 3.01 |

The Furnace has 2.35 times the Black Forge's triangles, and Crown has 4.22 times
Galdr's. Silence is slightly below Artisan's geometry count. There is no single
vanilla polygon ceiling: complexity must serve the station's silhouette and
working mechanism. Our 64–97 renderer objects also introduce substantially more
submission overhead opportunities than vanilla's consolidated geometry. Actual
rendering cost requires a live profiler; no performance improvement is claimed.

## Texture content, density and material language

Forge, Stonecutter, Artisan and Galdr use 256px principal diffuse atlases. Black
Forge's principal diffuse atlas is only 128px; its separate lava map is 32px.
Our worked station maps are stored at 512px, but their generated field is 128px
expanded into 4px blocks. Neither file resolution nor block size alone measures
world-space texel density: native atlases allocate different areas to different
parts, while ours generally repeat a 1.5m dimensional tile.

The native Forge atlas visibly contains directional timber grain, bright cut
ends, a dark scorch mark concentrated beneath the hearth, mottled stone and
distinct metal/tool islands. Black Forge uses directional metal wear and
individual surface treatment rather than one uniform field. Their visual
detail is attached to the object's geometry and function.

Our current worked-surface helper averages the source albedo colour, then builds
multiscale noise and wandering grain. That repairs interference/checkerboard
patterns and inconsistent stretching, but discards the source image's specific
marks. The result is broad mottling on many faces. It cannot place end grain on
an end, soot beside a fire, polished wear around a handle or grime in a joint.
Adding normal and roughness variation from that same field does not supply those
missing features. This is a principal cause of the remaining flat/perfect read.

## Design comparisons

**Forge versus Furnace.** Vanilla makes a compact work area from tapered,
unequal timber, an irregular hearth and recognizable tools. The new Furnace has
a coherent chamber, bellows, anvil and quench vessel, but its overall masonry
envelope is much larger and more architectural. Its repeated courses and broad
roof need locally authored stone/soot treatment, and the work area needs more
hierarchy than equal-weight rectangular masses.

**Artisan versus Silence.** Artisan's skewed timber, chains, mechanisms and
central apparatus form one distinctive silhouette. The new Silence fixtures
communicate grinding and pressing, but their spacing and finish remain regular.
The precision mechanism may reasonably be straight; the supporting timber,
joinery, stored stock and wear should not all inherit that same perfection.

**Black Forge versus Anchor.** Black Forge's long curved metal body and inset
working surfaces carry its identity, with chunky stone support and texture
detail doing much of the work. Anchor now has a readable press load path, but
its columns, cap, pedestal and braces remain an assembly of mostly straight
stock. Titanbone braces need an owned carved/bone profile, and the press needs
intentional forged shapes and surface damage, not additional gauges or bolts.

**Galdr versus Crown.** Galdr gets a strong ritual silhouette from organic horn
forms and a focused central arrangement with only 2,305 triangles. Crown's
stamp, mortar, clamp and sealed case now explain its recipes, but the tall square
frames read more like general workshop equipment. Its sealing process should
remain legible while its primary frame and reliquary gain distinctive Rotwood,
bone and amber forms. A halo/orb array would not solve that problem.

**Mycelial and Tidal.** Their new workpieces and basin depth are useful progress.
Mycelial still spends more triangles than any sampled vanilla station on
repeated component geometry; its surfaces need plank-specific authoring and
its supports need more grown-root character. Tidal's open recessed bowl now
communicates curing, but the surrounding timber/trays remain too uniformly
finished. Bath-line staining, salt deposits, worn bowl lips and fibre-specific
surface treatment should describe the process.

## What the next authoring pass should change

1. Keep the existing recipe-led working mechanisms and remove decorative detail
   that does not support the silhouette, construction or process.
2. Replace averaged noise as the principal finish with owned face-specific atlas
   work: directional grain/end grain, stone facets, forged edges, joint shadows,
   salt/soot staining and localized handling wear. Maintain a consistent effective
   pixel scale and review it in the game at ordinary player distance.
3. Shape primary members deliberately: tapers, uneven board widths, carved bone,
   grown roots, curved metal and worn stone. Apply asymmetry where craft and wear
   explain it; retain alignment where a precision mechanism requires it.
4. Consolidate static visual geometry by material when it can preserve named
   interaction/collision boundaries. Geometry consolidation is an efficiency
   change, not a substitute for the visual authoring above.
5. Replace the current 52–58-object station detail floors with review criteria
   for silhouette, joinery, materials, process readability and interaction. The
   existing floor rewards more mesh objects without proving greater visual detail.

Comparison did not modify or reinstall the 0.0.168 station assets. It records
the remaining authoring gap rather than treating the latest renders as vanilla
parity or in-game visual acceptance.

## Review files

- `dist/vanilla-station-comparison/vanilla-vs-magenheim.png`: four paired views.
- `dist/vanilla-station-comparison/texture-language-comparison.png`: native atlas
  content versus the current procedural Worldroot surface.
- `native-metrics.json`, `native-render-metrics.json`, `custom-metrics.json`:
  measured data, with native hierarchy, LOD and material evidence beneath `native/`.
