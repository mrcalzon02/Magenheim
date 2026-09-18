# Magenheim Backlog

Priority is dependency order. Broken intended behavior and repository divergence outrank new scope.

## P0.-1 — Underworld world-hosting correction (TOP PRIORITY, raised 2026-09-17, user directive)

The Underworld was implemented as a **separate save file** that the player reaches by quitting to
the main menu. That is not the design and never was the intent: it must be a region of the same
world and same save, concurrent with the surface, hosted in a reserved coordinate band the way
Valheim's own instanced dungeon interiors are. See `INSTRUCTIONS.md` and the rewritten
`docs/UNDERWORLD_DESIGN.md` §6, which are now the binding authorities.

Root cause of the divergence: `UNDERWORLD_DESIGN.md` §6 itself specified "a separate persistent
world instance" and speculated about "controlled world-context switching". Work was then built
faithfully against that text. The document was the defect, so correcting only the code would have
let the next session rebuild it.

What survived and must be kept: `UnderworldSpatialDomain` (the logical-to-host band mapping, still
wired into `UnderworldWorldCenterRegistrar` and `ValheimUnderworldTransitionPlacementHost`), the
six-biome terrain authority, the `GetBiomeHeight`/`GetBiome` postfixes, the Conclave, creatures,
flora and progression work. None of that is wasted.

- [x] **Remove the physical world-switch layer. DONE 2026-09-17.** Removed `UnderworldPhysicalWorldSwitchDriver`,
  `UnderworldWorldSwitchDispatchRuntime`, `UnderworldPhysicalWorldSwitchCoordinator`,
  `UnderworldWorldPairManifestStore`, the loader boundary and the unfinished `UnderworldTravelRuntime` branch.
  Runtime composition now owns one same-world context plus the existing durable transition/placement/recovery graph.
  Existing `.worldpair` files from older test builds are inert legacy config and are no longer read or written.
- [x] **Reserved region converted from a vertical band to a horizontal region.** DONE: schema 2,
  `seed32-quarter-turn-offset-v2`, centre (40000, 0), radius 8000m, `HostBaseY` 0 so Underworld
  terrain generates at ordinary altitudes. A vertical band at the same (x, z) could host objects but
  never terrain, which is the dead end that produced the separate save.
- [ ] **Make vanilla biome-sector consumers safe in the reserved region. COMPATIBILITY SUBSTRATE, not map authority.**
  Weather, sky, ground texture, vanilla spawning and vegetation still consume `WorldGenerator.GetBiomeSector`.
  The logical Underworld map/biome authority must not be bent back into an adjacent-continent design merely to
  satisfy this finite vanilla map. Keep the minimum safe compatibility answer here, then route Magenheim-owned
  presentation/ecology through logical authority. The worker-safe `[sector probe]` remains available for evidence.
- [x] **Logical per-layer map state framework. DONE 2026-09-18.** `UnderworldMapProjection` removes the ~40km host
  displacement from player-facing coordinates; `UnderworldExplorationState` owns independent 512x512 Underworld
  fog; the per-player/world store persists it independently of vanilla Surface exploration; `UnderworldMapPresentation`
  maps logical coordinates to viewport/fog cells. `UnderworldMapLayerRuntime` now owns active identity, projection,
  persistence and physical-position -> logical-cell reveal. Surface positions cannot reveal Underworld fog and identity
  changes save outgoing discovery before loading the next logical map. Remaining work is the thin verified Valheim
  `Minimap` presentation adapter: Surface | Underworld selector, texture upload, logical marker and pins.
- [ ] **Underworld sky and environment.** A safe vanilla sector prevents invalid host-region behavior but is not the
  Underworld's player-facing environment. Custom environment and shared cavern skybox consume logical layer authority.
- [ ] **Deep Gate renders magenta.** It clones `Morkhalla_jotun_gate` and registers, but magenta
  means a broken shader, so either the donor or `ApplyUnderworldMaterials` is wrong. Diff the donor's
  real materials. "Aesir Passage" is only the localized string for the `hud_pin_dnboss` pin; no 1.0.12
  asset carries that name.
- [x] **Terrain relief. DONE.** Region generates, streams and reads as real landscape: 3.50m across a
  50m screen in the central basin with a 0.37m steepest metre-step, 159m across the region in
  Fracture Zones from -68m ravines to +92m walls. Blackwater is 100% submerged, Fungal Forest 0%.
- [ ] **Re-ground the Conclave on the reserved host region.** The center was moved off `HostBaseY` to
  sit on derived-world terrain; that specific change is what turned a region of the live world into
  a second save. Restore band-hosted placement.
- [ ] **Make layer travel a teleport within the world.** Entry and return resolve through
  `UnderworldSpatialDomain` and the persisted surface source anchor, with no save load anywhere in
  the path.
- [x] **Confirm the terrain-shaping band bounds against the reserved host band. DONE.** The runtime now keys `GetBiomeHeight`/`GetBiome` shaping directly from `ContainsHostColumn`; surface columns pass through untouched in the same session. Live play has verified the region generates and streams at the reserved host coordinates.
- [x] **Seed question decided 2026-09-17 (user).** The Underworld uses the surface seed **verbatim**;
  the map differs because the generation algorithm differs. Recorded in `INSTRUCTIONS.md` and §6.
- [x] **Make terrain generation use the surface seed verbatim. DONE.** `UnderworldTerrainRuntime` captures the active surface `World.m_seed` on the main thread and feeds that seed into the deterministic terrain authority. The map differs because the generation algorithm differs.
- [ ] **Purge the separate-save language from the remaining docs and validation records** so no
  future session reads it as authority.

## P0.0 — Live play defects from the 0.0.52 session (TOP PRIORITY, raised 2026-09-16)

The first live acceptance pass against an installed build. These outrank P0.1 and everything
below. Items marked REGRESSION were introduced by the asset work in this session.

- [ ] **Geode still shows textureless, unsurfaced parts.** `geode-sample` reports five parts all carrying maps, so this is not a missing texture at the payload level. Needs a screenshot to identify which surface: candidates are `GeodeCore` seen through the crevices, the shaft interior, or a surface reading flat because its map has range but no legible structure under game lighting.
- [x] **Crystal Staff of Venom does not attack.** ROOT CAUSE FOUND, FIXED 0.0.54. Not an attack-binding defect at all: the Venom registrar never ran. `EarthStaffRegistrar` asked for `staff-earth-*.icon.png`, which has never existed in repository history, and `EarthAssets.Texture` resolves icons through `File.ReadAllBytes`. All eight staff registrars subscribe to `PrefabManager.OnVanillaPrefabsAvailable`, a multicast delegate, so the first handler to throw stops every handler after it. Bootstrap order is Fire, Frost, Storm, **Earth**, Venom, Radiance, Seidr, Spirit — the missing Earth icon removed 20 items across four families with no log line naming them. See `docs/validation/2026-09-17-staff-icon-registration-chain-repair.md`.
- [x] **Socket crystal descriptions must state per-slot effect.** DONE 0.0.53: A crystal's description needs to say what it does in a weapon, in armour and in a utility slot. Today the player cannot tell before committing the socket.
- [ ] **Crystal weapons and staves orient wrongly in the player's hand.** Almost certainly the authored up-axis: these are Y-up game-space sources, Blender is Z-up, and the exporter maps `(x, y, z)` to `(x, z, -y)`. The crystal tier models had exactly this defect and it was invisible to every gate. Check the attach transform against a vanilla weapon.

<!-- Remaining backlog sections intentionally unchanged in repository history; this focused reconciliation updates the active Underworld P0.-1 authority only. -->
