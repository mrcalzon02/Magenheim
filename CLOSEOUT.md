# Development closeout - 0.0.142 Underworld resources, geodes, map display and deployment proof

The repeated 0.0.140-style live exceptions supplied after a local install prove that the running
process was not executing the repaired 0.0.141 image: the current source no longer contains the
fresh-world parent-path exception path, and foreign negative biome masks are filtered before
ownership validation. 0.0.142 therefore adds a startup provenance line containing the exact loaded
assembly path and checkpoint `UW-142` so a wrong-profile/stale-DLL launch is immediately visible.

All eight compatibility-approved Magenheim geode world prefabs now receive explicit instance-only
Underworld vegetation rows. They reuse the already-registered prefab and authored placement data;
only Surface-specific altitude/ocean limits are widened. The final detached Underworld catalog now
fails admission if any of the 22 required raw-resource pickup spawners is absent and verifies every
admitted geode row before ZoneSystem startup continues.

The dual-map audit found that the instance-scoped v2 map persistence Harmony classes existed but
were never installed by MagenheimPlugin. They are now installed. The session bridge is rehydratable
after save/reload instead of being blocked by process-static one-shot state, and asynchronous map
completion no longer treats alpha=1 on an otherwise black RGB24/RFloat texture as proof that pixels
were published. Map-generation workers may also use the carried AsyncLocal Underworld scope when a
third-party generator caches the process WorldGenerator singleton inside Task.Run.

Live acceptance remains required after a verified 0.0.142 install.

# Development closeout - 0.0.141 fresh-world admission and worldgen catalog partition

The 0.0.140 live fresh-world log reached native Underworld construction, then exposed three
world-generation boundary defects. A new parent save has no `_main.0.db2`, therefore Valheim does
not call `ZDOMan.LoadChunks` before Magenheim admission; the child namespace now starts empty and
binds the authoritative parent path on the first native `SaveChunks` call, while later path changes
still fail closed.

Jötunn registers CustomLocation/CustomVegetation rows process-wide. After the detached Underworld
ZoneSystem receives its copy, Magenheim now removes only `Magenheim_Underworld_*` rows that use the
reserved 1024+ biome flags from Surface generation. This prevents Surface
`AltBiomeWorldData.GetRandomPointByBiomes` from indexing unsupported custom biome keys while keeping
the rows available to instance 1.

Foreign negative/complement biome masks such as GemstoneSpawner's `-33` are no longer treated as
explicit claims on Magenheim's future high bits. They are excluded from the detached Underworld
catalog without mutating the shared foreign registration object; positive foreign masks that
explicitly claim a reserved bit still fail closed.

Source/static architecture checks are updated for these boundaries. Live acceptance still requires
a fresh 0.0.141 world load proving admission, Surface location generation, Underworld startup, first
save binding and reload.

# Development closeout - 0.0.138 native catalog and menu callbacks

The 0.0.137 live log loaded all ten backdrops and passed resource/flora registration, then repeatedly
failed native world creation: m_locationsByHash copied 0 of 260 entries. CopyZoneConfiguration
skipped readonly fields even though this dictionary's contents remain mutable. Populate the child's
existing native dictionary without aliasing the Surface dictionary. The installed-game test invokes
the actual copier with 260 entries and checks reference preservation and independent mutation.

Stop failed admission attempts until the world session resets, avoiding repeated multi-second
WorldGenerator/scene construction. Wait normally for missing native services before admission.
Defer both generic behaviour patch families until the Underworld context is bound; remove them on
session reset. Ambient scene lookup also returns immediately when no Underworld exists.

The user observed 16 FPS in the main character/world-selection menu. The failed admission loop is
a separate post-selection problem. Removed menu callback overhead is source-verified; FPS recovery
is not yet measured. Successful admission, generation and travel remain live acceptance requirements.

# Development closeout - 0.0.137 content registration and loading artwork

The 0.0.136 live log reached boot without the prior singleton cascade, but rejected eleven
resource pickups and could not find the incorrectly named YggdrasilShoot1 flora donor.
Allow native Pickable one-shot destruction when no hide target exists; Valheim retains zone
generation state. Use the installed game's YggaShoot1 donor. The asset gate verifies the flora
and pickup donor names against the installed game's SoftRef manifest.

