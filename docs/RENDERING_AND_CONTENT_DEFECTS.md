# Rendering and content defect register — opened 2026-09-15

Raised from live play against installed 0.0.48/0.0.49 with screenshot evidence. Every original
entry below was confirmed by reading the authoritative source, not inferred from the report alone.
The source repairs described here are now committed on `main`; in-game visual acceptance remains
open until a current build is installed and exercised in Valheim.

---

## R1 — Runtime-generated surfaces were untextured

**Status: source repaired; live visual acceptance pending.**

The reported build had many runtime visual families assigning `mainTexture =
Texture2D.whiteTexture`, leaving meshes lit and tinted but without surface detail. This accounted
for the flat white, pale lavender and flat blue appearance across the screenshots.

`GeneratedSurfaceTextures.cs` now owns repeatable crystal, stone, timber, metal, cloth, bone and
generic surface maps. Repaired runtime material factories route through that authority instead of
the one-pixel white texture. Staffs, furniture, Crystal Beds, Ice Box, Dais, Sentinel, banners,
crystal architecture, physical crystal weapons, geology decor, world artifacts, capstone artifacts
and the Deep Fracture Obelisk Warden have been migrated. `GeodeVisuals` had already diverged from
the stale defect snapshot and no longer used the white-pixel assignment when re-read from live
`main`.

Acceptance still requires a rebuilt installed DLL and live lighting inspection; source repair alone
is not evidence that every material reads correctly in-game.

---

## R2 — Generated solids of revolution were wound inside-out

**Status: source repaired at the root; live visual acceptance pending.**

The reported build contained copied prism/cylinder builders whose triangle winding could point
surface normals into the solid. With back-face culling this made near faces disappear and exposed
the inside of far faces, producing the reported see-through/inverted geometry.

`Magenheim.Core/Geometry/MeshPrimitives.cs` owns correctly wound `Box`, `Cylinder` and `Prism`
solids, while `MeshSurface.cs` and `MeshGeometryTests` provide deterministic closed-surface,
signed-volume and outward-normal validation. `RuntimeMeshPrimitives.cs` is now the Unity adapter
for those tested Core solids.

All eleven families from the original defect inventory have been migrated away from their private
primitive builders: `WorldArtifactVisuals`, `GeologyDecorVisuals`, `FurnitureVisuals`,
`FrostStaffVisuals`, `CrystallineIceBoxVisuals`, `CrystalSentinelVisuals`,
`CrystalEnchantingDaisVisuals`, `CrystalBedVisuals`, `CrystalBannerVisuals`,
`CrystalArchitectureVisuals` and `CapstoneArtifactVisuals`. The separately discovered
`CrystalWeaponVisuals` and `DeepFractureObeliskWardenVisual` copies were also migrated so the same
failure cannot recur there through an independent builder.

Acceptance still requires a rebuilt DLL and live inspection of representative cylinders/prisms.

---

## Content and behaviour defects

### C1 — Staff families were rebranded vanilla staffs

**Status: source repaired; live combat/visual acceptance pending.**

Fire, Frost, Storm, Earth, Venom, Radiance, Seidr and Spirit now have Magenheim-owned visual
families and stamina-oriented Magenheim casting behavior rather than depending on the inherited
vanilla Eitr economy. Spirit's remaining inherited Eitr requirement and fireball carrier were
removed during this repair pass. The clone prefabs remain implementation carriers where needed,
but their user-facing geometry/economy is no longer the vanilla identity.

### C2 — Crystal Beds and Ice Box made bee sounds

**Status: source repaired; live audio acceptance pending.**

The beehive donor remains useful for persistent production state, but inherited hive audio is now
removed at the clone boundary for both systems rather than muted piecemeal.

### C3 — Snap points misplaced pieces

**Status: source repaired; live placement acceptance pending.**

`PlacementSnapAuthority` now owns the relevant placement geometry instead of trusting donor snap
coordinates. Architecture beams/foundations, furniture, the Dais, Crystal Beds and the Ice Box
have explicit base/footprint handling. This removes the source path that produced floating columns,
raised foundations and sunken donor-derived pieces.

### C4 — Crystal Sentinel had no icon and no readable facing

**Status: source repaired; live placement/targeting acceptance pending.**

