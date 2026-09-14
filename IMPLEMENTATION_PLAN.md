# Implementation Plan

1. Establish pure-core schemas and validators for worldgen, geodes, crystal definitions, refinement, socket metadata, and compatibility policy.
2. Keep compatibility planning read-only: snapshot host registrations, validate desired Magenheim additions, produce diagnostics, then hand only approved additions to adapters.
3. Build a thin Jötunn worldgen adapter. Translate `SpawnArea` to Valheim/Jötunn biome-area flags only after validation. Register Magenheim content once and do not use modification callbacks for foreign content.
4. Add Meadows geode prefab/assets and an Earth-dominant spawn definition under a stable `magenheim.geode.meadows.earth` key.
5. Add server-authoritative geode cracking and Crystal Shaping progression. Inventory consumption and grants must be transactional.
6. Add refinement grades Rough -> Simple -> Crystal -> Advanced -> Master with cumulative failure configuration and explicit failure products.
7. Add sidecar/socket metadata and adaptive item classification. Foreign item definitions remain untouched.
8. Add synchronized gameplay configuration/fingerprints and reject incompatible gameplay-significant definitions before state divergence.
9. Validate in layers: pure tests, adapter compilation, plugin startup, disposable-world generation, multiplayer/server authority, then persistence/removal safety.

## Current slice

The current pass implements steps 1-2 specifically for worldgen area validation and additive compatibility planning. It does not claim Jötunn runtime registration yet.