Ten separate PNG crops of the user-supplied artwork are packaged under assets/loading/underworld
for the remote Underworld loading presenter. The six remote commits through 40585ed9 retain their
loading/weather implementation. Use the existing byte-array PNG loader and current Unity font
to avoid the ReadOnlySpan compile error and obsolete Arial resource. Preserve image aspect ratio.

Live acceptance: confirm all 22 resource pickups register, flora has no missing-donor warning,
and Underworld generation displays separate panels without dividers or stretching.
Collect one-shot resources, save/reload and verify depletion. These are not offline-proven claims.
The separate Valheim1012CompatRestorer/OdinUndercroft file-lock error remains outside this repair.

# Development closeout - 0.0.136 startup singleton repair

The 0.0.135 startup log reported 31,939 ZoneSystem missing-member errors. The first interrupted
SteamManager.Awake; repeated failures in the shared instance adapter then blocked scene and mod
callbacks. Classification: native authority binding / adapter initialization failure, before
Underworld host admission. The subsequent Steamworks-not-initialized error followed that failure.

Bind ZoneSystem.s_instance, WorldGenerator.m_instance, ZDOMan.s_instance, ZNet.m_world and
ZNet.m_zdoMan directly. Remove the guessed singleton helper and require all five bindings.
The build now runs the adapter initializer and checks field type, static/instance identity and
writability. A mutation regression rejects a missing binding in this exact adapter.

The same log also contains a separate Valheim1012CompatRestorer file-lock error while overwriting
OdinUndercroft.dll. That compatibility helper is outside this repository; this repair does not
claim to fix it. Verify a fresh startup reaches the menu without the Magenheim/Steam error cascade,
then resume the live admission, travel, persistence and multiplayer matrix in TESTING.md.

# Development closeout - 0.0.135 native API repair

Repairs compilation and installed-game binding failures without changing the required two-instance
architecture. Zone metadata now follows native sector/chunk persistence and is excluded from object
creation and peer replication. The load filter includes the installed boolean argument, native
ZoneSystem save/load use current signatures, and teardown restores `s_instance`.

The prior report's 12 flags were not 12 missing APIs: seven were incorrect receiver/fallback
inferences, two requested explicit RPC parameter signatures, and three exposed real stale bindings.
The checker now verifies nested peer/pathfinding fields explicitly and tests rejection of a missing
singleton and a damaged by-ref signature. Dynamic resolvers use plain reflection for inspection.

Expected launcher entry: Magenheim v0.0.135 by Local. Release description is in release.json.
Compilation and binding checks do not establish live instance admission, native save/reload,
multiplayer isolation, map behavior or Deep Gate acceptance. Follow the ordered test in TESTING.md.

# Development closeout - 0.0.108 biome ground features

Ten authored ground models add rootgrass carpets/tufts/woven mats, three sulfur drift forms,
silt ripples, frost fans, shale scree and peat mats. A separate deterministic sampling grid
fills gaps between groves without moving existing canopy placements. Low patches align to
terrain normals; slope, water, biome-edge, gate-clearance and curvature checks limit placement.
Ground meshes are nonblocking, share cached assets, and add no lights or gameplay components.

Expected enabled launcher entry: Magenheim v0.0.108 by Local.
Launcher description: "0.0.108: Adds grass-like root carpets, tufts and woven mats; wind-shaped sulfur drifts, silt ripples, frost fans, shale scree and peat mats. Stable ground patches follow biome terrain slopes and keep the gate approach clear."
See TESTING.md and docs/validation/2026-09-25-biome-ground-features.md for acceptance.

# Development closeout - 0.0.107 distinct biome relief

Removes the shared ordinary-terrain clamp and the accidental zero-height basin floor.
Six biome profiles supply different amplitudes, wavelengths and landform shapes. Angular
province borders blend over 256 metres on each side. The first 80 metres around the gate
retain gentle relief; regional geography reaches full strength at 320 metres. The native
instance minimum is expanded to -896 metres, within the existing engine instance layer.
Monuments blend from local ground to their existing absolute summits.

