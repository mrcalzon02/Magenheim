# Magenheim Project State

## Authority

This file records verified repository state. Live committed repository state and the authoritative design specification outrank stale conversation or scheduled-task claims.

## Current branch

Active development branch: `main`.

The repository was initially created with GitHub's `master` default. The first commit contained only `README.md`, even though that README described a much larger 0.0.2 foundation. The missing implementation is treated as an incomplete bootstrap, not as verified completed work.

## Restored foundation

The dependency-free `Magenheim.Core` slice now owns the crystal/refinement domain boundary. The advanced-code review identified and corrected semantic drift introduced during bootstrap: the third tier is `Refined`, the normal element catalog is Earth/Fire/Frost/Storm/Venom/Radiance/Seidr/Spirit, and refinement uses skill-scaled failure rather than fixed success chances or invented progression gates.

Canonical refinement behavior is now defined as:

- base failure 10/20/30/40 percent from Rough through Master progression;
- Crystal Shaping reduction using the configured 50%-100% maximum-reduction range, default 75%;
- workstation/upgrade progression as the tier gate;
- valid failure destroys the source crystal and returns 1/2/3/5 matching shards;
- valid success and failure attempts are experience-eligible;
- invalid requests are non-mutating and award no experience;
- elemental alignment is preserved through refinement.

The pure-domain service remains intentionally independent of Unity, Valheim, BepInEx, and Jötunn so later inventory/RPC code has one deterministic calculation authority.

No claim is made that Valheim runtime integration, BepInEx/Jötunn registration, world spawning, item registration, inventory mutation, RPC authority, sockets, equipment effects, save/load persistence, or multiplayer transaction handling are implemented until those files exist and are directly verified.

## Validation boundary

This environment does not expose a .NET SDK/compiler, so this cycle can perform source-level and Git-object verification but cannot claim compilation or runtime validation. Runtime acceptance remains deferred.

## Current milestone

Milestone 0: repository truth and pure-domain crystal progression foundation.

Next dependency-valid advanced slice: add deterministic standalone tests/build execution for the corrected refinement engine, then bind validated static/config definitions into the BepInEx/Jötunn runtime bootstrap without enabling inventory mutation until server-authoritative transaction handling exists.
