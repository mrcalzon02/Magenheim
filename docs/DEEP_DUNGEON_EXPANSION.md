# Deep Dungeon Expansion

**Status:** Durable implementation plan  
**Authority:** Magenheim Underworld ordinary-dungeon program  
**Scope:** Fungal Forest, Blackwater Deep, Sulfurous Wastes, Frozen Caverns, Great Decay  
**Explicit exclusion:** Deep Fracture remains the bespoke Magenheim dungeon architecture lane

---

## 1. Purpose

The ordinary Underworld dungeons are not intended to be lightly reskinned copies of vanilla Valheim
dungeons, and they are not intended to become a second bespoke room-kit program competing with Deep
Fracture.

The intended experience is:

> “I recognize this dungeon. This is clearly the Burial Chamber / Sunken Crypt / Infested Mine /
> Frost Cave / Winding Tunnel construction grammar I already know — but holy shit, this thing is
> enormous, the rooms are bigger, it keeps going, the enemies are different, the resources are
> different, and there is now a reason to explore every branch.”

Vanilla supplies the proven entrance, room vocabulary, connection grammar, doors, collision,
pathing assumptions and procedural construction mechanics. Magenheim expands that chassis into a
substantially larger Underworld expedition and replaces the gameplay ecology inside it.

This document is the durable execution plan for finishing that system.

---

## 2. Non-negotiable architecture boundary

There are two distinct Underworld dungeon programs.

### 2.1 Deep Fracture

Deep Fracture remains fully Magenheim-owned architecture.

It retains:

- its bespoke custom room set;
- exact twenty-district expedition structure;
- Magenheim-authored traversal;
- custom encounters;
- custom persistence/return behavior;
- Fracture-specific architecture and visual language.

Nothing in the expanded-vanilla program may absorb, replace, simplify or genericize Deep Fracture.

### 2.2 Ordinary Underworld dungeons

The other five Underworld biome dungeons reuse vanilla Valheim dungeon families.

They must **not** inject the old bespoke Rootwarren, Drowned Vault, Cinderworks, Rime Sepulcher or
Carrion Catacombs room kits into the vanilla donor themes.

Those legacy authored assets are preserved for reference or future use elsewhere, but they are not
ordinary-dungeon runtime authority and are not production-forge requirements.

The runtime architecture is:

**vanilla entrance + vanilla room grammar + vanilla dungeon generator mechanics**
  
expanded by:

**1.5x linear room scale + at least 3.5x donor room-count target + Underworld enemies + Underworld resources + Underworld progression**

---

## 3. Canonical donor map

The current donor assignment is:

| Underworld biome | Magenheim dungeon identity | Vanilla donor | Generator | Donor entrance(s) |
|---|---|---|---|---|
| Fungal Forest | Rootwarren | Burial Chambers | `DG_ForestCrypt` | `Crypt2`, `Crypt3`, `Crypt4` |
| Blackwater Deep | Drowned Vaults | Sunken Crypts | `DG_SunkenCrypt` | `SunkenCrypt4` |
| Sulfurous Wastes | Cinderworks | Infested Mines | `DG_DvergrTown` | `Mistlands_DvergrTownEntrance1/2` |
| Frozen Caverns | Rime Sepulcher | Frost Caves | `DG_Cave` | `MountainCave02` |
| Great Decay | Carrion Catacombs | Winding Tunnels | `DG_Hole` | `TheHole01` |

The display names remain Magenheim identities. The structural language remains recognizably vanilla.

A donor mapping may change only if live verification proves the donor prefab/theme/generator is not
appropriate or not safely reusable. It must not change merely because a bespoke replacement would
look more novel.

---

## 4. Hard scale requirements

### 4.1 Minimum room scale: 1.5x linear

Every cloned vanilla room used in an ordinary Underworld dungeon must be at least **1.5 times the
vanilla room's linear size**.

This means all of the following must agree:

