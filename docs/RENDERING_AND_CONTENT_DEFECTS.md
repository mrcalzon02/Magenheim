# Rendering and content defect register — opened 2026-09-15

Raised from live play against installed 0.0.48/0.0.49 with screenshot evidence. Every entry
below was confirmed by reading the authoritative source, not inferred from the report alone.

Two root causes explain most of the visual complaints. They are independent and both are real.

---

## R1 — Every runtime-generated surface is untextured (root cause, highest impact)

**Status: confirmed, not yet repaired.**

Eighteen visual files assign `mainTexture = Texture2D.whiteTexture`, a 1x1 white pixel:

`CapstoneArtifactVisuals`, `CrystalArchitectureVisuals`, `CrystalBannerVisuals`,
`CrystalBedVisuals`, `CrystalEnchantingDaisVisuals`, `CrystallineIceBoxVisuals`,
`CrystalSentinelVisuals`, `CrystalWeaponVisuals`, `DeepFractureObeliskWardenVisual`,
`FrostStaffVisuals`, `FurnitureVisuals`, `GeodeVisuals`, `GeologyDecorVisuals`,
`RadianceStaffRegistrar`, `SeidrStaffRegistrar`, `SpiritStaffRegistrar`,
`VenomStaffVisuals`, `WorldArtifactVisuals`.

Consequence: the mesh is lit and tinted by `_Color` but carries no surface detail whatsoever.
This is the flat white, pale lavender and flat blue seen across every screenshot, and it is the
whole of the "crystal items just don't have textures" and "unacceptably flat crystal texture"
reports. The crystal is not a bad texture; there is no texture.

`EarthAssets.cs` is the sole exception and the working reference: it loads a real PNG atlas per
asset and supports a tinted variant. The file-backed Earth geode, crystals and workstation
therefore do have surfaces, which is why they read differently in-world from everything else.

The project already generates textures procedurally in nine icon generators
(`FurnitureIcons`, `CrystalArchitectureIcons`, `CrystalWeaponIcons`, and others), so the
capability exists and is simply not applied to world materials.

**Repair direction:** one owned surface-texture authority producing crystal facet, stone,
timber and iron-banding maps, fed into `mainTexture` for every generated material, replacing
`Texture2D.whiteTexture` at all eighteen sites. Materials should stop being hand-rolled per
file.

---

## R2 — Generated solids of revolution are wound inside-out (root cause)

**Status: root cause proven and the correct geometry is now authoritative; call sites not yet migrated.**

The prism/cylinder builder was copy-pasted into eleven visual files with reversed triangle
winding:

```
bottom,next,i   i,next,sides+i   next,sides+next,sides+i   sides+i,sides+next,top
```

Under Unity's convention the face normal is `cross(v1-v0, v2-v0)`, which `RecalculateNormals`
reproduces. For this order every normal points into the solid, so a back-face-culled material
renders the interior: you look through the near face onto the inside of the far one. This is
the "texture inverted / see through to the other side" report.

The box builder was the only correctly wound primitive, which is why flat slabs look solid
while columns, crystals and rounded pieces do not.

Affected files: `WorldArtifactVisuals`, `GeologyDecorVisuals`, `FurnitureVisuals`,
`FrostStaffVisuals`, `CrystallineIceBoxVisuals`, `CrystalSentinelVisuals`,
`CrystalEnchantingDaisVisuals`, `CrystalBedVisuals`, `CrystalBannerVisuals`,
`CrystalArchitectureVisuals`, `CapstoneArtifactVisuals`.

**Repaired so far:** `Magenheim.Core/Geometry/MeshPrimitives.cs` now owns correctly wound
`Box`, `Cylinder` and `Prism` solids, and `MeshSurface.cs` provides deterministic closed-surface,
signed-volume and outward-normal checks. `MeshGeometryTests` reproduces the exact shipped
winding and asserts it is rejected, asserts its signed volume is negative, asserts that
closedness alone cannot detect it, and asserts that reversing it recovers a valid solid.

**Remaining:** migrate the eleven visual files onto the shared primitives and delete the
duplicated builders.

---

## Content and behaviour defects

### C1 — Staff families are rebranded vanilla staffs

**Confirmed.** Only Frost and Venom have owned geometry (`FrostStaffVisuals.cs`,
`VenomStaffVisuals.cs`). Fire, Storm and Earth reference no visuals at all; Radiance, Seidr and
Spirit only tint. Clone sources are vanilla `StaffFireball` and `StaffIceShards`, and
`EarthStaffRegistrar` references `SledgeIron` — the giant hammer seen in-world.

Because they are vanilla staff clones they also inherit the vanilla magic economy: every
registrar sets `attack.m_attackEitr = definition.EitrCost` and the recipes require `Eitr`.
Magenheim's progression does not use Eitr, so this is a live design contradiction, not just an
art problem.

**Blocked on a decision:** what a Magenheim staff should cost instead of Eitr.

### C2 — Crystal beds and Ice Box make bee sounds

**Confirmed.** `CrystalBedRegistrar` and `CrystallineIceBoxRegistrar` both clone
`piece_beehive` and retain its `Beehive` component, which carries the vanilla hive audio.

### C3 — Snap points misplaced (floating columns, raised floors, sunken beds)

**Confirmed.** `ConfigureSnapPoints` exists only in `CrystalArchitectureRegistrar` and merely
rescales snap points inherited from the vanilla clone source. For beams it maps the lowest snap
to local `y = 0`, which is the centre of a centred mesh rather than its base, so columns sit
proportionally above the floor. Foundations rescale `x`/`z` only and keep the source's `y`.
Beds, dais and other pieces configure no snap points at all and inherit the beehive's.

### C4 — Crystal Sentinel has no icon and no readable facing

**Confirmed.** Clones `piece_turret`; no owned icon generator exists for it, unlike the nine
other families which have one.

### C5 — Eight munition variants instead of one

**Confirmed.** `CrystalSentinelRegistrar.RegisterMunition` is called per `ElementalAlignment`,
producing `Magenheim_CrystalMunition_{element}` plus a projectile and recipe for each. Intended
design is a single crystal munition refined from any crystal.

### C6 — Crystal Dais is not a dais

**Confirmed.** `CrystalEnchantingDaisVisuals` builds stacked prisms and cylinders with an apex,
not a flat raised platform.

### C7 — Socket interface is a raw Unity debug window

**Confirmed.** `SocketWorkstationOverlay` is the only file in the project using `OnGUI`, drawing
`GUILayout.Window(WindowId, ...)` with `GUILayout.Button`/`Label`/`TextArea`. It should be built
from the game's own crafting UI.

### C8 — Vanilla piece name shown on an owned piece

**Reported from screenshot only, not yet traced.** A white Magenheim platform chair displays the
vanilla name "Raven Throne". Needs confirmation of whether this is a Magenheim registration
inheriting the clone's `m_name` or an adjacent vanilla piece.

---

## Priority

R1 and R2 together account for nearly all of the visual report and should land before any new
content. C3 is next because misplaced snap points make the build set unusable. C1 carries a
design decision and is blocked. C2, C4, C5, C6 are bounded. C7 is a self-contained UI rebuild.
