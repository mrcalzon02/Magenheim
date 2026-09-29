# Underworld creature loot and trophy progression — 2026-09-29

Status: **source implemented and remote-read-back verified on main; live death/drop acceptance still required.**

## Audit result

The prior state did not justify claiming universal custom creature trophies or progression loot.
Ordinary Underworld fauna and elemental Surtlings still inherited donor CharacterDrop tables; Deep
Fracture variants likewise inherited donor drops. The six canonical biome-boss trophy identities
exist in Core progression authority, but their ordinary boss encounter/drop runtime remains a
separate boss implementation concern. The Nowhere King already owns a separately registered trophy.

## Implemented non-boss species trophy contract

`UnderworldCreatureLootRuntime` now owns one trophy identity per creature species:

- 42 ordinary Underworld fauna trophies;
- 6 elemental Surtling trophies, one per element and shared across feminine/masculine bodies;
- 13 Deep Fracture chassis trophies, shared across all elemental alignments.

That is 61 non-boss species trophy identities. Trophy visuals deliberately reuse an appropriate
vanilla trophy chassis when one exists, with Magenheim-owned tint/material copies; unavailable
specific donors fall back to TrophyDeer rather than failing the entire creature family. This is an
asset-reuse strategy, not a claim of 61 bespoke trophy meshes.

Ordinary and Surtling trophies use Jötunn CustomItem registration. Deep Fracture trophies use the
same item-registration helper before their CharacterDrop tables are assigned.

## Progression-resource drop contract

Donor creature loot is replaced rather than appended.

Every one of the 22 canonical raw Underworld resources occurs in at least one creature table and
also occurs in at least one existing progression consumer across station, equipment, weapon or
refining catalogs.

Common fauna favor common biological/mineral materials. Scarce station/equipment bottlenecks are
weighted toward rarer/heavier encounters:

- Blackwater Pearl: Blackwater Lamprey, Shoreclaw, Lantern Angler, Abyss Shellback, Deep Hunter,
  and Water Surtlings;
- Emberiron: Fume Wraith, Magma Leaper, Furnace Golem, and Fire Surtlings;
- Rimesilver: Pale Burrower, Rimewing, Glacier Stalker, and Cryolith Guardian;
- Titanbone: Fracture heavy fauna, Earth Surtlings, and multiple Deep Fracture chassis;
- Carrion Amber: Graft Warden, Corpse Orchard, and Umbral Surtlings.

Harvesting remains the broad acquisition route. Creature loot is an alternate route and combat
reward, not a replacement for biome gathering.

## Deep Fracture

All thirteen chassis replace inherited donor loot with Fracture Crystal / Shardstone / Titanbone
tables appropriate to their scale plus one chassis trophy. Elemental alignment does not create a
new trophy identity; Fire Crystal Golem and Frost Crystal Golem, for example, share the Crystal
Golem trophy.

## Boss boundary

The six canonical Underworld biome-boss trophy identities are now also registered as real custom
items directly from the validated Deepstone boss authority: First Bloom, Blackwater Maw, Furnace
Heart, White Silence, Rift Titan and Carrion Crown. Their names are no longer definition-only
strings, so Deepstone inventory resolution has concrete prefabs to consume.

This still does **not** claim the six boss encounters are finished or that those trophy items
currently drop from finished boss deaths. Death/reward binding remains part of each boss encounter
implementation. Nowhere King remains outside this six-biome boss set and already owns
`Magenheim_TrophyNowhereKing`.

## Static verification

Remote source coverage confirms:

- 42/42 ordinary creature names have explicit progression loot;
- 6/6 Surtling elements have explicit progression loot;
- 13/13 Deep Fracture chassis have explicit progression loot;
- 22/22 canonical raw Underworld resources appear in creature loot;
- 22/22 canonical raw resources also appear in at least one progression consumer;
- expected non-boss species trophy count is 61;
- all six canonical biome-boss trophy prefabs are runtime-registered from Deepstone authority;
- ordinary and Surtling CreatureConfig tables replace donor CharacterDrop data;
- Deep Fracture registration replaces its CharacterDrop directly.

## Live acceptance remaining

Complete `TESTING.md -> Underworld creature loot and trophy acceptance`. Static source cannot prove
drop generation, item pickup, item stands, save/reload, multiplayer authority or visual acceptance.