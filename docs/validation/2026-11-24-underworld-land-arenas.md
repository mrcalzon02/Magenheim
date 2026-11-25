# Underworld terrain and land arena verification

## Terrain texture decoding

Valheim 1.0.21 loaded Magenheim 0.0.179 in an isolated Direct3D 11 batch session. Windows reported no game window. Unity’s production terrain loader decoded all 18 packaged PNGs as 256 × 256 textures. Fungal Forest, Blackwater Deep, Sulfurous Wastes, Frozen Caverns, Fracture Zones and Great Decay each passed albedo, normal and roughness decoding.

The runtime build succeeded without warnings. The installed-assembly checks verified 102 explicit Harmony targets, 94 literal reflection bindings and 46 field contracts.

Valheim also bound the authored palettes to both native heightmap texture arrays while running the disposable world. The core suite passed 128,929 assertions.

## Native land arena placement

The registrar requests one site in each owning biome. The placement guard checks prospective sites before Valheim records them. A land site needs a center at least 768 m from origin and 1 m above water, an 8 m gap beyond both location footprints, and no more than 1.35 m of relief across the combat floor. The location runtime grades the model against terrain and extends its foundation down to the local footing.

The native audit passed in disposable world `TerrainQAMGNTRL27`. These are the five land placements logged by the game. Ground Y and planned floor Y are measured at the generated location before its model is streamed.

| Arena | Center X, Z (m) | Distance from origin (m) | Nearest feature gap (m) | Ground Y (m) | Planned floor Y (m) |
| --- | ---: | ---: | ---: | ---: | ---: |
| First Bloom | −704.4, 700.5 | 993.4 | 44.2 | 70.03 | 70.41 |
| Furnace Heart | −1350.7, 2300.1 | 2667.4 | 161.1 | 74.39 | 74.91 |
| White Silence | −2424.5, −3334.1 | 4122.5 | 259.8 | 169.84 | 170.24 |
| Rift Titan | −3391.7, 4480.9 | 5619.8 | 20.4 | 86.25 | 87.27 |
| Carrion Crown | −4928.3, −4803.6 | 6882.1 | 41.5 | 230.59 | 231.26 |