- cloned room root transform;
- visible geometry;
- `RoomConnection` world spacing;
- door geometry;
- `Room.m_size` generator collision/packing bounds;
- generator tile/grid spacing where applicable;
- vertical clearances;
- AI navigation through doors, stairs, drops and ledges.

Scaling only the visible transform is insufficient. The dungeon generator must understand the
larger physical footprint or rooms can overlap even when the art looks correct.

### 4.2 Minimum exploration size: 3.5x live donor room budget

The Magenheim clone must derive its room-count target from the **live vanilla donor generator**.

For donor values:

- `m_minRooms = X`
- `m_maxRooms = Y`

the Magenheim values must be at least:

- `ceil(X * 3.5)`
- `ceil(Y * 3.5)`

The project must not freeze historical vanilla values into permanent constants. If Valheim changes
the donor in a later release, the expansion should continue to derive from the live donor.

### 4.3 Generator packing volume

A dungeon with 1.5x rooms and 3.5x room count needs substantially more legal generation volume.

The current source derives the linear generator expansion as:

`roomScale * sqrt(roomCountMultiplier)`

At the current floors this is approximately:

`1.5 * sqrt(3.5) ~= 2.81x`

The exact formula may be tuned if live generation demonstrates truncation or excessive empty
packing space, but the legal generation volume must always be derived from donor scale and target
room count rather than set to an arbitrary fixed number.

---

## 5. Gameplay replacement contract

The ordinary dungeon should look structurally familiar but must play as an Underworld location.

### 5.1 Enemies

Vanilla dungeon creatures must not survive in the Magenheim clone.

Existing donor population sockets should be reused wherever practical:

- `CreatureSpawner`;
- `SpawnArea`;
- compatible room-local encounter sockets.

These sockets must be rebound to the owning Underworld biome roster.

Examples:

- Fungal dungeon -> Fungal Forest creature roster;
- Blackwater dungeon -> Blackwater Deep roster;
- Sulfur dungeon -> Sulfurous Wastes roster;
- Frozen dungeon -> Frozen Caverns roster;
- Decay dungeon -> Great Decay roster.

The reuse objective is to preserve proven room encounter placement while replacing the ecology.

### 5.2 Resources and loot

Vanilla dungeon progression rewards must not leak into the Underworld clones.

At minimum, the following donor reward paths must be audited and rebound:

- containers/chests;
- pickables;
- `MineRock`;
- `MineRock5`;
- `DropOnDestroyed`;
- breakable resource barriers;
- any donor-specific reward fixture that still yields Surface progression.

The replacement items must come from the canonical Underworld biome resource catalog.

Rare materials must remain rare. A dungeon that is 3.5x larger must not accidentally turn one
rare-resource chance into 3.5 times the intended economy throughput.

### 5.3 Lore and progression leakage

Vanilla donor content that implies the original Surface dungeon's story or progression must be
removed from the clone where inappropriate.

Examples include:

- Vegvisirs;
- donor runestones;
- donor boss/quest triggers;
- Surface progression keys;
- donor-specific progression interactables.

The goal is structural familiarity without narrative contradiction.

---

## 6. Current implementation state

The following foundation already exists in source.

### 6.1 Core donor authority

`UnderworldVanillaDungeonReuseCatalog` currently defines:

- the five donor mappings;
- minimum 1.5x room scale;
- minimum 3.5x room-count multiplier;
- mandatory vanilla enemy replacement;
- mandatory vanilla loot replacement;
- mandatory biome-resource use;
- explicit prohibition on bespoke Magenheim room injection.

### 6.2 Generic runtime registrar

`UnderworldVanillaDungeonRegistrar` currently contains source implementations for:

- private donor-room cloning from `DungeonDB`;
- private Magenheim dungeon themes;
- 1.5x room root scaling;
- corresponding `Room.m_size` expansion;
- live donor min/max room-count expansion;
- enlarged generator legal zone;
- enlarged tile width;
- required-room name remapping;
- cloned/scaled donor doors;
- creature-spawner rebinding;
- spawn-area rebinding;
- container resource rebinding;
- pickable resource rebinding;
- mineable/destructible reward rebinding;
- donor Vegvisir/runestone removal;
- cloned location registration in the owning Underworld biome.

