# Underworld atmosphere framework source validation — 2026-09-21

## Intent

Implement the shared obscuration system agreed for the six Underworld biomes: persistent Great Decay biological miasma, localized Sulfurous Wastes chemical fog, Frozen Caverns ice fog/whiteouts, Blackwater moisture haze, Fungal Forest spores and Fracture dust, all through one mechanism.

## Changed

- Added pure `UnderworldAtmosphere` evaluation in Core.
- Added biome-specific profiles for all six terrain biomes.
- Added event modifiers for Sporefall, Deep Fog, Ashfall, Thermal Surge, Whiteout, Stone Rain, Crystal Resonance and Black Bloom.
- Separated hazard resistance from local atmospheric suppression so progression can reduce damage pressure without simply deleting fog.
- Added deterministic source tests covering normalization, Great Decay dominance, water pooling, sulfur hazard coupling, whiteout behavior, resistance/suppression semantics and event-biome eligibility.
- Added `UnderworldAtmosphereRuntime` as a thin local presentation adapter over Unity `RenderSettings`.
- Runtime samples native Underworld instance terrain, restores the pre-entry fog state on exit, and exposes shader globals for later donor particle/material consumers.
- Runtime event choice is deliberately absent. It accepts synchronized event state but never selects an event client-side.
- Wired the runtime component into the existing plugin lifecycle.

## Valheim-first check

Jotunn 2.30 remains the project dependency. Its documented manager surface covers zones, creatures, pieces, prefabs, rendering, maps and related content, but does not provide a dedicated environment/fog manager abstraction. This implementation therefore uses Unity's existing RenderSettings as the smallest direct presentation adapter instead of introducing a Magenheim environment registry.

## Verification boundary

This commit is source-integrated only. No claim is made here that the current tree has compiled against the installed Valheim 1.0.12 assemblies, passed `closeout.ps1`, been installed into Central Fuckery, or been visually accepted in-game. Those are separate required closeout/live gates.

The deterministic test suite has been extended in source but was not executed through the GitHub connector.
