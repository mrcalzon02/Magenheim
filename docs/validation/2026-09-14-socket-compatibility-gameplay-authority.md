# Socket compatibility gameplay authority — 2026-09-14

## Intent

Advance adaptive equipment socket compatibility without mutating shared vanilla or foreign prefabs. Reconcile the already-present per-item socket implementation, make compatibility controls complete enough to target item identity as well as prefab/category/mod origin, and prevent server/client socket-policy drift from authorizing persistent gameplay mutation.

## Reconciled baseline

Live `main` already contained substantially more socket implementation than the backlog/state summary described:

- namespaced per-item metadata key `magenheim.sockets.v1`;
- pure socket state/mutation, eligibility, extraction, effect, transaction, and replay-guard services;
- runtime `ItemSocketAdapter` that mutates only the individual `ItemDrop.ItemData.m_customData` dictionary;
- BepInEx socket compatibility configuration;
- Geologist's Workstation socket-management overlay;
- runtime effect/tooltip patches;
- deterministic socket source coverage.

The live plugin also already called `ModQuery.Enable()` before equipment classification, which is required because Jötunn's ModQuery is disabled by default. No duplicate bootstrap fix was added.

## Defects repaired

### Item-level compatibility gap

Compatibility previously supported prefab, category, and mod-origin controls but did not expose the distinct shared item identity. `EquipmentDescriptor` now carries optional `ItemName`, the Valheim runtime adapter fills it from `ItemData.m_shared.m_name`, and configuration exposes `IncludedItemNames` and `ExcludedItemNames`.

Explicit item exclusion is evaluated before adaptive category admission and therefore cannot be bypassed by an otherwise eligible weapon/armor/tool category. Explicit item inclusion can opt otherwise-unknown equipment into the configured explicit-include slot limit. Existing prefab/mod/category behavior remains intact.

### Identity normalization

`SocketEligibilityPolicy` now validates `SocketIdentityComparison` and fails closed on unknown enum values. Compatibility identity collections are de-duplicated with the configured exact/case-insensitive comparer instead of always using ordinal comparison.

### Multiplayer authority drift

Socket policy previously lived outside `MagenheimDefinitionSet.Fingerprint`. Two peers could therefore present matching definition fingerprints while using different slot limits or include/exclude compatibility rules.

`GameplayAuthorityFingerprint` now hashes the validated definition fingerprint together with all gameplay-significant socket eligibility fields:

- weapon/armor/shield/tool/utility slot limits;
- explicit-include slot limit;
- identity comparison mode;
- item include/exclude sets;
- prefab include/exclude sets;
- mod-origin include/exclude sets;
- excluded equipment categories.

Case-insensitive identities are canonicalized for hashing, while exact comparison preserves casing significance. `DefinitionAuthoritySynchronizer` now exchanges this composite gameplay fingerprint, so different socket compatibility policy fails mutation admission instead of silently diverging.

## Non-destructive invariant

No shared `ItemDrop`, shared-data object, vanilla prefab, foreign prefab, recipe, or foreign custom-data key is modified by this pass. Socket persistence remains confined to Magenheim's per-item `magenheim.sockets.v1` metadata. Compatibility rules decide whether Magenheim may operate on an item instance; they do not rewrite the source content.

## Source coverage added

`SocketCompatibilityAuthorityTests` covers:

- case-insensitive identity de-duplication;
- item-name opt-in for otherwise unknown equipment;
- item exclusion overriding category eligibility;
- rejection of unknown identity-comparison values;
- authority hash changes when socket limits change;
- authority hash changes when item exclusions change;
- case-insensitive casing equivalence in authority hashing;
- exact-mode casing significance.

## Verification boundary

Remote GitHub read-back confirms the new core fingerprint implementation, eligibility policy, item identity adapter, runtime configuration, synchronization constructor, plugin wiring, and test source are present on authoritative `main`.

This execution host does not expose `dotnet`, `csc`, `mcs`, or `msbuild`, so no new compile or deterministic-test pass is claimed here. Runtime 0.0.23 source must be rebuilt in the normal Valheim development environment before release admission.

## Branch reconciliation

During this pass the repository exposed `radiance-content`, `tmp-radiance-content`, and `__delete_me__` in addition to `main`. All three point to `a8b6947c696e4da71e4837ff1b731ac53e98a387`. Direct comparison shows current `main` contains that commit and is ahead; the side refs contain no unique material work and are safe to prune. The available GitHub connector in this session exposes branch creation/update but not branch deletion, so deletion is not falsely claimed.

## Next dependency-valid step

Rebuild and execute the full deterministic suite under the normal .NET/Valheim toolchain. Then validate the Geologist's Workstation socket overlay against vanilla and representative third-party equipment, especially item/prefab/mod-origin exclusions, case-insensitive identities, metadata persistence through drop/storage/repair/upgrade, and multiplayer refusal when socket gameplay fingerprints differ.
