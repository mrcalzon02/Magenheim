# Deep Dungeon Expansion

**Program:** Magenheim Underworld ordinary-dungeon expansion  
**Status:** ACTIVE PLAN — source foundation exists; ordinary dungeon RuntimeReady promotion remains gated  
**Architecture boundary:** five expanded vanilla donor dungeons + one separate bespoke Deep Fracture expedition  
**Primary authorities:** `UnderworldDungeonCatalog`, `UnderworldVanillaDungeonReuseCatalog`, `UnderworldVanillaDungeonRegistrar`, `TESTING.md`

This document is the durable implementation and acceptance plan for the **Deep Dungeon Expansion**
program. It expands the ordinary Underworld dungeon plan beyond the short architectural statement in
`UNDERWORLD_DUNGEON_PROGRAM.md` and defines what still has to be implemented, measured, verified
and live-accepted before any ordinary dungeon can move from `Planned` to `RuntimeReady`.

Where older planning material describes Rootwarren, Drowned Vaults, Cinderworks, Rime Sepulcher or
Carrion Catacombs as bespoke sixteen- or seventeen-model room kits, that older ordinary-dungeon plan
is superseded. Those source assets may remain in the repository for reference or later reuse, but
they are not the runtime architecture of the five ordinary Underworld dungeons.

Deep Fracture is **not** superseded. It remains the bespoke Magenheim custom-dungeon program.

---

## 1. Player-facing goal

The target experience is immediately recognizable but dramatically deeper:

> “Yes, this is a Burial Chamber / Sunken Crypt / Infested Mine / Frost Cave / Winding Tunnel —
> but holy shit, it is huge, it keeps going, the enemies are different, and the things I am finding
> here belong to this Underworld biome.”

The five ordinary Underworld dungeons therefore reuse Valheim's existing entrances, tilesets,
connection grammar and dungeon-generation mechanics instead of replacing them with visually
unrelated custom architecture.

They differ from their Surface donors in four major ways:

1. **larger spaces** — donor rooms are at least **1.5x their vanilla linear size**;
2. **much longer expeditions** — donor min/max room-count targets are at least **3.5x vanilla**;
3. **Underworld ecology** — donor enemies and spawn tables are replaced with the owning biome's
   Magenheim creatures;
4. **Underworld economy** — donor loot, mineables, pickables and resource rewards are replaced with
   canonical resources from that Underworld biome.

At the current minimum floors, a similarly composed dungeon can approach roughly twelve times the
vanilla donor's gross explorable room volume: 1.5x linear scale is 3.375x room volume, multiplied by
a 3.5x room-count target. That is an order-of-magnitude experience change while still reading as the
same architectural family.

The goal is not merely “more rooms.” A successful dungeon should feel like an extended expedition:
early recognizable spaces, deeper pressure, stronger encounters, rarer resources, occasional
relief, and an eventual return from a route that felt materially longer than a normal Valheim
dungeon.

---

## 2. Architecture boundary

There are two dungeon lanes and they must remain technically separate.

### 2.1 Deep Fracture — bespoke custom content

Deep Fracture remains Magenheim-owned architecture and gameplay:

- exact-plan twenty-district expedition;
- custom authored room set;
- custom traversal and passage assembly;
- custom encounter authority;
- custom resource and crystal interactions;
- deterministic entrance/return ownership;
- persistence and multiplayer state owned by the Deep Fracture runtime.

The generic vanilla-reuse registrar must never register Deep Fracture, substitute vanilla rooms into
it, or treat it as one more donor dungeon.

### 2.2 Five ordinary Underworld dungeons — expanded vanilla reuse

| Underworld biome | Player-facing donor | Generator | Entrance donor(s) | Current catalog state |
|---|---|---|---|---|
| Fungal Forest | Burial Chambers | `DG_ForestCrypt` | `Crypt2`, `Crypt3`, `Crypt4` | Planned |
| Blackwater Deep | Sunken Crypts | `DG_SunkenCrypt` | `SunkenCrypt4` | Planned |
| Sulfurous Wastes | Infested Mines | `DG_DvergrTown` | `Mistlands_DvergrTownEntrance1/2` | Planned |
| Frozen Caverns | Frost Caves | `DG_Cave` | `MountainCave02` | Planned |
| Great Decay | Winding Tunnels | `DG_Hole` | `TheHole01` | Planned |

The mapping is intentionally one donor family per ordinary biome. This keeps the player read strong
and prevents all five biomes from collapsing into one repeated generic dungeon language.

---

## 3. Non-negotiable design rules

### 3.1 Preserve donor recognizability

A player familiar with Valheim should recognize the donor from the entrance and room construction
without needing a tooltip. We retain:

- donor entrance silhouette and interaction flow;
- donor `DungeonGenerator` algorithm;
- donor room/tile vocabulary;
- donor `RoomConnection` types and connection grammar;
- donor doors, gates and route-blocking mechanics where they remain functional;
- donor-specific verticality, branching and traversal character;
- donor collision and pathing logic unless a verified scaling defect requires a targeted repair.

### 3.2 Never mutate vanilla assets

All scale, population and loot changes happen on Magenheim-owned clones.

Do not:

- edit the live vanilla `DungeonDB` room prefab;
- alter the vanilla donor location in place;
- rescale a donor door globally;
- change donor loot/spawner fields on shared vanilla objects;
- change a donor theme so Surface dungeons begin consuming Magenheim rooms.

A Surface Burial Chamber, Sunken Crypt, Infested Mine, Frost Cave or Winding Tunnel must remain
indistinguishable from unmodded Valheim except for unrelated Magenheim systems that intentionally
apply globally.

### 3.3 No bespoke room injection into ordinary donors

The five ordinary dungeons may gain Magenheim gameplay, population and resources, but they do not
gain Rootwarren/Drowned Vault/Cinderworks/Rime Sepulcher/Carrion Catacombs room geometry.

Negative space and familiar architecture are strengths here. Do not decorate every square meter to
prove that the dungeon is modded. Identity comes primarily from scale, length, ecology, atmosphere,
encounters and resources.

### 3.4 Fail closed

A dungeon that has not cleared its required gates remains `Planned` and must seed **no entrance**.

