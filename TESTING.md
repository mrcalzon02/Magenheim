# Magenheim test acceptance

## Required delivery check before launching

Run `./closeout.ps1` (`-Offline` when using cached dependencies) with Valheim and
r2modman closed. Reopen r2modman and select Central Fuckery. The enabled entry must
show **Magenheim v0.0.153 by Local** and start its description with
**0.0.153: Retires arbitrary terrain spires in favor of rare biome-owned massif cells**.

The closeout verifies the catalog version/description and the installed DLL hashes.
If either differs, installation is incomplete. After Launch Modded, confirm the
BepInEx startup log says `Loading [Magenheim 0.0.153]` before reporting game results.
Future versions must update `release.json` and this expected version together.

Install the candidate ZIP into a disposable r2modman profile, then launch **Modded**. The plugin
is under `BepInEx/plugins/Local-Magenheim/Magenheim`. Launching vanilla Steam
does not use this profile. Exit the game before rebuilding or installing.

## Ground feature acceptance - 0.0.108

In the fungal forest, inspect the three low rootgrass forms between trees and across open
patches: carpet, taller tufts and low woven runners. In Sulfurous Wastes, inspect yellow
powder deposits with crescent edges, wind ripples and raised lee banks. Other biomes should
show silt ripples (wet Blackwater), frost needle fans, shale scree and fibrous peat mats.
Walk through all ground features; they are low decorative cover and must not block movement.
Check sloping ground, hill crests, shorelines and cliff edges for floating or buried patches.
Walk away and return: existing trees/rocks must stay fixed. Check frame time in a dense grove,
reload the world, compare two clients, and verify that the gate's 36m clearance stays open.
These checks require live acceptance; source rendering does not establish in-game appearance.

## Biome terrain acceptance - 0.0.108

Use a disposable world: the 0.0.107 terrain expansion changed the deterministic terrain fingerprint.
Check the dry, gentle first 80 metres around the gate, then increasing hills to 320 metres.
Explore all six biomes: fungal hills, flooded Blackwater basins, sulfur ridges, frozen ranges,
faulted Fracture Zones and sunken decay. Cross both central and outer biome borders and confirm
continuous ground, terrain collision, water placement and scenery grounding on steep slopes.
Check rare massif-cell crowns and walls, gate return, reload and two peers viewing the same terrain.
These live appearance, traversal, persistence and multiplayer checks remain pending.

## Rare-cell massif terrain acceptance — 0.0.153

### Fungal canopy variety acceptance — 0.0.108

Walk several Fungal Forest groves, allowing the cell population to finish. Confirm bent and
twin glowcaps, different spire tiers, spreading/forked puff crowns, and young/splayed/woven
tangles appear amongst the original forms. Look for differences in branching and crown shape,
not just size. Inspect stems and roots from below and walk between them: stems should block
movement, caps and ground cover should not. Check all forms under the sunless sky and mist.
Cross several cell boundaries and return; positions, forms and collision must stay stable.
Check frame time while new cells load, then entry/return and a peer with the identical package.
Offline authoring and import tests do not establish these in-game results.

### Terrain regression checks

Use a disposable world. Enter the paired Underworld and allow roughly 30 seconds for the
budgeted distant terrain presentation to fill. Find at least one rare massif cell with
`magenheim_underworld survey 1200`, then inspect it from the surrounding Blackwater seam, at the
cell wall and from the crown. Most of the cell interior should rise dramatically while retaining
its own biome ridges/basins/faults; it must not converge to an isolated needle spire. Ordinary
cells must remain common and the protected arrival basin must stay walkable.

Move across several 64m chunk boundaries and along the widened Voronoi/Blackwater walls. Look for
cracks, overlapping surfaces, vertical-smear artifacts and abrupt one-sample height discontinuities.
The nominal river band is now wider (440m before plasma variation), so the channels should read as
substantial subterranean cuts rather than hairline cracks.

Observe clear-air silhouettes, dense biome mist and weather separately. Return to the Surface and
confirm camera distance and fog restore. At massif height, verify standing collision, map/layer
identity and return travel. Recheck Motherbed, Drowned Ring, Furnace Heart and Sigil placement
against the changed terrain, then multiplayer agreement and save/reload. None of these live checks
are established by offline sampling or Core tests.

## Sunless sky and haze acceptance - 0.0.108

Look up from every biome: the same basalt roof, lava fissures and fungal star colonies must
remain, without a Surface sun, moon or cloud backdrop. Compare native dawn, noon, dusk and
midnight: light should cycle smoothly and stay dimmer than the Surface; glowing colonies must
remain readable at night. Lava and fungal cores should glow while unmasked rock stays dark;
turn the camera to verify their appearance is emissive rather than moving specular highlights.
Check zenith stretching and the panorama wrap seam in motion.

