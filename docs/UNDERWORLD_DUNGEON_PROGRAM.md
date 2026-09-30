# Underworld Biome Dungeon Program

> **Durable implementation authority:** [`DEEP_DUNGEON_EXPANSION.md`](DEEP_DUNGEON_EXPANSION.md) owns the detailed rollout order, gates, donor-specific requirements, telemetry, evidence, risk register and RuntimeReady promotion contract. This file remains the concise architecture summary.

**Status:** ACTIVE — Deep Fracture remains bespoke; the five ordinary biome dungeons are now an expanded-vanilla reuse program.

**Detailed implementation plan:** [`DEEP_DUNGEON_EXPANSION.md`](DEEP_DUNGEON_EXPANSION.md) is the durable gate-by-gate authority for completing and promoting the five ordinary donor dungeons.

The Underworld is a world-scale realm. Its ordinary dungeons should feel familiar enough that a
Valheim player recognizes the construction grammar immediately, but large and content-rich enough
that entering one is a worthwhile expedition rather than a recolored copy of a Surface dungeon.

## Governing architecture rule

There are now two deliberately separate dungeon lanes.

### Deep Fracture

Deep Fracture remains Magenheim-owned architecture: its bespoke twenty-district expedition, custom
rooms, exact-plan traversal, encounter authority, passage assembly, return portal and persistence
remain intact. None of the vanilla-reuse rules below replace or dilute it.

### Five ordinary Underworld dungeons

The other five biome dungeons reuse vanilla Valheim architecture directly:

| Underworld biome | Vanilla donor family | Generator | Entrance family |
|---|---|---|---|
| Fungal Forest | Burial Chambers | `DG_ForestCrypt` | `Crypt2`, `Crypt3`, `Crypt4` |
| Blackwater Deep | Sunken Crypts | `DG_SunkenCrypt` | `SunkenCrypt4` |
| Sulfurous Wastes | Infested Mines | `DG_DvergrTown` | `Mistlands_DvergrTownEntrance1/2` |
| Frozen Caverns | Frost Caves | `DG_Cave` | `MountainCave02` |
| Great Decay | Winding Tunnels | `DG_Hole` | `TheHole01` |

The intended player read is:

> This is recognizably the vanilla dungeon I know — but the spaces are bigger, it keeps going,
> and everything living or worth harvesting inside belongs to the Underworld.

These dungeons do **not** inject Rootwarren/Drowned Vault/Cinderworks/Rime Sepulcher/Carrion
Catacombs bespoke room geometry into the donor tilesets. The previously authored room-kit source is
preserved in the repository for reference or future reuse, but it is no longer ordinary-dungeon
runtime authority and is no longer a production-forge requirement.

## Scale and exploration floor

`UnderworldVanillaDungeonReuseCatalog` owns two hard minimums:

- **1.5x linear room scale.** Every cloned donor room is enlarged to at least 150% of vanilla size.
  The room transform, connection spacing and `Room.m_size` placement bounds must agree so the
  generator sees the true larger footprint rather than allowing visually enlarged rooms to overlap.
- **3.5x donor room-count target.** The cloned donor generator derives its minimum and maximum room
  counts from the live vanilla `DungeonGenerator.m_minRooms/m_maxRooms` values and expands both by
  at least 3.5x. Counts are not permanently hard-coded because Valheim may rebalance its donors.

The cloned generator must also enlarge its legal dungeon zone enough to contain the combination of
more rooms and larger footprints. Expansion is derived from the donor zone rather than moving the
interior to arbitrary world coordinates.

This is a floor, not an instruction that every seed be exactly identical in length. Vanilla
branching, dead ends, required rooms, loops, doors and donor-specific generation behavior should
continue to produce run-to-run variation.

## What is retained from vanilla

Ordinary Underworld dungeons deliberately keep the donor's expensive, proven infrastructure:

