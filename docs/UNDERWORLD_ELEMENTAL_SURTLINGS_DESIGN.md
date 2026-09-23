# Underworld Elemental Surtlings — 3D Model Design Reference

**Project:** Magenheim — The Underworld  
**Repository:** `mrcalzon02/Magenheim`  
**Status:** next-pass creature/model design reference  
**Scope:** adult humanoid elemental Surtling visual family; modeling, rigging, material, silhouette and biome-placement guidance  
**Elemental suite:** Fire, Water, Earth, Wind, Light/Radiance, Dark/Umbral

> This document is a durable art/design reference for the next Surtling production pass. It does not replace the shared Underworld creature framework, Crystal Shaping authority, biome definitions, spawn planning, balance, or progression documents. Implementations should reuse shared creature registration and animation infrastructure rather than creating a one-off runtime per elemental variant.

---

## 1. Core species concept

The familiar fire Surtling is one visible member of a broader family of elemental spirits.

Underworld Surtlings are **pure elemental intelligences embodied in deliberately humanoid forms**. They are not humans altered by an element, nor six unrelated humanoid species that coincidentally evolved the same body plan. Their consciousness chooses a humanoid shape and then expresses elemental identity through that shape.

This gives the family a unified visual grammar while allowing aggressive silhouette variation. Fire can be athletic, Earth massive, Wind extremely light, Water fluid, Light idealized, and Dark unnervingly elegant without requiring all six forms to share biological proportions.

All represented Surtlings are visually and fictionally adult.

### 1.1 Absolute visual rule

A Surtling must never read as **a normal person wearing an elemental costume**.

The element is the body.

Armor, regalia, belts, plates, sashes, crystal growth, cinder crust, shadow, flowing water, or other coverage may be added where necessary, but those features sit on or emerge from an elemental body rather than disguising a conventional human underneath.

### 1.2 Shared family recognition

Across every element, preserve enough common structure that players recognize the same spirit family:

- adult humanoid proportions;
- recognizable face, torso, arms, hands, hips, legs and feet;
- similar facial construction and eye placement;
- shared base skeleton and animation contracts;
- elemental body rather than conventional skin;
- pronounced but tasteful masculine/feminine silhouette variants;
- compact ornamental coverage instead of full conventional clothing;
- elemental hair or crown language;
- emissive/internal-energy treatment appropriate to the element.

---

## 2. Production architecture

Build Surtlings as **two master humanoid body meshes plus six elemental geometry/material kits**.

The two primary body meshes are:

1. feminine adult Surtling;
2. masculine adult Surtling.

The six elemental kits then modify proportions, surface construction, replacement geometry, hair/crown treatment, particles, materials, armor/regalia and idle presentation.

### 2.1 Skeleton

All twelve primary combinations should share the same bone naming and animation contract wherever practical.

Use controlled mesh variants and bounded bone scaling rather than twelve unrelated rigs.

Shared animation should cover:

- locomotion;
- turns;
- stagger;
- hit reactions;
- attack fundamentals where compatible;
- interaction;
- death/dissolution base timing;
- navigation;
- multiplayer/network animation expectations.

Element-specific overlays then carry identity.

### 2.2 Initial geometry budget

Target production budgets rather than heroic one-off meshes:

- **LOD0:** approximately 6,000–9,000 triangles including elemental regalia;
- **LOD1:** approximately 3,000–4,000 triangles;
- **distant LOD:** approximately 1,000–1,500 triangles.

These are working targets, not excuses to damage silhouette quality. Elemental VFX, alpha cards, translucent fins, flame plumes, shadow masses and similar effects should be budgeted independently.

### 2.3 Material budget

Prefer approximately two primary rendered material families per Surtling:

1. elemental body;
2. regalia/armor/ornament.

Particles, limited overlays, emissive effects and special elemental features may be separate where required. Avoid turning every elemental variant into a many-material draw-call stack.

Earth may require a bounded crystal/mineral secondary treatment. Water and Wind may require carefully controlled translucency. Dark should avoid expensive full-body transparency where a matte absorptive body plus localized shadow geometry will produce the same result.

---

## 3. Sexual dimorphism and attractive elemental tropes

Surtlings intentionally lean into recognizable fantasy-element body archetypes.

The goal is not to make every feminine model the same hourglass body or every masculine model the same bodybuilder. **The element determines what kind of attractive body the spirit chooses.**