A partially functional entrance is worse than no entrance because it permanently writes broken
worldgen into player saves.

### 3.5 Promotion is per donor

The five ordinary dungeon families do not need to be promoted simultaneously. Each can become
`RuntimeReady` independently after it clears the shared gates and its donor-specific gates.

---

## 4. Current source foundation

The source foundation already exists and should be extended rather than replaced.

### 4.1 Core authority

`UnderworldVanillaDungeonReuseCatalog` currently enforces:

- exactly five ordinary donor mappings;
- at least **1.5x** linear room scale;
- at least **3.5x** donor room-count multiplier;
- mandatory vanilla enemy replacement;
- mandatory vanilla loot replacement;
- mandatory biome-resource use;
- explicit prohibition on bespoke Magenheim room injection.

`UnderworldDungeonCatalog` keeps all five ordinary dungeons `Planned` and keeps Deep Fracture
`RuntimeReady`.

### 4.2 Generic runtime registrar

`UnderworldVanillaDungeonRegistrar` currently provides the intended generic seam:

- waits for vanilla room/location availability;
- clones donor room families into Magenheim-owned identities;
- registers a private dungeon theme for each biome;
- scales the clone root and `Room.m_size`;
- clones and scales donor doors;
- derives expanded min/max room counts from the live donor generator;
- expands the legal generator volume;
- preserves/remaps required rooms;
- clones donor entrance locations;
- restricts the custom location to the owning Underworld biome;
- replaces `CreatureSpawner` inhabitants;
- replaces `SpawnArea` tables;
- replaces chest drop tables;
- replaces ordinary pickables;
- replaces `MineRock`, `MineRock5` and `DropOnDestroyed` loot;
- removes obvious donor Vegvisir/Runestone progression hooks.

This is an implementation foundation, not live acceptance.

### 4.3 Production forge boundary

The Blender production forge no longer rebuilds or promotes the five retired bespoke ordinary
dungeon room families. That expensive pipeline should remain focused on assets that Magenheim
actually ships.

Deep Fracture remains a legitimate bespoke asset consumer.

---

## 5. Remaining implementation work

The generic clone path needs to be hardened into a full dungeon system rather than promoted merely
because the basic donor copy works.

### 5.1 Live donor inventory and compatibility audit

Before enabling any donor, capture the current Valheim runtime facts instead of assuming historical
wiki values or old decompilations:

- exact donor entrance prefab(s);
- exact generator object name;
- current `Room.Theme` value;
- current donor room count;
- room names and enabled state;
- entrance/endcap/divider flags;
- required-room list;
- door definitions and connection types;
- donor min/max room counts;
- donor zone size, tile width and algorithm;
- interior/environment settings;
- every gameplay-bearing component found in each donor room family.

The audit should fail on donor drift rather than silently choosing a “close enough” replacement.

### 5.2 Generic donor component sanitation

The current population replacement covers the major known components, but every donor family must
be audited for hidden Surface progression state.

Create a durable donor-component audit that categorizes components as:

- **retain unchanged** — geometry, connection, collision, visual props, structural animation;
- **clone and retarget** — doors, spawners, containers, pickables, mineables, destructible drops;
- **disable/remove** — Surface Vegvisir, Runestone, quest, boss, key, lore or location-specific
  progression hooks that make no sense in the Underworld clone;
- **donor-specific adapter required** — mechanics that are valuable but cannot be treated
  generically.

The gate must compare the actual loaded donor room components against the known policy. A new
Valheim update adding a gameplay-bearing component should create a review failure, not pass unseen.

### 5.3 Scale correctness

The 1.5x rule applies to usable architecture, not just visual meshes.

Verify and, where necessary, adapt:

- root transform scale;
- `Room.m_size`;
- `RoomConnection` world spacing;
- door scale and alignment;
- collider scale;
- stairs and ladder reach;
- ledges and jump distances;
- blockers/destructibles;
- trigger volumes;
- music/environment volumes;
- water surfaces and waterline-dependent geometry;
- nav/pathing behavior;
- any authored offsets stored as world distances rather than child transforms.

If a donor component does not naturally inherit the root scale, it must receive an explicit
Magenheim scaling adapter or be rejected for promotion.

### 5.4 Generator packing and room-count expansion

The 3.5x room-count floor must produce real longer dungeons, not repeated generation failure.

The current source derives expanded min/max counts from the live donor and expands legal
`m_zoneSize` using the room scale and square root of the count multiplier. Treat that formula as a
starting hypothesis, not accepted truth.

Instrument generation and record:

- target room count;
- actual placed room count;
- failed placement attempts;
- dead-end count;
- branch count;
- longest graph distance from entrance;
- total graph edges;
- loop/cross-link count where the donor algorithm permits them;
- generator bounding extents;
- percentage of runs reaching the expanded minimum;
- generation time.

If the generator regularly fails to reach its minimum, adjust legal span or donor-specific packing
parameters. Do not reduce the 3.5x target merely to make the gate green unless the project
explicitly changes the design floor.

### 5.5 Depth model

The dungeon should become more valuable and dangerous as the player travels farther from the
entrance.

Add a deterministic **dungeon depth model** based on generated graph distance or equivalent
placement order. It should not require bespoke room geometry.

Recommended bands:

- **Entry band:** first 0-20% of graph depth — recognizable donor rhythm, common biome resources,
  lower encounter density;
- **Developing band:** 20-45% — full ordinary biome roster, more blockers and resource nodes;
- **Deep band:** 45-75% — stronger species weighting, higher encounter density, improved resource
  rolls, fewer empty rooms;
- **Abyssal band:** final 25% — apex/hero weighting, rare-resource eligibility, highest pressure,
  but not every room becomes a boss arena.

Depth must be deterministic from the dungeon seed so host, peers and reloads agree.

Do not equate “deep” with “every room receives more particles and clutter.” Architectural negative
space should remain readable.

### 5.6 Encounter ecology

Use the biome's existing creature roster, but tune dungeon composition separately from exterior
world spawning.

For each biome define:

- ambient/small-creature weights;
- standard predator weights;
- heavy/construct weights;
- apex eligibility;
- room occupancy floor and ceiling;
- group size;
- respawn policy, if any;
- depth-band multipliers;
- wet/flying/ground constraints;
- exclusions for room shapes where the donor cannot support a creature safely.

