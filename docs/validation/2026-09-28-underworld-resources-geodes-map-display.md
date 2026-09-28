# Underworld resource/geode/map audit — 2026-09-28

This slice responds directly to the fresh-world test and the subsequent deployment mismatch.

Eight existing geode families are assigned to Underworld biomes without duplicate prefab
registration: Meadows Earth -> Fungal Forest; Black Forest -> Fracture Zones; Swamp -> Blackwater
Deep; Mountain -> Frozen Caverns; Plains -> Fracture Zones; Mistlands -> Great Decay; Ashlands ->
Sulfurous Wastes; Deep North -> Frozen Caverns. The bridge clones only the already-approved
ZoneVegetation row into the detached instance catalog.

The Underworld startup guard now verifies all 22 UnderworldResourceCatalog pickup spawners on their
exact custom biome flags and verifies all compatibility-admitted geode rows.

The map audit found three material issues: the v2 persistence patches were not bootstrapped, the
session bridge could not be rehydrated for the same derived identity after it was retired during a
save, and the asynchronous completion probe treated alpha on blank textures as generated content.
All three are repaired. Async map-generation workers can also resolve Underworld terrain/seed from
the carried instance scope when a map mod caches WorldGenerator.instance.

The plugin now logs `Runtime image 0.0.142 checkpoint UW-142 loaded from '<path>'`. A live run that
does not show that exact checkpoint is not running this candidate.

Acceptance: require the UW-142 line, no fresh-world parent-path exception, no GemstoneSpawner -33
collision, no key-1024 Surface location failure, a 22/22 resource audit, admitted geode audit,
visible independent Underworld map terrain, and independent fog/pins after save/reload.
