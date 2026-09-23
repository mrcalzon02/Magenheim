# Crystal Weapon Readability and Silhouette Pass — 2026-09-22

## Reconciled authority

`main` at `0df9f46`, matching `origin/main`. Baseline `build.ps1 -Offline` passed before any change
(0.0.97). Raised by user directive: with the crystal weapons now positioned and working correctly,
bring their detailing and texturing at least to vanilla Valheim's standard. This closes the Stage 3
item in `IMPLEMENTATION_PLAN.md` §15.4 and the two open weapon items in `BACKLOG.md`.

## What was wrong

Rendered front, edge-on, three-quarter and as a flat silhouette (the review sheets this pass used):

- **Blades were square prisms.** The sword blade measured 0.156m wide by 0.136m thick, so edge-on
  it was as wide as face-on and read as a crystal stick, not an edged weapon. Knife, spear and
  atgeir heads had the same section. Vanilla's own sword donor is 0.059m thick including its guard.
- **Axe heads were flat slabs** with no wedge toward the edge; guards were plain bars; grips were
  smooth cylinders; the mace head was a cluster of boxes.
- **The bow's string passed through its limbs**, which bowed out past the string line.
- **Surfaces carried no information.** Every part sampled one of seven shared 512px noise maps, and
  every crystal part carried a whole-part emission (the blade at `0.107,0.168,0.183`), which erased
  its shading and flattened the family to one pale tone.

## What changed

`tools/author-crystal-weapons.py` now owns all ten sources, registered in
`assets/generated.manifest.json` as `crystal-weapon-models` and rebuilt by
`tools/rebuild-crystal-weapons.ps1`.

- **Silhouette.** Blades are lofted from designed sections: a thin secondary edge bevel, a fuller on
  swords, a raised midrib on spear and atgeir heads, a straight spine on the seax knife. Guards are
  swept cross-guards with knobbed tips. Grips are leather wraps with raised bands, so the wrap reads
  in silhouette. Axe heads are wedges that thin to the edge, bearded (axe) or crescent (battleaxe),
  with a bright crystal edge laid along the cutting edge. The mace has six double-pointed flanges.
  The bow is a tapered continuous limb with the string outside it, nock to nock. The crossbow has a
  shaped tiller with a dropped butt, a trigger, a rail and a curved prod.
- **Vanilla fittings** that the family lacked: leather grip wraps at the hand on axe, battleaxe,
  mace, spear and atgeir; iron butt ferrules and socket langets on spear and atgeir; a gem-seat
  collar under the greatsword's pommel crystal.
- **Texturing.** Each weapon has one atlas, baked in Cycles at 1024 and filtered to 512: ambient
  occlusion from the whole weapon (guards shade the blade they hold), a worn-edge highlight from a
  bevel-normal mask, and a material pattern evaluated in 3D — running wood grain, mottled hide,
  hammered blackmetal with bright worn edges, tarnished silver, clouded and striated crystal,
  prismatic hue shift. Emission is kept to the accent intents (bright crystal, gems); the crystal
  body's emission is cut to a trace so it keeps its shading.
- **Kept fixed.** Blender Z remains the length axis with the working end at +Z and the grip on the
  origin; every weapon stays within 2cm of its confirmed Z envelope, which the author asserts,
  because the `HeldModelAlignment` trims are fractions of exactly that envelope. Part paths and the
  `magenheim.crystal-weapon.<model>.<intent>[.<n>].<family>` material identity are unchanged except
  where a part now needs its own index. The greatsword's hand-authored design — the wide silver
  quillon bar with its raised ricasso plate and the faceted pommel crystal — is rebuilt as itself.

New parts: `grip-wrap` (axe, battleaxe, mace, spear, atgeir), `butt-cap` (spear, atgeir),
`pommel-collar` (greatsword), `trigger` (crossbow). The crossbow stock is now `grip.timber` rather
than `grip.leather`; the mace strikers alternate deep crystal and prismatic around a bright core,
where before they were bright and prismatic only and the head had no dark value at all.

## Two defects found in the pass itself

