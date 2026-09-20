# Magenheim Backlog

Priority is dependency order. Broken intended behavior and repository divergence outrank new scope.

## P0.-2 — `main` did not build (raised 2026-09-18)

`main` at `df27a65` failed its own `build.ps1` six times over, and did not compile. Nothing
committed after those defects landed could have been built, packaged or installed, so the whole of
2026-09-18's work up to `df27a65` was unverified against the build. P0.1 requires a build before
push; that did not happen.

- [x] **`main` did not compile — five errors across Core and Runtime. FIXED 0.0.63.**
  `UnderworldMapRaster.cs` called `UnderworldMapPresentation.CellToLogical`, which has never
  existed; the method is `CellCenterToLogical`, and the wrong name appears exactly once in
  repository history, in `100619f` (08:12), the commit that introduced the caller.
  `UnderworldMapPresentationRuntime` read `UnderworldExplorationState.ExploredCount` twice, which
  did not exist either — it is now a real incrementally-maintained property on the Core state, since
  its purpose is to let the fog texture skip a rewrite without rescanning every cell, and computing
  it per frame in the runtime would defeat that. `UnderworldMapLayerRuntime` had two nullable-flow
  errors on the `identity` out-parameter, closed with the `|| identity is null` guard the rest of
  the runtime already uses. Six commits landed on top of the first of these.

- [x] **Icon gate was unsatisfiable for six staff icons. FIXED 0.0.63.** `verify-icon-assets.py`
  required 6% of the frame to carry alpha. All 32 staff icons are framed identically -- visible
  bounding box 78.5%-84.4% wide, 64.5%-69.9% tall -- but frame coverage runs 3.6%-9.9% because a
  staff is a hairline shaft and the tiers differ only in head mass. Six fell under the line, and no
  framing change could have lifted them: spirit-simple needs about 1.65x linear scale to pass, which
  crops the staff out of frame. The gate now measures three things instead of one -- frame coverage
  for a blank or filled-square render, visible bounding-box area for a subject rendered too small or
  off-centre, and ink density inside that box for an outline or ghost render. Proven on four
  synthetic probes that each trip a different rule, so the replacement is strictly more
  discriminating than the single floor it replaces, not a relaxation.
- [x] **Sporeling texture gate was tightened five times without regenerating the assets. FIXED
  0.0.63.** `gill-emission.png` and the other sixteen maps were last written at `33dc47a`
  (2026-09-17 16:25). Four generator commits and five gate commits landed after that, and
  regenerating from the authoritative generator today changed all seventeen files. The gate was
  being tuned against stale output. Regenerating alone did not fix it -- the generator itself could
  not satisfy its own gate, producing 0.09% hot pixels against a 0.2% floor -- so the glow gain was
  raised from 3.0 to 4.5 at the source, which is where the defect was.
- [x] **The Sporeling author had never run on Blender 5.0. FIXED 0.0.63.**
  `verify-underworld-sporeling` rejected `production-creature-r2` where the author writes
  `production-creature-r4`: the committed `.blend` was last written at `1ed1d89` (2026-09-17 17:04)
  and two author commits landed after it. The reason it was never regenerated is that the author
  could not run — Blender 4.4 moved an action's F-Curves into slotted channelbags and 5.0 removed
  `Action.fcurves` outright, so `72bea2c`, which added the animation actions, raised AttributeError
  on every invocation. Both the author and the gate now read F-Curves through
  `action.layers[].strips[].channelbags[].fcurves`, preferring the legacy collection when it exists.
  The animation gate added by `3d4158d` had also never executed: it did `REQ_ACTIONS - set(actions)`
  with `REQ_ACTIONS` a dict, which is a TypeError, hidden because the fidelity check above it always
  raised first on the stale blend. With that fixed the gate ran for the first time and caught a
  third defect: every action was authored and then discarded, because each loses its only user when
  the next is assigned to the rig and Blender does not write zero-user datablocks — they now carry a
  fake user. Re-authored and verified: 41 mesh parts, 28 bones, 10 actions actually present in the
  saved file.
- [x] **The Sporeling review renderer had never run either. FIXED 0.0.63.** `BLENDER_EEVEE_NEXT` was
  EEVEE's identifier only for Blender 4.2-4.5; 5.0 offers
  `('BLENDER_EEVEE','BLENDER_WORKBENCH','CYCLES')`, so assigning it raised a `TypeError` and the
  renderer added by `0a728f1` and wired into the build by `ffeed60` had never produced a plate. The
  engine is now resolved from the enum rather than named for a version — the same shape
  `render-deep-fracture-caverns.py` already uses. Twelve review plates render for the first time.
- [x] **Generated assets are now tied to the generators that own them. DONE 0.0.64.**
  `tools/verify-generated-freshness.py` plus `assets/generated.manifest.json` record, per generator,
  the SHA-256 of the generator itself and the set of files it owns. A changed generator, or a file
  added or removed outside the generator, fails the build; for reproducible generators a changed
  output does too. Verification is pure hashing — no Blender, no regeneration — so it runs first in
  `build.ps1`, ahead of the ten minutes of gates that used to be the only way to discover staleness.
  `--update` *runs the recorded command* before recording anything, so the manifest cannot be
  brought back into agreement without regenerating. Proven on both defect classes: an edited
  generator and a hand-edited output each fail, and the tree restores clean. No output-directory
  override was needed after all — recording hashes beside the generator answers the same question
  without touching a single generator.
  **Blender output is not reproducible, and the gate says so rather than pretending.** Re-rendering
  the 32 staff icons produced 20 files differing from the committed ones, but 16 of those differ by
  a single LSB on a handful of pixels — render jitter — and only 4 had really changed. An exact
  content hash would cry wolf on half the family after every render, and a gate that cries wolf gets
  refreshed blindly. Blender-backed entries therefore use `generator-only`: exact generator hash,
  recorded output names, no content hash, with the lost hand-edit detection stated in the tool.
  Pure-Python generators keep full content hashing — the Sporeling texture set is byte-reproducible
  across runs and is verified that way.
  **The gate found real staleness on its first run.** `staff-storm-crystal`, `staff-fire-crystal`,
  `staff-venom-crystal` and `staff-radiance-crystal` differ from what `render-staff-icons.py` now
  produces by about 10% of their pixels, with RGB deltas above 220 — not jitter. All four are the
  same tier across four families, which points at a model change those icons never picked up.
- [ ] **Extend freshness coverage to the rest of the generated library.** Covered today: Sporeling
  textures, the Sporeling source blend, the 32 staff icons. Not yet covered: `generate-earth-assets.py`
  (its outputs were last written 2026-09-14 and the generator has changed since, so seeding it means
  deciding whether ~90 committed files should be regenerated — a content decision, not a gate one),
  `generate-underworld-capcrawler-textures.py` (its output directory does not exist yet), and the
  fungal-forest and model-library generators.
- [ ] **Finish the Earth icon migration rather than merely raising its target.** The rebuilt tier
  family is now rendered at 256px, but six legacy 128px icons remain: the four workstation/process
  identities plus the geode/skill-side legacy set reported by the icon gate. Regenerate/re-author
  those from their authoritative models or dedicated source art, add each generator to the freshness
  manifest, then run `MAGENHEIM_ENFORCE_ICON_TARGET=1` so the 256px target becomes an acceptance
  invariant. Do not upscale the old 128px PNGs; that satisfies dimensions without adding readable
  information and defeats section 14's vanilla-fitness objective. Model payloads are already covered separately by
  `verify-model-assets.py`.
