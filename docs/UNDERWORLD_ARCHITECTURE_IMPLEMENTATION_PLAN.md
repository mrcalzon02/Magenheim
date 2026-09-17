# The Underworld — Architecture and Rootforged Construction Implementation Plan

> Authoritative correction (2026-09-17): the Underworld uses altered surface-world
> terrain beneath one shared custom fantastical cavern-roof skybox for all biomes.
> The roof cannot vary by biome or terrain area and is not physical geometry.
> Ignore conflicting roof-shelf, ceiling-generation, roof-attachment and biome-specific
> ceiling requirements below. Use ground-supported terrain, flora and local props.
> See `UNDERWORLD_FLORA_TERRAIN_PLAN.md` for the active implementation sequence.

**Project:** Magenheim for Valheim  
**Track:** parallel Underworld architecture/build-system workstream  
**Repository:** `mrcalzon02/Magenheim`  
**Branch:** `main` only  
**Relationship:** subordinate to `INSTRUCTIONS.md` and coordinated with `docs/UNDERWORLD_DESIGN.md` / `docs/UNDERWORLD_IMPLEMENTATION_PLAN.md`, but independently progressable where dependencies permit  
**Status:** durable implementation authority; source presence does not imply compile, runtime, multiplayer, or production admission

---

# 1. Purpose

This track creates the signature player-buildable and civilization-buildable architecture of the Underworld: monumental construction made from World Tree roots, heavy subterranean stone, iron reinforcement and later silver magical binding.

It exists as a parallel workstream rather than being buried inside biome production. The world-layer program answers where the Underworld exists, how players enter it, how its biomes generate, what lives there and how boss progression works. This architecture track answers what civilizations build from, what the player can harvest and unlock, how large halls are assembled, how the structural pieces snap and support one another, and how the build language scales from crude root-and-stone shelters to silverbound monumental structures.

The architectural target is not a reskin of vanilla wood or stone. The signature composition is:

- huge Worldroot columns and beams;
- massive stone plinths and load-bearing bases;
- dark iron straps, collars, ribs, gussets and crossing arches;
- silver rune channels and magical binding in later tiers;
- hanging fire, root growth and carved monolithic surfaces;
- large-format 4 m, 8 m and 12 m construction elements that let players define monumental halls without stacking dozens of tiny vanilla pieces.

The governing visual principle is: **the structure looks grown first and engineered second**.

---

# 2. Parallel-track rule

Architecture advances alongside the main Underworld program but does not create a second mod, repository, definition loader, persistence engine or build system.

This track may progress while world-layer feasibility work continues when the slice is independently implementable, such as:

- pure Core piece definitions and validation;
- stable prefab identity planning;
- procedural visual prototypes;
- icon generation;
- snap-point geometry experiments;
- recipe and progression definitions that do not claim unavailable runtime resources.

Runtime registration that depends on live Underworld resources, stations, unlock state or derived-world context must wait for the corresponding authoritative dependency to exist. Existing broken Magenheim behavior and open P0-P2 runtime gates still outrank expansion scope.

The existing `CrystalArchitectureRegistrar` / `CrystalArchitectureVisuals` pattern is the closest current implementation reference. Rootforged construction should reuse its additive Jötunn piece-registration philosophy, source-prefab compatibility approach, owned visual replacement, collider replacement and explicit snap-point handling instead of introducing another unrelated architecture framework.

---

# 3. Material and progression language

## 3.1 Understone

Understone is the structural masonry family of the Underworld. It anchors construction to terrain and provides foundations, plinths, retaining walls, stairs, buttresses and monumental carved surfaces.

Primary role: **anchor**.

Planned resource authority:

`magenheim.underworld.resource.understone`

Early implementation may derive its runtime harvesting source from an Underworld rock/mineral node, but the build identity must remain independent of any temporary donor prefab.

## 3.2 Worldroot Timber