### 6.3 Production pipeline cleanup

The ordinary bespoke dungeon rebuild/promotion/review path has been removed from the active
Underworld production forge.

The old files remain preserved, but future production runs should not spend Blender capacity
regenerating those five retired bespoke room families.

### 6.4 Fail-closed runtime status

All five ordinary dungeon definitions remain:

`UnderworldDungeonStatus.Planned`

Therefore the generic registrar currently seeds **none of them**.

Deep Fracture remains:

`UnderworldDungeonStatus.RuntimeReady`

This is intentional. No ordinary donor dungeon should become visible in worldgen until its gates
below have been passed.

---

# 7. Implementation gates

The following gates are sequential. A later gate does not compensate for an earlier failure.

A dungeon may be promoted individually; all five do not need to become RuntimeReady at once.

The gate identifiers in this document are authoritative and intentionally match `BACKLOG.md`,
`TESTING.md`, validation records and fail-closed source verifiers.

### Current execution state — 2026-09-29

| Gate | Current state | What exists now | What still clears the gate |
|---|---|---|---|
| DDE-00 | Source-enforced; execution evidence pending | `verify-deep-dungeon-expansion-gate0.py` is wired into normal build and production preflight | Successful execution in the current build/Valheim environment |
| DDE-01 | Tooling implemented; live evidence pending | `magenheim_underworld donors` captures a live donor census for all five families | Installed-game census succeeds and evidence is reviewed |
| DDE-02..DDE-10 | Static prerequisites implemented; live evidence pending | `verify-deep-dungeon-expansion-source-prerequisites.py` guards clone/scale/count/ecology/reward/worldgen/return source seams | Each donor passes its corresponding installed-game gates |
| DDE-11 | Open | Existing Valheim persistence/network substrate is reused | Save/reload, reconnect and two-peer dungeon cases pass |
| DDE-12 | Open | Balance/performance acceptance criteria are defined here and in `TESTING.md` | Economy and performance measurements are acceptable |
| DDE-13 | Blocked | Promotion remains explicit and per-family | A single donor has DDE-00..DDE-12 evidence and its catalog row is deliberately promoted |
| DDE-14 | Blocked | Deep Fracture is still separate and RuntimeReady | Final regression proves ordinary reuse did not alter Deep Fracture |

All five ordinary catalog entries remain `Planned`. This table must not be interpreted as runtime
acceptance merely because source prerequisites exist.

### Candidate-world workflow

Live validation of a Planned donor uses the developer-only
`Development.DeepDungeonExpansion.EnablePlannedCandidateWorldgen` policy. It is disabled by
default, admits exactly one Planned ordinary dungeon at a time, defaults to Fungal Forest for the
first validation lane, and is included in gameplay peer authority so multiplayer cannot silently
disagree about candidate admission.

Candidate admission is a test exception only. It does not change a catalog row to `RuntimeReady`
and must be disabled again outside disposable DDE validation worlds.

---

## DDE-00 — Architecture freeze

**Objective:** prevent scope drift.

Required:

- Deep Fracture remains explicitly separate.
- The five ordinary dungeons remain vanilla-donor architecture.
- No bespoke ordinary room injection.
- Scale floor remains >=1.5x.
- room-count floor remains >=3.5x.
- vanilla assets are cloned, never mutated.

Evidence:

- Core catalog validation passes.
- production-readiness verifier confirms retired bespoke ordinary dungeon paths are inactive.
- no runtime bootstrap references legacy ordinary room registrars.

**Failure means stop.**

---

## DDE-01 — Live donor identity verification

**Objective:** prove every donor name/theme/generator used in Core authority matches the currently
installed Valheim build.

For each donor, verify in the installed game:

- entrance prefab resolves;
- expected `DungeonGenerator` exists;
- generator object identity matches;
- expected `Room.Theme` resolves;
- `DungeonDB` returns at least one enabled donor room;
- donor min/max room counts are sane;
- required-room list is known;
- door types are known;
- custom interior-transform behavior is understood.

