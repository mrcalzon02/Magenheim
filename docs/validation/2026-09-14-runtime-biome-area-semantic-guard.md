# Runtime biome-area semantic guard — 2026-09-14

## Intent

Harden the Valheim/Jötunn area translation boundary without changing the validated Magenheim world-generation model or weakening additive-only compatibility.

## Reconciled baseline

The pure core already constrains `SpawnArea` to `Median`, `Edge`, or `All`, rejects negative flag values unless explicit fallback policy is configured, governs unknown bits through validated compatibility policy, and accepts `Everywhere` as a configuration alias for `All`.

The runtime adapter already avoided integer-casting the core enum into `Heightmap.BiomeArea`. It resolved `Median` and `Edge` by runtime enum name and resolved `All` through `Everything`, `Everywhere`, or a composed `Median | Edge` fallback.

## Defect repaired

The previous `All` runtime mapping trusted a defined alias named `Everything` or `Everywhere` without verifying that its value was actually equivalent to the two component areas Magenheim means by `All`. The composition fallback also rejected only an empty result; it did not reject a runtime in which `Median` and `Edge` accidentally resolved to the same underlying enum value.

That left a narrow version-drift failure mode: a future or malformed runtime enum could preserve familiar names while changing their flag semantics, silently broadening or narrowing geode placement.

`JotunnWorldgenAdapter.ParseAllBiomeArea` now:

- resolves `Median` and `Edge` first;
- requires both components to be non-empty and distinct;
- composes the runtime value and verifies that it contains both components;
- accepts `Everything` / `Everywhere` only when the alias exactly equals the composed `Median | Edge` value;
- rejects aliases containing extra bits instead of broadening placement semantics;
- returns the composed value only when no named combined alias exists.

The failure mode is deliberately fail-closed. Magenheim does not guess numeric values or reinterpret an incompatible host enum.

## Non-destructive compatibility boundary

No vanilla or foreign world-generation record is modified by this repair. `GeodeWorldgenRegistrar` continues to derive desired additions from validated definitions, observe host prefab occupancy read-only, re-run pure Add/Skip/Error planning, preflight every approved addition, refuse occupied prefab identities, and call Jötunn only for new Magenheim-owned vegetation.

The change affects only translation of already-validated area intent into the current runtime enum.

## Branch reconciliation

The repository still exposes `radiance-content`, `tmp-radiance-content`, and `__delete_me__`. Comparison against current `main` shows the historical `radiance-content` tip is the merge base and `main` is 45 commits ahead with zero commits behind. The three refs were previously observed at the same historical SHA and contain no unique work. The available connector does not expose branch-ref deletion, so physical pruning remains an explicit cleanup item rather than a claimed result.

## Verification boundary

Remote GitHub read-back verifies the semantic guard is committed on authoritative `main`. This connector execution did not run the local Valheim/.NET build environment, so compile, Jötunn API acceptance, plugin startup, and disposable-world area placement remain runtime gates.

## Next acceptance step

Build the current package in the normal Valheim development profile, run the deterministic suite, start a disposable world, and verify all configured geode definitions register with the expected `Median` / `Edge` / `All` semantics. Snapshot host registrations before and after repeated world loads to close the remaining idempotence gate.
