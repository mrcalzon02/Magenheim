# Implementation plan

The supplied v0.1 specification is the design reference. Its full roadmap is larger than this initial implementation. Version 0.0.2 intentionally precedes the playable 0.1 milestone.

1. **Foundation (started):** build plugin; validate static definitions; register localized Crystal Shaping; implement and test pure refinement, weighted cracking and shard recombination. Completed compilation and 118 core assertions, including yield simulations. Runtime startup is pending.
2. **Data and assets:** split the initial compact definition into the planned element/tier/geode catalogs when additional content needs them; add prefab-reference validation, a real Unity asset bundle, asset loader, and server snapshot synchronization. Do not label a local hash as synchronization.
3. **Geological loop:** author Meadows geode and workstation blockouts; verify current mining and world-generation APIs; register intact geode, Earth crystals/shards, workstation and upgrades. During the Earth-only slice, explicitly restrict Meadows results to Earth; restore full biome weighting in the catalog expansion.
4. **Transactions:** wire cracking, refinement and recombination to station UI and server-owned operations; validate resources, proximity and level before consuming input; handle replay, reconnect and full-inventory cases. Grant Valheim skill XP exactly once per accepted attempt.
5. **Equipment:** implement one socket per item using instance metadata; classify weapon, armor and utility; persist and apply Earth effects. Add four Earth staff behaviors and bounded VFX.
6. **Prove the slice:** test saves, death, dropped items, player transfers, host/client and dedicated server. Only expand beyond Earth after the complete vertical slice passes.

No external repository was created or published. The local source is self-contained under `Magenheim/`.
