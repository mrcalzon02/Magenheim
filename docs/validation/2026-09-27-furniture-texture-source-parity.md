# Furniture texture source parity — 0.0.140

Reconciled remote commit `cceb02c8` onto the clean local `main`. The active Central
Fuckery profile initially contained 0.0.138, while remote source had advanced to
0.0.139. The remote per-object texture files were present in runtime payloads, but
the exporter only substituted filenames: saved Blender materials and GLB exports
still used their older images.

The exporter now binds overrides to individual source objects before evaluating
and exporting them. Shared materials are copied where needed so one chair leg
cannot overwrite another's map. The original semantic material name is retained
for runtime classification. Packed grayscale artwork is multiplied by the original
material tint in Blender, matching the runtime texture/tint contract.

Re-exported the Crystal Bench, Geode Table, Geode Chair and Geode Pedestal (37
parts total), regenerated all four Hammer icons, and refreshed both catalogs.
The existing per-part texture pixels remain unchanged. Runtime geometry and
material properties are preserved; re-export introduces only floating-point UV
rounding (approximately 1e-7 for the chair).

`verify-object-texture-bindings.py` is now a build gate. It checks every override's
packed source bytes against its PNG, the texture/tint shader connections, original
semantic identity, runtime filename, and embedded GLB texture binding. All 37
bindings pass. The four refreshed icons were visually inspected for material
separation and complete framing.

Full build admission exposed an inherited contradictory Geode Table requirement:
the coverage gate demanded both the old shared material sheets and the nine newer
per-object maps. Removed the obsolete shared-sheet entry; all nine explicit part
checks remain required. The corrected authored-surface coverage gate passes.

Installation retains the remote clean-replacement behavior with an explicit
resolved-path and reparse-point guard before removing the managed payload.

No live game, world, multiplayer or persistence acceptance is claimed by this pass.

The corrected offline closeout build passed: runtime compilation, 94,391 core
assertions plus module suites, all 366 model triplets, production importer checks,
texture/geometry/icon gates, the new 37-part source parity gate, and native patch
binding checks. Offline mode skips the online dependency vulnerability audit.
Existing small-icon and importer nullable-reference warnings remain separate
from the furniture texture acceptance.

Closeout backed up the prior profile and copied/verified the 0.0.140 payload.
r2modman opened during the build, so the installer refused the final launcher
catalog write. At this checkpoint the installed payload is 0.0.140, but catalog
reconciliation awaits closing r2modman and rerunning `install-local.ps1 -SkipBuild`.
