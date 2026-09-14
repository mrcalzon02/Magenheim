# Geode world-prefab identity boundary — 2026-09-14

## Reconciled starting point

Authoritative development remained on `main`. The cycle began from live `main` commit `cb643b8216d9e8148d1d46ce8684f4ad0e8984e7`, whose latest material change added the read-only Jötunn worldgen adapter.

The current definition model exposes one geode `PrefabName`. `GeodeItemRegistrar` correctly uses that identity for the intact inventory item, currently cloned from vanilla `Stone` as a temporary visual. `DefinitionWorldgenPlanner`, however, was also forwarding the same identity into desired vegetation registrations.

That violated the recorded runtime boundary: the temporary Stone-backed inventory item must not become world vegetation, and the eventual mineable geode requires its own `ZNetView`/destructible world-object prefab.

## Repair

Added `GeodeWorldPrefabIdentity` as the single pure-core derivation authority for the mineable world-object identity.

For an authoritative item prefab such as:

`Magenheim_Geode_Meadows_Earth`

world generation now targets:

`Magenheim_Geode_Meadows_Earth_World`

The mapping is deterministic and rejects empty, whitespace-padded, non-Magenheim, and already-world-suffixed item identities. This avoids introducing a second independently editable definition field that could drift away from the item identity while still guaranteeing that inventory and world objects cannot accidentally share the same prefab.

`DefinitionWorldgenPlanner` now uses the derived world identity for collision observation and vegetation planning. The inventory item registrar remains unchanged and continues to own only the item identity.

## Deterministic coverage added

`DefinitionWorldgenPlannerTests` now asserts that:

- the definition registration key remains unchanged;
- worldgen derives `Magenheim_Geode_Meadows_Earth_World`;
- the planned world prefab is distinct from the item prefab;
- host collisions against the dedicated world prefab remain non-destructive and produce a skip under conservative policy;
- inventory prefab names cannot consume the reserved `_World` suffix.

The test source has been committed and statically reviewed. It has not been executed in this host because the required .NET SDK/compiler remains unavailable.

## Admission boundary

This cycle does **not** create or register the mineable prefab itself. It only repairs the identity contract so the upcoming prefab cannot accidentally alias the Stone-backed inventory item.

No vegetation was registered, no world was mutated, and no compile/runtime success is claimed.

## Next exact action

Create `Magenheim_Geode_Meadows_Earth_World` as a Magenheim-owned mineable prefab with persistent network/destructible behavior and an explicit intact-geode drop, validate that prefab in a current Valheim/Jötunn runtime, then bind approved `DefinitionWorldgenPlanner` additions through the thin Jötunn vegetation registrar. Runtime placement must continue to obey the validated compatibility policy and must never register the inventory item prefab as vegetation.
