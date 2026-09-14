# Deep Fracture connection graph — 2026-09-14

## Scope

This record covers the third pure-core Deep Fracture implementation slice: explicit district connectivity, branches, loops, transition connector types, dormant return shortcuts, and graph validation. It still does not register Valheim locations or create Unity/Jotunn dungeon prefabs.

## Root-cause correction

The earlier module model carried a single aggregate connection-state field. That was insufficient for the design because return travel, branching, shafts, bridges, lifts, reopened fractures, and unlockable shortcuts are relationships between two districts, not properties of one district. Connection state now lives on explicit graph edges.

`DungeonConnection` records source and destination module identities, route role, physical connector kind, traversal state, directionality, and whether the edge is required for Heart reachability. Module state remains responsible for the module itself: piece identity, depth band, elemental influences, structural condition, occupation, and resources.

## Connection generation

The default graph policy keeps roughly seventy percent of major modules on a legible primary descent and places the remainder on reachable branch chains. Optional branch reconnections create loops. Scale-dependent dormant shortcuts are then added as non-required return routes: one for Small fractures, two for Full fractures, and three for Grand fractures.

Physical connector vocabulary currently includes fault corridors, short passages, vertical shafts, bridges, ancient lifts, crystal transit, and reopened fractures. Cross-depth connections prefer shafts, lifts, or short transitions. Shortcut edges use ancient lifts, crystal transit, or reopened fractures and are always bidirectional once opened.

A separate deterministic random stream is used for graph construction so connection-policy evolution does not consume the district-selection random sequence. The xorshift implementation is now one shared pure-core utility rather than duplicated random logic.

## Validation invariants

The plan validator now requires:

- unique namespaced connection identities;
- valid distinct module endpoints;
- no duplicate module-pair edges;
- forward-progressing main-route edges;
- only main-route edges may be required for Heart reachability;
- required edges must begin traversable;
- shortcuts must be optional, bidirectional, and Dormant or Opened;
- every module must be reachable from DF-01 through initially traversable edges;
- DF-20 must be reachable from DF-01 through the required main-route subgraph alone.

The connection planner rejects a second attachment pass over a plan that already has connections, preserving the project's single-authority/no-stacked-mutator rule.

## Changed source

- `src/Magenheim.Core/DeepFractures/DeepFractureModel.cs`
- `src/Magenheim.Core/DeepFractures/DeterministicSequence.cs`
- `src/Magenheim.Core/DeepFractures/DeepFractureLayoutPlanner.cs`
- `src/Magenheim.Core/DeepFractures/DeepFractureConnectionPlanner.cs`
- `src/Magenheim.Core/DeepFractures/DeepFractureCatalog.cs`
- `tests/Magenheim.Core.Tests/DeepFractureCatalogTests.cs`

## Static validation

The deterministic test source now additionally checks scale-appropriate shortcut counts, shortcut state/directionality, presence of branch exploration, equal-seed graph reproducibility, required-route severing detection, and rejection of stacked connection planning.

The connector environment still has no `dotnet`, `csc`, `mcs`, or `msbuild`, so compilation and deterministic-suite execution are not claimed.

## Admission

**Static source admission only.** Graph architecture and validation are source-complete for this slice. Compile/runtime/worldgen/multiplayer/persistence acceptance remains open.

## Next exact action

Implement the pure-core occupation and encounter planner that maps each district's occupation profile, elemental influences, depth band, and piece identity into enemy chassis, alignment packages, crystal components, and encounter roles. After that, bind the already-validated district and connection graph to additive runtime location generation.
