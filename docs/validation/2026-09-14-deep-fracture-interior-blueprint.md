# Deep Fracture deterministic interior blueprint — 2026-09-14

## Scope

This record covers the pure-core projection layer between the authoritative Deep Fracture dungeon/encounter plans and the later Unity/Jötunn interior implementation. The purpose is to ensure the runtime binder receives an exact, validated build contract rather than rerolling topology or enemy populations through Valheim's generic dungeon-selection logic.

## Authority preserved

`DeepFractureInteriorBlueprintCompiler` consumes both the validated `DeepFractureDungeonPlan` and the validated `DeepFractureEncounterPlan`. It refuses either invalid input. The resulting blueprint preserves every module ID, piece-family ID, depth band, elemental state, occupation profile, resource state, encounter group, and graph edge exactly.

The compiler does not select rooms or enemies. It projects already-selected authority into runtime-placement data.

## Spatial projection

The default projection uses a bounded serpentine grid with scale-dependent widths:

- Small: 4 columns;
- Full: 6 columns;
- Grand: 8 columns.

Default major-district spacing is 128m with a nominal 96m district footprint, leaving at least 32m between neighboring district footprints. Depth bands are vertically separated by 36m per band. These are runtime-projection defaults, not replacements for the authored district identities or graph.

The entrance begins at the blueprint origin. Each module receives a finite center point and cardinal yaw. The blueprint calculates an interior radius large enough to contain every nominal district footprint plus boundary padding.

## Connection projection

Vanilla/Jötunn `DungeonGenerator` is not allowed to invent a second graph.

The graph is projected as follows:

- `MainRoute` and `Branch` connections become `PhysicalPassage` runtime edges;
- `Loop` and `Shortcut` connections become `TraversalLink` runtime edges.

This distinction is intentional. Vanilla room growth naturally expresses a tree-like physical placement but cannot safely be relied upon to realize arbitrary pre-authored cycles. Deep Fracture loop/shortcut semantics already include lifts, crystal transit, and reopened fractures, so those edges remain explicit authored traversal links that can connect already-placed districts without rerolling the physical room graph.

The validator requires every district to remain physically reachable from DF-01 through MainRoute/Branch passages alone. Loop/shortcut travel therefore improves navigation but is never required to make a district exist or to reach the Heart.

## Encounter preservation

Every projected module embeds the exact `PlannedEnemyEncounter` sequence from its corresponding encounter district. Encounter IDs are compared in order during validation. Runtime implementation therefore receives encounter authority together with the room placement instead of performing another ecology roll.

## Validation invariants

`DeepFractureInteriorBlueprintValidator` rejects scale or seed drift from dungeon authority, missing or duplicate module placements, module-order drift, piece/depth/element/occupation/resource drift, invalid or non-cardinal placement values, encounter-group drift, nominal district-footprint overlap, missing/duplicate/altered connection projections, runtime connection modes inconsistent with graph roles, districts reachable only through loop/shortcut traversal links, and an interior radius that cannot contain the projected district footprints.

## Deterministic coverage

`DeepFractureInteriorBlueprintTests` is wired into the existing deterministic executable test chain through `DefinitionAuthorityTests.Run()`. Static source coverage includes Small/Full/Grand fractures across multiple seeds, connection-mode projection, exact encounter preservation, module-footprint separation, equal-input determinism, encounter-seed mismatch rejection, invalid projection-spacing rejection, and runtime-mode tamper rejection.

A small compatibility extension is included because `Dictionary.TryAdd` is not part of bare .NET Standard 2.0. The helper preserves the intended fail-closed duplicate-ID behavior without raising the target framework.

## Validation boundary

This connector environment still does not provide the normal Magenheim .NET/Valheim compile profile. This slice is **static-source admitted only**. Compilation and deterministic-suite execution remain required in the normal development environment.

## Next exact action

Build the concrete runtime room set and interior binder against this blueprint. The binder may register Magenheim-owned `CustomRoom` templates and a custom dungeon theme, but it must consume `DeepFractureInteriorBlueprint` for actual module/connection/encounter placement rather than allowing generic `DungeonGenerator` room selection to replace the core graph. The surface location registrar must remain inactive until this exact-plan runtime path exists.
