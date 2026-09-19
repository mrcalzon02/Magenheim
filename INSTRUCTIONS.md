# Magenheim Project Instructions

## Authority

Resolve project truth in this order:

1. the user's current instruction;
2. this file;
3. verified committed source and current state records on `main`;
4. `BACKLOG.md`, `IMPLEMENTATION_PLAN.md`, and validation records;
5. archived design material and historical branches.

Live verified repository state outranks stale conversation, scheduled prompts, old README claims, and superseded branch content.

## Repository execution

- `main` is the single authoritative development branch.
- Do not create side branches, pull requests, replacement repositories, force-pushes, or history rewrites for normal development.
- Reconcile concurrent work instead of overwriting it.
- Do not add GitHub Actions unless explicitly authorized.
- Do not commit build outputs, packaged dependencies, BepInEx/Jötunn runtime copies, process files, or runtime logs as source.
- Preserve material historical work through Git history or an explicit archive when it is useful for future implementation.

## Stable design identities

- Skill ID: `magenheim.crystal_shaping`.
- Crystal tiers: Rough -> Simple -> Crystal -> Advanced -> Master.
- Ordinary refinement preserves elemental alignment.
- Normal elemental identities are Earth, Fire, Frost, Storm, Venom, Radiance, Seidr, and Spirit.
- Fate and boss resonance are later special systems, not ordinary geode elements.

## Underworld objective statement — do not reinterpret

**The Underworld is a separate Magenheim-owned instanced world-space. It is not a distant surface continent and must not be implemented as an ordinary Valheim far-landmass hidden behind coordinate projection.**

The Surface and Underworld remain part of the same Magenheim progression experience: players retain their character, inventory, skills and progression when crossing the Deep Gate. The Underworld, however, owns an isolated instance lifecycle, terrain/chunk authority, biome/environment state, exploration state, map pins and map presentation. Surface terrain generation must not be extended outward to manufacture the Underworld.

## Underworld instance architecture — non-negotiable

- Treat the Underworld as a dedicated **instance world-space**, analogous in separation semantics to a Valheim dungeon interior but world-scale and Magenheim-generated.
- Do **not** use the x=40000 horizontal band, Ashlands/Deep-North-style far-landmass hosting, or WorldGenerator.GetBiomeHeight/GetBiome patches against distant Surface columns as the final Underworld terrain mechanism.
- Do **not** expose, project, or disguise distant Surface-world coordinates as Underworld geography.
- The instance owns its own logical origin and chunk/terrain coordinate system. Player-facing Underworld coordinates are native instance coordinates, not translated Surface coordinates.
- Deep Gate travel is an instance transition. It must preserve the player's character state and provide a reversible return path without requiring ordinary traversal through Surface geography.
- Persistence must be explicit and independently keyed for the Underworld instance. Never silently alias Surface exploration, map, terrain-generation or environment state.
- Multiplayer authority must admit peers into the same authoritative Underworld instance identity and reject mismatched instance/configuration state.
- Existing deterministic Underworld biome, terrain-shape, exploration, progression, ecology and transition rules are reusable. The obsolete **40 km host-band adapter is not**.
- Do not revive the earlier implementation that required the player to quit to the main menu and manually load another save. "Separate instance" means an isolated gameplay world-space, not a user-visible second-save workflow.
- If Valheim's singleton world machinery cannot directly host this abstraction, Magenheim must own the instance/chunk adapter rather than redefining the Underworld as a remote continent merely to fit the singleton.

The 2026-09-17 same-world horizontal-region correction is superseded by the user's 2026-09-18 architecture correction. Any backlog, validation record, handoff or source comment that treats the 40 km horizontal region as the intended final architecture is historical evidence, not authority.

## Architecture

- `Magenheim.Core` owns deterministic rules and must remain independent of Unity, Valheim, BepInEx, and Jötunn.
- Runtime adapters consume the pure core; they must not recreate a second refinement, geode, socket, or worldgen authority.
- Balance and content should be data-driven where practical, with strict validation before gameplay mutation.
- Server-authoritative transactions must exist before persistent multiplayer inventory or socket mutations are enabled.
- Per-item state must not be implemented by mutating shared item/prefab definitions.

## Compatibility

- World generation is additive and non-destructive.
- Magenheim registrations use stable `magenheim.` namespaced keys.
- Vanilla and foreign registrations may be observed for collision detection but are not rewritten, deleted, disabled, or reordered for compatibility.
- Unknown foreign equipment remains untouched when safe classification is impossible.

## Validation and claims

### Required testing closeout and live delivery

- The user authorizes installing each testing closeout into the active Central Fuckery
  r2modman profile. Delivering only a ZIP is incomplete unless explicitly requested.
- Use `closeout.ps1` (or `closeout.ps1 -Offline` with cached dependencies) to test,
  build, package, back up, install and verify the active profile.
- Maintain `release.json` with the exact plugin version and a short description of
  that version's changes. Package manifest, assembly version, installed DLLs and
  r2modman's `mods.yml` version/description/dependencies must agree.
- Closeout must verify the launcher catalog as well as all installed file hashes.
  Preserve other mods, profile settings and enablement; back up the prior catalog.
- If Valheim or r2modman is running, prepare the package, ask the user to close it,
  and finish installation afterward. Do not terminate the user's game or launcher.
- Report the installed version and launcher description. Distinguish verified catalog
  data from a visually observed launcher and from actual game startup or world tests.

Keep source review, compilation, deterministic tests, plugin startup, disposable-world generation, multiplayer validation, and persistence validation distinct. Never claim a stronger state than was directly observed. Repair root causes in the authoritative source rather than stacking bypasses, mutators, or duplicate implementations.
