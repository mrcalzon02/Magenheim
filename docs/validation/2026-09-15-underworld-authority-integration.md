# Underworld U0/U1 authority integration and baseline repairs — 2026-09-15

## Scope and starting evidence

Started with clean local main at ec08754, 74 commits behind fetched origin/main.
Fast-forwarded to 5e2c385 without replacing local changes or creating another branch.
Read UNDERWORLD_DESIGN.md, its implementation program and A0 validation record.
Broken baseline behavior took priority over biome production.

## U0 extension map

| Concern | Existing authority | Extension / remaining gate |
|---|---|---|
| JSON | MagenheimDefinitionLoader.LoadFromJson, foundation.json | Schema 5 explicitly carries Underworld and architecture components; absent components must be explicit null. No second loader. |
| Validation / hash | MagenheimDefinitionValidator | Revalidates/freezes both components and recomputes their hashes before including them in the canonical hash. |
| Config | MagenheimDefinitionOverrideApplier | Preserves and revalidates both components through balance overrides. |
| Peer admission | DefinitionAuthoritySynchronizer / GameplayAuthorityFingerprint | Existing fingerprint chain now includes the components; no additional handshake or transport. Live peer mismatch test remains open. |
| Unlock | DarkThroneEncounterSnapshot / UnderworldUnlockRules | Pure rule requires validated Defeated state. Runtime currently stores ZDO booleans, not this snapshot; no durable world-wide completion adapter exists. Rematch resets the runtime defeated flag. This must be reconciled before entry. |
| World identity | UnderworldWorldIdentityFactory | Existing deterministic identity remains; control characters rejected to prevent ambiguous canonical input. No world loading is claimed. |
| Runtime world / map / recovery | No admitted Underworld authority | U2 must prove actual Valheim world-context switching, independent maps, saves, reconnect, failure recovery and independent host/client travel. |
| Rootforged building | UnderworldArchitectureValidator, eleven A0 pieces | Catalog is fingerprinted but Understone/Worldroot harvesting, resource registration, recipes and runtime build pieces remain unimplemented. |
| Packaging | build.ps1 / closeout.ps1 / release.json | Same Magenheim assembly and profile delivery path. |

## Implemented

- Canonical schema 5 ships the six biome/boss/Deepstone relationships and eleven
  Rootforged structural definitions. These are data, not registered world content.
- Required-field JSON validation, unknown/duplicate-field rejection, invalid
  reference checks and canonical fingerprint recomputation are tested using the
  actual runtime loader linked into the .NET test harness. Core remains independent
  of JSON, Unity, Valheim and BepInEx libraries.
- Reject fingerprint separators in Underworld text and empty architecture identities.
- Replace unavailable System.Numerics.Vector3 dependency with pure EncounterPosition.
- Repair current-game hover, private Player.Update patch naming, helmet lookup,
  obsolete velocity API, missing imports, invalid declarations and lifecycle fields.
- Repair Deep Fracture routes that need multiple bends through a packed district
  grid. Deterministic visibility-graph fallback retains the exact collision guard.
  Regression checks independently test every segment against unrelated expanded
  footprints for 18 generated layouts, across all three sizes.
- Reconcile stale harness calls with canonical APIs, remove duplicated suite runs,
  run the previously omitted socket replay suite and compare route point values.
  Preserve the success-only refinement XP behavior introduced by 48241a5.
- Dais resummoning now validates actual offering ownership and proximity and consumes
  it only after successful summoning. Failed/non-authoritative summons keep the item.
  Persistent encounter mutations require server ownership. Remote resummon RPC is
  still unimplemented and must not be represented as multiplayer-admitted.

## Validation

Core harness: 36,834 assertions plus 25 separately reported module assertions.
Runtime: zero warnings and zero errors against installed Valheim and Jotunn 2.30.0.
All 20 Harmony targets and named argument bindings verified against installed game metadata.
Final closeout and startup evidence are recorded below after execution.

## Remaining gates

U0 source reconciliation and U1 initial schema/catalog integration are complete.
U1 is a foundation, not the full biome/ecology/resource schema envisioned for later phases.
No Underworld generation, transition, Deepstone boon, boss, resource or build-piece
runtime is admitted. Existing P0-P2 live-world, multiplayer and persistence gates
remain open. The next world-layer milestone is U2's disposable transition proof,
preceded by a durable Nowhere King completion adapter that survives rematches.

Route clearance tests cover unrelated district footprints, not passage-to-passage
collisions, walkable slope, runtime mesh seams or room doorway placement; those
remain live passage-assembly checks. Runtime combat and resummon changes require
host/client and save/load testing. No production-world acceptance is claimed.