Worldroot Timber is harvested from severed, dead, shed or otherwise safely harvestable pieces of the World Tree root system rather than requiring the player to damage Yggdrasil itself. It forms beams, pillars, forks, braces, root arches and balcony skeletons.

Primary role: **carry**.

Planned resource authority:

`magenheim.underworld.resource.worldroot_timber`

Later world content may distinguish bark, heartroot, mineralized root or silver-veined root, but the first structural slice uses one stable Worldroot Timber identity to avoid premature resource proliferation.

## 3.3 Iron reinforcement

Iron creates physical reinforcement rather than simply changing color. It appears as collars, straps, arch ribs, tie plates, gussets, hangers, chains and reinforced composite pieces.

Primary role: **span**.

Iron reinforcement should visibly explain why a structure can bridge larger horizontal distances than raw root alone.

## 3.4 Silver binding

Silver is the late construction tier. It does not replace iron. It controls, conducts and magically stabilizes living/root architecture through rune channels, conductor bands and bound junctions.

Primary role: **stabilize**.

Silverbound pieces are intentionally deferred until the root/stone/iron skeleton works. Their eventual implementation may add emission, rune lighting or special support behavior, but no magical effect is considered real until runtime-tested.

---

# 4. Construction scale and snap grid

Normal integration must remain compatible with Valheim's familiar building cadence while adding monumental elements.

| Scale | Purpose |
|---|---|
| 1 m | trim, brackets, collars, railing and small connectors |
| 2 m | ordinary wall/floor rhythm and small structure integration |
| 4 m | primary halls, beams and arches |
| 8 m | great-hall spans and major structural frames |
| 12 m+ | prestige columns, monumental arches and civic/boss-scale structures |

Large pieces define architecture; small pieces finish it.

Required snap policy for major structural pieces:

- pillars: base, top, cardinal sides, selected diagonal sides, and intermediate attachment heights on great columns;
- beams: both ends, midpoint and side attachment anchors where useful;
- arches: both feet, crown and at least one intermediate rib attachment location;
- plinths/foundations: center, corners, edge midpoints and dedicated column socket points;
- cross-rib nodes: orthogonal and diagonal rib attachment points with predictable rotation;
- composite pieces: snap placement must follow the visible structural member, not an inherited donor-prefab shape.

The acceptance target is that a player can place a stone plinth, snap a Worldroot column to it, span columns with iron/root arches, cross ribs at the crown and then attach secondary beams/floors without manual free-placement correction.

---

# 5. Piece families

## 5.1 Slice A0/A1 structural skeleton

The first executable family is intentionally small and testable.

| Stable piece identity | Runtime prefab target | Role |
|---|---|---|
| `magenheim.underworld.build.understone_foundation_2x2` | `Magenheim_Underworld_UnderstoneFoundation_2x2` | basic terrain anchor |
| `magenheim.underworld.build.great_column_plinth` | `Magenheim_Underworld_GreatColumnPlinth` | monumental column base |
| `magenheim.underworld.build.worldroot_beam_2m` | `Magenheim_Underworld_WorldrootBeam_2m` | small root span |
| `magenheim.underworld.build.worldroot_beam_4m` | `Magenheim_Underworld_WorldrootBeam_4m` | primary root span |
| `magenheim.underworld.build.worldroot_beam_8m` | `Magenheim_Underworld_WorldrootBeam_8m` | great-hall root span |
| `magenheim.underworld.build.worldroot_pillar_2m` | `Magenheim_Underworld_WorldrootPillar_2m` | small vertical support |
| `magenheim.underworld.build.worldroot_pillar_4m` | `Magenheim_Underworld_WorldrootPillar_4m` | primary vertical support |
| `magenheim.underworld.build.worldroot_pillar_8m` | `Magenheim_Underworld_WorldrootPillar_8m` | monumental vertical support |
| `magenheim.underworld.build.iron_banded_worldroot_beam_4m` | `Magenheim_Underworld_IronBandedWorldrootBeam_4m` | reinforced span |
| `magenheim.underworld.build.rootforged_arch_rib_4m` | `Magenheim_Underworld_RootforgedArchRib_4m` | medium arch/rib |
| `magenheim.underworld.build.rootforged_arch_rib_8m` | `Magenheim_Underworld_RootforgedArchRib_8m` | great-hall arch/rib |

