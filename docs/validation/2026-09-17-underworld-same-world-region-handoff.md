# Underworld as a same-world region — handoff — 2026-09-17

Supersedes `2026-09-17-underworld-live-test-handoff.md`, which was written against the separate-save
architecture that the user has since rejected. Read `INSTRUCTIONS.md` first: the one-world, one-save
invariant there outranks every other document, including this one.

## Authority

- Repository `mrcalzon02/Magenheim`, branch `main` only.
- Session start `origin/main`: `5ba4a62f0746ca47cd9065f4ca01e5a442ae2c3b`.
- Session end `origin/main`: `09858f8ba09b793a0b92d82a5f8a46933dec0820`.
- Plugin/package version unchanged at `0.0.62`.
- Compiler output, the installed Valheim 1.0.12 assemblies and live play were the authorities.

## The correction that framed everything

The Underworld had been built as a **separate save file** reached by quitting to the main menu.
That was not the intent and never had been. It is now a **reserved region of the same world and the
same save**, resident concurrently with the surface, hosted the way Valheim's own Ashlands and Deep
North are: `worldSize = 10000`, `waterEdge = 10500`, `deepNorthMinDistance = 12000` with
`deepNorthYOffset = 4000`.

The document was the defect, not only the code — `UNDERWORLD_DESIGN.md` §6 was titled "Separate
persistent world instance" and specified exactly what had to be undone. §6 is rewritten and
`INSTRUCTIONS.md` carries the invariant, so the next session cannot rebuild it from the spec.

Decided by the user and recorded: **the Underworld uses the surface world's seed verbatim.** The map
differs because the generation algorithm differs. Do not hash, salt or derive a separate seed.

## Current architecture

- `UnderworldSpatialDomain` schema 2, `seed32-quarter-turn-offset-v2`: region centred at
  (40000, 0), radius 8000m, `HostBaseY` 0, minimum clearance 20000m from the world origin. The
  offset is **horizontal** because Valheim terrain is a 2D heightfield — `Heightmap.m_heights` is a
  flat list and `GetBiomeHeight` returns one height per column — so surface and Underworld ground
  can never share an (x, z). The old vertical band could host objects but never terrain, and that
  dead end is what produced the separate save.
- Terrain shaping keys off **coordinates**, not session: a column is Underworld iff
  `ContainsHostColumn`. Surface columns pass through untouched in the same session.
- Layer resolution keys off the **local player's position**, in
  `UnderworldRuntimeIdentityResolver`. All seven consumers follow from that one place.
- The terrain hooks run on `HeightmapBuilder`'s worker thread and must stay on an immutable
  main-thread snapshot plus pure maths. Any `UnityEngine.Object` access there is a hard crash.

## What works, verified in play

- Region generates and streams 40km out; the world loads without hanging.
- Real terrain: measured 3.50m of relief across a 50m screen in the central basin with a 0.37m
  steepest metre-step, 30.8m across 200m in Fracture Zones, 159m across the region from -68m ravines
  to +92m walls. Confirmed visually: ridges, valleys, cliff faces.
- Biome character falls out of the terrain rather than being special-cased: Blackwater Deep 100%
  below the 30m waterline, Fungal Forest 0%, Great Decay 48% wetland, Fracture 53.6%.
- Startup is clean: zero Magenheim errors, zero registrar exceptions, 26 Harmony patch targets,
  37198 Core assertions.

## The open defect: the biome sector override does not take effect

**This is the next task and everything else is downstream of it.**

Almost nothing except terrain height reads `GetBiome`. These all read `GetBiomeSector`:

| Caller | What it drives |
|---|---|
| `EnvMan::GetBiome`, `EnvMan::GetEnvironmentOverride` | weather and sky |
| `Heightmap::RebuildRenderMesh` | ground texture |
| `Minimap::UpdateBiome` | map biome |
| `SpawnSystem`, `SpawnArea` | what spawns |
| `ZoneSystem::PlaceVegetation` | vegetation |
| `Player::UpdateBiome` | the HUD biome |

Because the sector out there is still Ocean while `GetBiome` is overridden to Meadows, the game
disagrees with itself: `Player.UpdateBiome` logs `GetBiome error Ocean -> Meadows` every tick,
`SpawnSystem.UpdateSpawnList` throws a `NullReferenceException` every tick, **ocean fish spawn on dry
Underworld ground**, and the sky, weather and ground texture stay Ashlands.