- [ ] **`build.ps1` compiles last.** Roughly ten minutes of Blender and asset gates run before the
  compiler is ever invoked, so a one-word CS0117 costs a full cycle to surface. A fast
  `dotnet build src/Magenheim.Core` at the top would report it in seconds. Low priority next to the
  freshness gate above, and it is a workflow improvement rather than a defect.
- [x] **`build.ps1` treated native stderr as failure under Windows PowerShell 5.1. FIXED 0.0.63.**
  With both asset defects repaired the build still died at the same line, this time with the gate
  reporting success: Pillow 12.3.0 deprecated `Image.Image.getdata`, and PowerShell 5.1 wraps every
  native stderr line in an ErrorRecord, which terminates the script under
  `$ErrorActionPreference = 'Stop'` even though the tool exited 0. `tools/blender.ps1` already
  documented the same trap for Blender. Fixed at both levels: a `flat(im)` helper prefers Pillow's
  `get_flattened_data` and falls back to `getdata`, added to all four tools that called the
  deprecated API (which Pillow 14 removes outright), and `build.ps1` relaxes the preference to
  `Continue` for the gate block only — where every call already checks `$LASTEXITCODE` and each
  `.ps1` gate throws — restoring `Stop` before the packaging section's cmdlets.

## P0.-1 — Underworld instance-world correction (TOP PRIORITY, corrected 2026-09-18)

The Underworld is a **dedicated Magenheim instanced world-space**, not a second manually selected save and not an x=40000 Surface-world continent. The 2026-09-17 horizontal-host-band correction over-corrected the earlier second-save implementation and is now superseded by current user authority and the corrected INSTRUCTIONS.md / UNDERWORLD_DESIGN.md §6.

Preserve the reusable authorities: six-biome terrain rules, deterministic layout, exploration serialization, progression, ecology, Conclave content and independent minimap textures. The per-player transition transaction/recovery subsystem is superseded and quarantined; only a thin Deep Gate transport adapter may replace it.

- [x] **Correct authority documents. DONE 2026-09-18.** INSTRUCTIONS.md and UNDERWORLD_DESIGN.md now make the dedicated instance-world boundary explicit and forbid the x=40000 far-landmass model as final architecture.
- [x] **Stop extending host-band pin projection. DONE 2026-09-18.** The temporary projected-pin presentation mutation is removed; native instance coordinates will feed the Underworld map.
- [ ] **Introduce the instance lifecycle authority.** Own instance identity, admission, activation/deactivation, logical origin, chunk address space and recovery without hot-swapping Valheim singleton World/ZNet state.
- [ ] **Move terrain generation behind an instance chunk provider.** Reuse UnderworldTerrainLifecycle rules, but stop generating the Underworld through distant Surface WorldGenerator.GetBiomeHeight/GetBiome columns.
- [ ] **Replace host-band placement.** Deep Gate entry/return, Conclave placement, structures and ecology must consume native instance anchors rather than UnderworldSpatialDomain.ToHostAnchor x=40000 placement.
- [ ] **Reconcile instance persistence.** Key terrain/chunk, exploration and world-object state to the parent-world + Underworld-instance identity. Do not persist Magenheim-owned player-layer/transition truth.
- [ ] **Multiplayer instance admission.** Server owns instance identity/configuration synchronization. Use Valheim player/network authority rather than reconstructing instance state from per-player transition records.
- [ ] **Runtime acceptance.** Disposable-world Surface -> Underworld -> Surface, interrupted entry recovery, save/reload below, reconnect, host/client and dedicated-server proof.


## P0.0 — Live play defects from the 0.0.52 session (TOP PRIORITY, raised 2026-09-16)

The first live acceptance pass against an installed build. These outrank P0.1 and everything
below. Items marked REGRESSION were introduced by the asset work in this session.

- [ ] **Geode still shows textureless, unsurfaced parts.** `geode-sample` reports five parts all carrying maps, so this is not a missing texture at the payload level. Needs a screenshot to identify which surface: candidates are `GeodeCore` seen through the crevices, the shaft interior, or a surface reading flat because its map has range but no legible structure under game lighting.
- [x] **Crystal Staff of Venom does not attack.** ROOT CAUSE FOUND, FIXED 0.0.54. Not an attack-binding defect at all: the Venom registrar never ran. `EarthStaffRegistrar` asked for `staff-earth-*.icon.png`, which has never existed in repository history, and `EarthAssets.Texture` resolves icons through `File.ReadAllBytes`. All eight staff registrars subscribe to `PrefabManager.OnVanillaPrefabsAvailable`, a multicast delegate, so the first handler to throw stops every handler after it. Bootstrap order is Fire, Frost, Storm, **Earth**, Venom, Radiance, Seidr, Spirit — the missing Earth icon removed 20 items across four families with no log line naming them. See `docs/validation/2026-09-17-staff-icon-registration-chain-repair.md`.
- [x] **Socket crystal descriptions must state per-slot effect.** DONE 0.0.53: A crystal's description needs to say what it does in a weapon, in armour and in a utility slot. Today the player cannot tell before committing the socket.
- [ ] **Crystal weapons and staves orient wrongly in the player's hand.** Almost certainly the authored up-axis: these are Y-up game-space sources, Blender is Z-up, and the exporter maps `(x, y, z)` to `(x, z, -y)`. The crystal tier models had exactly this defect and it was invisible to every gate. Check the attach transform against a vanilla weapon.
- [x] **Staves need icons derived from their current models.** DONE 0.0.54. `tools/render-staff-icons.py` renders all 32 icons from the same `.blend` the runtime mesh is exported from. Twelve never existed (Earth, Fire, Storm), three were corrupt, and seventeen predated the C1 rebuild. Fire and Storm set no icon at all and inherited the `StaffIceShards` donor icon; Radiance, Seidr and Spirit used a tinted generic `crystal` icon. All eight families now resolve `m_icons` from their own asset name.
- [ ] **All item descriptions must be mechanically informative and player-facing.** One clear statement of what the item does, in the player's language, not the implementation's.
- [x] **Staves must take damage and degrade like normal weapons and tools.** DONE 0.0.54. Staves clone `StaffIceShards`, which spends Eitr rather than durability, so the clones inherited none and never degraded. `Magenheim.Core/CrystalStaffDurability.cs` owns the pure tier -> service-life rule (Simple 150, +75 per tier) with fail-closed tier resolution; all eight registrars set `m_useDurability`, `m_maxDurability`, `m_durabilityPerLevel`, `m_durabilityDrain` and `m_useDurabilityDrain`. The field set was read off the installed `SharedData` by reflection, not assumed. Live wear/repair acceptance is still open.
- [ ] **REGRESSION: crystal buildables are texturally broken.** *Claimed fixed in 0.0.53; live play on 2026-09-18 shows it is not.* See the 2026-09-18 section above for the cause, which is the runtime `GeneratedSurfaceTextures` sweep rather than the retro-texture pass alone. Original 0.0.53 note follows. Introduced by the generic retro-texture pass. Models whose UVs were smart-projected carry packed islands, so a coherent noise map lands as discontinuous patchwork across island boundaries. Models with purpose-authored UVs (geode, crystal tiers, caverns, passages) are unaffected. Either make the retro map fine-grained and low-contrast so island seams cannot read, or revert the pass on the affected families.

### Raised 2026-09-18 — live play against installed 0.0.64 (user report)