| Element | Feminine silhouette | Masculine silhouette | Core physical idea |
|---|---|---|---|
| Fire | athletic hourglass; powerful thighs; narrow waist; strong shoulders | heroic V-taper; lean muscular torso; powerful arms | heat, energy, aggression |
| Water | swimmer's build; pronounced hips; smooth long lines | broad swimmer shoulders; long torso; lean strength | fluidity, grace, sensual motion |
| Earth | tall, heavy, voluptuous, visibly strong | enormous strongman mass; thick chest, waist, arms and legs | mass, stability, physical force |
| Wind | very lean dancer/gymnast; long limbs and legs | lithe runner/climber; narrow waist; long limbs | weightlessness, speed, elegance |
| Light | statuesque classical ideal; regal symmetry | classical heroic sculpture proportions | perfection, authority, serenity |
| Dark | elegant, narrow-waisted, predatory curves, elongated proportions | tall, lean, sharply defined, unnaturally elegant | mystery, temptation, absence |

Coverage should remain deliberate and integrated into the elemental design. If anatomy becomes too suggestive for ordinary gameplay presentation, use elemental armor, natural plates, sashes, shadow, opaque current, mineral growth or ceremonial regalia rather than flattening the body design.

---

## 4. Fire Surtlings

### 4.1 Silhouette

Fire remains the closest relative to Valheim's recognizable Surtling language, but the Underworld form receives a complete humanoid body.

Bodies should read as **charcoal-black solidified flame with heat beneath it**. Glowing fissures appear between muscle masses, joints and plates. The eyes and mouth glow from within.

Feminine fire Surtlings are athletic and powerful rather than delicate. Masculine fire Surtlings use a heroic, lean-muscular shape rather than Earth-like bulk.

### 4.2 Surface and material

- carbonized black outer surface;
- deep orange/red emissive cracks;
- hotter yellow-white centers at eyes, mouth and selected seams;
- subtle heat distortion around the body;
- localized ember shedding;
- no conventional human skin.

### 4.3 Hair / crown

Hair is flame.

Masculine variants can use:

- flame crest;
- swept-back crown;
- blazing mane;
- short violent flame spikes.

Feminine variants can use:

- long flowing fire plume;
- layered flame locks;
- crown-like flame arcs.

Avoid expensive literal hair simulation. Use authored flame forms and controlled VFX motion.

### 4.4 Coverage / regalia

Use:

- cinder girdle;
- obsidian hip plates;
- asymmetric volcanic armor;
- naturally fused blackened plates;
- optional feminine obsidian/cinder breastguard with glowing internal cracks.

The armor should appear grown, forged from the same environment, or fused to the elemental body.

### 4.5 Motion

Fire never fully settles.

Idle overlays should include:

- constant small weight shifts;
- shoulder or chest flicker;
- momentary flame expansion;
- restless head movement;
- ember pulses.

---

## 5. Water Surtlings

### 5.1 Silhouette

Water Surtlings should be beautiful through smoothness, fluidity and continuous motion rather than muscular exaggeration.

Feminine forms use a flowing swimmer/hourglass archetype. Masculine forms use the broad-shouldered, lean-waisted physique of a powerful swimmer.

### 5.2 Surface and material

Do not create bright blue plastic humanoids.

Use:

- nearly black deep-water body mass;
- translucent or semi-opaque edges;
- moving cyan/blue internal caustics;
- slow current motion beneath the surface;
- localized bubbles or droplets;
- wet reflected highlights kept subordinate to the internal movement.

The body can slightly deform or soften during motion without requiring full fluid simulation.

### 5.3 Hair / fins

Hair becomes:

- water ribbons;
- upward-floating sheets;
- tendrils of current;
- cresting wave forms.

Optional fin geometry can appear along:

- forearms;
- calves;
- shoulder blades;
- hips.

Keep fins translucent and low-cost.

### 5.4 Coverage / regalia

The body itself can provide coverage through deliberately opaque current zones.

Additional features may include:

- dark swirling waist current;
- shell plates;
- pearl or mineral fittings;
- river-smoothed stone ornaments;
- limited chest or hip guards.

This should read as controlled elemental opacity, not censorship pasted over an otherwise conventional human body.

### 5.5 Motion

Water never holds an entirely rigid pose.

Idle behavior:

- subtle continuous shoulder/hip roll;
- drifting hair/current;
- breathing-like current expansion;
- slow weight transfer;
- ripple traveling down limbs.

### 5.6 Underworld home

Primary ecological placement: **Blackwater Deep**, subterranean rivers, springs, flooded ruins and pressure-water sites.

---

## 6. Earth Surtlings

### 6.1 Silhouette

Earth is the heaviest Surtling family.

Feminine Earth Surtlings should lean into the powerful earth-mother archetype: broad hips, thick legs, strong abdomen, substantial shoulders and visibly enormous physical strength.

Masculine Earth Surtlings should resemble strongmen rather than dehydrated superhero anatomy: barrel chest, thick waist, huge forearms, massive thighs and short visual transitions between major body masses.

The operative word is **mass**.

### 6.2 Surface and material

The body is assembled from interlocking stone masses functioning like musculature.

Possible variants include:

