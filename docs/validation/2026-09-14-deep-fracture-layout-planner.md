# Deep Fracture deterministic layout planner — 2026-09-14

## Scope

This record covers the second pure-core Deep Fracture implementation slice. It remains below the Unity/Jotunn runtime boundary and does not claim that a Valheim dungeon, location, enemy prefab, connector, shortcut, save state, or world-generation placement exists yet.

## Root-cause correction

The initial module-state model held a single nullable elemental alignment. That was too narrow for the authoritative design because mixed elemental domains are explicitly possible and the Confluence Heart is defined by several elemental systems meeting in one end-region. `DungeonModuleState` now carries a distinct collection of elemental influences. DF-01 is required to begin without a predeclared domain, while DF-20 requires at least two elemental influences.

## Deterministic generation implemented

`DeepFractureLayoutPlanner` now produces a validated dungeon plan from scale, integer seed, and a validated generation policy.

The default policy keeps the design's seven dungeon environmental languages as Earth, Fire, Frost, Storm, Venom, Radiance, and Spirit. Seidr remains part of Magenheim's broader elemental authority but is not silently inserted into Deep Fracture domain generation because it was not one of the seven environmental languages in the supplied dungeon design.

The initial generation policy is deliberately tunable rather than treated as immutable design canon. Its current defaults allocate the districts between entrance and Heart at 20% Upper Fracture, 30% Middle Works, and 50% Deep Domains. Upper and Middle elemental influence is probabilistic, Deep Domain districts always receive an elemental influence, mixed Deep Domain influence is uncommon, and the Confluence Heart receives three distinct influences by default.

Generation uses a small project-owned xorshift sequence rather than framework `System.Random`, so equal seed/policy input does not depend on runtime-specific random implementation behavior. Piece selection is stable-ordered, respects each piece's allowed depth bands and reuse ceiling, avoids immediate identical-piece repetition when alternatives remain, and fails rather than exceeding the catalog's reuse contract.

The generated major-module sequence always begins with DF-01 The Fracture Descent, progresses monotonically through Upper Fracture, Middle Works, and Deep Domains, and ends with DF-20 The Confluence Heart. Module count is selected within the scale contract: Small 12-18, Full 24-36, Grand 40-50.

This slice also assigns deterministic first-pass structural condition, occupation profile, resource state, and elemental influence. Connection topology remains deliberately open in this slice; actual branching, shortcut, connector, vertical-shaft, and return-travel graph generation is the next structural dependency rather than being faked with arbitrary blocked-path flags.

## Changed source

- `src/Magenheim.Core/DeepFractures/DeepFractureModel.cs`
- `src/Magenheim.Core/DeepFractures/DeepFractureCatalog.cs`
- `src/Magenheim.Core/DeepFractures/DeepFractureLayoutPlanner.cs`
- `tests/Magenheim.Core.Tests/DeepFractureCatalogTests.cs`

## Static validation

The deterministic test source now covers canonical catalog counts, scale ranges, valid/invalid depth placement, piece reuse rejection, componentized enemy encounter validation, default exclusion of Seidr from dungeon domains, generated plans across Small/Full/Grand scales and multiple seeds, entrance/Heart placement, Deep Domain elemental assignment, multi-element Heart generation, equal-seed reproducibility, and invalid generation-policy rejection.

The current connector execution environment still exposes no `dotnet`, `csc`, `mcs`, or `msbuild` executable. Compilation and test execution therefore remain deferred and are not claimed.

## Admission

**Static source admission only.** The generated plan contract is source-complete for this slice, but compile admission, deterministic-suite admission, runtime dungeon generation, multiplayer, persistence, performance, and fresh-world acceptance remain open.

## Next exact action

Implement the deterministic connection graph over the generated district sequence: main-route continuity, branch sockets, vertical transitions, shortcut unlocks, and return-travel guarantees. Keep that graph pure-core and validated before any Jotunn dungeon/location registration is added.