Required donor set:

- `DG_ForestCrypt`;
- `DG_SunkenCrypt`;
- `DG_DvergrTown`;
- `DG_Cave`;
- `DG_Hole`.

Record a donor census artifact for each family.

**Do not promote a dungeon whose donor census is incomplete.**

---

## DDE-02 — Vanilla isolation

**Objective:** prove Magenheim cannot damage the Surface donor.

Required:

- every room is cloned into a Magenheim-owned prefab;
- every door requiring scale changes is cloned;
- cloned location has a Magenheim-owned identity;
- private theme contains only Magenheim-owned room clones;
- vanilla `DungeonDB` entries remain untouched;
- vanilla entrance/location remains untouched;
- vanilla creature/loot behavior remains untouched.

Live check:

Generate both the vanilla donor and the Underworld derivative in the same test world/session and
compare them directly.

**Any mutation of vanilla content blocks promotion.**

---

## DDE-03 — 1.5x physical-scale integrity

**Objective:** make the rooms genuinely larger without breaking procedural construction.

Required:

- room root scale >=1.5x;
- `Room.m_size` expands coherently;
- `RoomConnection` spacing follows the enlarged room;
- door clones fit enlarged doorways;
- generator tile/grid assumptions remain valid;
- no overlapping generated rooms;
- no broken connection seams;
- no inaccessible stairs or ledges;
- no player-height traps;
- no AI navigation failures caused by scale.

Test representative:

- straight corridor;
- corner;
- junction;
- stairs;
- vertical room;
- large room;
- dead end;
- locked/gated connection;
- destructible barrier room where donor supports it.

**Scale that looks correct but breaks generation/pathing does not pass.**

---

## DDE-04 — 3.5x generation-volume integrity

**Objective:** make the dungeon meaningfully larger rather than merely stretching rooms.

Required:

- donor live `m_minRooms` and `m_maxRooms` recorded;
- clone min/max >= ceil(donor * 3.5);
- legal zone enlarged sufficiently;
- room placement success remains acceptable;
- required rooms still appear;
- branching/dead-end behavior remains recognizable;
- no pathological generation loops;
- no severe truncation;
- no huge empty padding regions created by over-expansion.

Generate multiple seeds.

The desired result is **a familiar procedural grammar operating over a much longer expedition**.

---

## DDE-05 — Required-room and door correctness

**Objective:** ensure the private theme remains functionally complete.

Required:

- donor required-room names remap to cloned room identities;
- `m_minRequiredRooms` remains satisfiable;
- required rooms never silently disappear because the private theme changed names;
- every donor door reference points to an appropriate clone when scale requires it;
- door interaction/collision works at 1.5x;
- no unscaled vanilla door appears inside a scaled room opening.

This gate must be repeated for every donor because required-room and door behavior differs by family.

---

## DDE-06 — Creature ecology replacement

**Objective:** make the dungeon belong to the Underworld biome.

Required:

- no vanilla donor dungeon creature spawns;
- every `CreatureSpawner` resolves a creature from the owning biome;
- every `SpawnArea` contains only owning-biome creatures;
- encounter density is tuned for the larger rooms and longer dungeon;
- creature combat-role balance remains intact;
- large/heavy creatures have enough room to telegraph attacks;
- fast creatures do not become oppressive merely because the dungeon contains more encounter
  sockets;
- multiplayer creature authority remains correct.

Do not simply fill every possible donor socket. A 3.5x dungeon needs pacing, not constant combat.

---

## DDE-07 — Resource and reward replacement

**Objective:** turn exploration into meaningful biome progression.

Required:

- donor chests no longer yield vanilla dungeon progression;
- donor pickables yield biome resources;
- mineable donor fixtures yield biome resources;
- destructible donor fixtures yield biome resources;
- rare resources remain scarcity-controlled;
- common resources provide useful exploration value;
- creature drops continue feeding the same biome economy;
- reward density does not make exterior gathering obsolete.

