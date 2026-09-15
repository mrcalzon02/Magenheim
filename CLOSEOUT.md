# Development closeout — 0.0.47 testing candidate

Date: 2026-09-14 (local). This is a testing handoff, not full feature completion.
Source on main was 0.0.44; older README (0.0.16) and state records (0.0.38) were stale.

## 0.0.47 startup repair

Fixed ambiguous damage/armor patch targets and an outdated tooltip signature.
All eleven patch declarations pass the new installed-assembly build gate. An
isolated startup registered Crystal Shaping and completed Magenheim bootstrap;
Steam initialization still prevents world acceptance in that isolated test.
See `docs/validation/2026-09-14-0.0.47-startup-repair.md` for evidence and limits.

## Historical 0.0.46 skill visibility follow-up

The active Central Fuckery profile was found loading 0.0.16, whose log confirmed
registration but not a player skill entry. Installed game IL confirms SkillsDialog
uses GetSkillList (instantiated skills only), while GetSkillLevel creates a missing
level-zero entry. The dialog prefix now initializes only the registered Crystal
Shaping skill without raising it or replacing existing XP.

## Implemented fixes in this candidate

- Restored compilation of the full deterministic test suite: missing crystal namespace
  in SocketingTests and nullable alignment projection in the encounter validator.
- Updated the Deep Fracture hover interface to the installed game's float offset API.
- Repaired workstation recipe/panel access through a single cached Harmony reflection
  boundary; current game members are private and refresh requires a boolean argument.
- Replaced private routed-RPC server lookup with public ZNet server-peer lookup.
- Fixed successful refinement nullable output access, geode collider variable shadowing,
  nullable configuration/localization checks and short-circuited stale socket diagnostics.
- Regenerated workstation mesh, OBJ, icon and preview from the already corrected strap
  generator. The generator fix had not reached the shipped geometry.
- Added package/assembly version agreement, guarded package cleanup, an explicit offline
  build option, SHA-256 inventory and updated testing documentation.

## Assets and features still needing implementation

| System | Source evidence | Remaining work |
| --- | --- | --- |
| Deep Fractures | Core expedition/layout/encounter plans, room geometry and dormant traversal exist. Plugin activates room registration only. | Deterministic collision-safe physical corridors, isolated interior assembly/restoration, entry/exit binding, creature/reward realization and full persistence; then enable surface location registration. |
| Wardstone, Passage Stone, Runed Totem, Runic Keelstone | Eleven model IDs in WorldArtifactVisuals have no external source call sites. | Item/build registration, recipes/icons, ownership/network state, ward/travel/totem/ship behaviors and acceptance tests. |
| Crystal Lantern/Brazier, Rune Engraver | Geometry exists in the same unbound model library. | Registration, icons, lighting/fuel or engraving rules, progression and persistence. |
| Seidr ritual focus and three Spirit fetishes | Model geometry only. | Ritual/summon rules, costs, cooldowns, networking, save state, recipes and icons. |
| Eight boss resonance hearts | CapstoneArtifactVisuals contains eight hearts with no external source call sites. | Trophy consumption, progression, actual resonance effects, authoritative transactions, recipes/icons and persistence. |
| Fate Shard, Fate Crystal, Norn Spindle | Three additional capstone models only. | Fate acquisition/intervention rules, costs, authoritative state, recipes/icons and persistence. |
| Art acceptance | Eleven file-backed models validated; many later pieces generate geometry/icons at runtime. | In-game review of silhouettes, hand placement, collisions, wear variants, material opacity, lighting and performance for every family. |

These model libraries are not playable features merely because they compile. The
surface dungeon gate stays closed: enabling intersecting corridor geometry would
violate the project's existing acceptance requirement. Later model-only systems
need their gameplay designs reconciled before implementation; they are excluded
from playable claims for this package.

## Connected features awaiting live acceptance

Workshop opening/refinement and XP; remote sockets/extraction and equipment effects;
eight elemental staff families; weapons; furniture/decor/architecture/banners;
Sentinel and munition recipes; elemental beds, Ice Box, Dais and alchemy all have
runtime bindings. Remaining work is the live matrix in TESTING.md, especially
server/client inventory outcomes, persistence and third-party compatibility.

## Observed validation

- Core harness: 13,974 assertions passed (plus separately reported worldgen/placement/compatibility groups).
- Runtime Release compilation against installed game assemblies and Jotunn 2.30.0:
  zero warnings and errors after repairs.
- All eleven mineral/workshop meshes pass closed/outward surface, finite geometry,
  UV, atlas and transparent-icon checks.
- Online NuGet vulnerability data was unavailable; the candidate uses explicit offline
  build mode and cached dependencies. No online vulnerability audit is claimed.
- No new disposable-world, multiplayer, visual or persistence acceptance is claimed.
  Historical live-world evidence remains limited to 0.0.16.
- An isolated batch startup attempt hit Steamworks initialization errors and did not
  establish BepInEx/plugin registration. Its process was stopped; evidence remains
  under ignored `dist/closeout-smoke-0.0.45/unity.log`.
- Package checksums cover the listed files, only the two owned DLLs, the
  manifest version and byte-for-byte matching source runtime assets. The local
  installer checks this inventory and assembly version before profile writes.

## Testing handoff

Testing closeout now requires live delivery through `closeout.ps1`, including
installation into the active profile and verification of r2modman's separate
`mods.yml` catalog. `release.json` owns the current version's description; the
builder rejects mismatched versions. The installer preserves all other catalog
records, backs up the catalog, and updates our version, description, dependencies,
website and install timestamp only after package files have been verified.

Open r2modman after closeout and confirm **Magenheim v0.0.47 by Local** with the
startup patch repair description. Earlier handoff statements
about leaving the active profile untouched describe the initial packaging pass.

Build output: `dist/Local-Magenheim-0.0.47.zip`. The ZIP includes this inventory,
TESTING.md, README.md, dependencies manifest and per-file checksums. Install into
a disposable mod profile and execute the ordered matrix. Retain the old profile
for comparison. Closeout installs the active mod and launcher metadata; it does
not modify saved worlds.
