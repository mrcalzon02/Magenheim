# Blackstone Throne Structure Set

Status: source-authoring implementation lane  
Runtime location identity: `Magenheim_DarkThrone`  
Boss: The Nowhere King

The old `dark-throne` model was an 880-triangle functional shell. It is retained only until this
family is successfully forged. The Blackstone program replaces that visual shell with a modular
authored environment while preserving the existing Dark Throne encounter, spawn, leash and
persistence authorities.

## Authored family

The forge owns these editable Blender/GLB/runtime identities:

- `blackstone-throne`
- `blackstone-banner`
- `blackstone-attendant-seat`
- `blackstone-brazier`
- `blackstone-stair`
- `blackstone-dais`
- `blackstone-parapet`
- `blackstone-bridge`
- `blackstone-arch`
- `blackstone-cliff-edge`
- `blackstone-floor-tile`
- `blackstone-spire`
- `blackstone-pillar`
- `dark-throne` — the assembled final-battle site built from the same design language

Every identity must exist as a real `.blend`, `.glb`, and `.model.json` triplet. Runtime C# does
not synthesize replacement geometry.

## Visual language

The set uses broad dark basalt/voidstone surfaces with selective old-bronze royal trim. Detail is
concentrated at silhouettes, joins, throne regalia, parapet finials, banner standards and brazier
crowns rather than covering every surface with noise. The circular eclipsed-sun emblem is the
recurring royal mark. Cold violet/blue ambience is expected from the world; the set supplies warm
orange brazier lights and restrained violet rune lights.

The 2D source art is owned under `assets/textures/underworld/blackstone/`: basalt, voidstone,
royal bronze, banner/sun cloth, a standalone sun sigil, and ember artwork. Blender sources pack
their images and the exporter content-addresses them into the runtime model-texture library.

## Encounter constraints

The assembled site remains inside the existing 52 x 60 meter Dark Throne authority footprint.
This is deliberate: the new architecture may become taller and denser, but it must not silently
invalidate `DarkThroneArena.CreateDefault()`, the King's hard leash, participant boundary, crystal
ecology positions or the existing King home anchor.

The combat plate remains solid. Bridges, arches, terraces and abyss-edge forms create depth around
the legal arena without punching accidental death holes through the actual fight floor. The King
still stands before the throne and never sits in it.

Eight `Brazier_*` families and eight `Rune_*` objects are authored into the assembled site so
future phase-presentation work can extinguish/relight them without re-authoring the structure.

## Production path

`tools/rebuild-blackstone-throne-set.ps1` performs the complete lane:

1. generate deterministic 2D source artwork;
2. author all Blender sources;
3. export GLB and runtime model payloads;
4. run the global model verifier;
5. run the Blackstone family contract;
6. record generated-asset ownership and verify freshness.

`.github/workflows/blackstone-throne-forge.yml` is a deliberately narrow Blender 5.0.0 forge.
Changing `.github/blackstone-throne.run` on `main` triggers it. Successful output is pushed to a
`production/blackstone-throne-*` review branch rather than being silently written over
authoritative `main`.

## Acceptance boundary

Source/generation acceptance requires all model triplets, owned textures, at least 170 assembled
site parts, complete throne/banner/seat/brazier/terrain role coverage, eight runes, eight ceremonial
braziers, and runtime lights while remaining inside the encounter footprint.

Live acceptance is still separate: Valheim traversal, collision feel, camera readability, boss
movement/leash behavior, multiplayer ownership, save/reload, and the phase-specific
extinguish/relight presentation cannot be proven by Blender export alone.
