# Underworld terrain and land arena verification

## Terrain texture decoding

Valheim 1.0.21 loaded Magenheim 0.0.179 in an isolated Direct3D 11 batch session. Windows reported no game window. Unity’s production terrain loader decoded all 18 packaged PNGs as 256 × 256 textures. Fungal Forest, Blackwater Deep, Sulfurous Wastes, Frozen Caverns, Fracture Zones and Great Decay each passed albedo, normal and roughness decoding.

The runtime build succeeded without warnings. The installed-assembly checks verified 102 explicit Harmony targets, 94 literal reflection bindings and 46 field contracts.
