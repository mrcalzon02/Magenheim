# Workshop content delivery - 0.0.16

## Content

| Model | Triangles | Atlas | Icon |
| --- | ---: | --- | --- |
| Geologist's Workstation | 1,172 | 512px PNG | 128px transparent PNG |
| Fracturing Block | 548 | 512px PNG | 128px transparent PNG |
| Faceting Wheel | 304 | 512px PNG | 128px transparent PNG |
| Resonance Frame | 408 | 512px PNG | 128px transparent PNG |

Every model includes mesh JSON, OBJ/MTL source exports, a matching texture atlas,
and an inventory/build icon. The software-rendered workshop preview was inspected.
Geometry, consistent outward winding, closed surfaces, UV bounds, image dimensions,
and icon transparency pass verification for all eleven mineral/workshop assets.

The four pieces register in Hammer/Crafting. The main station uses the workbench
behavior with its own name, icon, models, colliders, and Crystal Shaping skill.
The upgrades extend only this station within five meters and do not stack with
duplicates. Vanilla resource requirements and recoverable build costs are listed
in TESTING.md. New/worn/broken model objects are independent variants so WearNTear
can toggle them without restoring vanilla visuals. Area/connection effects and
trigger colliders are retained separately.

## Build and installation

- .NET SDK 8.0.425; current installed Valheim assemblies; Jotunn 2.30.0.
- Core test harness: 113 assertions passed.
- Runtime compilation: zero warnings and zero errors.
- Installed into the active Central Fuckery profile with SHA-256 verification.
- Prior folder backup: `backups/Local-Magenheim-20260914-075509.zip`.
- Runtime DLL SHA-256:
  `56FFA25DF7B2B1E315CDCCE969A184075D09D028B5F1E0364B137EDF7D878888`.

## Observed Valheim startup

An isolated loader under `dist/workshop-smoke` used the packaged Magenheim
assemblies plus copies of BepInEx, Jotunn, and JsonDotNET. The installed profile
DLL was separately hash-matched to this package. The test launched Valheim in
batch mode without a world argument and was stopped after registration completed.
It did not exercise the other mods in the active profile or a saved world.

Observed log entries:

```text
Loading [Magenheim 0.0.16]
Registered Crystal Shaping skill 'magenheim.crystal_shaping' (1979403462).
Registered 5 Earth crystal tiers and Earth Crystal Shards with original meshes, textures, and icons.
Registered Geologist's Workstation and 3 original station upgrades in Hammer > Crafting.
Registered 1 Magenheim geode item prefab(s) from definition authority.
Added geode vegetation 'Magenheim_Geode_Meadows_Earth_World' for biome 'Meadows', area 'All', configured max-per-zone/chance 0.35.
Registered 1 Magenheim geode vegetation addition(s); 0 skipped by compatibility policy.
```

No Magenheim registration exception was recorded. Jotunn logged duplicate base-game
asset-name warnings; headless Unity logged an unavailable intro-video shader.
Neither prevented the content registrations above. Local logs remain under ignored dist.

## Remaining tests and scope

Registration is observed; actual Hammer placement, visibility under in-game
lighting, station leveling, resource recovery, natural generation, mining drops,
save/reload, and multiplayer are not yet observed. Geode opening/refinement
inventory actions, mineral recipes, and earned XP are not implemented in this build.
See TESTING.md for the manual world checks.
