# Deep Dungeon Expansion — all-family runtime admission

**Date:** 2026-09-30  
**Program:** Magenheim Underworld Deep Dungeon Expansion  
**Authority:** `docs/DEEP_DUNGEON_EXPANSION.md`

## Runtime state

The five ordinary expanded-vanilla dungeon families are now RuntimeReady together:

- Fungal Forest / Rootwarren identity -> Burial Chambers donor / `DG_ForestCrypt`;
- Blackwater Deep / Drowned Vaults identity -> Sunken Crypt donor / `DG_SunkenCrypt`;
- Sulfurous Wastes / Cinderworks identity -> Infested Mines donor / `DG_DvergrTown`;
- Frozen Caverns / Rime Sepulcher identity -> Frost Caves donor / `DG_Cave`;
- Great Decay / Carrion Catacombs identity -> Winding Tunnels donor / `DG_Hole`.

Deep Fracture remains RuntimeReady as the separate bespoke architecture lane.

`UnderworldDungeonCatalog.Validate()` now requires all six dungeon families to remain RuntimeReady.
`UnderworldVanillaDungeonRegistrar` requires all five ordinary donor profiles together and treats a
partial ordinary-dungeon set as an error.

## Retired one-at-a-time path

The former developer-only one-family candidate admission path no longer controls runtime or
worldgen. The old BepInEx keys remain bound only for compatibility with existing profiles:

- `EnablePlannedCandidateWorldgen`;
- `CandidateDungeon`.

They are inert. The compatibility policy always reports disabled and worldgen admission is
RuntimeReady-only.

The detached Underworld worldgen bridge now requires every dungeon catalog row directly. The native
placement audit checks every family unconditionally. There is no supported one-family production
mode.

## Expanded-vanilla runtime contract

The ordinary dungeon registrar continues to enforce:

- private clones of vanilla donor rooms, doors and entrances;
- no mutation of Surface donor content;
- connection-safe >=1.5x room scaling under `Magenheim_DDE_ScaleRoot`;
- unscaled `Room` root;
- matching expanded `RoomConnection.localPosition` values and `Room.m_size`;
- >=3.5x room-count targets derived from each live donor generator;
- expanded legal generator packing volume;
- required-room remapping;
- donor structural mechanic retention;
- Surface lore/progression stripping;
- owning-biome Magenheim creature rebinding;
- owning-biome Magenheim reward/resource rebinding;
- exact owning Underworld biome placement;
- donor-native interior/return mechanics.

Deep Fracture remains outside this generic registrar.

## Source consistency readback

Authoritative GitHub readback confirmed:

- all five ordinary catalog definitions use `Ready(...)`;
- the catalog has an all-six RuntimeReady validation guard;
- the registrar has a five-of-five ordinary-family admission guard;
- candidate admission is inert;
- worldgen contains no candidate-gated branch;
- native placement contains no candidate-gated branch;
- DDE source/build verifiers expect all-family admission;
- the durable plan no longer contains a candidate-world workflow;
- TESTING no longer instructs enabling only one candidate;
- BACKLOG records Deep Dungeon source admission complete.

## Claim boundary

This is a **remote source/runtime-admission completion record**, not installed-Valheim play
acceptance.

The current execution container cannot resolve `github.com`, so the local PowerShell/.NET build
and installed-game validation cannot be executed from this session. No local compile, installed
Valheim launch, multiplayer run, or live dungeon generation is claimed here.

The next live run should load all five ordinary donor families together. Any failure is a runtime
defect to repair in the authoritative generic donor path, not a reason to restore one-at-a-time
candidate gating.
