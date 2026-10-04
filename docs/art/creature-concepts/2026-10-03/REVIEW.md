# Dedicated creature concept pass

48 selected sheets cover the 42 ordinary Underworld species and six named biome
bosses. Large creatures and bosses were generated first. All images were produced
with built-in imagegen, individually rather than combining species on one sheet.
The six biome overviews and boss overview are review layouts of those images.

The user adopted this set as the long-term creature construction design language
on 3 October 2026. See the [construction and reference guide](../README.md).

The roster, size bands and anatomical requirements come from
`docs/UNDERWORLD_CREATURE_ART_RIG_STANDARD.md`; boss names come from the runtime
encounter roster. Boss silhouette developments are concept proposals, not new
gameplay contracts. Prompts and refinement instructions are preserved in
`prompts.json`, and image provenance is in `generation-receipts.json`.

## Selected revisions

- Abyss Shellback: lowered horizontal body with four walking legs and two claws;
  layered shell now separates it from upright Shoreclaw.
- Gravity Leech: flexible mineral annuli, radial gripping anatomy and expanding
  field organ replace the first draft's crab-like form.
- Rotling: compact six-legged biomass scavenger replaces a humanoid first draft.
- Vent Spitter: full-size inspection confirmed its original four-legged anatomy;
  the initial thumbnail concern did not require a revision.

The revised sheets use sibling `--v2.png` filenames. First drafts remain available.
All selected images were decoded and checked; their hashes are recorded in
`generation-manifest.json`. Each biome has eight selected sheets: seven creatures
and one boss. The gallery supports name, biome and boss/creature filters.

## How to use the concepts

Start modeling from the whole-body silhouette, load-bearing anatomy and attack
organs. Use the smaller views to resolve joins and material boundaries. Painted
fibres, pores, fine gills and small surface damage should mainly become owned
texture details; silhouette edges and functional joints need geometry. Establish
measured production proportions and complete limb diagrams before rigging: these
generated concept views are visual references, not measured orthographic CAD.

The initial mesh experiment is retained under `dist/apex-creature-review/draft-meshes`
for comparison. It is not enabled in production authoring or exported/installed.
Original production source anatomy was regenerated after the switch to concepts.
The six production apex models pass targeted payload verification, and all 974
generated files across 28 generators pass freshness verification. Installed
0.0.176 remains unchanged by this art pass.
