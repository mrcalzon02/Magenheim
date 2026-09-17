# Underworld Spore Communion fungal provision authority — 2026-09-16

## Reconciled authority

Spore Communion is The First Bloom's Deep Boon. Its remaining design requirement is improved efficiency from Underworld fungal provisions. The Fungal Forest package already defines Glowcap tissue, Mycelial Fiber, Lantern Spores and Heartcap fragments as resources feeding food/alchemy, but it does not yet provide canonical food prefab identities. The existing poison adaptation remains unchanged.

## Material implementation

Added `UnderworldFungalProvisionCatalog` in Core as the sole admission boundary for foods that Spore Communion may amplify. The first content identities are `Magenheim_Underworld_Food_GlowcapStew`, `Magenheim_Underworld_Food_MycelialBroth`, and `Magenheim_Underworld_Food_HeartcapRation`. Vanilla and third-party foods fail closed rather than being inferred from display names, ingredients, or substring matches.

The catalog also owns the data boundary for the efficiency multiplier: configured bonus is interpreted as a fraction, negative and non-finite values resolve to neutral, and amplification is capped at +50 percent. This keeps exact balance data-driven without allowing configuration to turn the boon into an unbounded global food multiplier.

Deterministic Core coverage verifies canonical uniqueness/ownership, exact admission, rejection of vanilla and third-party lookalikes, neutral handling for ineligible foods, normal configured amplification, and clamp behavior. The suite is registered under the existing definition-authority harness.

## Validation boundary

This cycle changed authoritative source and deterministic test registration on `main`. It does not claim local .NET execution, Valheim compilation, food-consumption API compatibility, or live gameplay validation because those execution surfaces are not available through the repository connector.

## Next actionable slice

Register the three canonical Fungal Forest provisions through the existing content authority and recipes, then consume `UnderworldFungalProvisionCatalog` from the runtime food path so Spore Communion applies its configurable efficiency only to those exact owned prefab identities. Do not broaden eligibility to vanilla or third-party foods. Once that runtime path is closed, Spore Communion is complete enough to become the reference implementation for Deep Current.
