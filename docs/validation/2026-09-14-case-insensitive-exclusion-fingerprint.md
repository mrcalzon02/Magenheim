# Validation Record — Case-Insensitive Exclusion Fingerprint Canonicalization

Date: 2026-09-14

## Scope

This bounded pass reconciled definition fingerprinting with the already-authoritative case-insensitive worldgen compatibility exclusion semantics.

## Defect

`WorldgenAdditionPlanner` matches compatibility exclusions with `StringComparer.OrdinalIgnoreCase` when `RegistrationIdentityComparison.CaseInsensitive` is selected, but `MagenheimDefinitionValidator.ComputeFingerprint` previously hashed the original exclusion strings verbatim. Two peers could therefore configure semantically identical exclusions such as `magenheim.compatibility.foo` and `magenheim.compatibility.FOO`, produce different definition fingerprints, and fail the server/client authority handshake even though the effective compatibility behavior was identical.

## Repair

- Fingerprint ordering now uses the configured registration identity comparer.
- Case-insensitive exclusion identities are canonicalized with invariant uppercase before hashing.
- Exact comparison continues to hash original casing and therefore remains casing-sensitive.
- The validated snapshot preserves the configured exclusion spelling; canonicalization affects authority hashing only.
- Geode registration keys and prefab definitions are not case-normalized by this repair because their original names remain runtime-facing identities, not merely matching-set members.

## Deterministic coverage added

`DefinitionCompatibilityTests` now adds source-level vectors proving:

- semantically identical key/prefab exclusions differing only in suffix casing produce the same fingerprint under case-insensitive policy;
- exclusions differing only by case produce different fingerprints under exact policy.

## Validation boundary

The current execution host still does not expose a .NET compiler/runtime path. The repair and tests were statically reviewed and committed source can be inspected, but no passing test or compilation claim is made.

Next executable gate:

`dotnet run --project tests/Magenheim.Core.Tests/Magenheim.Core.Tests.csproj`

followed by runtime compilation and live definition-authority handshake validation against Jötunn 2.30.0/current Valheim dependencies.
