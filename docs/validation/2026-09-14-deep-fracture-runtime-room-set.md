# Deep Fracture runtime room set — 2026-09-14

## Scope

This slice introduces the concrete Magenheim-owned runtime room set for the canonical Deep Fracture district catalog without activating surface Deep Fracture world generation.

The runtime now has one procedural `Room` prefab for every canonical district family `DF-01` through `DF-20`, plus a reusable physical passage room and a traversal-node room. These identities are registered through Jotunn's supported dungeon-room lifecycle rather than through vanilla theme mutation.

## Registration boundary

`DeepFractureRoomRegistrar` subscribes to `DungeonManager.OnVanillaRoomsAvailable`. This is the same lifecycle used by Jotunn's current dungeon example. During that callback Magenheim registers its private `MagenheimDeepFracture` theme and all twenty-two room prefabs. Jotunn performs its custom-room hash generation after this callback, allowing Valheim's persisted dungeon loading path to resolve the Magenheim room hashes later.

No vanilla room, theme, prefab, or DungeonDB entry is replaced or modified. All identities are Magenheim-owned.

## District geometry

The district prefabs are deliberately large first-pass environmental rooms rather than repeated generic boxes. Their geometry follows the canonical identities already established in core authority: Fracture Descent, Split Strata, Geode Cathedral, Buried River, Thermal Veins, Crucible Cavern, Rime Fault, Glacier Vault, Conductor Chasm, Fulmination Gallery, Compression Hall, Seismic Basin, Contaminated Grotto, Dissolution Works, Prism Hall, Sanctified Vault, Echoing Deep, Hollow Ossuary, Shaping Works, and Confluence Heart.

The nominal district envelope remains 96m. Perimeter geometry is intentionally broken rather than sealed because final passage approaches are authored by the exact-plan connection projection, not by vanilla `RoomConnection` random growth.

Elemental-state presentation is applied to placed crystal/seam/core renderers from the authoritative module elemental-state list. It does not reroll elemental identity in runtime.

## Traversal template

The traversal-node room carries a Magenheim runtime traversal component. The component has no independently generated destination: its target must be configured from an authoritative Deep Fracture connection. Until configured it remains dormant.

## Worldgen gate remains closed

The surface `DeepFractureLocationRegistrar` is intentionally not activated by this slice.

Static review of the previously drafted straight-line passage projection found a root-cause topology defect: a MainRoute edge can connect two spine modules whose sequence indices skip side-branch modules. With the current compact spatial blueprint, a straight physical corridor can therefore intersect one of those intervening districts even though the abstract graph validates.

The valid repair is to compile collision-safe passage routes from the authoritative physical graph. It is not acceptable to disable colliders, allow vanilla random topology to replace the graph, or activate worldgen and treat intersections as a cosmetic issue.

## Validation boundary

This connector environment still does not provide the normal Valheim/BepInEx/Jotunn compile and runtime profile. This slice is static-source admitted only. No compilation, dedicated-server, persistence, multiplayer, or in-game room rendering acceptance is claimed here.

## Next exact action

Add a pure-core deterministic physical-route projection that routes MainRoute/Branch passages around non-endpoint district footprints and validates passage clearance. Feed those authored route points to the runtime assembler, then complete the +10000m isolated interior binding and activate the additive surface location only after the route projection and persisted-room restoration path are statically coherent.