The donor's existing spawner sockets are preferred placement anchors because they were authored for
that tileset. Additional runtime spawn anchors should only be introduced where the expanded dungeon
would otherwise contain long stretches with no encounter capacity.

### 5.7 Resource economy

The current generic resource table proves that donor loot can be redirected. It is not yet the
final economy.

Create an explicit per-biome dungeon reward profile with:

- common resource pool;
- uncommon resource pool;
- rare/progression resource pool;
- depth eligibility;
- chest quantities;
- mineable/pickable quantities;
- destructible drops;
- room-level reward budget;
- total expected yield per complete dungeon;
- caps that prevent a 3.5x dungeon from multiplying rare progression output without limit.

The target is for a long dungeon to be **worth doing**, not to make normal biome resource gathering
obsolete after one successful run.

Rare materials should be attached to deep travel, difficult encounters, constrained fixtures or
low-frequency reward rolls rather than sprinkled through every repeated donor room.

### 5.8 Atmosphere and biome identity

Keep the donor architecture, but ensure the interior reads as part of the owning Underworld biome.

Verify:

- correct Underworld environment/lighting family;
- no Surface daylight/weather leaking into the interior;
- biome-appropriate fog/haze;
- restrained creature VFX remain readable;
- Blackwater water remains functional and visually coherent;
- Sulfur heat/atmospheric pressure can operate without obscuring navigation;
- Frozen visibility remains playable under Whiteout-related systems;
- Great Decay contamination remains gameplay-readable.

Atmosphere should support the architecture, not bury it.

### 5.9 Entrance, exit and instance ownership

The donor's proven dungeon travel behavior should be retained wherever possible, but the owning
world instance must remain the Underworld.

A player who enters an ordinary Underworld dungeon must return to:

- the exact entrance they used;
- inside the same Underworld instance;
- with no accidental transfer to the Surface;
- with no swap to a different dungeon entrance;
- with no corruption of a simultaneous Surface player's world membership.

The return path must survive save/reload and multiplayer ownership changes.

### 5.10 Persistence

Verify the native donor persistence model still works after cloning and population replacement.

Required persistent state includes, where applicable:

- generated room graph;
- doors and destroyed blockers;
- opened containers;
- harvested resources;
- mineable/destructible depletion;
- creature clear state when the design intends permanent clearing;
- entrance/return pairing;
- any depth-band or dungeon-seed metadata needed by Magenheim.

Do not introduce a parallel save file for ordinary dungeon state if native ZDO/location persistence
can represent it safely.

### 5.11 Multiplayer

At minimum test:

- host generates and enters first;
- peer generates/enters first where possible;
- host and peer enter together;
- one player remains in Underworld exterior while another is inside;
- simultaneous Surface and Underworld players remain isolated correctly;
- peer kills the last encounter creature;
- peer opens/loots/harvests;
- disconnect/reconnect while another player remains inside;
- server save/restart with player state spanning dungeon/exterior.

The same dungeon seed must resolve to the same room graph, encounter population and resource state on
all peers.

### 5.12 Existing-world admission

RuntimeReady promotion may introduce new locations into existing Underworld saves.

Use the established Underworld location-recovery mechanism rather than requiring a new world,
provided it can safely generate the newly admitted family without disturbing existing chunks or
locations.

Acceptance must cover both:

- fresh Underworld generation;
- an existing Underworld save created before that donor was promoted.

---

## 6. Donor-specific implementation notes

### 6.1 Fungal Forest — Burial Chambers

**Recommended first promotion target.** Burial Chambers are the cleanest proof of the generic
program because their construction grammar is simple, strongly recognizable and mostly enclosed.

Preserve:

- stone chamber/corridor vocabulary;
- wooden doors;
- burial niches and turns;
- donor dead-end behavior.

Replace or audit carefully:

- Skeleton/Ghost/Rancid Remains spawning;
- Surtling Core and other crypt loot;
- Vegvisir/lore elements;
- donor treasure tables.

Desired gameplay read: ancient stone crypt architecture overtaken by fungal ecology, but not
physically rebuilt into a mushroom palace.

### 6.2 Frozen Caverns — Frost Caves

**Recommended second promotion target.** The donor already matches an underground cold biome, so
this is a strong test of scale, ecology and length without introducing water-system complexity.

Preserve:

- cave room language;
- vertical drops and ledges;
- ice/cavern navigation character.

Audit:

- Cultist/Ulv/Bat population;
- Fenris-related rewards;
- crystal/resource fixtures;
- vertical navigation after 1.5x scaling.

### 6.3 Blackwater Deep — Sunken Crypts

**Recommended third promotion target.** This family adds water and blocker/mining complexity.

Preserve:

- flooded masonry identity;
- gated/blocked route grammar;
- room-to-room water relationship.

Replace:

- Draugr/Blob/elite spawners;
- iron-scrap progression;
- withered-bone or Swamp-specific rewards;
- Surface progression hooks.

Audit waterline, swimming, Deep Current interaction and enlarged doorway/blocker geometry closely.

### 6.4 Sulfurous Wastes — Infested Mines

**Recommended fourth promotion target.** Infested Mines provide larger vertical architecture and
more complex donor content.

Preserve:

- Mistlands mine structural grammar;
- vertical movement;
- bridges, doors and large rooms that make the donor recognizable.

Replace:

- Seeker/Tick population;
- Black Core, Sealbreaker and other Mistlands progression rewards;
- any Dvergr- or Mistlands-specific progression hooks.

Audit large-room encounter density carefully. A 1.5x Infested Mine room can become too empty unless
spawn pressure scales intelligently.

### 6.5 Great Decay — Winding Tunnels

**Recommended fifth promotion target.** This is the newest/least-established donor in the current
Valheim data and should be admitted only after the generic system has proven itself on older donors.

Preserve:

- Winding Tunnel subterranean grammar;
- current Deep North traversal mechanics;
- distinctive tunnel construction.

Replace:

- Deep North-specific creatures/rewards;
- donor location events or special occupants that do not belong in Great Decay.

Treat donor drift as likely across game updates and keep this profile especially strict.

---

## 7. Gate sequence

