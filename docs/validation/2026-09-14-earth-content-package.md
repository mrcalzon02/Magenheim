# Earth content package - 2026-09-14

Follow-up: the pending drop-table/LOD correction was installed after game exit.
Version 0.0.16 then passed geode vegetation registration during the isolated startup
recorded in [the workshop validation](2026-09-14-workshop-content.md). The pending
installation statements below describe the earlier handoff, not the current state.

## Delivered files

Version 0.0.15 adds seven original models with editable OBJ/MTL exports,
seven 512px PNG atlases, seven transparent item icons, a Crystal Shaping badge,
and an art preview. Runtime loads mesh JSON and PNGs directly; players need no
Unity editor, Blender, Python, or separate asset-bundle installation.

`build.ps1` packages only Magenheim.dll, Magenheim.Core.dll, definitions, and art.
`install-local.ps1` backs up Local-Magenheim and verifies every copied file.

## Observed build and asset validation

- Started on clean main at d65f6f9. Installed mod was still 0.0.1.
- Installed official .NET SDK 8.0.425 locally under ignored dist/toolchain.
- Core harness: 113 assertions passed, including definition/worldgen/placement coverage.
- Runtime: zero errors and zero warnings against installed Valheim and Jotunn 2.30.0.
- Seven meshes: nondegenerate triangles, consistent outward winding, closed surfaces,
  finite vertices, valid UVs, 512px atlases, and transparent 128px icons verified.
- Generated preview inspected. This is a software-rendered art preview, not in-game evidence.

## First live startup

The Central Fuckery profile log recorded:

```text
Loading [Magenheim 0.0.15]
Registered Crystal Shaping skill 'magenheim.crystal_shaping' (1979403462).
Registered 5 Earth crystal tiers and Earth Crystal Shards with original meshes, textures, and icons.
Registered 1 Magenheim geode item prefab(s) from definition authority.
```

Schema 3 loaded with matching baseline/effective fingerprint
`0646b334d1337c544170cfa73559d775f4304740b041dac7ad4b977604b429c4`.

Worldgen registration then failed because the pre-existing code assumed a
`DropTable.m_onePerPlayer` field. Inspection of the installed game confirmed
`m_oneOfEach` instead. The corrected implementation constructs its own DropTable
and DropData rather than modifying inherited objects. It fixes min/max/count to
one, chance/weight to one, and sets `m_dontScale = true`.

That correction and disabling inherited LOD groups compiled cleanly. Computer
control was stopped by the user's Escape key; no further game UI actions were taken.

An unrelated OreMines DLL file-lock exception also appeared in the profile log.
No third-party DLL was changed for this content delivery.

## Remaining evidence

The first package was installed and SHA-256 verified at
`C:/Users/Admin/AppData/Roaming/r2modmanPlus-local/Valheim/profiles/Central Fuckery/BepInEx/plugins/Local-Magenheim`.
The original 0.0.1 installation was archived as
`backups/Local-Magenheim-20260914-073305.zip`.
At handoff Valheim was still running, so the final drop-table/LOD correction is
built in `dist/Local-Magenheim-0.0.15.zip` but awaits installation after game exit.
Run `install-local.ps1` once Valheim has closed; it will verify the corrected files.

The corrected worldgen path needs a fresh game launch. Natural placement, actual
mining/drop behavior, appearance under Valheim lighting, save/reload, and multiplayer
remain unverified. Workstation crafting, geode opening/refinement inventory actions,
and earned Crystal Shaping XP are not part of this content package.

Local build/startup logs and installation backups remain in ignored dist/backups.
Use TESTING.md for precise spawn commands and the next world checks.
