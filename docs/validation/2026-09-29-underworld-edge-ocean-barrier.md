# Underworld edge-ocean barrier — 2026-09-29

## Intent

The Underworld's native terrain domain is an 8 km radius plate, but the player should not perceive
that logical cutoff as the last piece of walkable ground. Like Valheim's surface world, the terrain
needs to dissolve into a broad ocean barrier before the hard world edge.

## Implementation

`UnderworldTerrainLifecycle.EdgeOceanAlgorithmId` is
`edge-ocean-v1-plasma-shore`.

- shoreline fade begins at 84% of world radius;
- a four-octave, ~1.8 km plasma field offsets that shoreline by up to about 260 m;
- the fade reaches guaranteed full depth by 96.5% radius;
- the outer target bed reaches approximately 140 m below the shared water level;
- once edge-ocean strength passes 0.18, the owning biome is Blackwater Deep;
- the carve runs after hex-river shaping and monumental landforms, so giant spires cannot punch
  through the final barrier;
- the hard 8 km domain boundary remains unchanged and fail-closed.

This produces an irregular coast followed by a deep Blackwater ring. The domain remains circular
for deterministic instance admission, but the visible shoreline is not a perfect circle.

## Authority

The edge-ocean algorithm is included separately in the Underworld composite fingerprint. Authority
schema advances from 7 to 8 so an old client cannot silently generate land where a current host
expects deep edge ocean.

## Acceptance boundary

Source implementation and deterministic gates are committed. Live acceptance still requires a
fresh 0.0.152 world to verify shoreline appearance, deep-water rendering, Blackwater atmosphere,
navigation, map presentation, structure suppression, persistence and multiplayer agreement.
