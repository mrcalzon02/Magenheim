# Biome-cell massif and progression terrain — 2026-09-29

## Intent

Replace arbitrary world-scale spires/plateaus with height extremes that belong to the same
cellular geography as biome identity, hydrology and ecology. Progression should also have a broad
spatial tendency: earlier biomes are more common inward, later biomes increasingly common outward,
without recreating hard rings.

## Current authority

- `UnderworldTerrainLifecycle.RareCellElevationAlgorithmId` =
  `rare-cell-massif-v1-biome-owned`.
- The independent `UnderworldMonumentalLandforms` generator and its dedicated test suite are
  removed from current source.
- Eligible non-Blackwater cells between 26% and 82% world radius have a 5.5% deterministic chance
  to become massif variants.
- A massif adds roughly 1.5–3.3 km across most of the owning Voronoi-cell interior. Elevation fades
  through a 300 m cellular wall band, preserving the cell shape rather than converging to a needle.
- The underlying biome relief still contributes ridges, basins, faults and local ground shape.
- Blackwater/Voronoi seam width increases from 320 m to 440 m, with plasma width variation
  increasing from ±90 m to ±130 m. River carving occurs after massif uplift.
- The protected Fungal arrival shoulder and the outer edge-ocean band cannot receive massif lift.

## Progression elevation

Nominal regional lifts are deliberately ordered:

1. Fungal Forest — 80 m
2. Blackwater Deep — 130 m
3. Sulfurous Wastes — 230 m
4. Frozen Caverns — 340 m
5. Fracture Zones — 455 m
6. Great Decay — 575 m

These are baselines, not hard terraces. Local terrain noise, Blackwater carving, rare massifs and
the outer ocean may cross them.

## Graded radial preference

Each biome has a broad preferred radius rather than an admission ring:

- Fungal Forest: 0.14
- Blackwater Deep: 0.27
- Sulfurous Wastes: 0.40
- Frozen Caverns: 0.53
- Fracture Zones: 0.66
- Great Decay: 0.77

The preference contributes only part of the cell score. Fractal regional noise and deterministic
site noise remain strong enough to produce enclaves, repeats and seed-specific maps.

## Authority and acceptance

The composite Underworld authority schema advances to 9 and fingerprints the rare-cell elevation
algorithm separately. Mixed old/new peers must fail closed.

Source tests now cover ordered nominal lifts, radial progression trend, deterministic sparse massif
coverage, broad-cell rather than needle behavior, arrival/edge-ocean exclusions, distributed
Blackwater hydrology and seed-dependent biome geography.

This does not claim live Valheim acceptance. A fresh 0.0.153 disposable Underworld must verify
visual scale, cell-wall transitions, river widths, structure placement, map/weather/ecology
agreement, persistence and multiplayer identity.
