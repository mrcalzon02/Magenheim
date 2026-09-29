# Underworld creature biome retexture — 2026-09-28

Status: **source implemented on main; live visual acceptance still required**.

## Problem closed by this pass

The 42 non-boss Underworld review creatures were previously created by cloning a Valheim creature,
uniformly scaling the whole prefab, and applying one flat `_Color` tint for the creature's biome.
That preserved useful native locomotion/combat behavior, but visually left obvious green/blue/orange
copies of Bat, Wolf, Seeker, StoneGolem, Serpent and the other donors.

`UnderworldCreatureBiomeVisuals` now owns the surface treatment for those donor-backed creatures.
The donor material is copied per registered prefab/material slot so vanilla shared materials are
never mutated. The clone then receives a generated 128x128 albedo keyed by creature identity, biome,
renderer and material slot. Flat prototype tinting is no longer the creature visual path.

## Biome material language

| Biome | Surface identity |
|---|---|
| Fungal Forest | charcoal/forest tissue, green fungal growth, pale mycelial/spore tracery |
| Blackwater Deep | blue-black wet body, pressure banding, cold cyan bioluminescent glints |
| Sulfurous Wastes | basalt/char body, oxidized brown surfaces, sulfur-yellow fissures |
| Frozen Caverns | dark under-ice body, blue rime, pale frost striation |
| Fracture Zones | dark slate/violet mass, quartz-purple fault seams and facet glints |
| Great Decay | bruised brown-green tissue, rot mottling, pale lesion/marrow inclusions |

## Chassis-aware surface families

The texture generator does not treat every donor as the same object. It classifies current creature
chassis into hide, carapace, scale, stone, membrane and decay families. A Seeker-derived creature
therefore receives segmented carapace patterning, a Serpent/Leech/Neck derivative receives scaled
or banded treatment, and a StoneGolem derivative receives mineral mottling before the biome overlay
is applied. Stable hashing varies each creature and renderer without relying on process-random
`string.GetHashCode()`.

## Preserved behavior

This pass changes visual materials only. It deliberately preserves donor:

- skeleton and animation controller;
- native attacks and attack sockets;
- Character/BaseAI/ZNetView behavior;
- colliders;
- faction and loot;
- existing uniform prototype scale.

The stronger re-body campaign remains authoritative for final silhouettes. Elemental Surtlings
already use authored bodies on donor humanoid rigs, and Deep Fracture creature families already
load Magenheim model assets. Lantern Moth remains the first explicit Underworld donor-silhouette
replacement benchmark.

## Live acceptance

For each biome, spawn at least two creatures sharing a donor chassis with another biome (Wolf,
Seeker, Tick or StoneGolem are useful comparisons) and verify:

1. the creature no longer reads as a flat hue-shift of the vanilla donor;
2. different Underworld biomes show materially different pattern language;
3. two creatures in one biome are not forced to an identical texture;
4. animation, attack sockets and hitboxes remain aligned;
5. vanilla donor creatures elsewhere in the world retain their original materials;
6. no renderer becomes magenta/unlit because its donor shader rejected the replacement texture.

The source path is complete; these six observations are still required before claiming live visual
acceptance.


## Natural spawning enabled

The same 42 identities are no longer console-only. `UnderworldCreaturePrototypeRegistrar` now
registers them through Jötunn `CreatureManager` with one native `SpawnConfig` per creature,
restricted to the creature's canonical reserved Underworld biome flag.

Spawn tuning is intentionally ecological rather than uniform:

- small fauna use shorter intervals, larger local caps and small groups;
- ordinary predators/skirmishers use medium intervals and one-to-two-creature groups;
- aquatic Serpent/Leech derivatives are constrained to submerged/near-water altitude bands;
- Blackwater Neck derivatives are constrained to the shoreline band;
- hero/heavy forms use long intervals, low chances and a local cap of one.

Surface WorldGenerator never returns the reserved 1024-32768 Underworld biome flags, so these rows
do not match Surface terrain. The existing instance-scoped SpawnSystem bridge remains responsible
for evaluating spawning against the active Underworld WorldGenerator/ZoneSystem context.

## Static source verification

Remote read-back after implementation reports:

- 42 roster creature entries;
- six explicit canonical biome mappings;
- `CreatureManager.Instance.AddCreature` registration with a natural spawn config;
- creature-specific signature masks layered over chassis-aware surface families;
- explicit high-contrast material identities for the shared Wolf and StoneGolem chassis families.

This is source verification, not live gameplay acceptance. The live matrix in `TESTING.md` still
must be completed in the installed game before claiming that all 42 natural spawns and material
reads are visually accepted.
