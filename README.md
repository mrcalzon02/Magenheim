# Magenheim

**Magic begins as geology.**

## 0.0.155 testing candidate

Deep structure work is now active. The existing Deep Fracture expedition is admitted as a sparse
Fracture Zones dungeon inside the Underworld: six entrances per world, at least 1.4 km apart, using
the same authored 20-district expedition, encounters, passages, traversal links and return path as
Surface Deep Fractures. The return portal is entrance-context aware, so an Underworld fracture
returns to its Fracture Zones entrance instead of implying a Surface transition.

A durable six-biome dungeon catalog now exists. Deep Fracture is the only runtime-ready entry; the
other five remain deliberately non-spawning until real interiors exist. Their shared production
contract is 15-20 large themed room families reused 2-3 times per run. The new deterministic biome
dungeon planner produces a connected branching topology with cross-links rather than a straight
corridor chain.

Rootwarren, Drowned Vaults, Cinderworks and Rime Sepulcher now have source/runtime interior
architectures behind that admission gate. Rime Sepulcher contributes sixteen Frozen room families
plus one adaptive physical passage, endpoint-derived Whiteout/cold exposure, persistent Frozen
encounters/resources and same-instance return travel. It remains Planned because its seventeen
Blender/GLB/runtime payloads have not been forged in this execution; missing or partial families
cannot seed a location.

### Earlier fixes in 0.0.154


The Underworld weather program is now source-complete rather than fog-only. All eight biome-owned
events have Magenheim-owned procedural VFX; hostile events have server-authoritative gameplay
pressure; Thermal Surge amplifies the existing geothermal hazard; Black Bloom temporarily increases
nearby Underworld creature perception; and Crystal Resonance pulses nearby crystal/geode/shard
renderers before restoring their original material property blocks.

Deep Boons and matching Underworld armour now reduce atmospheric gameplay pressure without deleting
the visual weather. The Defiant Censer is admitted as a real Crown Reliquary-crafted held item and
creates a 16m Great Decay suppression radius for nearby players, reducing contamination, fog and
bespoke particle density. Sporefall, Deep Fog and Crystal Resonance remain deliberately non-damaging
where their design is visual/navigation rather than arbitrary DOT pressure.

This is source completion, not live acceptance. 0.0.154 still requires a current Valheim build/test
for particle readability, damage/balance, host/client agreement, Censer radius, crystal restoration
and correct Surface weather restoration.

### Earlier fixes in 0.0.153


The old independent 3,072m monument grid is retired. Terrain height extremes now belong to the
same jittered Voronoi cells that own biome geography: roughly 5.5% of eligible non-Blackwater land
cells between 26% and 82% world radius become rare massif variants, lifting most of the cell
interior by roughly 1.5–3.3 km while preserving that biome's own ridges/basins/faults. The lift
falls away across the cellular wall band instead of converging to a needle spire.

Nominal biome elevation now rises in progression order (Fungal, Blackwater, Sulfur, Frozen,
Fracture, Great Decay), and biome scoring gets a broad graded radial preference toward earlier
biomes inward and later biomes outward. Noise still owns enough of the score to create enclaves,
returns and irregular boundaries rather than concentric rings.

Blackwater/Voronoi seams are wider: the nominal edge band grows from 320m to 440m with stronger
plasma width variation, producing broader river walls and more substantial cave-like cuts between
cells.

### Earlier fixes in 0.0.152


The Underworld terrain plate now fades into a broad Blackwater ocean ring before the hard 8 km
instance boundary. A plasma-warped shoreline begins around 84% of world radius, varies by roughly
260 m, and reaches guaranteed full ocean depth by about 96.5% radius. The outer band is forced to
Blackwater Deep and carved after all ordinary/rare-cell elevation so high massifs cannot punch
through the edge barrier.

The deepest outer ocean reaches roughly 140 m below the shared water level. The logical world
boundary therefore sits beyond deep water rather than at the edge of walkable terrain.

### Earlier fixes in 0.0.151


Underworld biome geography now uses a hybrid cellular generator instead of smooth noise alone:
an ideal hex lattice supplies macro spacing, each cell seed may move by up to 50% of the nominal
hex radius, Voronoi ownership chooses the regional cell, and plasma/domain noise bends and bulges
the cell edges.