Fly to the overhead haze around native Y=4800m; the highest terrain should end at or below
5100m, only just above that band. These are visual limits, not flight/build restrictions.
Place a supported ordinary building piece at floor and plateau height: the dungeon-location
rejection should not apply in the active Underworld. Verify ordinary Surface dungeons still
retain their normal restriction. Return to the Surface and check sky, clouds, camera distance,
weather and daylight restore; repeat entry/return and save/reload. Record actual frame time and
shader/haze failures from the log. Offline projection renders do not establish these results.

## What is present

- Crystal Shaping, permanent identifier `magenheim.crystal_shaping`, with its own icon.
- An original Earth geode model, texture, and inventory icon; a larger mineable world nodule.
- Rough, Simple, Crystal, Advanced, and Master Earth crystals and Earth Crystal Shards,
  each with its own model, texture atlas, and icon.
- Definition-driven Meadows geode world generation and intact-geode destruction drops.
- Geologist's Workstation plus Fracturing Block, Faceting Wheel, and Resonance Frame,
  all with original models, icons, build costs, collision shapes, and wear variants.
- Local-host workstation operation source for geode opening and Earth refinement, including
  authority/replay/capacity admission, atomic inventory mutation, failure shard returns,
  and Crystal Shaping XP awards. This source still requires a rebuilt live-world retest.

Remote-client workstation RPC and socket-management UI/transport are source-implemented.
Full persistence and multiplayer acceptance remain open. Use a disposable world and character.


## Crystal Shaping visibility regression

Open Skills immediately after loading an existing or new character, before using
the workstation. Crystal Shaping must appear at level 0 if untrained, with its
custom icon. Reopen the panel and reload the character: no duplicate entry or free
XP should appear. Previously earned Crystal Shaping level/XP must stay unchanged.

## Developer console — Underworld access and spawn IDs

Launch with `-console`, press F5, and run `devcommands` first; every command below is gated by it.

| Command | Effect |
|---|---|
| `magenheim_underworld audit` | Re-runs the fail-closed native instance admission audit and prints the Surface/Underworld World IDs, ZDO namespaces, Unity scene, and child-save namespace. Run this first. |
| `magenheim_underworld survey [radius]` | While physically in the Underworld, samples the real deterministic terrain around the player, reports biome height ranges and monumental hits, then lists actual loaded Magenheim location families and nearby living inhabitants. Radius defaults to 512m and clamps to 128–1600m. |
| `magenheim_underworld status` | Reports whether the paired Underworld instance is active, whether the Deep Gate is unlocked, and whether you hold a return anchor. |
| `magenheim_underworld enter` | Enters the Underworld through the same transit the Deep Gate uses, skipping the Nowhere King unlock. Records a return anchor where you stood. |
| `magenheim_underworld return` | Returns to that anchor. With no anchor (for example after relogging below ground) it returns you to your bed, or your home point if you have none. |

`audit` must report **AUDIT PASS** before transit acceptance is meaningful. If it reports **AUDIT FAIL**, preserve the exact line and log; do not continue treating the instance as admitted.

`enter` refuses with the reason if the Underworld instance is not active yet; load fully into the
world and try again. The gate path itself still requires the unlock.

Spawn IDs (`spawn <id> 1`):

- Elemental Surtlings: `Magenheim_Underworld_Surtling_<Fire|Water|Earth|Wind|Radiance|Umbral>_<Feminine|Masculine>`
- Underworld creatures (native biome spawns plus direct review): `Magenheim_Underworld_Prototype_<NameWithoutSpaces>` (see docs/UNDERWORLD_CONTENT_PROTOTYPES.md)
- Underworld resource items and pickups: see docs/UNDERWORLD_RESOURCE_MAPPING.md

## Underworld creature spawn and skin acceptance

Enter each Underworld biome normally and spend at least one native spawn interval moving through
unoccupied terrain. Do not use console-spawned creatures as proof of natural ecology.

1. Confirm all seven roster creatures for the biome can appear through the native SpawnSystem over
   repeated traversal; heavy/hero forms are intentionally much rarer than small fauna.
2. Confirm no `Magenheim_Underworld_Prototype_*` creature naturally appears on the Surface.
3. In Blackwater Deep, confirm Serpent/Leech-derived creatures remain submerged or near water and
   Shoreclaw/Abyss Shellback remain in the shoreline altitude band.
4. Compare the shared Wolf chassis directly: Mycelial Stalker, Cinder Hound, Iceblind, Glacier
   Stalker and Decay Hound must read as different creature skins at gameplay distance, not simple
   grayscale/hue variants.
