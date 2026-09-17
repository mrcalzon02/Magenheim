# Crystal Weapon Scale and Surface Authoring — 2026-09-17

## Reconciled authority

`main` at `5842128`, matching `origin/main`. Raised by user directive: weapon scaling should
match vanilla, and the weapons should be reworked to the project's modelling standard for
texturing and detail.

## Defect 1 — the weapon family shipped oversized

Measured from the committed runtime models. Against a Valheim player of roughly 1.8m:

| weapon | before | after | weapon | before | after |
|---|---|---|---|---|---|
| knife | 1.25 | 0.93 | battleaxe | 2.08 | 1.56 |
| axe | 1.56 | 1.17 | bow | 2.46 | 1.85 |
| mace | 1.74 | 1.30 | spear | 2.65 | 1.98 |
| crossbow | 1.76 | 1.32 | greatsword | 2.78 | 2.08 |
| sword | 2.09 | 1.56 | atgeir | 3.02 | 2.26 |

`crystal-weapon-knife` was longer than a vanilla sword and `crystal-weapon-sword` longer
than a vanilla greatsword.

The correction is **observed, not inferred**, which matters given the P0.1 lesson that a
large body of repairs was made from inference. The staff family was reported in play as
reading correctly, and measures 1.80-2.96m with a 2.00m median; the sword was reported as
about a third longer than it needed to be. That gives a uniform 0.75 factor.

Weapons straddle the origin at the grip (sword Y spans -0.75..+1.34), so the scale is
applied about the world origin and the grip stays at the hand. `tools/rescale-weapon-models.py`
bakes the factor into the authored mesh data and the models are re-exported through
`export-model-assets.py`; no runtime scale was applied, so the sources are correct rather
than corrected downstream.

## Defect 2 — weapon surfaces were borrowed, not authored

Material names are spelled `magenheim.crystal-weapon.<model>.<intent>[.<n>].<family>`.
The retro-texture pass assigned the family essentially at random: **76 of 79 weapon
materials contradicted their own declared intent.**

```
grip           -> stone      5     blackmetal -> stone      8
grip           -> timber     5     blackmetal -> metal      9
crystal        -> stone      9     blackmetal -> carapace   2
crystal        -> metal      3     rainbow    -> stone     11
crystal-bright -> stone     13     silver     -> stone      1
```

Five shared 256px maps served all ten weapons, and they were the generic `creature-*` maps.

The family token is load-bearing twice over. It selects the packed albedo, and
`GeneratedSurfaceTextures.Classify` reads the same material name for keywords and returns
the **first** match, so a grip ending `.stone` was classified `SurfaceKind.Stone` for the
runtime fallback surface as well. Repairing the token repairs both paths.

`tools/author-weapon-textures.py` authors seven 512px families and binds each material by
intent, choosing family tokens the classifier resolves correctly:

| intent | family token | classifier kind |
|---|---|---|
| grip (bladed/ranged) | leather | Leather |
| grip (polearm haft) | timber | Timber |
| blackmetal | metal | Metal |
| silver | silver | Metal |
| crystal, crystal-bright, rainbow | crystal | Crystal |

Grain is deliberately fine and low-contrast. `refine-retro-textures.py` established why:
these UVs are smart-projected into many small islands, so any feature larger than an island
reads as patchwork across the seams — the exact regression reported from play at 0.0.52.
Family character comes from tone, tint and grain direction, not large shapes.

Where one model carries two parts of the same intent (the greatsword has two blackmetal
parts), the name is disambiguated with an index the way the data already does it for
`rainbow.1` / `rainbow.6`, giving `blackmetal.2.metal`. Without that, Blender resolves the
collision to `.001` and silently breaks the family token — this was caught and repaired
during the pass.

## Gates added

- `tools/verify-model-scale.py` — per-archetype longest-axis target plus the staff reference
  band. Proven to fail on the pre-rescale sword with a `+0.52m` drift.
- `tools/verify-weapon-materials.py` — intent/family agreement and the 512px albedo floor.
  Proven to fail on the pre-pass sword with both the mismatch and the resolution.

Both wired into `build.ps1`. Nothing had previously checked either property; neither
produces a compile error, an exception or a log line, and both are only visible in a
player's hand.

## Verification boundary

`build.ps1 -Offline` passes: 37,120 deterministic assertions, 0 warnings, 0 errors, 281
model asset sets, 25 Harmony patch targets, 13 literal plus 42 helper-wrapped reflection
bindings. 0.0.55 installed into the active Central Fuckery profile and the installed DLL
hash independently read back as `10AEE5B9...`, matching the build.

**Static validation only. No runtime claim.** The weapons have not been seen in a player's
hand against this build. Unverified: that the new lengths read correctly when held, that
the grip stayed at the hand through the rescale, and that the authored maps read at held
distance rather than flat.

## Not done

**Weapon geometry detail is not addressed.** The weapons remain box primitives inflated by
bevel modifiers — a sword blade is a 14-vertex box — at a median 1,262 triangles against a
library median of 2,348 and a staff median of 2,316. They are the lowest-detail family in
the library on the assets held closest to camera. This is the same defect shape as the
crystal progression items under P0.1 and needs genuine re-authoring of the blade, head and
haft forms, not another modifier.

## Next actionable slice

Re-author the weapon forms to the library standard, highest player contact first: sword,
axe, knife. `revise-model-library.py` has the established technique (`replace_geometry`,
`ribbon` for a tapered section through a designed centreline) but note that it re-unwraps
with `smart_project`, which is what created the island-patchwork problem; a weapon re-author
should carry a purpose-authored UV rather than inherit that.
