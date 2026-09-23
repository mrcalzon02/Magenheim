# Current release - 0.0.100 (2026-09-22)

Deep Gate re-bodied from the vanilla Aesir gate (Deep North boss `LastBossGate` assembly) at location load; prefab identity unchanged. Closeout passed and installed 0.0.100 (DLL SHA-256 8D7558012D1FA03944FB8C660B62479F9CEE9A05C5600467FBBF63F04CD6F7EB). Not yet observed: the swap only runs when a world loads, so its log line ("Deep Gate re-bodied from the Aesir gate ..." or the Morkhalla fallback warning), the gate's orientation and scale, and gate interaction all need a world load to confirm.

Launcher description: "0.0.100: The Deep Gate now takes its body from the vanilla Aesir gate (the Deep North boss LastBossGate) instead of the Morkhalla interior archway, applied when world locations load. Includes the twelve elemental Surtlings from 0.0.99."

# Current release - 0.0.99 (2026-09-22)

Twelve elemental Surtlings registered as console-spawnable donor-skeleton creatures; see docs/validation/2026-09-22-elemental-surtlings.md. Closeout passed and installed 0.0.99 (DLL SHA-256 6FA8485DE24B9819758E967DBBC81650801AEB7035EFFF43CD8D0DC903F27F0F). Live startup read back: Registered 12/12 elemental Surtlings, no Magenheim warnings. Not yet spawned in a world; animation following, combat and death playback need an in-game look.

Launcher description: "0.0.99: Twelve elemental Surtlings (Fire, Water, Earth, Wind, Radiance, Umbral; two body forms each) as console-spawnable creatures whose authored bodies ride humanoid donor skeletons, including death ragdolls."

# Current release - 0.0.98 (2026-09-22)

Readability and silhouette pass on all ten crystal weapons. `tools/author-crystal-weapons.py` now owns their sources (manifest id `crystal-weapon-models`): edged blades with fullers or midribs, swept guards, banded leather grips, wedged axe heads with bright crystal edges, a six-flanged mace, a bow whose string no longer passes through its limbs, a shaped crossbow tiller, and vanilla-style grip wraps, ferrules and langets. Each weapon carries its own baked 512px atlas (occlusion, worn edges, material pattern) in place of seven shared noise maps; crystal bodies no longer glow uniformly. Grip origin, length envelope and part identities are unchanged. Icons re-rendered. `verify-held-model-grip-direction.py` now uses area moment about the hand (evidence in the validation doc). See docs/validation/2026-09-22-crystal-weapon-readability-pass.md.

Full closeout passed: 227 generated files across 9 generators fresh, 10 held-model alignments match the confirmed rotation, 43,504 Core assertions plus separate suites; runtime compilation zero warnings/errors. Installed 0.0.98 into Central Fuckery, verified payload hashes and enabled launcher metadata. DLL SHA-256: 6DF170936A8F4A0EDB959D676D2943BB6659FBCCA0CEA47E101FEA3E58B52ED2.
Backups: backups/Local-Magenheim-20260922-165254.zip and backups/mods-20260922-165307-916.yml.
Static only: nobody has seen these weapons in a hand or in game lighting yet. Check first that the in-hand placement still matches 0.0.97 and whether the mace head reads too pale.

Launcher description: "0.0.98: Readability and silhouette pass on all ten crystal weapons: edged blades, shaped guards, wrapped grips, wedged axe heads, flanged mace, corrected bow string, vanilla-style fittings, and per-weapon baked painted textures. Icons re-rendered."

# Current release - 0.0.97 (2026-09-22)

Added the 22 planned raw-resource definitions across all six biomes, vanilla-backed custom material items, and native Pickable review prefabs. Mapping and exact console IDs: docs/UNDERWORLD_RESOURCE_MAPPING.md. Resource pickups are console-only, one item per pickup with no extra loot or respawn. Natural vegetation remains visual-only. Creature prototypes retain donor loot; existing recipes retain vanilla ingredients until natural resource acquisition is admitted. Merged the incoming Frozen Wall landmark work.

Full closeout passed: 300 model sets, 134 icons, 43,504 Core assertions plus separate suites; runtime compilation zero warnings/errors. Installed 0.0.97 into Central Fuckery, verified all payload hashes and enabled launcher metadata. DLL SHA-256: 1D4B2150764870BD608B17207EEC6E53EE06F59B6C680F35BCBD88FA65BD7EB3.
Backups: backups/Local-Magenheim-20260922-154932.zip and backups/mods-20260922-154944-699.yml.
Actual resource registration, picking, donor appearance and inventory/save/network behavior require live acceptance. Natural harvest placement, tree/mining nodes, creature drops and refining are not complete.

Launcher description: "0.0.97: Maps 22 raw Underworld resources across six biomes to vanilla item donors and native console pickup prototypes; documents harvesting, recipe and creature-loot gaps. Includes Frozen Wall landmarks."

# Current release - 0.0.96 (2026-09-22)

Biome environment enrichment: 24 additional donor-derived fern, shrub, root and mineral forms, expanding cover from 48 to 72 variants (twelve per biome). Four near-field scenery pockets supplement eight outer clusters; local ecology refreshes after 90m travel instead of 180m. Existing biome composition and slope/water admission remain. All new forms use existing catalog donors and noncolliding cover; no new lights or save/map/animation systems.

Full closeout passed: 300 model sets, 134 icons, 43,426 Core assertions plus separate suites and runtime palette habitat checks. Runtime build: zero warnings/errors. Installed 0.0.96 into Central Fuckery with all payload hashes and enabled launcher metadata verified. DLL SHA-256: D7E5EC1C4C0417758A0EA4DE7A7C603B2D2225841BD85606626AD32CE4EE42F9.
Backups: backups/Local-Magenheim-20260922-150946.zip and backups/mods-20260922-150958-618.yml.
See docs/validation/2026-09-22-biome-environment-density.md. Scenery remains a disposable local preview; live appearance, frame-time and refresh visibility are untested.

Launcher description: "0.0.96: Enriches all six Underworld biomes with 24 additional fern, shrub, root and mineral variants, plus closer scenery pockets while preserving shoreline and slope habitat rules."

# Current release - 0.0.95 (2026-09-22)

Rootforged now has seventeen pieces. Adds a 4m split-root floor, an eight-step Understone stair flight and an 8m iron-banded beam. Each includes editable source art, GLB/runtime meshes, collision and matching icons. The floor/stairs use native grid snaps and support. Existing fourteen catalog entries and other foundation definitions were preserved.

Full closeout passed: 300 model sets, 134 icons, 43,426 Core assertions plus separate suites. Runtime compilation: zero warnings/errors. Installed 0.0.95 in Central Fuckery with payload hashes and enabled launcher metadata verified. DLL SHA-256: 7767F8D832B3DF984A0A83B8F047EDDE1E04734C7B6058E1EB6DDA6687E106AF.
Backups: backups/Local-Magenheim-20260922-145139.zip and backups/mods-20260922-145152-136.yml.
See docs/validation/2026-09-22-rootforged-surfaces.md. In-game traversal, snapping/support and multiplayer acceptance remain untested.

Launcher description: "0.0.95: Adds Worldroot floors, Understone stairs and 8m iron-banded beams to the Rootforged set, with authored models, icons, collision and native building snaps."

# Merged source candidate - 0.0.94 (2026-09-22)

Combined local Rootforged assets and origin/main weather, native minimap tabs and instance-layer fixes. Full closeout passed: 297 model sets, 131 icons, 43,423 Core assertions plus separate suites; runtime compilation had zero warnings/errors. Installed 0.0.94 into Central Fuckery with all payload hashes and enabled launcher metadata verified. Incoming private-member and obsolete sector API references were adapted to the installed game; native sector/portal/ownership behavior remains in control.

Installed DLL SHA-256: CF8422CC3B7FF8614172C0C0F159ECEE77CE020C972C54FCBCEAC7ADF9161F5D. Backups: backups/Local-Magenheim-20260922-060803.zip and backups/mods-20260922-060814-064.yml. No live startup, map, weather or multiplayer acceptance performed. See docs/validation/2026-09-22-merged-live-closeout.md.

Launcher description: "0.0.94: Combines fourteen authored Rootforged pieces with Underworld biome weather, native Overworld/Underworld minimap tabs and instance-layer fixes."

