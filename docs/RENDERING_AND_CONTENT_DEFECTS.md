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

## Current acceptance boundary

The source defects raised by the screenshot pass are repaired on `main`. The next gate is not more
model generation: build the current runtime against the active Valheim/Jotunn API and live-test the
repaired families. Required live checks include representative textured/faceted models, placement
snaps, bee-audio absence, Sentinel icon/facing/single ammunition, low Dais usability, native socket
UI behavior and Crystal Throne hover identity. Until that run is observed, these entries are
**source repaired**, not claimed as runtime-verified.