A0 establishes pure deterministic catalog authority for these eleven pieces. A1 binds them to runtime assets/registration after compile/test gates are available.

## 5.2 A2 cathedral frame expansion

After A1 placement works:

- Worldroot Great Column 12 m;
- Worldroot Forked Column;
- Worldroot Y Brace;
- Worldroot T Brace;
- Root Arch Quarter 8 m;
- Root Arch Half 8 m;
- Rootforged Cross Rib;
- Rootforged Crown Junction;
- Great Understone Foundation 4x4;
- Great Understone Foundation 4x8;
- Understone Buttress;
- Great Understone Buttress;
- Grand Stair 4 m;
- Balcony Support.

This phase must make the structural skeleton of a cathedral-scale hall possible before ornamentation begins.

## 5.3 A3 walls, floors and circulation

Add practical enclosure pieces:

- Understone Wall 2x2;
- Understone Wall 4x2;
- Understone Great Wall 4x4;
- carved variants of the above;
- Worldroot floor joists and balcony frames;
- root/iron railings;
- bridge deck and bridge side-frame pieces;
- monumental doorway frames;
- iron/root gate frames.

## 5.4 A4 Silverbound construction

Introduce the late structural tier only after root/stone/iron placement is proven:

- Silverbound Worldroot Column;
- Silverbound Great Column;
- Silver Conductor Rib 4 m / 8 m;
- Silver Root Junction;
- Silver Suspension Ring;
- Rune-bound Plinth;
- Silverbound Cross Rib;
- Silver Rune Arch;
- optional magical support or emission behavior only after deterministic/runtime rules exist.

## 5.5 A5 architectural finishing library

Finishing pieces include:

- hanging iron braziers;
- chain suspension elements;
- root chandeliers;
- silver rune lamps;
- carved capitals;
- iron pillar collars;
- wall straps and gusset plates;
- 4x4 and 4x8 carved history reliefs;
- great doors and gates;
- carved stone balcony fronts;
- biome/culture-specific trim variants.

These pieces are subordinate to structural usability. Decoration never substitutes for a working frame.

---

# 6. Source architecture and file plan

Do not create a second architecture engine. Extend Magenheim through one clear Underworld-specific adapter and pure catalog authority.

## 6.1 Pure Core authority

Initial source:

```text
src/Magenheim.Core/Underworld/
  UnderworldArchitectureDefinitions.cs

tests/Magenheim.Core.Tests/
  UnderworldArchitectureTests.cs
```

Responsibilities:

- stable piece IDs and prefab names;
- material tier and piece kind;
- meter-scale dimensions;
- logical crafting station identity;
- recoverable build costs;
- deterministic validation;
- duplicate/collision rejection within the catalog;
- deterministic fingerprint independent of source ordering.

This file does not import Unity, Valheim, BepInEx or Jötunn.

## 6.2 Runtime adapter target

Planned later source:

```text
src/Magenheim.Runtime/Underworld/
  UnderworldArchitectureRegistrar.cs
  UnderworldArchitectureVisuals.cs
  UnderworldArchitectureIcons.cs
  UnderworldArchitectureSnapLayout.cs
```

If the repository's existing flat Runtime layout remains authoritative when A1 begins, use the existing project convention instead of reorganizing unrelated files merely to match this planning tree.

Responsibilities:

