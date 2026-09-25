# Underworld live feedback repair - 0.0.105

User screenshots and the active profile log establish that 0.0.104 failed live acceptance.
The roof stayed blue, the wrong magenta gate remained, arrival intersected the central stone,
collidable scenery relocated during movement, and the small map showed Surface biome names.

## Root causes and repairs

- The player strips Skybox/Panoramic. Its name in globalgamemanagers was not evidence that a
  usable shader existed. Load native ParticleUnlit (asset e1e858596580e684788c10ca60865118),
  map the unchanged authored HDR roof onto a camera-centred background shell, disable fog and
  fading for that material, and restore the original sky/cloud state on exit. No roof collider.
- The old gate searched location containers and silently retained Morkhalla. LastBossGate is
  actually a directly loadable soft asset, 22a0a0b6f09802a0f861b9789a1bde32. Load it directly,
  retain its native materials, strip copied encounter authority, normalize the mesh foot and
  recalculate LOD bounds. Its high/low mesh bounds are about 35.78 x 27.53 x 20.08m.
- Entry now resolves outside the front of the actual admitted return gate, positioned 36m from
  the conclave centre. No destination is the central monolith. Preserve incoming role checks
  and durable Surface return anchors. Not-ready gate produces a refusal diagnostic.
- The ecology adapter previously destroyed its entire collidable patch after moving 90m, then
  regenerated relative to the player's new position. Use deterministic 64m cells, seed each by
  world/cell, load within three cells, retain four, and never reposition survivors. Two pockets
  per cell increase forest density; admission/biome/cover habitat checks remain. Massive rocks
  already found by the user are retained by the same fixed-cell mechanism.
- Fungal puffcap roots/trunk/branches now share one fused, curved mesh. Remove disconnected
  decimation slivers before UV baking; verify connectedness and unit corner normals. All sixteen
  Fungal Forest models regenerated through their recorded author/export command.
- Increase local relief amplitudes from 10/5/2m to 18/8/3m before biome scaling. Regional relief
  and monument height limits stay unchanged. Terrain algorithm identity updated for peers.
- Fungal calm visibility is roughly 0.8km instead of roughly 11km to communicate spore haze;
  clear rocky biomes retain distant cliff views. Biome labels bind to native Underworld samples,
  respecting large-map selected tab, hover and exploration, and refreshing Surface labels on exit.

## Reconciliation

Integrated existing main through c215114, including physical per-player instance classification,
chunk focus repair, gate role/relog return safety, instance-scoped map persistence and the new
Frozen/Great Decay locations. Resolved the stash conflict in gate transit by preserving incoming
persistence and adding only readiness/arrival changes. No global player layer setter restored.
Remote fetch initially failed DNS; retry succeeded with no additional incoming commits.

## Evidence and acceptance limits

Runtime compilation: zero warnings/errors. Core suite: 47,068 assertions plus separate modules.
Native asset IDs checked against the installed SoftRef manifest; actual gate hierarchy, mesh
bounds, shader properties and render-state metadata inspected read-only using UnityPy under
ignored artifacts. These checks do not establish live rendering correctness.

Fresh fungal canopy four-view review: artifacts/review/fungal/fungal-canopy-review-01.png.
The third row is the repaired puffcap. Source and export normal checks reject the disconnected
slivers seen in the first rebuild. Offline model review is not an in-game visual acceptance.

Required live
checks: walk across cells on collidable rocks; gate approach/return including relog; roof at day
and night and after Surface return; map tab/biome labels; forest density, haze and frame time.
TESTING.md contains the detailed procedure. No new startup or in-world pass is claimed.

## Completed reconciliation and delivery — 2026-09-25

- Fetched origin successfully: remote main and local HEAD both c215114 before this change;
  no additional incoming commits or merge were required. Preserved the staged and unstaged repair.
- Initial full build rejected the changed fungal generator's stale freshness record. Ran
  `verify-generated-freshness.py --update underworld-fungal-forest-models`, which authored and
  exported all sixteen models before recording 48 outputs. Catalog source/export hashes updated.
- Regenerated and inspected the four-view canopy review; the puffcap has connected root,
  trunk and branch geometry. The connectedness/normal gate also passed.
- `build.ps1 -Offline` passed: 47,068 Core assertions plus module suites, 344 model import
  cases, asset gates, 47 Harmony targets, 36 direct reflection bindings, 42 helper contracts,
  and 23 runtime assembly references. Runtime compile: zero warnings/errors. The separate
  model-import test project emitted six nullable-reference warnings. Twelve reflection bindings
  remain dynamic, six legacy Earth icons remain below target, and the existing Capcrawler
  source/render gates remain deferred. No online dependency audit or live acceptance claimed.
- Continued the build backlog: runtime/Core compilation and Core tests now run before expensive
  asset checks; a failed baked-surface gate now explicitly stops packaging.
- `install-local.ps1 -SkipBuild -Offline` installed the verified package into Central Fuckery,
  verified all payload hashes and the enabled launcher entry **Magenheim v0.0.105 by Local**.
  Previous installation: `backups/Local-Magenheim-20260925-090520.zip`.
  Launcher backup: `backups/mods-20260925-090533-565.yml`.
- Installed Magenheim.dll SHA-256:
  `997F771BD73B93D69E07B330F0598E19557AAB5EBA95C3D1B46FDF683F8A4B36`.
  Launcher description matches release.json. Launcher UI and game startup were not observed.
- Build evidence: ignored `artifacts/reconcile-build-2026-09-25.log`; author/export evidence:
  `artifacts/reconcile-flora-2026-09-25.log`.