The Sentinel now has an owned hammer-menu icon and an explicit forward emitter/yoke aligned with
the turret body's local firing direction.

### C5 — Eight munition variants instead of one

**Status: source repaired; live recipe/turret acceptance pending.**

The Sentinel now accepts one `Magenheim_CrystalMunition` item with one projectile and one loaded
ammo visual. Eight elemental-shard recipes are retained only as alternate refinement inputs to that
single standardized munition identity.

### C6 — Crystal Dais was not a dais

**Status: source repaired; live station acceptance pending.**

The tall central crystal monument and tall focus collider were removed. The owned visual is now a
low circular working platform with recessed focus geometry, elemental channels and corrected donor
snap placement.

### C7 — Socket interface was a raw Unity debug window

**Status: source repaired; compile/live UI acceptance pending.**

`SocketWorkstationOverlay` no longer exposes an `OnGUI`/`GUILayout` presentation path. The socket
surface is now parented to the active `InventoryGui` and reuses native crafting panel/button/text
styling while preserving the existing server-authoritative transaction, rollback and stale-state
logic. The obsolete IMGUI skin patch was removed after the rewrite.

### C8 — Vanilla "Raven Throne" name appeared on an owned piece

**Status: source repaired; live hover acceptance pending.**

The trace reached the furniture clone boundary: configuration supplied the Magenheim display name,
but the cloned runtime `Piece` component was not explicitly overwritten. Every owned furniture
clone now writes `Piece.m_name` and `Piece.m_description` from its authoritative definition after
cloning, including the Crystal Throne.

---

## R3 — Geode geometry is inverted, and the library-wide fidelity measurement

**Status: source repaired 2026-09-16; live visual acceptance pending.**

`tools/generate-geode-models.py` rebuilds `geode-sample` as fractured Voronoi plates over a
cut core, a cavity whose normals face the void, a concentric agate collar and a druzy field
of 220 inward-pointing crystals. 1,259 triangles across 8 parts became 3,593 across 5, and
all five parts now carry a generated greyscale albedo where none did before. Material names
keep the `GeodeVisuals` tint contract, so the eight biome geodes shade with no runtime change.

`tools/verify-model-geometry.py` is the permanent gate for this defect class and passes over
all 281 models. Two of my own defects were caught during the rebuild and are recorded below.

The original diagnosis follows.

Reported from play: the geode is "a vaguely lumpy D20 with transparency issues across various
faces and a flat textureless plain upon which a few crystals were sticking straight out of the
flat textureless side as our exposed interior side".

That is not a transparency defect. `assets/earth/geode.png` is 512x512 RGB with no alpha
channel, and `EarthAssets.ConfigureOpaqueMaterial` already forces opaque render state. It is
inverted winding, the same defect class as R2, measured on the exported payload by comparing
each face normal against the vector from the part centroid:

| Part | Faces pointing inward | Signed volume |
| --- | --- | --- |
| `stone-cavity` | 246 / 246 | negative |
| `interior-crystal-0` | 104 / 142 | negative |
| `interior-crystal-1` | 104 / 142 | negative |
| `interior-crystal-2` | 89 / 142 | negative |
| `interior-crystal-3` | 104 / 142 | negative |
| `interior-crystal-4` | 104 / 142 | negative |

The "flat textureless plain" is a fully inverted mesh being seen from behind. `stone-shell`
is correctly wound, which is why only part of the object misbehaves.

Method note: the same pass counted open edges, but the exporter writes unwelded vertices, so
every directed edge is unique and edge-pairing reports 3x the triangle count on every part
including correct ones. That metric is uninformative on this payload and was discarded. The
face-normal test is position-based and does hold.

Scope: `geode-sample` is a single model loaded by `GeodeVisuals.Apply` for both the item and
the world object, shared by all eight biome geodes, so one rebuild carries the whole set.

The biome-shading mechanism already exists and must be preserved: `GeodeVisuals.Apply` tints
any material whose name contains `magenheim.geode.interior-` with the biome colour and
lightens `interior-bright-`. The rebuilt interior must therefore be authored greyscale.

### What the new gate caught, including in my own work

