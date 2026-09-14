# Zone Observation Lifecycle Safety — 2026-09-14

## Defect

Static API review found that Jötunn `ZoneManager.GetZoneVegetation` directly dereferences `ZoneSystem.instance` when an identity is not already present in Jötunn's custom vegetation dictionary.

Magenheim's geode registrar executes from `PrefabManager.OnVanillaPrefabsAvailable`, an ObjectDB/prefab lifecycle event. A live `ZoneSystem` is not an invariant of that event. Calling `GetZoneVegetation` there therefore introduces a scene-order-dependent null-reference risk before Magenheim even reaches its additive registration decision.

## Repair

`JotunnWorldgenAdapter.ObserveDesiredHostIdentities` no longer calls `ZoneManager.GetZoneVegetation` during the prefab-availability registration pass.

It now observes the decisive collision boundary through `PrefabManager.GetPrefab` only. This is sufficient for the registration invariant because Jötunn `ZoneManager.AddCustomVegetation` itself calls `PrefabManager.AddPrefab` before adding the vegetation entry. Any existing vanilla/custom prefab identity that would make the additive vegetation registration unsafe is therefore observable through the same prefab namespace the Jötunn mutation path must claim.

This change does not weaken non-destructive behavior. The registrar's preflight still refuses an occupied prefab identity before mutation, and Jötunn's own `AddCustomVegetation` returns false if prefab registration fails.

## Version

Runtime source/package identity advanced to 0.0.14 because this changes runtime lifecycle behavior under patch-strict network compatibility.

## Validation boundary

The repair is source/API validated only. A current Valheim runtime must still prove registration succeeds at the chosen lifecycle point and remains idempotent across actual menu/world transitions.
