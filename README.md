# Magenheim

**Magic begins as geology.**

## 0.0.140 testing candidate

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
natural day/night lighting cycle. Overhead haze sits at 4.8 km; occasional sheer plateaus and
needle spires top out just above it at 5.1 km. The native dungeon-height classification no longer
blocks building across the dedicated instance. Live world appearance and building acceptance
remain pending.

Eight biome geodes, eight five-tier crystal families and shards, Crystal Shaping,
workstation opening/refinement, sockets, eight four-tier staff families, crystal
weapons, furniture, architecture, banners, Sentinel, elemental beds, Ice Box,
Enchanting Dais and alchemy are connected in runtime source.

This package is for disposable-world testing. Compilation and deterministic tests
do not establish multiplayer, persistence, balance or visual acceptance.

## Install and test

Import `dist/Local-Magenheim-0.0.140.zip` as a local mod in a disposable r2modman
profile with Jotunn 2.30.0 and JsonDotNET 13.0.4 and their loader dependencies.
Use the identical package on host and clients: patch versions must match.
Launch **Modded**. Confirm `Loading [Magenheim 0.0.140]` in the BepInEx log.
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
