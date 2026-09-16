# Underworld six-Deepstone progression validation

Date: 2026-09-16

## Bounded cycle

Reconciled authoritative `main` after the Deepstone replay-protection and compensating-transaction work. The next dependency-valid framework slice was deterministic coverage of the complete canonical Conclave progression rather than beginning individual Deep Boon effects.

## Changed

`UnderworldDeepstoneProgressionTests` now uses the complete six-biome, six-boss, six-Deepstone canonical progression fixture instead of the earlier Bloom/Tide subset.

The deterministic progression pass now exercises Bloom -> Tide -> Cinder, verifies that Fracture remains blocked while Rime is absent, then completes Rime -> Fracture -> Decay. Every admitted mount is required to authorize exactly one trophy consumption, atomically produce `TrophyMounted + BoonUnlocked`, and preserve the canonical Deep Boon identity. The completed Conclave must contain six mounted stones and six distinct unlocked boons.

Existing negative invariants remain covered: dependent stones fail closed, wrong trophies are rejected, missing authoritative inventory cannot authorize consumption, and duplicate mounted requests consume nothing.

## Verification boundary

The authoritative test source was committed directly to `main`. This connector environment cannot execute the .NET test project or launch Valheim, so compilation and live runtime behavior are not claimed by this record.

## Next actionable slice

Framework completion should now move one layer outward from Core progression into runtime reconstruction. Add deterministic coverage for rebuilding the complete six-stone Conclave from persisted world keys across reconnect/restart boundaries, including rejection of impossible boon-without-trophy persistence and confirmation that the reconstructed state still satisfies the Cinder/Rime join required by Fracture. After that, validate the runtime interaction adapter against the same six-stone fixture before beginning individual Deep Boon behavior.
