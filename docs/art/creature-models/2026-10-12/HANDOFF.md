# Boss model handoff — 12 October 2026

The user stopped local modeling and requested that everything be committed and
published so the online equivalent can continue. Do not run more local modeling
as part of this handoff.

## Acceptance tally

The rolling tally is in `INSTRUCTIONS.md`: **1 of 6 bosses adequately reworked in
this conversation**. First Bloom r22 was accepted sufficiently for the user to
authorize moving to another boss. Native admission, animation and performance
remain separate acceptance gates.

Blackwater Maw was not accepted. The reviewed r13 study, source recipes and
unaccepted r14 drafts are preserved. The last requested fin shortening was 20%,
followed by an explicit stop. The r14 draft used larger changes; **do not claim
the 20% correction was completed**. Later online commits may contain a separate
production Blackwater Maw author and runtime binding; inspect current `main`
rather than replacing that work with the older study.
During this handoff, remote `main` through `f49db4f6` was merged. Its later commits
withdrew the premature Maw author/forge hook and restored the paused boss state.
Preserve those withdrawals; do not resurrect the withdrawn runtime override.

## Current new candidate: Rift Titan primary r3

- Editable source: `assets/models/studies/underworld-boss-rift-titan-primary-r3.blend`.
- Review assets: this directory's `rift-titan-primary-r3/` subdirectory.
- Portable skinned export and six inspection renders are present, along with
  `concept-pass.json`, `refinement-metrics.json` and `verification.json`.
- Four packed 512px maps: `assets/textures/underworld/bosses/rift-titan-primary-r3/`.
- 1,480 triangles; native Stone Golem reference 1,200; soft 1.5x cap 1,800.
- One mesh/material, retained Rift Colossus rig and seven named actions.
- Offline geometry, weights, atlas and GLB checks passed. The review samples
  inherited attack bounds; it does not prove full animation or runtime acceptance.

This is an **unaccepted primary-form study**, not a finished concept model.
The clay review exposed short, block-like limb plates and overly thick shoulder
layers. The announced next change was to elongate/narrow the limb strata and
thin/layer the shoulders, but the user interrupted before that change was made.
Do not mark it done. The adequate tally remains 1 of 6.

## Starting points for the online session

Read `AGENTS.md`, `INSTRUCTIONS.md` and
`docs/art/creature-models/REFINEMENT-STANDARDS.md` first. Match primary silhouette
and negative spaces against `docs/art/creature-concepts/2026-10-03/the-rift-titan.png`
before claiming detail progress. Existing r2 comparison assets show the rejected
repeated-block approach. Preserve accepted First Bloom and stopped Blackwater
studies; do not infer acceptance from file validity or triangle counts.

Relevant new recipes:

- `tools/rift_titan_primary_refinement.py`: the primary plate/cage author.
- `tools/refine-rift-titan-primary.py`: compact atlas and skinned study export.
- `tools/review-rift-titan-primary.py`: saved geometry/export checks and renders.
- `tools/inspect-rift-titan-rig.py`: rig inspection helper.

Use `tools/blender.ps1` for Blender execution. No runtime release, installer or
gameplay changes were made by this local primary study. Ignored build outputs,
runtime copies, caches and temporary logs are not source and are not included
in the handoff commit.