- basalt;
- granite;
- slate;
- dark fractured stone;
- ore-bearing stone;
- crystal-veined stone.

Connective seams can glow faintly with mineral or elemental energy.

Tiny grit and chips may fall during heavy motion.

### 6.3 Hair / crown

Avoid conventional hair.

Use:

- mineral ridge;
- crystal crown;
- layered stone plates;
- ore spines;
- short gravel-like crest.

### 6.4 Coverage / regalia

Coverage can be part of the anatomy:

- natural stone pelvic plates;
- broad mineral belt;
- chest plates;
- crystal bands;
- heavy ore ornament.

This family needs very little conventional clothing because its body is already constructed from plates.

### 6.5 Motion

Earth should feel costly to accelerate and difficult to stop.

Idle behavior:

- minimal unnecessary movement;
- slow head tracking;
- occasional stone adjustment;
- grit fall;
- deep weight-settle animation.

### 6.6 Underworld home

Primary ecological placement: **Fracture Zones**, mineral seams, crystal fields, tectonic breaks and stable stone enclaves.

---

## 7. Wind Surtlings

### 7.1 Silhouette

Wind should look as though gravity is only loosely applicable.

Feminine forms use a dancer/acrobatic body: long legs, compact athletic torso, long arms and an intentionally light silhouette.

Masculine forms use a runner/climber build: lean shoulders, narrow waist, long limbs and sharp but lightweight muscle definition.

### 7.2 Surface and material

The solid core can be pale translucent material wrapped in moving air.

Use:

- translucent extremity fades;
- mist ribbons;
- dust spirals;
- refractive/distortion hints;
- partial loss of hard silhouette around calves, forearms or hair;
- no requirement for full-body transparency.

### 7.3 Hair / crown

Wind hair should visibly disregard normal gravity.

Use:

- backward streaming plumes;
- upward floating locks;
- spiral ribbons;
- thin vapor crests.

### 7.4 Coverage / regalia

Use orbiting or streaming elemental cloth analogues:

- waist sash;
- crossing chest ribbons;
- floating hip guards;
- feather-light mineral plates.

These should stream, circle or lift rather than hang normally.

### 7.5 Motion

Wind should almost never plant both feet with full visual weight.

Idle behavior:

- heel lift;
- slight hovering impression without necessarily leaving the ground;
- body sway driven by invisible current;
- constant ribbon motion;
- quick head turns;
- very fast recovery from pose changes.

### 7.6 Underworld home

Primary placement: **Fracture Zones**, vertical shafts, pressure vents, large temperature-gradient caverns and other places with violent subterranean airflow.

Wind can overlap spatially with Storm-aligned systems without being synonymous with electricity.

---

## 8. Light / Radiant Surtlings

### 8.1 Elemental authority

Light should map to Magenheim's existing **Radiance** elemental language rather than inventing a redundant progression alignment.

### 8.2 Silhouette

Radiant Surtlings should look deliberately idealized.

Feminine variants use statuesque, classical, regal proportions. Masculine variants use heroic sculpture proportions. Facial symmetry should be noticeably higher than other Surtling families.

Their beauty should feel almost constructed.

### 8.3 Surface and material

Avoid yellow humanoids.

Use:

- translucent alabaster;
- ivory mineral;
- pale crystal;
- strong subsurface illumination;
- white-gold internal glow;
- limited bloom against dark Underworld environments.

Eyes have no obvious pupil and read as concentrated illumination.

### 8.4 Hair / crown

Use:

- luminous thread masses;
- corona;
- halo-like broken arcs;
- light filaments;
- crystalline radiant crown.

### 8.5 Coverage / regalia

Ceremonial radiant armor:

- thin geometric gold/mineral plates;
- luminous girdle;
- floating collar pieces;
- detached chest or hip plates;
- small elements suspended slightly off the body.

The regalia does not have to obey conventional straps or gravity because the wearer is an elemental spirit.

### 8.6 Motion

Radiant Surtlings should exhibit extreme bodily control.

Idle behavior:

- nearly perfect posture;
- measured breathing-light pulse;
- minimal nervous movement;
- slow deliberate gaze;
- controlled hand placement.

They should remain serene even during violence.

---

## 9. Dark / Umbral Surtlings

### 9.1 Elemental authority

For the current pass, **Umbral/Dark is a Surtling phenotype and visual identity, not automatically a new Crystal Shaping alignment**.

Do not expand Magenheim's elemental progression taxonomy merely because this creature family includes a dark form. That decision belongs to the appropriate progression design authority.

### 9.2 Silhouette

Dark is not Radiance recolored black.

Where Radiance represents excessive presence, Umbral presentation should suggest **missing visual information**.

Feminine forms are long-legged, elegant and predatory rather than heavily muscular. Masculine forms are tall, lean and sharply defined with slightly elongated proportions.

