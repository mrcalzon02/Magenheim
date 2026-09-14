# Geode Opening Transaction Planner Validation — 2026-09-14

## Scope

This bounded cycle added the pure-core bridge between deterministic geode cracking and future server-owned Valheim inventory mutation. The planner does not mutate inventory and does not perform networking. It produces a transaction plan only when all preconditions are already satisfied.

## Admission invariants

A geode-opening plan is `Ready` only when:

- definition authority status is `Compatible`;
- `MutationAuthorized` is true;
- at least one source geode exists;
- the deterministic `GeodeCrackingService` request succeeds;
- available output capacity can contain the complete cracking result.

Every rejected plan has `ConsumeGeodeCount == 0` and an empty crystal grant list. The planner therefore cannot represent a partial consume/grant transaction.

An inconsistent authority record such as `Pending` with `MutationAuthorized = true` is rejected. Mutation admission requires both the compatible status and the authorization flag rather than trusting the flag alone.

## Deterministic coverage added

`GeodeOpeningTransactionTests` covers:

- compatible authority produces a ready plan;
- ready plans consume exactly one source geode;
- successful 35%/10% bonus outcomes propagate all three crystal grants;
- Rough-tier output remains owned by `GeodeCrackingService`;
- pending authority fails closed;
- inconsistent/forged-looking authority state fails closed;
- missing source rejects without consumption;
- insufficient output capacity rejects without partial grants;
- exact capacity succeeds;
- malformed cracking input rejects without consumption or grants.

The new test set is wired into the existing console harness through `DefinitionAuthorityTests.Run()`.

## Validation boundary

Source and Git readback were verified. This environment still does not provide the .NET SDK/compiler required to execute the harness, so no passing-test or compile claim is made.

## Runtime contract

Future Valheim runtime code must treat a `Ready` plan as a preconditioned proposal, not as proof that the inventory is still unchanged. The server adapter must revalidate source ownership and capacity immediately before applying the transaction, then consume the source and grant every planned output atomically or perform no mutation.

## Next dependency-valid action

The executable gate remains first: run the complete pure-core harness and compile the runtime against Jötunn/current Valheim. Once that gate passes, register `magenheim.crystal_shaping`, then bind this planner to a server-owned atomic inventory transaction rather than reimplementing geode outcome logic in the runtime layer.
