# Magenheim Changelog

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