5. Compare StoneGolem derivatives: Furnace Golem, Cryolith Guardian, Stonebound and Rift Colossus
   must have clearly different mineral structure, contrast and accent placement.
6. Compare Bat/membrane and Seeker/carapace derivatives across biomes. Their large-scale markings
   must remain readable in motion and under each biome's actual lighting/fog.
7. Verify donor animation, attack sockets, hitboxes, ragdolls where applicable, faction and loot
   remain intact, and verify the corresponding vanilla donor elsewhere still uses its untouched
   original material.
8. Verify each biome's presentation layer in motion: Fungal spore drift, Blackwater bioluminescent
   motes, Sulfurous smoke/ember glow, Frozen rime mist, Fracture sparks and Great Decay motes.
   Selected luminous creatures should cast a restrained local glow without washing out the whole
   cavern or producing obvious point-light popping.
9. Compare temperament against the donor. High-pressure hunters (including Deep Hunter, Cinder
   Hound, Glacier Stalker, Chasm Stalker, Decay Hound and Graft Warden) should acquire and pressure
   a player earlier and spend less time circling/idling. Ambient forms such as Lantern Moth, Cave
   Ray and Rime Moth must remain much closer to donor pressure and must not become map-wide pursuers.
10. Confirm the eight explicit apex `HuntPlayer` species can pursue naturally spawned players, while
    ordinary fauna do not inherit that spawn flag.
11. Exercise elemental tolerances with controlled hits. Sulfurous creatures must strongly resist
    fire and be vulnerable to frost; Frozen creatures must strongly resist frost and be vulnerable
    to fire; Blackwater creatures must tolerate water but be vulnerable to lightning; Fungal/Decay
    creatures must resist poison; Fracture creatures must resist lightning. Confirm named immunity
    overrides on Furnace Golem (fire), Cryolith Guardian (frost), Fume Wraith/Corpse Orchard/Spore
    Husk (poison) and the stronger Fracture Wisp lightning resistance.
12. Land the same donor attack from a vanilla creature and its Underworld derivative against an
    identical target. The Underworld hit must preserve the donor animation while its total damage
    obeys the creature's role/biome multiplier and per-hit cap, then adds the biome rider: poison
    (Fungal/Decay), frost (Blackwater/Frozen), fire (Sulfurous), or lightning (Fracture). The
    vanilla donor must remain completely unchanged.
13. Verify the role tradeoff directly. Swarm creatures should be the most mobile but have the
    lowest health and per-hit damage; Heavy/Apex creatures should have clearly larger health pools
    and stronger hits but materially slower movement and longer pauses between attacks.
14. Compare the same role across progression: a Fungal creature and Great Decay creature of the
    same role must preserve the same movement relationship while the later-biome creature has
    higher health and a higher damage envelope.
15. Specifically test Glacier Stalker as a mobile Hunter rather than an Apex tank, and Graft Warden
    as a slow Heavy. The former should pressure movement without huge individual hits; the latter
    should be easy to read and punish badly if its slower attack connects.
16. Repeat VFX, aggression, balance and elemental-hit checks with a second peer. Presentation may
    be local, but combat results and target behavior must agree with the authoritative simulation.

## Underworld creature combat-balance acceptance

This pass replaces inherited donor health/damage assumptions for the 42 ordinary Underworld fauna
and twelve elemental Surtlings with an explicit biome × role policy.

1. Check at least one Swarm, Skirmisher, Hunter, Bruiser, Heavy and Apex creature. Record displayed
   health, observed locomotion and repeated attack damage against the same armor/resistance setup.
2. Confirm role ordering: Swarm/Skirmisher are faster and cheaper per hit; Heavy/Apex are slower,
   tougher and hit harder. No Heavy/Apex should gain speed as a consequence of higher aggression.
3. Confirm minimum attack recovery is visibly longer for Heavy/Apex creatures. This is the monster
   pacing equivalent used instead of inventing player-style stamina for AI creatures.
4. Confirm the per-hit cap actually constrains extreme donor attacks; a reused Troll/StoneGolem
   chassis must not bypass the Magenheim role envelope merely because its vanilla donor attack is
   unusually large.
5. Confirm all six biome bands scale upward in health/damage envelope while movement remains role
   controlled rather than biome controlled.
6. Verify AoE/shared-hit attacks on two targets. Damage to target two must not be multiplied again
   because target one was hit first; Magenheim clones the incoming HitData before modifying it.
7. Verify the twelve elemental Surtlings use the same role policy: Wind is Skirmisher, Fire/Water/
   Umbral are Hunters, Radiance is Bruiser and Earth is Heavy.
