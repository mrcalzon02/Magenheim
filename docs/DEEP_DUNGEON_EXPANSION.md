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
