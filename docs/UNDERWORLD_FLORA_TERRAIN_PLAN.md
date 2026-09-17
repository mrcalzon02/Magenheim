# Underworld Flora, Terrain, Resources and Progression Plan

Companion to `UNDERWORLD_DESIGN.md` (what the Underworld is),
`UNDERWORLD_IMPLEMENTATION_PLAN.md` (how it is built) and
`UNDERWORLD_ARCHITECTURE_IMPLEMENTATION_PLAN.md` (what the player builds with).

This document covers what the Underworld is *made of* and what the player *does* with it:
the living cover, the ground, the harvestable materials, the stations, the equipment, and
the landmarks that make one cavern legible as somewhere.

This is a staged implementation plan. Completed slices are recorded in section 12 and
in validation records; unlisted content remains planned.

## Shared cavern skybox constraint (authoritative)

The Underworld is a copy of the surface world with altered terrain generation. Its custom
fantastical underground skybox depicts **one shared cavern roof across every biome**.
Keep that custom skybox: neither its roof nor its appearance changes by terrain area.
There is no physical world ceiling to generate, collide with, attach flora to, mine or drop.
Biome identity comes from heightfield terrain, ground-supported flora and rock formations,
water, props and local effects. Ignore conflicting roofing/ceiling assumptions in companion
plans. Plant caps are local vegetation, never a substitute world roof. Player-built
architecture remains distinct from the shared skybox.


---

## 1. Why this document exists now

The Underworld already has its skeleton. Six biomes, six bosses, six Deep Boons and six
Deepstones are canonical and fingerprinted. Twenty Deep Fracture districts are the best
assets in the model library at 10,012-11,070 triangles. Transitions, geothermal hazards,
vessels, Deep Gates and provisions all have runtime authority.

What it does not have is **an economy**. A district is a magnificent empty room. The player
arrives through an entrance, walks a corridor, finds a cavern with nothing growing in it,
nothing to gather, nothing to make, and only two raw materials in the entire realm:
`understone` and `worldroot_timber`.

The Underworld currently reads as architecture without ecology, and as a place to visit
rather than a place to live.

---

## 2. The governing principle: progression parity

**The Underworld is not a dungeon appended to the overworld. It is a second Valheim.**

Each overworld biome hands the player a complete package: a material tier, a set of tools, an
armour set, a weapon line, food, a building material and a crafting station that unlocks the
next step. Meadows gives wood, flint and the workbench. Black Forest gives core wood, copper,
tin, bronze and the forge. Mistlands gives eitr, carapace, the eitr refinery and the galdr
table.

Every core Underworld biome must do the same. Six biomes, six complete packages.

This is the single most important constraint in this document, and it is what separates this
plan from "add some mushrooms". If a biome does not give the player a reason to set up camp,
gather, build a station and craft something they could not craft before, that biome is
scenery.

### 2.1 What "a complete package" means

| Element | Overworld precedent | Underworld requirement |
|---|---|---|
| Raw materials | Ore, wood, hide, resin | 3-5 per biome, at least one gated behind the biome's hazard |
| Refined materials | Bronze, iron, black metal | A refining step per tier, at a station |
| Crafting station | Workbench, forge, artisan table | One new station or upgrade per biome |
| Tools | Axe, pick, hammer, cultivator | At least one tool that opens new interaction |
| Armour set | Leather, bronze, iron, wolf, padded | A full head/chest/legs/cape set per biome |
| Weapons | Per-tier lines | At least two weapon types per biome |
| Food | Per-biome cooking chain | 2-3 foods, one cooked |
| Building material | Wood, stone, black marble | A building set (section 6) |
| Comfort / base | Beds, banners, furniture | Underworld furniture line |

### 2.2 Parity does not mean duplication

The Underworld is **not** a reskin of the surface ladder. Its progression is built on
different verbs. The overworld progresses by *hitting things harder*; the Underworld should
progress by *surviving deeper*. Hazard resistance, light, breath, traversal and anchoring are
the Underworld's equivalents of raw damage tiers, and the Deep Boons already establish that
grammar. Equipment below is designed around it.

---

## 3. The standard this work must meet

The model quality campaign (`IMPLEMENTATION_PLAN.md` section 15) set bars that this content
inherits from day one rather than needing a later uplift pass:

