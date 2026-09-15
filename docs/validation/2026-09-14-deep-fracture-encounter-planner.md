# Deep Fracture enemy occupation and encounter planner — 2026-09-14

## Scope

This record covers the pure-core encounter-authority slice that follows the Deep Fracture district and connection graph. It maps validated dungeon modules into deterministic enemy populations without registering Valheim creatures, spawning runtime prefabs, or mutating Jötunn/vanilla spawn systems.

## Authority implemented

`DeepFractureEncounterPlanner` consumes the already-validated `DeepFractureDungeonPlan`. It does not create a parallel dungeon authority and refuses to plan encounters for an invalid district/connection graph.

Each module is planned independently from the dungeon seed plus a stable hash of the module instance ID. This gives deterministic population generation while preventing an occupation change in one district from reshuffling unrelated districts.

Occupation profiles own the required primary ecology:

- `WispRoaming` -> Annoyance Wisp;
- `CrawlerColony` -> Geode Crawler;
- `ShardlingFeeding` -> Shardling;
- `RevenantHold` -> Crystal Revenant;
- `SentryDefense` -> Facet Sentry;
- `GuardianObjective` -> Stone Guardian;
- `GolemLair` -> Crystal Golem;
- `Mixed` -> multiple distinct chassis selected from the district depth pool;
- `Empty` -> no encounter groups.

Piece family supplies a separate territorial affinity, so the environment matters in addition to the abstract occupation profile. Examples include Burrowers in the Buried River, Sentries in conductor/prism districts, Parasites in contaminated/dissolution districts, Revenants in echoing/ossuary districts, and Guardians in deep stone domains. The default affinity chance is 100 percent for occupied districts when the affinity is distinct from the required primary chassis, bounded by the three-group district cap.

The Confluence Heart is a fixed high-tier exception rather than a random ordinary occupation. It requires three distinct groups: a Deep Colossus with the `HeartGuardian` role, an Obelisk Warden, and a Crystal Golem. Their alignments are drawn from distinct Heart elemental influences when available.

## Elemental binding

Encounter alignment is never invented outside district authority. A district with no elemental state produces unaligned enemies and no elemental-attack component. A district with elemental influences binds each planned group to one of those influences and gives the group exactly one elemental-attack crystal component.

The default Deep Fracture layout still uses Earth, Fire, Frost, Storm, Venom, Radiance, and Spirit as dungeon-domain elements. Encounter planning consumes whatever validated elemental states are actually present on the module rather than hard-coding another element table.

## Crystal component anatomy

Chassis define their own component language. Current pure-core packages include movement cores for Wisps/Shardlings/Hounds, carapace or armor cores for Crawlers/Sentinels/Guardians/Golems, regeneration cores for Parasites and Golems, environmental-control cores for Sentries/Wardens/Colossi, resistance cores on heavier constructs, and elemental foci when the district provides an alignment.

Crystal Golems are required to expose Armor, Regeneration, and Resistance component functions. Obelisk Wardens and Deep Colossi expose a `DeathAnchor` component marked `RequiredForPermanentKill`, giving later runtime implementation an explicit component target rather than hiding permanent-kill behavior in prefab-specific code.

All component IDs are Magenheim-owned under `magenheim.fracture.component.*`. All planned encounter IDs are namespaced beneath their module instance IDs.

## Population scaling

Spawn counts are chassis-specific and bounded by encounter policy. Small nuisance/skirmisher groups can grow with Deep Domain depth, rich/greater-geode resources, and Grand expedition scale. Guardians, Golems, Wardens, and Colossi remain deliberately small-count chassis rather than receiving the generic swarm bonuses.

The default policy admits at most three encounter groups per district and eight units per group. The minimum group cap is three because every valid Deep Fracture contains DF-20 and its Colossus/Warden/Golem composition cannot be represented with fewer groups.

## Validation invariants

`DeepFractureEncounterValidator` rejects:

- encounter plans built against an invalid dungeon graph;
- seed mismatch;
- missing or duplicate district entries;
- occupation or piece-family drift from module authority;
- encounters outside their territory module/piece;
- duplicate group IDs or duplicate chassis groups within a district;
- out-of-policy spawn counts;
- enemies aligned to an element absent from their district;
- elemental components in unaligned districts or missing elemental components in aligned districts;
- non-Magenheim component IDs;
- Crystal Golems missing Armor/Regeneration/Resistance anatomy;
- Wardens or Colossi missing their required death anchor;
- occupation profiles missing their required primary chassis;
- Mixed districts with fewer than two chassis;
- Confluence Hearts missing the Deep Colossus HeartGuardian, Obelisk Warden, Crystal Golem, or multi-element representation.

## Deterministic coverage

`DeepFractureEncounterPlannerTests` is wired into the existing deterministic executable test chain through `DefinitionAuthorityTests.Run()`. Source coverage includes Small/Full/Grand layouts over multiple seeds, empty versus occupied districts, alignment admission, Confluence Heart composition, component anatomy, all occupation-profile primary mappings, equal-input determinism, module-local random-stream isolation, and rejection of an encounter alignment not present in district authority.

## Validation boundary

This connector execution environment still does not provide the normal C#/.NET/Valheim build profile used by Magenheim. This slice is therefore **static-source admitted only**. Compilation, deterministic-suite execution, Valheim creature registration, spawning, AI behavior, component-damage runtime behavior, multiplayer authority, persistence, and performance remain unadmitted.

## Next exact action

Build the additive Jötunn/Valheim Deep Fracture location boundary on top of the existing pure-core district, connection, and encounter plans. The runtime layer should translate validated Magenheim-owned plans into locations/prefabs without rewriting vanilla or foreign worldgen registrations, and should not reimplement layout or encounter selection logic at the runtime boundary.
