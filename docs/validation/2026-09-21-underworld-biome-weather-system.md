# Underworld biome-owned weather system — source validation

**Date:** 2026-09-21
**Source candidate:** 0.0.91
**Status:** committed source implementation; compile/installation/live acceptance not claimed.

## Problem closed in source

The six custom Underworld biomes were still exposed to whatever Surface `EnvMan` weather happened
to be selected for the host presentation context, which allowed ordinary Valheim storm weather to
appear below. That contradicts the design rule that the Underworld has subterranean environmental
events rather than a normal Surface weather cycle.

## Implementation

- `UnderworldWeatherCycle` is pure Core authority. It derives a four-minute period from Valheim's
  server-authoritative world time, hashes that period with the paired Underworld seed and biome, and
  selects only events legal to that biome. Every peer therefore reaches the same presentation state
  without inventing a second replicated weather-state machine; future gameplay effects must still be
  re-evaluated/admitted server-side rather than trusting client-reported state.
- `UnderworldWeatherRuntime` lazily registers namespaced `EnvSetup` clones using installed
  Valheim environments as donors. It does not rewrite `EnvMan.m_biomes` or any Surface weights.
- While the local player is in the active Underworld instance, the matching Magenheim environment
  is forced through Valheim's existing `EnvMan.SetForceEnvironment` path. Leaving the instance
  clears the forced environment and returns control to Valheim.
- The runtime feeds the selected event/intensity into `UnderworldAtmosphereRuntime`, so weather,
  fog/exposure, wind and event identity advance together.
- Surface rain-cloud alpha is zeroed in every custom Underworld environment. Thunder/rain/storm
  particle identities are filtered. Whiteout alone admits a storm-named particle when its identity
  also contains `snow`.
- Donor ambient loops are currently suppressed rather than risking rain/thunder audio below. Custom
  subterranean ambience remains a later content pass.

## Deterministic coverage added

`UnderworldWeatherTests` verifies:

- identical seed/time/biome produces identical state;
- every selected state belongs to that biome's event vocabulary;
- calm windows carry zero event intensity;
- all intended events appear over a 512-window schedule;
- the shared rare Crystal Resonance event remains reachable in all six biomes;
- derived instance seed changes the sequence;
- malformed world time fails closed.

## Compatibility boundary

Magenheim owns only `Magenheim_Underworld_*` environment names. Existing Valheim or foreign
environment entries are observed as donors and never overwritten or removed. The runtime removes
only the names it registered when its component is destroyed.

## Remaining runtime gates

A current Valheim/Jotunn build must still prove the exact installed `EnvSetup`/EnvMan API surface,
then a disposable-world visual run must traverse all six biomes and specifically test entering and
leaving the Underworld while the Surface is in ThunderStorm. Multiplayer should confirm peers see
the same world-time/seed-derived event window. No stronger claim is made by this record.
