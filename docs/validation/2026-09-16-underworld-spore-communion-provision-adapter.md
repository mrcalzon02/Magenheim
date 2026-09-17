# Underworld Spore Communion provision adapter — 2026-09-16

## Reconciliation

The authoritative `main` branch advanced through the manifest-backed Underworld physical world-switch coordinator after the prior provision-adapter slice. The Deep Boon dependency chain remained intact: Core owns exact fungal-provision eligibility and bounded efficiency; Runtime owns durable Deep Boon projection; Spore Communion already owned poison adaptation and the neutral/fail-closed provision multiplier. The remaining gap was that successful food consumption never applied that multiplier to Valheim's player-owned active food state.

## Material implementation

- `SporeCommunionRuntime` retains `Balance.Underworld.DeepBoons.SporeCommunion.FungalProvisionEfficiencyBonus`, defaulting to +20% and bounded by Core to +50%.
- Added `ApplyConsumedProvision(Player, ItemDrop.ItemData)` at the player-owned food-state boundary.
- Added a postfix on the exact `Player.EatFood(ItemDrop.ItemData)` overload; failed consumption is ignored.
- After successful consumption, Runtime first passes through `ProvisionEfficiencyMultiplier`, which requires authoritative `DeepBoonRuntime.IsActive(player, "spore_communion")` and exact Core-owned fungal prefab admission.
- Runtime obtains the consuming player's private `m_foods` collection through Harmony `AccessTools`, then matches the newly created/replaced `Food` entry by the exact consumed `ItemData` reference.
- Only that player-owned `Food` entry's `m_health`, `m_stamina`, and `m_eitr` values are multiplied. Duration is intentionally unchanged: the boon improves nutritional efficiency rather than making food persist longer.
- `ItemDrop.SharedData`, prefab food statistics, inventory item definitions, vanilla food, third-party food, and other players' active food entries are never mutated.
- If Valheim changes the private `m_foods` field shape, the hook fails neutral instead of falling back to shared-state mutation.

## Verification boundary

Repository inspection, external API-shape corroboration, GitHub write, and read-back are available in this cycle. Current Valheim mod code corroborates `Player.EatFood(ItemData)`, private `m_foods`, and player-owned `Food.m_item/m_health/m_stamina/m_eitr` state. Local .NET compilation, installed-Valheim API execution, Jötunn runtime execution, and live multiplayer food testing are not claimed.

## Next dependency-valid slice

Spore Communion's two designed mechanical behaviors now have concrete runtime paths: poison adaptation and canonical fungal-provision nutrition. The next bounded slice should close the boon as a reference implementation by adding deterministic/runtime-facing validation where possible and checking installed-game behavior when an executable environment is available. Unless that reveals a defect, stop expanding Spore Communion plumbing and begin the next Deep Boon, Deep Current, by defining its Core-owned identity/balance contract and first concrete movement/water adaptation behavior through the established `DeepBoonRuntime` authority.