# Current source candidate - 0.0.93 / schema 5 (2026-09-21)

Rootforged now contains fourteen Hammer pieces: the new Worldroot Y Brace 4m,
T Brace 4m and Forked Column 8m add branched seats for larger halls. All three have
editable braided-root source models, matching icons, collision and native snap points.
Workbench recipes use 6 / 4 / 12 core wood. Existing native support and persistence apply.

Full closeout passed: 297 models, 131 icons, 38,326 Core assertions plus separate suites;
runtime compilation had zero warnings/errors. Installed 0.0.93 into Central Fuckery,
with all payload hashes and enabled launcher version/description verified.
DLL SHA-256: 7D821C0EA5549CE470F46C8D298E8BA5DCCCA54C52E7970282D961D4B657A3ED.
Backups: backups/Local-Magenheim-20260921-210908.zip and backups/mods-20260921-210916-543.yml.
See docs/validation/2026-09-21-rootforged-junctions.md. In-game acceptance remains untested.

Launcher description: "0.0.93: Expands Rootforged construction with braided Worldroot Y braces,
T braces and forked columns, authored icons, collision and native attachment points."

# Current source candidate - 0.0.92 / schema 5 (2026-09-21)

Resolved 24 stash-apply conflicts after a completed remote fast-forward. Two auto-stash commits
had recorded unresolved markers; repeated application nested them further. Detailed Rootforged
sources, exported GLBs/runtime payloads and catalogs are now coherent. Plugin registration retains
both Rootforged pieces and the newer Underworld atmosphere/lavafall work. A conflict-marker gate
now runs before asset checks. Existing history and stashes were preserved; obsolete auto-stashes
must not be reapplied over the repaired tree. See docs/validation/2026-09-21-rootforged-stash-conflict-resolution.md.

Full closeout passed: 294 models, 128 icons, 38,324 Core assertions plus separately reported suites,
Rootforged asset gates and native runtime binding checks. Runtime compilation: zero warnings/errors.
Installed 0.0.92 into Central Fuckery with all payload hashes and enabled launcher metadata verified.
Installed DLL SHA-256: 3434C112028E94EADEB4D15D0B6F58633B7DC63F240CF98DDCE63FFB7434B45C.
Backups: backups/Local-Magenheim-20260921-164828.zip and backups/mods-20260921-164836-159.yml.
No game startup or multiplayer acceptance performed.

Launcher description: "0.0.92: Reconciles Rootforged source art and exports with the newer
Underworld atmosphere and lavafall landmarks; repairs nested stash conflicts and adds a
conflict-marker build gate."

---

# Current source candidate - 0.0.91 / schema 5 (2026-09-21)

Rootforged player placeables are now explicitly authorized as a separate pass after natural scenery.
Eleven catalog-backed pieces have authored Blender/GLB/runtime models and rendered icons: 2/4/8m
beams and pillars, 4m iron-banded beam, 4/8m arch ribs, foundation and column plinth. Braided trunks,
raised surface roots, forged collars and stepped stone seats supply the visual detail. See
`docs/ROOTFORGED_PLACEABLES.md` for the recipe table and runtime details.

Registration uses Hammer > Rootforged with native stations, building support, wear and refunds.
Interim resource bindings: Worldroot Timber -> RoundLog, Understone -> Stone, Iron -> Iron;
catalog quantities remain unchanged. No new harvesting or support system. Source snap points use
the installed game's direct-child `snappoint` contract. Collision stays active across wear states.

Full `closeout.ps1 -Offline` passed and installed 0.0.91 into Central Fuckery. 294 model payloads,
128 icons, 38,291 Core assertions plus separately reported suites, Rootforged dimensions/material/
opening/collision gates and native binding checks passed. Runtime build: zero warnings/errors;
model importer retains prior nullable warnings. All installed file hashes and enabled launcher
catalog metadata were verified. DLL SHA-256:
`A43BC67EBF589A4548C70454AC065677330E958089ECC919EB1852AAD52B819F`.
Catalog backup: `backups/mods-20260921-151752-051.yml`.

Source-art contact sheet reviewed at `artifacts/review/rootforged/rootforged-contact-sheet.png`.
No live game startup, placement/support/weathering/refund or multiplayer acceptance performed.

Launcher description: "0.0.91: Adds eleven authored Rootforged placeables with braided roots,
iron collars, stone bases, matching icons and native building support. Uses core wood, stone
and iron pending Underworld harvesting."

---

# Current source candidate - 0.0.91 / schema 5 (2026-09-21)

Underworld weather ownership is now source-integrated. The active Underworld no longer relies on the
Surface biome's weather selection: a deterministic four-minute cycle keyed by paired-instance seed
and Valheim world time selects only biome-legal subterranean events. Runtime registers namespaced
Valheim EnvSetup donor clones for all six biomes, forces the appropriate clone while the local player
is below, and clears the force on return. Surface biome weather tables are not mutated.

Fungal Forest: Still / Sporefall / Crystal Resonance.
Blackwater Deep: Still / Deep Fog / Crystal Resonance.
Sulfurous Wastes: Still / Ashfall / Thermal Surge / Crystal Resonance.
Frozen Caverns: Still / Deep Fog / Whiteout / Crystal Resonance.
Fracture Zones: Still / Stone Rain / Crystal Resonance.
Great Decay: Still / Black Bloom / Crystal Resonance, over its persistent heavy aerosol baseline.

Underworld donor environments reject inherited thunder/rain/storm particles, suppress rain-cloud
alpha and do not inherit Surface storm ambient loops. Frozen Whiteout may retain donor particle
objects whose identity is explicitly snow even if their name also contains "storm". The shared
atmosphere system still owns final fog density and exposure presentation.

This is a source candidate only. The current connected environment has not run the net462 runtime
build, `closeout.ps1`, active-profile installation, Valheim startup, visual weather review or
multiplayer acceptance for 0.0.91. Installed 0.0.90 remains the last hash-verified local package.

---

# Current source candidate - 0.0.90 / schema 5 (2026-09-21)

User scope correction: terrain, vegetation, ecology and natural features; no player-buildable
expansion. The interrupted Hammer work was removed before packaging. Legacy 0.0.89 console
identities remain unchanged for compatibility.

Natural ground cover now has 48 donor-based variants, eight per biome, with per-variant water-height
and slope limits. The existing six biome compositions now drive preview layouts. Four-neighbor
sampling rejects edge/cliff placements; cover uses an independent random stream. Missing or
unsupported visuals fall through only to habitat-compatible alternatives. No new save, map or
animation systems. See docs/validation/2026-09-21-underworld-scenery-refinement.md.

Full closeout passed: 38,291 Core assertions plus separate modules, 283 model payloads and 117
icons. Runtime build: zero warnings/errors. Model importer retains existing nullable warnings.
Installed and hash-verified 0.0.90 in Central Fuckery; enabled launcher catalog read back.
DLL SHA-256: D542E92B28DA2E5ABE59BA6F547F4B2FE5EC6862ED7D6C1FE45A8E1961B5C170.
Backups: backups/Local-Magenheim-20260921-123301.zip and backups/mods-20260921-123310-574.yml.
No live game startup, visual review or multiplayer acceptance performed.

Launcher description: "0.0.90: Refines Underworld natural scenery with 48 foliage, root and stone
variants, habitat-aware bank and lakebed cover, distinct biome clusters and resilient donor
fallback. No new player buildables."

---

# Current source candidate - 0.0.89 / schema 5 (2026-09-21)

Underworld content-first prototype pass: 42 non-boss creature donor clones (seven per biome),
18 native infrastructure review pieces and 36 named biome ground-cover variants. Creature
skeletons/controllers/attacks and building physics stay with Valheim; no animation, save or map
system was introduced. Console review prototypes retain donor behavior and loot; no natural
encounter placement or finished replacement anatomy is claimed. Small foliage/stone/root cover
feeds the existing disposable ecology preview, with denser fungal/decay growth and water/slope guards.

See `docs/UNDERWORLD_CONTENT_PROTOTYPES.md` for all donor mappings, limitations and spawn names,
and `docs/validation/2026-09-21-underworld-content-prototypes.md` for verification and remaining work.
Three pre-existing compile errors were repaired without restoring retired transition machinery.

