# 2026-09-15 — Underworld transition persistence codec

Status: **source implementation committed; compile/runtime admission still required**

## Intent

Advance the U3 persistence framework behind the derived-world transition system before biome production. The previous slice established Core transition transactions and a thin Runtime orchestrator, but persisted snapshots still lacked a canonical serializer independent of Unity/Valheim/runtime serializer versions.

## Implemented

`src/Magenheim.Core/Underworld/UnderworldTransitionStateCodec.cs` adds one versioned canonical codec for `UnderworldPlayerLayerState`.

The codec:

- serializes stable Surface/Underworld state and incomplete transition state;
- preserves the surface return anchor, operation identity, source/target anchors, transition phase, authority fingerprint and recovery diagnostic;
- encodes arbitrary text as UTF-8 base64 so delimiters/control-like punctuation cannot corrupt the record structure;
- writes floating-point coordinates/headings using invariant round-trip formatting;
- rejects unknown format versions, duplicate fields, unknown/missing fields, invalid enum values and malformed numbers;
- reconstructs the state and then routes it through `UnderworldTransitionRules.ValidatePersistedState`, so a payload for the wrong parent/derived world pair or a corrupted transition cannot become runtime authority.

No Unity, Valheim, BepInEx or Jötunn dependency was added to Core. The codec does not decide where the runtime stores the payload; it supplies the stable representation that the eventual host adapter must persist atomically.

## Deterministic source coverage

`tests/Magenheim.Core.Tests/UnderworldTransitionStateCodecTests.cs` is wired into the existing Core harness and covers:

- exact stable-state round trip;
- canonical repeat encoding;
- prepared transition round trip;
- recovery-required transition/diagnostic round trip;
- unknown format rejection;
- unknown field rejection;
- invalid active marker rejection;
- corrupted authority rejection;
- cross-world replay rejection.

## Reconciliation

The slice was based on current `main` after concurrent rendering/combat work. It does not modify those systems and adds only the new Core codec, its deterministic tests, harness registration and this validation record.

## Deferred gates

This connector execution does not provide the normal local .NET/Valheim build environment, so compilation and test execution are not claimed.

U3 is not complete. The next dependency-valid slice is the runtime persistence adapter: store/read this canonical payload through a server-owned durable Valheim persistence location, perform atomic replace semantics, bind player/world identity, and invoke `ResumeOrRecover` on reconnect/load. Then execute crash/reload/reconnect and host/client recovery tests before marking persistent transition state complete.
