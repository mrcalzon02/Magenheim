# Underworld shared atmosphere system

**Status:** source implementation added 2026-09-21; deterministic Core logic is test-covered in source. Runtime compilation and live visual acceptance remain separate closeout work.

The Underworld uses one obscuration mechanic with six biome interpretations rather than six independent fog implementations.

| Biome | Atmospheric identity | Normal behavior | Event pressure |
|---|---|---|---|
| Fungal Forest | luminous healthy spores | readable, lightly particulate air | Sporefall thickens particles without becoming a poison cloud |
| Blackwater Deep | cold moisture mist | pools over water and low terrain | Deep Fog sharply reduces navigation distance |
| Sulfurous Wastes | chemical/geothermal miasma | localized by terrain hazard and low basins | Ashfall adds particulate; Thermal Surge adds exposure |
| Frozen Caverns | ice fog | low cold haze, often relatively clear | Whiteout is the strong visibility/audio event |
| Fracture Zones | mineral dust | normally the clearest hostile biome | Stone Rain/tremor dust produces temporary opacity |
| Great Decay | biological aerosol | persistent heavy miasma, strongest in low/hazard terrain | Black Bloom drives the field toward near-whiteout density |

## Authority split

`Magenheim.Core.Underworld.UnderworldAtmosphere` owns deterministic evaluation. Inputs are biome, terrain height, water depth, existing terrain hazard, synchronized event/intensity, hazard resistance and local suppression. Outputs are normalized visual density, visibility distance, particle density, gameplay exposure, audio damping and fog colour.

Resistance and suppression are intentionally different verbs. Armour/Deep Boons can lower exposure without making the biome visually disappear. A local clearing tool such as the planned Censer feeds suppression, which lowers both pressure and visual density while preserving a minimum biome identity.

`UnderworldAtmosphereRuntime` is a thin local renderer adapter. It samples the native instance terrain already owned by `UnderworldTerrainRuntime`, applies the Core state through Unity's existing `RenderSettings` fog, publishes shader globals for later particle/material consumers, and restores the pre-entry fog state on exit. It does not create a second save/environment manager, choose events, persist player occupancy, or mutate Valheim environment registrations.

The runtime exposes two narrow future inputs:

- `ApplySynchronizedEvent(...)`: only a server-authoritative event service should call this. The renderer never rolls events locally.
- `ApplyMitigation(...)`: equipment/Deep Boon/Censer code may feed already-resolved resistance/suppression without moving those item rules into the atmosphere system.

## Current event vocabulary

`Sporefall`, `DeepFog`, `Ashfall`, `ThermalSurge`, `Whiteout`, `StoneRain`, `CrystalResonance`, and `BlackBloom`.

Whiteout is the added Frozen Caverns expression of the same shared mechanic. Deep Fog remains valid for Blackwater and a weaker frozen moisture event.

## Next dependency-valid slices

1. Connect the existing Underworld hazard/Deep Boon results to `ApplyMitigation`; do not duplicate their rules.
2. Implement one server-selected/synchronized subterranean-event state and feed it to `ApplySynchronizedEvent`.
3. Reuse donor particle/VFX prefabs for the six atmospheric presentations before authoring custom particle art.
4. Live-test transitions between Surface and every Underworld biome, including restoration of Surface fog.
5. Tune visibility from screenshots/video only after donor scenery is present; do not optimize the numbers in an empty biome.