First runtime evidence since 0.0.52. The build loaded clean: `Magenheim 0.0.64` in the log, **zero
Magenheim errors or warnings**, all staff and projectile prefabs registered, no exceptions during
play. Surface play only — Meadows and Black Forest, Day 1 — so **nothing about the Underworld
region, the biome sector, the map or travel was exercised**, and 0.0.63's sector repair remains
entirely unverified.

- [x] **Six of eight Master staves do nothing; Radiance works. ROOT CAUSE FOUND, FIXED 0.0.68.**
  `Attack.ProjectileAttackTriggered` fires a single-burst projectile attack synchronously inside the
  animation trigger and defers anything with more than one burst to `Attack.UpdateProjectile`, which
  only runs while the attack is live; `Attack.Update` calls `Stop()` the moment `InAttack()` goes
  false, setting `m_attackDone`. Every staff clones `StaffIceShards`, a single-shot staff whose
  animation ends right after its trigger, and no registrar set `m_loopingAttack`, so a staff authored
  for 3-6 deferred bursts had no window to fire them. The split is exact: bursts==1 fires
  (Radiance, Earth), bursts>1 does not (Fire/Storm/Venom 3, Seidr 4, Frost 6). Stamina drained
  regardless because `Attack.Update` charges it up front when `m_perBurstResourceUsage` is false,
  which every staff sets -- which is why it read as "costs stamina, does nothing". `StaffBurstContract`
  folds the burst count into the projectile count so each family's authored bolt count, damage,
  damage type, spread, velocity and payload are untouched and the volley reaches the code path that
  works. A damage rebalance attempted earlier in the session was wrong and was reverted in full.
- [ ] **Restore the staggered burst cadence.** 0.0.68 releases each staff's volley in one shot. The
  authored stagger needs `m_loopingAttack` plus an attack animation that holds open while the
  sequence plays. A looping attack that never self-terminates leaves the player stuck mid-swing, so
  this needs its own design pass rather than a flag flip.
- [ ] ~~Six of eight Master staves do nothing~~ original note: User: Storm, Venom, Spirit, Fire
  and Frost deplete stamina with no visible effect; Earth fires visible projectiles that do nothing;
  Radiance "works quite well against the undead". Established from the installed assemblies and the
  log, so these are *not* the cause: the weapon's damage does reach the projectile
  (`Attack.FireProjectileBurst` sets `hitData.m_damage = weapon.GetDamage()` and `Projectile.Setup`
  copies it over the prefab's own `m_damage`, so `StaffEffectPayloads.CreateProjectile` zeroing
  `m_damage` is harmless); stamina is consumed *inside* `FireProjectileBurst` before the projectile
  is instantiated, so the burst ran and a projectile object was created; and nulling the projectile's
  `m_statusEffect` is safe because `Projectile.Awake` guards it before hashing.
  **Leading hypothesis is the balance data, not the plumbing.** Radiance Master is an outlier on
  every axis against the five that fail:

  | Master staff | damage | × multiplier | effective | velocity | spread° | bolts × bursts |
  |---|---|---|---|---|---|---|
  | Radiance | 18 pierce + 38 spirit | 1.00 | **56** | **66** | **0.12** | 1 × 1 |
  | Earth | 20 blunt | 0.92 | 18.4 | 26 | 3 | — |
  | Spirit | 16 | 0.40 | 6.4 | 44 | 10 | 4 × 3 |
  | Fire | 9 | 0.70 | 6.3 | 34 | 9 | 3 × 3 |
  | Frost | 5 | 0.75 | 3.75 | 40 | 7 | 2 × 6 |
  | Venom | 8 | 0.35 | 2.8 | 26 | 16 | 3 × 3 |

  A 2.8-damage bolt at 26 m/s with 16° of scatter, under the gravity inherited from the
  `StaffIceShards` projectile, lands short and wide of anything aimed at. That reads exactly as
  "depletes stamina, does nothing", and Earth's higher damage and 3° spread reads as "fires
  projectiles that do nothing". **Not proven.** What it does not explain is why five families show
  no *visible* projectile at all. Settle it with one instrumented run: log the resolved attack
  configuration per staff at registration, and a line per shot, then read it back.
- [x] **Crystal placeables are still texturally broken in game while correct in preview. FIXED
  0.0.66.** Measured: 326 of 1979 owned model materials — 16.5% — were classified by their model
  id rather than their own surface, because material names embed the id and `Classify` matches
  substrings. 119 Metal read as Stone, 67 Crystal read as Stone, 24 Crystal read as Cloth. The
  classifier now uses the trailing semantic token with an instance index dropped. The authored-UV
  half of the problem is separate and still open below.
  Original cause note:** The model preview and the game do not use the same material path.
  `render-model-catalog.py` renders the authored Blender materials through the authored UVs.
  At runtime, `GeneratedSurfaceTextures.RepairOwnedVisuals` — hooked to
  `PrefabManager.OnPrefabsRegistered` — sweeps **every** Magenheim-owned material in memory and
  replaces `mainTexture` with a procedurally generated 256px surface, at `mainTextureScale = 1`,
  chosen by keyword-matching the *material name* through `Classify`. Two consequences, both visible:
  a coherent generated texture sampled through packed smart-projected UV islands reads as patchwork
  across every island seam; and the keyword table mis-assigns families, because `Classify` maps
  `rim`, `band`, `collar`, `brace` and `rail` to Metal and `core`, `focus`, `light` and `growth` to
  Crystal, so a material's *name* decides its surface rather than its intent. This is the same
  defect that 0.0.55 fixed for weapons — 76 of 79 weapon materials contradicted their declared
  intent — by authoring real texture families and rewriting the family token so the runtime
  classifier agrees. **The placeables never received that treatment.** The 0.0.53 entry above
  claiming crystal buildables were fixed is therefore wrong, and is corrected here.
- [ ] **Geode: the inner open face pushes through the outer shell around the sides.** The crystal
  interior geometry breaches the silhouette of the closed shell rather than being contained by it.
- [ ] **Geode size variation has regressed.** Placement scale variety was present in earlier builds
  and is now inconsistent. Check the placement definition's scale range against what the worldgen
  registrar actually applies.
- [ ] **Geode interior material needs far stronger per-biome colour separation.** The interiors do
  not read as biome-distinct at play distance.

### Raised 2026-09-18 — second live pass, 0.0.64 (user report)

Reported good: axe, battleaxe and bow read correctly in hand; the Crystal Sentinel build and model
read correctly; the banners read correctly; the Crystal Hearth is much improved and working.