8. Verify Deep Fracture creatures retain their existing explicit health/speed ladder and Nowhere
   King retains its separate boss profile; this pass must not silently overwrite those systems.

## Advanced / Master staff damage acceptance

The 2026-09-29 balance pass raises both Advanced and Master staff damage by 50% across all eight
families. Test with the same character skill, target, range and resistance state.

1. Confirm Simple and Crystal tier damage is unchanged.
2. Confirm each Advanced and Master primary hit is approximately 1.5x its pre-rebalance value.
3. Confirm each tier-specific damaging field/impact is also approximately 1.5x its previous value.
4. Confirm stamina cost, projectile count, burst cadence, accuracy, force/stagger and durability are
   unchanged.
5. For Fire, Frost, Storm, Venom, Seidr and Spirit multi-projectile/multi-pulse casts, record both a
   single-projectile hit and a full-cast all-projectiles-hit case so the resulting total cast damage
   can be judged independently of the requested 50% per-damage-component increase.
6. Confirm Frost control, Venom corrosion strength, Seidr binding and Spirit suppression retain
   their prior non-damage values.

## Candidate acceptance matrix

### 0.0.135 dependency-ordered Underworld acceptance

Use a disposable campaign with identical packages on host and client. Stop at the first failure
and preserve the full BepInEx log; do not treat a later symptom as the first broken dependency.

1. **Bootstrap/bindings:** confirm 0.0.135 loads with no patch, missing-member or registration errors.
2. **Native host admission:** run `magenheim_underworld audit`; require AUDIT PASS, distinct native
   World/WorldGenerator/ZoneSystem/ZDOMan and scene/physics identities, and a child save namespace.
3. **Gate and worldgen:** enter and return; verify terrain, collision, vegetation and locations use
   the Underworld context, and character inventory/progression survive travel.
4. **Two-instance persistence:** put identifiable objects and connected structures/portals in both
   instances. Save twice (the second save must exercise incremental dirty chunks), quit, reload,
   audit again, and verify objects, connections and generated-zone state in both instances. Check
   logs for both chunk writes and no early shared-snapshot cleanup or failed child write.
5. **Concurrent multiplayer:** keep one player in each instance at overlapping logical coordinates.
   Move, build, fight and cross the gate independently. Neither player's objects, collisions or
   native peer visibility may leak into the other instance. Repeat save/reload and reconnect.
6. **Maps:** explore and pin both layers; browsing the other map tab must not move the player or
   alter the active world context. Verify both map payloads after restart.

These are live acceptance steps, not claims made by passing the offline build.

Confirm the startup log reports **0.0.136** and has no Magenheim bootstrap or registration
errors. Keep logs and screenshots with each result. Mark checks PASS / FAIL / NOT RUN;
record game version, mod list, host/client role, seed, steps, expected/actual result.

| Area | Required acceptance |
| --- | --- |
| Geology | Mine each biome geode; exactly one intact drop; opening output follows definitions; no mutation of vanilla rocks; reload without duplicate registrations. |
| Deep Fracture caverns | 0.0.52. Enter a Deep Fracture district. It must read as an enclosed cavern: a vault overhead, terraced walkable floor, dripstone. Walk the full floor and confirm no step blocks movement; check passage mouths connect and the tunnel between districts matches them. Where a district has a surface fissure, confirm the shaft lights the floor below. |
| Geode and crystals | 0.0.52. Open a geode item and the world nodule: the exterior must read as fractured plates and the exposed face as a banded, druzy-lined cavity, tinted to its biome with no see-through faces. Check all five crystal tiers and shards read as a progression in hand and on the ground. |
| Surface fidelity | 0.0.52. Every model now carries a generated albedo. Look for surfaces that read as flat colour, unexpectedly dark, or tiled at the wrong scale. There are no normal maps; judge albedo only. |
| Inventory notification | 0.0.49 repair. Force a workshop transaction to fail (full inventory) and confirm the rollback completes with no `TargetParameterCountException` and no lost or duplicated items. Install and extract a socket, host and remote client, and confirm the inventory view refreshes immediately after each mutation. |
| Workshop | Place all upgrades; verify station levels, all eight refinement families, success/failure shard returns, XP, full-inventory rejection and repaired iron straps. |
| Sockets | Host and remote client open/install/extract; Dais permission boundary; stale item, duplicate response, reconnect, mismatch and spoofed descriptor rejection without loss/duplication. |
| Persistence | Save/reload, drop/pickup, chest, repair, upgrade, transfer, death and dedicated server for socketed items and new pieces. |
| Staffs/weapons | All eight four-tier staff families: recipe, icon, held appearance, projectile, hit/status effect and damage; all ten physical crystal weapons. |
| Construction | Furniture/decor, hearth, beams/foundations, 24 banners, eight beds, Dais, Ice Box: placement, collisions, wear, removal/refunds, comfort, interaction and persisted state. |
| Sentinel/alchemy | Sentinel targeting and all eight munition types; grinding returns; Dust recipes; Eitrwine fermentation and effects. |
| Compatibility | Representative third-party equipment and exclusions; unknown-item rejection; identical host/client authority; unchanged foreign prefab behavior. |
| Underworld maps | Large map exposes Overworld/Underworld tabs on one vanilla Minimap; small map follows the physical layer; movement reveals only the physical layer; saved pins/fog persist independently; shared-map/discovery writes stay on the physical layer; async/map-size mods do not corrupt Surface textures. |
| Disabled scope | No surface Deep Fracture locations or model-only artifact recipes should appear. |