A recommended distribution model is:

- outer/early branches -> common biome materials;
- deeper branches -> improved material density;
- dangerous dead ends -> uncommon/rare resource chance;
- rooms with heavy/apex encounters -> strongest alternate resource opportunity;
- no guaranteed rare-resource flood simply because the dungeon is long.

---

## DDE-08 — Donor mechanics retention review

**Objective:** preserve useful vanilla dungeon mechanics rather than stripping them blindly.

Per donor, determine which structural mechanics should remain.

Examples:

### Burial Chambers

Potentially retain:

- doors;
- narrow crypt routing;
- burial niches;
- stairs;
- branching chamber logic.

### Sunken Crypts

Potentially retain:

- gated room grammar;
- wet/blocked passage feel;
- destructible obstruction behavior, but with Underworld rewards.

### Infested Mines

Potentially retain:

- vertical transitions;
- barricades;
- secret/hidden route behavior;
- larger masonry chambers;
- complex branching.

### Frost Caves

Potentially retain:

- vertical shafts;
- breakable ice;
- falling/ice hazards if technically safe;
- cave-to-worked-room transitions.

### Winding Tunnels

Potentially retain:

- long irregular subterranean route grammar;
- donor tunnel branching;
- donor vertical changes.

Mechanics that leak donor lore/progression should be removed even when mechanically functional.

---

## DDE-09 — Underworld worldgen confinement

**Objective:** guarantee the derivative exists only where intended.

Required:

- each location uses exactly its owning custom Underworld biome flag;
- no Surface worldgen registration;
- no cross-Underworld-biome leakage;
- no location generated while status is Planned;
- expected quantity and minimum spacing enforced;
- entrances place properly on Underworld terrain;
- entrance exterior footprint does not float/bury excessively;
- custom interior transform remains paired with the correct entrance.

Run fresh-world generation tests, not only console placement.

---

## DDE-10 — Entrance / interior / return integrity

**Objective:** preserve the donor's proven dungeon travel mechanics while keeping world identity
correct.

Required:

- entry from owning Underworld exterior enters the correct generated interior;
- exit returns to that exact entrance;
- exit never returns to Surface;
- exit never returns to the Conclave;
- exit never uses the Deep Gate;
- multiple copies of the same dungeon family do not cross-link;
- save/reload inside preserves the correct owning entrance;
- host/client agree on interior ownership and return target.

This is especially important for donors using custom interior transforms.

---

## DDE-11 — Persistence and multiplayer

**Objective:** prove the enlarged derivative behaves like a real Valheim dungeon over time.

Required live cases:

- single-player generation;
- save/reload outside;
- save/reload inside;
- opened chest persistence;
- harvested fixture persistence;
- killed enemy/spawner behavior;
- host + one client;
- host outside / client inside;
- both players inside;
- reconnect while inside;
- return after reconnect;
- no duplicated resource population;
- no duplicated procedural room generation;
- no stale donor references after reconnect.

---

## DDE-12 — Economy, progression, performance and stability

**Objective:** prove that the larger dungeon is worth exploring, does not replace the rest of the
biome economy, and remains practical at the expanded scale.

### Economy and progression measurements

Measure:

- common resources per completed dungeon;
- rare resources per completed dungeon;
- creature-drop contribution;
- chest contribution;
- mineable contribution;
- average encounter count;
- average dungeon completion time;
- resource gain per minute versus exterior biome gathering;
- repair/food/consumable cost;
- death/recovery risk.

Desired result:

A full expanded dungeon should feel **materially rewarding**, but exterior exploration, gathering,
geodes, stations and ordinary biome combat must remain useful.

A 3.5x dungeon must not imply a 3.5x multiplier to every rare reward.

### Performance and stability measurements

Measure:

- generation time;
- peak frame hitch on generation;
- memory cost;
- instantiated room count;
- active creature count;
- network traffic with two peers;
- pathfinding failures;
- light count / shadow cost;
- physics/collider cost.