- [ ] **The held-model gates measure self-consistency, not correctness. CAUSE FOUND.**
  `verify-held-model-orientation.py` and `verify-held-model-grip-direction.py` assert a convention —
  long axis local +Y, grip at −Y, working end at +Y — that has never been validated against how
  Valheim actually presents a held item. Every weapon passes both gates, and more than half are
  visibly wrong in hand, in *different* ways. Measured from the committed runtime payloads:

  | weapon | in hand | long axis | X | Y | Z | grip/work vertex mass |
  |---|---|---|---|---|---|---|
  | axe | correct | Y | 0.50 | 1.17 | 0.12 | 1260 / 1776 |
  | battleaxe | correct | Y | 0.90 | 1.56 | 0.14 | 1260 / 3775 |
  | bow | correct | Y | 0.29 | 1.85 | 0.10 | 972 / 972 |
  | crossbow | **backwards** | **X** | 1.32 | 0.93 | 0.17 | 864 / 864 |
  | greatsword | **sideways** | Y | 0.54 | 2.08 | 0.23 | 420 / 888 |
  | knife | **wrong** | Y | 0.19 | 0.93 | 0.13 | 498 / 545 |
  | mace | **wrong** | Y | 0.41 | 1.30 | 0.42 | 1092 / 2688 |
  | spear | **wrong** | Y | 0.15 | 1.98 | 0.14 | 420 / 612 |
  | sword | not reported | Y | 0.31 | 1.56 | 0.14 | 156 / 1092 |
  | atgeir | not reported | Y | 0.59 | 2.26 | 0.16 | 420 / 774 |

  Three specific findings fall out. **The crossbow is exempt from both gates** — the orientation gate
  explicitly exempts bows and crossbows from the long-axis test, and the grip gate's name list omits
  both — so nothing has ever checked which way it faces, which is why it points its rear at the
  target. Its long axis is X while every other weapon uses Y, and it is centred on that axis, so no
  forward direction was ever established. **The knife is near-symmetric** at 498/545, a ratio of
  1.09 against the grip gate's 1.30 reversal threshold, so it is reported ambiguous rather than
  failed and passes by default. **Roll is unconstrained**: greatsword reads sideways, and nothing in
  either gate constrains rotation *about* the long axis, so blade-plane orientation is untested.
  The fix is not another geometry gate against the same assumed convention. The convention itself
  has to be established from a vanilla weapon's attach-space orientation first, then the sources
  re-authored against it, then gated.
- [x] **Crystal Hearth flame is far too large. FIXED 0.0.65.** `CrystalArchitectureVisuals.Apply` loads the model
  with `preserveParticles: true`, which retains the donor's particle systems untouched while the
  visible mesh is replaced. Nothing reconciles the retained particle scale or `startSize` with the
  Magenheim hearth's footprint, so the flame is sized for the donor rather than the piece.
- [x] **Crystal Hearth flame reads red, not rainbow. FIXED 0.0.65.** `TintVanillaFlame` puts the
  seven-key spectrum on `colorOverLifetime`, so the gradient is swept across each particle's *life*
  rather than distributed across the particles present at any instant. Every particle is born red
  at key 0, reaches green only at 33% and blue at 50%, while the alpha keys hold full opacity to 75%
  and fade to zero by 100% — so each particle spends its bright phase in the red-to-green half and
  dies out through blue, purple and pink. The fire therefore reads predominantly red with occasional
  green, which is exactly what was reported. Distribute the spectrum across particles — a random
  `main.startColor` between two gradients — instead of sweeping it over one particle's lifetime.
- [ ] **Crystal Sentinel does not shoot skeletons in default target-everything mode.**
  `CrystalSentinelRegistrar` sets no targeting field at all: `m_targetPlayers`, `m_targetTamed`,
  `m_targetEnemies`, `m_configTargets` and `m_targetCharacters` are inherited wholesale from
  `piece_turret`, and `CrystalSentinelVisuals.Apply` uses `hideOriginal`, which disables renderers
  only and leaves `m_eye`, `m_turretBody` and `m_turretNeck` intact, so the aiming rig is untouched.
  This is therefore vanilla Ballista behaviour unless something else interferes. One cheap test
  settles which: place a vanilla Ballista in the same spot with the same skeletons. If it also
  ignores them, this is not ours; if it engages them, the difference is in our clone.
- [ ] **Crystal weapons still carry generated placeholder textures.** Reported against the staff
  approach, which is considered correct. Needs clarifying whether "the snapshotting the staffs are
  using" means their Blender-rendered icons or their material treatment, because the remedy differs:
  `render-staff-icons.py` renders icons from the same `.blend` the runtime mesh is exported from,
  while `author-weapon-textures.py` generates weapon surfaces procedurally in Pillow.

### Raised 2026-09-17 — custom weapon scale and fidelity (user directive)

- [x] **Custom weapon scale does not match vanilla.** DONE 0.0.55. Measured from the committed runtime models, every weapon is oversized and the family is internally inconsistent. `crystal-weapon-knife` is 1.25m along its longest axis — longer than a vanilla sword — and `crystal-weapon-sword` is 2.09m, longer than a vanilla greatsword. Measured longest axis: knife 1.25, axe 1.56, mace 1.74, crossbow 1.76, sword 2.09, battleaxe 2.08, bow 2.46, spear 2.65, greatsword 2.78, atgeir 3.02. A held weapon is the most-looked-at asset in the game, and the player character is roughly 1.8m, so these read as props rather than weapons. Rescaled all ten authored sources by 0.75 about the world origin and re-exported through `export-model-assets.py`. The factor is the one observed in play: the staff family reads correctly at 1.80-2.96m and the sword was about a third longer than it needed to be. Sword is now 1.56m, knife 0.93m, atgeir 2.26m. `tools/verify-model-scale.py` gates the declared per-archetype target and the staff reference band, and is wired into `build.ps1`; proven to fail on the pre-rescale sword with a +0.52m drift.
- [ ] **Weapons are the lowest-detail family in the library.** Median 1,262 triangles against a library median of 2,348 and a staff median of 2,316 — roughly half the library standard, on the assets held closest to camera. This is the same defect shape as the crystal progression items under P0.1 (lowest-poly assets despite highest player contact) and needs the same treatment: re-author to the library standard with silhouette detail that survives at held distance.
- [x] **Weapon texturing is at the library floor, not vanilla's.** DONE 0.0.55. Every family samples at 256px albedo, so weapons are not behind the rest of Magenheim, but they are behind vanilla, whose weapon albedos carry materially more surface information at held distance. The real defect was worse than resolution: the retro pass assigned surface families essentially at random, so 76 of 79 weapon materials contradicted their own declared intent. A sword grip was textured as stone, its crystal as metal, and a greatsword's blackmetal as carapace, on five shared 256px maps. The family token is load-bearing twice over, because `GeneratedSurfaceTextures.Classify` also reads it to pick the runtime fallback surface. `tools/author-weapon-textures.py` now authors seven 512px families (leather, timber, blackmetal, silver, crystal, crystal-bright, prismatic) and binds each material by intent, rewriting the family token so the runtime classifier agrees. `tools/verify-weapon-materials.py` gates it.

### Raised 2026-09-17 — placeable collision audit

- [x] **Crystal Banners (24 pieces) have no owned collision.** DONE 0.0.58. Measured from the loaded meshes rather than hand-written constants, so it cannot go stale when the model library is rebuilt, and narrowed to the pole so a banner does not block a player beside it. **The Crystal Sentinel is deliberately not included:** it is a working turret whose colliders serve targeting and interaction, and replacing them blind risks breaking a feature that nobody has reported as broken. It needs a live check first.
- [ ] **Crystal Sentinel collision.** Every other placeable family disables the donor's non-trigger colliders and installs fitted Magenheim boxes via a `ConfigureCollider(s)` pass. `CrystalBannerRegistrar` and `CrystalSentinelRegistrar` contain no collider code at all, so they clone `piece_banner01` and `piece_turret` and keep the *donor's* collider while `ModelAssets.Load` replaces the visible mesh. `hideOriginal` disables Renderers and LODGroups only — it does not touch colliders — so the piece collides with the donor's shape, not the model the player sees.
- [ ] **No placeable carries model-derived collision.** Only 25 of 281 models declare any `collider` part, and all 25 are Deep Fracture districts/passages, the Dark Throne and the geode. Every buildable depends entirely on hand-authored box constants in C#. That is a legitimate approach and matches vanilla, but nothing ties the constants to the models.
- [ ] **Gate placeable collision against model bounds.** The authored furniture boxes were checked this session against the committed model bounds and all ten pieces match on every axis, including base alignment — so this is prevention, not repair. The constants are hand-written and the model library has been rebuilt repeatedly; nothing would catch the next drift. `FurnitureRegistrar.ConfigureColliders` also has a `default:` branch that gives any unmatched model a generic 1m cube, which would ship silently.

