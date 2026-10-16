# Refined boss implementation — 0.0.177

The reviewed boss models were previously study files. Actual encounter prefabs
cloned their ordinary apex species and retained those older visuals. This
candidate connects three dedicated weighted models to those existing encounters:

| Encounter prefab | Reviewed source | Production model | Triangles / bones / actions |
| --- | --- | --- | --- |
| `Magenheim_Underworld_Encounter_FirstBloom` | First Bloom arm seams r22 | `underworld-boss-first-bloom-refined` | 8,846 / 22 / 10 |
| `Magenheim_Underworld_Encounter_RiftTitan` | Rift Titan broken strata r18 | `underworld-boss-rift-titan-refined` | 1,800 / 8 / 7 |
| `Magenheim_Underworld_Encounter_CarrionCrown` | Carrion Crown anatomy r3 | `underworld-boss-carrion-crown-refined` | 2,046 / 7 / 5 |

The ordinary Crowncap Brute, Rift Colossus and Corpse Orchard retain their
existing production models. Blackwater Maw remains paused. The three other boss
visual routes are unchanged. The art campaign tally remains **1/6**: deploying
Rift Titan and Carrion Crown for testing does not establish art acceptance.

## Runtime behavior

Each dedicated boss uses one `SkinnedMeshRenderer`, one native Rock_4-derived
material and four owned 512px atlas maps. Skinning retains at most two normalized
bone influences per vertex, including blended attachment roots. Bone transforms
are exported as complete parent-local poses, avoiding rigid-piece detachment or
incorrect conversion of bone-local animation channels. The existing creature
presentation driver selects the retained actions from native character state and
rebinds spawned clones to their own skeletons. Native AI, Animator/attack sockets,
colliders, damage, loot, networking and persistent encounter identities remain
on the existing chassis. Inherited ordinary visual hierarchies are removed only
from the three dedicated boss clones.

The production coordinate conversion maps the studies' front +Y to Unity +Z and
puts the authored rest sole plane at zero. It cancels the ordinary/encounter
visual scale so the authored final model heights are not multiplied twice.
Animated culling bounds enclose all exported action frames. The editable study
files are unchanged, verified by their original SHA-256 values. Production-only
triangulation preserves Blender's exact tessellation; backwards inherited smooth
cap normals are corrected without changing face geometry or winding.

`tools/export-refined-boss-runtime.py` owns these three `*-refined` production
identities. The ordinary rigid-creature exporter is unchanged. The generated
manifest records the new authority through an actual regeneration, with the
reviewed studies as inputs. Production JSON carries the immutable study path and
hash; the catalog and validator support that source location explicitly.

## Completed offline verification

- Runtime 0.0.177 compiles against the installed Valheim/BepInEx assemblies with
  zero warnings and zero errors.
- Core: 128,917 assertions, plus separately reported module suites.
- All 534 model sets pass geometry, winding, UV, normals, texture and catalog
  validation. All 534 import twice through the actual `ModelAssets` code in the
  regression harness. Existing test-shim nullable warnings remain separate from
  the clean native runtime compilation.
- Dedicated skin regressions check shared geometry, instance-specific bones,
  blended joints, native shader selection, animated bounds, rejection of wrong
  bone order and rejection of accidental static import.
- Every exported action frame has finite complete transforms. Blender posed
  geometry is compared at five points per action: 243,600 First Bloom vertices,
  38,640 Rift Titan vertices and 30,475 Carrion Crown vertices. Maximum error is
  respectively 0.00000596, 0.00000333 and 0.00000255 metres.
- Actual production GLB/JSON triangle positions, skin joint names/weights,
  grounding, atlas content hashes and unchanged source hashes pass independent
  verification. See [production verification](2026-10-16-refined-boss-runtime/production-verification.json)
  and [export measurements](2026-10-16-refined-boss-runtime/export-summary.json).
- Installed-assembly compatibility: all 24 assembly references resolve; 100
  explicit Harmony targets, 23 dynamic groups / 677 targets, 92 literal reflection
  bindings and 46 helper field contracts pass. The existing 38 dynamic reflection
  sites retain their own live acceptance boundary.
- The new refined-boss freshness authority passes for all ten owned outputs.

The full historical asset freshness gate still reports 13 baseline families.
Their implicated inputs are Git-unchanged from the pre-task HEAD; working-copy
differences from Git blob bytes are CRLF/LF. No unrelated generator outputs or
recorded hashes were refreshed to conceal those failures. This is a verified
local runtime test package, **not a claim that the full historical build gate
passed**. See [baseline evidence](2026-10-16-refined-boss-runtime/baseline-freshness.json).

## Installation and live acceptance

The managed installer completed in Central Fuckery with every package file and
the launcher catalog verified at **0.0.177**. All three installed boss payloads,
twelve atlas maps and both DLLs independently match the tested source/package.
The runtime DLL SHA-256 is
`5334A0B0F48FF160E2076A55F787444CC36F334C7D91AD3149558617B54B0C87`.
The previous installation was backed up to
`backups/Local-Magenheim-20261016-153120.zip`; launcher metadata was backed up to
`backups/mods-20261016-153154-196.yml`.
See [installed-file proof](2026-10-16-refined-boss-runtime/installation.json).

Use the 0.0.177 steps in [TESTING.md](../../TESTING.md), including the exact boss
encounter names. These checks do not establish live appearance, frame rate,
attack alignment, collision fit, death presentation or save/reload acceptance.
No live game inspection is claimed by this document.