Do not promote this candidate to production until the multiplayer and persistence
checks pass. Detailed unfinished scope is in [CLOSEOUT.md](CLOSEOUT.md).

## Underworld dual-map acceptance — 0.0.92

Use one disposable character and world. Reveal a recognizable patch of Overworld terrain and add a
saved pin. Open the large map: **Overworld** must be selected. Select **Underworld** without using
the Deep Gate. A different terrain map and independent fog state must appear; Surface transient
pins must not leak onto it. Add an Underworld saved pin, switch back and forth, and verify each pin
belongs only to its own tab.

Enter the Underworld through the Deep Gate. The small minimap must switch to the Underworld
automatically. Walk far enough to cross the normal Valheim reveal interval/radius and verify fog
opens along the route using ordinary movement. While physically below, browse the Overworld tab and
back; browsing must not move the player or change the active world layer. Repeat the inverse after
returning to Surface.

Save, quit, and reload. Both fog states and both saved-pin sets must survive independently. Exercise
cartography/shared-map data and a discovered location on each physical layer; those writes must go
to the physical layer even if the other tab was being browsed immediately beforehand.

If a map-size or asynchronous map-generation mod is installed, repeat the first Underworld-tab
generation. The mod must still run through its ordinary Valheim Minimap hooks, Underworld
generation must not write into the Overworld texture set, and an autosave during generation must
not replace the Surface profile map with the Underworld payload.

Record the result separately from the still-open Underworld world-instance isolation acceptance:
map correctness does not prove ZDO/terrain layer isolation, and layer isolation does not prove map
persistence.

## Build the workshop

Use **Hammer > Crafting**. The workstation requires a nearby vanilla Workbench.
Its three upgrades require the Geologist's Workstation. The menu entries become
known as you discover their materials. Place upgrades within five meters of the
workstation; three different upgrades can raise its displayed station level to four.
Duplicate copies of one upgrade do not stack.

| Piece | Build materials |
| --- | --- |
| Geologist's Workstation | 10 Wood, 10 Stone, 2 Flint |
| Fracturing Block | 10 Wood, 8 Stone, 4 Flint |
| Faceting Wheel | 10 Fine Wood, 10 Stone, 4 Bronze |
| Resonance Frame | 10 Fine Wood, 4 Iron, 5 vanilla Crystal |

All listed resources are recoverable on removal. Check placement preview, collision,
repair/removal, upgrade connection effects, station level, and save/reload in a
disposable world. The main bench can be used outdoors without a roof or fire.

Direct prefab inspection commands, after `devcommands` in the local test world:

```text
spawn Magenheim_GeologistWorkstation 1
spawn Magenheim_StationUpgrade_FracturingBlock 1
spawn Magenheim_StationUpgrade_FacetingWheel 1
spawn Magenheim_StationUpgrade_ResonanceFrame 1
```

Use the Hammer for placement/removal tests: console spawning alone does not verify
build-resource consumption or return behavior.

## Disposable character/world test

Enable the game's console with `-console` in the profile launch arguments, enter a
new disposable world locally, press F5, then enter `devcommands`.

```text
spawn Magenheim_Geode_Meadows_Earth 1
spawn Magenheim_Crystal_Earth_Rough 1
spawn Magenheim_Crystal_Earth_Simple 1
spawn Magenheim_Crystal_Earth_Crystal 1
spawn Magenheim_Crystal_Earth_Advanced 1
spawn Magenheim_Crystal_Earth_Master 1
spawn Magenheim_Shard_Earth 5
spawn Magenheim_Geode_Meadows_Earth_World 1
```

