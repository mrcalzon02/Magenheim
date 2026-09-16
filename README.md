# Magenheim

**Magic begins as geology.**

## 0.0.47 testing candidate

Eight biome geodes, eight five-tier crystal families and shards, Crystal Shaping,
workstation opening/refinement, sockets, eight four-tier staff families, crystal
weapons, furniture, architecture, banners, Sentinel, elemental beds, Ice Box,
Enchanting Dais and alchemy are connected in runtime source.

This package is for disposable-world testing. Compilation and deterministic tests
do not establish multiplayer, persistence, balance or visual acceptance.

## Install and test

Import `dist/Local-Magenheim-0.0.47.zip` as a local mod in a disposable r2modman
profile with Jotunn 2.30.0 and JsonDotNET 13.0.4 and their loader dependencies.
Use the identical package on host and clients: patch versions must match.
Launch **Modded**. Confirm `Loading [Magenheim 0.0.47]` in the BepInEx log.
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
source repository. Procedural furniture, weapons and other models are built by
runtime source. Players require no art tools. Development follows `INSTRUCTIONS.md`
on `main`.

## Editable 3D models

See [the model library](assets/models/README.md) for 276 Blender sources, GLB exports, review sheets, and the edit/export workflow. Runtime visual builders load those assets instead of constructing shapes.
