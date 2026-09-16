# Valheim-style elemental staff family rebuild validation

Date: 2026-09-16
Scope: source/static visual-construction validation only
Branch: `main`

## Intent

Bring every Magenheim elemental staff family to the visual construction bar established in `docs/STAFF_VISUAL_STANDARD.md`: authored low-poly silhouettes comparable in construction density and readability to Valheim's existing magic staves, rather than straight shafts with terminal primitive shapes.

## Family coverage

The runtime source contains eight ordinary Magenheim staff families and all are now covered by this standard:

- Fire
- Frost
- Storm
- Earth
- Venom
- Radiance
- Seidr
- Spirit

Spirit was rebuilt immediately before this pass. Fire, Frost, Storm, Earth, Venom, Radiance and Seidr were rebuilt in this pass.

## Shared construction authority

`src/Magenheim.Runtime/ValheimStaffVisualBuilder.cs` now owns the common runtime construction mechanics used by the seven newly rebuilt families:

- canonical `RuntimeMeshPrimitives` box/cylinder/prism reuse;
- segmented, tapered and deliberately crooked two-handed shafts;
- wrapped grip bands, necks, pommels and optional structural reinforcement;
- arbitrary oriented structural segments for forks, cages, ribs, hooks, spires and braces;
- faceted crystal shards rather than Unity spheres;
- small structural plates where an elemental language calls for slabs rather than crystals;
- Magenheim-owned generated material surfaces with semantic wood/leather/metal/stone/crystal treatment;
- concentrated focus lights instead of whole-object emissive wash;
- one hide-original boundary for the vanilla carrier renderer/LOD after the owned silhouette is complete.

The builder consumes the existing canonical runtime mesh authority and does not create a second triangle/winding implementation.

## Element-specific construction language

### Fire

Progresses from a forked ember cradle through iron flame cages to a blackmetal master crown with major spires, unequal cage ribs, satellite embers and a suspended coal. The construction emphasizes charred wood, hot crystal and containment hardware rather than a colored ball on a pole.

### Frost

Progresses from an open wooden/iron fork around an elongated ice focus through silver cages and antler-like rime ribs to a large master ice crown with secondary icicles. The family keeps the open-cradle language associated with Valheim's Frost staff while remaining visually distinct.

### Storm

Uses copper/silver/blackmetal conductor forks, needles, lightning rods and irregular arc cages. Higher tiers add unequal conductor arms, grounding spurs and a three-spire thunderhead rather than merely enlarging or recoloring the focus.

### Earth

Uses heavier shafts, stone yokes, fault plates, deep-stone ribs and blackmetal reinforcement. Higher tiers build a tectonic slab/cage language around a mineral core; the family deliberately avoids relying on point lights as its primary read.

### Venom

Uses thorn hooks, fang cages, crooked growth-like ribs, hanging toxin forms and increasingly aggressive blackmetal containment. The silhouette becomes predatory and asymmetric rather than remaining a radial green crystal assembly.

### Radiance

Uses ceremonial wood/ivory, bronze/gold ray structures, open prism cradles, lens arms, corona rays and a large sanctuary/daybreak crown. Emission and light remain concentrated in the focus and ray nodes while physical construction remains readable.

### Seidr

Uses blackwood, black iron, rune silver and violet/pale eitr crystal. The progression moves from omen hooks through rune-spear cages and crossed witchweave threads to a master fate loom with major spires, eight nodes and crossing structural threads.

### Spirit

Already rebuilt to the same standard in the preceding slice with crooked shafts, bone/metal forks, lantern/reliquary cages, secondary spirit shards, hanging chimes and structural tier escalation.

## Static acceptance observations

- The current runtime directory exposes no additional ordinary elemental staff family beyond the eight named above.
- The seven newly rebuilt families consume `ValheimStaffVisualBuilder` rather than keeping their former private box/cylinder/prism generators.
- Their visible model assemblies are constructed from tested `RuntimeMeshPrimitives` meshes; the shared builder itself does not call `GameObject.CreatePrimitive`.
- Staff materials use `GeneratedSurfaceTextures`, whose existing `OnPrefabsRegistered` owned-only reconciliation provides 256px generated surfaces and UV0 repair for Magenheim-readable procedural meshes that lack usable UVs.
- Hidden vanilla staff prefabs remain behavior/animation carriers; the completed Magenheim visual root replaces their presentation without mutating the vanilla source prefab.

## Validation boundary

No compilation, plugin startup, model export, inventory render, third-person render, dropped-item render, daylight/night lighting comparison, or live Valheim acceptance is claimed by this connector-only cycle.

Source/static completion therefore means the old under-authored staff construction has been replaced. It does **not** mean every scale, attachment transform, clipping edge, lighting intensity or inventory icon has been visually approved in-engine.

## Next actionable gate

Build and install the current `main` through the normal Magenheim closeout, then inspect all 32 staff tier models in Valheim from inventory, equipped third-person, dropped-world and representative bright/dark environments. Correct scale, grip alignment, clipping, excessive emission, material tiling and any silhouette that collapses at normal camera distance. Once the actual rendered models are accepted, capture/model-specific inventory icons from the final silhouettes where the current generic icon authority is weaker than the item model.