If performance is unacceptable, reduce active ecology density, lighting cost or donor-clone runtime
overhead before reducing the exploration-size requirement. The first response must not be to shrink
the dungeon back toward vanilla size.

**DDE-12 passes only when both the economy and the performance sides pass.**

---

## DDE-13 — Per-family promotion

A dungeon may move from `Planned` to `RuntimeReady` only after its donor-specific gates pass.

Recommended order:

1. **Fungal Forest / Burial Chambers**  
   Lowest-risk first proof of the generic clone pipeline.
2. **Frozen Caverns / Frost Caves**  
   Strong visual fit and useful vertical/pathing test.
3. **Blackwater Deep / Sunken Crypts**  
   Tests obstruction/resource conversion and wet crypt grammar.
4. **Sulfurous Wastes / Infested Mines**  
   Highest ordinary complexity: verticality, hidden routes, larger room vocabulary.
5. **Great Decay / Winding Tunnels**  
   Newer donor family and therefore last until its current-game behavior is fully censused.

Promotion changes only the individual catalog entry's status after evidence exists.

Do **not** mark all five RuntimeReady merely because one donor family works.

---

## DDE-14 — Final regression against Deep Fracture

Before the ordinary dungeon program is considered complete:

- generate and complete at least one Deep Fracture;
- verify its twenty-district bespoke architecture is unchanged;
- verify its custom creature/encounter behavior is unchanged;
- verify its placement is unchanged;
- verify its resources/loot are unchanged;
- verify the generic vanilla-reuse registrar never sees Deep Fracture;
- verify no production cleanup removed Deep Fracture assets or tooling.

This is the final architectural boundary gate.

---

# 8. Per-biome content direction

## 8.1 Fungal Forest — Rootwarren / Burial Chamber donor

Player read:

> “Black Forest crypt, but enormous and colonized by the Underworld.”

Keep the crypt's recognizable stone grammar.

Replace population with Fungal fauna.

Primary resource language:

- Worldroot Timber;
- Glowcap Flesh;
- Spire Fibre;
- Understone.

Suggested exploration pressure:

- frequent small fauna in early branches;
- stalkers deeper in;
- bruiser/apex encounters guarding high-value dead ends;
- fungal-resource abundance without turning rare progression into guaranteed loot.

This should be the first donor promoted because its architecture is comparatively simple and makes
an ideal validation chassis for the generic system.

---

## 8.2 Blackwater Deep — Drowned Vaults / Sunken Crypt donor

Player read:

> “The Sunken Crypt kept going far past where a normal crypt should end.”

Primary resources:

- Blackwater Flowstone;
- Pale Fibre;
- Deep Salt;
- Blackwater Pearl.

Preserve the donor's damp masonry, gates and obstructed-route language.

Convert donor obstruction/resource mechanics rather than simply deleting them where possible.

Pearls should be concentrated behind meaningful combat/exploration pressure rather than scattered
through every converted loot socket.

---

## 8.3 Sulfurous Wastes — Cinderworks / Infested Mine donor

Player read:

> “An Infested Mine expanded into a major subterranean industrial ruin.”

Primary resources:

- Slagstone;
- Sulfur;
- Charred Timber;
- Emberiron.

This donor has the greatest ordinary procedural complexity and should not be the first implementation
target.

Pay particular attention to:

- vertical routing;
- door/barricade scaling;
- hidden rooms;
- required rooms;
- large-room packing;
- AI pathing in 1.5x vertical spaces;
- encounter pacing across a very large room count.

Emberiron must remain a meaningful reward, not a guaranteed consequence of dungeon length.

---

## 8.4 Frozen Caverns — Rime Sepulcher / Frost Cave donor

Player read:

> “A Frost Cave that has become a real expedition.”

Primary resources:

- Rimewood;
- Clear Ice;
- Rimesilver.

This donor should retain much of its natural cave identity.

The larger scale should make vertical chambers and frozen spaces feel genuinely monumental.

Audit carefully:

- ice hazards;
- vertical drops;
- narrow donor routes after scaling;
- cave collision;
- flying creature behavior;
- Rimesilver scarcity.

