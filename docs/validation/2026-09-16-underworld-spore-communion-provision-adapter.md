# Underworld Spore Communion provision adapter — 2026-09-16

## Reconciliation

The authoritative `main` branch had advanced through Radiance Warden presentation work after the fungal-provision bootstrap slice. The Underworld dependency chain remained intact: Core owns the exact three eligible fungal prefab identities and the bounded efficiency calculation; Runtime owns Deep Boon projection and Spore Communion poison adaptation; the three fungal foods are registered through normal Magenheim content bootstrap.

## Material implementation

- Extended `SporeCommunionRuntime` with `Balance.Underworld.DeepBoons.SporeCommunion.FungalProvisionEfficiencyBonus`, defaulting to +20%.
- Added `ProvisionEfficiencyMultiplier(Player, ItemDrop.ItemData)` as the narrow Runtime consumption adapter.
- The adapter first requires authoritative `DeepBoonRuntime.IsActive(player, "spore_communion")`.
- Runtime resolves only `item.m_dropPrefab.name`; exact eligibility and the +50% upper bound remain owned by `UnderworldFungalProvisionCatalog` in Core.
- Null players/items, inactive boons, vanilla food, third-party food, and noncanonical lookalikes resolve to the neutral multiplier.
- The adapter does not mutate `ItemDrop.SharedData`, prefab food statistics, or any globally shared content object. This prevents one player's boon from leaking altered food values to other players.

## Verification boundary

Repository inspection and GitHub write/read-back are available in this cycle. Local .NET compilation, installed-Valheim API execution, Jötunn runtime execution, and live food-consumption behavior are not claimed.

## Next dependency-valid slice

Bind the adapter to Valheim's consumed-food instance at the narrowest stable player food-consumption boundary. Apply the multiplier only to the consuming player's canonical provision instance (prefer duration/benefit state owned by the player's food entry), never by temporarily rewriting shared item/prefab data. Validate switch/clear behavior, canonical versus foreign food, multiplayer isolation, and reconnect behavior before declaring Spore Communion complete. If the installed Valheim API shape does not expose a safe per-player food entry at `EatFood`, inspect the actual referenced assembly/API and adapt at the downstream player-food state boundary rather than using a shared-data mutator.
