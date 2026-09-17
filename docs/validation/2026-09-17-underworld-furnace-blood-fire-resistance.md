# Underworld Furnace Blood fire-resistance slice — 2026-09-17

## Reconciliation

Deep Current is now structurally connected through swimming stamina, Wet recovery, canonical Blackwater Skiff admission/content, controller-specific steering, and explicit Harmony activation. No newer Underworld implementation superseded that state. The next dependency-valid boon is Furnace Blood, whose authoritative design calls for reduced thermal/heat buildup and improved resistance to fire/geothermal hazards without immunity.

## Material implementation

- Added `FurnaceBloodRuntime` as a concrete consumer of `DeepBoonRuntime.IsActive` for the authoritative `furnace_blood` selection.
- Added a bounded fire-damage reduction contract: 30% default, clamped to 0..75%, so the boon can never create fire immunity through this adapter.
- Applied the reduction only to `HitData.m_damage.m_fire` on player-owned incoming damage. Physical, frost, poison, lightning, spirit and other damage channels are untouched.
- Reused the already-active `Character.Damage` Deep Boon patch boundary instead of adding a second competing Harmony prefix to the same method. Spore Communion poison mitigation and Furnace Blood fire mitigation remain independently gated by their exact active boon IDs.
- No shared prefab, global damage definition, vanilla Forsaken state, or foreign content is mutated.

## Verification boundary

Repository read/write verification confirms the implementation is committed on authoritative `main`. This cycle does not claim local .NET compilation, installed-Valheim execution, multiplayer execution, or measured fire-damage behavior.

## Next actionable slice

Wire `FurnaceBloodRuntime.Configure` into normal plugin bootstrap so the bounded fire-resistance value is user-configurable, then implement Furnace Blood's distinct thermal/heat-buildup adaptation at the narrowest player-owned geothermal/status boundary. Do not represent the fire-damage reduction alone as completion of Furnace Blood.
