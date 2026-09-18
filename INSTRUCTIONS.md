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

## The Underworld is one world, one save — non-negotiable

The Underworld is a **region of the same Valheim world and the same save file** as the surface,
hosted in a reserved coordinate band exactly the way Valheim's own instanced dungeon interiors are.
Both layers are resident in the same running session at the same time. Travel between them is a
teleport within the world.

- Never introduce a second save, a derived save name, a world-pair manifest, a "physical world
  switch", or a second server process as the mechanism for layer separation.
- Never call `ZNet.LoadWorld`, `WorldGenerator.Initialize` or `FejdStartup` paths to change worlds.
- Never produce a design in which the player must leave the session to reach the Underworld.
- Valheim's `ZNet`, `ZoneSystem`, `WorldGenerator` and `ZDOMan` are per-world singletons. Two
  simultaneously loaded worlds are impossible in one process; do not attempt or plan around it.
- `UnderworldSpatialDomain` in `Magenheim.Core` is the authority for the logical-to-host coordinate
  mapping. Terrain inside the band comes from Valheim's own generator via the
  `WorldGenerator.GetBiomeHeight`/`GetBiome` postfixes, reshaped to Underworld biomes from a seed
  deterministically derived from the surface seed.

This was got wrong once: `docs/UNDERWORLD_DESIGN.md` §6 previously specified a separate persistent
world instance, an implementation was built against it, and the result could only be entered by
quitting to the main menu and loading a different save. §6 now records the corrected architecture.
If any document, handoff or backlog item disagrees with this section, this section wins and the
other document is the defect.

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