### Raised 2026-09-17 — socket station ownership (user directive)

- [x] **Move socketing off the Geologist's Workstation.** DONE 0.0.56. The Workstation carries the mod's whole recipe list (14 configs resolve to it), and the socket surface is hosted inside `InventoryGui`, so it drew over that station's crafting menu. The Crystal Enchanting Dais already carried no item recipes — only two `PieceConfig` entries gate Hammer placement near it — and is registered with `m_showBasicRecipies = false`, so it is the correct host. Client UI and server RPC authority moved together.
- [x] **Decouple socket removal from the refinement chain.** DONE 0.0.56. `SocketExtractionService.RequiredStationId` was `Magenheim_StationUpgrade_FacetingWheel`. The Faceting Wheel is a Workstation upgrade whose real job is gating Crystal -> Advanced refinement; that one shared constant made socket removal look like a refinement step and tied socketing to the wrong station. Removal is a socket operation — the third beside opening a slot and installing — and the geode/refinement route never removes anything from a socket. The Dais is now the gate; the Faceting Wheel keeps its refinement role unchanged.
- [x] **Delete the additive Dais Harmony patch.** DONE 0.0.56. `CrystalEnchantingDaisSocketPatch` existed only to admit the Dais *alongside* the Workstation through three postfixes. With the Dais primary the behaviour belongs in the authority, not in a postfix over it. Harmony patch targets drop 25 -> 22.
- [x] **Item descriptions follow the station split.** DONE 0.0.57. Every crystal closes with "Socket at the Crystal Enchanting Dais", generated in the same Core authority as the per-slot effect lines so it cannot drift. Rough crystals are sent to refining instead, because they cannot be socketed at all. The Dais and Workstation descriptions each state what they own.
- [x] **Disallowed items must not appear in the socketing list.** DONE 0.0.57. The list previously admitted any item carrying Magenheim socket metadata, so an item the policy had since disallowed still appeared and could not be acted on. Only eligible equipment is listed now, with one deliberate exception: an item that still holds installed crystals stays listed and marked `[removal only]`, because hiding it would strand the player's crystals inside it. Empty leftover metadata is filtered out with the rest.
- [ ] **Rebuild the socket menu now that it owns the panel.** The whole point of the move. `CrystalDaisSocketOverlay` still lays out a flat vertical button list designed for a cramped shared surface: one button per installed crystal, one per candidate crystal in inventory. With a dedicated station it can show the equipment and its sockets as a real layout — slot states, what each crystal would do in that slot, and the break risk before committing.
- [ ] **Live-confirm the split.** Unverified: that the Dais opens the socket surface, that the Workstation's crafting menu is now unobstructed, and that removal works without the Faceting Wheel.

### Raised 2026-09-17 — Underworld ecology and progression parity (user directive)

Full plan: `docs/UNDERWORLD_FLORA_TERRAIN_PLAN.md`.

- [ ] **The Underworld has architecture but no economy.** Six biomes, six bosses, six Deep Boons and twenty excellent districts exist, and the entire realm has two raw materials: `understone` and `worldroot_timber`. Nothing grows, nothing is gathered, nothing is crafted. It reads as a place to visit rather than a place to live.
- [ ] **Each core biome must carry a full overworld-parity tier.** The governing principle: every overworld biome hands the player raw materials, a refining step, a crafting station, a tool that opens a new interaction, an armour set, a weapon line, a food chain and a building material. Six Underworld biomes must each do the same. A biome that gives the player no reason to make camp is scenery.
- [ ] **Underworld progression is built on different verbs.** The overworld progresses by hitting harder; the Underworld progresses by surviving deeper. Hazard resistance, light, breath, traversal and anchoring are its tiers, which is also what keeps the Deep Boons meaningful: the boon is the innate version, the armour is the craftable version, and they stack.
- [ ] **Custom fungal trees.** Eight species across the biomes, five models each. Not recoloured vanilla trees: a fungal tree has no branches, no bark grain and no leaf card, and cloning a Beech produces a green Beech.
- [ ] **Three Underworld stone sets.** Understone, Blackwater Flowstone and Slagstone, roughly 24 pieces each, snapping to each other and to the existing Worldroot timber set. Three genuine sets are a palette; one set with three tints is a swatch.
- [ ] **Six foundational stations.** Mycelial Bench, Tidal Basin, Furnace Heart Forge, Silence Table, Anchor Forge, Crown Reliquary. Each gates the next biome's crafting, and each is sited by its biome (water, vent, stable ground) so the world dictates where a base can go.
- [ ] **Decide the portal question before building the economy.** If Underworld portals exist the realm becomes convenient and loses its weight; if they do not, every trip is a committed expedition and the Skiff matters more. This one decision shapes the entire material economy and should be settled first.
- [ ] **Start at F1, the Fungal Forest.** ~40 models for a complete tier. It is the biome the player meets first, it has the clearest identity, and it is the smallest complete proof that an Underworld biome can carry a full parity tier.

### Raised 2026-09-17 — Underworld model revision

- [x] **Underworld structural landmarks.** DONE 0.0.61 for three of four. Standing stone 76 -> 332, dais 572 -> 956, Deep Fracture entrance 384 -> 864, chamfered to read as cut stone under low light.
- [ ] **Dark Throne ships at a fifth of its authored fidelity, and its source has inverted faces.** Its exported payload is 880 triangles while the source evaluates to 4,080: the export is stale, so the boss arena centrepiece has been shipping far below what was authored. Re-exporting it is blocked, because the fresh export immediately fails `verify-model-assets` with four inverted faces on `Dais_Step_1` that the stale payload was hiding. `recalc_face_normals` does not fix them, which suggests that part is not a closed solid and outward cannot be inferred. It needs inspection in Blender, not another automated pass. Reverted to the stale export for now so the defect is not traded for more triangles.
- [ ] **Re-author the Deep Fracture passage and traversal.** At 744 and 352 triangles they connect districts of roughly 10,000, and the corridor is the first thing seen after a chamber. A chamfer pass produces no geometry on either, which means there are no hard edges to cut: the forms themselves are too simple and need authoring, not refinement. This is the same finding the 2026-09-16 pass recorded, and the gap has closed only partially since.

### Raised 2026-09-17 by the staff registration investigation

- [x] **Gate the icon payload.** DONE 0.0.54 as `tools/verify-icon-assets.py`, wired into `build.ps1`. Walks every icon's PNG chunk stream verifying CRCs and decompressing the pixel data, requires every staff model in the catalog to have a matching icon, and resolves every literal `EarthAssets.Icon("x")` against disk. Proven to fail on the pre-repair tree with exactly the three real defects and nothing else.
- [x] **Declare binary assets to Git.** DONE 0.0.54. The repository had no `.gitattributes`, so nothing stopped Git applying text or EOL conversion to PNG, blend, glb, zip or dll payloads. Three committed staff icons carry bad IDAT CRCs or truncated chunks in the committed blobs, not merely in a worktree.
- [ ] **One content registrar throwing still removes every registrar after it.** The icon gate closes the trigger found this session, but not the class. All content registrars share `PrefabManager.OnVanillaPrefabsAvailable`, so any single failure silently deletes unrelated content and the player sees "this item does not work" rather than a named failure. Deciding the right failure semantics is a design call: per-registrar isolation would keep the rest of the mod alive but risks admitting a half-registered world, which the project's fail-closed doctrine otherwise rejects. Do not fix this by swallowing exceptions.
- [ ] **Audit the other asset-name contracts for the same defect shape.** `staff-earth-*` was requested for months and never existed, because nothing resolved asset names until build time. The icon gate now covers `EarthAssets.Icon`. Model, texture, mesh and prefab name lookups that resolve only at registration time have not been audited for the same class.