Inspect ground models and inventory icons. Pick up and drop each item; verify stack
names, sizes, and visuals. Mine the world nodule and verify exactly one intact geode.
Save, quit to menu, and reload to check item/nodule persistence and no duplicate registrations.
Natural geodes appear in newly generated Meadows zones; existing explored terrain
is not retroactively regenerated. A console-spawned nodule does not validate natural placement.

To inspect skill display and saving on the disposable character, use the permanent
single-token skill identifier. Valheim's `raiseskill` parser treats the display name's
space as another argument before Jotunn can resolve it, so `raiseskill Crystal Shaping 1`
is not a valid diagnostic command.

```text
raiseskill magenheim.crystal_shaping 1
```

After rebuilding/installing current source, use the Geologist's Workstation Craft panel
on a local-host world to test the Magenheim operation recipes. Opening a Meadows Earth
geode should consume exactly one intact geode, grant one to three Rough Earth crystals,
and award Crystal Shaping XP. Earth refinement recipes should consume exactly one source
crystal; success grants the next tier, while a destructive failure returns the configured
matching shards. These operation paths are source-implemented but remain unaccepted until
observed in the live disposable world.

## Live world test 1 - 2026-09-14

Observed in the disposable world before the later workstation-operation implementation:

- the Geologist's Workstation exists and opens its Craft panel;
- the Craft panel contained no Magenheim mineral operations in that build;
- no equipment-slotting operation was exposed;
- all directly spawned Earth inventory items appeared correctly;
- the spawned world geode used the custom mesh but rendered translucent;
- visible workstation surface fighting was observed around the iron tabletop bands;
- `raiseskill Crystal Shaping 1` was rejected by the command parser.

The translucent geode was traced to cloned source-material render state rather than
texture alpha: Magenheim's checked-in atlases are opaque RGB images. Source now normalizes
Magenheim custom materials to opaque blend/depth state; that repair requires a rebuild/install
and live retest before it is accepted. The workstation banding issue and socketing operation
remain open. Geode-opening/refinement operations have since been implemented for local-host
execution and now also require the rebuilt live retest.

## Build and install

With Valheim closed, run one command from the repository root:

```powershell
./install-local.ps1
```

The installer now runs `build.ps1` itself before touching the active profile. The build runs
the core test harness, compiles the runtime against the selected BepInEx/Valheim installation,
derives the package version directly from `MagenheimPlugin.PluginVersion`, creates a clean
versioned package, and only then installs it. Use `-SkipBuild` only when deliberately reinstalling
a package that was already built from the same current source.

Both scripts accept `-ProfileRoot`; the installer also forwards `-GameRoot` to the build.
Building requires .NET SDK 8, the installed game, BepInEx, and NuGet restore. The local SDK is
in the ignored `dist/toolchain/dotnet` folder; system dotnet is the fallback.

The installer archives the previous `Local-Magenheim` folder under `backups`, refuses duplicate
Magenheim DLL installations, validates package/source version agreement, verifies every copied
file by SHA-256, prints the installed `Magenheim.dll` hash, and prints the exact plugin version
expected in the BepInEx startup diagnostic. This closes the old failure mode where Git source
was current while the active profile silently continued loading an obsolete DLL.

## Evidence boundaries

See `docs/validation/2026-09-14-earth-content-package.md`,
`docs/validation/2026-09-14-workshop-content.md`, and
`docs/validation/2026-09-14-live-world-test-1.md` for observed results and current
acceptance boundaries. Natural worldgen, persistence, and multiplayer still require
separate observations.

## 0.0.108 live regression checks

- Enter through the console or Deep Gate. Arrival must be outside the actual Aesir gate's front
  footprint, facing toward the conclave, never inside the central monolith. Repeat enter while
  already below must refuse without replacing the Surface return anchor. Test return after relog.
- Roof must show lava/fungal artwork rather than the blue Surface sky. Logs must contain no
  panoramic-shader lookup failure. Test noon/night, camera rotation, and Surface restoration.
- Walk 95m in either direction across several ecology cell boundaries, including while standing
  on large collidable rocks. Nearby trees/rocks must keep exactly the same position and collider.
  Walk away and return: the seeded placement must match. Watch frame time in dense groves.
- The puffcap's roots, trunk and branches must form one curved skin with no flat cylinder collar.
- Small-map biome label must use Underworld names. Large-map hover uses the selected layer and
  hides unexplored names. Surface names must return when leaving or browsing the Surface tab.
- Check denser fungal groves, stronger background haze, rougher local relief, and remaining
  massive cliffs in clearer biomes. Terrain relief changed: use a disposable test world first.
# 0.0.137 loading artwork and content registration acceptance

- Fresh boot: confirm 22 resource items and 22 native vegetation pickups, with no hide-target
  rejection and no missing YggaShoot1 donor warning.
