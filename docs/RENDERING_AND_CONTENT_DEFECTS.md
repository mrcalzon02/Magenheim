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

## Current acceptance boundary

The source defects raised by the screenshot pass are repaired on `main`. The next gate is not more
model generation: build the current runtime against the active Valheim/Jotunn API and live-test the
repaired families. Required live checks include representative textured/faceted models, placement
snaps, bee-audio absence, Sentinel icon/facing/single ammunition, low Dais usability, native socket
UI behavior and Crystal Throne hover identity. Until that run is observed, these entries are
**source repaired**, not claimed as runtime-verified.
