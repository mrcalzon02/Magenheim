# Organic Underworld biome field — 2026-09-28

## Intent

Remove the early five-province angular scaffold. The Underworld should preserve one protected
arrival country but otherwise generate like a world: large seeded regions, distorted borders,
fingers, enclaves, repeated biome appearances and materially different maps between seeds.

## Implementation

`UnderworldTerrainLifecycle.BiomeLayoutAlgorithmId` is now
`biome-layout-v1-warped-multifield`.

Only the inner 10% of the 8 km radius is unconditionally Fungal Forest. Outside it, two broad
seeded noise fields displace the sampling coordinates by up to 850 m. Six independently salted
regional fields then compete, with secondary heat/moisture/tectonic/decay/fungal fields providing
small thematic affinities. No score contains compass angle or a fixed biome ordering.

The nominal Fungal arrival region survives as a radial bias, not a hard disk. Its gameplay
hazard/terrain transition fades from the guaranteed core through 24% radius, allowing the visible
edge to meander while keeping hostile pressure suppressed near the gate.

When the two strongest biome scores are within 0.08, their terrain profiles blend smoothly.
Biome/ecology/weather ownership remains discrete.

## Deterministic gates

Core tests now require a guaranteed Fungal core, an irregular outer shoulder, at least four biome
identities and at least ten boundary crossings on a 6 km ring, repeated disconnected arcs for at
least one biome, substantial ordinary relief for all six terrain profiles, and material map
rearrangement between representative seeds.

The biome-layout algorithm is included in the composite Underworld authority fingerprint and that
authority schema advances from 5 to 6, preventing mixed old/new generators from silently agreeing
on content while disagreeing on world geography.

## Acceptance boundary

This is source implementation plus deterministic algorithm review. It is not a claim that a
0.0.150 package has been built, installed, or observed in Valheim. Live acceptance requires a new
disposable Underworld because already-generated zones cannot demonstrate fresh biome assignment.