Those Voronoi seams are also the hydrology authority. Strong edge bands are carved below the shared
water plane and become Blackwater Deep, producing a connected warped river/ocean-depth grid through
the other biome territories. The river network fades in outside the protected Fungal arrival core
so the Deep Gate is not surrounded by a synthetic moat. The deepest seams reach roughly 30m below
water level after rare-cell uplift is applied, so the cellular drainage network remains legible
even beside massif cells.

### Earlier fixes in 0.0.150


The Underworld biome map is no longer a central disk surrounded by five fixed angular provinces.
Only the inner 10% radius is guaranteed Fungal Forest. Beyond it, six independently seeded
kilometre-scale fields compete through warped coordinates and biome affinities, producing broad
organic regions, fingers, enclaves and repeated biome pockets. A soft Fungal arrival shoulder keeps
the gate progression-safe without drawing a perfect circular border. Near-tied biome fields blend
terrain profiles while ecological identity stays discrete.

The layout algorithm is now part of the Underworld authority fingerprint, so peers with the old
pizza-wheel generator fail closed instead of silently generating different terrain.

### Earlier fixes in 0.0.149


The developer console now has `magenheim_underworld survey [radius]`: an in-engine environment
sampling path that reports deterministic terrain height/biome variation, rare-cell massif hits,
actual loaded Magenheim structure families and nearby inhabitants around the player's real
Underworld position. This is the evidence path for comparing loading artwork to generated worlds.

### Earlier fixes in 0.0.148


The authored elemental Surtlings now provide genuine non-donor-silhouette inhabitants in every
canonical home already declared by their roster: Fire in Sulfurous Wastes, Water in Blackwater
Deep, and Earth/Wind in Fracture Zones. Radiance and Umbral remain site-specific rather than being
forced into a biome. Population limits are deliberately sparse while bespoke fauna are being
re-bodied.

### Earlier fixes in 0.0.147


Sulfurous Wastes now receives a much denser burnt ecology layer through Magenheim-owned native
vegetation: multiple scorched tree silhouettes, charred fallen branches, burnt stumps and cinder
bushes. These are stripped scenery copies, so Valheim's Ashlands harvest/drop behavior is not
imported. The two authored Fire Surtling bodies now also have native Sulfurous Wastes spawn rows,
providing a genuine custom inhabitant while the larger donor-creature re-body campaign continues.

### Earlier fixes in 0.0.146


Magenheim's custom loading presentation is now explicitly scoped to mod-owned Underworld work:
native instance construction plus local Deep Gate transfers. Successful synchronous transfers keep
the overlay alive for rendered frames so it can actually be seen; it does not replace Valheim's
ordinary global loading presentation. Loading-art acceptance also requires a real biome inhabitant
or enemy silhouette grounded in runtime creature art.

### Earlier fixes in 0.0.145


Stops the map-tab performance collapse caused by repeated 8 MiB Surface/Underworld map payload
swaps, and idles detached Underworld physics and pathfinding until a player enters. Placeable
materials now explicitly disable inherited parallax/displacement, the Crystal Hearth fire is
contained to its basin, and the Crystal Sentinel faces its target direction.

The Crystal Bench, Geode Table, Chair and Pedestal now use the same 37 per-part
UV textures in editable Blender sources, GLB previews and runtime. Material tints
are preserved, their Hammer icons are refreshed, and packaging verifies source
pixels and bindings. Includes 0.0.139's clean managed installation replacement.

### Earlier fixes in 0.0.138

Copies the readonly native location index into the independent Underworld catalog. Failed native
construction stops for the session instead of rebuilding every frame. Broad scene callback hooks
are installed only when the Underworld is bound and removed on unload. Main-menu FPS and successful
world generation still require live confirmation; offline gates exercise the actual catalog copier.

Adds ten individually cropped Underworld loading backdrops from the supplied artwork. The remote
Underworld loading presenter rotates them while preserving the image aspect ratio.
Fixes eleven rejected one-shot resource pickups and corrects the flora donor to `YggaShoot1`.

Repairs native game API bindings, keeps Underworld zone metadata in Valheim's native chunk
save index, and fixes reflection and dynamic patch validation. One process, one campaign and
two native world instances remain the required architecture. Live admission, save/reload,
gate travel and simultaneous two-player isolation still require acceptance in TESTING.md.