The following order is mandatory unless a later documented decision explicitly changes it. A later
gate cannot compensate for an earlier failure.

### DDE-00 — Authority freeze

**Purpose:** prevent architecture drift while implementation continues.

Pass conditions:

- five donor mappings match the table in this document;
- 1.5x scale and 3.5x room-count floors remain Core-enforced;
- ordinary dungeon room injection remains forbidden;
- Deep Fracture remains outside the generic registrar;
- old bespoke ordinary dungeon forge/promotion paths remain retired.

**Evidence:** Core tests + source inspection.

### DDE-01 — Current Valheim donor audit

**Purpose:** prove that all donor identities and component assumptions match the installed game.

Pass conditions:

- every entrance donor exists;
- every expected generator exists;
- every theme parses and resolves rooms;
- current donor room/component inventory is recorded;
- required-room and door identities are known;
- no unexplained gameplay-bearing component remains unaudited.

**Evidence:** runtime donor dump checked into validation evidence.

### DDE-02 — Clean compile and static contract

**Purpose:** prove the generic runtime actually builds against the current Valheim/Jötunn references.

Pass conditions:

- Core tests pass;
- runtime compiles with warnings-as-errors;
- patch/reflection/type availability gates pass;
- no stale bespoke ordinary-dungeon production gate is reintroduced.

**Evidence:** local/current-reference build log or approved compile environment.

### DDE-03 — Clone isolation

**Purpose:** prove Magenheim changes only private clones.

Pass conditions:

- private Magenheim room identities exist;
- private themes contain only cloned donor rooms;
- vanilla donor rooms/locations/doors are unchanged;
- Surface donor dungeons remain normal after Magenheim dungeon initialization.

**Evidence:** runtime identity dump + Surface donor comparison.

### DDE-04 — Physical scale

**Purpose:** prove 1.5x is real, navigable and collision-correct.

Pass conditions:

- representative room dimensions >= 1.5x donor;
- `Room.m_size` and visible geometry agree;
- connections align;
- doors align;
- no room overlap or exposed void attributable to scale;
- player and intended creature navigation work.

**Evidence:** in-game measurements/screenshots/logged bounds across representative room types.

### DDE-05 — Expanded generation

**Purpose:** prove 3.5x becomes actual exploration.

Pass conditions:

- cloned min/max room targets are >= ceil(live donor * 3.5);
- generated runs routinely reach the expanded minimum;
- legal packing zone is sufficient;
- generation time remains acceptable;
- branching/dead-end character remains recognizably donor-like.

**Evidence:** multi-seed generation telemetry.

### DDE-06 — Population sanitation

**Purpose:** remove Surface gameplay leakage.

Pass conditions:

- no donor creature spawns;
- no donor progression loot remains in chests;
- no donor pickable/mineable/destructible progression loot remains;
- no donor Vegvisir/Runestone/quest/boss hook survives unless explicitly whitelisted;
- retained structural mechanics still function.

**Evidence:** automated donor-component audit + live loot/spawn survey.

### DDE-07 — Depth progression

**Purpose:** make a huge dungeon develop as the player travels deeper.

Pass conditions:

- deterministic depth is calculable for every generated room/placement;
- encounter weights change by depth;
- reward eligibility changes by depth;
- host/peers/reloads agree on depth classification;
- rare rewards cannot appear everywhere merely because a room prefab repeats.

**Evidence:** deterministic seed replay + depth trace.

### DDE-08 — Economy balance

**Purpose:** make the dungeon rewarding without destroying biome progression.

Pass conditions:

- per-biome dungeon reward profile exists;
- expected common/uncommon/rare yield is measured;
- rare-resource caps are enforced;
- full-clear rewards justify the expedition;
- repeated farming does not trivialize the normal resource ecology.

**Evidence:** simulated/recorded loot totals + gameplay sampling.

### DDE-09 — Biome atmosphere and donor-specific mechanics

**Purpose:** retain structural identity while making the dungeon belong to the Underworld.

Pass conditions:

- correct Underworld atmosphere;
- no Surface weather/lighting leak;
- water/heat/frost/decay mechanics behave correctly;
- donor-specific vertical or blocker mechanics still work at 1.5x.

**Evidence:** per-donor live checklist.

### DDE-10 — Worldgen and return

**Purpose:** prove the dungeon belongs to the correct Underworld biome and returns correctly.

Pass conditions:

- locations generate only in owning biome;
- no Surface leakage;
- quantity/spacing obey catalog authority;
- entrance is grounded and usable;
- exit returns to exact owning Underworld entrance;
- existing-world recovery works if the family is newly admitted.

**Evidence:** fresh-seed + existing-save worldgen survey.

### DDE-11 — Persistence and multiplayer

**Purpose:** prove the dungeon is safe for real servers.

Pass conditions:

- generated graph survives save/reload;
- loot/resource state survives;
- encounter state survives as designed;
- host/client see the same dungeon;
- peer interactions persist;
- simultaneous Surface/Underworld players remain correctly isolated;
- disconnect/reconnect does not orphan the return path.

**Evidence:** host + second-peer acceptance record and save/restart test.

### DDE-12 — Performance/stress

**Purpose:** prove “goes on forever” does not mean “kills the server.”

Measure:

- generation duration;
- peak active GameObjects;
- active networked objects;
- active creatures;
- memory growth entering/leaving;
- frame-time impact in large rooms;
- server tick behavior with host + peer;
- save size/change after one and several generated dungeons.

Pass conditions must be set from actual measurements, but obvious runaway generation, repeated
multi-second stalls, unbounded spawner population or save bloat blocks promotion.

### DDE-13 — Donor promotion review

A donor may be promoted from `Planned` to `RuntimeReady` only when DDE-00 through DDE-12 have
passed for that donor.

Promotion is a small targeted source change to `UnderworldDungeonCatalog`; it is not performed by
an asset forge or an automatic “all green” script.

Immediately after promotion:

1. rerun Core validation;
2. compile runtime;
3. verify the location now registers;
4. create a fresh world and confirm at least one expected location;
5. rerun exact entrance/return behavior.

### DDE-14 — Regression closure

After all five ordinary donors are RuntimeReady:

