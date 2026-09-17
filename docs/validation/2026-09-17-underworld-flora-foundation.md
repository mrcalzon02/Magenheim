# Underworld flora foundation — 0.0.62

## Scope

The user corrected the environment model: altered surface-world terrain under one custom
fantastical cavern-roof skybox shared by all biomes. The flora/terrain plan now states this
explicitly; companion plans have a controlling correction superseding roof-shelf and
ceiling requirements. No skybox implementation or existing district asset was changed.

F1a adds Glowcap (6–8m), Spirestalk (10–14m), and Shelfwood (4–5m) definitions to
foundation.json. Terrain eligibility requires the Underworld layer, the declared Fungal
Forest biome, dry supported ground, bounded slope and no local obstruction. Shelfwood
also requires an exposed rock face. Initial slope limits (30/25/45 degrees) are provisional
placement tuning, awaiting visual/world acceptance.

The immutable flora snapshot is validated by the standard loader, included in canonical
peer fingerprints and preserved through balance overrides. Unknown biomes, duplicate
identities, invalid dimensions, missing fields and non-finite terrain samples fail closed.
The optional flora field preserves older schema-5 documents; populated data changes the
fingerprint. No roof, roof collision or per-biome skybox input exists in placement rules.

## Verified

- build.ps1 -Offline completed: 37,166 core assertions plus separately reported suites.
- Runtime compilation: zero warnings/errors.
- All 281 existing model sets and asset gates passed; no new models in this slice.
- 22 Harmony targets, 13 direct reflection bindings and 42 helper field contracts passed.
- Packaged as 0.0.62. Offline dependency vulnerability audit was not performed.

## Remaining

F1a is definition and rule implementation, not playable flora. Runtime terrain/biome
sampling must feed these rules before any vegetation registration. Species assets,
native felling/harvesting, persistence, resources, Mycelial Bench, recipes, equipment and
ground cover remain F1b/F1c. World startup, visual acceptance, multiplayer and save/reload
have not been observed. Do not report F1 or the shared skybox as newly implemented.

## Installed closeout

closeout.ps1 -Offline completed successfully. Magenheim 0.0.62 is installed in the
Central Fuckery profile; every packaged file was hash-verified after copying. The prior
installation and launcher catalog were backed up. The enabled Local/Magenheim launcher
entry, version, dependencies and description were verified by catalog read-back.

DLL SHA-256: `646D442E3A993F12BAE2F3CFA6A647C9DEFE5A3E76F6D2E0F8961662F473E3A9`.

Launcher description: "0.0.62: Begins Fungal Forest flora with validated species definitions
and terrain placement rules. Preserves one shared custom cavern skybox across biomes.
Flora assets, spawning and harvesting remain pending."

The launcher UI and game startup were not visually observed.
