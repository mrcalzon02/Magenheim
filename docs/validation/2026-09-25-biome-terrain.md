# Biome terrain expansion — 0.0.107

The old generator shared a small relief spectrum, suppressed regional noise across the
entire forest, clamped deltas to +/-300 m, and used max(height, monumentHeight). Since a
missing monument returns zero, that last operation flattened all negative basins.

The new Core authority uses six immutable biome profiles with metre amplitudes and
independent seeded noise. Forest rolls, sulfur/frozen provinces form ridges, Fracture uses
faulted relief, and Decay forms basins. Heights are naturally bounded by octave amplitudes,
not clipped. Ecology normalization is retained. Ordinary minimum is conservatively above
-878 m; the default admitted bottom is -896 m, inside the existing engine layer. Monument
summits remain capped by their existing sky design. Arrival protection covers 80 m,
ramps to full strength at 320 m, and no longer flattens the full forest. Province heights
blend 256 m either side of a sector boundary; the radial forest blend remains in place.
The biome algorithm ID participates in the authority fingerprint.

## Measured before/after

Core TerrainSurvey tool, seed 12345, 80 m grid, radius 8000 m. Monument footprints excluded.
These are sampled ranges, not theoretical limits or live screenshots.

| Biome | Previous min/max (m) | New min/max (m) |
|---|---:|---:|
| Fungal Forest | 39 / 64 | 46 / 372 |
| Blackwater Deep | 0 / 69 | -331 / 371 |
| Sulfurous Wastes | 0 / 129 | -217 / 911 |
| Frozen Caverns | 0 / 124 | -257 / 1240 |
| Fracture Zones | 0 / 182 | -711 / 1424 |
| Great Decay | 0 / 84 | -380 / 727 |

Run tools/TerrainSurvey with output path and optional seed to reproduce the current data.
Local raw evidence: artifacts/terrain-before-0107.json and terrain-after-0107.json.
Regression coverage surveys three seeds, checks every admitted sampled height and standing
player against instance bounds, determinism, distinct minimum relief spans, negative basins,
old clamp exceedance, exact gate fine-relief preservation and all five angular borders.
Core harness: 94,349 assertions plus separately reported module suites passed.
Build, install and catalog verification are recorded below. No new live world,
collision, visual, multiplayer or persistence acceptance is claimed.

## Remote, branches and stashes

Remote main was 722b294 with no incoming changes. Merge 6b73e46 records the historical
rescue/local-master-before-cleanup merge a155349 as reconciled with authoritative main.
Its parents were already integrated; its residual tree included obsolete content and conflict
markers, so the current main tree was retained. rescue/pre-models-origin-main was already
an ancestor. No local branches remain outside main's ancestry.

All four stashes were audited against main history before removing their active stash entries.
Local recovery tags preserve the exact original snapshots (tags are not published):

| Tag suffix (prefix archive/reconciled-20260925-) | Stash commit | Resolution |
|---|---|---|
| gate | 5e6df3b3eb2dfee318d5821782ab9e02b3447077 | Gate/forest repairs integrated and extended by subsequent release work. |
| rootforged | 4d9997714861c1e4fb370d308e2a1dcc1a6cc35d | 122 of 126 file blobs already in main history; content integrated by 019f404/7bcb10d, extended by f28e070. Remaining snapshots are superseded generated/model/plugin versions. |
| sword | 3b37629bfb58bc0396a5a7b90053eba7cb912673 | Earlier inconsistent sword export superseded by d12222c and bc29d5f. |
| legacy-rendering | 8e4510d8ff34cc55b91cda173d47048cf62fcb49 | Historical rendering/procedural files and conflict markers superseded by authored source and later fixes; original snapshot retained. |

The local blob audit is artifacts/reconciliation-audit.json. Archive tag targets were checked
against stash hashes before removal. No stash carried a separate untracked-file payload.

## Delivery verified

Offline package build passed, including runtime compilation with zero warnings/errors,
94,349 Core assertions, 356 model imports, placement and fungal catalogue gates.
Existing ModelAssetTests nullable warnings and six legacy 128px icon advisories remain;
the existing build script still defers Capcrawler source/render acceptance.
Installed 0.0.107 into the active Central Fuckery profile, verified all installed package
hashes and the enabled launcher catalog version/description/dependencies. Launcher display
was not visually inspected and no game startup was performed.

- Runtime DLL SHA-256: CEE87CCF3EE36E0F79B5A2676369239645225C62F91336D4C271B791C60483F0
- Core DLL SHA-256: A73A2365C47BCD7189A23A07670DDEF42F87D710A3195F14F4C179D380F30D25
- Prior install backup: backups/Local-Magenheim-20260925-224959.zip
- Prior launcher catalog: backups/mods-20260925-225012-636.yml
- Build evidence: artifacts/biome-terrain-build.log

Launcher description: "0.0.107: Distinct biome terrain with unclipped relief: fungal hills,
deep Blackwater basins, sulfur ridges, frozen ranges, faulted Fracture Zones and sunken decay.
Smooth biome borders and a protected gate approach."