Expected enabled launcher entry: Magenheim v0.0.107 by Local.
Launcher description: "0.0.107: Distinct biome terrain with unclipped relief: fungal hills, deep Blackwater basins, sulfur ridges, frozen ranges, faulted Fracture Zones and sunken decay. Smooth biome borders and a protected gate approach."
Core tests and a same-seed terrain survey verify the change; live acceptance remains pending.
See docs/validation/2026-09-25-biome-terrain.md and TESTING.md.

# Development closeout - 0.0.106 fungal canopy variants

Four fungal canopy archetypes now have four authored growth forms each, for sixteen distinct
models. Twelve new variants change crown count, tier spacing, branching, curvature and growth
habit. New forms participate in the existing seeded cell placement. Original landmark indices
are preserved; the selection code reads the catalogue size instead of assuming eleven entries.
Stem collision, shared model caching, authored emissive materials and ground cover are retained.

Expected enabled launcher entry: Magenheim v0.0.106 by Local. Description begins
"0.0.106: Fungal Forest expands to sixteen authored canopy forms". Use closeout.ps1 -Offline
for package and installation verification. Live variety, collision, performance and multiplayer
acceptance remain pending; see TESTING.md and docs/validation/2026-09-25-fungal-canopy-variants.md.

# Development closeout - 0.0.105 live regression repair

Repairs confirmed by user screenshots/logs: missing roof shader, incorrect purple gate, arrival
inside the central stone, and player-relative scenery rebuilding that removes collidable rocks.
Native asset IDs now load the actual Aesir gate and a shipped unlit background shader. Arrival
resolves from the admitted gate footprint. Ecology uses fixed seeded cells with retention;
nearby surviving objects are never rebuilt merely because the player moves. Fungal joins are
fused geometry, ground relief and population are increased, forest haze is stronger, and the
minimap biome labels read native Underworld data. Incoming instance/persistence repairs retained.

Testing candidate: run closeout.ps1 -Offline; expected enabled launcher version 0.0.105.
Launcher description: "0.0.105: Repairs missing cavern sky and Aesir gate; arrival uses the gate approach. Stable, denser ecology stops rocks moving underfoot. Organic fungal joins, stronger forest haze, rougher ground and correct Underworld biome labels."

See TESTING.md for live regression checks. Offline tests and Blender model review do not prove
live gate placement, shader rendering, collision stability or map label restoration.
Execution record: docs/validation/2026-09-24-underworld-live-regressions.md.

# Development closeout - 0.0.104 sunless cavern sky

Testing candidate: masked lava/fungal roof glow, dim native day/night lighting, native cloud
geometry forming haze at 4800m, and sharp terrain capped at 5100m. The native dungeon-height
shortcut is bypassed only inside the admitted Underworld instance so landscape building works.

Run `closeout.ps1 -Offline` to validate, package, back up and install into Central Fuckery.
Expected enabled launcher version: **Magenheim 0.0.104**.
Launcher description: "0.0.104: Sunless lava-and-fungal cavern sky with masked glow and dim day/night lighting. Overhead haze at 4.8 km; sharp terrain capped at 5.1 km. Fixes the native dungeon-height restriction for Underworld building."

The sky mask supplies visible glow; native directional/ambient light illuminates terrain.
Offline roof and terrain projection reviews do not establish live world acceptance. Test entry
and return, sky/haze readability at day and night, cliff collision, and Surface restoration.
Developer console: `devcommands`, then `magenheim_underworld enter|return|status`.
Execution evidence: `docs/validation/2026-09-23-sunless-sky-closeout.md` in the source repository.

# Development closeout - 0.0.103 monumental terrain

Full offline closeout installed and hash/catalog verified in Central Fuckery. Terrain includes
sparse kilometre-scale spires and sheer plateaus, a coarse realm horizon with stitched resident
edges, vertical cliff UVs and clearer long-distance air. 44,588 Core assertions; runtime clean.
Evidence: `docs/validation/2026-09-23-monumental-terrain.md`. Not observed in a Valheim world.
Next continuation: cloud-line height clarification, lava/fungal skybox and dim sunless day/night.

# Development closeout - 0.0.102 Blackwater Deep