Both should be attractive in an uncanny, dangerous way.

### 9.3 Surface and material

Use:

- matte near-black body;
- unusually low environmental reflection;
- faint violet/deep-blue/cold-silver seam light;
- selective silhouette loss;
- localized drifting shadow;
- occasional regions that appear visually absent.

Prefer absorptive material treatment and localized shadow geometry over expensive full-body transparency.

### 9.4 Hair / crown

Use a mutable smoke-shadow mass rather than normal hair.

Its outline can change slowly. Very faint lights or eye-like points may occasionally appear within it, but should not become a constant monster-face gimmick.

### 9.5 Coverage / regalia

Shadow can deliberately obscure parts of the elemental body.

Reinforce it with:

- dark metallic hip guards;
- ritual plates;
- thin belts;
- floating black mineral ornaments.

The result should feel intentional to the species rather than like an external censor layer.

### 9.6 Motion

Dark should use very little wasted movement.

Idle behavior:

- long periods of perfect stillness;
- sudden smooth repositioning;
- shadow motion continuing after the body stops;
- tiny delayed head turns;
- silhouette briefly becoming harder to read.

### 9.7 Underworld ecology

Do **not** automatically bind Umbral Surtlings to the Great Decay.

The Great Decay is corruption, consumption and biological collapse. Pure Darkness is not inherently diseased or evil. Healthy Umbral spirits should be allowed to inhabit places that surface cultures find frightening without making them servants of corruption.

---

## 10. Element-specific idle identity

Shared animation keeps production manageable, but elemental idle overlays are mandatory for silhouette recognition.

| Element | Idle identity |
|---|---|
| Fire | flicker, restless weight shifts, ember pulses |
| Water | continuous current-like shoulder and hip motion |
| Earth | extreme stillness, slow settling, grit fall |
| Wind | constant light sway, lifted heels, streaming attachments |
| Radiance | perfect controlled posture, measured light pulse |
| Umbral | unnatural stillness, delayed shadow movement |

A player should be able to distinguish several elemental Surtlings at moderate distance before reading a health bar or nameplate.

---

## 11. Model-part modularity

The next production pass should favor reusable authored components.

Recommended modular component groups:

- feminine elemental base body;
- masculine elemental base body;
- head/face base;
- elemental eye insert;
- Fire flame crown set;
- Water current-hair set;
- Earth mineral-crown set;
- Wind vapor/ribbon set;
- Radiance corona set;
- Umbral shadow-hair set;
- shared hip-regalia anchors;
- shared chest-regalia anchors;
- shared forearm/calf accessory anchors;
- element-specific body overlay/replacement geometry;
- particle/VFX anchor bones;
- optional weapon attachment anchors.

This should permit controlled individual variation later without creating entirely new character assets.

---

## 12. Biome relationship

The Underworld currently provides natural homes for several variants:

- **Fire:** Sulfurous Wastes, geothermal fields, lavafalls and thermal infrastructure;
- **Water:** Blackwater Deep, rivers, springs and flooded structures;
- **Earth:** Fracture Zones, mineral fields and tectonic environments;
- **Wind:** Fracture Zones, shafts, pressure vents and strong subterranean currents;
- **Radiance:** luminous/crystal/sanctified locations as supported by future ecology definitions;
- **Umbral:** deep-dark ecology and low-luminosity regions, but not automatically the Great Decay.

Biome placement remains subordinate to the authoritative Underworld creature/ecology definitions and shared spawn framework.

---

## 13. Relationship to existing Magenheim systems

These Surtlings must extend existing Magenheim/Underworld authorities rather than creating duplicate systems.

In particular:

- use the shared Underworld creature registration/spawn framework;
- preserve current biome and environment ownership;
- reuse Magenheim elemental identity where an appropriate alignment already exists;
- keep Radiance tied to the existing Radiance identity;
- do not silently create a Dark Crystal Shaping progression family;
- preserve multiplayer/server authority;
- do not build one registrar, AI system or persistence subsystem per elemental Surtling;
- keep model/VFX decisions separate from progression balance until the corresponding design pass.

---

## 14. Next modeling pass

The next asset-development pass should begin with **silhouette proof models**, not final textures.

Recommended order:

1. create shared masculine and feminine humanoid blockout rigs;
2. prove deformation using one shared locomotion set;
3. author Fire and Earth as opposite mass extremes;
4. author Wind and Water as opposite light/fluid extremes;
5. author Radiance and Umbral as opposite material/presence extremes;
6. add coverage/regalia modules;
7. validate all twelve front/side/back silhouettes at gameplay camera distance;
8. create initial LODs;
9. only then proceed into final materials, emissive masks and particles.

The family passes when every element is recognizable in silhouette, both sex variants remain recognizably related, and none of the models read as ordinary humans wearing elemental-themed costumes.