---

## 8.5 Great Decay — Carrion Catacombs / Winding Tunnel donor

Player read:

> “A familiar tunnel grammar continuing far beyond any sane natural depth.”

Primary resources:

- Rotwood;
- Decay Spore;
- Carrion Amber;
- Bone Gravel.

The long irregular donor grammar is useful specifically because this biome should feel like a
deepening, diseased subterranean network.

Carrion Amber should be tied to deeper/high-risk branches, not routine tunnel clutter.

Because Winding Tunnels are newer donor content, this family should remain last in promotion order
until all donor identities and generation behavior are verified against the installed game.

---

# 9. Runtime implementation responsibilities

The generic runtime should ultimately own only responsibilities common to all five families:

- donor discovery;
- room cloning;
- private theme registration;
- room scaling;
- placement-bound scaling;
- door cloning/scaling;
- room-count expansion;
- generation-zone expansion;
- required-room remapping;
- biome confinement;
- population replacement;
- resource replacement;
- donor-lore stripping;
- lifecycle cleanup;
- validation logging.

Donor-specific exceptions should be explicit small adapters, not five separate duplicate registrars.

If a donor requires special treatment, add a narrowly scoped profile/handler keyed by donor identity
rather than restoring five parallel bespoke implementations.

---

# 10. Logging and diagnostics required before promotion

Each generated family should log enough information to diagnose failures without decompiling the
world save.

At registration time log:

- donor entrance chosen;
- donor generator identity;
- donor theme;
- number of donor rooms discovered;
- number of room clones registered;
- room scale;
- donor min/max room count;
- expanded min/max;
- donor zone size;
- expanded zone size;
- donor required rooms;
- remapped required rooms;
- number of cloned door types;
- number of creature sockets rebound;
- number of resource fixtures rebound.

At generation/test time, where practical, capture:

- final placed room count;
- rejected placement count;
- required-room success;
- generated branch depth;
- encounter socket count;
- resource fixture count.

These diagnostics are part of the admission evidence, not optional debug noise.

---

# 11. Evidence package for each promoted dungeon

Each dungeon should have a validation file under `docs/validation/` containing:

1. donor census;
2. source commit;
3. room clone count;
4. scale verification;
5. donor min/max and expanded min/max;
6. generated seed samples;
7. screenshots or recorded observations of joins/doors/vertical traversal;
8. vanilla donor comparison;
9. enemy replacement evidence;
10. loot/resource replacement evidence;
11. Surface-leak check;
12. save/reload evidence;
13. multiplayer evidence;
14. economy sample;
15. final promotion commit.

The claim vocabulary remains:

- **implemented** = source exists;
- **source verified** = static/read-back checks pass;
- **compiled** = current runtime builds against the current game assemblies;
- **runtime verified** = installed Valheim test succeeds;
- **promoted / RuntimeReady** = all required gates for that family have passed.

Do not collapse those terms.

---

# 12. Definition of done

The Deep Dungeon Expansion program is complete only when all of the following are true:

- all five ordinary Underworld dungeons are individually RuntimeReady;
- each remains clearly recognizable as its vanilla donor family;
- all donor rooms are at least 1.5x linear size;
- each generator targets at least 3.5x live donor room count;
- all five generate reliably over multiple seeds;
- all vanilla enemy population is gone from derivatives;
- all vanilla progression loot is gone from derivatives;
- Underworld creatures/resources fully occupy the donor gameplay sockets;
- economy remains balanced against exterior biome gathering;
- entrances generate only in their owning Underworld biome;
- save/reload and multiplayer work;
- vanilla Surface donors remain untouched;
- Deep Fracture remains fully bespoke and unaffected;
- production tooling no longer wastes Blender capacity on retired ordinary bespoke room families;
- validation evidence exists for every family.

The objective is not to hide the reuse.

The objective is to make the player recognize what Valheim gave us, then discover that Magenheim
has made it **larger, deeper, more dangerous, more rewarding and worth exploring again**.
