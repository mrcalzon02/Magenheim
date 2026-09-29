# Deep Gate terrain placement and bidirectional transit audit — 2026-09-29

Status: **source repaired and remote-read-back verified on main; live Valheim round-trip acceptance remains open.**

## Audit result

The gate transport path was already a real native-instance transfer rather than a coordinate-layer
simulation: physical gate interaction reaches the server-owned transit path, explicit player world-
instance membership changes, the player/ZDO is routed to the target native context and Unity scene,
and Valheim `Player.TeleportTo` performs the physical move. Surface return anchors are persisted on
the player's ZDO and transfer failure has rollback.

The placement audit nevertheless found four concrete defects:

1. `LastBossGate` is normalized so its visible foot is local Y=0, but the Underworld return gate
   was positioned at terrain height +1.25m, which could make the whole gate float.
2. Surface worldgen admitted as much as 8m of terrain-height delta over a 38m radius, far too much
   for a roughly 35.78 x 20.08m monumental gate.
3. The Underworld arrival country's fine terrain noise remained active across the gate footprint;
   grounding only the gate centre could still bury or float the corners.
4. Entry arrived on the gate's front side but used the gate's forward vector, so the player could
   face away from the arch.

A fifth authority gap was found in multiplayer: the transit RPC authenticated the peer/player but
did not prove that the player was physically near a matching gate.

## Surface gate placement

`UnderworldDeepGateLocationRegistrar` still registers six persistent Surface sites through Jotunn's
native `CustomLocation`/`LocationConfig` worldgen path. The gate remains the native Aesir
`LastBossGate` clone and keeps random Y rotation.

The placement envelope is now:

- six sites;
- eligible ordinary Surface biomes unchanged;
- minimum altitude 8m;
- exterior terrain-check radius 22m;
- maximum terrain delta 0.75m;
- no slope rotation;
- no water snapping;
- vegetation clearing retained;
- visible gate foot embedded only 0.08m below the location datum.

The 22m terrain radius exceeds the approximately 20.51m half-diagonal of the measured gate plan, so
the full monument footprint participates in the flat-site admission check.

## Underworld gate foundation

`UnderworldTerrainLifecycle` now owns
`deep-gate-foundation-v1-flat-footprint-blend`.

The canonical return-gate centre is native Underworld `(36, 0)`. Terrain is exactly level within
22m of that centre and blends smoothly back into the existing Fungal arrival terrain from 22m to
34m. This is applied by deterministic Core terrain authority, not by a runtime terrain-modifier
side effect.

The gate foundation algorithm is included in the composite Underworld fingerprint and the composite
authority schema advances from 9 to 10. Mixed old/new peers therefore fail closed rather than
generating different gate terrain.

`UnderworldWorldCenterRegistrar` consumes those same Core centre constants, embeds the normalized
return gate 0.08m into its foundation, and resolves the arrival point three metres beyond the
actual rendered gate footprint. The arrival point receives standing clearance and now faces back
toward the gate.

## Transit authority

The physical path remains:

`UnderworldGateEndpoint -> UnderworldGateTransitRpc -> UnderworldGateTransitRuntime`.

The server-side transfer:

- verifies source player-instance membership;
- requires the paired Underworld native context for entry;
- persists the exact Surface position/rotation before entry;
- routes the player ZDO/peer between native instance contexts;
- moves the player GameObject between the corresponding Unity scenes;
- changes explicit `ValheimWorldInstanceRegistry` membership;
- calls native `Player.TeleportTo` inside the target context;
- rolls scene/membership/ZDO routing back if transfer fails;
- restores the persisted Surface anchor and facing on return;
- clears the return anchor only after a successful Surface return.

Coordinates do not establish instance identity.

## Physical RPC admission

Normal gate interaction now additionally requires the server to find an active matching
`UnderworldGateEndpoint` in the same Unity scene within 6m of the gate collider/renderer envelope.
This check applies to local-host gate use and remote-client gate requests. A client therefore cannot
turn the known RPC into an arbitrary remote teleport.

The developer `magenheim_underworld enter|return` commands intentionally call the lower-level
transit runtime directly and remain the explicit test bypass.

## Static verification

Remote read-back of current `main` confirms all of the following together:

- `LastBossGate` remains the donor and its visible foot normalization remains present;
- six Surface gate sites remain configured;
- Surface terrain admission is 22m radius / <=0.75m delta / no slope rotation / no water snap;
- the 0.08m Surface foot embed is present;
- the deterministic 22m/34m Underworld foundation is present and called from terrain evaluation;
- the gate-foundation identity is in the composite authority fingerprint;
- Core tests cover measured-footprint containment, level samples and dry approach restoration;
- the Underworld return gate consumes the Core foundation centre and is embedded at terrain -0.08m;
- arrival is derived from actual renderer bounds plus 3m and faces back toward the gate;
- Surface anchors are both cached and persisted on player ZDO state;
- scene, ZDO/peer routing, explicit instance membership and native teleport are all in the transfer;
- rollback code remains present;
- remote requests resolve the authenticated server-side player;
- normal gate RPC requests require a matching same-scene physical endpoint within 6m;
- `TESTING.md` contains the full placement, round-trip, relog and multiplayer acceptance matrix.

## Verification boundary

The connector environment cannot resolve github.com from the local build container, so the updated
Core suite/runtime could not be executed here after these changes. No GitHub Action was introduced
or invoked for this audit.

Static/source coherence is therefore verified, but **actual runtime acceptance is not claimed**.
The closure evidence remains a disposable installed-Valheim test that completes:

Surface gate placement -> locked refusal -> unlocked Surface-to-Underworld transfer -> grounded
Conclave arrival -> Underworld return -> exact Surface anchor restoration -> relogged return ->
two-peer split-instance transit -> failed-transfer rollback.

`BACKLOG.md` P0.-1 should remain open until that live evidence exists.