# Underworld Creature Art, Rigging, and Host-Chassis Standard

**Project:** Magenheim for Valheim  
**Authority:** subordinate to `UNDERWORLD_DESIGN.md` and `UNDERWORLD_IMPLEMENTATION_PLAN.md`  
**Purpose:** production specification for every non-boss Underworld creature model, texture set, animation rig, and reusable host chassis.

## 1. Production rule

Underworld creatures are production Valheim mobs, not placeholders. Every admitted creature must be readable at combat distance, survive close inspection, animate without obvious rigid-body cheating, and retain a distinct ecological/combat silhouette. Shared host rigs are encouraged only where anatomy and motion genuinely match. A host chassis is an animation/technical inheritance point, never permission to reskin one mesh into several unrelated creatures.

Source art must preserve editable high-detail geometry and authored UVs. Runtime meshes may be optimized after visual acceptance. Do not destroy the source asset to meet runtime budgets.

## 2. Creature quality gates

Every creature requires:

- a source model with named anatomical parts and explicit scale metadata;
- coherent topology around every deforming joint, mouth, wing root, tail root, neck, shoulder and hip;
- authored UVs with no automatic patchwork projection;
- purpose-authored albedo plus normal and roughness maps; emission, opacity or mask maps where the design requires them;
- texture source resolution normally 1024px for small/common creatures and 2048px for large, elite, apex, or visually complex creatures; runtime resolution is chosen after inspection;
- silhouette detail at three frequencies: primary body masses, secondary anatomical/armor structures, restrained tertiary surface breakup;
- no painted-on geometry for major eyes, mandibles, horns, claws, plates, exposed cores, major fungal caps, large crystal growth, fins or wing membranes when those features affect silhouette or animation;
- closed/outward-facing geometry except intentionally open mouths, membranes, wounds, vents, gills or shell cavities;
- an explicit pivot/origin, forward axis, ground contact, eye height, attack origins and VFX/socket points;
- a host-rig declaration and a creature-specific animation manifest;
- an acceptance render/inspection before runtime optimization.

### Runtime geometry targets

These are targets, not excuses to damage silhouette.

| Class | Runtime triangle target | Source expectation |
|---|---:|---|
| Tiny ambient/swarm | 1.5k–4k | enough geometry for wings/legs/body articulation |
| Small/common | 4k–9k | complete anatomy and facial/attack structures |
| Medium/common/uncommon | 7k–14k | strong secondary forms and clean deformation |
| Large uncommon | 10k–18k | close-range readable anatomy |
| Elite | 14k–24k | high-detail armor/anatomy and attack structures |
| Apex | 18k–32k | hero-creature treatment below boss scope |

LOD production should normally retain silhouette first: LOD1 ~60–70% of accepted runtime mesh, LOD2 ~30–40%, distant LOD ~12–20% where the creature remains visible at range.

## 3. Host rig library

### HOST-INSECT-FLY
For moths and light flying arthropods. Root, thorax, abdomen chain, head, two antenna chains, independent wing roots, optional leg pairs. Minimum animation set: idle-hover, directional flight, bank-left/right, land, grounded-idle, takeoff, hit, death, flee/alert.

### HOST-SWARM-HEXAPOD
For small six-legged ground organisms. Root/pelvis, thorax/head, three leg chains per side, optional abdomen/tail. Minimum: idle, walk, run/scuttle, turn, alert, attack, hit, stagger where appropriate, death, spawn/emerge.

### HOST-QUADRUPED
For hound/stalker/herbivore body plans. Root, pelvis, 3–5 spine bones, neck/head, jaw, tail chain, scapula/upper/lower/paw forelimbs, thigh/shin/paw hindlimbs. Minimum: idle, walk, trot/run, turn, alert, attack variants, hit, stagger, death; pounce/charge/burrow added by creature.

### HOST-LOW-CRAWLER
For crustacean, capcrawler and heavily armored low bodies. Root, central body, articulated leg chains, claws/mandibles, optional abdomen. Minimum: idle, scuttle, turn, attack-left/right/front, guard, hit, stagger, death.

### HOST-WALL-CLINGER
For shelf/chasm ambushers. Root, segmented torso, four or more long limb chains with IK-friendly end bones, neck/head/jaw. Must support floor/wall presentation poses without requiring mesh rotation hacks. Minimum: cling-idle, crawl, reposition, drop/pounce, recover, attack, hit, death.

### HOST-AQUATIC-FISH
For Gloomfin/lamprey-class swimmers. Root, head/chest, 4–7 spine/tail bones, jaw, paired fin roots. Minimum: swim-idle, cruise, sprint, turn/bank, attack, hit, death, surface/breach when applicable.

