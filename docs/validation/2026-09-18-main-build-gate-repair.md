# `main` did not build — gate and generated-asset repair — 2026-09-18

`main` at `df27a65f94f121c237afeea464d1edc5f43ae33c` failed `build.ps1 -Offline` six times over,
and did not compile. Every failure was in code, gates or assets committed during 2026-09-18's own
work, so none of that day's commits up to `df27a65` had been built, packaged or installed. P0.1
requires a build before push.

This record covers the repairs. The Underworld biome-sector work delivered in the same version is
recorded separately in `2026-09-18-underworld-biome-sector-world-space-binding.md`.

## Failure 0 — `main` did not compile

```
UnderworldMapRaster.cs(27,53): error CS0117: 'UnderworldMapPresentation' does not contain
a definition for 'CellToLogical'
```

The method is `CellCenterToLogical`. `CellToLogical` appears exactly once in repository history — in
`100619f` (08:12), the commit that introduced the caller — so the raster was committed without ever
being compiled, and six further commits landed on top of it. One-word fix; it is listed first
because it means the compiler had not seen `main` all day.

## Failure 1 — the icon gate was unsatisfiable for six staff icons

```
FAIL: icon asset verification
  - staff-radiance-simple.icon.png: visible silhouette occupies only 5.2% of the icon
  - staff-seidr-simple.icon.png: visible silhouette occupies only 5.5% of the icon
  - staff-spirit-advanced.icon.png: visible silhouette occupies only 5.1% of the icon
  - staff-spirit-crystal.icon.png: visible silhouette occupies only 4.4% of the icon
  - staff-spirit-master.icon.png: visible silhouette occupies only 6.0% of the icon
  - staff-spirit-simple.icon.png: visible silhouette occupies only 3.6% of the icon
```

`verify-icon-assets.py` required at least 6% of the icon frame to carry alpha ≥ 24. Measured all 44
shipped icons on three axes — frame coverage, visible bounding-box area, and ink density inside that
box:

| Group | Frame coverage | Bounding box width | Bounding box area | Ink inside box |
|---|---|---|---|---|
| 32 staff icons | 3.64% – 9.87% | 78.5% – 84.4% | 50.6% – 58.2% | 6.4% – 18.8% |
| 12 other icons | 18.2% – 36.7% | 40.6% – 75.0% | 30.5% – 51.6% | 47.2% – 85.4% |

Every staff icon is framed the same way. `staff-spirit-simple` has the *widest* bounding box of any
staff (84.4%) and the lowest frame coverage (3.64%). The difference across the family is ink density
alone — a staff is a hairline shaft, and the tiers differ only in how much mass the head carries.

No change to `render-staff-icons.py` could have satisfied the floor. That renderer already scales
each staff so the larger of its x/y extent fills 1.86 of a 2.2-unit orthographic frame; lifting
spirit-simple past 6% needs roughly 1.65x more linear scale, which crops the staff out of frame. The
gate was asking for something framing cannot produce.

### Repair

The gate now measures three properties instead of one, because one number cannot separate *where a
subject sits* from *how thick it is*:

- **frame coverage**, floor 1.5%, ceiling 94% — a blank render, or an icon that is a filled square;
- **visible bounding-box area**, floor 22% — a subject rendered tiny or pushed into a corner, which
  frame coverage alone cannot distinguish from a correctly framed thin one;
- **ink density inside that box**, floor 4% — an outline-only or ghost render whose box is large but
  whose subject is not there.

All three are still overridable through environment variables for tighter review passes. Floors sit
below the measured library minimum with margin: 1.5% against 3.64%, 22% against 30.5%, 4% against
6.4%.

### Proof that the replacement is stronger, not weaker

Four synthetic icons were written into `assets/earth`, the gate run, and the icons removed again
(`git status` clean afterwards). Each trips a different rule:

```
  - zz-gateprobe-blank.icon.png: visible silhouette occupies only 0.0% of the icon; the render is effectively blank
  - zz-gateprobe-filled.icon.png: visible silhouette occupies 100.0%; icon is effectively a filled square
  - zz-gateprobe-small.icon.png: subject occupies only 15.3% of the icon area; it is rendered too small or off-centre
  - zz-gateprobe-ghost.icon.png: only 3.4% of the subject bounding box is drawn; the render is an outline or a ghost
```

