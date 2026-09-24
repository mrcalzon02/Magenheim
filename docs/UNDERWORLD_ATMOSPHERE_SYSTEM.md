# Underworld shared atmosphere system

**Status:** updated for 0.0.104 on 2026-09-23. Deterministic Core logic and runtime compilation are verified; live visual acceptance remains separate.

The shared roof is a dark basalt panorama with lava fissures and fungal stars. A separate,
pixel-aligned emission mask is composed into an HDR texture once at load. Unity's existing
panoramic sky shader renders it without a sun disc. The mask makes the roof appear luminous;
Valheim's dimmed directional and ambient environment lights illuminate the world, using its
existing day fraction. This is an artistic lighting approximation, not light transport from
individual roof pixels. No specular trick, extra clock or per-star lights are used.

Borrowed native cloud geometry forms overhead haze at native height 4800m. Occasional sheer
spires and plateau crowns end at or below 5100m, just above that haze. These are visual height
targets, not a physical sky collider or flight/build ceiling. Surface sky/cloud state is restored
on exit. In-game brightness, haze opacity, shadows and restoration still require acceptance.

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

`UnderworldAtmosphereRuntime` is the thin local fog/exposure renderer. `UnderworldWeatherCycle` now selects biome-legal subterranean conditions deterministically from the paired instance seed and Valheim's authoritative world clock. `UnderworldWeatherRuntime` registers namespaced clones of existing Valheim `EnvSetup` donors, forces the matching Underworld environment while the local player is below, and clears that override on exit. Surface biome weather tables are never rewritten. Default thunder/rain particles, rain clouds and storm ambient loops are explicitly filtered out of the Underworld clones.

The runtime exposes two narrow future inputs:

- `ApplySynchronizedEvent(...)`: only a server-authoritative event service should call this. The renderer never rolls events locally.
- `ApplyMitigation(...)`: equipment/Deep Boon/Censer code may feed already-resolved resistance/suppression without moving those item rules into the atmosphere system.

## Current event vocabulary

`Sporefall`, `DeepFog`, `Ashfall`, `ThermalSurge`, `Whiteout`, `StoneRain`, `CrystalResonance`, and `BlackBloom`.

Whiteout is the added Frozen Caverns expression of the same shared mechanic. Deep Fog remains valid for Blackwater and a weaker frozen moisture event.

## Weather ownership

The Underworld no longer accepts whatever Surface weather the host biome happens to select. Every four-minute world-clock window resolves one biome-owned state from the shared deterministic schedule:

| Biome | Calm share | Native events |
|---|---:|---|
| Fungal Forest | 60% | Sporefall 30%, Crystal Resonance 10% |
| Blackwater Deep | 55% | Deep Fog 35%, Crystal Resonance 10% |
| Sulfurous Wastes | 50% | Ashfall 25%, Thermal Surge 20%, Crystal Resonance 5% |
| Frozen Caverns | 50% | Deep Fog 20%, Whiteout 25%, Crystal Resonance 5% |
| Fracture Zones | 55% | Stone Rain 30%, Crystal Resonance 15% |
| Great Decay | 45% | Black Bloom 45%, Crystal Resonance 10% |

Great Decay remains heavily obscured even in a calm window because that is its baseline atmosphere; Black Bloom intensifies it rather than creating it from nothing.

The runtime clones existing Valheim environments only as donors for sky/light/wind/particle machinery. Names are Magenheim-owned. Surface rain clouds are disabled, thunder/rain/storm particle systems are rejected, and donor storm ambient loops are not inherited. Whiteout is the deliberate exception for snow particle systems: a particle object whose name contains both `snow` and `storm` is admissible only to the Frozen Caverns Whiteout clone.

## Next dependency-valid slices

1. Connect the existing Underworld hazard/Deep Boon results to `ApplyMitigation`; do not duplicate their rules.
2. Add/kitbash missing donor particle effects where the installed game's named particle systems do not provide spores, sulfur ash, fracture dust or decay aerosol.
3. Live-test all six forced environment families and Surface restoration, especially transitions during a vanilla storm outside.
4. Tune light/wind/visibility from screenshots/video only after donor scenery is present.
5. Add custom ambient loops later; the current clones intentionally suppress inherited Surface storm audio.
