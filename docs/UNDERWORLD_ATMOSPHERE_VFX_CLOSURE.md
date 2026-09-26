# Underworld Atmosphere VFX Closure

**Status:** OPEN — runtime authority exists; visual-effects package and live acceptance are incomplete.

**Purpose:** Preserve the distinction between the implemented Underworld atmosphere/weather systems and the remaining player-visible VFX work. This is a closure document, not a replacement for `UNDERWORLD_ATMOSPHERE_SYSTEM.md`.

## 1. What is already implemented

The Underworld already has deterministic atmosphere and biome-owned weather authority. `UnderworldAtmosphere` evaluates visual density, visibility distance, particle density, gameplay exposure, audio damping and fog colour from biome and local conditions. `UnderworldWeatherCycle` selects biome-legal events from synchronized world time, and `UnderworldWeatherRuntime` applies Magenheim-owned environment clones while the player is in the Underworld.

The current event vocabulary is:

- Sporefall
- Deep Fog
- Ashfall
- Thermal Surge
- Whiteout
- Stone Rain
- Crystal Resonance
- Black Bloom

This code-level implementation MUST NOT be described as final visual completion. Automated/core validation is not a substitute for observing the effects in the running game.

## 2. Missing or incomplete visual packages

The six biomes require persistent, readable atmosphere in calm conditions as well as event intensification. Several required particle/VFX expressions still need donor resolution, kitbashing or Magenheim-authored effects.

| Biome | Required baseline identity | Event intensification | Closure state |
|---|---|---|---|
| Fungal Forest | Luminous healthy spore haze; visible particulate depth without poison-cloud presentation | Sporefall; Crystal Resonance | **INCOMPLETE VFX** — confirm/complete spore particle package and live density |
| Blackwater Deep | Cold moisture mist pooled over water and low terrain | Deep Fog; Crystal Resonance | **INCOMPLETE ACCEPTANCE** — verify low-terrain/water pooling and navigation-distance reduction live |
| Sulfurous Wastes | Localized chemical/geothermal miasma in hazard areas and basins | Ashfall; Thermal Surge; Crystal Resonance | **INCOMPLETE VFX** — sulfur/ash particulate package still requires donor/kitbash/custom completion |
| Frozen Caverns | Persistent low ice fog while preserving long readable views when calm | Deep Fog; Whiteout; Crystal Resonance | **INCOMPLETE ACCEPTANCE** — Whiteout may use admissible native snow-storm particles; verify transition and visibility live |
| Fracture Zones | Normally clear hostile air with localized mineral dust | Stone Rain; Crystal Resonance | **INCOMPLETE VFX** — fracture/tremor dust package still requires donor/kitbash/custom completion |
| Great Decay | Persistent biological aerosol/miasma, strongest in low and hazardous terrain | Black Bloom; Crystal Resonance | **INCOMPLETE VFX** — decay aerosol package still requires donor/kitbash/custom completion; calm state must remain visibly contaminated |

## 3. Mistlands-like baseline rule

Weather events are not the biome atmosphere. Every Underworld biome must remain visually identifiable when its weather state is calm.

The intended model is comparable to Mistlands in the limited sense that atmospheric obscuration is a persistent spatial characteristic of the biome rather than only a temporary weather roll. Magenheim must retain its own visual language and must not simply copy Mistlands fog globally.

Baseline obscuration is terrain-sensitive. Height, water depth and local hazard state can alter density. Great Decay remains substantially obscured during calm weather. Blackwater moisture should read most strongly around water and low terrain. Sulfurous miasma should remain associated with geothermal/hazard terrain. Fungal Forest should carry readable luminous particulate depth even without Sporefall.

## 4. Remaining implementation work

1. Resolve installed-Valheim donor particle systems suitable for spores, sulfur/ash, fracture dust and biological aerosol.
2. Kitbash donors where they can meet the required identity without copying inappropriate surface weather presentation.
3. Author Magenheim-specific particle/VFX assets where donor coverage is insufficient.
4. Connect the existing hazard/Deep Boon results to atmosphere mitigation through `ApplyMitigation(...)` without duplicating progression rules.
5. Preserve the existing rule that normal Underworld environments do not inherit surface rain clouds, thunder/rain particles or surface storm ambient loops.
6. Keep Frozen Caverns Whiteout as the deliberate exception for appropriate native snow-storm particles.
7. Add biome-specific ambient audio only after the visual atmosphere has passed acceptance; do not restore inappropriate donor storm loops as a shortcut.

## 5. Live acceptance gate

No biome atmosphere is **DONE** until observed in a current packaged build.

For each of the six biomes, capture and evaluate:

- calm baseline atmosphere;
- every biome-native weather event;
- transition from calm to event and back;
- transition across biome borders;
- low versus high terrain where relevant;
- water-adjacent behavior where relevant;
- hazard-area behavior where relevant;
- day/night readability under the shared cavern roof;
- visibility of monumental terrain and navigation landmarks;
- player readability in combat;
- multiplayer agreement between host and peer;
- save/reload consistency;
- Underworld exit followed by correct restoration of surface weather/environment state.

The visual pass must explicitly reject effects that are technically active but effectively invisible, effects so dense that ordinary navigation becomes unintentionally hostile, and effects that resemble surface rain/storm weather rather than subterranean atmosphere.

## 6. Current known review target

Fungal Forest calm visibility currently targets approximately 0.8 km rather than the former approximately 11 km so background spore haze can become perceptible. This is a tuning target, not an accepted final value. Density, brightness, colour, particle scale and landmark readability remain subject to live review.

## 7. Definition of done

Underworld atmosphere/VFX closure is complete only when:

- all six baseline atmospheric identities are visibly present;
- missing spore, sulfur/ash, fracture-dust and decay-aerosol packages are resolved;
- all eight named weather events have an appropriate player-visible expression wherever biome-legal;
- event and baseline atmosphere interact coherently rather than replacing one another;
- hazard resistance and local suppression affect the intended channels without erasing biome identity;
- surface weather does not leak into the Underworld and restores correctly on exit;
- host/client presentation agrees;
- the packaged build has been manually accepted from in-game observation.

Until those conditions are met, the Underworld atmosphere system is **implemented but visually incomplete**.