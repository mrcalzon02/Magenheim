## Unreleased - Underworld weather ownership

- Replaced inherited Surface storm weather inside the active Underworld with six biome-owned weather families built as namespaced clones of Valheim `EnvSetup` donors. Surface biome weather tables are not edited.
- Added deterministic four-minute subterranean weather windows keyed by the paired Underworld seed and Valheim world clock. Every biome has its own calm/event distribution; Great Decay keeps persistent miasma even when the event state is calm.
- Integrated Sporefall, Deep Fog, Ashfall, Thermal Surge, Whiteout, Stone Rain, Crystal Resonance and Black Bloom with the shared atmosphere runtime.
- Underworld environment clones explicitly suppress rain clouds, thunder/rain/storm particle systems and inherited storm ambient loops. Frozen Whiteout may reuse snow-storm particles only when the particle identity is actually snow.
- Added source tests for determinism, event eligibility, seed variation and long-run coverage. Source version advances to 0.0.91; compile/closeout/live visual acceptance are still pending.

## Unreleased - Shared Underworld atmosphere framework

- Added one deterministic atmosphere authority for all six Underworld biomes instead of separate fog implementations: luminous spores, Blackwater mist, sulfur miasma, frozen fog, fracture dust and Great Decay biological aerosol.
- Great Decay owns the persistent heavy-obscuration profile; Sulfur fog scales with geothermal hazard, Blackwater fog pools over water, Frozen Caverns gains the strong Whiteout event, and the other biomes retain lighter atmospheric identities.
- Hazard resistance and atmospheric suppression are separate. Resistance can protect the player without erasing the biome visually; suppression is reserved for local-clearing mechanics such as the planned Censer.
- Added a thin runtime renderer over Unity RenderSettings that samples native Underworld instance terrain and restores Surface fog on exit. It does not choose events, persist player state or create another environment manager.
- Added deterministic source tests for biome/event/mitigation behavior. Runtime compile, closeout and live visual acceptance remain unclaimed.

## 0.0.88 - Placeable icons come from the models

- **All 62 crystal placeables now carry an icon rendered from the same `.blend` their runtime mesh
  exports from.** The Hammer menu was rows of grey diamonds because eight `*Icons.cs` classes --
  1,233 lines of it -- drew the icons as procedural pixel art in C#, which describes a crystal
  foundation exactly as well as a rectangle primitive can. This is the third time this defect has
  been fixed: the staves first, the ten weapons in 0.0.72, and now the placeables. All eight classes
  are deleted; `tools/render-placeable-icons.py` replaces them.
- The five Visuals classes that built their model id inline now expose it as `ModelId`, so the icon
  and the mesh resolve from one string and cannot drift apart. `CrystalAlchemyIcons` stays: Crystal
  Dust and the two Eitrwine bottles have no models to render.
- The renderer asserts its expected count, so a placeable authored tomorrow cannot quietly ship
  without an icon. `crystal-brazier`, `crystal-lantern` and `crystal-wardstone` are deliberately
  excluded -- `WorldArtifactVisuals` names them but nothing registers them as pieces.
