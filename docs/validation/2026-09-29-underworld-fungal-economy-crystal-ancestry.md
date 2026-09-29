# Underworld Fungal economy seam and crystal-weapon ancestry — 2026-09-29

## Intent

Turn the first Underworld parity tier from a resource-population demonstration into the beginning of an actual economy, while enforcing the new progression rule that Underworld weapons extend Magenheim's existing crystal weapon language rather than bypassing it.

## Implemented source

- Added a twelve-entry Underworld weapon-upgrade catalog covering the weapon identities already named by the six biome parity tiers.
- Every entry has an explicit Magenheim crystal-grade base prefab. Physical weapons descend from the matching Crystal Sword, Greatsword, Axe, Mace, Spear, Atgeir, Bow or Crossbow. Icebind Staff descends from the Crystal Staff of Frost.
- Only the Fungal Forest pair is runtime-admitted in this slice: Worldroot Club consumes the Crystal Mace; Worldroot Bow consumes the Crystal Bow.
- Added the Mycelial Bench as the first Underworld crafting station. It is donor-backed for native workbench behavior in this slice, but its construction cost is real Worldroot Timber, Understone and Spire Fibre.
- Worldroot weapon prefabs clone their crystal chassis, preserving attack behavior, held alignment and the existing authored silhouette. A non-destructive runtime material accent makes the inherited chassis visibly read as a Worldroot/Fungal reworking. Authored derivative geometry remains the next asset pass; no runtime mesh generation was introduced.
- Rootforged construction now resolves Worldroot Timber and Understone to the registered Underworld resource prefabs instead of the temporary RoundLog/Stone substitutions. Iron remains vanilla Iron where the existing definitions require it.
- Fungal provision recipes now consume Glowcap Flesh from the Underworld resource catalog instead of MushroomMagecap. Their item donor remains unchanged.
- Added deterministic Core tests that reject vanilla weapon ancestry, missing Underworld material dependencies, duplicate weapon identities and incorrect crystal-chassis mappings.

## Dependency shape

Surface progression -> Magenheim crystal weapon -> enter Underworld -> gather Fungal Forest resources -> build Mycelial Bench -> add Worldroot/Spire/Understone materials to the existing crystal chassis -> Worldroot weapon.

That same shape is now catalogued for Blackwater, Sulfurous Wastes, Frozen Caverns, Fracture Zones and Great Decay, but those later weapons are not runtime-registered until their own stations and refining loops exist.

## Validation status

Source admission is implemented and committed. A full local Runtime compile and live Valheim acceptance cannot be claimed from the GitHub connector environment. The new code deliberately avoids System.ValueTuple and string.Contains overloads unavailable under the Runtime net462 target. Live acceptance still needs to confirm Mycelial Bench placement, custom-resource requirement resolution, recipe consumption of the base crystal weapon, inherited held visuals, accent readability, repair behavior, multiplayer and save/reload.


## Crystal-model derivation pipeline

The asset boundary now records model ancestry separately from prefab ancestry. Every planned weapon
has a unique `underworld-weapon-*` model identity and names the Crystal model it is designed to
develop. The first authoring tool, `tools/author-underworld-weapons.py`, opens the committed
`crystal-weapon-mace.blend` and `crystal-weapon-bow.blend` directly, keeps their original parts,
grip origin and envelope, and adds Worldroot bindings, structural root ribs and fungal crystal
accents around them. It does not rebuild a vaguely similar club or bow from primitives and call it
related after the fact.

`tools/rebuild-underworld-weapons.ps1` is the bounded local production path: author the two
derivative .blend files, export them through the normal model exporter, rebuild the model catalog,
render icons from those exact sources, and run the icon gate. Runtime checks for the exported model
and icon and automatically graduates from the transitional tinted Crystal clone to the authored
derivative when those files are present.

Held-item alignment now has an explicit derivative-to-Crystal profile map. This matters for later
Greatsword, Spear, Atgeir and Crossbow upgrades: their Crystal ancestors already carry field-tested
trim/forward-axis corrections, and adding biome plating must not discard that knowledge.

The temporary accent path also received a correctness repair: material classification is by the
final family suffix. The previous substring test for `.crystal` matched the namespace
`magenheim.crystal-weapon` on every material, which would have tinted timber, leather and metal as
crystal. The suffix gate distinguishes those families correctly.

The author/export command itself still requires the project's local Blender environment; no
generated .blend/GLB/runtime JSON/icon is claimed by this connector-only commit.


## First refining rung

The Fungal Forest dependency tree now includes its planned intermediate materials rather than
jumping from pickups straight to equipment. The Mycelial Bench produces Worldroot Planks (from
Worldroot Timber), Spire Cord (from Spire Fibre), and Cured Glowcap (from Glowcap Flesh plus Spire
Fibre). Worldroot Club and Worldroot Bow consume these refinements as well as their Crystal chassis.
That makes the first equipment loop structurally equivalent to the parity target:
gather -> establish station -> refine -> upgrade prior weapon.

Rootforged construction deliberately continues to consume raw Worldroot Timber/Understone and
fungal food deliberately continues to consume raw Glowcap Flesh, so the ecology has multiple sinks
instead of every pickup existing only as weapon currency.


## Rootforged station gate closed

The first-tier construction set was still carrying one surface-world bypass after its material
bindings were repaired: the architecture authority named Workbench or Stonecutter as the station on
all seventeen pieces. That contradicted the Fungal parity rule that the Mycelial Bench is the
Underworld's first station and gates the tier.

The Core architecture catalog and shipped `foundation.json` now name
`Magenheim_Underworld_Station_MycelialBench` on every Rootforged/Understone piece. The station
identity is shared by the architecture and weapon catalogs instead of duplicated. Runtime bootstrap
subscription order is resources -> Mycelial Bench -> later Rootforged registration, so the custom
station exists before Jotunn resolves piece requirements. Architecture tests now reject any
first-tier piece that falls back to a surface station.
