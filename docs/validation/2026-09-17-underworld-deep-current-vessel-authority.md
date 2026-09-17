# Underworld Deep Current Vessel Authority — 2026-09-17

## Reconciled authority

Deep Current remains the Blackwater Maw Deep Boon. Its swimming-stamina and Wet-recovery adaptations are already implemented. The remaining designed mechanic is improved Underworld vessel handling, but authoritative `main` had no canonical Magenheim-owned Underworld vessel identity. Applying a handling patch directly to `Ship` would therefore have leaked the boon into vanilla and third-party vessels.

## Material implementation

- Added `UnderworldVesselCatalog` in Core as the sole admission boundary for vessels eligible for Underworld-specific handling adaptations.
- Established `Magenheim_Underworld_Vessel_BlackwaterSkiff` as the initial canonical Blackwater vessel identity.
- Added `HandlingMultiplier`, neutral for foreign/null identities and non-finite configuration, with a hard +50% ceiling.
- Added deterministic coverage proving exact/case-sensitive admission, vanilla raft/longship rejection, neutral foreign behavior, configured amplification, negative-value neutrality, non-finite neutrality, and the +50% clamp.
- Registered the coverage in `DefinitionAuthorityTests`.

## Verification boundary

Repository source was written directly to `main` and re-read through GitHub. No local .NET compilation, installed-Valheim API execution, Jötunn registration, ship prefab construction, multiplayer session, or live vessel handling measurement is claimed from this connector-only cycle.

## Next actionable slice

Create/register the Blackwater Skiff through existing Magenheim Runtime content authority, deriving its prefab identity from `UnderworldVesselCatalog`. Then bind Deep Current handling to that exact vessel through a player/ship-owned runtime boundary and `HandlingMultiplier`, without mutating vanilla `Ship` prefab values or admitting third-party ships. Once that path is connected, Deep Current is complete enough to close and progression can move to Furnace Blood.