- **Bake margin bleed.** All parts bake into one atlas in one call, and Blender applies the bake
  margin per object, so a white crystal edge's margin painted streaks down the hafts beside it. The
  bake now writes no margin; `pad()` grows every island into its gutter afterwards, which also stops
  mip levels averaging black into island borders.
- **Atlas packing.** A 1.6m haft left as a few full-length UV strips set the scale of the whole
  atlas (atgeir coverage 22%). Long parts are cut into 0.28m islands before packing; the bake is
  evaluated in 3D so the extra seams carry no pattern discontinuity.

## Gate change: grip direction now measures area moment, not end-slice area

`verify-held-model-grip-direction.py` failed the new sword and greatsword while both are correct
(blade at +Y by render and by coordinates): a properly tapered blade has little area in its last
22%, while a banded grip and pommel have a lot in the first. The metric was replaced with the first
moment of surface area about the attach origin, measured over every held asset first:

| metric | 40 correct assets, min pos/neg | 5 known-reversed, max pos/neg |
|---|---|---|
| end-slice area (old) | 0.70 — fails the new sword | 0.80 — the reversed sword was never failed |
| reach alone (rejected) | 0.90 — battleaxe and spirit staves | — |
| area moment about the hand (new) | 1.47 (`staff-spirit-simple`) | 0.28 — all five fail |

The known-reversed set is the four Crystal staves at `ee4e765^` and the sword at `bc29d5f^`; the
updated gate's own `inspect()` fails all of them. The new metric is stricter on the defect the gate
exists for, not looser.

## Measurements

From the committed catalog before, and the regenerated payloads after. Length is the runtime
longest axis (unchanged, gated by `verify-model-scale`); thickness is the runtime Z extent, the
edge-on depth of the whole weapon including guard and gems.

| weapon | parts | triangles | length m | thickness m | atlases |
|---|---|---|---|---|---|
| atgeir | 8 -> 10 | 1,328 -> 2,044 | 2.26 | 0.104 | 1 |
| axe | 6 -> 7 | 1,340 -> 1,700 | 1.19 | 0.084 | 1 |
| battleaxe | 10 -> 11 | 2,120 -> 2,596 | 1.56 | 0.148 | 1 |
| bow | 12 -> 12 | 1,296 -> 1,912 | 1.86 | 0.080 | 1 |
| crossbow | 9 -> 10 | 1,116 -> 1,224 | 0.93 | 0.174 | 1 |
| greatsword | 5 -> 6 | 1,040 -> 1,940 | 2.13 | 0.100 | 1 |
| knife | 5 -> 5 | 760 -> 1,116 | 0.93 | 0.036 | 1 |
| mace | 10 -> 11 | 1,792 -> 2,524 | 1.30 | 0.368 | 1 |
| spear | 5 -> 7 | 1,004 -> 1,708 | 1.98 | 0.100 | 1 |
| sword | 6 -> 6 | 1,032 -> 1,656 | 1.56 | 0.072 | 1 |

Median 1,206 -> 1,810 triangles. That is still under the library median of 2,348, deliberately:
the triangles went where they change the silhouette. Sword thickness fell from 0.136m to 0.072m
including its guard; the vanilla donor is 0.059m. Ten 512px atlases replace seven shared maps;
the rebake through the manifest reproduced all ten byte-for-byte.

## Verification boundary

`closeout.ps1 -Offline` passed and installed 0.0.98 into Central Fuckery: generated-file freshness
(227 files, 9 generators), model scale, weapon materials, held-model grip direction, the compiled
`ModelAssetTests` alignment replay (10 of 10 on the confirmed rotation), icon assets, 43,504 Core
assertions, zero compiler warnings. Installed DLL SHA-256 `6DF17093...52ED2`, read back from the
profile; launcher entry verified enabled at v0.0.98.

**Static validation only. No runtime claim.** Not yet observed: the weapons in a hand, under game
lighting, or at gameplay distance. Unverified, in order of risk: that in-hand placement is unchanged
(the alignment test and envelope assert argue it is, but only a hand settles it); that the mace head
does not read too pale in daylight (it is the palest in the icon set); that the baked edges hold up at
512px in first-person range.
