# October 1 live repair — twelve requested items

Current evidence: 0.0.164 Central Fuckery run preserved in ignored
`dist/world-run-164-errors.log` and four user screenshots. Prior 0.0.164 source
changes remain present and must be preserved. This is a work record, not acceptance.

| Request | Work / acceptance status |
| --- | --- |
| 1 Invisible Raven | Route native Raven into the local player's scene before Spawn; restore transferred renderer states and preserve inactive prefab flags. Live visibility pending. |
| 2 New-world companion selection dialog | Adapter 1.0.13 calls the vendor starter routine once when native HUDs are ready and its world key is absent. Existing history retained and skip reasons logged. Original missed-trigger cause not proven; live startup pending. |
| 3 Last Argument knives in both hands | Use native VisEquipment left-hand attachment instead of rejecting Valheim's non-Humanoid Animator. Live pair/animation acceptance pending. |
| 4 Distinct Underworld weapon revisions | Twelve r3 models replace working-part materials, widen heads/limbs and thicken bodies with biome hardware/organic sweeps; preserve grip and longitudinal working extent. Sources, exports and icons rebuilt and checked. |
| 5 Biome staves and magenta staff icons | All 32 source image nodes were unbound. Restore exact owned images, pack and multiply authored tints; verify dependencies after export and render all 32 icons. Icebind derivative also receives r3 model changes. |
| 6 Armor / Null Mantle icons | Render owned native donor equipment through installed Jotunn RenderManager into new 256px sprites, including Null Mantle. Actual Unity render/readability acceptance pending. |
| 7 Custom capes | Six native donor silhouettes (Lox, Linen, Ash, Wolf, Asksvin, Troll Hide) retain skinning/cloth and receive owned woven ribs/stitched hems. Live textile/cloth acceptance pending. |
| 8 Crystal bed edge z-fighting | Eight sources have side-wall butt joints, trimmed side bands and metal coping 3 cm above stone. Exports/icons rebuilt; canonical stone/water/metal/crystal maps preserved. Live edges pending. |
| 9 Stations / basins | Native placement on both worlds; fix cube half-extent authoring mismatch that detached components. Rebuild six assemblies and carve sixteen chevrons into a continuous flowstone bowl. Mesh winding/UV/icon checks pass; live Surface placement pending. |
| 10 Crystal floor / pillar support | Remove donor LODs and obsolete donor colliders from native wear bounds; use Stone material support consistently. Live cantilever/stack support pending. |
| 11 Rootforged invisible / geode storage | Remove donor LOD/collision and retain separate wear visuals. Eight registered geode identities cover both worlds; each gets a native recoverable 50-geode pile in Stacks, represented by fourteen 30cm geodes in 9+4+1 layers. Live build/recovery pending. |
| 12 Gate freeze / centre arrival | Preserve foreign-scene views during native ZNetScene.RemoveObjects using native keep earmarks; regression verifies both directions. Entry now targets (0, terrain+1.28, 6), faces the stone circle and requires Fungal terrain. Original freeze is not fully attributed; live traversal/return pending. |

The log also contains 3,684 ZenUI CraftingPanelGrid.UpdateGuiOptimized null
exceptions from a missing Behaviour while inventory UI is open. Trace its native
UI references and repair the compatibility boundary without disabling the mod.

### Existing compatibility adapter update

The user's authoritative instruction is to host cross-mod repairs in the existing
Magenheim 1.0 Compatibility Adapter, leaving source mods authoritative. Source is
in the sibling `Jewelcraft1.0` repository. Version 1.0.13 contains a runtime adapter
for ZenUI's crafting grid; it preserves referenced native controls during cleanup
and rebinds missing references to existing controls. No vendor assembly is edited.
Do not claim the precise original null field is proven: installed native icon,
equipped, food and tooltip references all exist in the vanilla prefab. A foreign
modified template or lifecycle mismatch still needs confirmation in the next run.

Broadcast regression tests and the actual runtime transpiler tests pass, including
branch/exception metadata preservation, rejection of zero/two cleanup call sites
and installed native/ZenUI target signatures. Native Unity rendering and the
absence of the repeating null exception still require live acceptance.

Installed adapter 1.0.12 in Central Fuckery on October 1 at 18:22, after the user
closed r2modman. Adapter-only installer backed up the existing adapter/catalog,
verified copied DLL hashes and catalog read-back, and verified all 45 foreign/game
binaries were unchanged. Jewelcrafting, Offline Companions, ZenUI and Zen.ModLib
remain byte-for-byte unchanged from before this adapter update.

