# Monumental terrain closeout — 2026-09-23

Pulled and fast-forwarded main through `998098d`, retaining the new unique location and Deep
Sigil residency/discovery registrations. Added a terrain development checkpoint in 0.0.103.
The user's subsequent cloud-line height clarification and lava/fungal sunless skybox request
are the next continuation; this checkpoint is not final visual acceptance of those requirements.

## Implemented

- Seeded, irregular faceted spires and broad plateaus add 3.2–5.6 km relief in sparse outer cells.
  Plateau walls rise across a 64m horizontal band; crowns retain normal biome surface relief.
  The whole footprint stays outside the central 27% radius and within the realm boundary.
- Default vertical admission extends to 8192m; the engine adapter extends correspondingly.
  Terrain-shape algorithm identity participates in authority fingerprint schema 5.
- Distant terrain uses the same Core heightfield at 16m spacing across 1024m tiles, with no
  collision or ecology duplication. Four tiles build per 0.5s residency pass (about 30s warmup).
  Resident-facing boundary fans sample native 2m edges; adjacent distant tiles are invalidated
  together when residency changes. Steep triangles use vertical texture projection and tangents.
- Clear-air sight distance now reaches kilometres; dense weather/miasma still reduces visibility.
  The existing atmosphere adapter extends and restores the active camera's far clipping distance.

## Verification

- 44,588 Core assertions plus separate suites passed. Five seeds cover deterministic landmarks,
  >3km summits, steep walls, plateau crowns/spire tips, protected arrival footprints, sparse
  coverage, short custom ceilings, and exact shared chunk border heights.
- Core-derived meshes rendered through `tools/render-underworld-terrain-review.py`: spire and
  plateau at 8m geometry-review and 16m horizon sampling. Inspected all silhouettes; widened
  crowns, increased horizon detail, made support planes unequal and added irregular recesses.
  Local evidence: `artifacts/review/terrain/{spire,plateau}-{detail,horizon}.png`.
- `closeout.ps1 -Offline` passed: 367 fresh generated files / 13 generators; 344 model sets;
  142 icons; creature source, texture and gameplay review gates; runtime zero warnings/errors;
  42 patch targets; reflection and assembly binding gates; launcher metadata checks.
- Installed 0.0.103 into Central Fuckery; all payload hashes and enabled launcher metadata match.
  DLL SHA-256: `97071D6B4BA120DFFFB57D8A077FA1A19A21C62E73CE1E513062FFEA0CD0C698`.
  Backups: `backups/Local-Magenheim-20260923-170121.zip`,
  `backups/mods-20260923-170139-846.yml`. Log: `artifacts/terrain-closeout-20260923.log`.

Launcher description: "0.0.103: Monumental Underworld terrain - kilometre-high spires and sheer plateaus, distant terrain, clearer skyline visibility and cliff texture projection. Includes new unique biome locations and Deep Sigil discovery."

No Valheim world or launcher UI was observed. In-game silhouettes, frame time, LOD transitions,
plateau collisions/building, fog, Surface restoration, location placement and multiplayer remain
unverified. Offline renders are geometry evidence only; they do not render Valheim materials,
atmosphere or the upcoming skybox. See TESTING.md for live acceptance steps.
