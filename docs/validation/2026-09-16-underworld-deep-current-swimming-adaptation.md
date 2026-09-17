# Underworld Deep Current swimming adaptation — 2026-09-16

## Reconciled authority

The Underworld implementation plan defines Deep Current as the Blackwater Maw boon and gives it three bounded directions: reduced Wet burden, improved swimming stamina, and improved Underworld vessel handling. Deep Boons remain mutually exclusive per player, separate from vanilla Forsaken powers, with exact values data-driven. The existing `DeepBoonRuntime` remains the sole runtime projection of durable selection.

## Material implementation

- Added `DeepCurrentRuntime` as the second concrete consumer of `DeepBoonRuntime.IsActive`.
- Added `Balance.Underworld.DeepBoons.DeepCurrent.SwimmingStaminaReduction`, defaulting to 25% and clamped to 0..60% so the boon cannot make swimming free.
- Added a narrow `Player.UseStamina` prefix. It changes the requested stamina charge only when the player is currently swimming and `deep_current` is the authoritative active boon.
- Registered the patch through Magenheim's existing gameplay Harmony owner and normal `UnpatchSelf` teardown path.
- No vanilla Forsaken state, shared item definition, or global stamina value is mutated.

## Verification boundary

Repository source was written directly to `main` and re-read through GitHub. No local .NET compilation, installed-Valheim API execution, multiplayer session, or live stamina measurement is claimed from this connector-only cycle.

## Next actionable slice

Continue Deep Current rather than adding another boon. Implement reduced Wet burden at the player-owned status-effect boundary without mutating the shared `SE_Wet` prefab or suppressing Wet entirely. After that, establish a narrowly scoped Underworld-vessel handling adapter so ordinary vanilla/third-party vessels do not inherit the boon accidentally. Once all three mechanics are connected, Deep Current becomes the second complete reference boon.
