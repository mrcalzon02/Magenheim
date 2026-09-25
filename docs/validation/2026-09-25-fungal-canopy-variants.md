# Fungal Forest canopy variety — 0.0.106

The four existing fungal trees are species archetypes. Each now has three additional growth
forms, producing sixteen distinct canopy models. These are authored Blender meshes with GLB
and runtime exports, not just new scale/tint settings on the original four.

| Family | Additional growth forms | Geometry changes |
| --- | --- | --- |
| Glowcap | Bent, twin, elder | Curved supporting tissue, two unequal crowns, broad old-growth umbrella |
| Spirestalk | Young, crooked, towered | Two/three/five cap tiers, independent trunk curvature and tier spacing |
| Puffcap | Forked, spreading, clustered | Two/four/five branching fruiting bodies and different crown widths |
| Tanglecap | Young, splayed, woven | Two/five/six strands, different twist, spread and cap counts |

## Authoring and runtime

`tools/author-underworld-fungal-forest.py` owns the growth forms. Its existing rebuild command
authors and exports all 28 fungal flora/resource models and updates the catalog. Freshness is
recorded only by `verify-generated-freshness.py --update underworld-fungal-forest-models`.
Each canopy model has a 1024px baked atlas and uses the established species materials.

The existing donor catalog gains twelve entries after its original eleven slots, preserving
landmark index identities. Seeded ecology selects larger forms in landmark positions, mixed
branching forms in the middle canopy, and younger/spreading forms at the edges. Palette size
comes from the catalog rather than the old hard-coded eleven. All sixteen models participate.
This changes the selected form at some existing seeded positions; positions remain cell-owned.

The native model importer and shared mesh cache remain the rendering path. Stems collide;
crowns, gills and ground cover do not. New branching glowcaps and all puffcaps use fused tissue.
No per-tree point lights or new runtime mesh generation are introduced.

## Acceptance

- `verify-fungal-canopy.py` checks four forms per family, distinct geometry after removing
  scale/translation, tree dimensions, grounded feet, stem-only collision, no point lights,
  and a 20,000-triangle ceiling per model.
- The Blender junction gate checks all seven fused models for one connected stalk and valid
  normals. Existing UV, winding, surface and model-export gates still apply to every asset.
- The model importer harness verifies all sixteen identities are reachable and packaged.
- Four fresh family sheets show front, side, three-quarter and silhouette views under
  `artifacts/review/fungal/fungal-canopy-review-01.png` through `-04.png`.

Live biome appearance, collision, frame time, persistence and multiplayer require the
0.0.106 procedure in TESTING.md. Offline checks do not establish in-game acceptance.

## Execution and installation

- Full `build.ps1 -Offline` passed. Runtime compiled with zero warnings/errors; 47,068 Core
  assertions plus separate suites passed. All 356 model payloads imported twice and all sixteen
  fungal canopy identities resolved through the catalog. All seven fused stems passed the
  Blender connectedness check. The final models range from 3.88m to 13.76m before placement scale.
- Reviewed all four final family sheets and `artifacts/review/fungal/fungal-canopy-overview.png`.
  The sheets normalize specimens for comparison; they do not depict relative world heights.
- The new twelve models add 88,988 triangles across the complete asset library, not per grove.
  Per-model triangle budgets and shared mesh reuse passed; in-game frame time remains unmeasured.
- 47 Harmony targets, 36 direct reflection bindings, 42 helper contracts and 23 assembly
  references passed. Existing limitations remain: six test-harness nullable warnings, twelve
  dynamic reflection bindings, six legacy Earth icons below target, deferred Capcrawler
  source/render gates, and no online vulnerability audit in offline mode.
- `install-local.ps1 -SkipBuild -Offline` installed and verified **Magenheim v0.0.106 by Local**,
  enabled in Central Fuckery. The description starts "0.0.106: Fungal Forest expands to sixteen
  authored canopy forms" and matches release.json. All installed file hashes passed.
- Prior installation backup: `backups/Local-Magenheim-20260925-094848.zip`.
  Launcher catalog backup: `backups/mods-20260925-094859-302.yml`.
  Installed DLL SHA-256: `786BF2DBC812CE038668FAED5109B2C8B1FE56BD4814B66C55C9F723C6AAA7FF`.
- Evidence: ignored `artifacts/fungal-variants-generation.log`, `fungal-variants-build.log`,
  `fungal-variants-review.log` and `fungal-variants-overview.log`. Launcher UI and game startup
  were not observed. Remote main matched HEAD when fetched before closeout.
