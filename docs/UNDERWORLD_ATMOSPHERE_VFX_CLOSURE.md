# Underworld Atmosphere VFX Closure

**Status:** SOURCE-COMPLETE FOR 0.0.154 — live visual/runtime acceptance remains required.

This document records the completed source-side coupling between biome atmosphere, deterministic
weather selection, bespoke visual effects, gameplay consequences and progression mitigation.
It does not replace the live acceptance matrix in `TESTING.md`.

## Runtime authority

`UnderworldWeatherCycle` selects one biome-legal four-minute state from the paired Underworld seed
and Valheim's authoritative world clock. `UnderworldWeatherRuntime` applies the matching
Magenheim-owned environment only while the local player occupies the admitted Underworld instance.
Surface rain/thunder/cloud/storm state is not used as Underworld weather authority.

`UnderworldAtmosphere` remains the shared deterministic obscuration authority. It evaluates fog,
visibility, particle density, exposure and audio damping from biome, local terrain/hazard, event
state, resistance and suppression.

## Bespoke event expression

`UnderworldWeatherVfxRuntime` owns two procedural particle layers per event rather than relying on
best-effort donor-name discovery.

- **Sporefall** — luminous descending healthy spores and secondary motes.
- **Deep Fog** — slow cold moisture/mist particulates, with Frozen and Blackwater colour identity.
- **Ashfall** — falling dark ash plus hotter sulfur particulate.
- **Thermal Surge** — rising ember/spark field plus heat haze particulate.
- **Whiteout** — dense fast wind-driven snow/ice particulate.
- **Stone Rain** — falling mineral fragments plus fracture dust.
- **Crystal Resonance** — local resonance motes, pulsing light, and nearby crystal/geode/shard
  renderers temporarily pulse emission before their original property blocks are restored.
- **Black Bloom** — dense biological aerosol plus dark stagnant particulate.

Suppression scales the bespoke particle field as well as the shared fog/exposure channels.

## Gameplay coupling

`UnderworldWeatherGameplay` and `UnderworldWeatherGameplayRuntime` provide server-authoritative
material consequences from the same deterministic event state.

- Sporefall and Crystal Resonance remain non-hostile.
- Deep Fog remains navigation/visibility pressure rather than arbitrary health loss.
- Ashfall applies modest hot-particulate fire pressure.
- Thermal Surge applies stronger fire pressure and amplifies existing geothermal vent/lava
  exposure rather than creating a disconnected hazard system.
- Whiteout applies frost pressure in addition to cold/visibility effects.
- Stone Rain produces intermittent deterministic blunt/stagger impacts rather than a continuous DOT.
- Black Bloom applies poison/contamination pressure and temporarily increases nearby Magenheim
  Underworld creature perception/aggression ranges; authored AI values are restored when the bloom
  ends.

Damage and creature reactions are admitted from server world time and Underworld instance/biome
authority. Local particle rendering is not allowed to decide gameplay damage.

## Mitigation

`UnderworldWeatherMitigationRuntime` now feeds the existing `ApplyMitigation(...)` inputs.

- the biome-matching active Deep Boon contributes atmosphere resistance;
- each equipped matching Underworld armour piece contributes additional resistance;
- the **Defiant Censer** is now a runtime-registered Crown Reliquary item rather than a catalog-only
  future dependency;
- an actively held Censer creates a 16 metre Great Decay suppression radius for nearby players,
  reducing exposure, fog and bespoke particle density without deleting the biome's minimum identity.

The other five authored Underworld tools remain separate gameplay slices; they are not falsely
registered as complete merely to make the Censer symmetrical.

## Source verification

Core tests cover deterministic event vocabulary, harmless-event invariants, hostile damage
language, Stone Rain intermittency, Black Bloom biological activity and mitigation response.
The plugin bootstraps the bespoke VFX/gameplay runtimes and the Defiant Censer registrar.

## Live acceptance still required

0.0.154 must still be built and exercised in Valheim before this feature is called live-accepted.
For all six biomes verify calm baseline, every native event, border transitions, day/night
readability, multiplayer agreement, save/reload and correct Surface restoration.

Specifically verify:

- Sporefall reads as luminous spores, not rain or poison gas.
- Ashfall/Thermal Surge are visually distinct and Furnace Blood/Emberiron mitigation is noticeable.
- Whiteout materially reduces visibility without becoming permanently unnavigable.
- Stone Rain impacts match visible falling debris and do not double-hit between host/client.
- Crystal Resonance visibly pulses nearby Magenheim crystals and restores materials afterward.
- Black Bloom increases danger and creature alertness only while active.
- Deep Boons plus matching armour reduce gameplay pressure without erasing the visual weather.
- a held Defiant Censer clears a roughly 16 m Great Decay work area for both carrier and nearby
  players, and full contamination returns outside that radius.
- exiting the Underworld restores Surface fog, sky and weather with no residual particles/lights.

The source-side weather/VFX/effects/gameplay coupling item is therefore closed; only live acceptance
and tuning remain open.