- rerun Deep Fracture live acceptance;
- rerun one normal Surface example of every donor family;
- verify the production forge still ignores retired bespoke ordinary dungeon kits;
- verify no obsolete ordinary room registrar is reintroduced;
- verify existing Underworld save recovery can admit all five families without duplicate placement.

Only then is the overall Deep Dungeon Expansion program complete.

---

## 8. Recommended rollout order

Promote one family at a time:

1. **Fungal Forest / Burial Chambers** — simplest proof of clone, scale, population and loot logic;
2. **Frozen Caverns / Frost Caves** — validates vertical/cave traversal at scale;
3. **Blackwater Deep / Sunken Crypts** — adds water, blockers and mineable reward complexity;
4. **Sulfurous Wastes / Infested Mines** — adds the largest vertical/room-density challenge;
5. **Great Decay / Winding Tunnels** — newest donor, highest donor-drift risk.

Do not wait to finish all five before learning from the first. The point of independent RuntimeReady
promotion is to harden the generic system through real donor differences.

---

## 9. Instrumentation required before promotion

Add or extend developer diagnostics so acceptance does not depend on guessing.

Minimum useful output for an active ordinary dungeon:

- Magenheim dungeon id;
- donor location and generator identity;
- donor theme;
- donor room-family count;
- cloned room-family count;
- donor min/max rooms;
- expanded min/max rooms;
- room scale;
- generator zone scale;
- generated room count;
- graph longest depth;
- encounter count by creature identity;
- resource fixtures by resource identity;
- remaining donor creature/loot violations, if any;
- owning Underworld biome;
- entrance world-instance id;
- interior/return ownership id.

The existing `magenheim_underworld dungeons` diagnostics should be extended where practical rather
than creating many one-off commands.

---

## 10. Tests that should become automated

Automate what can be proven without pretending automation replaces live acceptance.

Core/static tests should cover:

- five donor mappings exactly;
- minimum scale >= 1.5;
- room-count multiplier >= 3.5;
- no bespoke room injection;
- one donor family per ordinary biome;
- Deep Fracture excluded;
- expanded count math;
- unique Magenheim prefab/theme identities;
- promotion requires a known donor profile.

Runtime/static-source gates should cover:

- generic registrar remains wired into plugin bootstrap;
- obsolete five bespoke ordinary registrars are not bootstrapped;
- production forge does not call bespoke ordinary dungeon rebuild/promotion/review scripts;
- room clone changes never write into the donor object;
- population sanitation covers all audited gameplay-bearing component families.

Live acceptance remains mandatory for:

- actual room placement;
- collision;
- pathing;
- water;
- vertical traversal;
- atmosphere;
- return behavior;
- save/reload;
- multiplayer;
- performance.

---

## 11. Failure policy

When a donor fails, fix the root cause at the shared runtime seam if the failure is generic.

Examples:

- all donors overlap after scaling -> fix packing/size authority generically;
- all donor doors are misaligned -> fix cloned door/connection scaling generically;
- one donor contains a unique progression component -> add a donor-specific sanitation adapter;
- only one donor cannot support a creature body plan -> change that biome dungeon encounter table,
  not the creature globally;
- one donor's 3.5x room target cannot fit under the generic zone formula -> create a donor-specific
  packing profile supported by telemetry rather than weakening all five donors.

Do not solve failures by:

- mutating the vanilla donor;
- disabling the gate;
- converting the dungeon back to a bespoke room kit;
- silently lowering the 1.5x/3.5x floors;
- replacing the donor generator with a hand-built teleport chain;
- stubbing persistence or multiplayer behavior;
- marking the catalog RuntimeReady before installed-game evidence exists.

---

## 12. Definition of done

The Deep Dungeon Expansion program is complete when:

- all five ordinary Underworld dungeon families are independently `RuntimeReady`;
- every donor remains recognizably vanilla in architecture;
- every ordinary dungeon is at least 1.5x donor room linear scale;
- every ordinary dungeon targets at least 3.5x live donor room counts;
- generation reliably produces large, navigable expeditions;
- deeper travel changes encounter pressure and reward quality;
- all enemy/resource/loot population belongs to the owning Underworld biome;
- vanilla Surface donors remain untouched;
- entrance/return, persistence and multiplayer are proven;
- performance is acceptable;
- existing worlds can safely admit newly promoted families;
- Deep Fracture still runs its separate bespoke twenty-district program unchanged.

The intended final result is not five replacement dungeons. It is five **massively expanded versions
of Valheim dungeons the player already knows**, repopulated and repurposed as part of the
Underworld — alongside Deep Fracture, which remains the place where Magenheim fully owns the
architecture.


---

## 13. Current implementation state

This section is intentionally operational. It records what exists now so future work does not
re-implement finished source seams or mistake source implementation for runtime acceptance.

### Implemented in Core

- donor-to-biome mapping for all five ordinary dungeons;
- 1.5x minimum linear room scale;
- 3.5x minimum live donor room-count multiplier;
- mandatory vanilla enemy replacement;
- mandatory vanilla loot/resource replacement;
- prohibition on bespoke Magenheim room injection;
- validation that all five ordinary dungeon ids are covered exactly once;
- Deep Fracture exclusion from the generic donor program;
- independent `Planned` / `RuntimeReady` promotion model.

### Implemented in Runtime source

- generic `UnderworldVanillaDungeonRegistrar` bootstrap path;
- private donor room cloning;
- private Magenheim themes;
- room root scale and `Room.m_size` expansion;
- live donor min/max room-count expansion;
- derived generator-zone expansion;
- required-room remapping;
- donor door cloning and scale adjustment;
- cloned donor entrance registration;
- Underworld-biome confinement;
- `CreatureSpawner` replacement;
- `SpawnArea` replacement;
- chest resource-table replacement;
- pickable resource replacement;
- `MineRock`, `MineRock5` and `DropOnDestroyed` replacement;
- removal of obvious donor `Vegvisir` and `Runestone` progression hooks;
- generic registrar wired into the plugin instead of the five retired bespoke ordinary registrars.

### Deliberately not yet enabled

All five ordinary catalog entries remain `Planned`.

Therefore the current source should **not** seed these five new entrances in live worlds yet. That is
intentional. The implementation seam exists so it can be hardened without writing unfinished
locations into player saves.

