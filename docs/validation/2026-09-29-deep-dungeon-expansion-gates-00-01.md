# Deep Dungeon Expansion — DDE-00 / DDE-01 execution record

**Date:** 2026-09-29  
**Authority:** `docs/DEEP_DUNGEON_EXPANSION.md`

## DDE-00 — architecture freeze

Source enforcement is implemented on authoritative `main`.

The new `tools/verify-deep-dungeon-expansion-gate0.py` fails closed if any of the following drift:

- the 1.5x linear donor-room scale floor;
- the 3.5x live donor room-count multiplier floor;
- the prohibition on bespoke ordinary-room injection;
- the five canonical donor mappings;
- any ordinary dungeon leaving `Planned` without deliberate per-family promotion;
- Deep Fracture losing its separate `RuntimeReady` architecture lane;
- generic clone/isolation seams disappearing;
- a legacy ordinary room/location registrar returning to plugin bootstrap;
- retired bespoke ordinary dungeon rebuild/promotion tooling returning to production.

The gate is now invoked by both `build.ps1` and
`tools/rebuild-underworld-production.ps1`.

**Claim boundary:** this establishes source enforcement. DDE-00 is not recorded as executed/cleared
until the normal build actually runs the gate and Core/runtime compilation on the installed
Valheim/Jotunn environment.

## DDE-01 — live donor identity census

`UnderworldVanillaDungeonDonorCensus` and the developer command

`magenheim_underworld donors`

are implemented.

The census operates while all five ordinary dungeons remain `Planned`. It reads live vanilla
`ZoneSystem` and `DungeonDB` data without registering a derivative and writes one durable evidence
file per donor plus an index under the Magenheim BepInEx config validation directory.

Captured evidence includes:

- installed Valheim version;
- every configured entrance identity;
- donor generator object identity;
- donor theme;
- enabled donor-room count and names;
- live min/max room bounds;
- required-room set and minimum;
- generator zone/tile/grid dimensions;
- door prefab/type/chance set;
- location/generator custom-interior-transform state.

**Claim boundary:** DDE-01 tooling is implemented, but no donor family is marked census-passed until
that command succeeds in the installed game and its evidence files are reviewed.

## Promotion state

No ordinary dungeon status is changed by this work. Fungal, Blackwater, Sulfurous, Frozen and Great
Decay remain `Planned`. Deep Fracture remains the bespoke `RuntimeReady` exception.


## Durable-plan authority reconciliation

`docs/DEEP_DUNGEON_EXPANSION.md` is now aligned with the execution vocabulary used by the backlog
and verifiers:

- DDE-00 through DDE-12 are the sequential per-family implementation/acceptance gates;
- DDE-13 is deliberate per-family `Planned -> RuntimeReady` promotion;
- DDE-14 is the final Deep Fracture regression closure.

The plan now includes a dated execution-state table and the single-family candidate-world workflow.
`verify-deep-dungeon-expansion-gate0.py` also reads the durable plan and fails closed if the plan
loses the 1.5x scale rule, 3.5x room-count rule, five donor identities, promotion/regression gates or
the explicit Deep Fracture separation.

Current stop point remains unchanged: no ordinary dungeon has been promoted. DDE-00 still needs a
successful normal build execution in the current game environment, and DDE-01 still needs an
installed-game donor census plus evidence review.
