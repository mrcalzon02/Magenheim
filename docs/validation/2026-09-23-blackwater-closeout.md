# Blackwater Deep B2 closeout - 2026-09-23

User authorized proceeding from the visual review candidate `fbf4bca` to wiring, closeout and push.
The interrupted Blackwater export and wiring were already committed in `4b6fac0` when resumed.
Integrated through `213f6fc` before final validation; preserved incoming Underworld location work.

## Delivered

- Seven canopy slots and twelve cover slots map to the 12 authored Blackwater scenery models.
  Landmark indices and shoreline/slope admission remain unchanged. Scale now follows the authored
  metre dimensions; canopy uses authored colliders and cover remains nonblocking.
- Blackwater Flowstone, Pale Fibre, Blackwater Pearl and Deep Salt items/pickups use the four
  resource models, fitted collision and rendered 256px icons. Item identities and behaviour persist.
- Blackwater and Fungal Forest entries own distinct source/GLB/runtime outputs and track both
  shared kits. Fungal Forest was regenerated with UV-coverage atlas padding. Resource icons were
  regenerated afterwards. All generated freshness checks pass.
- Incoming Deep Sigil compilation fixes: `GetHoverOffset()` follows the other Hoverable components;
  private minimap pins are read through existing `RuntimeGameApi.GetMapPins`.

## Executed verification

`closeout.ps1 -Offline` passed, including:

- 367 fresh generated files across 13 generators; 344 model sets with topology, winding, textures,
  hashes and runtime import checks; 142 decoded icons. The four new resource icons were inspected.
- Creature source/texture gates and review renders, scale/orientation/material checks, 17 Rootforged
  bindings, ten held-model alignments and cover habitat contracts.
- 43,508 Core assertions, plus separate definition/worldgen/socket suites; launcher metadata tests.
- Runtime build: zero warnings/errors. The importer test shim emitted existing nullable warnings;
  legacy 128px icon debt remains reported separately from the new 256px icons.
- 42 Harmony patch targets, 33 direct reflection bindings and 42 helper field contracts;
  21 assembly references resolvable. Twelve dynamic bindings still require runtime observation.

## Installed result

Installed release **0.0.102** into the active **Central Fuckery** profile. All installed payload
hashes match the package; launcher entry is enabled and its version/description/dependencies agree.

DLL SHA-256: `7273EE6121E0609F085DEF7973C8E1880B8F7660AC736248FD9A012A100B8D51`.

Launcher description: "0.0.102: Blackwater Deep custom pass - authored mineral columns, rimstone pools, drowned roots, eight ground covers and four resource models with icons replace vanilla visuals. Fixes Blackwater atlas seams."

Backups: `backups/Local-Magenheim-20260923-114453.zip` and
`backups/mods-20260923-114506-180.yml`. Package: `dist/Local-Magenheim-0.0.102.zip`.
Local execution log: `artifacts/blackwater-closeout-20260923.log`.

No game launch or world inspection was performed. Biome appearance, placement, collisions,
resource interactions, multiplayer and save/reload remain live acceptance items. Natural resource
harvesting and recipes were not added by this visual pass. Sulfurous Wastes is the next ecology slice.