## P0.1 — Asset fidelity, live acceptance and geometry gating (TOP PRIORITY, raised 2026-09-16)

These outrank every item below, including P0.4 and P0.5 remnants and all new content.
Measurements are from the committed library at `924f5e7`; evidence and method are in
`docs/RENDERING_AND_CONTENT_DEFECTS.md`.

- [ ] **Run one disposable-world acceptance session.** Everything since 0.0.16 is recorded as "source repaired, live acceptance pending". A large number of defects were repaired from *inferred* behaviour rather than observed behaviour, and nothing currently distinguishes the correct inferences from the wrong ones. This is the cheapest highest-value action available and it gates honest claims on all the rest.
- [x] **Rebuild the geode.** DONE 2026-09-16. `geode-sample` is a single model shared by all eight biome geodes and it is actively broken, not merely dated: `stone-cavity` has 246 of 246 faces pointing inward and all five `interior-crystal-*` parts are 63-73% inverted, every one with negative signed volume. The reported "transparency" is back-face rendering; the atlas carries no alpha and the material is forced opaque. Rebuild the exterior as fractured Voronoi plates with deep crevices over a darker inner mass, and the exposed face as concentric agate banding plus a dense inward-pointing druzy field, authored greyscale. Preserve the existing runtime contract: `GeodeVisuals.Apply` tints any material named `magenheim.geode.interior-*` and lightens `interior-bright-*`, so biome shading needs no runtime change.
- [x] **Gate inverted faces across the whole model library.** DONE 2026-09-16 as `tools/verify-model-geometry.py`; it immediately caught inverted stalactites and columns in all twenty rebuilt districts. This defect class has now appeared three separate times: the copied prism/cylinder builders, the geode cavity, and the geode crystals. It produces no compile error, no exception and no log line, and is invisible until a player looks at the wrong side of a surface. Promote the ad-hoc check that found the geode into a permanent gate over all 281 models, in the manner of `tools/verify-deep-fracture-caverns.py`. Note that exported vertices are unwelded, so edge-pairing tests are uninformative; the face-normal-versus-centroid test is the one that holds.
- [x] **Close the texture gap.** DONE 2026-09-16: 281/281 models and 3,681/3,681 parts carry an albedo map, from 111/281 models and 36% of parts at `924f5e7`. A tonal-range gate in `verify-model-assets.py` keeps a map that carries no image from passing.
- [x] **Develop the crystal progression items.** DONE 2026-09-16. The items the entire mod is built around are the lowest-poly assets in the library: `earth-simple` is 36 triangles, `earth-rough`/`earth-crystal`/`earth-shards` are 108, and `earth-advanced`/`earth-master` are 180. The player handles these constantly across the whole refinement loop.
- [x] **Bring the Deep Fracture passages up to the districts.** DONE 2026-09-16. `deep-fracture-passage` is 60 triangles (five boxes) and `deep-fracture-traversal` is 172. They now connect districts of roughly 10,000 triangles each. The corridor is the first thing seen after a chamber, so the mismatch is immediately legible.
- [ ] **Compile before pushing.** Eighteen compile errors arrived on `main` in one batch and two more of the identical class followed hours later, after that class had already been repaired six times on the same branch. Running `build.ps1` before push catches all of it; nothing more elaborate is required. **It happened again:** on 2026-09-17 `main` at `5aa8ab0` carried eight unbuilt compile errors, including a seventh `netstandard2.0`/`net462` BCL-availability defect (`Math.Clamp`, `string.Contains(string, StringComparison)`). Repaired in `72b0652`. **It happened again on 2026-09-19:** `origin/main` at `7776fce` failed `build.ps1` twice over -- `verify-model-assets.py` rejected `UnderworldInstanceChunkMaterializer` for building a mesh in C#, and `verify-runtime-type-availability.ps1` found `System.ValueTuple` referenced through the chunk streaming focus list, the defect class that silently removes every registrar behind it. Both repaired in `689a3a3` and `9f15cd5`. Note what these two have in common: the guards exist and both fired correctly, but they only run inside `build.ps1`, roughly eight minutes in, which is downstream of the push. A guard that fires after the code has left the machine records the mistake rather than preventing it. Tuple syntax in `Magenheim.Runtime` is detectable from source in under a second without compiling anything; a check that cheap can sit in a pre-commit hook, where it would actually bite. This item stays open until a push actually runs the gate.

- [ ] **A structural crystal fuse that accepts genuinely mixed shards.** 0.0.85 registers eight
  fusing recipes, one per alignment, because Valheim's crafting UI cannot express "any four shards"
  as a single requirement and `WorkshopOperations` is built around a single source item
  (`ItemDrop.ItemData source`, `consumeAmount`) rather than a multi-input transaction. The player
  can use whichever alignment they have, but one craft still draws on one alignment, so eight
  single-shard leftovers cannot be swept into a block. Closing this means extending the
  server-authoritative transaction layer to multiple inputs, which is worth doing on its own merits
  and should not be bolted onto the item.

## P0 — repository and pure-core foundation

