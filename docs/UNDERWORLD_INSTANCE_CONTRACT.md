# Underworld Instance Contract Catalog

This catalog is a development gate, not optional guidance. It mirrors the hard-coded
`Magenheim.Core.Underworld.UnderworldInstanceContract`. Any implementation that conflicts
with this catalog is architectural drift and must be corrected before adding features.

## Required

- Dedicated Magenheim-owned instance world-space.
- Dedicated Underworld map; the Surface minimap is not reused as the Underworld map.
- Independent Underworld exploration/cloud state.
- Independent Underworld terrain/chunk authority.
- Independent Underworld biome/environment authority.
- Independent Underworld persistence namespace beneath the parent Magenheim world.
- Deep Gate travel is an instance transition that preserves character/inventory/skills/progression.
- Multiplayer peers join the same authoritative instance identity without player-count lifecycle authority.
- Deep Gate player movement remains a thin adapter over Valheim player/network authority; Magenheim does not own a persistent player-layer or player-transition lifecycle.

## Forbidden

- Treating x=40000, another far-landmass band, or any Surface coordinate region as the Underworld.
- Defining player-facing Underworld geography by projecting or disguising Surface coordinates.
- Extending Surface `WorldGenerator` terrain columns as the final Underworld terrain mechanism.
- Reusing the Surface map as the Underworld map.
- Creating, releasing, activating, or deactivating the Underworld according to player population.
- Requiring the player to select or load a second ordinary Valheim save.
- Persisting Magenheim-owned per-player Surface/Underworld layer truth or reconstructing instance state/residency from player transition records.
- Expanding gate transfer safety into occupancy tracking, player-population reconstruction, or a parallel player lifecycle/recovery system.

## Adapter boundary

Valheim may require singleton services or opaque engine-space backing coordinates. Those details
may exist only in the lowest runtime adapter. They are not gameplay authority and must not leak
into Core identity, map, terrain, biome, exploration, ecology, or persistence semantics.

When implementation convenience conflicts with this contract, the implementation changes.
The contract does not.
