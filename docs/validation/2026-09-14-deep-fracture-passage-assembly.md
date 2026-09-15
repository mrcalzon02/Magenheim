# Deep Fracture physical passage assembly — 2026-09-14

## Scope

This bounded slice advances the existing collision-safe Deep Fracture route authority into deterministic runtime-placement instructions and a concrete Unity/Jötunn assembler. Surface Deep Fracture location registration remains deliberately disabled.

## Pure-core assembly authority

`DeepFracturePassageAssemblyCompiler` consumes only the validated interior blueprint and the existing `DeepFracturePassageRouter`. Every routed leg is divided into deterministic segments no longer than the registered 16m passage template. Each segment records connection identity, stable per-connection sequence index, center point, exact longitudinal length and yaw.

This prevents runtime from recomputing topology or inventing its own route. MainRoute/Branch graph authority remains in the pure core; the runtime receives placement instructions only.

## Runtime realization

`DeepFracturePassageAssembler` resolves the already registered Magenheim passage Room, instantiates one copy per compiled segment under an explicitly supplied isolated-interior root, places it at the core-authored center/yaw, and scales only the longitudinal axis for the final fractional segment. Width and height remain those of the registered passage geometry.

No vanilla or foreign room/prefab is modified. The assembler creates only Magenheim-owned instances from the Magenheim private room authority.

## Admission boundary

This does not activate `DeepFractureLocationRegistrar`. A complete binder still has to place the district rooms, call this assembler, realize traversal links, bind entrance/exit behavior and provide persisted restoration before surface locations may enter world generation.

## Verification

Repository read-back confirms the collision-safe router remains authoritative and the runtime passage template remains 16m long. This connector run does not have the local Valheim managed-assembly build/profile boundary, so compilation and in-game placement are not claimed. The new source is committed directly to `main` and is suitable for the next local closeout compile gate.

## Next exact action

Implement the concrete isolated-interior binder that consumes `DeepFractureInteriorBlueprint`: place the 20-family district instances at their authored transforms, call `DeepFracturePassageAssembler.Assemble`, instantiate/configure traversal nodes for Loop/Shortcut links, and return a validated interior binding. Keep surface registration gated until restoration and entry/exit persistence are coherent.