The old single floor would have passed `zz-gateprobe-small` at 15.3% frame coverage. The new gate
rejects it.

### What is not fixed

The staff family carries real readability debt: ink density 6.4%–18.8% against 47%–85% for every
other icon means a staff reads as a hairline in the inventory. That is an icon-composition problem —
pose, head emphasis, shaft weight — not something a threshold decides, and it is recorded in
`BACKLOG.md` P0.-2 rather than hidden behind a lowered number.

## Failure 2 — the Sporeling texture gate was tuned against stale assets

```
RuntimeError: gill: emission has no readable focal highlights (0.18% hot)
```

`assets/textures/underworld/creatures/sporeling/*.png` were last written by `33dc47a`
(2026-09-17 16:25). Four commits to `generate-underworld-sporeling-textures.py` and five to
`verify-underworld-sporeling-textures.py` landed after that. Regenerating from the authoritative
generator changed **all seventeen** maps, so every tightening of the gate since `33dc47a` was
measured against output the generator no longer produces.

Regenerating alone did not fix it. Two defects were in the generator itself:

1. **Emission had no focal highlights.** Emission is `glow × mask`, so a pixel is only a highlight
   when the noise field and the gill ridge mask are both high at once. At glow gain 3.0 that product
   almost never saturated: 0.09% of the gill map read above half brightness against the 0.2% floor.
   Scored seven glow floor/gain and three mask gain combinations against the gate's own measures and
   took the most conservative passing one — gain 3.0 → 4.5, which raises the gill to 0.80% hot (4x
   margin) while lit coverage moves only 6.1% → 7.5%, so the emission stays localized to the gills.

2. **Flesh and the spore sac were the same hue.** `flesh` was tinted `(.72,.57,.78)` and `spore-sac`
   `(.83,.65,.91)` — both magenta-forward, differing only in brightness. Normalized RGB delta at
   combat scale was 0.008 against the 0.025 the gate requires, so the creature's body and its
   identity organ read as one material. The tints have never changed since the generator was
   authored; the chroma rule was added on 2026-09-18 against the stale maps and never run against
   the generator. Flesh is now `(.78,.60,.66)`, a warm tissue tone with the blue removed, which
   leaves the spore sac as the only magenta. Scored five candidates: with flesh moved, the binding
   minimum becomes an unrelated pair (cap-chitin/gill at 0.059, a 2.4x margin), which confirms
   flesh/spore-sac was the only redundant pair.

## Failure 3 — a warning on stderr failed the build after the gate had passed

With both asset defects repaired, `build.ps1` still died at the same line — this time with the gate
reporting success:

```
python.exe : .../verify-underworld-sporeling-textures.py:33: DeprecationWarning:
Image.Image.getdata is deprecated and will be removed in Pillow 14 (2027-10-15).
    + FullyQualifiedErrorId : NativeCommandError
```

Windows PowerShell 5.1 wraps every native stderr line in an ErrorRecord, so under
`$ErrorActionPreference = 'Stop'` a tool that merely warns terminates the script even when it exits
0. `tools/blender.ps1` already documents and works around exactly this trap for Blender; the Python
gates in `build.ps1` did not have the same protection.

Repaired at both levels:

- **The warning.** Pillow 12.3.0 deprecates `Image.Image.getdata` and removes it in Pillow 14, while
  older Pillow has no `get_flattened_data`. A `flat(im)` helper prefers the new accessor when it
  exists and falls back to the old one, added to all four tools that called it:
  `generate-earth-assets.py`, `generate-underworld-capcrawler-textures.py`,
  `generate-underworld-sporeling-textures.py` and `verify-underworld-sporeling-textures.py`. Both
  affected verifiers now run with zero stderr lines.