- [x] Reconcile false bootstrap claims against committed repository reality.
- [x] Establish authoritative instructions, project state, design, backlog, and validation records.
- [x] Reconcile divergent `main`/`master` histories without force-pushing or discarding ancestry.
- [x] Remove committed unresolved-merge text from the live authority by constructing an explicit resolved consolidation tree.
- [x] Preserve material legacy design work without making stale content authoritative.
- [x] Restore canonical crystal/refinement domain authority with Rough -> Simple -> Crystal -> Advanced -> Master.
- [x] Retain additive-only spawn-area validation and non-destructive worldgen planning from divergent work.
- [x] Add deterministic standalone tests for refinement and worldgen planning.
- [x] Add an `IsExternalInit` compatibility shim for record-based core models.
- [x] Align `Magenheim.Core` to `netstandard2.0` so it can be consumed by the .NET Framework 4.6.2 Valheim/Jötunn runtime while remaining consumable by the .NET 8 test harness.
- [x] Reject negative spawn-area integers from clamping so sign extension cannot silently become All.
- [x] Add configuration parsing for Median/Edge/All plus the Jötunn-facing `Everywhere` alias.
- [x] Detect prefab collisions as well as registration-key collisions without mutating observed data.
- [x] Add compatibility exclusions by Magenheim registration key/prefab and optional case-insensitive identity matching.
- [x] Move worldgen compatibility policy into schema-versioned definition authority and include it in the deterministic fingerprint.
- [x] Make configured invalid-area behavior govern geode area parsing rather than hard-coding Reject before policy admission.
- [x] Restrict compatibility exclusions to Magenheim-owned registration/prefab namespaces so configuration cannot target foreign content.
- [x] Make definition duplicate detection and exclusion normalization obey the configured exact/case-insensitive identity comparer.
- [x] Canonicalize case-insensitive compatibility exclusions in the definition fingerprint while preserving exact-mode casing authority.
- [x] Reconcile the former `main`/`master` split and make `main` authoritative.
- [x] Delete redundant remote refs. Four existed, not three: `radiance-content`, `tmp-radiance-content`, `__delete_me__` and `tmp-compat-work`. All four pointed to `fc84a519`, not the `a8b6947c` recorded by the earlier connector-only cycle. Containment in `origin/main` was re-verified immediately before deletion. `git ls-remote --heads origin` now returns `refs/heads/main` only.
- [x] Advance definition schema to 3 so geode placement behavior is validated, fingerprinted, and synchronized with the rest of gameplay/worldgen authority.
- [x] Add server-side BepInEx overrides for fingerprinted geode placement fields and route them through full definition revalidation before use.
- [x] Harden runtime `All` area mapping so `Everything` / `Everywhere` aliases are admitted only when semantically identical to distinct non-empty `Median | Edge` runtime components.
- [x] Execute the standalone tests with a .NET 8 SDK and record the observed result for the earlier admitted build.
- [x] Compile `Magenheim.Core` with warnings as errors for the earlier admitted build.
- [x] Add the thin BepInEx/Jötunn plugin bootstrap on `main`, with hard Jötunn dependency and everyone-must-have network declaration.
- [x] Compile the runtime project against Jötunn 2.30.0 and a current Valheim development environment for the earlier admitted build.
- [x] Rebuild accumulated source as 0.0.45; full core harness passes 13,974 assertions and runtime compiles without warnings/errors. See CLOSEOUT.md.
- [x] Add strict schema-validated static definition loading with deterministic definition fingerprinting and hard failure on malformed/unknown content.
- [x] Add controlled balance/compatibility/world-placement overrides on top of the validated static definition snapshot; effective authority is always revalidated and re-fingerprinted.
- [x] Test actual runtime JSON loader failure cases in the .NET harness, including Underworld required fields, duplicate/unknown properties and invalid references.
- [x] Add deterministic schema/fingerprint authority comparison with fail-closed mutation admission.
- [x] Register a two-way Jötunn gameplay-authority handshake and reset cached admission per sync session.
- [x] Include socket eligibility/configuration policy in the synchronized gameplay fingerprint so peers with different socket rules cannot authorize persistent mutations.
- [x] Assign a strictly increasing server-local session generation and retire prior operation replay records for reused peers.
- [x] Bind authority acknowledgement plus workshop/socket request-response-acknowledgement traffic to that server-issued session generation, reject stale-session traffic fail-closed, retire prior-generation client pending records, and preserve unapplied live queue entries. See `docs/validation/2026-09-15-session-generation-handshake-binding.md` and `docs/validation/2026-09-15-session-bound-mutation-rpcs.md`.
- [ ] Compile and execute the updated gameplay-authority synchronization path in a current Valheim/Jötunn environment and repair any API/serialization defect before runtime admission.
- [x] Register Crystal Shaping under permanent ID `magenheim.crystal_shaping`.
- [ ] Confirm the live console diagnostic `raiseskill magenheim.crystal_shaping 1`; the spaced display name is not a valid `raiseskill` argument.

## P0.5 — Valheim 1.0.12 compatibility repair

- [x] Reconcile origin/main's Broken Crown encounter-reset commits with the local 1.0.12 API pass without overwriting either intent.
- [x] Replace `Character.SetPos` and `Rigidbody.velocity`, both removed or superseded by Valheim 1.0.12, with the installed API.
- [x] Repair the `Inventory.Changed` binding: 1.0.12 declares `Changed(bool, bool)` and three call sites invoked it with zero arguments, throwing `TargetParameterCountException` in workshop rollback and local/remote socket mutation. Bound at the `RuntimeGameApi` boundary with a pinned signature.
- [x] Add `tools/verify-reflection-targets.ps1` so literal reflection bindings are resolved against installed assemblies at build time; proven to fail on the pre-repair 0.0.48 DLL.
- [x] Extend reflection verification to helper-wrapper bindings for `Aoe`, `Projectile`, `Destructible`, `ZNetView`, and `SE_Stats`, including exact type checks for required fields while preserving optional-field semantics.
- [x] Install 0.0.49 into the active Central Fuckery profile. Installed and SHA-256 verified after the user closed Valheim; launcher catalog entry and description verified by read-back.
- [x] Repair `install-local.ps1` checksum verification. Windows PowerShell emits a top-level JSON array as one pipeline item, so `@(... | ConvertFrom-Json)` produced a single nested entry and every package failed integrity. Semantics unchanged; empty manifests now rejected explicitly.
- [ ] Live-confirm that a failed workshop transaction rolls back without an unhandled exception and that socket install/extract refreshes the inventory.

## P0.4 — Rendering and content drift (raised from live play 2026-09-15)

Full evidence and root causes: `docs/RENDERING_AND_CONTENT_DEFECTS.md`. Source repair is complete for the screenshot defect set below; live Valheim visual/runtime acceptance remains open where recorded in that register and `TESTING.md`.

- [x] R1: replace the one-pixel `Texture2D.whiteTexture` material path with Magenheim-owned generated surface textures across the affected runtime visual families.
- [x] R2a: give `Magenheim.Core` correctly wound `Box`/`Cylinder`/`Prism` primitives plus deterministic closed-surface, signed-volume and outward-normal checks, with a regression test that rejects the exact winding shipped in eleven visual files.
- [x] R2b: migrate the original eleven affected visual files onto shared tested primitives and remove their duplicated private builders; additional affected Crystal Weapon and Obelisk Warden geometry was migrated to the same authority.
- [x] C1: give Fire, Storm, Earth, Radiance, Seidr and Spirit owned staff presentation/geometry and remove the unintended vanilla Eitr gameplay dependency from the elemental staff progression.
- [x] C2: strip inherited vanilla Beehive behavior/audio from Crystal Bed and Crystalline Ice Box clones.
- [x] C3: own piece snap points instead of inheriting or merely rescaling incompatible donor snap positions.
- [x] C4: give the Crystal Sentinel an owned icon/readable front and explicit forward emitter presentation.
- [x] C5: collapse the eight elemental Crystal Munition item identities into one `Magenheim_CrystalMunition` with multiple shard refining routes.
- [x] C6: rebuild the Crystal Enchanting Dais as a broad, low raised working platform rather than a stacked apex monument.
- [x] C7: replace the raw `OnGUI`/`GUILayout` socket window with an `InventoryGui`-hosted native Unity UI surface and remove the obsolete IMGUI skin patch.
- [x] C8: overwrite cloned furniture `Piece.m_name` and `m_description` with Magenheim-owned identity so donor names such as "Raven Throne" cannot leak into hover text.

## P1 — Meadows/Earth vertical slice