### Still unproven

The following must be treated as unknown until tested:

- whether every current Valheim donor prefab/theme name is still correct;
- whether all cloned rooms behave correctly at 1.5x;
- whether all donor connection/door mechanics survive scaling;
- whether the current generator-zone formula reliably supports 3.5x room counts;
- whether donor-specific gameplay components have all been sanitized;
- whether the generic resource-table replacement is economically sane;
- whether dungeon-return behavior remains bound to the correct Underworld entrance;
- whether large cloned dungeons survive save/reload and multiplayer;
- whether performance remains acceptable;
- whether all five donors can use one generic implementation without donor-specific adapters.

---

## 14. Execution work packages

Development should proceed in work packages rather than making all five donors RuntimeReady and
debugging the resulting failures simultaneously.

### DDX-W01 — Donor introspection tooling

**Goal:** make the game tell us exactly what each donor contains.

Implement a diagnostic pass that records, for every donor profile:

- resolved entrance prefab;
- resolved `DungeonGenerator` object;
- theme enum;
- live room list;
- enabled/disabled room state;
- entrance/endcap/divider flags;
- room size;
- room connection count and connection types;
- donor door definitions;
- required-room list;
- min/max rooms;
- zone size;
- tile width;
- algorithm;
- interior environment;
- gameplay-bearing component inventory.

Output should be deterministic, human-readable and suitable for checking into
`docs/validation/`.

**Blocks:** DDE-01 and every later donor-specific gate.

### DDX-W02 — Component sanitation registry

**Goal:** replace ad-hoc component stripping with an explicit reviewable policy.

Create a registry that classifies known donor components into:

- structural-retain;
- gameplay-retarget;
- remove;
- donor-specific review.

The runtime should report any gameplay-bearing component that appears in a cloned donor room but is
not represented in this registry.

This becomes the durable protection against future Valheim updates quietly introducing new loot,
quest, event or spawn behavior into reused rooms.

**Blocks:** DDE-03, DDE-06.

### DDX-W03 — Scale verifier

**Goal:** prove that 1.5x works as architecture, not just as a transform.

Instrumentation should compare donor vs clone for representative room types:

- renderer bounds;
- collider bounds;
- `Room.m_size`;
- connection world positions;
- door opening dimensions;
- vertical step/ledge distances.

Log ratios and fail static/runtime acceptance when a cloned gameplay-critical dimension remains at
roughly vanilla scale.

**Blocks:** DDE-04.

### DDX-W04 — Generation telemetry

**Goal:** measure whether the 3.5x target produces real dungeons.

Record each generated ordinary dungeon:

- requested minimum/maximum rooms;
- actual room count;
- placement failures;
- bounding extents;
- generation duration;
- required rooms placed;
- dead ends;
- graph edges;
- longest entrance-to-room graph distance;
- room-family frequency.

Generate aggregate summaries over multiple seeds.

**Blocks:** DDE-05, DDE-12.

### DDX-W05 — Deterministic depth authority

**Goal:** turn added size into progression rather than repetition.

After generation, construct a room graph from the entrance and assign each room a normalized depth
value from 0.0 to 1.0.

Recommended implementation:

1. locate entrance room;
2. build adjacency from actual connected room instances;
3. breadth-first search shortest graph distance;
4. identify maximum reachable depth;
5. normalize each room distance;
6. persist or deterministically recompute from generated dungeon state;
7. expose depth to encounter and reward selection.

If a donor produces disconnected room islands, generation should fail validation rather than
silently assign arbitrary depth.

**Blocks:** DDE-07, DDE-08.

### DDX-W06 — Dungeon encounter profiles

**Goal:** make interior ecology deliberate.

Create one profile per ordinary biome containing:

- creature identities;
- role weighting by depth band;
- permitted room/spawner types;
- maximum simultaneous population;
- group sizes;
- apex eligibility;
- respawn rules;
- aquatic/flying restrictions;
- empty-room probability.

Reuse the existing creature balance authority. Do not create dungeon-specific stat clones unless a
specific encounter mechanic requires one.

**Blocks:** DDE-06, DDE-07.

### DDX-W07 — Dungeon resource profiles

**Goal:** make large dungeons valuable but bounded.

Create one reward profile per ordinary biome with:

- common/uncommon/rare resource pools;
- depth thresholds;
- per-container budget;
- per-mineable budget;
- per-pickable budget;
- per-dungeon rare-resource cap;
- full-clear expected yield;
- refill/respawn policy.

The generic `ResourceTable()` implementation should ultimately consume these profiles rather than
treating all raw materials as roughly interchangeable.

**Blocks:** DDE-08.

### DDX-W08 — Donor-specific adapters

**Goal:** keep one generic system without pretending all vanilla dungeons are identical.

Adapters should only exist when a donor has a real unique mechanic.

Expected candidates:

- Frost Cave vertical traversal;
- Sunken Crypt water/blockers;
- Infested Mine large-room/bridge behavior;
- Winding Tunnel new-version mechanics.

Each adapter must document why the generic path is insufficient and should remain narrow.

**Blocks:** donor-specific DDE-04, DDE-09.

### DDX-W09 — Return/persistence validation

**Goal:** prove dungeon travel remains inside the owning Underworld world instance.

Add diagnostics for:

- owning entrance identity;
- owning world-instance id;
- interior identity;
- return target;
- player instance before entry;
- player instance inside;
- player instance after exit.

Then test save/reload and peer ownership transitions.

**Blocks:** DDE-10, DDE-11.

### DDX-W10 — Promotion tooling

**Goal:** make RuntimeReady promotion small and auditable.

Create a validation command/report that prints, per donor:

- DDE gate state;
- evidence document paths;
- current catalog state;
- unresolved blockers.

It may recommend promotion but must **not** modify `UnderworldDungeonCatalog` automatically.

Promotion remains an explicit source edit after review.

---

## 15. Biome encounter and resource planning

These are planning defaults for the dungeon profiles. They are not permission to hard-code every
room with the same encounter.

### 15.1 Fungal Forest / Burial Chambers

**Creature pool**

