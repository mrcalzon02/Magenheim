# Underworld reserved-domain center admission

Date: 2026-09-15
Branch: `main`
Scope: source/static implementation reconciliation

## Reconciled defect

The authoritative spatial model hosts the logical Underworld inside a reserved parent-world vertical band. `UnderworldWorldCenterRegistrar` still implemented the Deepstone Conclave as a normal Jotunn `CustomLocation`, but `UnderworldWorldSessionLifecycle` intentionally never registered it and retained a stale comment about a separate-world loader. The result was dead center code: registering it would have violated the reserved-domain architecture, while leaving it unregistered meant the Conclave could never be admitted.

## Repair

`UnderworldWorldCenterRegistrar` is now a reserved-domain composition factory rather than a ZoneManager worldgen registrar. It constructs the six canonical Deepstones, central Descent Monolith, dais, and return Deep Gate at logical Underworld origin `(0,0,0)`, then maps that anchor through `UnderworldSpatialDomain.ToHostAnchor` using the current derived-world identity.

`UnderworldWorldSessionLifecycle` now admits exactly one center object after a local derived Underworld session identity resolves. The center is destroyed and its identity cleared on parent-world unload/change, alongside the existing logical context reset. This removes the stale separate-world-loader assumption and prevents the Conclave from entering ordinary surface random world generation.

## Architecture preserved

- deterministic coordinate mapping remains owned by `Magenheim.Core`;
- runtime consumes `UnderworldRuntimeServices.SpatialDomain` rather than recreating mapping rules;
- the surface Deep Gate location remains additive Jotunn worldgen;
- the Underworld center is unique session-owned content in the reserved host domain;
- the six Deepstone identities introduced by the prior progression repair remain unchanged.

## Validation boundary

The GitHub connector verifies committed source only. Compilation, plugin startup, host/client replication, Valheim zone streaming at the high reserved Y band, collider behavior, and live transition into the Conclave are not claimed in this cycle.

## Next actionable framework slice

Bind each physical Deepstone object to its canonical Deepstone ID and add the server-authoritative runtime interaction/persistence adapter around `UnderworldDeepstoneProgression.PlanMountTrophy`. The adapter must reconstruct synchronized mounted/boon state, consume exactly the Core-authorized trophy quantity, reject client-side mutation, and expose state without creating a second progression authority.