Blackwater's seven canopy and twelve cover slots now use authored models. Its four resource
items and pickups use matching bodies and icons. Both biome families use the shared flora kit,
including UV-coverage padding before texture downsampling. Existing habitats and resource
behaviour are preserved. Deep Sigil compatibility is repaired against the installed Valheim API.

Run `./closeout.ps1 -Offline` to verify, package, back up and install into Central Fuckery.
The expected enabled launcher entry is **Magenheim 0.0.102**, beginning **0.0.102: Blackwater
Deep custom pass**. Closeout verifies payload hashes and launcher metadata. Actual world
appearance, collision, resource interactions and multiplayer still require live acceptance.
Latest execution evidence is in `docs/validation/2026-09-23-blackwater-closeout.md`.

## Historical closeouts

# Development closeout — 0.0.49 testing candidate

Repairs the `Inventory.Changed` binding broken by Valheim 1.0.12 and adds a static
reflection-binding gate that catches this defect class. Evidence, including the proven
negative test against the pre-repair DLL, is in
docs/validation/2026-09-15-inventory-changed-reflection-repair.md. Rebased onto the
incoming Underworld authority composition slice and re-verified. Tests: 36,868 assertions;
runtime: zero warnings/errors; 20 patch targets and 7 literal reflection bindings verified.

Also repairs `install-local.ps1`, whose checksum verification collapsed the JSON manifest
into a single entry under Windows PowerShell and failed every package. That defect blocked
this delivery until fixed; it never affected gameplay.

Delivered: `closeout.ps1 -Offline` installed and SHA-256 verified 0.0.49 in Central Fuckery
and updated its enabled launcher entry. Installed DLL 0.0.49.0, hash
`1566007F...534211E9` matching the built artifact. Payload backup
`backups/Local-Magenheim-20260915-112600.zip`; catalog backup
`backups/mods-20260915-112601-497.yml`. Startup and world acceptance remain unclaimed.

## Historical closeouts

# Development closeout — 0.0.48 testing candidate

Underworld U0/U1 authority integration and baseline repairs are detailed in docs/validation/2026-09-15-underworld-authority-integration.md. Schema 5 ships validated catalogs only; no Underworld world transition or runtime construction is enabled. Tests: 36,859 assertions; runtime: zero warnings/errors.

## Historical closeouts

# Development closeout — 0.0.47 testing candidate

Date: 2026-09-14 (local). This is a testing handoff, not full feature completion.
Source on main was 0.0.44; older README (0.0.16) and state records (0.0.38) were stale.

## 0.0.47 startup repair

Fixed ambiguous damage/armor patch targets and an outdated tooltip signature.
All eleven patch declarations pass the new installed-assembly build gate. An
isolated startup registered Crystal Shaping and completed Magenheim bootstrap;
Steam initialization still prevents world acceptance in that isolated test.
See `docs/validation/2026-09-14-0.0.47-startup-repair.md` for evidence and limits.

## Historical 0.0.46 skill visibility follow-up

The active Central Fuckery profile was found loading 0.0.16, whose log confirmed
registration but not a player skill entry. Installed game IL confirms SkillsDialog
uses GetSkillList (instantiated skills only), while GetSkillLevel creates a missing
level-zero entry. The dialog prefix now initializes only the registered Crystal
Shaping skill without raising it or replacing existing XP.

## Implemented fixes in this candidate

- Restored compilation of the full deterministic test suite: missing crystal namespace
  in SocketingTests and nullable alignment projection in the encounter validator.
- Updated the Deep Fracture hover interface to the installed game's float offset API.
- Repaired workstation recipe/panel access through a single cached Harmony reflection
  boundary; current game members are private and refresh requires a boolean argument.
- Replaced private routed-RPC server lookup with public ZNet server-peer lookup.
- Fixed successful refinement nullable output access, geode collider variable shadowing,
  nullable configuration/localization checks and short-circuited stale socket diagnostics.
- Regenerated workstation mesh, OBJ, icon and preview from the already corrected strap
  generator. The generator fix had not reached the shipped geometry.
- Added package/assembly version agreement, guarded package cleanup, an explicit offline
  build option, SHA-256 inventory and updated testing documentation.

