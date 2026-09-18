# Underworld biome sector — world-space binding repair — 2026-09-18

Closes the open defect handed over by
`docs/validation/2026-09-17-underworld-same-world-region-handoff.md`: the biome-sector override
resolved, ran, and never took effect. The cause was settled from the installed Valheim 1.0.12
assemblies rather than from the live `[sector probe]`, which — as the evidence below shows — could
never have fired.

## Authority

- Repository `mrcalzon02/Magenheim`, branch `main` only.
- Session start `main` and `origin/main`: `df27a65f94f121c237afeea464d1edc5f43ae33c`.
- Plugin/package version `0.0.62` -> `0.0.63`.
- Evidence authority: `valheim_Data/Managed/assembly_valheim.dll` read through Mono.Cecil
  (`BepInEx/core/Mono.Cecil.dll` from the active Central Fuckery profile).

## Root cause

`WorldGenerator` declares three `GetBiomeSector` overloads. Both world-space overloads convert to
grid space and delegate:

```
GetBiomeSector(float wx, float wy, bool clamp)
  -> GetBiomeSector(AltBiomeWorldData.WorldSpaceToMapSpace(wx),
                    AltBiomeWorldData.WorldSpaceToMapSpace(wy), clamp)

GetBiomeSector(Vector3 worldPos, bool clamp)
  -> GetBiomeSector(WorldSpaceToMapSpace(worldPos.x),
                    WorldSpaceToMapSpace(worldPos.z), clamp)
```

`AltBiomeWorldData.WorldSpaceToMapSpace(x) = (int)((x - 6) / 12 + 1024)`, and its inverse
`MapSpaceToWorldSpace(x) = (x - 1024) * 12 + 6`. The biome map is 2048 cells at 12m, so it
addresses only ±12282m.

The grid overload then does this, before any lookup:

```
if (gridx < 0) gridx = 0;
if (gridy < 0) gridy = 0;
if (gridx >= 2048) gridx = 2047;
if (gridy >= 2048) gridy = 2047;
return m_world.m_biomeData.PointSectors[gridx, gridy] ?? BiomeSector.EmptyMeadows;
```

The `clamp` argument is never read in the body; the clamp is unconditional, and it is written with
`starg`, so it overwrites the parameter slot a Harmony postfix later reads.

The reserved region is centred at x = 40000. `WorldSpaceToMapSpace(40000)` is 4356, which the grid
overload clamps to 2047, which converts back to 12282m. Magenheim's postfix was bound to the grid
overload and reconstructed the world coordinate from the grid index it was handed, so it tested
containment at 12282m — 27.7km outside the region. `inside` was therefore always false, the override
always returned the vanilla sector, and the probe (which only logs when `inside`) could never emit a
line. Live play agrees: the patch was present, Harmony reported no failure, and nothing changed.

The ±12282m bound was the handoff's leading hypothesis and it is confirmed, but its feared
consequence is not: the region does **not** have to move. The bound only destroys the coordinate at
the grid boundary, and every consumer is upstream of it.

## Why binding to the world-space overloads is sufficient

Scanned every `call`/`callvirt` to `WorldGenerator::GetBiomeSector` across all assemblies in
`valheim_Data/Managed`:

| Caller | Overload |
|---|---|
| `EnvMan::GetBiome`, `EnvMan::GetEnvironmentOverride` | `(Vector3, bool)` |
| `Minimap::UpdateBiome` | `(Vector3, bool)` |
| `Player::UpdateBiome` | `(Vector3, bool)` |
| `SpawnSystem::GetLevelUpChance`, `SpawnArea::GetLevelUpChance` | `(Vector3, bool)` |
| `ZoneSystem::PlaceVegetation`, `ZoneSystem::GenerateLocationsTimeSliced` | `(Vector3, bool)` |
| `WorldGenerator::GetBiomeArea` | `(Vector3, bool)` |
| `Terminal::InitTerminal` (console biome command) | `(Vector3, bool)` |
| `Heightmap::RebuildRenderMesh` | `(float, float, bool)` |
| `HeightmapBuilder::Build` | `(float, float, bool)` |
| `WorldGenerator::GetBiomeHeight` | `(float, float, bool)` |
| `WorldGenerator::GetBiomeSector(float/Vector3)` | `(int, int, bool)` |

Nothing reaches the grid overload except the two world-space overloads themselves, so patching those
two covers every consumer with the true world coordinate still intact.

## The change

`src/Magenheim.Runtime/UnderworldTerrainRuntime.cs`:

- `UnderworldBiomeSectorPatch` (grid overload) is removed. It cannot work at any region position
  outside ±12282m, and leaving it would be a dead patch next to a live one.
- `UnderworldBiomeSectorColumnPatch` postfixes `GetBiomeSector(float wx, float wy, bool clamp)`.
- `UnderworldBiomeSectorPointPatch` postfixes `GetBiomeSector(Vector3 worldPos, bool clamp)`.
- `SelectBiomeSector` now takes world coordinates and tests `ContainsHostColumn` directly, with no
  grid round-trip.
- The temporary `[sector probe]` and its throttle are removed; the question they existed to answer
  is answered, and the answer is that they could not have answered it.

The returned value is unchanged: `BiomeSector.EmptyMeadows`, which agrees with the existing
`GetBiome` override. Verified from IL that it is constructed as `new BiomeSector(null, Meadows)`,
and that the null-world branch of the constructor assigns `BiomeType = new BiomeTypeInfo(Meadows)`
with three allocated lists. So `Biome` and `BiomeType.Biome` both read Meadows — `Player.UpdateBiome`
reads the latter — and `AltBiomes` is empty but non-null, which `Heightmap.GetBiomeColor(BiomeSector)`
enumerates before falling back to `sector.Biome`. No consumer on this path dereferences null.

`HeightmapBuilder.Build` reaches the new patch on its worker thread through `GetBiomeHeight`, so the
handler stays on the immutable `WorldSnapshot` plus pure arithmetic with no `UnityEngine.Object`
access, exactly as the height and biome hooks already do.

## Static validation

- `tools/verify-patch-targets.ps1` resolves every Harmony declaration against the installed
  assemblies and binds each named postfix parameter to a parameter of the same name and type on the
  target. Both new declarations pass; targets move 27 -> 28.
- `tools/verify-reflection-targets.ps1`: 13 direct literal reflection bindings and 42
  helper-wrapped field contracts verified against the installed assemblies; 12 resolve dynamically
  and are not statically checkable.
- `build.ps1 -Offline`: 38,320 deterministic Core assertions pass, 281 model assets import twice,
  and `Magenheim.Core` plus `Magenheim.Runtime` compile with 0 warnings and 0 errors. This is the
  first passing build of `main` since 2026-09-17; see
  `2026-09-18-main-build-gate-repair.md` for what had to be repaired to get here.

## Not claimed

Nothing here is runtime-accepted. The game has not been launched against 0.0.63. Specifically
unverified: that the HUD biome stops logging `GetBiome error Ocean -> Meadows`, that
`SpawnSystem.UpdateSpawnList` stops throwing, that ocean fish stop spawning on dry Underworld
ground, that the ground texture stops reading as Ashlands, and that weather and sky change. Those
are the observations the next live session should make, in the region, with `-console`.

Meadows sky is still not the Underworld's sky; a custom environment and skybox remain separate work
under P0.-1.
