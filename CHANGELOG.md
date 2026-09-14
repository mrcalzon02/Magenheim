# Magenheim Changelog

## Unreleased — repository consolidation

- Reconciled divergent `main` and `master` histories while preserving both ancestries.
- Replaced a contaminated committed merge tree containing unresolved conflict markers with an explicitly resolved live tree.
- Retained the material additive worldgen area validator/planner and deterministic worldgen tests from the divergent work.
- Reconciled crystal tier authority to Rough -> Simple -> Crystal -> Advanced -> Master.
- Added project-level repository and compatibility instructions.
- Added a standalone combined pure-core test harness and `IsExternalInit` compatibility shim.
- Preserved the large pre-reconciliation design specification under `docs/archive/` for provenance and future recovery.
- Did not admit bundled runtime/vendor binaries, runtime logs/process files, unrelated third-party repair utilities, duplicate legacy engines, or stale build/runtime claims into the live source tree.
- Compilation and Valheim runtime validation remain unclaimed because the current execution environment does not provide the required compiler/runtime test environment.