- entrance silhouette and interaction grammar;
- room/tile vocabulary;
- `RoomConnection` layout and connection types;
- `DungeonGenerator` algorithm;
- doors, gates, barricades and structural route mechanics where compatible;
- collision, pathing and ordinary room construction behavior;
- donor-specific verticality and branching character.

Vanilla assets are never mutated. Magenheim clones the location, room and any required door
prefabs into owned identities before applying scale or population changes.

## What is replaced

The dungeon may look structurally familiar, but its ecology and rewards are Underworld-owned.

- vanilla creature spawners are rebound to the owning biome's Magenheim creatures;
- vanilla spawn-area creature tables are replaced by the same biome roster;
- vanilla treasure/resource drop tables are replaced with canonical Underworld resources;
- ordinary pickables are rebound to biome resources where the donor position is suitable;
- vanilla quest/boss/lore content that would imply the Surface dungeon's original story is removed
  or disabled on the clone;
- the Surface donor remains completely untouched elsewhere in the world.

The five biome rosters and resource catalogs already own the creature/material identities. Dungeon
population therefore consumes those authorities instead of inventing a parallel dungeon-only set.

Rare materials should appear deeper or behind stronger encounter pressure; common materials may be
present earlier. A 3.5x dungeon must reward exploration rather than multiplying empty hallways.

## Biome-specific gameplay identity

### Fungal Forest — expanded Burial Chambers

The stone crypt grammar remains obvious, but the enlarged chambers are inhabited by Fungal fauna
and supply Worldroot Timber, Glowcap Flesh, Spire Fibre and Understone. Existing crypt turns,
stairs, burial niches and door logic provide the recognizable skeleton; fungal ecology provides the
new reason to explore it.

### Blackwater Deep — expanded Sunken Crypts

Sunken Crypt architecture is retained because flooded masonry, gated rooms and blocked routes
already fit Blackwater. Vanilla Draugr/Blob loot is removed. Blackwater creatures and Flowstone,
Pale Fibre, Deep Salt and Blackwater Pearl become the reward language.

### Sulfurous Wastes — expanded Infested Mines

The large masonry/vertical grammar of Infested Mines becomes a much longer Sulfurous expedition.
Seekers/Dvergr and Mistlands loot are removed from the clone and replaced by Sulfurous fauna,
Slagstone, Sulfur, Charred Timber and Emberiron.

### Frozen Caverns — expanded Frost Caves

This is intentionally the least visually alien donor. The player should recognize Frost Cave
construction immediately, but at 1.5x room scale and at least 3.5x room count it becomes a major
Frozen Caverns destination. Cultists/bats and Mountain loot are replaced by the Frozen roster,
Rimewood, Clear Ice and Rimesilver.

### Great Decay — expanded Winding Tunnels

The Hole/Winding Tunnel tileset supplies a long subterranean donor grammar without importing a
second bespoke architecture family. It is populated by Great Decay fauna and Rotwood, Decay Spore,
Carrion Amber and Bone Gravel.

## Admission and safety

The five ordinary catalog entries remain `Planned` until the generic vanilla-reuse runtime path has
all of the following:

1. read the live donor generator/room data without mutating vanilla assets;
2. clone the donor location and room family into Magenheim-owned identities;
3. enlarge room geometry and placement bounds coherently to at least 1.5x;
4. expand live donor min/max room counts by at least 3.5x and enlarge the legal generation zone;
5. replace vanilla creature/resource/loot population with the owning Underworld biome;
6. register only in the matching native Underworld biome;
7. preserve entrance/exit behavior, save/reload and generated-room persistence;
8. compile against current Valheim/Jötunn and pass host/client runtime acceptance.

A planned dungeon must seed **no entrance**. This remains fail-closed.

## Production pipeline effect

The expensive Underworld Blender production forge no longer rebuilds, promotes or reviews the five
legacy bespoke ordinary room families. That work is preserved but is not required to ship ordinary
Underworld dungeons.

Deep Fracture remains the bespoke custom-dungeon lane and continues to use Magenheim-authored
geometry where its design requires it.
