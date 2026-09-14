# Compatibility

## Magenheim 0.0.1

| Component | Local baseline | Evidence |
|---|---|---|
| Valheim | 1.0.12, network 40 | Existing profile startup log |
| Unity | 6000.0.75 | Existing profile startup log |
| BepInExPack Valheim | 5.4.2350 | Installed dependency and compile reference |
| Jotunn | 2.30.0 | Installed dependency and compile reference |
| JSON | Game-provided Newtonsoft.Json 13 | Used by loader; not redistributed |

Magenheim compiles against this local baseline. Compilation does not establish compatibility with every other mod or a completed multiplayer test. The plugin requires BepInEx and Jotunn; it uses no unrelated magic framework and installs no Harmony patches.

The Jotunn declaration requires the mod on all participants and checks minor-version compatibility. Server-authoritative balance synchronization is not implemented yet; no gameplay transactions are enabled. Crystal Shaping uses the stable identifier `magenheim.crystal_shaping`.

## September 13 crash investigation

The 16:49–16:51 failed session contains no Magenheim loading line, and no Magenheim DLL was present in the profile. The initial build was staged in the source project only. These logged failures therefore were not caused by Magenheim executing.

- Backpacks 1.3.8, RenegadeVikings 1.4.0 and Blacksmithing Expanded 1.1.7: embedded ServerSync reads `ZRoutedRpc.Everybody` as a static field; Valheim 1.0.12 exposes a literal constant.
- Multiple older mods reference four-argument `Character.Message`; the game now requires a fifth `log` boolean, default false. This error repeats during gameplay, including a patched sleeping update. The abbreviated stack does not identify every active caller.
- Seasons 1.8.2: obsolete ConsoleCommand constructor and missing `Hoverable.GetHoverOffset` implementation in `IceFloeClimb`. The invalid type also disrupts Protective Wards' type inspection.

These are confirmed binary incompatibilities. The available logs do not prove which exception caused the final process exit; no native crash conclusion is inferred from them.

## Local compatibility repair

`tools/RepairLegacyApis.cs` creates staged copies of affected third-party assemblies. It replaces only the obsolete message/constant/constructor call sites and supplies Seasons' missing hover offset as zero. Message forwarding preserves the game's new default (`log=false`); console forwarding preserves `hideBehindDevCommands=false`. It does not change game assemblies, mod GUIDs, versions, item IDs, save data, recipes or world content.

Repairs belong to the existing profile and are separate from the Magenheim ZIP. Original DLLs must be backed up before deployment; manager updates may replace local repairs. Prefer an upstream compatible release when available. Keep the repair report and backup record when diagnosing subsequent errors. Static rewriting checks are not a substitute for an in-game retest.