- Lantern Moth — ambient/small;
- Sporeling — swarm;
- Capcrawler — skirmisher;
- Mycelial Stalker — hunter;
- Puffback — bruiser;
- Shelf Lurker — hunter/skirmisher;
- Crowncap Brute — deep/apex.

**Resource pool**

- Worldroot Timber — common;
- Glowcap Flesh — common;
- Spire Fibre — common/uncommon;
- Understone — common.

**Depth intent**

Entry rooms should lean toward Sporelings, Lantern Moths, Understone and Glowcap Flesh. Stalkers and
Puffbacks become more common deeper in. Crowncap Brutes should be deep-room events rather than
routine crypt occupants.

**Special concern**

Burial Chambers have many compact corridors. The 1.5x scale helps, but Lox/Troll-derived body plans
still need pathing checks before Puffback/Crowncap Brute are admitted to every room type.

### 15.2 Blackwater Deep / Sunken Crypts

**Creature pool**

- Cave Ray — ambient aquatic;
- Gloomfin — aquatic;
- Blackwater Lamprey — hunter;
- Shoreclaw — skirmisher/amphibious;
- Lantern Angler — hunter;
- Abyss Shellback — heavy;
- Deep Hunter — apex.

**Resource pool**

- Blackwater Flowstone — common;
- Pale Fibre — common/uncommon;
- Deep Salt — common/uncommon;
- Blackwater Pearl — rare.

**Depth intent**

Flowstone and salt may appear throughout. Pearls should become substantially more likely deeper in.
Deep Hunters and Abyss Shellbacks should require rooms with enough water/volume to function.

**Special concern**

Do not place Serpent-derived creatures into dry sealed rooms merely because a generic spawner socket
exists. The encounter profile must understand aquatic suitability.

### 15.3 Sulfurous Wastes / Infested Mines

**Creature pool**

- Ashmite — swarm;
- Cinder Hound — hunter;
- Basalt Crawler — bruiser;
- Vent Spitter — skirmisher;
- Fume Wraith — hunter/spectral;
- Magma Leaper — hunter;
- Furnace Golem — apex/heavy.

**Resource pool**

- Slagstone — common;
- Sulfur — common;
- Charred Timber — common/uncommon;
- Emberiron — rare.

**Depth intent**

Sulfur and Slagstone establish baseline value. Emberiron should strongly favor deep rooms, defended
mineable fixtures or heavy encounters.

**Special concern**

Infested Mines already contain large combat spaces. The 1.5x scale can create enormous volumes;
encounter density must rise enough to avoid empty halls without turning every large room into a
mob pile.

### 15.4 Frozen Caverns / Frost Caves

**Creature pool**

- Rime Moth — ambient;
- Frost Tick — swarm;
- Iceblind — hunter;
- Pale Burrower — skirmisher;
- Rimewing — flying pressure;
- Glacier Stalker — hunter;
- Cryolith Guardian — heavy/apex.

**Resource pool**

- Clear Ice — common;
- Rimewood — common/uncommon;
- Rimesilver — rare.

**Depth intent**

Clear Ice should be common enough to make the dungeon visibly worthwhile. Rimesilver should be deep,
low-frequency and often associated with stronger encounters.

**Special concern**

Vertical traversal after 1.5x scaling is the primary gate: ledges, drops and stairs that are fair at
vanilla scale may become impossible or annoying at 1.5x.

### 15.5 Great Decay / Winding Tunnels

**Creature pool**

- Rotling — swarm;
- Carrion Bloom — skirmisher/caster;
- Spore Husk — hunter;
- Marrow Creeper — skirmisher;
- Decay Hound — hunter;
- Graft Warden — heavy;
- Corpse Orchard — heavy/rare encounter.

**Resource pool**

- Rotwood — common;
- Decay Spore — common;
- Bone Gravel — common/uncommon;
- Carrion Amber — rare.

**Depth intent**

Decay Spore and Bone Gravel establish broad value. Carrion Amber belongs primarily in the Deep and
Abyssal bands, especially behind Warden/Orchard pressure.

**Special concern**

Winding Tunnels are the highest donor-drift risk. Re-audit this donor after every relevant Valheim
update before assuming prior component sanitation remains complete.

---

## 16. Depth-band defaults

Until live telemetry gives a reason to change them, use these as starting design targets:

| Band | Normalized graph depth | Encounter pressure | Resource intent |
|---|---:|---|---|
| Entry | 0.00-0.20 | low | common only, rare exceptional |
| Developing | >0.20-0.45 | moderate | common + uncommon |
| Deep | >0.45-0.75 | high | uncommon + rare eligible |
| Abyssal | >0.75-1.00 | highest | strongest rare eligibility and apex pressure |

These bands should influence **probability**, not produce rigid scripts. A deep room can still be
empty; an entry room can still surprise the player. The overall dungeon should feel authored by
pressure curves without losing vanilla procedural variation.

Recommended initial occupancy targets:

- Entry: 45-60% of encounter-capable sockets populated;
- Developing: 55-70%;
- Deep: 65-80%;
- Abyssal: 70-85%.

Do not exceed these merely because the dungeon is large. Large spaces need pauses.

---

## 17. Reward-budget planning

The final numbers require live balance, but implementation should support a bounded total reward
model from the beginning.

### 17.1 Budget principle

A full clear of a 3.5x dungeon should return materially more value than a vanilla dungeon and enough
Underworld resources to justify the risk/time, but should not equal several hours of uncontested
surface-biome gathering in its rarest resource.

### 17.2 Proposed control model

Each generated dungeon receives a deterministic reward budget derived from:

- biome;
- generated room count;
- graph depth distribution;
- dungeon seed;
- difficulty/encounter population.

Each room consumes part of that budget when its loot/resource fixtures are resolved.

Rare-resource budget should be capped at the dungeon level. This avoids a seed that happens to
repeat many treasure-room donor pieces from generating absurd quantities of Emberiron, Rimesilver,
Blackwater Pearl or Carrion Amber.

### 17.3 No room-prefab rarity exploit

Resource quality must not be permanently tied only to donor room prefab identity. If one vanilla
treasure room repeats several times in a 140-room run, the player should not receive the rare payout
several times without regard to total budget/depth.

Depth + dungeon budget should be authoritative; donor room type is only one weighting factor.

---

## 18. Telemetry schema

