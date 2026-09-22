# Underworld dual-tab vanilla Minimap implementation — 2026-09-21

## User-visible requirement

The large map must expose two maps, **Overworld** and **Underworld**, without creating a second
mapping engine. The Underworld must reveal through the same Valheim exploration cadence/radius,
fog-of-war mutation, pin mechanics, map input, rendering and map serialization used by the
Overworld so ordinary minimap mods continue to patch the same Valheim methods.

The small minimap follows the player's physical layer. Opening the large map may browse either
layer without moving the player or changing world-layer authority.

## Source implemented

`UnderworldMapTabRuntime` binds two independent map payload/texture sets to the single live
`Minimap` component.

- Overworld continues to use Valheim's ordinary profile map slot.
- Underworld uses Valheim's own private `GetMapData` / `SetMapData` codec and stores that resulting
  native payload under the namespaced player custom-data key
  `magenheim.map.underworld.v1.<world uid>`.
- No custom exploration bitmap, reveal-radius loop, fog codec, saved-pin codec or replacement map
  renderer is present.
- The retired `UnderworldMapPresentation`, `UnderworldMapProjection` and `UnderworldMapRaster`
  engines and their tests were deleted from live source.

The large map receives two simple selector controls. Selecting a tab swaps only the data and
textures currently bound to the same Valheim `Minimap`. The small map is rebound to the physical
layer whenever the large map is closed.

## Vanilla hook preservation

Normal player exploration is kept on the physical layer by temporarily binding that layer around
Valheim's own `Minimap.UpdateExplore(float, Player)` / `Explore(Vector3, float)` calls. This means a
mod that changes Valheim's normal exploration cadence or reveal radius still reaches the same
methods for the Underworld.

Valheim shared-map read/write operations and `DiscoverLocation` are also bound to the physical
layer so cartography/discovery gameplay cannot accidentally mutate the other tab merely because
the player was browsing it.

Transient physical-world pins are suppressed while browsing the other layer. Repeated
`SetMapData` swaps clear Valheim's private transient-pin caches (bed/death/location/ping/shout/
player/event caches) so the native update paths can rebuild them instead of retaining references to
pins that `ClearPins` removed.

## Underworld terrain rendering through GenerateWorldMap

The Underworld tab calls the ordinary `Minimap.GenerateWorldMap` path. During that call only, the
`WorldGenerator.GetBiome` / `GetBiomeHeight` sample boundary is supplied from Magenheim's native
Underworld terrain authority. The Minimap still owns pixel color, forest mask, height texture,
zoom, shaders and UI.

The sample context is carried through `AsyncLocal` rather than `ThreadStatic`. That is deliberate:
map-generation mods can move vanilla map sampling into `Task.Run`. Execution-context propagation
carries the immutable Underworld domain/seed/water-level snapshot into those workers without
globally changing unrelated terrain-generation threads or reading Unity singletons from workers.

If `GenerateWorldMap` returns before the map/height textures are published, the adapter treats the
generation as asynchronous and retains the Underworld texture binding until both texture families
have data. This prevents a late external generator from writing Underworld pixels into the
Overworld texture set. The lease times out after 120 seconds and discards the incomplete layer so a
later bind can retry.

## Build/runtime status

Source is committed on `main`. This session does **not** claim a local Valheim runtime compile,
package/install, or live map test; the current environment does not contain the user's installed
Valheim assemblies/profile needed by the repository build gate.

The repository's existing build gates are expected to validate the exact Harmony targets and the
literal private `Minimap` reflection fields against installed Valheim assemblies on the next local
closeout.

## Live acceptance required

1. Surface: reveal new terrain and add a saved pin.
2. Open large map: confirm Overworld is selected and the Surface map/pin are present.
3. Select Underworld while still on Surface: confirm a distinct Underworld terrain map/fog appears
   and no Surface transient pins leak onto it.
4. Enter the Underworld: confirm the small minimap automatically follows Underworld and ordinary
   movement reveals Underworld fog at the normal Valheim cadence/radius.
5. While below, browse Overworld and back without changing the player's physical layer.
6. Add saved pins independently on both tabs and verify neither crosses layers.
7. Save/logout/reload: verify both exploration states and saved pins survive independently.
8. Use cartography/shared-map behavior on each physical layer and verify data lands on that layer.
9. Exercise a map-size/map-generation mod, especially an asynchronous generator, and verify its
   ordinary Minimap hooks affect both tabs without Surface texture corruption.
10. Complete Surface -> Deep Gate -> Underworld -> Surface and confirm map switching agrees with the
    separate internal world layer and never with Surface coordinate projection.
