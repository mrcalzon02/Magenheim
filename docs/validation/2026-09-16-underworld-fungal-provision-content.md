# Underworld Fungal Provision Content Slice — 2026-09-16

## Reconciliation

Authoritative `main` had already closed the Core admission boundary for Spore Communion fungal provisions in `UnderworldFungalProvisionCatalog`. Intervening work advanced reversible physical-world pairing and Venom presentation without superseding that Deep Boon boundary.

## Material implementation

Added `UnderworldFungalProvisionRegistrar` as the Runtime content adapter for the three Core-owned provision identities. It defines concrete food statistics and fail-closed Jotunn registrations for Glowcap Stew, Mycelial Broth, and Heartcap Ration, plus explicit cauldron recipes. The registrar derives prefab identities from Core rather than maintaining a second eligibility list and refuses occupied identities or missing dependencies instead of mutating foreign content.

The first recipes intentionally use existing Valheim Mistlands ingredients as bootstrap dependencies. Dedicated Fungal Forest harvest materials remain a later content replacement once those resources have authoritative runtime registrations; this avoids inventing unresolved prefab dependencies in the current slice.

## Verification boundary

Repository source was written directly to `main`. No .NET compilation, Jotunn registration execution, Valheim startup, recipe UI inspection, or multiplayer food consumption is claimed from the repository connector environment.

## Next actionable slice

Wire `UnderworldFungalProvisionRegistrar` into the existing Magenheim content bootstrap and teardown, then connect Spore Communion's runtime food-consumption path to `UnderworldFungalProvisionCatalog.EfficiencyMultiplier`. The runtime must amplify only the three exact Core-admitted prefab identities and must leave vanilla and third-party food untouched. After that end-to-end path is closed, replace bootstrap ingredients with dedicated Fungal Forest harvest resources as those resources become registered content and treat Spore Communion as the reference implementation for Deep Current.
