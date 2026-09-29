# Hex/Voronoi Blackwater river grid — 2026-09-29

## Intent

Use the cellular skeleton itself as geography instead of treating biome assignment and hydrology as
unrelated layers. The Underworld should retain visible macro organization from a hex-derived lattice
while still reading as natural terrain rather than a board game.

## Generator

- nominal hex radius: 900 m;
- site jitter: up to 50% of that radius;
- Voronoi ownership selects each regional cell;
- a 1.25 km plasma/domain field warps sampling coordinates by up to 210 m;
- a second ~700 m plasma field varies the apparent edge width;
- biome family for each site comes from low-frequency fractal regional scores plus a deterministic
  site term, so neighbouring cells can cluster without collapsing into one giant province.

## Hydrology

The difference between nearest and second-nearest Voronoi-site distance is the edge metric.
Where those distances converge, the boundary becomes a river candidate. Plasma varies the width;
strong seams become Blackwater Deep and are carved toward the shared water level.

The strongest seam reaches about 30 m below water. River ownership begins only outside the protected
Fungal core and reaches full strength by 18% of world radius. This prevents a synthetic moat at the
arrival gate while still allowing the river lattice to dominate the outer realm.

The channel is not merely visual map shading. Blackwater ownership means terrain material, weather,
ecology, map identity and water depth all agree with the river geometry.

## Deterministic gates

Core tests retain seed determinism, multi-biome cellular topology and cross-seed map rearrangement,
and now additionally require substantial deep Blackwater coverage without drowning most of the map,
with deep-water corridors represented in every world quadrant.

## Acceptance boundary

This is committed source authority, not live visual acceptance. A fresh 0.0.151 build/world must
confirm bank shape, channel continuity, water depth, Blackwater material/weather transition,
monument interactions, navigation, map presentation and multiplayer agreement.
