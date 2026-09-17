# Underworld Live Transit Dependency Chain

**Recorded:** 2026-09-17  
**Authority:** implementation/acceptance archive for the separate persistent Underworld world-instance program.  
**Scope:** records the exact dependency chain required before a dev command, Deep Gate, or other caller may claim a working Surface ↔ Underworld transit.

## Acceptance invariant

A successful Underworld transit is not a coordinate teleport. It is:

```text
caller (dev command / Deep Gate)
→ prepare durable transition
→ resolve manifest-backed paired save
→ server/host owns physical handoff
→ unload/source session boundary
→ load exact paired target save
→ admit and verify physical target identity
→ resume persisted transition
→ place player at target anchor
→ commit transition
→ select independent target-layer map dataset
```

The derived Underworld save must activate Magenheim's Underworld terrain/environment authority. Returning must load the exact persisted parent Surface save and restore the recorded Surface return anchor.

## Ordered dependency chain

1. **Durable transition transaction — implemented.** Core transition intent, operation identity, authority fingerprint, rollback/recovery state and persisted player layer state remain the only transaction authority.
2. **World-pair manifest — implemented.** Surface save identity and deterministic derived Underworld save identity/seed are persisted and reversible.
3. **Physical session identity — implemented.** Runtime distinguishes the actual Surface save from the manifest-recognized derived Underworld save; Underworld-only content must fail closed in a Surface session.
4. **Restart reconstruction — implemented.** A Prepared transition can survive process shutdown and reconstruct its manifest-backed handoff when a physical session is admitted.
5. **Server/host ownership — implemented.** Clients may not resolve or approve physical save switches.
6. **Engine-facing physical loader — REQUIRED / unfinished.** Implement and prove the Valheim 1.0.12 API that consumes the exact target save name and changes the hosted physical session. Do not replace this with distant coordinates, a logical layer flag, or a fake loader.
7. **Caller/command dispatch — REQUIRED after loader.** Dev commands and Deep Gate interaction must use the same transition service and loader dispatch. Debug commands may bypass progression only when explicitly named as unsafe/test operations; production gates retain Nowhere King progression.
8. **Destination admission — framework implemented; live proof required.** New session UID/save identity must match the manifest target before recovery may continue.
9. **Target placement and return — framework implemented; live proof required.** Underworld entry resolves the Underworld arrival/Conclave anchor. Surface return resolves the exact persisted SurfaceReturnAnchor.
10. **Independent map persistence — REQUIRED / unfinished.** Overworld and Underworld exploration, fog, pins, death markers, boss/structure markers and shared/cartography data must be keyed by world layer/instance and cannot cross-reveal.
11. **Map selector UI — REQUIRED / unfinished.** Normal map gains `[ Overworld ] [ Underworld ]`; current physical world selects its tab automatically while manual inspection of the other dataset remains possible without changing worlds.
12. **Terrain/environment runtime — implemented source, live acceptance required.** A verified derived Underworld session activates custom Underworld terrain/biome/environment generation rather than vanilla Surface generation.
13. **Round-trip acceptance — REQUIRED.** Fresh entry, return, repeated transit, save/reload during each phase, crash/restart during Prepared handoff, host/client behavior, dedicated-server behavior, map isolation and terrain generation must be observed in a disposable runtime world.

## Dev-command acceptance

Do **not** document a command as functioning merely because it prepares a transaction. A command is accepted only when one invocation can be observed to load the paired Underworld save, activate custom terrain, place the player, display the independent Underworld map dataset, and later return to the original Surface save/anchor.

## Multiplayer constraint

A physical save switch is session-level authority, not ordinary per-player teleportation. The first production loader must therefore be server/host-directed. If Valheim 1.0.12 cannot host different physical worlds for different connected players in one server process, the implementation must explicitly choose and validate a whole-session handoff or paired-session/server architecture. It must never silently regress to a coordinate overlay.

## Immediate implementation sequence

```text
engine loader API proof
→ loader dispatch/idempotency
→ command + Deep Gate integration
→ destination admission/placement live proof
→ map store separation
→ map tabs
→ full Surface ↔ Underworld round-trip test
```

Content teams may continue terrain, biome, ecology, structure and asset work against the existing derived-world identity contracts, but none of those tracks may redefine transition, world-pair, map-layer or persistence authority.
