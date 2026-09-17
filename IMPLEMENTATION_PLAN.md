# Magenheim Implementation Plan

> **Current execution priority is section 14, not section 1.** Sections 1-13 describe
> programs that are already underway or complete in source. Section 14 outranks all of them
> until its gates are met. See `BACKLOG.md` P0.1.

## 1. Repository and pure-domain authority

Keep one authoritative implementation on `main`. `Magenheim.Core` owns deterministic crystal, geode-outcome, refinement, socket-model, and compatibility rules without Unity/Valheim/Jötunn dependencies. A historical design may inform future work but cannot silently reintroduce duplicate engines or stale semantics.

## 2. Deterministic validation

Run the standalone .NET test harness first. Add boundary vectors for every pure rule before runtime adapters depend upon it. Compilation and deterministic tests are separate from in-game acceptance.

## 3. Runtime bootstrap

Add a thin BepInEx/Jötunn plugin entry point and explicit dependency/network declarations. Register the permanent `magenheim.crystal_shaping` skill and localization. Load validated data/config snapshots and fail clearly on unsupported schema or invalid gameplay definitions.

## 4. Additive world generation

Translate the validated pure `SpawnArea` and approved `WorldgenAdditionPlan` entries into current Jötunn/Valheim types. Explicitly map Magenheim `All` to the runtime `Heightmap.BiomeArea.Everywhere` concept; do not rely on enum integer coincidence. Snapshot existing registrations before and after registration. Add only Magenheim-owned entries. Repeated lifecycle events must not duplicate additions.

The compatibility pass must remain configurable without becoming destructive: reject or explicitly normalize invalid areas, detect occupied registration keys and prefabs, allow per-Magenheim-key/prefab exclusions, and optionally use case-insensitive identity matching for interoperability. Negative area integers must fail closed unless the operator explicitly selects fallback-to-All behavior.

## 5. Meadows/Earth geological loop

Create the Meadows geode prefab and intact geode item. Default opening yields one guaranteed crystal plus independent 35% and 10% rolls for second and third crystals. Select elemental alignment from the configured biome weighting for each produced crystal. Build the Geologist's Workstation and Earth crystal/shard items.

## 6. Server-authoritative transactions

Implement geode opening, refinement, shard handling, XP, inventory consumption, and item grants as atomic server-owned operations. Validate proximity, station requirements, source identity, configuration fingerprint, replay/reconnect safety, and full-inventory behavior before consuming input.

## 7. Adaptive sockets

Persist Magenheim-owned per-item socket metadata. Classify weapons, armor, shields, tools, and utility equipment from observable capabilities plus config overrides. Apply elemental bonuses through Magenheim's own effect layer without replacing foreign definitions.

## 8. Runtime validation ladder

Validate plugin startup, disposable-world worldgen, repeated world loads, save/load, drop/pickup, chest storage, player transfer, death, repair/upgrade, host/client, dedicated server, mismatch rejection, and mod-removal safety. Do not promote static validation to runtime acceptance.

## 9. Expansion

Only after the Earth vertical slice is playable and admitted should biome catalogs and later magic systems expand. Recover detailed feature intent from `docs/archive/MAGENHEIM_DESIGN_SPEC_0.1.0_PRE_RECONCILIATION.md`, reconciling each feature against current authority before implementation.

## 10. The Underworld expansion track

`docs/UNDERWORLD_DESIGN.md` is the durable design authority for The Underworld. `docs/UNDERWORLD_IMPLEMENTATION_PLAN.md` is the execution-grade plan that decomposes that design into framework, world-layer, biome, creature, boss, Deepstone, progression, traversal, building, portal, persistence, multiplayer and validation phases.

The Underworld remains inside Magenheim for the foreseeable development horizon. It must extend Magenheim's existing definition validation, fingerprints, progression, multiplayer authority and persistence boundaries rather than creating a duplicate mod/repository or second implementation stack.