- **The trap.** `build.ps1` relaxes `$ErrorActionPreference` to `Continue` for the gate block only
  and restores `Stop` before packaging. Every native call in that block is already followed by an
  explicit `$LASTEXITCODE` check and each `.ps1` gate sets its own `Stop` and throws, so the exit
  code remains the decision and no error handling is lost — while the packaging section, which uses
  cmdlets whose non-terminating errors must still stop the build, is unchanged.

## Failure 4 — the Sporeling author had never run on Blender 5.0

```
RuntimeError: Expected production-creature-r4, got production-creature-r2
```

`assets/models/source/underworld-creature-sporeling.blend` was last written at `1ed1d89`
(2026-09-17 17:04); `72bea2c` and `8b59cb9` changed `tools/author-underworld-sporeling.py`
afterwards without regenerating it. The reason nobody regenerated it is that the author could not
run: Blender 4.4 moved an action's F-Curves into slotted channelbags and 5.0 removed
`Action.fcurves` entirely, so `72bea2c` — the commit that added the ten animation actions — raised
`AttributeError: 'Action' object has no attribute 'fcurves'` on every invocation against the
installed Blender 5.0.0.

Confirmed the replacement API against the installed Blender rather than assuming it: a probe that
inserts keyframes and introspects the result reports `has_fcurves_attr False`, one layer, one slot,
one `KEYFRAME` strip, and three F-Curves reachable through
`action.layers[].strips[].channelbags[].fcurves`.

Repairing the author exposed two further defects that had been masked behind it:

1. **The animation gate had never executed.** `3d4158d` wrote `REQ_ACTIONS - set(actions)` with
   `REQ_ACTIONS` a dict, which is a `TypeError`. It was never reached because the fidelity check
   above it always raised first on the stale blend. It is now `set(REQ_ACTIONS) - set(actions)`, and
   its `a.fcurves` check reads through the same channelbag-aware accessor.
2. **Every authored action was being discarded on save.** With the gate finally running it reported
   all ten actions missing from a file the author had just written. Each action loses its only user
   when the next one is assigned to the rig, and the last loses it when the rig is cleared; Blender
   does not write zero-user datablocks. They now carry a fake user.

Re-authored and verified: 41 mesh parts, 28 bones, 10 actions present in the saved file.

## Failure 5 — the review renderer had never run either

```
TypeError: bpy_struct: item.attr = val: enum "BLENDER_EEVEE_NEXT" not found in
('BLENDER_EEVEE', 'BLENDER_WORKBENCH', 'CYCLES')
```

`BLENDER_EEVEE_NEXT` was EEVEE's identifier only for Blender 4.2–4.5. The renderer added by
`0a728f1` and wired into local acceptance by `ffeed60` had therefore never produced a plate against
the installed Blender 5.0.0. The engine is now resolved from the enum rather than named for a
version, which is the shape `render-deep-fracture-caverns.py` already used. Twelve review plates
render for the first time into `artifacts/review/sporeling`.

## Result

`build.ps1 -Offline` now completes end to end, for the first time since 2026-09-17:

```
PASS: 44 icons decode as square RGBA >=128px with useful alpha silhouettes; 32 staff models
      carry a matching icon; coverage=1.5%-94%, box_fill>=22%, ink_density>=4%
VERIFIED Sporeling texture fidelity: 17 maps, 1024px source, 64px combat readability
VERIFIED Sporeling r4 source gate
RENDERED Sporeling r4 fidelity review: 12 fresh plates
PASS: 281 model assets imported twice
Magenheim.Core.Tests: 38320 assertions passed
Build succeeded. 0 Warning(s) 0 Error(s)
Verified 28 Harmony patch targets and named argument bindings against installed assemblies.
Verified 13 direct literal reflection bindings and 42 helper-wrapped field contracts
Built Magenheim 0.0.63
```

## The shape these failures share

A gate and a generator evolved while the committed output did not, and only a full build noticed —
on the fiftieth commit rather than the first. `verify-model-assets.py` already compares model
payloads against their sources; generated textures and icons have no equivalent freshness check. A
gate that regenerates into a temporary directory and compares hashes would have caught both asset
failures on the first commit. Recorded as an open item in `BACKLOG.md` P0.-2.