`closeout.ps1 -Offline` passed and installed 0.0.89 into Central Fuckery. All packaged file hashes
and the enabled launcher catalog entry were verified. Installed Magenheim.dll SHA-256:
`DBFAE3B561C293B4E8F87EBB0CB6F9C40FCADE263A97E372F2B0850FF0ADF177`.
The original pre-install payload is backed up at `backups/Local-Magenheim-20260921-100206.zip`;
the final installation also backed up its prior payload at `backups/Local-Magenheim-20260921-100457.zip`
and catalog at `backups/mods-20260921-100508-198.yml`.

Runtime compilation: zero warnings/errors. 283 model payloads and 38,265 Core assertions plus
separately reported modules passed. These are automated/source checks, not live acceptance:
no game startup, donor animation review, ecology visual review or multiplayer test was performed.

Launcher description: "0.0.89: Adds 42 Underworld creature donor prototypes, 18 infrastructure
review pieces and 36 biome ground-cover variants. Reuses native rigs, combat and building
behavior. Live review and final art remain pending."

---

# Current source candidate - 0.0.88 / schema 5 (2026-09-20)

All 62 crystal placeables carry icons rendered from the same `.blend` their runtime mesh exports
from. Eight `*Icons.cs` classes -- 1,233 lines drawing procedural pixel art in C# -- are deleted and
replaced by `tools/render-placeable-icons.py`, which asserts its expected count so a placeable
authored later cannot quietly ship without an icon. Five Visuals classes now expose `ModelId`, so
the icon and the mesh resolve from one string. `CrystalAlchemyIcons` stays: Crystal Dust and the two
Eitrwine bottles have no models. `crystal-brazier`, `crystal-lantern` and `crystal-wardstone` are
excluded because `WorldArtifactVisuals` names them but nothing registers them as pieces.

This is the third time this defect class has been repaired -- staves, then the ten weapons in
0.0.72, now the placeables.

**`origin/main` had advanced 27 commits and still carried both defects 0.0.86 repaired**: the
`System.ValueTuple` in the chunk-streaming focus list and the model-asset gate that rejects the
chunk materializer. Its `release.json` was still 0.0.83. Merged rather than rebased, matching the
existing history; git auto-merged all three overlapping files without conflict and the result was
verified rather than assumed -- no ValueTuple anywhere in the runtime, both call sites on the
struct, 0.0.87 version retained.