- Underworld generation: confirm individual panoramic loading artwork and no stretched image
  or collage dividers. While the presenter is visible, panels should rotate every eight seconds.
- Collect a one-shot stone resource, save and reload: it must stay depleted through native persistence.
- Game startup, rendered loading screens and collection persistence still require live validation.


## Loading-screen / worldgen fidelity acceptance

Loading artwork is now a player-facing worldgen contract. For each replacement backdrop, record the
biome/scene, a seed and an in-game location that reproduces its major visual ingredients. Exact
camera composition is not required, but the scene must be recognizably attainable without noclip,
console-spawned scenery or developer-only terrain mutation.

For the Blackwater Worldroot Span, explore newly generated Blackwater Deep zones until the native
location system places the landmark. Confirm the span is a genuinely large navigational silhouette,
its feet meet the terrain/water rather than floating, the opening remains traversable, collision
matches the visible root mass, distant haze does not erase it at ordinary navigation distance, and
the same location survives unload/reload and agrees for a second peer. Check frame time while the
landmark streams in. The checked-in 7,932-triangle authored root model is deliberately reused at
monumental scale rather than replacing it with a high-density special-case mesh.

A backdrop fails fidelity acceptance when a major shape in the picture has no runtime analogue. In
that case either implement that analogue through the existing terrain/location pipeline or redraw
the backdrop within the current generator envelope. Do not waive the mismatch as "concept art".


## 0.0.146 loading-screen scope acceptance

The Magenheim loading presenter must appear only for Magenheim-owned Underworld work: initial
construction/admission of the native Underworld instance and local Deep Gate or developer transit
between Surface and Underworld. Ordinary Valheim menu/world loading that does not execute those
paths retains Valheim's own presentation.

Enter and return through the Deep Gate. Confirm artwork remains visible across rendered frames
instead of flashing inside one synchronous call stack, then disappears after transfer. A host
moving a remote player must not show that remote player's transition overlay on the host.
Dedicated servers must never create the UI.

For each final loading image, verify both promises: the depicted geography/landmarks are attainable,
and at least one depicted inhabitant has the recognizable body plan of its runtime counterpart.
A tinted/scaled donor prototype does not validate substantially different finished monster art.


## 0.0.147 Sulfurous Wastes population acceptance

Use a newly generated Sulfurous Wastes area. Confirm native vegetation now mixes scorched tree
silhouettes, fallen/charred branches, burnt stumps and cinder bushes with the sulfur drifts, rock
chains, vents and three-lavafalls landmark. These are stripped visual vegetation: chopping or
interacting with them must not expose Ashlands TreeBase/Destructible behavior or donor drops.

Observe natural Fire Surtling population over several spawn intervals. Both authored body variants
may appear in Sulfurous Wastes, using their existing donor combat/AI/loot, but the combined
population should remain sparse enough that the biome still reads as an ecosystem rather than a
continuous combat event. Confirm no Fire Surtling world spawns occur on the Surface or in another
Underworld biome, then repeat on a second peer and after save/reload.

The 42 ordinary donor-chassis creatures now participate in native Underworld biome spawning and
use owned biome/creature skin materials. They are accepted as the current reusable-chassis fauna
layer, not as finished bespoke silhouettes. Full re-body work remains necessary only where the
concept materially contradicts its donor anatomy; Lantern Moth is still the first explicit example
because a four-wing moth cannot be sold convincingly by a Bat silhouette alone.


## 0.0.148 canonical Surtling population acceptance

In newly generated Underworld areas, verify the authored Surtling bodies spawn only in homes already
declared by the roster: Fire in Sulfurous Wastes, Water on dry/shoreline ground in Blackwater Deep,
and Earth plus Wind in Fracture Zones. Radiance and Umbral must remain absent from general biome
spawning because their roster homes are site conditions rather than canonical terrain biomes.

Check several spawn intervals in each biome, then cross biome boundaries. Counts should remain
sparse: Fire allows at most two of each body variant from its row; Water/Earth/Wind allow one of
each body variant. Verify donor combat/AI/loot still function, no Surface spawn-list pollution
occurs, and host/client observe the same creatures.


## 0.0.149 live environment survey acceptance

After entering the Underworld, run `magenheim_underworld survey` in each biome, then repeat with
`survey 1200` from at least one high overlook. Preserve the console output with a screenshot of
the same view. The terrain rows must agree with the visible biome and relief,
rare-cell-massif-samples must correspond to actual lifted biome-cell interiors when present, and
every reported structure
must be an actually loaded Magenheim native location in the current Underworld scene.

The inhabitant section is observational only: it reports living characters already present within
the sample radius and does not create, despawn or count players as biome fauna. Use these paired
survey outputs/screenshots as evidence when deciding whether loading artwork is attainable.


