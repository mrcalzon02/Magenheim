# Underworld fungal provision bootstrap slice — 2026-09-16

## Reconciliation

Authoritative `main` was reconciled after the prior fungal provision content slice. Intervening Radiance and geode presentation commits did not alter the Underworld content authority. `UnderworldFungalProvisionRegistrar` existed and defined the three Core-owned provisions, but it was not instantiated by `MagenheimPlugin`, so none of that concrete content could enter the normal Jotunn registration lifecycle.

## Material implementation

`MagenheimPlugin` now owns one `UnderworldFungalProvisionRegistrar`, registers it during the normal content bootstrap, and disposes it during plugin teardown. This preserves the registrar's existing fail-closed prefab/dependency checks and keeps the canonical food identities owned by `UnderworldFungalProvisionCatalog` rather than duplicating them in bootstrap code.

This closes the missing lifecycle edge between the previously committed provision definitions and the live Magenheim content registration graph. It does not claim that the foods have been observed in a running Valheim instance.

## Verification boundary

Repository source was re-read after the write and the remote `main` head was verified. No local compiler, Jotunn runtime, Valheim process, recipe UI, or food-consumption test was available in this connector-only cycle, so no stronger runtime claim is made.

## Next actionable slice

Complete Spore Communion rather than adding another framework: add a bounded configurable fungal-provision efficiency bonus and apply it through a narrow player-food runtime adapter that derives eligibility from `UnderworldFungalProvisionCatalog`. The adapter must affect only the three exact Magenheim prefab identities, must not mutate shared item/prefab definitions, and must leave vanilla and third-party food untouched. Once that is implemented and deterministically covered where possible, Spore Communion can serve as the first complete Deep Boon reference implementation before Deep Current begins.