| Property | Requirement |
|---|---|
| Detail | Library median is 2,348 triangles. Hero flora, stations and landmarks aim at district scale. Scatter props may sit lower and must justify it. |
| Winding | Closed solids, outward normals. Gated by `verify-model-geometry` and `verify-earth-assets`. |
| Texture | Square, at least 256px, real tonal range. Fine grain, low contrast. |
| UVs | **Never `smart_project` on anything carrying a generated map.** It packs small islands and a map sampled across their boundaries reads as patchwork. Purpose-authored or triplanar only. |
| Scale | Declared and gated. A Valheim player is roughly 1.8m; every dimension below is against that. |
| Held items | Grip at -Y, working end at +Y, gated by `verify-held-model-grip-direction`. |
| Namespacing | `magenheim.underworld.*`. Additive only; no vanilla registration is rewritten, disabled or reordered. |
| Data | Materials, recipes and station requirements belong in the validated, fingerprinted definition snapshot, not in registrar literals. |

Assets are exported through `export-model-assets.py`, and re-exported only when actually
authored, because export is content-stable but not byte-stable.

---

## 4. The six biomes as ecologies

Each biome gets a **canopy layer**, a **ground layer**, a **hazard or atmosphere**, and
**one silhouette** that identifies it from across a cavern.

### 4.1 Fungal Forest — the first breath

*Boss: First Bloom. Boon: Spore Communion. The player's first Underworld.*

The welcoming one, and it must be beautiful, because it is the moment the Underworld either
earns the player's curiosity or does not. Bioluminescence is the light source: the cavern is
lit by what grows in it.

- **Canopy.** Four fungal tree species, the tallest reaching 12-14m, with caps broad enough
  to form a local plant canopy the player walks beneath. Light comes from gill undersides, so the
  floor is lit from above by living things.
- **Ground.** Mycelial mat that takes footprints of light; clustered small caps; a waist-high
  puffshelf the player pushes through; spore drift as ambient particles, not geometry.
- **Atmosphere.** No damage hazard. Spore density affects visibility; Spore Communion clears it.
- **Silhouette.** The **Motherbloom**: one colossal fungal trunk per district, 18m, its broad cap a
  distinctive local landmark, with a hollow base large enough to shelter or build inside.

### 4.2 Blackwater Deep — the drowned dark

*Boss: Blackwater Maw. Boon: Deep Current. The Blackwater Skiff already exists.*

- **Vertical cover.** Ground-supported **column forests**: freestanding mineral spires
  and broken pillars. Nothing connects to the skybox roof.
- **Ground.** Mostly water — black, still, opaque. Shorelines of wet flowstone terraces,
  rimstone pools stepping down like paddy fields, pale blind growths at the waterline.
- **Atmosphere.** Cold, wet, quiet. Wet recovery is already implemented for Deep Current.
- **Silhouette.** The **Drowned Arcade**: a colonnade half-submerged, implying this cavern was
  walked long before the player arrived.

### 4.3 Sulfurous Wastes — the furnace floor

*Boss: Furnace Heart. Boon: Furnace Blood. Geothermal hazard volumes already exist.*

- **Canopy.** Mineral, not living: sulfur chimneys and flue-stacks venting hot gas.
- **Ground.** Cracked basalt plate, sulfur crust breaking underfoot, boiling mud pots,
  obsidian glass fields catching the red light.
- **Atmosphere.** The thermal exposure system is built. Flora is heat-adapted and sparse.
- **Silhouette.** The **Slagfall**: a frozen cascade of cooled flow down a terrain cliff, live
  glow still visible in its cracks.

### 4.4 Frozen Caverns — the white silence

*Boss: White Silence. Boon: Rimebound.*

- **Vertical cover.** Grounded ice spires and frozen cascades on terrain cliffs.
- **Ground.** Blue ice sheet, rime-crusted stone and snow collected in terrain hollows.
- **Atmosphere.** Cold, as the mirror of the Sulfurous Wastes. Rimebound is the answer.
- **Silhouette.** The **Silence**: a still field of freestanding ice fins where a sound-triggered
  fracture is an event. Mechanically a destructible ground-supported formation.

### 4.5 Fracture Zones — the broken country

*Boss: Rift Titan. Boon: Stone Anchor. Where the Deep Fracture districts live.*