### HOST-AQUATIC-RAY
For Cave Ray. Root, body, tail chain and multiple wing-fin deformation chains. Minimum: glide, flap impulse, bank, dive/rise, flee, hit, death.

### HOST-AMPHIB-ARMORED
For Shoreclaw/Shellback. Root, body, multi-leg chains, claws, neck/head where present, shell independent from deforming abdomen. Supports land and swim locomotion sets.

### HOST-BIPED-MASS
For brute/warden/golem-like creatures. Root, pelvis, spine, chest, neck/head/jaw, clavicle/arms/hands, legs/feet, optional core/armor bones. Minimum: idle, locomotion, turn, two attacks, heavy attack, hit, stagger, knockdown/recover if used, death.

### HOST-GLIDER
For Rimewing/Shardwing. Root, chest, neck/head, tail, two multi-bone wing chains plus membrane controls and legs/talons. Minimum: perch, takeoff, flap, glide, bank, dive, strike, land, hit, death.

### HOST-BURROWER
Quadruped/serpentine hybrid extension with dedicated dig forelimbs or head, spine chain and emerge/submerge control. Burrowing must use authored transition animations and terrain eligibility, not instantaneous disappearance.

### HOST-SEGMENTED-FLOAT
For Stonebound/Rift Colossus. Root/control core with independently transformable body segments and optional limb chains. Segment motion must remain server-authoritative in gameplay; animation supplies presentation rather than client-owned physics.

### HOST-ROOTED-COLONY
For Carrion Bloom/Corpse Orchard. Fixed root, trunk/body chains, tendril chains, mouth/spore organs and optional spawn sockets. Minimum: dormant, awaken, idle, attack/tendril, recoil, damaged, death/collapse.

## 4. Fungal Forest roster

| Creature | Tier | Approx. size | Host | Model/animation requirements |
|---|---|---|---|---|
| Lantern Moth | Ambient | 0.45–0.7m span | HOST-INSECT-FLY | four luminous wing surfaces, fuzzy thorax silhouette, antennae; convincing hover/bank/landing |
| Sporeling | Common | 0.35–0.55m | HOST-SWARM-HEXAPOD | fungal abdomen/cap, six articulated legs, visible spore sac; swarm scuttle and death-puff anticipation |
| Capcrawler | Common | 0.8–1.1m long | HOST-LOW-CRAWLER | low armored cap carapace, mandibles, 6–8 legs; cover-height locomotion and frontal strike |
| Mycelial Stalker | Uncommon | 1.2–1.5m shoulder | HOST-QUADRUPED | lean fungal predator, mycelial sensory fronds, opening jaw; crouch, conceal-idle, pounce, failed-pounce retreat |
| Puffback | Uncommon/neutral | 1.6–1.9m shoulder | HOST-QUADRUPED | massive spore bladder/back growth and heavy feet; graze/root, warning display, charge, defensive inflation/puff |
| Shelf Lurker | Uncommon | 1.8–2.4m reach | HOST-WALL-CLINGER | long gripping limbs, flattened torso, downward mouth/sensory crown; cling, lateral crawl, drop ambush |
| Crowncap Brute | Elite | 2.8–3.4m tall | HOST-BIPED-MASS | huge layered crown, armored fungal shoulders, thick forelimbs, exposed gills beneath cap; sweeping control attacks and heavy stagger |

## 5. Blackwater Deep roster

| Creature | Tier | Approx. size | Host | Model/animation requirements |
|---|---|---|---|---|
| Cave Ray | Ambient | 2.5–4m span | HOST-AQUATIC-RAY | broad membrane deformation, long tail, subtle luminous underside |
| Gloomfin | Common | 1.0–1.5m | HOST-AQUATIC-FISH | muscular tail, predatory jaw, pack-readable dorsal silhouette |
| Blackwater Lamprey | Common | 0.7–1.1m | HOST-AQUATIC-FISH | circular toothed mouth modeled in geometry, flexible body; attach/latch animation required |
| Shoreclaw | Common/Uncommon | 1.2–1.7m | HOST-AMPHIB-ARMORED | shell, asymmetric claws, amphibious legs; swim-to-land transition |
| Lantern Angler | Uncommon | 1.8–2.4m | HOST-AQUATIC-FISH | articulated lure, expanding jaw/throat, light organ; lure-idle and pressure/ranged tell |
| Abyss Shellback | Elite | 3–4.5m | HOST-AMPHIB-ARMORED | layered shell plates, vulnerable underside/joints, heavy claws; brace/guard and amphibious locomotion |
| Deep Hunter | Apex | 6–9m | dedicated HOST-AQUATIC-FISH extension | hero predator anatomy, 7+ body/tail deformation bones, breach/turn/attack sequences without rigid-body fish motion |

