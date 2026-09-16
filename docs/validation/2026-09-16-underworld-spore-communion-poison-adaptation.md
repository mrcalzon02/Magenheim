# Underworld Spore Communion poison-adaptation slice

## Reconciled authority

The authoritative Underworld plan defines Spore Communion as The First Bloom's Deep Boon and directs it toward fungal toxin/poison-pressure resistance plus improved efficiency from Underworld fungal provisions. Deep Boons remain separate from vanilla Forsaken powers, only one Magenheim Deep Boon may be selected at a time, and exact values must remain data-driven.

The existing `DeepBoonRuntime` is the sole runtime projection of the durable, server-revalidated player selection. This slice consumes that authority rather than creating another unlock or selection path.

## Material implementation

- Added `SporeCommunionRuntime` as the first concrete consumer of `DeepBoonRuntime.IsActive`.
- Added a `Character.Damage` prefix that changes only incoming poison damage for a `Player` whose sole active Magenheim boon is `spore_communion`.
- Added `Balance.Underworld.DeepBoons.SporeCommunion.PoisonDamageReduction` to BepInEx configuration. The default is 0.35 and runtime clamps the value to 0..0.90 so configuration cannot turn the adaptation into poison immunity.
- Registered the patch through the existing gameplay Harmony owner, preserving normal `UnpatchSelf` teardown and avoiding a second patch authority.
- The implementation does not inspect or mutate vanilla Forsaken-power state.

## Validation boundary

This repository-only cycle verified source placement, authoritative identity (`spore_communion`), configuration wiring, and gameplay-patch registration on `main`. It does not claim local .NET compilation, installed-Valheim API compatibility, multiplayer execution, or live damage validation because those execution surfaces are not available through the repository connector.

## Next actionable slice

Complete Spore Communion rather than immediately starting the next boon: define the canonical Underworld fungal-provision identity set through existing content authority, then implement a bounded, data-driven provision-efficiency benefit that cannot amplify arbitrary vanilla or third-party food. After that behavior is deterministic and runtime-wired, use the established Deep Boon patch pattern for Deep Current.
