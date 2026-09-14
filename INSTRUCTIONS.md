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

Keep source review, compilation, deterministic tests, plugin startup, disposable-world generation, multiplayer validation, and persistence validation distinct. Never claim a stronger state than was directly observed. Repair root causes in the authoritative source rather than stacking bypasses, mutators, or duplicate implementations.