For each generated dungeon, emit one summary record with at least:

`dungeon_id`  
`donor_generator`  
`seed`  
`underworld_instance_id`  
`biome`  
`room_scale`  
`donor_min_rooms`  
`donor_max_rooms`  
`target_min_rooms`  
`target_max_rooms`  
`actual_rooms`  
`placement_failures`  
`generation_ms`  
`bounds_x` / `bounds_y` / `bounds_z`  
`max_graph_depth`  
`dead_ends`  
`graph_edges`  
`required_rooms_expected`  
`required_rooms_placed`  
`creature_spawners`  
`spawn_areas`  
`containers`  
`pickables`  
`mineables`  
`donor_population_violations`  
`rare_resource_budget`  
`rare_resource_allocated`

For development builds, a per-room trace should optionally include:

- cloned room prefab;
- donor room prefab;
- graph depth;
- normalized depth band;
- connections;
- assigned encounter profile;
- assigned resource budget;
- any sanitation action applied.

Release builds should keep only concise diagnostics necessary for support unless verbose dungeon
logging is explicitly enabled.

---

## 19. Evidence layout

Use consistent evidence paths so promotion can be audited later.

Recommended layout:

`docs/validation/deep-dungeon-expansion/<donor>/<date>/`

Each donor acceptance directory should contain or reference:

- `donor-audit.md`
- `compile.md`
- `clone-isolation.md`
- `scale.md`
- `generation-telemetry.md`
- `population-sanitation.md`
- `depth-economy.md`
- `worldgen-return.md`
- `multiplayer-persistence.md`
- `performance.md`
- `promotion.md`

Do not create empty evidence files merely to satisfy this structure. An evidence file exists only
when it contains actual observations/results.

---

## 20. Donor promotion packet

Before changing a donor to `RuntimeReady`, assemble this checklist in its `promotion.md`:

- [ ] DDE-00 Authority freeze passed
- [ ] DDE-01 Donor audit passed
- [ ] DDE-02 Compile/static passed
- [ ] DDE-03 Clone isolation passed
- [ ] DDE-04 Physical scale passed
- [ ] DDE-05 Expanded generation passed
- [ ] DDE-06 Population sanitation passed
- [ ] DDE-07 Depth progression passed
- [ ] DDE-08 Economy balance passed
- [ ] DDE-09 Atmosphere/donor mechanics passed
- [ ] DDE-10 Worldgen/return passed
- [ ] DDE-11 Persistence/multiplayer passed
- [ ] DDE-12 Performance/stress passed
- [ ] Surface donor regression checked
- [ ] Deep Fracture regression checked where relevant
- [ ] Existing-world admission tested
- [ ] catalog change reviewed
- [ ] post-promotion fresh-world smoke test completed

The packet should record the exact commit tested. If code changes after the packet is completed, any
affected gate must be rerun.

---

## 21. Development sequence from current state

Given the source that exists today, continue in this order:

1. **Do not promote any ordinary dungeon yet.**
2. Implement **DDX-W01 donor introspection tooling**.
3. Run it against all five live donor families and commit the audit evidence.
4. Implement **DDX-W02 sanitation registry** using the actual donor-component inventory.
5. Compile the generic registrar against the current managed assemblies and fix every API mismatch.
6. Enable **only Fungal/Burial Chambers** in a disposable development branch/state for runtime
   testing; keep the committed production catalog Planned until gates are satisfied.
7. Complete DDE-03 through DDE-06 for Fungal.
8. Implement shared **DDX-W05 depth authority**, **W06 encounters** and **W07 reward budgets** using
   Fungal as the first live donor.
9. Complete DDE-07 through DDE-12 for Fungal.
10. Promote Fungal only after its packet is complete.
11. Repeat the process for Frozen, adding only the adapters discovered by its vertical layout.
12. Repeat for Blackwater, then Sulfurous, then Great Decay.
13. After all five promotions, execute DDE-14 regression closure.

This order intentionally front-loads generic-system defects into the simplest donor so later donors
mostly exercise additional mechanics instead of rediscovering the same clone/scale bugs.

---

## 22. Explicitly deferred enhancements

These are valid future improvements but are **not** prerequisites for first RuntimeReady promotion
unless testing proves one is necessary:

- bespoke dungeon-only creature species;
- custom ordinary-dungeon room geometry;
- custom boss room for every ordinary dungeon;
- unique dungeon-only crafting station;
- dungeon map/minimap UI;
- special dungeon keys beyond donor mechanics;
- procedural wall retexturing of every donor room;
- new voice/lore narration systems;
- custom soundtrack per ordinary donor;
- hand-authored set-piece room inserted into vanilla donor themes.

The program should first prove that **expanded vanilla architecture + Underworld ecology + meaningful
depth/resource progression** is already worth exploring. Add complexity only where the tested
experience actually needs it.

---

## 23. Durable decision log

Record major design changes here instead of allowing them to survive only in chat history.

### 2026-09-29 — Ordinary architecture direction

Decision: ordinary Underworld dungeons reuse vanilla entrances, room/tile sets and construction
mechanics rather than custom Magenheim room architecture.

Reason: the desired experience is recognition followed by surprise at scale and content, not a
light reskin and not an unrelated replacement dungeon.

### 2026-09-29 — Scale floor

Decision: vanilla donor room geometry is at least 1.5x linear scale.

Reason: larger spaces make the donor feel physically transformed without requiring extensive mesh
editing.

### 2026-09-29 — Exploration floor

Decision: ordinary dungeons target at least 3.5x the live donor's min/max room counts.

Reason: the Underworld version must feel like a significant expedition and be worthwhile to explore,
not the exact same dungeon with different enemies.

### 2026-09-29 — Custom-content separation

Decision: Deep Fracture remains the bespoke Magenheim dungeon architecture lane. The five ordinary
donor dungeons do not inject the retired bespoke ordinary room kits.

Reason: preserve a clear distinction between expanded familiar Valheim spaces and Magenheim's fully
custom expedition content.

### 2026-09-29 — Ecology/economy ownership

Decision: all ordinary donor-dungeon enemies and progression rewards come from the owning Underworld
biome.

Reason: architecture is reused; gameplay progression is not. The point is new reasons to explore a
familiar structural language.
