# Deep Fracture districts rebuilt as cavern terrain — 2026-09-16

Static and geometric validation only. No in-game acceptance is claimed: the districts have
not been walked in a live world.

## What the districts were

Each of the twenty districts was a box assembly. DF-01 is representative:

| Part | Size | Note |
| --- | --- | --- |
| `DistrictFloor` | 88 x 2.2 x 88 | one flat slab |
| `BoundaryMass0-3` | ~14-20 m | four corner boxes |
| `BoundaryNorthWest/NorthEast/SouthWest/SouthEast` | 20 x 10 x 5 | four wall slabs |
| `DescentStep0-5` | 27 x 2.4 x 10 | a stacked staircase |

Every part was 36 vertices and 12 triangles, i.e. a cube. A 96 m district carried 144-988
triangles, less than a single staff (~2,000). Nothing existed above y ~= 13.7: **there was no
ceiling at all**. The space was a walled courtyard, which is why it read as a giant square
room rather than anything underground.

## What they are now

`tools/generate-deep-fracture-caverns.py` rebuilds all twenty from a deterministic,
per-district seed:

- **Floor** - multi-octave undulation, a terraced descent replacing the stacked staircase,
  and a central basin, so the middle of the chamber sits below its approaches.
- **Vault** - a dome peaking at 21-29 m and falling to low eaves at the rim. This is the
  single largest change: the districts now have a roof.
- **Walls** - perimeter rock joining floor to vault, cut by a passage mouth on each of the
  four edges so districts still chain into a network.
- **Formations** - stalactites, stalagmites and floor-to-ceiling columns built as stacks of
  jittered, leaning, bulging rings. A single smooth taper reads as a traffic cone; these
  read as dripstone. They gather around three to five cluster centres so open floor remains
  between them.
- **Surface fissures** - fourteen of the twenty districts break the surface. A fissure is a
  hole cut in the vault on the same lattice the vault is built from, so the join is
  seamless; a shaft rises 16 m from the rim to a pale cap; and a runtime point light sits in
  the throat of the shaft, lighting the shaft walls and spilling down onto the floor.
  Nineteen such lights ship across the library. Six districts have no fissure, because
  breaking the surface reads as an event only if it is uncommon.
- **Signature features** - each district's authored identity is re-seated on the new floor
  rather than discarded: Descent, Cathedral, Thermal, Crucible, Rime, Vault ribs, Conductor,
  Gallery, Compression, Seismic, Grotto, Dissolution, Prism, Sanctified, Echo, Ossuary,
  Shaping and Heart.

Normals face into the cavity, because the player stands inside the shell. The shell
materials are additionally double-sided: an interior invisible from the inside is a failure
this project has already shipped once.

## Traversability is enforced, not hoped for

The raw shaping functions produced a 3.45 m step at one terrace lip - a 58 degree face the
player cannot climb. Rather than tune it away by eye, the floor is baked onto the sample
lattice once and relaxed until no neighbouring pair exceeds 1.45 m, which is about 34
degrees over the 2.14 m lattice. Every consumer - surfaces, walls, formations, signature
features - reads that same relaxed lattice, so the floor the verifier checks is the floor
the geometry is built from. The relaxation asserts convergence rather than silently
returning a steep field.

## Verification

`tools/verify-deep-fracture-caverns.py` gates the exported payload on the properties that
decide whether the space is playable:

```
DF-01 .. DF-20  ok  parts=9  triangles=9990-11070
                    headroom>=4.5m  step<=1.45m  mouths=72
Verified 20 Deep Fracture districts as enclosed, traversable caverns.
```

It checks a vault exists above the floor everywhere, that interior headroom clears 4.4 m,
that no floor step exceeds 1.75 m, and that every passage mouth keeps 8 m of clear height.
It fails non-zero and names the district, so it can gate a build. It caught the 3.45 m step
above before any of this was committed.

Supporting checks at this revision:

- `verify-model-assets.py`: PASS, 281 Blender/GLB/runtime sets, 828,123 triangles;
- core harness: 37,085 deterministic assertions;
- Release runtime build: zero warnings, zero errors;
- `verify-patch-targets.ps1`: 22 patch targets;
- `verify-reflection-targets.ps1`: 11 literal and 42 helper-wrapped bindings.

District triangle totals went from 6,976 across all twenty to 207,500.

## Runtime room volume

`DeepFractureRoomVisuals` declared every district as a 96 x 32 x 96 `Room`. Measured extent
across the rebuilt districts is -6.5 m to 45.9 m, a 52.3 m span, because the basin drops
below zero and the fissure shafts rise above the vault. The declared volume is now
96 x 56 x 96; leaving it at 32 would have had the dungeon generator placing districts
against bounds that no longer matched their geometry.

## Not claimed

No live world test. Collision behaviour, whether the vault reads at Valheim's in-game
lighting levels, whether the fissure lights are the right intensity in practice, creature
navmesh over the new floor, and passage docking against the new mouths are all unverified.
The previews under `assets/models/previews/cavern-*.png` are diagnostic renders with their
own lighting, not representative of in-game appearance.
