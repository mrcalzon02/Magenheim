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
- `blackstone-terrace`
- `blackstone-wall-buttress`
- `blackstone-cathedral-wall`
- `blackstone-gate`
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
royal bronze, banner/sun cloth, a standalone sun sigil, and ember artwork. Owned normal,
metallic-smoothness, and ember-emission maps live under `assets/material-source/blackstone/`.
Blender sources bind both authorities explicitly and the exporter content-addresses them into the
runtime model-texture library; Blackstone does not inherit unrelated donor PBR maps.

## Encounter constraints

The 52 x 60 meter Dark Throne authority remains the encounter's X/Z leash contract, but it is no
longer represented by a giant walkable floor. `Arena_Foundation` is now a non-colliding marker
roughly twenty meters beneath the playable architecture. The actual site is a suspended vertical
hall: gate and lower bridge, lower terrace, first grand stair, intermediate terrace, second grand
stair, upper combat terrace, throne stair, and a separate throne platform.

The King spawns and recovers on the upper battle tier in front of the throne. Crystal ecology and
the dais interaction point are moved onto real walkable tiers instead of remaining at old ground
level. Side galleries, cross-bridges, under-arches, abyss-edge geology, cathedral walls, buttresses
and a high rear skyline create the depth visible in the reference without changing the authoritative
X/Z leash into scenery.

Twelve `Brazier_*` families and eight `Rune_*` objects are authored into the assembled site so
phase presentation can extinguish/relight them without re-authoring the structure.

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

Source/generation acceptance requires all model triplets, owned textures, at least 360 assembled
site parts, four distinct walkable elevation tiers, complete throne/banner/seat/brazier/terrain role
coverage, cathedral side/rear wall massing, buttresses, eight runes, twelve ceremonial braziers,
runtime lights, and at least ~49 meters of total vertical envelope from abyss marker to skyline.

Live acceptance is still separate: Valheim traversal, collision feel, camera readability, boss
movement/leash behavior, multiplayer ownership, save/reload, and the phase-specific
extinguish/relight presentation cannot be proven by Blender export alone.


## Visual usability revision

The Cycles acceptance plates exposed hero-prop quality failures that topology checks alone could not
catch. Banner revision 2 replaces the rigid wallpaper panel with a weighted twin-standard assembly,
folded/tapered cloth, one non-repeating 0..1 heraldic field and one restrained eclipsed sun. The
raised duplicate sigil is forbidden. The throne now uses a narrower seat, layered back, wings,
capped arms and central apex. Braziers use an octagonal vessel, coal bed, collar and clean emissive
flame family instead of square trays with noisy ember-clump flames.

The verifier now treats these as production requirements: banner hardware, cloth vertex/fold depth,
single-tile UV range, throne silhouette parts and brazier vessel parts must all pass before admission.