## Earlier content: 0.0.108

Ten new ground features add grass-like root carpets, tufts and woven mats; sulfur powder
drifts, rippled deposits and lee banks; Blackwater silt ripples, frost needle fans, shale scree
and fibrous peat mats. Stable patches fill spaces between groves and align to terrain slopes.

Ordinary Underworld terrain now uses distinct biome height and shape profiles without a shared
height clamp. Deep basins, broad ridges and faulted ranges blend across province borders.
The gate approach retains gentle relief for 80 metres; full regional relief begins at 320 metres.

The Fungal Forest now draws from sixteen authored canopy models: four species with four
growth forms each. Twelve new meshes add bent/twin/elder glowcaps, young/crooked/towered
spires, forked/spreading/clustered puffcaps and young/splayed/woven tangles. Stable ecology
cells select tall landmarks, a mixed middle canopy and younger edge growth from this palette.
Each model has stem collision and an editable Blender source; no per-tree lights are added.

The Underworld now has a shared lava-and-fungal-star cavern sky with no visible sun and a dim
natural day/night lighting cycle. Overhead haze sits at 4.8 km; rare biome-owned massif cells can
rise several kilometres above their surrounding country while retaining the underlying biome
surface language. The native dungeon-height classification no longer blocks building across the
dedicated instance. Live world appearance and building acceptance remain pending.

Eight biome geodes, eight five-tier crystal families and shards, Crystal Shaping,
workstation opening/refinement, sockets, eight four-tier staff families, crystal
weapons, furniture, architecture, banners, Sentinel, elemental beds, Ice Box,
Enchanting Dais and alchemy are connected in runtime source.

This package is for disposable-world testing. Compilation and deterministic tests
do not establish multiplayer, persistence, balance or visual acceptance.

## Install and test

Import `dist/Local-Magenheim-0.0.155.zip` as a local mod in a disposable r2modman
profile with Jotunn 2.30.0 and JsonDotNET 13.0.4 and their loader dependencies.
Use the identical package on host and clients: patch versions must match.
Launch **Modded**. Confirm `Loading [Magenheim 0.0.155]` in the BepInEx log.
Building the ZIP alone does not update a profile; use install-local.ps1 to install it.

See [TESTING.md](TESTING.md) for the acceptance matrix and Earth workshop commands.
See [CLOSEOUT.md](CLOSEOUT.md) for unfinished features, fixes and validation limits.

## Build

For testing closeout, run `./closeout.ps1` (or `./closeout.ps1 -Offline` with cached
dependencies). This tests, packages and installs into the active Central Fuckery
profile, backs up the prior installation/catalog, verifies installed hashes, and
updates r2modman's visible version and latest-change description. Close Valheim
and r2modman first, then reopen r2modman to see the refreshed entry.

`release.json` supplies the version-specific description. Packaging fails if its
version differs from the plugin. A ZIP alone does not complete testing closeout.

Run `./build.ps1` to run the core suite, compile and package. On a disconnected
machine with cached dependencies, use `./build.ps1 -Offline`; this explicitly
skips the online dependency vulnerability audit. Package contents include only
Magenheim assemblies, runtime assets, definitions, test instructions and checksums.

With Valheim closed, `./install-local.ps1` builds, backs up and installs into the
selected profile. To install an already verified package, use `-SkipBuild`.

Original editable art and generators are under `assets/earth` and `tools` in the
source repository. Furniture, weapons and other models load from the exported Blender library. Players require no art tools. Development follows `INSTRUCTIONS.md`
on `main`.

## Editable 3D models

See [the model library](assets/models/README.md) for 356 Blender sources, GLB exports, review sheets, and the edit/export workflow. Runtime visual builders load those assets instead of constructing shapes.

### 0.0.105 live feedback repair

Scenery stays at seeded world positions as you move, including collidable rock formations.
The Aesir boss gate loads directly from the native asset and entry uses its clear approach.
The cavern roof uses a shader actually shipped by Valheim. Underworld names now appear on the
minimap; fungal groves are denser, their background haze is stronger, ground is rougher, and the
puffcap's roots/trunk/branches form a fused organic skin. Live verification remains in TESTING.md.
