# Deep Fracture exact passage-footprint collision — 2026-09-14

## Scope

Bounded repair to the pure-core physical passage router. Surface Deep Fracture registration remains gated; this change does not enable locations or claim runtime world acceptance.

## Defect

`DeepFracturePassageRouter.SegmentIntersectsFootprint` approximated segment-versus-district collision by projecting the district center onto the passage segment and testing that single point against the expanded square footprint. The comparison was also strict (`<`). A passage tangent to an expanded footprint edge or corner could therefore be admitted even though the passage has non-zero width and the four-metre clearance expansion is intended to be a hard exclusion boundary.

## Repair

The authoritative pure-core router now performs an exact inclusive two-axis parametric slab intersection against the expanded district AABB. Parallel segments are handled explicitly; invalid non-finite/negative clearance is rejected. Boundary contact is treated as collision rather than safe clearance.

A deterministic regression was added to `DeepFractureInteriorBlueprintTests`: a diagonal direct route tangent to a blocker district's expanded footprint must be rejected and replaced with a three-point bent route. Existing deterministic route generation across Small, Full, and Grand seed samples remains part of the same test surface.

## Compatibility and persistence

The change is deterministic pure geometry. It does not mutate Valheim/Jotunn state, save data, network state, prefabs, foreign registrations, or existing item metadata. Equal blueprints still resolve through the same ordered candidate set, so multiplayer peers with identical authority derive identical routes.

## Verification boundary

Source and regression construction were reviewed against the current `main` authority at start SHA `48241a509567795acde7aef42c99ee766135c32a`. This connector environment cannot execute the repository's .NET test harness or Valheim runtime, so compilation/test execution and live corridor assembly remain deferred. The existing surface-generation gate remains closed.

## Next dependency-valid slice

After this exact module-footprint primitive is admitted by the local deterministic suite, extend physical assembly safety to passage-versus-passage conflicts and then bind the validated routes to deterministic runtime corridor segment placement. Do not enable Deep Fracture surface generation until physical assembly passes collision validation.