The first Underworld code work is not biome decoration. After higher-priority broken behavior and runtime validation gates are resolved, begin with the authority/schema skeleton, derived-world feasibility proof and cavern-illusion framework defined in Phases U0-U4 of `docs/UNDERWORLD_IMPLEMENTATION_PLAN.md`.

## 11. Underworld content production order

Once the Underworld framework is proven, production proceeds through the complete Fungal Forest vertical slice and the shared boss/Deepstone loop before multiplying biomes. The intended major progression is Fungal Forest -> Blackwater -> Sulfurous Wastes and Frozen Caverns in either order -> Fracture Zones -> Great Decay. Each biome must satisfy its full package gate, including ecology, structures, resources, unique boss, trophy and Deepstone boon, before the next dependent layer is considered complete.

## 12. Parallel Underworld architecture and Rootforged construction

`docs/UNDERWORLD_ARCHITECTURE_IMPLEMENTATION_PLAN.md` is the durable execution authority for the player/civilization construction language that runs in parallel with the world-layer Underworld program.

This track may advance through pure definitions, deterministic catalogs, asset prototypes and additive build-piece implementation whenever its dependencies are satisfied, without creating a second Underworld runtime or bypassing higher-priority Magenheim validation gates. It reuses the current architecture registration pattern and eventually contributes gameplay-significant unlock/catalog data to the canonical Magenheim/Underworld fingerprint authority.

The first bounded slice is A0: eleven structural identities comprising an Understone foundation and great plinth, 2/4/8m Worldroot beams, 2/4/8m Worldroot pillars, one iron-banded beam, and 4/8m Rootforged arch ribs. A0 is pure Core catalog/validation/test work only. Runtime registration follows after Core compile/test and resource-authority integration.

## 13. Verified Underworld foundation — 2026-09-15

U0 extension mapping and U1 initial schema/catalog integration now pass compilation and deterministic tests. See docs/validation/2026-09-15-underworld-authority-integration.md. U2 world switching, persistent Nowhere King completion, Underworld resource registration and A1 runtime construction remain gated by the documented runtime acceptance requirements.

## 14. Asset fidelity and live acceptance program — top priority from 2026-09-16

This section outranks sections 1-13. It exists because the project has accumulated a large
body of work that compiles, tests and verifies statically, while almost none of it has been
observed running, and because the asset library does not yet meet the fidelity bar the
project is aiming at.

### 14.1 Live acceptance before further inference

Every register entry since 0.0.16 reads "source repaired, live acceptance pending". Defects
have been repaired from inferred behaviour rather than observed behaviour. Some of those
inferences are wrong and nothing currently separates them from the correct ones. One
disposable-world session against a current installed build is the gate: until it runs, every
claim about repaired behaviour stays provisional and further inference compounds the risk.

The ordered matrix is in `TESTING.md`. Record the result as a validation document; do not
promote static validation to runtime acceptance in any state record.

### 14.2 Geometry gating for the model library

Inverted winding has now shipped three times from three independent sources: the copied
prism/cylinder builders across eleven visual files, the geode cavity, and the geode interior
crystals. The defect produces no compile error, no exception and no log entry. It is only
visible when a player looks at the wrong side of a surface, which is why it survives every
existing gate.

Promote the face-normal check into a permanent tool over all 281 models, following
`tools/verify-deep-fracture-caverns.py`: compare each face normal against the vector from the
part centroid, and report signed volume. Exported vertices are unwelded, so edge-pairing and
manifold tests are uninformative on this payload and must not be used as the basis of a pass.

### 14.3 Asset fidelity standard

Measured at `924f5e7`: 170 of 281 models (60%) carry no texture map; 1,320 of 3,688 parts
(36%) are textured. Flat Principled base colour is why assets read as clay. The standard to
meet is vanilla Valheim's: an albedo map on everything, with normal maps where surface relief
carries the read.