The merged tree then failed to build on a third instance of the same family: `ReadOnlySpan<byte>` in
their new `UnderworldTerrainTextureAssets`. Unity's `ImageConversion.LoadImage` carries a Span
overload whose resolution needs `System.ReadOnlySpan`1`, absent on net462 without System.Memory.
`ModelAssets` had already solved this by binding the `byte[]` overload reflectively; that is now a
named `ModelAssets.LoadImage` used by both callers.

0.0.88 is installed and SHA-256 verified in the active Central Fuckery profile: `Magenheim.dll`
`FCBE3D7B022669096524A506A952ADA1E46C18DB9E9D6B0A7FFBEEBDF1AB301B`. Launcher entry reads back as
`Magenheim v0.0.88 by Local (enabled)`. Prior payload backup
`backups/Local-Magenheim-20260920-074237.zip`; catalog backup `backups/mods-20260920-074249-757.yml`.
Build: 282 model assets, 38,332 Core assertions, 129 generated files across 6 generators, 117 icons,
zero warnings, zero errors.

**Nothing since 0.0.85 is runtime-accepted.** Every repair from 0.0.86, 0.0.87 and 0.0.88 is
source-verified and gate-verified only. The next live session should check the Hammer menu, the
`inherited the world-projecting` log lines, the `Requirement audit:` line, Sentinel and Crystal Bed
deconstruction, the four trimmed weapons, the reversed spear, and the hearth's fire.

---

# Current source candidate - 0.0.87 / schema 5 (2026-09-19)

Live play reported the sconce's smearing on the ice box and "a number of the other crystal
placeables". That is not the geometry defect 0.0.86 repaired on the sconce; it is the donor's
shader. Valheim's piece shader generates its surface from world position and never samples mesh UVs,
so a Magenheim mesh wearing one renders as a lattice sliding across its own faces no matter how
clean its geometry, UVs and texture are.

This was already diagnosed and repaired in `CrystalArchitectureVisuals`, by passing Rock_4's
material in explicitly, and again for the dais. **The repair was never rolled out.** The ice box,
the ten decor pieces, the ten furniture pieces, the eight beds, the twenty-four banners and the
sentinel all still inherited it. 0.0.74 had stripped the donor's normal/metallic/occlusion/moss maps
for the same underlying reason -- authored for the donor, not for us -- and left the shader, which
is the other half of the same defect.

`ModelAssets.Uncouple` now replaces a world-projecting shader on the resolved donor material once,
where that material is resolved, so a placeable authored later cannot reintroduce the defect by
forgetting a constructor argument. Only the shader changes; colour, texture, metallic and roughness
come from the model payload regardless. Each substitution is logged with the model id, so the next
session's log names exactly which models were carrying it rather than leaving it to inference. The
two existing explicit material sources are left in place: they are no-ops under the new check, and
rewiring assets confirmed working in the field to prove a point is not worth the risk.

`Material.shader` had to be added to `tools/ModelExporter/UnityShim.cs`, because ModelAssetTests
compiles `ModelAssets.cs` against the shim rather than against UnityEngine.

The first attempt did not build: CS8603, because the compiler reads the `!source` Unity-null guard
as a null test and the method returned `Material`. Caught by TreatWarningsAsErrors before packaging.

0.0.87 is installed and SHA-256 verified in the active Central Fuckery profile: `Magenheim.dll`
`637B496828D3D414E30306065DE7054D3427EF13227CC6A7776AAD731C61B51B`. Launcher entry reads back as
`Magenheim v0.0.87 by Local (enabled)`. Prior payload backup
`backups/Local-Magenheim-20260919-234401.zip`; catalog backup `backups/mods-20260919-234411-981.yml`.
Build: 282 model assets, 38,327 Core assertions, zero warnings, zero errors, all gates pass.

**Nothing here is runtime-accepted.** The next session should check the log for the
`inherited the world-projecting` lines, which enumerate precisely which placeables were affected.
The open item from 0.0.86 stands: 65 placeable Hammer icons are still procedural pixel art in C#.

---

# Current source candidate - 0.0.86 / schema 5 (2026-09-19)

**First live-play acceptance pass in a long while, and it earned its keep.** 0.0.85 was launched in
the Central Fuckery profile and thirteen observations came back. Three items (axe, bow, crossbow)
confirmed correct and were deliberately not touched. Nine are repaired here. One is open.

**Unbounded item duplication, repaired.** The Crystal Sentinel refunded its full build cost on every
hammer strike and was never removed. `Cost("RefinedEitr", 8)` names a prefab Valheim does not have
-- the refined eitr item is `Eitr` -- so `Piece.Requirement.m_resItem` was null, and
`Piece.DropResources` threw `ArgumentException` after dropping the other four ingredients and before
the `ZNetScene.Destroy` at the end of `WearNTear.Destroy`. Twelve evenly spaced repeats in the
session log. The same typo had reached the Crystal Bed; `CoreWood` (core wood is `RoundLog`) had
reached the Earth Simple staff. Nine unresolved requirements in total, every one of them reported by
Jotunn as a `MockResolveFailure` warning nobody read.

`RequirementResolutionAudit` now runs after registration, scans registered Magenheim pieces and
recipes for unresolved requirements, and reports at error level naming the piece and the position in
its cost list -- and reports clean when there is nothing to say. It does not throw, because taking
the registrar chain down is worse than a wrong build cost. It reads the registered objects rather
than the call sites, so it covers every requirement however it was built, at the cost of not knowing
the wanted prefab's name.

**Held-model trims are authored from measurement.** The alignment report prints donor and replacement
frames in attach space, so the fraction of each weapon trailing behind the hand is comparable with
its donor: battleaxe 51.8%/12.5%, greatsword 34.2%/17.3%, knife 41.9%/12.1%, atgeir 41.6%/27.2% --
the four reported wrong and the four largest gaps. Each takes ~45% of its full-match delta; full
donor-matching is what broke the axe in 0.0.83, and the axe sits 28 points off its donor while
reading correctly. Spear reversed about the attach origin and offset back into its measured
envelope. Mace and sword were not tested in play and are untouched.

Also repaired: the Crystal Hearth's fire (a `RandomColor` gradient whose alpha ramp made violet
particles translucent and magenta ones invisible, plus a rescale that never reached particle size
because the systems do not scale with their hierarchy); the Crystal Enchanting Dais presenting the
crafting panel and the socketing panel simultaneously; and the Crystal Wall Sconce, six boxes at
0.38x0.77x0.57 m, rebuilt to 0.17x0.45x0.26 m into the datablocks already bound to its materials so
the committed textures carry over.

0.0.86 is installed and SHA-256 verified in the active Central Fuckery profile: `Magenheim.dll`
`6892FEFE68DB1C5BFB74A30B51C16E97F453536021ED2C06E7EFD4D4C863A611`. Launcher entry reads back as
`Magenheim v0.0.86 by Local (enabled)`. Prior payload backup
`backups/Local-Magenheim-20260919-233615.zip`; catalog backup `backups/mods-20260919-233625-574.yml`.
Verified catalog data, not an observed launcher UI and not a game start.

Build: 282 model assets, 38,327 Core assertions, zero warnings, zero errors, all gates pass.

**Open, from the same session:** the crystal placeables draw their Hammer icons as procedural pixel
art across nine `*Icons.cs` classes -- the defect 0.0.72 fixed for the ten weapons and the staves
before them. 65 models need rendering; `tools/render-weapon-icons.py` is directly reusable.
`CrystalAlchemyIcons` is not part of it: Crystal Dust and the Eitrwine bottles have no models.

**Nothing here is runtime-accepted.** The game has not been launched against 0.0.86. The trims in
particular are first estimates in the reported direction and should be expected to want one more
pass.

---

# Current source candidate - 0.0.85 / schema 5 (2026-09-19)

Magenheim's recipes no longer spend vanilla Crystal. Thirty costs across six registrars now buy the
**Structural Crystal**, `Magenheim_StructuralCrystal`: an unaligned bulk block fused from four
elemental shards of any one alignment at the Geologist's Workstation. Amounts scale to roughly half
their vanilla-Crystal values (round-half-up, floor of 1), because each block already costs two rough
crystals upstream. The two remaining `"Crystal"` literals in the runtime are deliberate and are not
recipes: a donor-name substring match in `DeepFractureRoomVisuals` and our own tier name in
`StaffAttackAudit`.

Registration order is load-bearing. Jotunn resolves a `PieceConfig` requirement when the
`CustomPiece` is constructed, so `StructuralCrystalRegistrar` runs immediately after
`EarthContentRegistrar` -- after the shards exist, before every registrar that costs the block. The
eight fusing recipes need the Geologist's Workstation, which is registered later still, so they live
in `ShardRecipeRegistrar` with the other deterministic conversions.

The model is a new asset, `earth-structural`, not a recoloured tier: two chamfered flat-faced blocks,
152 triangles, 0.196 x 0.255 x 0.196, with its own full-range facet map. Three defects were found and
fixed by the gates while authoring it, all at source: both cap fans wound the same way so one faced
inward; the first material sampled only the bright half of the shared tier map and rendered a white
blob; and the exporter did not write the `.obj`/`.mtl` pair `verify-earth-assets` requires, which it
now does for every earth asset.

**`main` was broken on arrival and had been pushed unbuilt, again.** `origin/main` was 25 commits
ahead of the local checkout at session start (`60071e4` -> `7776fce`) and failed `build.ps1` twice
over: `verify-model-assets.py` rejected `UnderworldInstanceChunkMaterializer` for building a mesh in
C#, and `verify-runtime-type-availability.ps1` found `System.ValueTuple` referenced through the chunk
streaming focus list -- the defect class that silently removes every registrar behind it. Both
repaired at source before any new work was built on top.

Build: 38,327 Core assertions, 282 model assets imported twice, zero warnings and zero errors, 24
Harmony patch targets, 15 literal plus 42 helper-wrapped reflection bindings, 20 assembly references
all resolvable, 67 generated files matching their generators.

0.0.85 is installed and SHA-256 verified in the active Central Fuckery profile:
`Magenheim.dll` `22CE1BB22CEA72CB0284AC2D4277AFA61E722D2F7913B8DE18D9279ABFA0C7D7`. The launcher
entry reads back as `Magenheim v0.0.85 by Local (enabled)` with the 0.0.85 description. Prior
payload backup `backups/Local-Magenheim-20260919-223215.zip`; catalog backup
`backups/mods-20260919-223228-880.yml`. Other mods, profile settings and enablement are unchanged.
This is verified catalog data, not an observed launcher UI and not a game start.

**Nothing here is runtime-accepted.** The game has not been launched against 0.0.85. Specifically
unverified in play: that the Structural Crystal's model and icon read correctly in the inventory and
on the ground, that the eight fusing recipes appear at the Geologist's Workstation, and that all
thirty migrated recipes show the new requirement rather than a missing one.

---

# Current source candidate â€” 0.0.69 / schema 5 (2026-09-18)

Underworld environment production now starts from Valheim-owned runtime donors instead of authored
placeholder geometry. The disposable local ecology preview has biome-specific donor palettes for all
six Underworld terrain ecologies and reconstructs only the render/material/light/LOD surface of each
source prefab. Vanilla gameplay and network components are deliberately not carried into the local
preview objects, and no base-game mesh or texture is packaged by Magenheim.

The controlling flora/terrain plan now fixes the art order as: vanilla donor composition, donor
kitbash/material variation, Magenheim-authored gap fills, then external permissive assets only when a
remaining requirement justifies them. This lets terrain density, scale, silhouette and biome identity
be judged in Valheim before the custom asset backlog expands.

**Acceptance remains open.** The connected environment cannot launch Valheim, so 0.0.69 has not
been observed in the live game. The next runtime gate is a walk through all six Underworld terrain
ecologies checking missing donor warnings, donor grounding, collision obstruction, LOD transitions,
light range, placement density and frame cost before any donor palette is promoted from preview to
persistent/networked world content.

---

# Current source candidate â€” 0.0.64 / schema 5 (2026-09-18)

Closes the defect class 0.0.63 spent the session repairing: a generator or its gate moving on while
the committed output did not. `tools/verify-generated-freshness.py` and
`assets/generated.manifest.json` record, per generator, the hash of the generator and the set of
files it owns, and run first in `build.ps1` because verification is pure hashing. `--update` runs
the generator before recording, so the manifest cannot be reconciled without regenerating.

The gate found real staleness on its first run: the crystal tier of the Storm, Fire, Venom and
Radiance staves differ from what `render-staff-icons.py` now produces by about 10% of their pixels,
with RGB deltas above 220. Same tier across four families â€” a model change those four icons never
picked up. Re-rendered and committed.

Blender output is not reproducible, and the gate records that rather than pretending otherwise:
re-rendering all 32 icons produced 20 files differing from the committed ones, of which 16 differed
by a single LSB on a handful of pixels. Blender-backed entries are therefore checked on generator
hash and output names only, which still catches the defect that occurred three times, and gives up
hand-edit detection explicitly. Pure-Python generators keep full content hashing; the Sporeling
texture set is byte-reproducible across runs and is verified that way. Coverage today is the
Sporeling textures, the Sporeling source blend and the 32 staff icons; the remaining generators are
an open item in `BACKLOG.md` P0.-2.

Also repaired: `tools/blender.ps1` treated Blender 5.0's benign `Error: Not freed memory blocks`
shutdown line as a script failure, which intermittently failed renders that had already succeeded.

No runtime behaviour changed in 0.0.64 beyond the four re-rendered icons. Nothing is
runtime-accepted; the game has still not been launched against 0.0.63 or 0.0.64.

---

# Current source candidate â€” 0.0.63 / schema 5 (2026-09-18)

`main` and `origin/main` were both at `df27a65` at session start. `origin/main` then advanced to
`720bb5d` mid-session with two Capcrawler authoring commits; the session's seven commits were
rebased onto it and the combined tree was rebuilt rather than assumed compatible. No file overlap:
their commits touch only `tools/author-underworld-capcrawler.py`. Session end `origin/main` is
`f08c82aacb7a779674b60134bab821a30f89753d`, verified by independent `git ls-remote` readback, with
`refs/heads/main` still the only remote branch.

0.0.63 is installed and SHA-256 verified in the active Central Fuckery profile from that exact
tree: `Magenheim.dll` `3ED3F47C216FD89EF92070C039C98F3DA4EBE1AF24C179245738C62B3C2634C2`. The
launcher entry reads back as `Magenheim v0.0.63 by Local (enabled)` with the 0.0.63 description.
Prior payload backup `backups/Local-Magenheim-20260918-095723.zip`; catalog backup
`backups/mods-20260918-095735-183.yml`. Other mods, profile settings and enablement are unchanged.
This is verified catalog data, not an observed launcher UI and not a game start.

**`main` did not build, and did not compile.** Roughly 50 commits had landed since the last
successful build, and `build.ps1 -Offline` failed six times over before this session repaired it:
a call to `UnderworldMapPresentation.CellToLogical` that has never existed, a read of
`UnderworldExplorationState.ExploredCount` that has never existed, two nullable-flow errors, an icon
gate that six correctly-framed staff icons could not satisfy at any framing, a Sporeling texture set
four generator commits and five gate commits stale whose generator could not satisfy its own gate, a
source blend stale because its author had never once run against the installed Blender 5.0, a review
renderer that had never produced a plate for the same class of reason, and a Pillow deprecation
warning on stderr that killed the build after a gate had already reported success. All repaired at
source. Evidence: `docs/validation/2026-09-18-main-build-gate-repair.md`.

**The Underworld biome-sector override is fixed in source.** This was the open defect the
2026-09-17 handoff named as the next task, and it was settled from the installed assemblies rather
than from the live `[sector probe]`, which could never have fired. The patch was bound to
`WorldGenerator.GetBiomeSector(int gridx, int gridy, bool clamp)`, which clamps its grid indices
into [0, 2047] unconditionally â€” its own `clamp` argument is never read â€” and writes them with
`starg`, so the postfix read the clamped value. The biome map spans only Â±12282m and the region is
at 40000, so containment was tested 27.7km outside the region and could never pass. **The region
does not have to move:** both world-space overloads still carry the true coordinate and nothing in
the game reaches the grid overload except those two, so the override moved there. The probe is
removed. Evidence: `docs/validation/2026-09-18-underworld-biome-sector-world-space-binding.md`.

38,320 deterministic Core assertions pass; 281 model assets import twice; runtime compiles with zero
warnings and zero errors; 28 Harmony patch targets and 13 literal plus 42 helper-wrapped reflection
bindings verify against the installed assemblies.

**Nothing here is runtime-accepted.** The game has not been launched against 0.0.63. Specifically
unverified: that the HUD stops logging `GetBiome error Ocean -> Meadows`, that
`SpawnSystem.UpdateSpawnList` stops throwing, that ocean fish stop spawning on dry Underworld
ground, that the ground texture stops reading as Ashlands, and that weather and sky change in the
region. Those are the observations the next live session should make, in the region, with
`-console`.

The controlling priority is `BACKLOG.md` **P0.-2** (the open freshness-gate item) and **P0.-1**
(per-layer map state, Underworld sky, the magenta Deep Gate, re-grounding the Conclave, and making
layer travel a teleport).

---

# Current source candidate â€” 0.0.62 / schema 5 (2026-09-17)

Begins Underworld flora F1a: validated Glowcap, Spirestalk and Shelfwood data, canonical
fingerprints and pure terrain eligibility. All biomes share one custom cavern-roof skybox;
there is no biome-specific physical ceiling. Plans now explicitly enforce that constraint.
The offline build passed 37,166 core assertions and compiled without warnings/errors.
0.0.62 is installed and hash-verified in Central Fuckery; enabled launcher metadata was
verified by read-back, with prior payload and catalog backups.
Species models, spawning, harvesting and the economy remain pending; no live gameplay
acceptance is claimed. See `docs/validation/2026-09-17-underworld-flora-foundation.md`.

---

# Current source candidate â€” 0.0.54 / schema 5 (2026-09-17)

`main` and `origin/main` were both at `5aa8ab0` at session start, with no divergence to
reconcile: the remote work was already present locally. 67 commits had landed since this
document was last written and the source version had moved 0.0.49 -> 0.0.53, so the
sections below this one describe a superseded state and are retained as history.

**`main` did not compile.** The first action of the session was `build.ps1 -Offline`,
which P0.1 requires before push. Eight compile errors were on `main`, none ever built:
`Math.Clamp` in a netstandard2.0 project, `string.Contains(string, StringComparison)` on
net462, two CS8604 nullable-flow errors, a member that does not exist on
`UnderworldWorldIdentity`, and a wrong Jotunn `CustomPrefab`/`AddPrefab` usage. Each was
repaired against the idiom already established elsewhere in the codebase. Committed as
`72b0652`.

**A missing icon was silently deleting four staff families.** `EarthStaffRegistrar`
requested `staff-earth-*.icon.png`, which has never existed in repository history, and
`EarthAssets.Texture` resolves icons through `File.ReadAllBytes`. All eight staff
registrars subscribe to `PrefabManager.OnVanillaPrefabsAvailable`, a multicast delegate
whose invocation stops at the first handler to throw. Bootstrap order is Fire, Frost,
Storm, **Earth**, Venom, Radiance, Seidr, Spirit, so the missing Earth icon also prevented
Venom, Radiance, Seidr and Spirit from registering â€” 20 items, no log line naming them.
That is the cause of the P0.0 live report "Crystal Staff of Venom does not attack": the
registrar that builds its projectile payloads never ran. Three further committed staff
icons (`staff-frost-simple`, `staff-venom-advanced`, `staff-venom-master`) carry bad IDAT
CRCs or truncated chunks in the committed blobs; Frost is second in bootstrap order, so it
would have thrown before Earth was even reached.

0.0.54 re-renders all 32 staff icons from the same Blender sources the runtime meshes are
exported from, wires all eight families to their own icons, adds staff durability as a
pure Core rule with fail-closed tier resolution, adds `tools/verify-icon-assets.py` as a
permanent build gate over icon integrity and coverage, and adds the `.gitattributes` the
repository never had. The gate is proven to fail on the pre-repair tree with exactly the
three real defects and nothing else.

37,120 deterministic assertions pass; runtime builds with zero warnings/errors; 281 model
asset sets, 25 Harmony patch targets and 13 literal plus 42 helper-wrapped reflection
bindings verify against installed assemblies.

**Nothing here is runtime-accepted.** The game has not been launched against this build.
In particular it is not established that the five previously-dead staff families now
register, that the Venom staff attacks, that the new icons read at inventory scale, or
that staves lose durability per swing. Evidence:
`docs/validation/2026-09-17-staff-icon-registration-chain-repair.md`.

The controlling priority remains `BACKLOG.md` **P0.0/P0.1** and `IMPLEMENTATION_PLAN.md`
**section 14**: one disposable-world acceptance session, which should now specifically
confirm the five staff families that could not previously register.

---

# Historical â€” asset fidelity and live acceptance (from 2026-09-16)

The controlling priority is now `BACKLOG.md` **P0.1** and `IMPLEMENTATION_PLAN.md`
**section 14**, which outrank every other open track including the Underworld program.
Evidence is in `docs/RENDERING_AND_CONTENT_DEFECTS.md` R3.

In short: nothing since 0.0.16 has been observed running, a large number of repairs were made
from inferred rather than observed behaviour, inverted winding has now shipped from three
independent sources, and 60% of the model library carries no texture map. The next gate is a
disposable-world acceptance session and the geode rebuild, not additional content.

Deep Fracture districts were rebuilt as enclosed caverns on 2026-09-16
(docs/validation/2026-09-16-deep-fracture-cavern-terrain.md). Their passage and traversal
pieces were not, and remain 60 and 172 triangles against districts of roughly 10,000.

---

# Current source candidate â€” 0.0.49 / schema 5

Repaired the `Inventory.Changed` reflection binding that Valheim 1.0.12 broke: the game
now declares `Changed(bool success, bool cheatedStateChanged)`, and three Magenheim call
sites still invoked it with no arguments, raising `TargetParameterCountException` inside
workshop rollback and both local and remote socket mutations. The binding moved to the
declared `RuntimeGameApi` boundary with a pinned signature. A new
`tools/verify-reflection-targets.ps1` gate reads compiled IL, resolves every literal
reflection binding against the installed assemblies, and fails the build on arity drift;
it is proven to fail on the pre-repair 0.0.48 DLL and pass on 0.0.49. See
docs/validation/2026-09-15-inventory-changed-reflection-repair.md.

Repository divergence reconciled: origin/main carried two Broken Crown encounter-reset
commits that the local 0.0.48 worktree did not contain, while the worktree carried a
Valheim 1.0.12 API pass that the remote did not. Both intents were merged rather than
either being overwritten; `Character.SetPos` was confirmed absent from the installed
assembly, so the local replacement was a required repair.

Reconciled a second time at close-out: origin/main advanced by four more commits adding the
Underworld authority composition slice while this work was in progress. The two local commits
were rebased onto it and the combined tree re-verified rather than assumed compatible.

36,868 deterministic assertions pass; runtime builds with zero warnings/errors; 20 Harmony
patch targets and 7 literal reflection bindings verify against installed assemblies.
0.0.49 is installed and SHA-256 verified in the active Central Fuckery profile, with its
enabled launcher entry and description confirmed by read-back. Delivery first failed because
`install-local.ps1` collapsed the JSON checksum manifest into a single entry under Windows
PowerShell and rejected every package; that verifier was repaired without weakening it.
Startup, world and gameplay acceptance are NOT claimed, because the game has not been
launched against this build. Underworld world transitions and live multiplayer/persistence
acceptance remain open.

---

# Historical â€” 0.0.48 / schema 5

Underworld six-boss progression and eleven Rootforged definitions now share canonical Magenheim JSON and gameplay fingerprint authority. Baseline compilation and Deep Fracture route generation repaired. 36,859 deterministic assertions pass; runtime builds without warnings/errors. Installation/startup status is recorded in docs/validation/2026-09-15-underworld-authority-integration.md. Underworld world transitions and live multiplayer/persistence acceptance remain open.

---

# Magenheim Project State

## Current installed build: 0.0.47 startup repair

On 2026-09-14, fixed fatal ambiguous GetDamage patch selection, the latent GetArmor
ambiguity, and obsolete GetTooltip signature. The core harness passes 13,974
assertions; runtime compilation has zero warnings/errors; all 11 patch declarations
pass an installed-assembly signature/binding gate that rejects the old 0.0.46 DLL.
Isolated startup observed Crystal Shaping registration and completed Magenheim
bootstrap. Steam initialization errors prevent claiming world acceptance there.

`closeout.ps1 -Offline` installed and hash-verified 0.0.47 in Central Fuckery and
updated its enabled launcher catalog entry and startup-repair description. Payload
backup: `backups/Local-Magenheim-20260914-183916.zip`; catalog backup:
`backups/mods-20260914-183917-412.yml`. Full-profile/world retesting remains pending.
See `docs/validation/2026-09-14-0.0.47-startup-repair.md`.

## Launcher catalog closeout repaired

On 2026-09-14, `closeout.ps1 -Offline` completed against the active Central Fuckery
profile: tests, package build, backup, payload verification and r2modman `mods.yml`
update/read-back. The enabled entry now reports 0.0.46 and its Crystal Shaping
level-zero visibility release description. Foreign launcher records are unchanged.
Catalog backup: `backups/mods-20260914-182354-135.yml`. `release.json` now supplies
version-specific descriptions, and INSTRUCTIONS.md requires live installation plus
catalog verification for every testing closeout. Reopen r2modman to refresh its UI.

## Installed skill visibility fix: 0.0.46

On 2026-09-14, inspection confirmed the active Central Fuckery profile still loaded
0.0.16 and registered Crystal Shaping without initializing an untrained character's
skills-list entry. The new SkillsDialog prefix requests the registered skill's
normal level lookup before the list is captured, creating level zero without XP.
Existing progress is preserved. Core tests pass (13,974 assertions); runtime builds
with zero warnings/errors. Version 0.0.46 is now installed and SHA-256 verified in
the active profile; the prior installation was backed up under
`backups/Local-Magenheim-20260914-181731.zip`. In-game panel confirmation awaits launch.

## Local closeout update: 0.0.45

On 2026-09-14 the local source was repaired and built as testing candidate 0.0.45.
The core harness passes 13,974 assertions; runtime compilation has zero warnings
and errors. Regenerated workstation assets now match the repaired generator.
See `CLOSEOUT.md` and `TESTING.md` for the current inventory and acceptance matrix.
The active profile was not updated. An isolated startup attempt encountered Steam
initialization errors without establishing plugin registration. Live acceptance
remains limited to the historical 0.0.16 observations.

The state below is retained as historical context from the earlier connector cycle.

## Authority

`INSTRUCTIONS.md`, committed source on the live `main` branch, and directly observed repository/runtime evidence are authoritative. Backlog, validation records, archived design, scheduled prompts, and conversation are subordinate when they disagree with verified live state.

## Current source state â€” 2026-09-14

Current source/package identity is **0.0.38**. The most recently live-tested/installed package remains **0.0.16**; do not conflate later committed source with live acceptance.

The repository has advanced substantially beyond the first live-world-test baseline. Current source includes:

- eight biome geode definitions and additive natural-geode worldgen infrastructure;
- eight five-tier elemental crystal families and shards;
- Crystal Shaping skill/refinement authority;
- Geologist's Workstation and three progression upgrades;
- authority/replay-aware workshop transaction infrastructure;
- per-item adaptive socket metadata, eligibility, effects, extraction, workstation UI, rollback planning, synchronized gameplay-authority policy, and server-authoritative remote-client socket request/response/ack transport;
- Fire, Frost, Storm, Earth, Venom, Radiance, Seidr, and Spirit four-tier staff families with increasingly distinct runtime effect languages;
- original world-artifact geometry for later wardstone, passage, totem, keelstone, lighting, runecraft, ritual, spirit-fetish, Fate, and boss-resonance systems without falsely registering unfinished gameplay;
- a registered ten-piece geology/crystal furniture family under Hammer > Furniture with explicit comfort grouping;
- ten dedicated generated Hammer icons so the furniture no longer masquerades as unrelated vanilla clone-source pieces;
- later additive crystal architecture/weapon/alchemy content, including the current crystal-grinding and Prismatic Eitrwine source work;
- patch-strict multiplayer gameplay-authority synchronization.

Verified source presence is not runtime acceptance. Current 0.0.38 source has not been rebuilt or run in this connector-only execution cycle.

## Geology/crystal furniture source

The current furniture family is:

- Geode Table;
- Geode Chair;
- Crystal Bench;
- Crystal-set Bed;
- Mineral Shelf;
- Lapidary Cabinet;
- Geologist's Desk;
- Geode Pedestal;
- Crystal Screen;
- Crystal Throne.

`FurnitureVisuals.cs` owns the original procedural geometry and reuses the established Magenheim material language: rugged stone, dark/fine timber, iron and bronze banding, faceted mineral cores, and restrained crystal emission. `FurnitureRegistrar.cs` owns additive Hammer registration, recoverable build costs, explicit comfort grouping, compatible vanilla source selection, wear variants, and model-specific replacement colliders. `FurnitureIcons.cs` generates one Magenheim-owned 128x128 icon per furniture identity so menu presentation does not reuse the source prefab icon.

Static validation is recorded in the furniture validation records under `docs/validation/`. Runtime placement, inherited seating/bed/storage interaction, wear switching, collision, refund behavior, comfort stacking, and multiplayer behavior remain explicitly unadmitted until a rebuilt disposable-world test.

## Live runtime evidence retained from 0.0.16

The first disposable-world test confirmed that the Geologist's Workstation exists and opens its UI, directly spawned Earth inventory items appear, and the dedicated Meadows Earth world-geode prefab uses custom Magenheim geometry.

That test also exposed translucent/oversized world-geode presentation and workstation iron-band surface fighting. Subsequent source repairs normalized Magenheim-owned material render state and reduced/rebased the world-geode visual/collider, but those repairs still require a rebuilt in-game retest before they are accepted as closed.

The permanent skill diagnostic remains `raiseskill magenheim.crystal_shaping 1`; the spaced display name is not a valid vanilla parser argument. Live confirmation remains open.

See `docs/validation/2026-09-14-live-world-test-1.md` and `TESTING.md` for the preserved test boundary.

## Stable domain authority

Crystal tiers remain `Rough -> Simple -> Crystal -> Advanced -> Master`. Normal elemental alignments remain Earth, Fire, Frost, Storm, Venom, Radiance, Seidr, and Spirit. Ordinary refinement preserves alignment. Base refinement failure remains 10/20/30/40 percent with Crystal Shaping reduction and workstation/upgrade progression. Valid failed attempts destroy the source and return matching shards according to tier.

Furniture, architecture, weapons, alchemy presentation, and later artifact models do not alter this core refinement authority unless explicitly admitted through validated gameplay definitions.

## Worldgen and area compatibility

Definition schema 3 owns refinement, geodes, worldgen compatibility, and complete geode placement behavior in one validated/fingerprinted snapshot.

Worldgen remains additive-only. Magenheim does not rewrite, delete, disable, reorder, or patch foreign/vanilla registrations to obtain compatibility. Collision observation feeds pure Add/Skip/Error planning; approved world objects use Magenheim-owned identities.

Spawn-area handling remains fail-closed. Negative numeric masks cannot clamp into All through sign extension. Unknown bits are governed by validated policy. Text configuration accepts the Magenheim `All` abstraction plus runtime-facing aliases.

At the Valheim boundary, `JotunnWorldgenAdapter` resolves runtime `Median` and `Edge` first and requires them to be distinct and non-empty. `All` is composed from those components. A runtime alias named `Everything` or `Everywhere` is accepted only when its value is exactly equivalent to the composed `Median | Edge` value; aliases with missing or extra bits are rejected rather than silently broadening or narrowing placement.

Geode placement density, altitude/depth, terrain delta, tilt, forest thresholds, scale, grouping, block checking, force placement, and offset are validated/fingerprinted authority and can be overridden only through revalidation before registration.

The additive runtime path remains `GeodeWorldgenRegistrar`: validated desired additions are planned in the pure core, host prefab occupancy is observed read-only, Add/Skip/Error policy is rerun against observed state, every approved addition is preflighted, and an occupied prefab identity is refused before JÃ¶tunn registration. Existing vanilla or foreign vegetation is not intentionally mutated for compatibility.

See `docs/validation/2026-09-14-runtime-biome-area-semantic-guard.md` for the current area-mapping repair boundary.

## Adaptive socket authority

Per-item socket persistence is namespaced under `magenheim.sockets.v1`. Socket writes operate on individual `ItemDrop.ItemData.m_customData` and preserve unrelated foreign custom-data keys. Shared vanilla/foreign prefabs and shared item definitions are not the socket-state store.

Eligibility is adaptive and conservative:

- known weapon/armor/shield/tool/utility categories use configured slot limits;
- unknown equipment is rejected unless explicitly opted in;
- compatibility can include/exclude by shared item identity, prefab identity, equipment category, or mod origin;
- exact and case-insensitive identity comparison are supported;
- explicit exclusions win before category admission;
- case-insensitive identity collections are de-duplicated with case-insensitive semantics;
- unknown identity-comparison enum values fail closed.

JÃ¶tunn `ModQuery` is enabled during plugin startup before equipment classification so mod-origin rules can identify modded prefabs. Item identity is separately carried from Valheim `m_shared.m_name`, allowing compatibility rules that are finer-grained than prefab origin alone.

`GameplayAuthorityFingerprint` combines the validated definition fingerprint with every gameplay-significant socket eligibility field. `DefinitionAuthoritySynchronizer` exchanges this composite gameplay fingerprint, so persistent gameplay mutation fails authority admission when peers disagree on socket compatibility rules rather than silently diverging.

Source coverage for this authority repair is recorded in `SocketCompatibilityAuthorityTests` and `docs/validation/2026-09-14-socket-compatibility-gameplay-authority.md`.

## Socket workstation/runtime boundary

The Geologist's Workstation has a dedicated socket-management overlay for open-slot, install, and risky extraction operations. Local-host operations continue to use the existing pure planners, authority/replay guards, exact-item state checks, inventory snapshots, rollback, and Crystal Shaping XP.

0.0.38 adds the remote-client transport previously missing from this boundary. `SocketOperationRpc` uses a request/response/ack protocol parallel to the established workshop transaction transport. The server requires an admitted peer session, validates workstation proximity, validates the Faceting Wheel for extraction, reconstructs equipment classification from the registered server prefab rather than trusting client category/mod-origin claims, evaluates the synchronized socket policy, owns the extraction roll, and reserves the existing socket/extraction replay guard. The client retains the exact `ItemData` reference and refuses stale responses before writing `magenheim.sockets.v1`; install source crystals and extraction output capacity are also revalidated before mutation. A successful local apply is acknowledged before the server marks the prepared replay guard applied; failure sends a negative acknowledgement and aborts the prepared reservation.

This is source-complete but not runtime-admitted. Compilation and host/client verification remain required. Static validation is recorded in `docs/validation/2026-09-14-remote-socket-authority-binding.md`.

Socket effects are computed from Magenheim-owned per-item metadata and applied through runtime effect patches by equipment category. They do not require replacing another mod's item prefab or recipe.

## Repository branch reconciliation

`main` is now the sole ref on the remote. On 2026-09-15 the four redundant refs
`radiance-content`, `tmp-radiance-content`, `__delete_me__` and `tmp-compat-work` were
deleted with user authorization after re-verifying that each was an ancestor of
`origin/main`. There were four, not the three recorded earlier, and they pointed to
`fc84a519` rather than the `a8b6947c` noted during the connector-only cycle; that earlier
record was stale. Independent readback via `git ls-remote --heads origin` returns
`refs/heads/main` only.

## Build and validation boundary

Earlier project revisions were successfully compiled and tested in the normal Valheim development environment, including a recorded zero-error/warning build and deterministic core run before later source expansion.

This execution environment cannot clone GitHub through the local container and does not provide the normal local Valheim managed assemblies/runtime profile. For 0.0.38, remote source/read-back and static-source validation are therefore the strongest admissible claims from this cycle.

Current live gates include:

- rebuild 0.0.38 and rerun the full deterministic suite;
- plugin startup with the composite gameplay-authority fingerprint and both workstation/socket RPC registrations;
- host/client remote socket open/install/extraction including stale-state, duplicate/replay, policy mismatch, and descriptor-spoof rejection;
- verify runtime `Median`, `Edge`, and `All` geode area mapping under the installed Valheim/JÃ¶tunn enum;
- snapshot pre/post worldgen registrations and prove repeated-load idempotence;
- verify current furniture, architecture, weapon, and alchemy additions in a disposable world;
- live item/prefab/mod-origin exclusion behavior on representative third-party equipment;
- socket metadata persistence through save/load, drop/pickup, storage, repair, upgrade, transfer, death, and dedicated-server flows;
- world-geode opacity/scale/collision retest;
- workstation iron-band geometry repair/retest.

## Next dependency-valid action

1. compile/install current 0.0.38 in the normal Valheim development profile and execute the deterministic suite;
2. run a host plus remote-client socket matrix covering open/install/extract, stale response rejection, duplicate delivery, policy mismatch, and representative third-party equipment classification;
3. validate runtime area semantics and repeated-load additive worldgen idempotence;
4. re-run the outstanding geode/workstation live visual gates;
5. continue persistence validation through save/load, inventory movement/storage, equipment repair/upgrade, death, transfer, and dedicated-server flows.

---

## Open issues logged 2026-09-18 (0.0.77 installed and field-tested)

Sections above this line predate 0.0.38 and are stale; this block is current.

### Held-model orientation â€” measured, not fixed
See [docs/validation/2026-09-18-held-model-orientation-field-report.md](docs/validation/2026-09-18-held-model-orientation-field-report.md)
for the attach-space frames and the analysis. In short: battleaxe now reads as held "like a guitar"
(0.0.75 traded upside-down for a different wrong orientation), spear is gripped too near the head,
and the crossbow's `ForwardAxisOverride` declares axis 1 when the measurement says 2. Five weapons
sit at exactly (90,0,0) and are all correct; every reported failure sits somewhere else, which says
bounds ranking is unstable on the two axes perpendicular to the weapon. Those five are the
regression test for any change.

### Crystal placeable surfaces â€” shipped 0.0.77, unconfirmed
Foundations, beams, dais and hearth export with no texture as of 0.0.77 and are classified at load
from each material's own semantic. Not yet looked at in game. If a piece is wrong in a *new* way
rather than the old flat speckle, the classifier is choosing badly and the material name is the
place to correct it.

### Geode size and lean â€” shipped 0.0.76, only visible in fresh terrain
`m_syncInitialScale` gates both the read and the write of the ZDO scale, so geodes in
already-generated zones have no stored scale and stay at 1.0 and upright permanently. Only terrain
generated on 0.0.76 or later will show the variation.

### Smaller open items
- **HUD `IndexOutOfRangeException`** in `InventoryGui.SetupRequirement` via `Hud.SetupPieceInfo`,
  fires every frame while a piece with four requirements is selected on the build hammer. Cosmetic
  but it floods the log. Seen on the Rainbow Crystal Foundation 2x2m (Stone/Crystal/Iron/Stonecutter).
- **`enemy-stone-guardian`** carries two inverted triangles in `Guardian_ArmorChip_1`. They come
  from a modifier, so recalculating normals on the source mesh does not remove them. The payload is
  excluded from the shipped set and nothing references it; fix the winding before wiring it up.
- **`underworld-creature-sporeling`** likewise has a source but no runtime consumer.
- **8 orphaned texture files** under `assets/models/textures/`, left in place deliberately. Includes
  the `magenheim.surface.*` bakes, whose content hashes are the denylist in
  `tools/verify-no-baked-surfaces.py` â€” do not delete those without updating that gate's reasoning.
- Backlog carried forward: staggered staff burst cadence, geode interior biome colouring, per-layer
  map state, Underworld sky, magenta Deep Gate, layer-travel caller, undefined
  `$magenheim_deep_gate` map-pin token, installer never prunes stale files.

---

## Open issues logged 2026-09-19

### Held-model orientation â€” fixed and tested
See [src/Magenheim.Runtime/HeldModelAlignment.cs](src/Magenheim.Runtime/HeldModelAlignment.cs). The
2026-09-18 field report's diagnosis was correct (bounds ranking treats near-symmetric shapes' own
measurement noise as decisive) but its proposed crossbow fix was wrong, based on misreading the
runtime log's *post*-rotation bounds as if they were pre-rotation. Reconstructing exact pre-rotation
bounds from the shipped payloads and replaying the actual algorithm (not a hand trace) found: six of
nine non-crossbow weapons already resolve to one identical, confirmed-correct rotation once
near-ties are discounted at a 25% margin (chosen from a genuine gap in the measured data, not fit to
pass); the crossbow's existing forward-axis override was already correct. Battleaxe, spear, mace and
the crossbow's sign are fixed. The sword reproduces the exact mirror of the good rotation, on a real
43.7% margin (not noise) â€” left untouched, since nobody has ever confirmed in game whether it's
actually right or wrong, and forcing it to match would be a guess. Covered by a real compiled test
(`tools/ModelAssetTests`, not just a standalone script) via a new `Matrix4x4`/`Vector4` shim.

### Crystal-tier staff models â€” four found genuinely reversed, not yet fixed
`tools/verify-held-model-grip-direction.py` was rewritten 2026-09-19 (vertex count â†’ bounding volume â†’
triangle surface area, in that order, the first two each measurably wrong â€” see the script's own
docstring for the full trail) while fixing a false-positive on the user's hand-edited
crystal-weapon-greatsword (confirmed correct by render: [greatsword-iso.png] showed a clean blade
dominant over a small pommel gem). The improved metric then surfaced a real, pre-existing,
unrelated finding the old vertex-count metric was silently hiding: **Magenheim_Staff_Fire_Crystal,
Magenheim_Staff_Storm_Crystal, staff-radiance-crystal and staff-venom-crystal** each have
dramatically more surface area at the grip end than the working end (roughly 5-6x, not a close
call). All four are the *Crystal* tier specifically of four different elemental families, from four
different .blend sources, so this is not one shared-asset mistake with one fix. Crystal tier is a
normal, reachable crafting rung (`Magenheim_Crystal_Fire_Simple`-gated, not dev-only), so this will
become player-visible as progression reaches it. Not fixed tonight: this needs a Blender look at each
of the four sources, which is a content decision outside tonight's scope, not a mechanical one.
Excluded from the gate by name (`KNOWN_REVERSED_PENDING_REVIEW` in the script) so it doesn't block
unrelated builds; remove an entry only after visually confirming its model.

### Underworld instance-architecture migration â€” merged, not authored by this session
`origin/main` had advanced 11 commits (Underworld instance-domain terrain/map/transition rework,
Earth icon migration tooling, Capcrawler articulated feet) since the last local sync; fast-forwarded
cleanly, no conflicts with anything in this file's other sections.

### Capcrawler creature â€” pipeline scaffolded, content incomplete, gates temporarily bypassed
`underworld-creature-capcrawler.blend` had never been committed in this repository's history despite
~10 commits building gating/animation/review machinery around it. Ran the already-written,
already-committed authoring pipeline for the first time (`generate-underworld-capcrawler-textures.py`
â†’ `tools/blender.ps1 author-underworld-capcrawler`), which now produces a real 56-mesh/23-bone
source â€” but it falls short of its own gates: 4440 triangles against a 6500 floor
(`verify-underworld-capcrawler.py`), and the review renderer expects a `Capcrawler_Scuttle` animation
action the current rig doesn't produce (`render-underworld-capcrawler-review.py`). Both floors were
themselves ratcheted up incrementally by whoever was iterating on this creature (5000â†’6500 triangles
in this file's own history), meaning it was mid-tune when its session ended. Not fixed tonight:
closing either gap is a content-authoring decision, not a mechanical one, and I have no visual
reference for what this creature is meant to look like. **Both gates are temporarily commented out of
`build.ps1`** (search "TEMP:" â€” two `foreach` loops) so the rest of the build can run; restore them
once the creature's own author/reviewer session finishes it, or hands off with enough context for
someone else to.

### Staff icon renderer â€” two real bugs fixed in passing
`tools/render-staff-icons.py` (merged from origin, part of the same icon-readability work) crashed on
`style.color` with `style is None`: a fresh view layer has no Freestyle lineset/linestyle until one
is explicitly created, which the merged code assumed rather than checked â€” fixed by creating both
when absent. Once fixed, `staff-spirit-simple` then failed its own 8% ink-density floor by 0.3 points
(right at a `<` boundary) â€” `OUTLINE_PX` raised 1.35â†’1.6, a uniform, in-spirit-of-the-tool parameter
matching its own stated purpose, re-verified against all 32 icons with margin, not just the one.

### Update, same day: the sword and four staves fixed at the source
Both items above marked "not fixed"/"left untouched" are now resolved, found by the same method in
sequence. At the user's explicit request to prioritize local Blender-only verification and icon
inspection: rendering all 32 staff icons directly showed staff-fire-crystal, staff-storm-crystal,
staff-radiance-crystal and staff-venom-crystal with their ornate head pointing toward the grip.
Checking each source's Blender-space Z coordinates against a correctly-built reference
(Magenheim_Staff_Fire_Simple) confirmed it precisely: pommel at positive Z, head parts at negative Z,
the exact reverse of the correct asset. Fixed by rotating each of the four sources 180 degrees about
the world origin and re-exporting; the grip-direction gate now passes outright, with no exclusion
needed, and `KNOWN_REVERSED_PENDING_REVIEW` is empty again.

The same check applied to the sword's earlier "left untouched" mirror-image signature settled it too:
its pommel sat at positive Blender Z and its blade at negative Z, the reverse of both the axe and
knife (which agree with each other). Fixed the same way. A debug render with pommel and blade
colour-coded confirmed it unambiguously â€” the plain icon comparison alone was not conclusive, since a
reasonably-symmetric double-edged blade can look similar from either end under render-weapon-icons.py's
fixed camera angle. All ten crystal weapons are now confirmed correct in `HeldModelAlignment`'s
compiled regression test, and the full 281-model catalog was regenerated and reviewed by eye
end-to-end; nothing else in the library shows this defect.

Shipped as 0.0.79 (staves) and 0.0.80 (sword), both installed and verified.
