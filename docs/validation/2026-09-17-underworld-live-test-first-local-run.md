# Underworld live test — first local build, install and run — 2026-09-17

Executes `docs/validation/2026-09-17-underworld-live-test-handoff.md`. This is the first local
compile of the remotely authored Underworld work, as that handoff predicted.

## Authority

- Repository `mrcalzon02/Magenheim`, branch `main` only.
- Start `HEAD` and `origin/main`: `5ba4a62f0746ca47cd9065f4ca01e5a442ae2c3b`.
- End `origin/main`: `4b03806bee287847f8bd8139cacfee1f0c4891d0`.
- Commits: `33dc47a` (build/gate/install repairs), `4b03806` (ValueTuple runtime repair).
- Plugin/package version unchanged at `0.0.62`.
- Compiler output and the installed Valheim 1.0.12 assemblies were treated as authority throughout.

## Handoff acceptance order — status

| # | Step | Result |
|---|---|---|
| 1 | Build completes with Core tests and patch/reflection verification | **Pass** |
| 2 | Startup reaches `Magenheim 0.0.62` without fatal bootstrap | **Pass** |
| 3 | Terrain Harmony targets match the installed build | **Pass** |
| 4 | Load/admit the manifest-backed derived Underworld save | **Not run** — blocked, see below |
| 5 | Log contains `Underworld terrain shaping active` | **Not run** (requires 4) |
| 6 | Conclave appears on terrain, not at legacy host altitude | **Not run** (requires 4) |
| 7 | Local player arrives alive beside it, not repeatedly teleported | **Not run** (requires 4) |
| 8 | Terrain changes and placeholder ecology refreshes while walking | **Not run** (requires 4) |
| 9 | Surface world terrain remains vanilla | **Not run** (requires 4) |
| 10 | Capture the first exception exactly and repair before widening scope | **Partly done** — three captured, one repaired, two are content decisions |

Step 3 evidence is stronger than a name match: `verify-patch-targets.ps1` resolves all 24 patch
declarations against `assembly_valheim.dll`, and the runtime logged
`Underworld terrain generation hooks installed for GetBiomeHeight/GetBiome`.

## Repairs, in dependency order

1. **Escaped newlines blocking compilation.** `UnderworldPlaceholderEcologyRuntime.cs` and
   `UnderworldWorldCenterRegistrar.cs` carried literal `\n` two-character sequences where real
   newlines belong — the same corruption `fb4dc80` fixed in the lifecycle. A context-aware sweep of
   `src/**/*.cs` (skipping string literals, char literals and comments) now reports zero
   code-context occurrences; all 78 remaining `\n` sequences are inside string literals. Note the
   registrar's corruption hid *inside* a line comment that had swallowed an entire method body, so a
   code-context-only scan misses it — classify by lexical context, not by grep.
2. **Placeholder ecology vs. the asset gate.** The streamer built silhouettes with
   `GameObject.CreatePrimitive`, which `verify-model-assets.py` rejects. No authored flora assets
   exist for six biomes, so it now instances the two authored Underworld meshes
   (`underworld-standing-stone`, `underworld-dais`), distinguished per biome by mesh choice, scale
   and tint. The gate keeps its protection; it was not allowlisted. Fungal caps stay walk-through.
3. **Missing creature sources.** `enemy-stone-guardian.blend` and
   `underworld-creature-sporeling.blend` had never been committed in repository history, so both
   creature gates failed on a missing source. Generated from their committed authoring tools; the
   sporeling additionally required `generate-underworld-sporeling-textures.py` (17 maps) first.
4. **Guardian contradicted its own contract.** `author-stone-guardian.py` declared
   `magenheim_character_height_m = 3.4` while building a 4.28m chassis its own gate rejected. It now
   normalises to the declared height and grounds the soles at Z=0, preserving authored proportions.
   Verified: `2.69 x 1.20 x 3.40m`.