Work order within this item, highest player contact first:

1. the geode, detailed in 14.4;
2. the crystal progression items, currently the lowest-poly assets in the library
   (`earth-simple` 36 triangles, `earth-rough`/`earth-crystal`/`earth-shards` 108,
   `earth-advanced`/`earth-master` 180) despite being handled constantly;
3. the Deep Fracture passage and traversal pieces, 60 and 172 triangles, now connecting
   districts of roughly 10,000 triangles each;
4. the remaining untextured families.

### 14.4 Geode rebuild specification

`geode-sample` is one model shared by all eight biome geodes, so a single rebuild carries the
whole set. It is actively broken rather than merely dated: `stone-cavity` has 246 of 246 faces
pointing inward and every `interior-crystal-*` part is 63-73% inverted, all with negative
signed volume. The reported transparency is back-face rendering; the atlas is RGB with no
alpha and `ConfigureOpaqueMaterial` already forces opaque state.

Exterior: a fractured shell of Voronoi plates over a sphere, each plate jittered and raised
with a bevelled edge, separated by deep crevices, with a darker inner mass behind the gaps so
crevices read as depth rather than holes.

Exposed interior face: concentric agate banding inward from the rim in several bands of
varying width and value, then a dense druzy field of small crystals following the cavity
surface and pointing inward, deepening toward a darker core. Not a flat plane with spikes.

Author the interior **greyscale**. The biome-shading mechanism already exists and must be
preserved unchanged: `GeodeVisuals.Apply` tints any material whose name contains
`magenheim.geode.interior-` with the biome colour and lightens `interior-bright-`. A greyscale
authored surface is what makes that tint read as the biome rather than muddying it.

Fix the winding as part of the rebuild rather than patching the existing meshes.

### 14.5 Build discipline

Eighteen compile errors reached `main` in one batch and two more of an identical, already
repaired class followed hours later. `build.ps1` before push catches all of them. No new
tooling is required; the gate exists and is not being run.

## 15. Model quality program — staged Blender campaign, from 2026-09-17

This section plans one campaign to bring the whole 281-model library to a single quality
bar, and to do it in stages that each end at a green build and a commit, so nothing is lost
if a stage has to stop.

### 15.1 Measured starting state

Median triangles by family, against a library median of 2,348:

| family | n | median | note |
|---|---|---|---|
| df- (districts) | 20 | 10,448 | the bar the rest should approach |
| crystal-banner | 24 | 3,384 | |
| deep-fracture | 27 | 3,188 | |
| staff | 32 | 2,316 | at the median |
| deep-fracture-creature | 80 | 2,266 | |
| architecture | 7 | 2,108 | |
| furniture | 10 | 1,366 | |
| **crystal-weapon** | 10 | **1,262** | half the median, held closest to camera |
| **earth-** | 11 | **454** | worst gap; all single-part |
| sentinel ammo | 8 | 252 | small projectiles, may be intentional |
| effect- | 3 | 80 | small effects, may be intentional |

`earth-` is the largest gap and the highest player contact in the mod: it is the crystal
progression items the whole refinement loop handles, plus the Geologist's Workstation and
its three upgrades. Every one is a **single-part** mesh, which is why they are low; the rest
of the library is multi-part. They were raised from 36-180 triangles on 2026-09-16, so this
is a second pass on known-weak assets rather than a regression.

### 15.2 Open correctness defects

Three verifiers fail today, and none of them are wired into `build.ps1`:

- `verify-geode-topology` — 14 boundary edges outside the intentional mouth: literal holes
  in the geode, the asset shared by all eight biome geodes.
- `verify-geode-shell-topology` — 198 visible boundary edges outside the crystal mouth.
- `verify-held-model-grip-direction` — 5 models hold the wrong way round: the four
  Crystal-tier staves for Fire, Storm, Radiance and Venom, plus `crystal-weapon-sword`.