## 6. Sulfurous Wastes roster

| Creature | Tier | Approx. size | Host | Model/animation requirements |
|---|---|---|---|---|
| Ashmite | Ambient/Common | 0.25–0.45m | HOST-SWARM-HEXAPOD | heat-cracked shell, fast leg cycle, vent-scavenging idle |
| Cinder Hound | Common | 1.1–1.4m shoulder | HOST-QUADRUPED | lean heat-adapted predator, venting mouth/ribs; pack sprint and bite/lunge |
| Basalt Crawler | Common | 1.0–1.5m | HOST-LOW-CRAWLER | overlapping basalt plates and protected face; guard/turn/ram |
| Vent Spitter | Uncommon | 1.3–1.7m | HOST-QUADRUPED extension | visible pressure sac and articulated throat/mouth; charge-up must physically inflate before ranged attack |
| Fume Wraith | Uncommon | 1.8–2.3m visual height | dedicated deforming spectral rig | central readable body/core plus cloth/smoke appendage bones; must not be only a particle cloud |
| Magma Leaper | Uncommon | 1.0–1.4m body | HOST-QUADRUPED extension | oversized hind limbs and heat-resistant ventral plates; crouch, leap arc, landing recovery |
| Furnace Golem | Elite | 3.2–4m | HOST-BIPED-MASS | basalt armor slabs over visible molten seams/core; armor-break visual states and heavy impact animations |

## 7. Frozen Caverns roster

| Creature | Tier | Approx. size | Host | Model/animation requirements |
|---|---|---|---|---|
| Rime Moth | Ambient | 0.45–0.7m span | HOST-INSECT-FLY | related rig, distinct pale crystalline wing anatomy rather than Lantern Moth recolor |
| Frost Tick | Common | 0.3–0.5m | HOST-SWARM-HEXAPOD | grasping legs, expandable abdomen; leap/latch tells |
| Iceblind | Common | 1.3–1.6m shoulder | HOST-QUADRUPED | eyeless head, sensory pits/fronds, heavy neck; listening/search animations essential |
| Pale Burrower | Common/Uncommon | 1.5–2.2m | HOST-BURROWER | digging forelimbs, wedge skull, ice-shedding back; emerge/submerge transitions |
| Rimewing | Uncommon | 2.5–3.5m span | HOST-GLIDER | translucent/crystalline membrane structures, talons; wall/perch-to-glide lifecycle |
| Glacier Stalker | Uncommon | 1.5–1.8m shoulder | HOST-QUADRUPED | long low predator with ice-breaking camouflage plates; patient compressed idle and explosive opener |
| Cryolith Guardian | Elite | 3–3.8m | HOST-BIPED-MASS | ancient mineral/ice armor, internal frozen core; deliberate construct motion distinct from Furnace Golem |

## 8. Fracture Zones roster

| Creature | Tier | Approx. size | Host | Model/animation requirements |
|---|---|---|---|---|
| Fracture Wisp | Ambient/Common | 0.35–0.6m core | dedicated simple float rig | modeled fractured core with orbiting pieces; event-hostile state visually readable |
| Rift Skitter | Common | 0.7–1.0m | HOST-WALL-CLINGER | broad gripping feet and low center of mass; steep-surface presentation |
| Shardwing | Common/Uncommon | 2–3m span | HOST-GLIDER | mineral wing spars and membrane/crystal planes; dive and ridge landing |
| Gravity Leech | Uncommon | 0.8–1.3m | dedicated segmented crawler | radial gripping anatomy and expandable field organ; pull-field charge animation |
| Chasm Stalker | Uncommon | 2.5–3.2m standing reach | HOST-WALL-CLINGER | extremely long load-bearing limbs, compact torso, counterbalancing tail; narrow-route attacks |
| Stonebound | Uncommon | 1.8–2.4m | HOST-SEGMENTED-FLOAT | 5–9 distinct mineral masses around core; push telegraph expressed by segment compression/expansion |
| Rift Colossus | Elite | 4–5.5m | HOST-SEGMENTED-FLOAT | large destructible-looking masses and exposed binding core; stagger/displacement body language |

## 9. Great Decay roster

