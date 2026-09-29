# Underworld center leaked into Surface spawn

Status: open, confirmed in live 0.0.145 testing.

Evidence: `artifacts/field-reports/2026-09-28-underworld-center-surface-leak.png`

Observed behavior:

- The Underworld central Deepstone/standing-stone assembly was instantiated directly over the
  overworld starting spawn.
- Several of the Underworld stones render as flat, untextured grey geometry.
- The Aesir gate and chained stone assembly are visible within the same leaked Surface structure.

Required repair:

1. Keep `UnderworldWorldCenterRegistrar.Create` output exclusively in the detached Underworld
   Unity scene and verify its hierarchy after all deferred `Awake`/`Start` callbacks.
2. Prevent the Surface `ZoneSystem` or scene-routing patches from adopting or activating any
   Underworld world-center child.
3. **Source repair implemented 2026-09-29; live acceptance pending.** Every canonical Deepstone
   now uses its own multi-part model with explicit Magenheim-owned UV material-family textures,
   emission and bounded local light. The Descent Monolith also uses its own model. Re-test under
   live Underworld shaders/fog before closing this visual symptom.
4. Add an admission assertion that the Surface scene contains zero objects from the Underworld
   world-center hierarchy before gameplay begins.

