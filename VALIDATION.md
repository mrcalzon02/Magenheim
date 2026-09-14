# Validation

## Repository reconciliation

Observed on 2026-09-13: the GitHub repository contained one branch (`master`) and one file (`README.md`). The README described implementation artifacts that were absent. This pass restores repository truth by adding the missing authority documents and a small, inspectable compatibility core. No prior build/test claim is carried forward as evidence.

## Pure-core acceptance checks

The worldgen compatibility slice is accepted only after these behaviors execute successfully:

- `None` biome area is rejected under conservative policy.
- unknown area bits are rejected under conservative policy.
- `ClampKnownBits` preserves only recognized bits and rejects a result of `None`.
- `FallbackToAll` broadens placement only when explicitly configured.
- valid Magenheim namespaced keys produce `Add` decisions.
- an occupied key produces `Skip` by default and leaves the observed registration unchanged.
- unowned/non-namespaced desired keys error.
- duplicate Magenheim desired keys cannot produce two additions.
- `AdditiveOnly = false` is rejected rather than enabling a destructive mode.

The source includes a console test harness for these checks. It is not marked verified until it has actually executed in an environment with .NET 8.

## Adapter acceptance checks

Before any Jötunn adapter is considered complete:

- compile against the exact installed Jötunn/Valheim references;
- confirm the current `BiomeArea` mapping by type/value rather than guessed integers;
- snapshot ZoneManager state before registration;
- execute approved Magenheim additions;
- snapshot afterward and prove no pre-existing registration changed;
- invoke the relevant world-load lifecycle more than once and prove Magenheim did not duplicate its additions;
- validate a disposable world with Median-only, Edge-only, and All placement definitions;
- intentionally inject an invalid/unknown area value and verify it fails closed with a diagnostic;
- intentionally occupy a Magenheim key before planning and verify the foreign entry survives unchanged.

## Runtime boundary

No Valheim runtime, Jötunn registration, generated world, multiplayer authority, or persistence behavior has been observed by this repository pass. Those remain open acceptance items.
