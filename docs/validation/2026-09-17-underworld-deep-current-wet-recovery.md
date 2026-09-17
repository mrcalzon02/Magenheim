# Underworld Deep Current wet recovery slice — 2026-09-17

## Reconciliation

The authoritative runtime already projected `deep_current` through `DeepBoonRuntime` and implemented its swimming-stamina adaptation. The next dependency-valid design edge was reduced Wet burden. Recent mainline work also advanced the physical Underworld world-switch driver; this slice does not replace or fork that authority.

## Material implementation

- Added `Balance.Underworld.DeepBoons.DeepCurrent.WetDurationReduction`, default 35%, clamped to 0..60%.
- Added a narrow `SE_Wet.UpdateStatusEffect` postfix through the existing Magenheim gameplay Harmony owner.
- The adapter resolves the status effect's owning Character and applies accelerated recovery only when that owner is a Player whose authoritative active Deep Boon is `deep_current`.
- Recovery advances only the player-owned status-effect instance's elapsed time. It never edits the shared `SE_Wet` prefab, never changes its TTL, never removes Wet outright, and does not prevent normal environmental exposure from refreshing Wet.
- Reflection dependencies fail neutral: if Valheim no longer exposes the expected `StatusEffect.m_character` or `StatusEffect.m_time` fields, the adaptation does nothing rather than falling back to shared mutation.
- The acceleration formula converts the configured duration reduction into additional elapsed time, so 35% reduction targets 65% of vanilla recovery duration rather than merely adding 35% update speed.

## Verification boundary

Repository source was written directly to `main` and re-read through GitHub. No local .NET compilation, installed-Valheim API execution, multiplayer session, or live Wet-duration measurement is claimed from this connector-only cycle.

## Next actionable slice

Continue Deep Current rather than opening another boon. Establish the third and final adaptation at a narrowly scoped Underworld-vessel handling boundary. First identify the canonical Magenheim-owned Underworld vessel identities/authority; do not grant handling bonuses to arbitrary vanilla or third-party ships. Once vessel handling is connected, Deep Current is complete enough to close as the second reference boon and progression can move to the next Deep Boon.
