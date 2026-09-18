# Underworld live-test handoff — 2026-09-17

> **SUPERSEDED — do not execute.** This handoff was written against the separate-save architecture
> the user has since rejected. It is retained as evidence of what was attempted and observed, not as
> instructions. The binding architecture is the one-world, one-save invariant in `INSTRUCTIONS.md`,
> and the current handoff is
> `docs/validation/2026-09-17-underworld-same-world-region-handoff.md`. Where this file disagrees
> with `INSTRUCTIONS.md`, this file is the defect.

## Authority and repository state

- Repository: `mrcalzon02/Magenheim`
- Authoritative branch: `main` only. Do not create a routine repair branch.
- Plugin/package version remains `0.0.62`.
- This handoff is intentionally about compile/install/live-runtime acceptance, not new feature scope.
- Preserve the existing Core -> Runtime/Jotunn/Harmony boundary and the persisted Surface/Underworld world-pair manifest.
- Do not add GitHub Actions.

## Immediate local objective

Get the current `main` building, install it into the disposable r2modman Valheim profile, launch Modded Valheim, and obtain the first useful Underworld runtime evidence. Fix compile/API/runtime faults in dependency order. Do not spend the first test window refining models.

Canonical local command from the repository root:

```powershell
./install-local.ps1
```

That script runs the repository validation gates, Core tests, runtime build, Harmony/reflection target verification, packages 0.0.62, backs up the previous installation, installs into the configured disposable profile, and SHA-256 verifies the installed DLL. Valheim and r2modman must be closed while it installs.

If online restore is unavailable, use:

```powershell
./install-local.ps1 -Offline
```

## Critical framework now present

The derived Underworld physical session is manifest-recognized. Runtime terrain hooks alter `WorldGenerator.GetBiomeHeight` only in an admitted derived Underworld session and neutralize vanilla biome ecology through the Meadows compatibility substrate. Surface sessions pass through unchanged.

The Underworld center/Deepstone Conclave is grounded against generated derived-world terrain instead of the legacy ~8192m reserved-band host altitude. The local player has a test arrival path beside that grounded center.

Six-biome deterministic terrain authority exists for Fungal Forest, Blackwater Deep, Sulfurous Wastes, Frozen Caverns, Fracture Zones, and Great Decay. A disposable local placeholder-ecology streamer gives nearby visible biome silhouettes for traversal testing without persistence/network ownership. Placeholder materials now fail neutral if expected shaders are stripped.

The physical transition transaction, persistence/recovery, manifest target preparation, pending-handoff state, and idempotent dispatch loop exist. The remaining engine boundary is `IUnderworldPhysicalWorldLoader`: there is still no verified concrete Valheim save-loader adapter. Do not fake this by mutating logical layer state. For the first terrain test it is acceptable to load the exact manifest-backed derived save directly.

## Last-minute repair already made

`UnderworldWorldSessionLifecycle.cs` contained nine literal escaped `\\n` sequences inside `TryPlaceLocalUnderworldPlayer`, which are invalid C# source in that location and would block compilation. They were converted to actual newlines in commit `fb4dc80b52cd9187cb833486a880a6f17d33d69c`.

Treat the first local compiler output as authority. There has not yet been a local compile after this remote repair.

## First live-run acceptance order

1. Build must complete with Core tests plus patch/reflection target verification.
2. Plugin startup must reach the normal `Magenheim 0.0.62` loaded diagnostic without fatal bootstrap.
3. Confirm terrain Harmony targets still match the installed Valheim build.
4. Load/admit the manifest-backed derived Underworld save.
5. Confirm log contains `Underworld terrain shaping active`.
6. Confirm the Conclave appears on terrain, not at legacy host altitude.
7. Confirm the local player arrives alive beside it and is not repeatedly teleported.
8. Walk outward. Confirm terrain changes and local placeholder ecology refreshes without exceptions.
9. Verify ordinary Surface world terrain remains vanilla/unmodified.
10. Capture the first exception/stack trace exactly and repair that before widening scope.

## Known highest-priority unresolved item

Implement `IUnderworldPhysicalWorldLoader.TryLoad(UnderworldPhysicalWorldSwitchRequest,...)` only after checking the actual installed Valheim 1.0.12 assemblies/API. It must select/load the exact `TargetSaveName` already produced by the manifest-backed driver. It must not derive another save, commit transition state, place the player, or invent a parallel world-switch authority.

If Valheim does not expose a safe in-session save switch, retain the existing durable pending handoff and use the direct derived-save test route rather than adding a brittle reflection hack merely to claim seamless switching.

## Runtime logs worth grepping

- `Underworld terrain generation hooks installed`
- `Underworld terrain shaping active`
- `Admitted Underworld world center`
- `Placed local player at grounded Underworld Conclave test arrival`
- `Underworld transition awaits physical save`
- `Underworld physical handoff`
- `Fatal`
- `Exception`

## Stop condition for the handoff

A successful first pass is not “the Underworld is finished.” It is: current main builds and installs; derived save is admitted; terrain visibly differs; Conclave/player placement is sane; placeholder ecology is observable; Surface remains untouched; and logs provide actionable evidence for every remaining failure.