- additive Hammer registration;
- compatible donor-prefab resolution;
- Magenheim-owned visual replacement;
- material creation/normalization;
- colliders matching visible geometry;
- explicit snap-point generation rather than trusting donor geometry;
- WearNTear health/support configuration;
- icon ownership;
- recoverable recipe requirements;
- occupied prefab identity refusal;
- clean event unsubscription/disposal.

## 6.3 Resource runtime target

Worldroot/Understone item and harvest registration must reuse the eventual Underworld resource authority. Do not create temporary permanent item identities in the architecture registrar merely to make recipes work.

Before A1 becomes player-craftable, the following must resolve to real registered resource items:

```text
magenheim.underworld.resource.worldroot_timber
magenheim.underworld.resource.understone
```

Iron and Silver continue to use Valheim's existing items unless later design explicitly creates a processed intermediate.

---

# 7. Structural behavior targets

Valheim structural support tuning is runtime-specific and therefore not numerically frozen in this planning document until measured against the active build. The relative hierarchy is authoritative:

**Understone anchors. Worldroot carries. Iron spans. Silver stabilizes.**

Expected behavior:

- Understone performs best when terrain-connected and serves as the preferred base for great construction;
- Worldroot must outperform ordinary wood for tall vertical structures;
- iron-banded/rootforged pieces must make visibly larger horizontal spans practical;
- Silverbound construction becomes the highest structural tier if runtime testing supports doing so without destabilizing Valheim's support model.

Support values must be measured through repeatable test structures. Do not simply assign extreme values to force success.

---

# 8. Resource acquisition and biome relationship

The construction system should pull the player back into Underworld exploration.

Worldroot Timber comes primarily from dead, severed, shed or geological root formations. Living Yggdrasil roots should not become ordinary destructible trees unless the design later explicitly establishes a safe harvest mechanic.

Understone comes from Underworld stone formations or processed subterranean rock.

Later biome variants may yield specialized materials:

- Fungal Forest: overgrown/fungal root variants;
- Blackwater: water-darkened or resin-saturated root;
- Sulfurous Wastes: carbonized/heat-cured root;
- Frozen Caverns: frostbound root;
- Fracture Zones: mineralized/crystal-bearing root;
- Great Decay: decayed/carrion-infused architectural variants.

These are style/content extensions, not reasons to duplicate the structural family six times. Shared pieces should accept biome/cultural visual variants where feasible.

---

# 9. Progression gates

Architecture progression is intended to be visible in the player's settlements.

**Rootstone tier:** first Underworld access, Worldroot + Understone. Provides shelter, foundations, beams and pillars.

**Rootforged Iron tier:** follows access to the necessary iron construction process/station. Provides collars, reinforced beams, great ribs and practical monumental spans.

**Silverbound tier:** later progression, tied to Underworld advancement rather than being granted merely because the player carried surface Silver underground. Provides rune-bound monumental pieces and late magical architecture.

Exact unlock flags must eventually enter the Underworld progression definition authority. Until then, the catalog records tier intent only and must not invent a parallel progression state.

---

# 10. Implementation phases

## A0 — deterministic architecture catalog

Implement and validate the eleven-piece structural catalog in `Magenheim.Core` with no runtime registration. Acceptance:

- IDs and prefab identities are stable and Magenheim-owned;
- all dimensions/costs are positive and validated;
- duplicates fail closed;
- fingerprint is order-independent;
- gameplay-significant catalog changes alter the fingerprint;
- deterministic tests are wired into the existing Core harness;
- no Unity/Valheim/Jötunn dependency enters Core.

## A1 — first runtime structural slice

Create owned procedural visuals, icons, colliders, snap points and Hammer registration for the eleven A0 pieces. Reuse proven architecture registration patterns. Acceptance requires compile success and in-game placement, not merely source inspection.

## A2 — great-hall frame

Add 12 m great columns, forks, braces, cross ribs, crown nodes, great foundations, stairs and buttresses. Acceptance structure: a freestanding hall frame using large pieces rather than stacked vanilla substitutes.

