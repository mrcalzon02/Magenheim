# Backlog

## Immediate

- Run plugin in a disposable BepInEx/Jötunn profile and record version/startup/skill/localization evidence.
- Verify installed Valheim version and API compatibility; the spec's future 1.0-era targets are not assumed to exist locally.
- Build Unity asset project with workstation, Meadows geode, Earth crystal tiers, shards, upgrades and staff.
- Implement real bundle loading and prefab validation; missing required assets must fail before gameplay registration.
- Add authoritative data manifest/payload synchronization and reject mismatched snapshots before gameplay.

## Earth playable slice

- Geode world generation, valid pickaxe-only destruction, exact intact-geode drop.
- Wire the tested cracking planner (one guaranteed roll, independent 35% and 10% bonuses) to real geode inventory and server-generated randomness.
- Transactional inventory adapters for the tested refinement and recombination planners, with attempt XP, full-inventory handling and reconnect/replay protection. Count matching shards by element when invoking recombination.
- Workstation UI and four progression levels.
- Instance socket metadata, unknown-version preservation, classification and three Earth equipment paths.
- Four Earth staves with distinct spell behaviors.
- Save/load, transfer, duplication resistance and dedicated-server tests.

## Deferred

Remaining elements, runes, Galdr, rituals, wards, travel, ships, lighting, resonance, totems, spirits, Fate and boss artifacts follow the supplied roadmap after the Earth slice passes.
