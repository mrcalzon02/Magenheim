# Underworld bespoke weather closure — 2026-09-29

## Scope

This record closes the source-side weather/VFX/effects/gameplay item that was explicitly logged as
open on 2026-09-28. It does not claim live Valheim acceptance.

## Event authority

`UnderworldWeatherCycle` remains the deterministic selector. It derives four-minute event windows
from the paired Underworld seed, biome and Valheim world time. Surface weather tables remain
untouched and biome-illegal events remain rejected by Core authority.

## Visual expression

`UnderworldWeatherVfxRuntime` provides Magenheim-owned procedural two-layer particle profiles for
Sporefall, Deep Fog, Ashfall, Thermal Surge, Whiteout, Stone Rain, Crystal Resonance and Black Bloom.
The visual field is local presentation only; it does not decide gameplay damage.

Crystal Resonance additionally scans a bounded 55m local radius for up to 40 active
crystal/geode/shard renderers, captures their current MaterialPropertyBlocks, applies a synchronized
emission pulse and restores the captured blocks when the event ends.

## Gameplay expression

`UnderworldWeatherGameplay` is pure Core pressure authority and
`UnderworldWeatherGameplayRuntime` is the server adapter.

- Sporefall, Deep Fog and Crystal Resonance do not gain arbitrary direct damage.
- Ashfall adds modest fire pressure.
- Thermal Surge adds stronger fire pressure.
- Whiteout adds frost pressure.
- Stone Rain uses deterministic intermittent blunt/stagger pulses.
- Black Bloom adds poison pressure and a temporary Magenheim-creature perception boost.

The aggression boost captures authored MonsterAI alert/view/hearing ranges and restores them after
the event hold expires.

Thermal Surge also feeds the existing geothermal hazard path through
`GeothermalIntensityMultiplier`, so vents/lava become more dangerous during the event instead of
creating a second unrelated heat system.

## Mitigation and progression

`UnderworldWeatherMitigationRuntime` resolves already-owned progression rather than duplicating
unlock authority.

- matching Deep Boon: atmosphere resistance;
- each matching equipped Underworld armour piece: additional resistance;
- Defiant Censer: Great Decay suppression.

The Defiant Censer is now runtime-registered from the canonical equipment catalog using its
authored model/icon and Crown Reliquary recipe. It carries no combat damage. While actively held,
it creates a 16m local clearing radius that applies to nearby players in the same scene. Suppression
reduces shared exposure/fog and the bespoke particle emission field.

The other five authored Underworld tool identities remain outside this closure until their own
gameplay verbs are implemented.

## Test authority

`UnderworldWeatherGameplayTests` is part of the Core test runner and covers harmless-event
invariants, hostile damage language, Thermal Surge > Ashfall, Whiteout frost, Stone Rain
intermittency/stagger, Black Bloom biological activity, mitigation response and illegal-event
containment.

`TESTING.md` contains the live 0.0.154 acceptance matrix for all events, host/client behavior,
mitigation, Censer radius, Crystal Resonance restoration and Surface weather restoration.

## Acceptance boundary

The source implementation, documentation and test wiring are committed. This environment does not
contain a .NET SDK and cannot reach GitHub over ordinary container networking, so no local compile
or Valheim execution is claimed here. A rebuilt 0.0.154 package must pass the project build gates
and the live acceptance matrix before weather is called runtime-accepted.