## 0.0.150 organic biome-layout acceptance

Use a fresh disposable Underworld seed. The Fungal arrival country must remain safe around the
Deep Gate, but its outer edge must not form a perfect circle. Travel or use
`magenheim_underworld survey` on several long transects and one broad loop approximately 5–7 km
from centre. Biomes must appear as broad irregular regions with fingers/enclaves and may reappear
after another biome; there must be no five-spoke angular order.

Create a second fresh seed and repeat comparable transects. The second biome map must be materially
rearranged rather than the first map rotated around the centre. At organic biome boundaries,
terrain height should remain continuous while minimap/weather/ecology switches to the owning biome.

This change intentionally invalidates the old biome-layout authority fingerprint. Host and peer
must use the identical 0.0.150 package; a mismatched old generator must fail admission rather than
produce divergent world truth.


## 0.0.151 hex/Voronoi river-grid acceptance

Use a newly generated disposable Underworld. The outer biome map should show broad cellular regions
whose spacing still faintly reflects a hex-derived structure, but the cells must be visibly
deformed by 50% site jitter and plasma edge warp rather than reading as a clean board-game grid.

The cell boundaries are physical hydrology. Follow several long boundaries and confirm the strong
seams become continuous Blackwater/ocean-depth corridors with actual submerged terrain, Blackwater
terrain material/biome identity, Blackwater atmosphere/ecology, and irregular plasma-shaped banks.
The deepest seam should reach roughly 30m below the shared water level, while weaker edge shoulders
form shallows and banks.

The river grid must be distributed through every world quadrant, but must fade out inside the
protected Fungal arrival core. The Deep Gate approach must remain dry and traversable rather than
being surrounded by a circular moat. Monumental rocks/landforms may bridge or locally obstruct a
channel; they must not erase the overall network.

Repeat on a second seed. Both the cellular ownership pattern and the river lattice deformation must
change materially with the derived seed. Run `magenheim_underworld survey 1200` from several
banks/overlooks and preserve screenshots so the loading-screen/worldgen contract can use the river
network as a real attainable feature.


## 0.0.152 edge-ocean barrier acceptance

Use a fresh disposable Underworld. Travel toward the outer world limit along several bearings.
The final landmass must not terminate at the hard 8 km domain edge. Instead, terrain should descend
through an irregular shoreline into Blackwater and become a broad ocean ring before the actual
boundary.

The shoreline should begin roughly around 84% of world radius, but must wander rather than forming
a mathematically perfect circle. By about 96.5% radius the terrain should be fully submerged, and
near 98.5% radius every sampled bearing should report Blackwater Deep with more than 100 m of water
above the seabed. The deepest intended outer bed is roughly 140 m below the shared water level.

Check that monumental spires/plateaus do not survive as walkable islands inside the final deep-ocean
band: edge-ocean carving is intentionally applied after monumental terrain. Verify the map,
Blackwater weather/ecology, water rendering, swimming/boat traversal, terrain streaming, save/reload
and a second peer agree at the same coordinates. The actual hard domain edge should only be
encountered beyond the deep-water barrier.


## 0.0.153 progression-height and massif acceptance

Use a fresh disposable Underworld because biome ownership and height authority changed again.
Across a broad route from centre toward the outer ocean, the *nominal* terrain should trend upward
with progression: Fungal country lowest, then Blackwater land cells, Sulfur, Frozen, Fracture and
Great Decay progressively higher. Local ridges, basins and rivers may cross those baselines; the
rule is a statistical/landscape tendency, not six concentric terraces.

Biome occurrence should also trend with distance without becoming radial rings. Earlier biomes
should be common inward, later biomes increasingly common outward, but long routes should still
find enclaves and returns produced by the fractal/cellular field. Compare at least two fresh seeds.

Find multiple rare massif cells. They should be sparse, absent from the protected arrival shoulder
and edge-ocean band, and occupy broad portions of their owning Voronoi cell. They should rise
roughly 1.5–3.3 km above the same biome's ordinary relief while retaining the biome's surface
character. No independent needle-spire/plateau generator should appear.

Follow widened Blackwater seams around ordinary and massif cells. The nominal cellular edge width
is 440m with ±130m plasma width variation; the resulting channels/walls should be visibly thicker
than 0.0.151 while remaining irregular. Confirm rivers still cut down after massif uplift and that
the final edge-ocean carve still defeats high terrain near the 8 km boundary.

Run `magenheim_underworld survey 1200` from ordinary cells, river banks and a massif crown and
capture the output/screenshots for comparison against the loading-screen worldgen contract.