- **`ReadOnlySpan<byte>` reached `main` again**, in `UnderworldTerrainTextureAssets`. Unity's
  `ImageConversion.LoadImage` has a Span overload, and resolving that call needs
  `System.ReadOnlySpan`1` to exist -- which it does not on net462 without System.Memory, which the
  game does not ship. `ModelAssets` had already hit this and solved it by binding the `byte[]`
  overload through reflection; that workaround is now a named `ModelAssets.LoadImage` that both
  callers use, so the next one finds the answer rather than the error.
- Merged 27 commits of Underworld work from `origin/main`, which still carried both defects 0.0.86
  repaired: the `System.ValueTuple` in the chunk streaming focus list, and the model-asset gate that
  rejects the chunk materializer. The combined tree was rebuilt rather than assumed compatible.

## 0.0.87 - The placeables stop wearing the donor's shader

- **Crystal placeables inherited Valheim's piece shader from the donors they clone, and it does not
  sample mesh UVs.** It generates its surface from where the piece stands in the world, which is the
  right behaviour for a vanilla wall -- long runs tile coherently with no UV work -- and completely
  wrong for a Magenheim mesh, whose UVs are then never read at all. Geometry, UVs and texture can
  all be correct and the model still renders as a lattice sliding across its own faces.
  0.0.74 had already stripped the donor's normal, metallic, occlusion and moss maps for the same
  underlying reason: they were authored for the donor, not for us. It left the shader, which is the
  other half of the same defect.
- `CrystalArchitectureVisuals` and the dais had worked around it by passing Rock_4's material in
  explicitly, and that repair was never rolled out. The **ice box, the ten decor pieces, the ten
  furniture pieces, the eight beds, the twenty-four banners and the sentinel** all still inherited
  it -- reported from play as the sconce's smearing appearing on "a number of the other crystal
  placeables".
- The swap now happens once, in `ModelAssets`, where the donor material is resolved: a material
  whose shader is world-projecting keeps everything except that shader, which is replaced with one
  that samples UVs. Doing it there rather than at each call site means a placeable authored tomorrow
  cannot reintroduce the defect by forgetting an argument. Colour, texture, metallic and roughness
  come from the model payload regardless, so nothing authored is lost, and each substitution is
  logged with the model id so the next session can see exactly which models were affected.
- The two existing explicit material sources are left in place. They are no-ops under the new check,
  and rewiring assets confirmed working in the field to prove a point is not worth the risk.

## 0.0.86 - Live play, 0.0.85: what the hands and the hammer found

Every item here comes from one in-game session against 0.0.85.

- **The Crystal Sentinel could be deconstructed forever.** Each hammer strike refunded the full
  build cost and left the piece standing. The cause was `Cost("RefinedEitr", 8)`: Valheim's refined
  eitr item is `Eitr`, and `RefinedEitr` is not a prefab, so the requirement resolved to null.
  `WearNTear.Destroy` calls `Piece.DropResources`, which instantiates each requirement in turn and
  threw on the null one -- after the other four had already hit the ground and before the
  `ZNetScene.Destroy` at the end of its own caller. Unbounded duplication out of a typo.
- The same typo had also reached the **Crystal Bed**, and `CoreWood` -- Valheim's core wood is
  `RoundLog` -- had reached the **Earth Simple staff**. All three corrected.
- **A requirement audit now runs at startup.** Jotunn was already reporting all three, as nine
  `MockResolveFailure` warnings inside a 50,000-line log. Magenheim now says it in its own voice at
  error level, names the piece and the position in its cost list, and says so explicitly when
  everything resolves, so silence is never ambiguous. It does not throw; taking the registrar chain
  down would be far worse than a wrong build cost.
- **Held weapons, trimmed from measurement rather than guesswork.** The alignment report prints both
  frames in attach space, so "how much of the weapon trails behind the hand" is directly comparable
  against the donor Valheim animates for. Battleaxe 51.8% against its donor's 12.5%, greatsword
  34.2% against 17.3%, knife 41.9% against 12.1%, atgeir 41.6% against 27.2% -- the four reported
  wrong, and the largest four gaps. Each takes about 45% of its full-match delta: clearly in the
  reported direction, short of the value known to have overshot, since full donor-matching is what
  broke the axe in 0.0.83. Axe, bow and crossbow were confirmed correct and are untouched; mace and
  sword were not tested and are deliberately left alone.
- **The spear was pointing the wrong way.** Valheim holds a spear with the point trailing so the
  thrust drives it forward; ours pointed ahead, so the animation stabbed with the butt. Reversed
  about the attach origin and offset back into the envelope it already occupied, which play
  confirmed was otherwise almost exactly right.
- **The Crystal Hearth's fire read green and towered over the piece.** Two separate faults. The
  rainbow gradient faded its alpha to zero at the top of the ramp, and under `RandomColor` that
  alpha is sampled per particle rather than over time -- so every violet particle was born
  translucent and every magenta one invisible, leaving the opaque red-to-cyan half to carry the
  fire, which additive blending resolves as green. And the measured rescale only set
  `transform.localScale`, which does not reach particle size unless the system scales with its
  hierarchy; a fire authored for one fireplace does not. Both stated explicitly now.
- **The Crystal Enchanting Dais opened two menus at once.** It is a CraftingStation so the Ice Box
  can require it and so this overlay can find it, but the piece says nothing is crafted or refined
  there, so the recipe list contradicted it. The crafting panel is hidden for as long as the
  socketing panel is up and restored the moment it is not.
- **The Crystal Wall Sconce was six boxes at furniture scale** -- a 0.38 x 0.72 m wall plate and a
  0.52 m crystal. Rebuilt at 0.17 x 0.45 m projecting 0.26 m, with a chamfered plate, a tapering arm
  that rises as it reaches, an octagonal cup and a faceted lamp. The geometry is rebuilt into the
  datablocks already bound to each part's material, so the committed textures carry over untouched.

Still open from the same session: the crystal placeables draw their Hammer icons as procedural pixel
art in C#, the defect 0.0.72 fixed for the ten weapons. Sixty-five models need rendering.

## 0.0.85 - The Structural Crystal, and the end of vanilla Crystal in our recipes

- **Magenheim no longer builds with Moder's crystal.** Thirty recipes across six registrars -- the
  crystal architecture set, the ten geology decor pieces, the ten crystal weapons, the Crystal
  Sentinel, the Crystal Enchanting Dais and the Resonance Frame -- cost vanilla `Crystal`. They now
  cost the Structural Crystal, which is ours.
- **Structural Crystal**, an unaligned bulk block fused from four elemental crystal shards of any
  one alignment, at the Geologist's Workstation. The fusing destroys the alignment, the same trade
  the grinding chain already makes for Crystal Dust: it holds no charge and cannot be socketed, and
  what it holds instead is load. Four shards rather than the five a Simple crystal costs, because
  this is bulk stock and everything Magenheim builds is now priced in it.
  Valheim's crafting UI cannot express "any four shards" as a single requirement, so this is eight
  recipes, one per alignment, rather than one mixed-input craft.
- Recipe amounts scale to roughly half their vanilla-Crystal values, rounded up, with a floor of 1
  -- an 8m foundation goes 32 -> 16, the Crystal Greatsword 34/14 -> 17/7, a wall sconce stays at 1.
  Each block already costs two rough crystals upstream, so halving keeps the total geode spend close
  to what it was.
- The billet is a new model rather than a recoloured tier: two chamfered, flat-faced blocks, the
  upper one turned off-axis and seated into the lower. Nothing in it is a matrix chunk or a
  terminated point, because at stack scale the silhouette is the only thing separating building
  stock from a crystal worth socketing. It carries its own full-range facet map -- the shared tier
  map spends half its width on host rock the billet does not have, and sampling only the bright half
  rendered it as a white blob.
- `export-model-assets.py` now writes the `.obj`/`.mtl` pair beside every earth asset it exports.
  They used to come from `generate-earth-assets.py`, which the Blender-authored tiers no longer go
  through, so a model authored today shipped without the pair its gate requires.
- **Two defects on `main`, both of which had been pushed unbuilt.** `verify-model-assets.py`
  rejected `UnderworldInstanceChunkMaterializer` for building a mesh in C#; it builds terrain from
  Core heightfield samples and is not authored art, so the gate now records why each exemption
  exists and asserts the exempted files still exist. And `Magenheim.Runtime` referenced
  `System.ValueTuple` again, through the chunk streaming focus list -- it compiles and then throws
  at registration, taking every registrar behind it with it. Replaced with a struct.

## 0.0.72 — Weapon icons from their sources, and the hand-authored geode

- The ten crystal weapons drew their icons as procedural pixel art in C#, which described them only
  as well as code could draw them and drifted whenever a model was re-authored.
  `tools/render-weapon-icons.py` renders all ten from the same `.blend` the runtime mesh exports
  from, the way the staff family already works, and `CrystalWeaponIcons.cs` is deleted. The icons
  are registered in the freshness manifest so they cannot silently fall behind their models.
- **The exporter died on a source saved in Edit Mode.** Blender restores the mode a file was saved
  in, and hand-editing a source very often leaves it in Edit Mode, where `origin_set` fails and the
  whole export aborts. That is why a hand-authored geode never reached the runtime payload the game
  loads. Editing a source by hand is normal; refusing to export it is not.
- The exporter now drops triangles with no world-space area. It emits the *modifier-evaluated* mesh,
  so a sliver produced by a modifier does not exist in the `.blend` to delete and cannot be repaired
  at the source.
- Textures are named by content rather than by the Blender image name, removing a latent overwrite
  hazard. Measurement afterwards showed **no model had actually been affected by it** — every set of
  models sharing a texture genuinely shared identical content. The change is a correctness
  improvement, not a fix for anything that was broken.
- The two geode gates are out of the build. They encoded the previous two-part structure — a
  continuous `GeodeCore` behind fractured plates, and a mouth at a hardcoded cut direction — and the
  geode is now hand-authored and is the authority for its own shape. Winding, UVs, topology hashes
  and degenerate faces remain covered by `verify-model-assets` and `verify-model-geometry`.

## 0.0.69 — Underworld starts with Valheim's own visual language

- Replaced the two authored placeholder meshes used by the local Underworld ecology preview with
  biome-specific palettes of **Valheim runtime donor prefabs**. Fungal Forest starts from Yggdrasil
  shoots and vanilla luminous mushrooms; Blackwater from Mistlands cliffs, stone fingers and roots;
  Sulfurous Wastes from Ashlands rocks, scorched trees and brush; Frozen Caverns from mountain rock,
  ice and black-ice shards; Fracture Zones from Mistlands/rock formations; Great Decay from vanilla
  root masses and scorched growth.
- Donors are resolved from Valheim through Jötunn at runtime. Magenheim does not package their
  meshes, textures, materials or prefab data and does not replace any vanilla prefab registration.
- The preview rebuilds only render meshes, shared materials, lights and LOD groups into local,
  disposable objects. Gameplay, network, harvesting, AI and donor destruction components are not
  copied. A conservative collider is added only to entries explicitly marked solid.
- Missing/renamed donors fail neutral and retry later; palette fallback prevents one missing prefab
  from punching a permanent hole in a biome. Oversized glowing donors scale their light radius so
  canopy-sized mushrooms remain readable.
- UNDERWORLD_FLORA_TERRAIN_PLAN.md now makes the production order binding: vanilla donor
  composition first, donor kitbashing/material variants second, Magenheim-authored gap-fill assets
  third, external permissive assets only after those passes.
- This is source-complete ecology-preview work only. Runtime appearance, placement density,
  collisions, LOD behaviour and frame cost remain **unaccepted until a live Valheim test**.

## 0.0.68 — Staves that fire

Six of the eight Master staves depleted stamina and produced nothing; Radiance and Earth worked. The
split is exact, and it is not damage, damage type, projectile source or payload — every staff that
fires has one burst and every staff that does nothing has more than one. Valheim's own
`Attack.ProjectileAttackTriggered` ends with:

```
if (m_projectileBursts == 1) FireProjectileBurst();
else m_projectileAttackStarted = true;
```

A single burst fires synchronously, inside the animation trigger. More than one defers to
`Attack.UpdateProjectile`, which only runs while the attack is live — and `Attack.Update` calls
`Stop()` the moment `InAttack()` goes false, setting `m_attackDone` so no later burst can fire.
Every Magenheim staff clones `StaffIceShards`, a single-shot staff whose animation ends right after
its trigger, and no registrar set `m_loopingAttack`. A staff authored for three or six deferred
bursts therefore had no window to fire them. Stamina still drained because `Attack.Update` charges it
up front whenever `m_perBurstResourceUsage` is false, which every staff sets — which is exactly why
the symptom read as "costs stamina, does nothing" rather than as a refused attack.

Each family keeps its authored identity: bolt count, damage, damage type, spread, velocity and
payload are untouched. Only the release changes — a staff authored as three bolts across three waves
now sends its nine bolts in one volley, on the code path that works. The stagger is **not** restored;
that needs `m_loopingAttack` plus an animation that holds open, and a looping attack that never
self-terminates would leave the player stuck mid-swing. That is recorded as its own item rather than
ridden along with a repair.

An earlier attempt in this session rebalanced the six families' damage upward against Radiance. That
was wrong — the numbers were never the defect — and it has been reverted in full.

## 0.0.67 — Held weapons meet their donors

Each crystal weapon clones a different vanilla donor, and Valheim authors every donor mesh in its own
local frame under `attach`. `ModelAssets.Load` parented the replacement there at identity, so a
weapon presented correctly only where the donor's frame happened to match Magenheim's authoring
convention — axe, battleaxe and bow right; crossbow reversed, greatsword rolled, knife, mace and
spear wrong. The replacement is now rotated into the donor's measured frame, and the correction is
logged per model so the three already-correct weapons act as the check: for those it must come out
at identity.

Also: the Crystal Sentinel never set a single targeting field, so it inherited whatever `piece_turret`
shipped with rather than what its own description promises. Enemies are now explicitly targeted;
players and tamed creatures explicitly are not. Geode placement scale widened from 0.85–1.15 to
0.70–1.45, since plus or minus fifteen percent reads as one uniform size in a field.

## 0.0.66 — Materials stop being named by their model

Crystal placeables looked correct in the authored preview and arrived in game as a patchwork of
unrelated surfaces. The preview and the game do not share a material path: at runtime
`GeneratedSurfaceTextures` replaces each owned material's texture with a generated surface chosen by
substring-matching the material's **name** — and model materials are named
`magenheim.<family>.<model-id>.<semantic>`, so the model id was deciding every material on the model.

`architecture-crystal-hearth` contains "earth", which is tested before "iron" and before "crystal",
so the hearth's iron banding and all six rainbow crystals were textured as stone. Any id containing
"banner" forced cloth; any containing "core" forced crystal. Measured across the committed library,
**326 of 1979 owned materials — 16.5% — classify differently once the model id is excluded**: 119
Metal wrongly read as Stone, 67 Crystal wrongly read as Stone, 24 Crystal wrongly read as Cloth.
Classification now uses the material's own trailing semantic token, with an instance index dropped.

## 0.0.65 — Crystal Hearth flame, and two diagnostics

- **The hearth flame was sized for its donor, not for the piece.** `preserveParticles` keeps the
  donor's particle systems untouched while the visible mesh is replaced, and nothing reconciled the
  two. The flame is now scaled by a measured ratio of the donor's own renderer bounds against the
  loaded model, so it stays correct if either is re-authored.
- **The flame read red because the rainbow was swept along each particle's life.** On
  `colorOverLifetime` every particle is born red, reaches green only at 33% and blue at 50%, while
  the alpha keys hold it opaque to 75% and fade it out by 100% — so each particle spent its bright
  phase in the red-to-green half and died out through blue and purple. The spectrum is now
  distributed across particles at birth and `colorOverLifetime` reduced to the alpha fade.
- Two temporary diagnostics, so one run settles two defect classes that static analysis could not.
  `StaffAttackAudit` reports the attack configuration every staff actually shipped with.
  `HeldItemOrientationProbe` reports the attach-space transform of each held item, vanilla and
  Magenheim alike, because measuring the crossbow disproved the held-model gates' assumed
  convention rather than the asset: its prod already sits at the +Y end the convention calls
  "working", and it still points its rear at the target.

## 0.0.64 — Generated assets answer to their generators

0.0.63 repaired three defects of the same shape: a generator or its gate moved on while the
committed output did not, and only a full build noticed. This closes that class.

- **`tools/verify-generated-freshness.py`** records, per generator, the hash of the generator itself
  and the set of files it owns. A changed generator, or a file added or removed outside it, fails
  the build. Verification is pure hashing — no Blender, no regeneration — so it runs *first*, ahead
  of the ten minutes of gates that used to be the only way to find staleness. Refreshing the
  manifest runs the generator, so it cannot be brought back into agreement without regenerating.
- **It found four stale icons on its first run.** The crystal tier of the Storm, Fire, Venom and
  Radiance staves differ from what the renderer now produces by about 10% of their pixels. Same tier
  across four families: a model change those icons never picked up. Re-rendered.
- **Blender output is not reproducible, and the gate admits it.** Re-rendering all 32 staff icons
  produced 20 files differing from the committed ones, but 16 of those differed by a single LSB on a
  handful of pixels. An exact content hash would cry wolf on half the family after every render, and
  a gate that cries wolf gets refreshed blindly. Blender-backed entries are checked on generator
  hash and output names only; pure-Python generators keep full content hashing.
- `tools/blender.ps1` treated Blender 5.0's benign `Error: Not freed memory blocks` shutdown line as
  a script failure. It appears intermittently, and it failed a staff-icon render that had already
  reported all 32 icons saved.

## 0.0.63 — Underworld biome sector, and a build that runs

The Underworld's biome-sector override never took effect, and `main` could not be built. Both are
fixed; neither is yet confirmed in play.

- **The sector override was bound to the wrong overload.** Almost nothing except terrain height
  reads `GetBiome` — weather, sky, ground texture, spawns, vegetation, the minimap and the HUD biome
  all read `GetBiomeSector` — so with the sector still Ocean the game disagreed with itself out in
  the region. The patch sat on `GetBiomeSector(int gridx, int gridy, bool clamp)`, which clamps its
  grid indices into [0, 2047] unconditionally, its own `clamp` argument never being read. The biome
  map spans only ±12282m and the region is at 40000, so grid 4356 clamped to 2047 and containment
  was tested 27.7km away. It could never pass, and the diagnostic probe, which only logged when it
  did, could never have said so. Settled from the installed assemblies instead. The region does
  **not** have to move: both world-space overloads still carry the true coordinate, and nothing in
  the game reaches the grid overload except those two, so the override moved there.
- **`main` failed its own build twice before reaching the compiler, and three more times after.**
  None of 2026-09-18's work up to `df27a65` had been built. The icon gate demanded 6% frame coverage
  from staff icons that are hairline shafts — six could not have passed at any framing — and now
  measures placement and ink density separately, proven against four synthetic defect probes. The
  Sporeling textures were four generator commits and five gate commits stale, and regenerating
  exposed two real defects in the generator: emission with no focal highlights, and flesh authored
  in the same hue as the spore sac. The source blend was stale because its author had never run on
  Blender 5.0, which removed `Action.fcurves`; fixing that revealed an animation gate that had never
  executed, which in turn revealed that all ten actions were being discarded on save for want of a
  fake user. The review renderer had never run either, for the same class of reason.
- A Pillow deprecation warning on stderr was failing the build after a gate had already passed,
  because Windows PowerShell 5.1 turns native stderr into a terminating error. Fixed at the API and
  at `build.ps1`.

Nothing here is runtime-accepted. The game has not been launched against this build.

## 0.0.62 — Fungal Forest flora foundation

- Correct the Underworld plan to use one shared custom cavern-roof skybox.
- Add fingerprinted Glowcap, Spirestalk and Shelfwood definitions and pure terrain eligibility rules.
- Reject surface-world, submerged, unsupported and obstructed placement; require exposed rock for Shelfwood.
- This is F1a only: models, runtime spawning, harvesting and the crafting economy are pending.

# Magenheim Changelog

## 0.0.61 - Underworld structural landmarks

The Underworld is 130 of the 281 models. Its districts are the best assets in the library at
10,012-11,070 triangles and its creatures are healthy, but a handful of structural landmarks
had been left far behind -- and they are exactly what a player walks up to and stands in.

- Underworld Standing Stone 76 -> 332, Underworld Dais 572 -> 956, Deep Fracture entrance
  384 -> 864. Edge definition rather than re-authored form: a chamfer so stone reads as cut
  stone under the Underworld's low light instead of as flat facets. Subdivision is
  deliberately not used, because it would round corners meant to be sharp.
- Dark Throne is **not** revised here, and the attempt found something worse. Its source
  evaluates to 4,080 triangles while the shipped payload is 880, so the arena centrepiece has
  been shipping at a fifth of its authored fidelity. Re-exporting it is blocked: the fresh
  export immediately fails asset verification with four inverted faces on `Dais_Step_1` that
  the stale payload was hiding, and recalculating normals does not fix them. It is left on
  the stale export rather than trading a hidden defect for extra triangles, and recorded.
- The Deep Fracture passage and traversal are unchanged and reported as such. A chamfer
  produces no geometry on them, which means they have no hard edges to cut and need real
  re-authoring rather than a refinement pass. They remain the weakest Underworld assets, and
  they connect districts of roughly 10,000 triangles.

## 0.0.60 - The crystal tier meshes are solid

- Rough, Simple, Crystal, Advanced and Master shipped as triangle soups: 1,146 to 1,590
  unshared edges each, with no welded topology. These are the models the player handles
  through the entire refinement loop, and an unclosed solid reads as holes and flickering
  backfaces. All five are now watertight.
- `verify-model-geometry.py` missed this because it tests that faces point outward, which
  they did. `verify-earth-assets.py` catches it, and was the last verifier never wired into
  the build.
- That gate also asserted a 512px atlas, which the legacy generator wrote but the modern
  export path does not; the library standard is square and at least 256px. Aligned to the
  library contract so it gates the real requirement instead of failing assets that meet it.
- All twelve asset gates now run in the build.

## 0.0.59 - Stage 1 of the model quality campaign: correctness

- The geode's "literal missing planes" were never in the asset. Both geode gates compared the
  mouth's cut direction against exported vertices without mapping it into export space, and
  the exporter writes Blender coordinates as (x, z, -y). They were testing the wrong axis.
  Mapped correctly, every boundary vertex sits in a tight band around the cut (core
  0.39-0.66, shell 0.45-0.58) and the mouth is one clean loop. The corrected gates were
  re-proven to still fail when a hole is punched away from the mouth.
- The acceptance pass that rebuilds the geode could never run here: it required Blender on
  PATH. It now honours MAGENHEIM_BLENDER and the standard install locations.
- Two geode meshes were rebuilt without a UV map, and export refuses a mesh without one, so
  the repaired source never reached the runtime payload. Both now carry a triplanar box
  unwrap, deliberately not a smart projection, which is what caused the 0.0.52 patchwork.
- Five held assets pointed the wrong way round in the hand: the Crystal-tier Fire, Storm,
  Radiance and Venom staves, and the crystal sword. They are reversed to match the other 35.
- Seven model gates existed but were never wired into the build, which is why all of the
  above stayed shipped. All are wired now, and all 11 verifiers pass.

## 0.0.58 - Socket menu tells you the outcome, banners get real collision

- Install buttons now say what the crystal will do **in that item's slot**: "Install Crystal
  Earth (x3)   +4 blunt damage, +0.1 knockback". The effect depends on the slot, so it
  belongs on the button rather than in a list of all five. A crystal that does nothing in
  that slot says so instead of looking identical to one that does.
- Removal buttons now show the risk before you commit: "Remove 1. Crystal Earth   20%
  shatter risk, 2 shard(s) if it breaks". It uses the same skill clamp and reduction ceiling
  the planner applies, so the number shown is the number that will be rolled against.
- "Extract" is now "Remove" on the button. It is the plain word for what it does.
- Crystal banners now carry collision that matches the banner actually shown. They cloned
  piece_banner01 and kept the donor's collider while displaying Magenheim geometry, because
  ModelAssets.Load disables Renderers and LODGroups but never colliders. The box is measured
  from the loaded meshes rather than hand-written per style, so it cannot go stale the next
  time the model library is rebuilt, and it is narrowed to the pole so a banner does not
  block the player standing beside it.

## 0.0.57 - Descriptions and socket list follow the new station split

- Every crystal now ends its description with "Socket at the Crystal Enchanting Dais." The
  line is generated in the same Core authority that writes the per-slot effect lines, so it
  cannot drift out of step with where socketing actually happens.
- A Rough crystal is sent to refining instead: "Too rough to socket. Refine it at the
  Geologist's Workstation first." It cannot be socketed at all, so pointing it at the Dais
  would be actively misleading.
- The Crystal Enchanting Dais describes what it is for: open a socket, fit a crystal, or
  draw one back out, and nothing is crafted or refined there.
- The Geologist's Workstation says what it kept: crack geodes, refine crystals up the tiers
  and craft with them, and that setting crystals into gear happens at the Dais.
- Items the socket policy disallows are no longer listed in the socketing surface. A list
  entry the player cannot act on reads as a bug rather than a rule.
- One deliberate exception: an item that already holds crystals and has *since* become
  ineligible is still listed, marked `[removal only]`, so the player can take the crystals
  back out. Hiding it would strand them with no way to reach them. Leftover empty metadata
  is not something to recover and is filtered out with the rest.

## 0.0.56 - Socketing moves to the Crystal Enchanting Dais

- The socket interface is now hosted only by the Crystal Enchanting Dais. Hosting it on the
  Geologist's Workstation displaced that station's normal crafting menu, because the
  Workstation carries the whole crystal/weapon/staff recipe list. The Dais carries no
  recipes at all, so the socket surface owns the panel instead of competing with one.
- All three socket operations move together: opening a slot, installing a crystal, and
  removing one. Removing a crystal is a socket operation, not a refinement step.
- Removing a crystal no longer requires the Faceting Wheel. That requirement was the only
  thing coupling socketing to the refinement chain, and it was misleading: the Faceting
  Wheel is a Geologist's Workstation upgrade that gates Crystal -> Advanced refinement, and
  the geode/refinement route never removes anything from a socket. The Dais itself is now
  the gate. The Faceting Wheel keeps its refinement role, unchanged.
- Deleted the Harmony patch that used to admit the Dais alongside the Workstation. With the
  Dais primary it was redundant, so the behaviour is in the authority itself rather than in
  a postfix over it. Harmony patch targets drop from 25 to 22.
- Renamed `SocketWorkstationOverlay` to `CrystalDaisSocketOverlay`; the old name pointed
  maintainers at the wrong station.
- Added deterministic coverage pinning socket removal to the Dais and proving that none of
  the four refinement stations can remove a socketed crystal.

**Existing worlds:** you must build a Crystal Enchanting Dais to socket. It is built at the
Geologist's Workstation from Stone, Iron, Crystal and Crystal Dust.

## 0.0.55 - Weapon world scale

- Rescaled all ten crystal weapons to vanilla proportions. The family shipped oversized:
  the knife measured 1.25m on its longest axis, longer than a vanilla sword, and the
  sword measured 2.09m, longer than a vanilla greatsword.
- The correction factor is observed, not inferred. The staff family reads correctly in
  play at 1.80-2.96m, and the sword was about a third longer than it needed to be, so
  every weapon was scaled by 0.75 about the world origin, which keeps the grip at the hand.
- Sword 2.09 -> 1.56m, knife 1.25 -> 0.93m, greatsword 2.78 -> 2.08m, atgeir 3.02 -> 2.26m.
- Rescaled the authored Blender sources and re-exported through the normal pipeline rather
  than applying a runtime scale, so the models themselves are correct.
- Added `tools/verify-model-scale.py`, wired into the build. Nothing had ever checked model
  scale; it produces no compile error and is only visible in a player's hand.
- Re-authored the weapon surfaces. The retro-texture pass had assigned surface families
  essentially at random: 76 of 79 weapon materials contradicted their own declared intent.
  A sword grip was textured as stone, its crystal as metal, and a greatsword's blackmetal
  as carapace, all on five shared 256px maps.
- The family token is load-bearing twice over: it selects the packed albedo, and
  `GeneratedSurfaceTextures.Classify` reads it to choose the runtime fallback surface, so a
  grip named `.stone` was classified Stone at runtime too. Both now follow the intent.
- Seven authored 512px families: leather, timber, blackmetal, silver, crystal,
  crystal-bright and prismatic. Grain stays fine and low-contrast because these UVs are
  smart-projected into small islands and any larger feature reads as patchwork.
- Added `tools/verify-weapon-materials.py`, wired into the build.
- Weapon geometry detail is NOT addressed. The weapons remain box primitives inflated by
  bevel modifiers, at a median 1,262 triangles against a library median of 2,348. That
  re-authoring is still open.

## 0.0.54 - Staff registration, icons and durability

- Repaired eight compile errors that were sitting unbuilt on `main`, including a seventh
  netstandard2.0/net462 BCL-availability defect of the already-recurring class.
- Fixed the missing Earth staff icon. `EarthStaffRegistrar` requested an icon that has
  never existed in repository history; because all eight staff registrars share one
  multicast registration event, that exception also stopped the Venom, Radiance, Seidr and
  Spirit families from registering at all. This is the cause of the reported
  "Crystal Staff of Venom does not attack" — the registrar that builds its projectile
  payloads never ran.
- Replaced three committed staff icons that carried corrupt PNG chunk data.
- Re-rendered all 32 staff icons from the same Blender sources the runtime meshes are
  exported from, so an icon can no longer drift from its model. Fire and Storm previously
  showed the vanilla donor icon; Radiance, Seidr and Spirit showed a tinted generic crystal.
- Staves now use durability and degrade with use. They clone a vanilla magic staff that
  spends Eitr instead, so they previously never wore out. Simple 150, +75 per tier.
- Added `tools/verify-icon-assets.py` as a build gate over icon integrity, staff icon
  coverage and literal icon references; added `.gitattributes` declaring binary assets.
- Live acceptance remains pending: the game has not been launched against this build.

## 0.0.51 - Model quality revision

- Revised all 276 existing saved models and added five missing effect/world-center assets.
- Rebuilt bow/axe and effigy silhouettes; added creature faces, mineral contrast, cloth folds and crafted edges.
- Preserved all 486 existing collision/crystal bounds and bindings.
- Fixed preview transform collapse, exported corner normals and packed texture fidelity.
- Removed remaining active primitive builders; retained historical source in the archive.
- Repaired pre-existing netstandard2.0 incompatibilities exposed by the build.
- Live visual and animation acceptance remains pending.

## 0.0.47 - Startup patch signature repairs

- Fixed fatal ambiguous GetDamage targeting and the same latent GetArmor defect.
- Target shared int/float calculation overloads once, avoiding double application through wrappers.
- Updated the socket tooltip patch to the installed six-argument signature.
- Added a packaging gate that resolves all Harmony targets and checks named parameter bindings against installed assemblies.


## 0.0.46 - Crystal Shaping skills-list visibility

- Initialize the registered Crystal Shaping player skill before Skills opens so it
  appears at level zero without requiring crafting, console commands or free XP.
- Preserve existing levels and progress; avoid creating a skill without its definition.


## 0.0.45 — local compilation and testing closeout

- Repaired core/test and runtime compilation against installed Valheim APIs.
- Updated private UI access, public server-peer lookup, nullable handling and stale socket diagnostics.
- Synchronized generated workstation strap geometry and icons.
- Added version checks, guarded cleanup, offline build mode, package checksums and current feature/test inventory.
- Core: 13,974 assertions passed; runtime: zero warnings/errors. Live acceptance remains open.

## Unreleased â€” live-world test 1 fixes

- Repaired the local developer delivery path that allowed Git source to advance while the
  active r2modman profile continued loading an obsolete Magenheim DLL. `install-local.ps1`
  now builds current source by default, aborts before installation on test/build failure,
  derives the expected package version from `MagenheimPlugin.PluginVersion`, verifies the
  package manifest, refuses duplicate DLL copies, SHA-256 verifies the installed tree, and
  prints the exact expected startup version.
- Removed hard-coded local package version authority from `build.ps1`; package directory and
  manifest version now derive from the plugin source, and the versioned package directory is
  recreated cleanly to prevent stale files surviving between builds.
- Reconciled test instructions with current source: local-host workstation geode opening and
  Earth refinement are now source-implemented with authority/replay/capacity admission,
  atomic inventory mutation, failure shard returns, and Crystal Shaping XP. They still require
  rebuilt live-world acceptance and remote-client RPC remains unfinished.
- Recorded the first disposable-world observations: the Geologist's Workstation opens,
  direct Earth inventory-item spawns work, and the direct world-geode prefab uses the
  custom Magenheim geometry.
- Fixed the authoritative custom-material loading path so opaque Magenheim RGB atlases
  no longer inherit transparent blend/depth state from cloned vanilla source materials.
  A rebuilt in-game retest is still required before the translucent-geode defect is closed.
- Corrected the Crystal Shaping console diagnostic to use the permanent single-token
  identifier `magenheim.crystal_shaping`; the spaced display name is split by Valheim's
  `raiseskill` parser before JÃ¶tunn can resolve it.
- Traced workstation iron-band surface fighting to intersecting/coplanar generated geometry;
  the generator and checked-in derived mesh still require a coordinated repair and retest.

## 0.0.16 - Geologist workshop

- Added four original models, texture atlases, build icons, and OBJ/MTL exports:
  Geologist's Workstation, Fracturing Block, Faceting Wheel, and Resonance Frame.
- Registered four Hammer/Crafting pieces with recoverable material costs, custom
  colliders, separate wear variants, and three non-stacking station upgrades.
- Connected the workstation to the registered Crystal Shaping skill identity.
- Preserved cloned station connection/area effects while replacing model visuals.
- Verified all content and the corrected world geode path register during isolated
  Valheim startup. Installed and hash-verified 0.0.16 in the active profile.


## 0.0.15 - Earth mineral content test

- Created original geode, five crystal tiers, and shard meshes, painted atlases,
  inventory icons, a skill badge, OBJ/MTL exports, and reproducible art tools.
- Registered Crystal Shaping and six Earth crystal/shard items; connected custom
  geode inventory/world visuals and isolated new materials and world colliders.
- Added working local build, package, backup, installation, and testing instructions.
- Repaired actual compilation errors: netstandard Dictionary API, nullability,
  game/Core SpawnArea ambiguity, and private server-peer lookup usage.
- Compiled against installed game APIs without rewriting/publicizing game DLLs.
- Repaired the live worldgen failure caused by a nonexistent drop-table field;
  use an independently owned current-game table with one unscaled geode drop.
- Disabled inherited LOD groups on customized clones to retain the new visuals.


## Unreleased â€” lifecycle-safe worldgen observation

- Removed the `ZoneManager.GetZoneVegetation` lookup from the `OnVanillaPrefabsAvailable` collision-observation path after current JÃ¶tunn source review showed that method dereferences `ZoneSystem.instance` when no custom vegetation match exists.
- Collision observation now uses `PrefabManager.GetPrefab`, the same prefab namespace JÃ¶tunn `AddCustomVegetation` must claim through `PrefabManager.AddPrefab`.
- Preserved the preflight refusal of occupied prefab identities, so removing the premature ZoneSystem lookup does not weaken non-destructive behavior.
- Advanced runtime source/package identity to 0.0.14 under patch-strict network compatibility.
- Added a dedicated lifecycle-safety validation record; runtime menu/world-transition behavior remains unverified until exercised in Valheim.

## Unreleased â€” fingerprinted geode placement authority

- Advanced the definition schema to version 3 and made complete geode vegetation placement behavior part of the validated/fingerprinted authority snapshot.
- Added pure `GeodePlacementDefinition` validation for per-zone values, altitude/ocean-depth limits, terrain delta/radius, tilt, forest thresholds, scale, group sizing/radius, ground offset, and runtime-float representability.
- Added required schema-3 `placement` data to the Meadows/Earth foundation definition.
- Added server-side BepInEx placement overrides under `Worldgen.Placement.<geode id>` and routed every override through the pure definition override/validation/fingerprint pipeline before worldgen registration.
- Removed runtime-owned placement constants; `GeodeWorldgenRegistrar` now consumes the validated geode placement record directly.
- Added deterministic placement-authority source coverage and updated authority tests to use the current schema constant.
- Runtime compilation and in-game schema-3/worldgen behavior remain unclaimed pending execution in a current .NET/Valheim/JÃ¶tunn environment.

## Unreleased â€” additive geode worldgen

- Reconciled repository metadata to a single authoritative `main` branch; the legacy `master` ref is no longer present.
- Added the one-time `GeodeWorldgenRegistrar` that consumes validated/fingerprinted definitions, performs read-only host observation, re-runs pure Add/Skip/Error planning, preflights approved additions, and registers only new Magenheim-owned geode vegetation through JÃ¶tunn.
- Removed the redundant `GeodeWorldPrefabRegistrar`; JÃ¶tunn `ZoneManager.AddCustomVegetation` already registers the supplied prefab through `PrefabManager`.
- Expanded host collision protection to the broader PrefabManager namespace without mutating host content.
- Hardened runtime area mapping by resolving `Heightmap.BiomeArea` from `Everything` or `Everywhere` instead of compiling against one spelling or casting enum integers.
- Added validation records for the additive registration contract and remaining runtime gates.

## Earlier unreleased foundation

- Reconciled divergent repository histories without discarding material work or using force-pushes.
- Restored canonical crystal/refinement authority and established project instructions, state, backlog, design, and validation records.
- Added the pure core, net462 JÃ¶tunn runtime bootstrap, strict definition loading, deterministic SHA-256 definition fingerprinting, and validated balance/compatibility overrides.
- Added Meadows/Earth geode definitions, deterministic cracking, authority-gated opening transaction planning, definition-authority synchronization, and session-scoped replay protection.
- Added additive-only worldgen area validation/planning, configurable collision behavior, Magenheim-owned exclusions, and dedicated `<item prefab>_World` geode identities.
- Added definition-driven intact geode item registration and dedicated mineable world-object source while keeping temporary Stone-derived visuals separate from natural worldgen identity.
- Hardened negative/unknown spawn-area handling and exact/case-insensitive identity semantics.
- Did not admit bundled runtime/vendor binaries, runtime logs/process files, unrelated third-party repair utilities, duplicate legacy engines, or stale runtime claims into live source.