| Creature | Tier | Approx. size | Host | Model/animation requirements |
|---|---|---|---|---|
| Rotling | Common | 0.45–0.7m | HOST-SWARM-HEXAPOD | asymmetrical biomass scavenger, biting mouth and grasping limbs |
| Carrion Bloom | Common | 1–1.6m tall | HOST-ROOTED-COLONY | rooted flower-mouth, tendrils, spore organs; ranged attack grows visibly from body |
| Spore Husk | Common/Uncommon | 1.7–2.1m | HOST-BIPED-MASS | consumed humanoid-like shell but non-human silhouette; fungus/biomass must alter gait and joint range |
| Marrow Creeper | Uncommon | 0.8–1.2m | HOST-LOW-CRAWLER | bone-supported low body, hooked limbs, flexible abdomen |
| Decay Hound | Uncommon | 1.2–1.5m shoulder | HOST-QUADRUPED | corrupted pack predator with contamination sacs/tendrils; distinct from Cinder Hound geometry |
| Graft Warden | Elite | 3–4m | HOST-BIPED-MASS | fused bone/biomass armor, multiple asymmetrical graft structures, visible vulnerable tissue |
| Corpse Orchard | Elite stationary | 4–7m crown | HOST-ROOTED-COLONY | major rooted organism with trunk, mouths/pods/tendrils and explicit spawn sockets; collapse/death is a major animation event |

## 10. Shared-rig limits

Sharing a host is acceptable when skeleton proportions can be retargeted without breaking gait or silhouette. The following are specifically prohibited:

- Lantern Moth -> Rime Moth as texture-only swap;
- Cinder Hound -> Decay Hound -> Iceblind as mesh recolors;
- Furnace Golem -> Cryolith Guardian as material swaps;
- Crowncap Brute -> Graft Warden as the same body with attachments;
- Gloomfin -> Deep Hunter as simple uniform scaling;
- any creature using a humanoid Valheim skeleton solely because it is convenient.

A shared host should normally share locomotion fundamentals while retaining creature-specific attacks, idles, reactions, anatomy and timing.

## 11. Animation technical contract

Every mobile creature source file must expose a root suitable for networked world motion and a deforming skeleton beneath it. Root motion policy is decided per animation/runtime implementation; clips must not mix baked translation and code-driven translation accidentally.

Required technical sockets/bones where relevant:

`Head`, `Jaw`, `Eye_L/R`, `MouthFX`, `AttackOrigin`, `ProjectileOrigin`, `CoreFX`, `SporeFX`, `Foot_L/R` or numbered feet, `WingTip_L/R`, `TailTip`, `HitCenter`, plus creature-specific attachment points.

Attack animation events must correspond to readable physical contact or release frames. Damage must not fire substantially before the visible claw, jaw, body, projectile, spore organ or shockwave reaches its telegraphed action frame.

IK is recommended for quadruped feet, long-limbed wall creatures and large elites where uneven Underworld terrain would otherwise cause obvious floating. IK must be bounded so it does not distort the authored silhouette.

## 12. Texture/material contract

Creature textures must establish material identity, not merely color identity. Stone, fungal tissue, shell, chitin, hide, ice, bone, wet membrane, crystal, molten seams and Decay biomass require different roughness/normal responses.

Emission is reserved for biological light organs, exposed energetic cores, crystal channels and specific supernatural tissues. Do not make entire creatures glow. Emissive surfaces need a non-emissive daylight/readability state underneath.

High-detail source texture sets should retain:
- albedo/base color;
- normal;
- roughness or packed material mask;
- emission mask where required;
- opacity/cutout only for justified membranes/fibers;
- optional AO/cavity source for baking, not as a substitute for geometry.

## 13. Production order

Creature art follows biome vertical-slice order:

1. Fungal Forest seven-creature ecology;
2. First Bloom only after ordinary ecology establishes the biome's visual language;
3. Blackwater seven-creature ecology;
4. Sulfurous Wastes seven-creature ecology;
5. Frozen Caverns seven-creature ecology;
6. Fracture Zones seven-creature ecology;
7. Great Decay seven-creature ecology.

Within each biome: ambient creature establishes material language, common creature establishes the first gameplay rig, uncommon creatures expand motion/body plans, and elite establishes the biome's maximum non-boss fidelity.

## 14. Acceptance record per creature

A creature is not art-complete until the record contains: source-model path, scale, triangle counts by LOD, material slots, texture dimensions/maps, host rig, unique bones, animation clip list, sockets, collider plan, hitbox plan, VFX attachment plan, render inspection, deformation inspection, Unity/Valheim import inspection, in-game locomotion/attack inspection, multiplayer observation, and identified optimization work.

Passing source generation alone is **authored**, not **accepted**.