What is already established:

- Every consumer reaches the sector through `GetBiomeSector(int gridx, int gridy, bool clamp)`;
  the float and Vector3 overloads both delegate to it and nothing calls it directly. One patch is
  the right shape.
- Grid to world is `MapSpaceToWorldSpace(g) = (g - 1024) * 12 + 6`, and the biome map is a 2048²
  texture at 12m per pixel, so it only spans **±12282m**. The region at 40000 is outside it. This is
  the most likely cause and the probe will confirm or eliminate it.
- `Player.UpdateBiome` IL: `loc.1` is the sector, `loc.2` is `GetBiome`, and the warning formats
  `{0}` from `loc.1.Biome`, so `Ocean -> Meadows` means the **sector** is Ocean. The HUD biome is
  read from `sector.BiomeType.Biome`, **not** `sector.Biome` — any working override must satisfy
  both.
- The patch is present and correctly declared in the shipped assembly and Harmony reports no
  failure, so it resolves; it is either not executing, failing containment, or returning a sector
  whose fields the consumers do not read.

A throttled probe is in `UnderworldTerrainRuntime.SelectBiomeSector`. It fires at most once every
five seconds, only for columns inside the region, and logs the grid received, the world coordinate
it converts back to, whether containment passed, and what vanilla's sector and
`BiomeSector.EmptyMeadows` each report for `Biome` and `BiomeType.Biome`. **Walk into the region and
read `[sector probe]` in the log** — that distinguishes all three remaining hypotheses in one run.
Remove the probe once the cause is known.

If the ±12282m bound is the cause, the region has to move inside it, which means re-deciding the
centre against `MinimumHostClearanceMeters` and Valheim's own far landmasses. That is a design
decision, not a code fix, and it belongs to the user.

## Also open

- **Map.** `Minimap::GenerateWorldMap` builds a texture sized to the vanilla world, so the region is
  off the canvas entirely: no unfurling cloud, and both layers share one plane. §25 already requires
  separate per-layer map state. Its own feature; the DungeonMaps mod is the precedent.
- **Sky.** Even once the sector is Meadows-consistent, Meadows sky is not the Underworld's sky. A
  custom skybox and environment is separate work.
- **Deep Gate.** Now clones `Morkhalla_jotun_gate` and registers, but renders as a magenta slab, so
  the donor or the material retheme is wrong. Magenta means a broken shader. Diff the donor's actual
  materials rather than guessing. Note "Aesir Passage" is only the localized display string for the
  `hud_pin_dnboss` map pin — no asset in 1.0.12 carries that name.
- **Placeholder models** are poor quality by the user's assessment. Deliberately deferred until the
  world underneath is right.
- **P0.-1 remainder.** The ~360-line physical world-switch layer and the `.worldpair` manifests are
  still present and unused on the terrain path; layer travel is still not a teleport.

## Traps that cost time this session, do not repeat them

- `ValidateDefinition` recomputes a **SHA-256**. Calling it from a per-column helper hung Valheim
  until Windows closed it (Application Hang, event 1002). Validate at construction only.
- `Mathf.PerlinNoise` with the seed folded into the coordinate rounds every column to the same
  input at the region's offset. Use `UnderworldTerrainNoise`.
- Normalising noise to [0,1] then scaling by one amplitude produces terrain nobody can see.
  Amplitude must be per octave, in metres.
- Raising relief without attenuating the coarse octaves toward the centre **floods the arrival
  basin** — 35% of the Fungal Forest under water at 2.3x.
- `System.ValueTuple` is not deployed; a tuple crossing the Core-to-Runtime boundary fails at
  runtime, and the compiler catches it as CS7036 only sometimes. Use a named struct.
- Launch with `-console` or the session is untestable: `valheim.exe --doorstop-enabled true
  --doorstop-target-assembly "<profile>\BepInEx\core\BepInEx.Preloader.dll" -console`. The
  `DOORSTOP_*` environment variables silently launch vanilla.

## Next action

Walk into the region, read `[sector probe]`, and fix the sector override from what it reports.
