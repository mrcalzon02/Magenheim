# 2026-09-21 — Underworld internal-layer live defect and correction

## Live evidence

The first live test of the new Underworld terrain materializer showed Magenheim's generated
Underworld terrain and world-center content rendered directly over ordinary Meadows terrain.
That is a failed instance boundary. The Core terrain data was instance-local, but the runtime
presentation adapter treated those logical coordinates as Unity world coordinates.

This is not a content-placement bug and must not be repaired by moving the Underworld sideways
to another Surface continent.

## Correct runtime model

The reference implementation supplied during the live test was JereKuusela's
`valheim-dungeon_splitter`. Its important lesson is not "make a second save"; it is that Valheim's
X/Z sector machinery can be given an additional layer discriminator. Server synchronization,
ownership and client loading can then reject objects belonging to the other layer while transport
still uses normal Valheim movement.

Magenheim now applies that model to its own dedicated instance: Core terrain, biomes, chunk keys,
map coordinates and persistence stay native to the Underworld; runtime presentation alone maps
instance index 1 into an opaque engine-space vertical backing layer. Player-facing systems convert
back to logical instance coordinates before consulting Core.

Source correction is not a live-runtime claim. A rebuilt Surface -> Deep Gate -> Underworld ->
Surface run still must prove no Surface overlap, no cross-layer ZDO visibility/ownership, correct
weather/biome sampling, and reversible transport.

Reference: https://github.com/JereKuusela/valheim-dungeon_splitter
License: Unlicense.