The districts are already excellent. This biome needs *cover*, not more architecture.

- **Vertical cover.** Tilted, ground-supported slabs and isolated floating fragments;
  these are local props with no roof attachment.
- **Ground.** Scree, tilted plates, crystal seams carrying the mod's core material language
  into the Underworld.
- **Atmosphere.** Instability; periodic tremor.
- **Silhouette.** The **Hanging Stone**: a fragment the size of a house, suspended and slowly
  turning, anchored by nothing visible.

### 4.6 The Great Decay — the last biome

*Boss: Carrion Crown. Boon: Defiant Flesh.*

The end of the progression, and it should feel like an ending.

- **Canopy.** Ground-rooted fungal trunks and exposed root arches draped in pale sheeting.
- **Ground.** Deep soft matter that slows movement, bone gravel, pooled sourness.
- **Atmosphere.** Contamination over time; Defiant Flesh is the resistance.
- **Silhouette.** The **Crown**: an immense ring of fused remains, and the arena approach.

---

## 5. Fungal trees

The flagship system, and the one that most changes how the Underworld feels.

### 5.1 Why they are not vanilla trees recoloured

A fungal tree has no branches, no bark grain and no leaf card. Its structure is a stalk and a
cap, its material is soft and fibrous, and its light comes from inside. Cloning a `Beech` and
tinting it green produces a green beech. These need their own geometry, their own material
language and their own felling behaviour.

### 5.2 Species

| Species | Biome | Height | Yield | Note |
|---|---|---|---|---|
| Glowcap | Fungal Forest | 6-8m | Worldroot Timber, Glowcap Flesh | The common tree; the player's first Underworld wood. |
| Spirestalk | Fungal Forest | 10-14m | Worldroot Timber x2, Spire Fibre | Tall, thin, sways. Forms the canopy. |
| Shelfwood | Fungal Forest, Great Decay | 4-5m | Shelfwood Plank | Bracket shelves off a stone face; harvested standing, never felled. |
| Motherbloom | Fungal Forest | 18m | Heartwood, Motherbloom Spore | One per district. A landmark, felled only with intent. |
| Palefinger | Blackwater Deep | 3-4m | Pale Fibre | Blind, waterline, no glow. |
| Cinderstalk | Sulfurous Wastes | 5-7m | Charred Timber | Heat-adapted; smoulders when struck. |
| Rimecap | Frozen Caverns | 5-8m | Rimewood | Frozen through; shatters rather than falls. |
| Rotbloom | Great Decay | 6-9m | Rotwood, Decay Spore | Collapses into spores that linger. |

### 5.3 Behaviour

- Felling uses Valheim's existing tree behaviour, so axes, skills and physics work normally.
  Magenheim supplies the prefab, the drops and the material, and does not reimplement felling.
- Caps and stalks drop **separately**, so a felled tree leaves a usable cap on the floor.
- Species that shatter or collapse (Rimecap, Rotbloom) use destruction rather than a falling
  trunk, sidestepping trunk physics in tight caverns.
- **Regrowth** is a design decision, not an assumption. See section 10.

### 5.4 Assets per species

Stalk, cap, felled trunk section, stump and a sapling stage: **5 models per species, 40 in
total**, plus the Motherbloom's hollow-base interior. Caps carry emissive materials; stalks
carry fibre grain.

---

## 6. Underworld stone sets

`understone` exists as a resource and eleven architecture pieces are defined. That is a
foundation, not a set. Vanilla's stonecutter offers a full building vocabulary; the Underworld
should match it, so a player can live down there.

### 6.1 The three stones

- **Understone** — the base grey, already defined. Quarried from terrain outcrops and ground-supported deposits.
- **Blackwater Flowstone** — banded and wet-looking, from Blackwater Deep. The decorative stone.
- **Slagstone** — vesicular and dark, from the Sulfurous Wastes. The heavy stone, and the only
  one that resists the thermal hazard when built with.

### 6.2 The set each stone needs

Floors and foundations (1x1, 2x2, quarter, triangle); walls (full, half, arch, window,
pillared); stairs and ramps; columns (plain, fluted, plus the great column already defined);
arches and vaults (rib, boss, half-dome); edge pieces (cornice, plinth, corbel, baluster).