Writing the gate was not a formality. Its first useful output was a failure in the cavern
rebuild I had shipped the previous day: every stalactite and column in all twenty districts
was wound inside-out. Split into connected components, all 18 cones in DF-01 had negative
signed volume. The cause was `add_cone` orienting faces against a reference point, which is
correct for an upright cone and wrong for one that hangs, because a negative span reverses
the natural ring winding. Formations are now built in isolation, measured, and reversed once
if the measured volume is negative, rather than reasoned about.

The gate also needed one correction before it was worth anything. Its first version demanded
a perfectly closed manifold before applying the volume test, which put the geode cavity
(99% of edges paired) and the interior crystals (96%) into the "cannot judge" bucket and
passed them silently. A shell with a mouth cut in it is still a shell; the tolerance is now
10% unpaired edges. A gate that misses its own motivating case is worse than no gate, because
it manufactures confidence.

Two defects in the geode rebuild were found by rendering rather than by any gate, and both
were invisible to geometry checks because the meshes were individually valid:

- the darker core sphere was built whole, so it sealed the mouth and no interior was visible
  at all; it now carries the same opening as the shell and cavity;
- the druzy placement excluded the wall opposite the mouth as "hidden", which is precisely
  the surface the player looks at through the opening, leaving crystals only around the rim.

### Two winding gates, deliberately

`tools/verify-model-assets.py` gained its own winding check in parallel with this work. The
two are complementary and neither should be removed as redundant:

| Gate | Test | Catches | Blind to |
| --- | --- | --- | --- |
| `verify-model-assets.py` | geometric face normal vs authored vertex normals | a face flipped out of step with its neighbours, hand-edited normals; valid on open and concave meshes | a uniformly inside-out solid |
| `verify-model-geometry.py` | signed volume of a sealed surface | a whole solid wound inside-out | partial flips, and open surfaces, which it reports rather than fails |

The blind spot is not theoretical. The exporter derives authored normals from the winding, so
in a uniformly inverted solid the geometric and authored normals agree perfectly. Applied to
the pre-rebuild geode, whose cavity was 246 of 246 faces inverted, the normal-agreement test
reports **zero** disagreeing faces on every part. That is the exact defect that has now
shipped three times, and only the signed-volume test sees it.

### Non-planar quads as a third source of the same symptom

Both gates converged on a cause neither was written for. A quad whose corners are not
coplanar is triangulated at export, and the two halves can end up facing opposite ways. The
mesh is valid, the winding is "consistent" in the source, and recalculating normals does not
help, because there is no single correct normal for a bent quad.

It appeared twice:

- the jittered ring bands in the cavern formation builder, found as two disagreeing faces in
  `DF-01/DistrictSignature`. The builder now emits two planar triangles per band instead of a
  quad;
- `spirit-fetish-raven`, whose wings were 13 n-gons each carrying four disagreeing faces per
  wing. Pre-existing, from `Models updates-1`; `normals_make_consistent` did not clear it, and
  triangulating each wing to 40 planar faces did.

This mattered beyond tidiness: `verify-model-assets.py` runs from `build.ps1`, so the raven
left the asset gate failing on `main` from the moment the winding check landed.

### R4 — Crystal progression items rebuilt

**Status: source repaired 2026-09-16; live visual acceptance pending.**

Six models carry forty-eight items: `EarthContentRegistrar` registers every tier for all
eight elements from one shared mesh per tier, tinted per element, plus the shard item. They
were the lowest-poly assets in the library, for the items the player handles through the
whole refinement loop.

| Model | Before | After |
| --- | ---: | ---: |
| `earth-rough` | 108 | 422 |
| `earth-simple` | 36 | 382 |
| `earth-crystal` | 108 | 428 |
| `earth-advanced` | 180 | 520 |
| `earth-master` | 180 | 530 |
| `earth-shards` | 108 | 480 |

`tools/generate-crystal-tier-models.py` builds a readable progression: a matrix chunk with
stubby points, one terminated point on matrix, a well formed point with a secondary, a main
point with three secondaries, a radiating seven-point cluster with the matrix nearly gone,
and six angular fragments. Each carries a generated greyscale albedo split by UV, matrix on
the left of the map and crystal facets on the right, so `EarthAssets` tinting reads per
element.

Two constraints from the surrounding code shaped it. `EarthAssets.LoadMesh` calls
`RecalculateNormals`, so winding alone decides lighting; every solid is built in isolation,
measured and reversed once if its signed volume came out negative. `ReplaceVisual` builds a
single `MeshFilter`, so each model is one object with one material.

