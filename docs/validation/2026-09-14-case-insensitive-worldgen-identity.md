# Validation Record — Case-Insensitive Worldgen Identity Authority

Date: 2026-09-14

## Scope

This bounded pass reconciled schema-level duplicate validation with the already-configurable `RegistrationIdentityComparison` used by the additive worldgen planner.

## Defect

`WorldgenAdditionPlanner` correctly treated registration and prefab identities case-insensitively when configured, but `MagenheimDefinitionValidator` still checked duplicate geode ids, duplicate prefab names, and duplicate exclusion entries with ordinal case-sensitive comparison. A schema could therefore admit two definitions which the planner would later collapse into a collision. That produced order-dependent skip/error behavior after definition admission instead of rejecting the conflicting authority at the source.

## Repair

- Definition duplicate geode-id checks now use the configured identity comparer.
- Definition duplicate prefab checks now use the configured identity comparer.
- Registration-key and prefab exclusion normalization now uses the same configured comparer.
- Exact comparison remains case-sensitive by design.
- Case-insensitive comparison rejects case-only duplicate identities before fingerprinted definition admission.
- Namespace ownership checks remain exact: geode ids still require `magenheim.geode.` and prefab names still require `Magenheim_`.

No vanilla or foreign registration mutation path was added. This is validation tightening only.

## Deterministic coverage added

`DefinitionCompatibilityTests` adds source-level vectors for:

- case-only duplicate geode ids under case-insensitive policy;
- case-only duplicate prefab names under case-insensitive policy;
- case-only duplicate compatibility exclusions under case-insensitive policy;
- preservation of case-distinct owned exclusions under exact policy.

`Program.cs` now invokes the new test group.

## Validation boundary

The repository host used for this pass does not provide a .NET compiler/runtime execution path, so the added tests are committed source coverage only. No passing test or compilation claim is made. The next executable gate remains:

`dotnet run --project tests/Magenheim.Core.Tests/Magenheim.Core.Tests.csproj`

followed by runtime compilation against Jötunn 2.30.0/current Valheim dependencies.