5. **`tools/blender.ps1` reported passing gates as failures.** Windows PowerShell 5.1 wraps native
   stderr in ErrorRecords, so under the script-scope `Stop` preference the first line terminated the
   wrapper before its log was written — and Blender emits `DeprecationWarning` to stderr on a fully
   successful run. The wrapper's own log-inspection logic was unreachable. Now scoped to `Continue`
   around the invocation. Probed with a deliberately raising script: still exits 1, and now prints
   the real traceback, which it never could before.
6. **Province rotation ignored the seed.** `UnderworldTerrainLifecycle.SeedRotation` mapped the raw
   seed linearly across the `uint` range, so ordinary small seeds rotated by ~1e-6 rad and every
   such world got an identical province layout. Now avalanches the seed with the same finalizer as
   `UnderworldTerrain.Hash`. This was caught by the existing Core test, assertion 9.
7. **Valheim 1.0.12 API drift.** `ZoneSystem` exposes `instance`, not `m_instance` (3 sites);
   `WorldGenerator.GetBiomeHeight` now takes `out Color mask` plus two optional flags (2 sites);
   `UnderworldGeothermalHazardVolume.ReadHeat` already treated a null player as zero heat but
   declared the parameter non-nullable.
8. **Harmony patches declared pre-1.0.12 signatures.** Both terrain patches resolved to zero
   methods. `GetBiomeHeight` needs Harmony's parallel `ArgumentType[]` because an attribute cannot
   hold a by-ref `Type`; `GetBiome` gained two optional parameters. **`verify-patch-targets.ps1`
   could not verify either fix** — it had no Cecil assembly resolver for `0Harmony.dll`, so an
   `ArgumentType[]` decoded as zero constructor arguments and surfaced as a misleading "unsupported
   patch declaration", and it ignored `ArgumentType` entirely so by-ref parameters could never
   match. Both gaps are closed; the gate is strictly stronger than before.
9. **`System.ValueTuple` broke fungal provision registration at runtime.**
   `UnderworldFungalProvisionRegistrar.RegisterContent` threw `TypeLoadException` and never reached
   its own try/catch, so its error logging stayed silent and the three provisions were absent with
   only Jötunn's generic SafeInvoke warning. `Magenheim.Runtime` targets net462, where
   `System.ValueTuple` is a separate facade assembly that nothing deploys — not the package, not
   `valheim_Data/Managed`, not BepInEx. The build cannot catch this because the compiler resolves
   the reference from NuGet. Replaced with a named `ProvisionRequirement` struct, matching the
   existing `StaffRequirement` idiom. `Magenheim.Core` is unaffected: netstandard2.0 resolves
   `ValueTuple` through the shipped `netstandard.dll`. Runtime verified.

## Verification evidence

- `verify-model-assets.py`: 281 Blender/GLB/runtime sets, 834,782 triangles, no active shape
  generators.
- `verify-stone-guardian`: 44 parts, 2.69x1.20x3.40m, 4 crystal growths, 14 armor chips.
- `verify-underworld-sporeling`: meshes=41 bones=28 bounds=(0.505, 0.626, 0.386) textures=17.
- `Magenheim.Core.Tests`: 37,192 assertions passed.
- Runtime build: 0 warnings, 0 errors.
- `verify-patch-targets.ps1`: 24 Harmony patch targets and named argument bindings.
- `verify-reflection-targets.ps1`: 13 direct literal bindings and 42 helper-wrapped field contracts
  (12 dynamic, not statically checkable).
- Installed and SHA-256 verified `Magenheim 0.0.62` into the `Central Fuckery` profile; installed
  `Magenheim.dll` SHA256 `C3CE29988844950BB5B6F1BBCC250749713EFC7DB947415F8577E98440811C9F`.
- Cecil sweep: `Magenheim.dll` no longer references `System.ValueTuple`, and every remaining
  assembly reference resolves against the runtime search set.
- Runtime log: `Magenheim 0.0.62 loaded definition schema 5`,
  `Underworld terrain generation hooks installed for GetBiomeHeight/GetBiome`,
  `Registered the three canonical Fungal Forest provisions and cauldron recipes for Spore Communion`.