- [x] Define the initial Meadows geode data with one guaranteed Earth crystal and independent 35%/10% additional-crystal chances.
- [x] Add deterministic pure-core coverage for geode cracking and authority-gated geode-opening transaction planning.
- [x] Add session-scoped exact-once geode-opening admission keyed by peer/session generation/operation id.
- [x] Add definition-driven Jötunn geode item registration source using canonical prefab identities and a temporary vanilla Stone visual base.
- [ ] Validate intact Meadows geode item/prefab registration in a current Valheim/Jötunn runtime and replace the Stone placeholder only after the custom geode asset passes prefab/visual validation.
- [x] Carry authoritative geode biome metadata into `DesiredWorldgenAddition` and build worldgen plans directly from the validated definition snapshot.
- [x] Add an explicit Jötunn adapter boundary for validated biome/area mapping and read-only desired-host collision observation.
- [x] Bind the additive worldgen planner to a one-time Jötunn `CustomVegetation` registrar using the validated definition compatibility policy.
- [x] Resolve pure-core `SpawnArea.All` against runtime `Heightmap.BiomeArea` names `Everything`/`Everywhere` instead of casting enum integers.
- [x] Move geode placement density, terrain, scale, group, and offset controls into schema-3 fingerprinted definition authority.
- [x] Expose those placement controls through server-side BepInEx overrides that revalidate/re-fingerprint before registration.
- [ ] Snapshot pre/post worldgen registrations and prove repeated-load idempotence in a live Valheim/Jötunn environment.
- [x] Add a dedicated Magenheim-owned mineable geode world-object prefab derived as `<item prefab>_World`, with persistent network state and exactly one intact-geode destruction drop.
- [x] Collapse world-prefab and vegetation registration into one additive path because Jötunn `AddCustomVegetation` registers the prefab itself.
- [ ] Compile and validate the mineable geode world-object/vegetation path in current Valheim/Jötunn, including exactly-one intact drop, natural placement, host/client persistence, and proof vanilla `Rock_4` remains unchanged.
- [x] Implement Geologist's Workstation registration and building recipe (0.0.16; live world test confirms the station exists and opens its UI).
- [x] Record live-world test 1 confirming station UI access, direct Earth inventory-item spawning, and direct custom world-geode geometry.
- [x] Normalize Magenheim-owned cloned materials to opaque blend/depth state at the authoritative visual-loading boundary after the live world geode rendered translucent.
- [ ] Rebuild/install and confirm the spawned world geode is opaque under live Valheim lighting after the material-state repair.
- [x] Synchronize the repaired workstation strap generator with the shipped mesh, OBJ, icon and preview. Live visual acceptance remains open.
- [ ] Revalidate the current authority-gated geode-opening/refinement workstation operation binding in live Valheim after the source advanced beyond the first-world-test baseline.
- [x] Register Earth Rough/Simple/Crystal/Advanced/Master items and Earth Crystal Shards.
- [ ] Verify a failed refinement transaction consumes exactly one source and returns exactly the configured matching shards in live runtime.
- [ ] Verify disposable-world generation, save/load, host/client, and dedicated-server behavior before production admission.

## P2 — adaptive sockets

- [x] Define namespaced persistent per-item socket metadata under `magenheim.sockets.v1` without changing shared item/prefab definitions.
- [x] Discover eligible equipment adaptively from runtime item category with a fail-safe Unknown result; unknown equipment requires explicit compatibility opt-in.
- [x] Add configurable include/exclude rules by shared item identity, prefab, category, and mod origin, with exact/case-insensitive identity semantics.
- [x] Enable Jötunn ModQuery before equipment classification so mod-origin compatibility rules can resolve non-Jötunn as well as Jötunn-added prefabs.
- [x] Map crystal bonuses by equipment category through Magenheim-owned runtime effect patches without replacing external prefabs or recipes.
- [x] Add an explicit Geologist's Workstation socket-management surface for local-host operations; mutations remain per-item metadata operations.
- [x] Preserve unknown foreign custom-data keys when writing or clearing Magenheim socket metadata.
- [x] Add authority/replay-gated socket add/install/extraction planners and local-host transactional rollback paths.
- [x] Fold gameplay-significant socket limits and compatibility rules into the multiplayer gameplay authority fingerprint.
- [x] Add deterministic source coverage for item-level compatibility, identity normalization, policy fingerprint drift, replay identity, metadata preservation, socket mutation, extraction, and effect calculation.
- [x] Bind remote-client socket-management requests to a server-authoritative, session-generation-bound request/response/ack RPC using existing socket/extraction replay guards; server reconstructs equipment classification from the registered prefab and owns extraction randomness.
- [ ] Compile and live-validate remote-client socket open/install/extraction, stale-state and stale-session rejection, replay behavior, policy mismatch rejection, and third-party descriptor spoof rejection in a current Valheim/Jötunn multiplayer environment.
- [ ] Validate persistence and behavior through save/load, drop/pickup, chest storage, repair, upgrade, player transfer, death, dedicated server, and mod removal.
- [ ] Live-test representative third-party equipment, including explicit item/prefab/mod-origin exclusions and unknown-category opt-in behavior.

## P3 — biome and magic expansion

Continue remaining elemental/magic content only when it does not outrank broken intended behavior or validation gates above. The archived detailed design remains recovery material, not evidence of implementation. Current source already contains substantial elemental staff work beyond the original Earth vertical-slice baseline; live source and validation records outrank this historical phase label.

## Underworld framework objective

**Target architecture: a dedicated Magenheim-owned instanced world-space, not an adjacent continent, coordinate-projected Surface layer, or second manually selected save.** Framework work converges on native instance terrain/chunks, biome/environment authority, independent persistence/exploration/map state, and a thin Deep Gate transport adapter. Per-player layer/transition persistence is explicitly out of scope; see `docs/validation/2026-09-20-underworld-player-transition-drift-cutoff.md`.

## P4 — The Underworld expansion track

Durable authorities: `docs/UNDERWORLD_DESIGN.md` and `docs/UNDERWORLD_IMPLEMENTATION_PLAN.md`.

This is expansion scope and remains subordinate to broken intended behavior and unresolved runtime acceptance in P0-P2. Do not allow exciting new world content to hide failures in the existing Magenheim baseline.

- [x] Commit the durable Underworld design and heightfield cavern-illusion architecture.
- [x] Commit the detailed Underworld implementation program, including biome framework, creature ecology, six-boss progression, Deepstone Conclave and milestone gates.
- [x] U0: reconcile live definition, fingerprint, world-state, network and Nowhere King completion authorities; see docs/validation/2026-09-15-underworld-authority-integration.md. Runtime completion adapter remains a prerequisite to U2.
- [x] U1: compile/test the Core skeleton and embed six-boss progression plus A0 architecture in canonical schema-5 JSON/fingerprint authority; no runtime world registration.
- [ ] U2: prove dedicated instance admission/materialization in a disposable environment before biome production.
- [ ] U3: harden instance persistence, independent map/exploration state, and multiplayer instance synchronization. Do not build persistent player-transition state.
- [ ] U4: prove Macro Basin + Wall Mass + inaccessible Roof Shelf + cavern-sky/fog illusion.
- [ ] U5-U7: implement generic biome, ecology, boss and Deepstone frameworks.
- [ ] U8-U9: complete Fungal Forest vertical slice and The First Bloom progression loop.
- [ ] U10-U11: complete Blackwater Deep and The Blackwater Maw.
- [ ] U12-U15: complete Sulfurous Wastes/Furnace Heart and Frozen Caverns/White Silence parallel progression branches.
- [ ] U16-U18: implement traversal framework, Fracture Zones and The Rift Titan.
- [ ] U19-U20: implement Great Decay and The Carrion Crown.
- [ ] U21: complete all six Deepstones and Deepbound Portal unlock using the authoritative transition service.
- [ ] U22-U25: expand civilizations, building, events/audio/music, configuration and compatibility.
- [ ] U26: execute expansion-scale validation across transition, generation, persistence, multiplayer, bosses, structures, building and performance.

## Closeout follow-through

Current unfinished asset/gameplay inventory is in `CLOSEOUT.md`. Execute the current TESTING.md matrix before enabling later world-generation scope or adding model-only gameplay. Historical unchecked runtime gates above remain open.