Roughly **24 pieces per stone, 72 in total**. They must snap to each other and to the existing
Worldroot timber set; snap-point authority already exists in `PlacementSnapAuthority`.

### 6.3 Why three sets rather than one recoloured

A stone set is judged by whether two pieces meet cleanly. Three genuine sets snapping to a
shared grid give the player a palette; one set with three tints gives them a swatch.

---

## 7. Biome resource and progression chains

This is section 2's parity principle made concrete. Each biome is a full tier.

### 7.1 Fungal Forest — the Worldroot tier

| | |
|---|---|
| **Raw** | Worldroot Timber, Glowcap Flesh, Spire Fibre, Understone |
| **Refined** | Worldroot Plank, Spire Cord, Cured Glowcap |
| **Station** | **Mycelial Bench** — the Underworld's workbench. Built from Worldroot Timber and Understone. Everything else is gated behind it. |
| **Tool** | **Sporelight Lantern** — a placed and carried light that is *fuel-free but dimmed by spore density*, teaching the biome's mechanic through an item. |
| **Armour** | **Sporeweave set** — head, chest, legs, cape. Light. Grants passive spore visibility. |
| **Weapons** | Worldroot Club, Worldroot Bow |
| **Food** | Glowcap Flesh (raw), Grilled Glowcap, Spore Broth (cooked, regen) |
| **Building** | Worldroot timber set (defined), Understone set |
| **Gate out** | First Bloom requires Sporeweave to survive the arena's spore saturation. |

### 7.2 Blackwater Deep — the Flowstone tier

| | |
|---|---|
| **Raw** | Blackwater Flowstone, Pale Fibre, Blackwater Pearl, Deep Salt |
| **Refined** | Cut Flowstone, Pale Cloth, Salt-cured provisions |
| **Station** | **Tidal Basin** — a water-fed station for curing, salting and pearl-working. Must be placed adjacent to water, which forces a shoreline base. |
| **Tool** | **Diving Bell Hood** — extends breath; the biome's traversal key alongside the Skiff. |
| **Armour** | **Palewater set** — breath and wet resistance, swim speed. |
| **Weapons** | Flowstone Maul, Harpoon (throwable, retrievable) |
| **Food** | Blind Fish, Salted Fish, Pearl Broth |
| **Building** | Flowstone set; docks and piers that tie to the Skiff |
| **Gate out** | Blackwater Maw requires breath and swim capability. |

### 7.3 Sulfurous Wastes — the Slag tier