One defect worth recording because it is invisible to every gate: the forms were authored in
game space, Y up, while Blender is Z up and the exporter maps `(x, y, z) -> (x, z, -y)`. The
first export therefore produced crystals lying on their side. Geometry, winding, UVs and
hashes were all valid; only rendering the result showed it. A conversion at emit now makes
the exported game coordinates identical to the authored ones.

### R5 — Deep Fracture passages brought up to the districts

**Status: source repaired 2026-09-16; live visual acceptance pending.**

The cavern rebuild left the joining pieces behind. `deep-fracture-passage` was five boxes
totalling 60 triangles and `deep-fracture-traversal` 172, feeding districts of roughly 10,000.

| Model | Before | After | Envelope | Declared Room |
| --- | ---: | ---: | --- | --- |
| `deep-fracture-passage` | 60 | 744 | 9.9 x 9.0 x 16.0 | 10 x 10 x 16 |
| `deep-fracture-traversal` | 172 | 352 | 4.5 x 6.2 x 4.4 | 12 x 10 x 12 |

`tools/generate-deep-fracture-passages.py` sweeps the passage as a rock tunnel with an arched
roof, a flattened walkable floor and irregular walls, open at both ends so it docks into the
district mouths, with pendants and floor rubble. The traversal node keeps its plinth, ring
and core role and envelope but is built as faceted stone and crystal.

`PassageShell` is declared in `verify-model-geometry`'s `INTERIOR_SURFACES`: a swept tube has
no enclosed volume to test, and the player walks inside it, so its faces point at the axis.
The sweep checks its own orientation against the tunnel axis instead.

The preview render was too dark to judge surface quality; it confirms enclosure, floor and
formations only.

### R6 — Every generated texture shipped black

**Status: repaired 2026-09-16, and gated.**

The geode, crystal tier, passage and district rebuilds each added a generated greyscale
albedo where none existed. All of them were **pure black**: sampled minimum and maximum of 0.
Albedo multiplies, so a black map is strictly worse than the flat base colour it replaced.

Cause: `bpy.data.images.new` produces a generated image, and a generated image stores only
its settings in a `.blend`, not its pixel buffer. The generator wrote pixels and saved the
file; the exporter reopened it, the image regenerated as its default colour, and that is
what was packed and hashed. `image.update()` and `image.pack()` in the generator fix it by
embedding the buffer.

Nothing could see it. The PNG was the right size, correctly formatted, correctly hashed and
referenced by the right material; geometry, UVs and winding were all valid. Both winding
gates passed. It was found by decoding a texture and looking at the numbers.

`verify-model-assets.py` now decodes each referenced PNG and requires actual tonal range,
rejecting both an all-black map and a flat fill. Verified against synthesised cases:

```
pure black (the shipped defect)    min=0    max=0    -> REJECTED
flat mid grey (no tonal range)     min=128  max=128  -> REJECTED
current cavern wall map            min=86   max=154  -> accepted
```

The districts also gained maps in this pass, having previously carried flat base colours
only. Library texture coverage is now 134 of 281 models (48%), from 111 at `924f5e7`.

### R7 — Creature models textured

**Status: source repaired 2026-09-16; live visual acceptance pending.**

104 creature and creature-visual models carried 1,520 untextured parts, the largest single
block of the texture gap.

Their materials could not say what a surface is: the model migration left most of them named
`export-source`, `export-source.001` or `export-source.002`, shared across hundreds of parts.
The part names survived and could - `body`, `torso`, `leg-l`, `back-plate`, `hook-r`, `prong`
against `crystal`, `core`, `shard`, `spire`, `eye` - so `tools/texture-creature-models.py`
classifies by part name and gives each object a copy of its own material carrying either a
crystal facet map or a pitted carapace map.

Copying rather than editing the shared material is the point: one `export-source` is used by
carapace and crystal parts alike, so editing it in place would have put a crystal facet map
on a leg.

Base colour, roughness and metallic are preserved exactly. Those already encode the elemental
tint across all eight variants, and Unity multiplies albedo by base colour, so the map adds
surface without disturbing the tint. Geometry, UVs, custom properties and object names are
untouched; this is a material pass only, so orientation and collision are unchanged by
construction.