Launching the profile without r2modman's GUI requires Doorstop **command-line arguments**
(`--doorstop-enabled true --doorstop-target-assembly "<profile>\BepInEx\core\BepInEx.Preloader.dll"`).
The `DOORSTOP_*` environment variables do not work with Doorstop 4.4.0 and launch the game vanilla
and silently; confirm injection by checking that the profile's `LogOutput.log` mtime advanced.

## Open decisions — two registrars still fail at startup

Both carry deliberate author guards that refuse to substitute content, so neither should be resolved
without a decision.

### 1. `UnderworldDeepGateRegistrar` — no Aesir Passage prefab exists

```
InvalidOperationException: Valheim 1.0 Aesir Passage gate geometry was not found.
Magenheim will not substitute unrelated geometry.
```

The registrar searches prefab names for `aesir` + (`gate`|`passage`). **"Aesir Passage" is not a
prefab name in Valheim 1.0.12 — it is a localization string.** `resources.assets` contains
`hud_pin_dnboss,Aesir Passage,...`: it is the localized display name of the Deep North boss map pin.
No asset with `aesir` in its name exists in `StreamingAssets/SoftRef/manifest_extended` at all.

The Deep North boss location is internally **Morkhalla**. Candidate donors present in the installed
build include `Morkhalla_jotun_gate`, `DG_MorkHalla`, `LastBossGate` and the `Morkhalla_GateDoor*`
family. Choosing which geometry Magenheim's Deep Gate clones is a content decision.

### 2. `DeepFractureCreatureRegistrar` — `Wisp` is not a creature

```
InvalidOperationException: Vanilla creature source 'Wisp' lacks required Character/BaseAI/ZNetView behavior.
```

`S("Wisp")` is the first element of the source array, so this failure costs **all** Deep Fracture
creature variants, not just the Annoyance Wisp. `Wisp.prefab` exists but is an ambient light prop
with no AI, so it can never satisfy the guard. The other seven sources (`Tick`, `Greydwarf`,
`Draugr`, `DvergerMage`, `StoneGolem`, `Fenring`, `Seeker`) all exist. The wisp-family prefabs in
this build are `Wisp`, `LuredWisp`, `FrostWisp`, `FrostWisp_Storm`. `FrostWisp` is the Deep North
flying creature and is the only plausible chassis, but picking it is a content decision and the
guard exists precisely to refuse an unverified chassis.

Jötunn's `EventExtensions.SafeInvoke` wraps each handler, so one throwing registrar no longer
removes the ones after it — the failure mode recorded for `PrefabManager.OnVanillaPrefabsAvailable`
is contained for these subscribers.

## Blocker for steps 4–9

The derived save `Magenheim_Underworld_63e3f5c507e6dfb842a3` **does not exist**. Only its manifest
does, at
`BepInEx/config/Magenheim/underworld-world-pairs/Magenheim_Underworld_63e3f5c507e6dfb842a3.worldpair`
(parent save `rollandhiem`, parent world `572463657`, parent seed `v6UClzwtId`, derived seed32
`1675883973`). `IUnderworldPhysicalWorldLoader` is still unimplemented, so nothing creates it.

`UnderworldRuntimeIdentityResolver.TryResolveWorldSession` admits the Underworld layer purely by
**world save name** matching a `.worldpair` manifest, so the direct test route the handoff permits
is:

1. In Modded Valheim, create a new world named exactly `Magenheim_Underworld_63e3f5c507e6dfb842a3`.
   Its own Valheim seed does not matter — admission is by name, and terrain shaping uses the
   manifest's derived seed32. Do not rename the parent world `rollandhiem`.
2. Enter it with a character and watch for `Underworld terrain shaping active`,
   `Admitted Underworld world center`, and
   `Placed local player at grounded Underworld Conclave test arrival`.
3. Walk outward past 180m to force a placeholder ecology rebuild.
4. Load `rollandhiem` afterwards and confirm Surface terrain is unchanged.

This requires driving Valheim's menu, which this session cannot do.

## Next action

Decide the two donor questions above, then run the in-game sequence. Everything up to and including
plugin startup is reproducible with `./install-local.ps1`.