Adapter 1.0.13 was subsequently installed at 19:07 with the companion callback
bridge and the same 45-file preservation checks. Backup is
`Jewelcraft1.0/backups/runtime-20261001-190726`; receipt is ignored
`Jewelcraft1.0/dist/runtime-install.json`. Its tests verify the installed vendor
starter signature and native Game lifecycle through metadata, in addition to
the real ZenUI transpiler. Selection keys are never cleared automatically.

### Offline evidence and remaining acceptance

The inspected icon contact sheet is ignored `dist/repair-165-visual-review.png`.
Station r3 fixes were checked by model winding/UV gates; numerical slivers from
the carved bowl's boolean cuts are welded/dissolved in its source authoring step.
Packed crystal-bed exports use content-hash texture names. The authored-surface
gate accepts those only when the hash is valid and decoded RGBA pixels equal
the canonical model-specific artwork; arbitrary replacements still fail.

Do not mark the gate freeze, Raven, starter panel, native equipment icons/cloth,
Surface station placement, Rootforged appearance or structural support as live
accepted after compiling or rendering source art. All require the next game run.

### Installed candidate 0.0.165

Full `build.ps1 -Offline` passed with zero compile warnings/errors, 114,198 core
assertions plus module suites, the actual production world-scope/streaming
regression, 531 source/GLB/runtime model sets, 224 icon checks, 974 generated
outputs across 28 generators, 93 explicit patch targets, 23 dynamic resolvers
covering 676 targets, and 83 direct/46 helper reflection contracts. The owned-art
gate also rejects unrelated valid hashes, different artwork and invalid hashes.
Offline dependency vulnerability auditing was unavailable.

`closeout.ps1 -Offline` completed at 19:19: Magenheim 0.0.165 is enabled in
Central Fuckery, copied payloads and launcher catalog were verified, and the
previous installation/catalog were backed up. Runtime SHA-256 is
`9E6F3B4A48452F2176DA1AF173F19511228FF3ECC1FF8DAF9F1321D11212C639`.
Backups: `backups/Local-Magenheim-20261001-191854.zip` and
`backups/mods-20261001-191913-701.yml`. Install log:
`dist/repair-165-install.log` (ignored). Native game was not launched.

Launcher release description: “0.0.165: Repairs Raven, paired knives, building
support and station placement. Rebuilds staff textures, Underworld weapons,
stations and bed edges; adds armor icons, distinct capes and geode piles.
Repairs gate streaming and central arrival.” This describes the implemented
candidate, not completed live acceptance.

### Follow-up 0.0.166: native teleport timing

Further native IL inspection explains the premature Sulfurous Wastes discovery:
`Player.TeleportTo` stores its destination but `UpdateTeleport` waits for a
two-second timer before moving the transform. `UpdateBiome` samples that transform
after one second. World membership has already changed, so the Surface gate's
old coordinates can be sampled as Underworld geography during that interval.

The direct `Player.UpdateBiome` prefix defers only while a player in the registered
Underworld scene is natively teleporting. Normal discovery resumes after the
native teleport completes; Surface behavior is unchanged. Owned weather, fog and
weather gameplay also wait for native teleport completion. No replacement
teleport or transition state is introduced. Production-prefix regressions cover
in-flight entry, completed entry and unchanged Surface teleport behavior.

Completion requires appropriate tests/asset renders, full build, active-profile
installation with backup/hash/catalog verification, and an explicit boundary
between offline checks and the next live world test. Do not reset existing worlds
or loot to make these checks pass.

Full offline build 0.0.166 passed: 114,198 core assertions plus module suites,
the production teleport/streaming regression, 531 model sets, 224 icons,
974 generated files across 28 generators, 94 explicit patch targets and
23 dynamic resolvers covering 676 targets. Runtime compilation reported zero
warnings/errors; the standalone world-scope test harness reported nullable/stub
warnings. Live game acceptance remains pending.

Active-profile closeout completed at 22:11. Installed runtime SHA-256:
`382D1CE5590C6F654AF06BB9D9948AD43B07F5F5378173106F01A67274B5545D`.
The prior installation and launcher catalog are backed up as
`backups/Local-Magenheim-20261001-221102.zip` and
`backups/mods-20261001-221122-828.yml`. Installation log is ignored
`dist/repair-166-install.log`. Payload hashes and the enabled 0.0.166 launcher
entry were verified. Adapter 1.0.13 runtime/patcher hashes still match its receipt;
ZenUI remains in the catalog. No native game startup or world test was performed.

Verified launcher description: “0.0.166: Defers Underworld biome discovery and
weather until native gate teleport finishes, preventing false Sulfur arrival.
Includes visual, crafting, building, geode-pile and central-stone repairs from
0.0.165.”