## A3 — enclosure and circulation

Add walls, carved variants, balconies, floors, bridges, railings and major door/gate frames. Acceptance structure must be traversable and weather/enclosure behavior must be intentional.

## A4 — Silverbound tier

Add silver/rune structural family and any validated magical structural behavior. Acceptance must include multiplayer-visible state and deterministic unlock rules where gameplay-significant.

## A5 — finish library

Add braziers, lights, chains, reliefs, trim, capitals and cultural variants.

## A6 — civilization integration

Use the same player-buildable language in Underworld settlements, ruins, boss approaches and Deepstone-adjacent civic structures where appropriate. World structures may use prefabs/assemblies of the same visual language but do not need to be literally Hammer pieces when a more efficient worldgen prefab is appropriate.

## A7 — full validation and tuning

Run performance, placement, snap, support, damage, repair, weather, refund, save/load, host/client, dedicated server and mod-removal tests. Tune large-piece collider complexity and rendering cost against real halls.

---

# 11. A1 asset requirements

Each initial piece requires:

- one owned visible root/stone/metal model hierarchy;
- one or more reusable materials with normalized opaque render/depth state;
- intentional worn/broken visual handling;
- replacement colliders matching actual geometry;
- explicit snap layout;
- dedicated build-menu icon;
- correct Hammer category;
- appropriate crafting station;
- recoverable costs;
- occupied-prefab collision guard;
- localization-ready display name/description.

Procedural models are acceptable for first production implementation because Magenheim already owns procedural architecture/furniture geometry. If later authored meshes replace them, prefab identities and recipes remain stable.

---

# 12. Reference-hall acceptance test

The architectural program has a deliberately visual final test inspired by the target hall language.

A player must be able to construct a hall containing:

- stone bases at regular structural bays;
- 8-12 m root columns;
- 4-8 m root/iron arches between bays;
- crossing rib geometry overhead;
- secondary balconies or walkways;
- hanging fire/light fixtures;
- carved wall surfaces or monumental infill;
- visible root growth integrated into the frame;
- no requirement to hide the structure under hundreds of stacked vanilla beams.

The hall must remain practical to place, traverse, repair and reproduce. A visually correct screenshot obtained through free-placement abuse is not acceptance.

---

# 13. Validation ladder

Architecture claims use the normal Magenheim ladder:

1. **definition/source validation** — deterministic identities, catalog and pure tests;
2. **compile validation** — Core and Runtime compile in the normal development environment;
3. **registration validation** — every intended prefab/piece registers once, no occupied identities are overwritten;
4. **placement validation** — pieces appear in the Hammer, place and refund correctly;
5. **snap/collider validation** — the great-hall test assembles without systematic offsets or invisible collision;
6. **structural validation** — support hierarchy behaves intentionally across representative spans;
7. **persistence validation** — placed pieces survive save/load and zone unload/reload;
8. **multiplayer validation** — host and remote clients see identical pieces/state and cannot produce recipe/progression divergence;
9. **performance validation** — large halls remain within acceptable renderer/collider/network budgets;
10. **production admission** — only after required runtime gates pass.

Static source presence must never be reported as in-game completion.

---

# 14. Immediate execution order

The next architecture operations are:

1. land A0 pure Core catalog and deterministic tests;
2. compile/run the existing Core harness in the normal development environment and repair any defect;
3. map the A0 catalog into the existing Magenheim definition/fingerprint authority rather than creating a separate multiplayer authority;
4. implement the two resource item/harvest identities through the Underworld resource framework;
5. implement A1 procedural visuals and snap layouts;
6. register the eleven pieces additively through one Underworld architecture registrar;
7. build and validate a minimum hall frame using plinths, 8 m pillars, 4/8 m beams and 4/8 m arch ribs;
8. only then expand into A2 great-hall framing.

This sequence is the architecture track's durable continuation point.