Correctness is repaired before quality. A hole is a defect; a low triangle count is a
shortfall.

### 15.3 Token and time strategy

The campaign is dominated by Blender invocations, and their cost is mostly avoidable noise.

1. **`--factory-startup` on every Blender call.** Two user-installed addons fail on import
   in background mode and print about 45 lines of traceback per invocation, on every run,
   for the whole campaign. The flag skips addon loading and removes all of it.
2. **One Blender process per stage, not per model.** Process startup is small, but each
   invocation costs a tool round trip. A stage authors every model it owns in one process.
3. **Redirect Blender output to a file and read only a summary.** The glTF exporter prints
   roughly six lines per mesh part; a 280-model export is thousands of lines that say
   nothing a count does not.
4. **Idempotent revision markers**, following the existing `art_revision` scene property in
   `revise-model-library.py`. A stage that stops halfway can resume instead of restarting,
   and a re-run is safe.
5. **Author and export stay separate processes**, as they already are. Authoring is the
   risky step; export is mechanical and re-runnable.
6. **Contact sheets, not individual renders.** Judging a family needs one grid image, not
   thirty. Render only where human judgement is actually required.
7. **One `model-quality-report.py`** so progress is measured by a single cheap call rather
   than ad-hoc scripting each time.

### 15.4 Stages

Each stage ends at a green `build.ps1`, a commit, and its gate wired into the build so the
defect it fixed cannot come back.

- **Stage 0 — harness.** `--factory-startup` everywhere, the quality report tool. No model
  changes. Cheap, and it pays for itself across every later stage.
- **Stage 1 — correctness.** Geode holes and the 5 wrong-way-round held models. Touches few
  models. Wire all three failing gates into `build.ps1`.
- **Stage 2 — `earth-` family.** The crystal progression items and the Workstation set:
  the worst gap and the highest contact. **Scope corrected 2026-09-17 after inspection:**
  these eleven are not simply low-detail Blender assets. They are the surviving output of
  the older `generate-earth-assets.py` pipeline -- one merged single-material mesh each,
  with a face-atlas UV -- and they carry a second asset contract that the rest of the
  library does not: `.mesh.json`, `.obj`, `.mtl`, a 512px atlas and a 128px icon, all
  enforced by `verify-earth-assets.py`. Raising them is therefore a pipeline migration to
  the multi-part authored library, honouring or retiring that second contract, not a
  modifier pass. It is the largest stage, not the quickest, and should start with a clear
  session rather than the tail of one.
- **Stage 3 — held equipment.** `crystal-weapon` geometry to the library bar, continuing
  the 0.0.55/0.0.56 weapon work.
- **Stage 4 — texture resolution.** 256px is the library floor everywhere except the
  weapons. Raising it is the largest single job and changes package size materially; it
  deserves its own decision and probably its own session.
- **Stage 5 — placement and spacing.** Snap points, base alignment and collider fit across
  the buildable families. This is largely C# (`PlacementSnapAuthority`, the per-registrar
  collider passes) rather than Blender, so it does not need to share a stage with the
  authoring work.

### 15.5 Export is content-stable but not byte-stable

Re-exporting an untouched model rewrites its UVs by about 1.2e-07, one float32 ULP, on
roughly 4% of values. The geometry, materials and part structure are identical; it is
round-trip precision, not a change. But it is enough to dirty the file and change its
catalog hash.

So a stage re-exports **only the models it actually authored**, by explicit id. Never
re-export the whole library "to be safe": it produces 281 meaningless diffs, obscures the
real change in review, and burns the token budget on noise.

### 15.6 Standing rule

Do not re-unwrap with `smart_project` during uplift. That is what produced the island
patchwork reported from play at 0.0.52 and repaired in 0.0.53; `refine-retro-textures.py`
records the mechanism. Added geometry must carry the existing UV layout, or the family
needs a purpose-authored unwrap as deliberate work rather than a side effect.