| | |
|---|---|
| **Raw** | Slagstone, Sulfur, Charred Timber, **Emberiron** (the biome's ore) |
| **Refined** | **Emberiron ingot**, Slag Brick, Sulfur Powder |
| **Station** | **Furnace Heart Forge** — the Underworld's smelting tier. Requires a geothermal vent as its heat source, so it can only be built where the biome allows. |
| **Tool** | **Slag Pick** — the only pick that cuts Fracture Zone seams, gating the next biome. |
| **Armour** | **Emberiron set** — heavy; grants thermal resistance, stacking with Furnace Blood. |
| **Weapons** | Emberiron Axe, Emberiron Greatsword |
| **Food** | Sulfur Crust (raw, penalty), Ember Bread, Ash Stew |
| **Building** | Slagstone set — the only heat-resistant building material |
| **Gate out** | Furnace Heart requires thermal resistance; the Slag Pick gates Fracture Zones. |

### 7.4 Frozen Caverns — the Rime tier

| | |
|---|---|
| **Raw** | Rimewood, Clear Ice, **Rimesilver** |
| **Refined** | Rimesilver ingot, Tempered Ice (a transparent building material) |
| **Station** | **Silence Table** — precision work: enchanting-adjacent, sound-sensitive, and the tier that begins tying Underworld materials back into Crystal Shaping. |
| **Tool** | **Rime Chisel** — harvests Clear Ice without shattering it |
| **Armour** | **Rimeward set** — cold resistance, stacking with Rimebound; silent movement |
| **Weapons** | Rimesilver Spear, Icebind Staff (ties to the elemental staff families) |
| **Food** | Clear Ice Draught, Rime Cake, Frozen Marrow |
| **Building** | Tempered Ice — transparent pieces, a genuinely new building verb |
| **Gate out** | White Silence requires cold resistance and silent approach. |

### 7.5 Fracture Zones — the Anchor tier

| | |
|---|---|
| **Raw** | Fracture Crystal (ties to the mod's core crystal loop), Shardstone, **Titanbone** |
| **Refined** | Anchored Crystal, Titanbone Plate |
| **Station** | **Anchor Forge** — works only on stable ground, which the tremor system periodically denies |
| **Tool** | **Anchor Spike** — placeable; stabilises ground locally, the biome's core verb |
| **Armour** | **Stoneanchor set** — tremor immunity, knockback resistance, heavy |
| **Weapons** | Titanbone Atgeir, Shardstone Crossbow |
| **Food** | Marrow Broth, Crystal Draught, Stonebread |
| **Building** | Suspended and cantilevered pieces exploiting the Anchor Spike |
| **Gate out** | Rift Titan requires tremor immunity. |

### 7.6 The Great Decay — the Defiant tier

| | |
|---|---|
| **Raw** | Rotwood, Decay Spore, **Carrion Amber**, Bone Gravel |
| **Refined** | Purified Amber, Defiant Fibre |
| **Station** | **Crown Reliquary** — the final station; combines Deepstone authority with material crafting |
| **Tool** | **Censer** — carried; suppresses contamination in a radius, enabling base-building here |
| **Armour** | **Defiant set** — contamination resistance, the Underworld's endgame armour |
| **Weapons** | Amber Blade, Crown Sceptre |
| **Food** | Amber Preserve, Defiant Stew, Bone Marrow Pie |
| **Building** | Amber-inlaid pieces; light-emitting, decorative endgame |
| **Gate out** | Carrion Crown is the Underworld's final boss. |

### 7.7 Cross-biome design notes

- Every biome introduces **one metal or metal-analogue** (Emberiron, Rimesilver, Titanbone,
  Carrion Amber), so the smithing ladder has the same shape the overworld's does.
- Every biome introduces **one station**, and each station gates the next biome's crafting.
- Every biome's **armour answers that biome's hazard**, which is the Underworld's substitute
  for raw armour tiers and keeps the Deep Boons meaningful rather than redundant: the boon is
  the innate version, the armour is the craftable version, and they stack.
- Every biome introduces **one tool that opens a new interaction**, not just a faster one.
- Food is a genuine chain per biome, because Valheim's health ceiling is food-driven and an
  Underworld without food is an Underworld the player cannot stay in.

---

## 8. Foundational items and stations

| Station | Biome | Role | Precedent |
|---|---|---|---|
| Mycelial Bench | Fungal Forest | The Underworld workbench; everything is gated behind it | Workbench |
| Tidal Basin | Blackwater Deep | Curing, salting, pearl-working; must touch water | Cauldron |
| Furnace Heart Forge | Sulfurous Wastes | Smelting; must touch a vent | Forge / smelter |
| Silence Table | Frozen Caverns | Precision and enchant-adjacent work | Artisan table |
| Anchor Forge | Fracture Zones | Heavy work; needs stable ground | Blast furnace |
| Crown Reliquary | Great Decay | Endgame; Deepstone-aware | Eitr refinery |

Plus the shared base kit the player needs to live down there at all: an Underworld bed, a
comfort-granting furniture line, storage, a light source per tier, and a portal-equivalent
decision (section 10).

Each station follows the established Magenheim pattern: it is a `CraftingStation`, it is
additive, it owns its recipes, and — per the 0.0.56 lesson — a station that hosts a bespoke UI
should not also carry a recipe list.

---

## 9. Feature items and landmarks

Per-district set dressing that makes one cavern distinguishable from another.

- **Wayfinding.** Understone markers, carved arrows, the standing stone (revised 0.0.61), and
  a placeable lit **Deepmark**.
- **Gathering.** Understone deposits, Worldroot outcrops, fungal cluster nodes, crystal seams
  tying the Underworld to the surface mod's core loop.
- **Ruin dressing.** Broken columns, toppled plinths, collapsed arches — the same stone sets
  authored damaged, which costs far less than authoring new forms.
- **Water and heat.** Rimstone pools, mud pots, vents, ice falls.
- **The unsettling.** Sparse and deliberate: a chair facing a wall, a cold hearth, a door in a
  cavern with no building around it. At most once per district.

---

## 10. Open decisions

These change the work materially and are deliberately not assumed.

1. **Do fungal trees regrow?** Vanilla trees do not; Valheim expects replanting. Regrowth
   makes the Underworld sustainable but removes a reason to keep exploring. Replanting needs a
   sapling item and a cultivator analogue.
2. **Is the Underworld farmable?** A fungal cultivator would be a system in its own right, and
   it is what decides whether players settle or raid.
3. **Portals.** If Underworld portals exist, the realm becomes convenient and loses its
   weight. If they do not, every trip is a committed expedition and the Skiff matters more.
   This single decision shapes the whole economy.
4. **Three stone sets or one plus variants?** Seventy-two pieces is a large commitment.
5. **Does Underworld flora burn?** Fire touches the Sulfurous Wastes hazard and Furnace Blood.
6. **Scale of the Motherbloom.** At 18m it is a structure, not a tree, and may deserve to be a
   location rather than a vegetation prefab.
7. **Do Underworld metals feed surface recipes, or stay sovereign?** Sovereign keeps the two
   ladders clean; feeding back makes the Underworld feel worth the trip.

---

## 11. Asset manifest and sequencing

| Stage | Content | Models | Rationale |
|---|---|---|---|
| F1 | Fungal Forest: Glowcap, Spirestalk, Shelfwood, ground cover, Mycelial Bench, Sporeweave, Worldroot tier | ~40 | The first Underworld the player sees. Proves the whole pattern end to end. |
| F2 | Understone building set | ~24 | Lets the player live there; unlocks everything social. |
| F3 | Blackwater Deep: Palefinger, column forests, Tidal Basin, Palewater, Flowstone tier | ~38 | Existing Skiff and wet-recovery systems to build on. |
| F4 | Sulfurous Wastes: Cinderstalk, vents, Furnace Heart Forge, Emberiron tier | ~38 | Existing thermal hazard system to build on. |
| F5 | Flowstone and Slagstone building sets | ~48 | Large; only worth it once F2 proves the grid. |
| F6 | Frozen Caverns: Rimecap, ice forms, Silence Table, Rime tier | ~36 | Late progression. |
| F7 | Fracture Zones: cover, Anchor Forge, Anchor tier | ~30 | Districts already exist; this is cover and economy. |
| F8 | Great Decay: Rotbloom, Crown Reliquary, Defiant tier | ~36 | The ending. |
| F9 | Motherbloom, biome silhouettes, the unsettling | ~14 | Landmarks last, once their biomes exist around them. |

Roughly **300 new models and six full equipment tiers**, against a current library of 281.
This is by a wide margin the largest content program the mod has planned — comparable in scope
to the overworld content that already exists — and the staging exists so each stage is
playable on its own rather than a down payment on a later one.

**F1 is the stage to start.** It is the biome the player meets first, it has the clearest
visual identity, the Deep Fracture districts around it are already finished to a standard the
flora can be judged against, and it is the smallest complete proof that an Underworld biome
can carry a full overworld-parity tier.

## 12. F1 implementation slices

1. **F1a — flora authority and terrain eligibility.** Define Glowcap (6–8m), Spirestalk
   (10–14m) and Shelfwood (4–5m), validate and fingerprint their placement constraints,
   and gate placement by Underworld layer, Fungal Forest biome, dry ground, slope and
   local obstruction. Shelfwood requires an exposed rock face. No roof/skybox input.
2. **F1b — authored assets and runtime harvesting.** Author and verify the species models;
   register native tree/destructible behaviour and persist harvest state. Consume F1a
   only after actual Underworld biome/terrain sampling is available. Do not use surface
   biome names as a substitute or inject plants into surface worlds.
3. **F1c — first economy.** Register harvest materials, Mycelial Bench, recipes, food,
   equipment and ground cover through validated definitions; prove the gather/craft loop.
4. **F1d — playable acceptance.** Verify new-world placement, reload, multiplayer and visual
   quality under the same custom skybox in every biome. F1 is complete only after this.

F1a begins this implementation; it does not claim spawned flora or a playable tier.

### F1a implementation record — 0.0.62

Definitions, loader/fingerprint integration and terrain eligibility rules are implemented
and pass the offline build and deterministic tests. See
[validation record](validation/2026-09-17-underworld-flora-foundation.md).
Runtime flora registration and authored models are still pending; F1 remains incomplete.