Library texture coverage: **230 of 281 models (82%)**, from 111 at `924f5e7`. Parts 82%, from
36%. 51 models remain, led by crystal (13), decor (10) and boss (8) families.

### R8 — Texture gap closed

**Status: source repaired 2026-09-16; live visual acceptance pending.**

Every model in the library now carries an albedo map: **281 of 281 models and 3,681 of 3,681
parts**, from 111 models and 36% of parts at `924f5e7`.

The pass that textured the creatures was generalised to six surface roles chosen by part
name - crystal, metal, timber, cloth, carapace, and stone as the fallback - and pointed at
any model whose exported payload still had no texture. That cleared the last 51: crystal
architecture, decor, boss hearts, artifacts, effects and assorted singletons.

The same rules apply throughout: each object gets a copy of its own material so a shared
`export-source` cannot put the wrong map on the wrong surface, and base colour, roughness and
metallic are preserved so existing tints are undisturbed. Geometry, UVs, custom properties
and object names are untouched.

What this is and is not: every surface now has tonal variation instead of a flat fill, which
is what made the library read as clay. It is not hand-authored art, there are no normal maps,
and no in-game lighting check has been made.

### Library-wide fidelity measurement at `924f5e7`

| Measure | Value |
| --- | --- |
| Models with no texture map | 170 of 281 (60%) |
| Textured parts | 1,320 of 3,688 (36%) |
| `earth-simple` | 36 triangles |
| `earth-rough`, `earth-crystal`, `earth-shards` | 108 triangles each |
| `earth-advanced`, `earth-master` | 180 triangles each |
| `deep-fracture-passage` | 60 triangles (five boxes) |
| `deep-fracture-traversal` | 172 triangles |
| Deep Fracture districts, after the cavern rebuild | ~10,000 triangles each |

Flat Principled base colour with no map is why assets read as clay. The crystal progression
items, which the player handles throughout the entire refinement loop, are the lowest-poly
assets in the library. The passage and traversal pieces were not part of the cavern rebuild
and now connect chambers roughly 150 times their own triangle count.

---

## R9 — First live acceptance pass, 0.0.52

**Status: eight defects reported from play 2026-09-16; see BACKLOG.md P0.0.**

This is the first report from an installed build since 0.0.16, and it is the evidence that was
missing behind every "source repaired, live acceptance pending" entry above.

Two findings matter beyond their own fix.

**The retro-texture pass regressed the crystal buildables. Fixed in 0.0.53.** Closing the texture gap to 100%
applied generated maps to models whose UVs were smart-projected into packed islands. A
coherent noise field sampled across island boundaries reads as discontinuous patchwork, which
is worse than the flat colour it replaced. Models with purpose-authored UVs - geode, crystal
tiers, caverns, passages - are unaffected, because their UVs were built for the map. The
tonal-range gate cannot see this: the map has range, it is simply being sampled across seams.
Coverage percentage was the wrong measure of success.

The repair replaces every retro-fitted map with fine, low-contrast grain: no feature is larger
than a few pixels, so no island boundary can expose a discontinuity. 147 sources and 203
exported models were rewritten. This is a floor, not a finish; a proper fix is a per-family
unwrap with an authored map, which is a much larger job and remains open.

**Hand orientation is very likely the up-axis defect again.** The crystal tier models were
authored Y-up against a Z-up Blender and a `(x, y, z) -> (x, z, -y)` exporter, and shipped
lying on their side with entirely valid geometry, winding, UVs and hashes. If weapons and
staves are wrong in hand, that is the same class, and no gate in the project can see it.

Counted with the earlier four, this session has now produced six defects invisible to every
automated check and found only by looking: sealed geode core, sideways crystals, under-lit
passage, black textures, island-seam patchwork, and hand orientation.

---

## Current acceptance boundary

The source defects raised by the screenshot pass are repaired on `main`. The next gate is not more
model generation: build the current runtime against the active Valheim/Jotunn API and live-test the
repaired families. Required live checks include representative textured/faceted models, placement
snaps, bee-audio absence, Sentinel icon/facing/single ammunition, low Dais usability, native socket
UI behavior and Crystal Throne hover identity. Until that run is observed, these entries are
**source repaired**, not claimed as runtime-verified.