## Assets and features still needing implementation

| System | Source evidence | Remaining work |
| --- | --- | --- |
| Deep Fractures | Core expedition/layout/encounter plans, room geometry and dormant traversal exist. Plugin activates room registration only. | Deterministic collision-safe physical corridors, isolated interior assembly/restoration, entry/exit binding, creature/reward realization and full persistence; then enable surface location registration. |
| Wardstone, Passage Stone, Runed Totem, Runic Keelstone | Eleven model IDs in WorldArtifactVisuals have no external source call sites. | Item/build registration, recipes/icons, ownership/network state, ward/travel/totem/ship behaviors and acceptance tests. |
| Crystal Lantern/Brazier, Rune Engraver | Geometry exists in the same unbound model library. | Registration, icons, lighting/fuel or engraving rules, progression and persistence. |
| Seidr ritual focus and three Spirit fetishes | Model geometry only. | Ritual/summon rules, costs, cooldowns, networking, save state, recipes and icons. |
| Eight boss resonance hearts | CapstoneArtifactVisuals contains eight hearts with no external source call sites. | Trophy consumption, progression, actual resonance effects, authoritative transactions, recipes/icons and persistence. |
| Fate Shard, Fate Crystal, Norn Spindle | Three additional capstone models only. | Fate acquisition/intervention rules, costs, authoritative state, recipes/icons and persistence. |
| Art acceptance | Eleven file-backed models validated; many later pieces generate geometry/icons at runtime. | In-game review of silhouettes, hand placement, collisions, wear variants, material opacity, lighting and performance for every family. |

These model libraries are not playable features merely because they compile. The
surface dungeon gate stays closed: enabling intersecting corridor geometry would
violate the project's existing acceptance requirement. Later model-only systems
need their gameplay designs reconciled before implementation; they are excluded
from playable claims for this package.

## Connected features awaiting live acceptance

Workshop opening/refinement and XP; remote sockets/extraction and equipment effects;
eight elemental staff families; weapons; furniture/decor/architecture/banners;
Sentinel and munition recipes; elemental beds, Ice Box, Dais and alchemy all have
runtime bindings. Remaining work is the live matrix in TESTING.md, especially
server/client inventory outcomes, persistence and third-party compatibility.

## Observed validation

- Core harness: 13,974 assertions passed (plus separately reported worldgen/placement/compatibility groups).
- Runtime Release compilation against installed game assemblies and Jotunn 2.30.0:
  zero warnings and errors after repairs.
- All eleven mineral/workshop meshes pass closed/outward surface, finite geometry,
  UV, atlas and transparent-icon checks.
- Online NuGet vulnerability data was unavailable; the candidate uses explicit offline
  build mode and cached dependencies. No online vulnerability audit is claimed.
- No new disposable-world, multiplayer, visual or persistence acceptance is claimed.
  Historical live-world evidence remains limited to 0.0.16.
- An isolated batch startup attempt hit Steamworks initialization errors and did not
  establish BepInEx/plugin registration. Its process was stopped; evidence remains
  under ignored `dist/closeout-smoke-0.0.45/unity.log`.
- Package checksums cover the listed files, only the two owned DLLs, the
  manifest version and byte-for-byte matching source runtime assets. The local
  installer checks this inventory and assembly version before profile writes.

## Testing handoff

Testing closeout now requires live delivery through `closeout.ps1`, including
installation into the active profile and verification of r2modman's separate
`mods.yml` catalog. `release.json` owns the current version's description; the
builder rejects mismatched versions. The installer preserves all other catalog
records, backs up the catalog, and updates our version, description, dependencies,
website and install timestamp only after package files have been verified.

Open r2modman after closeout and confirm **Magenheim v0.0.47 by Local** with the
startup patch repair description. Earlier handoff statements
about leaving the active profile untouched describe the initial packaging pass.

Build output: `dist/Local-Magenheim-0.0.47.zip`. The ZIP includes this inventory,
TESTING.md, README.md, dependencies manifest and per-file checksums. Install into
a disposable mod profile and execute the ordered matrix. Retain the old profile
for comparison. Closeout installs the active mod and launcher metadata; it does
not modify saved worlds.
