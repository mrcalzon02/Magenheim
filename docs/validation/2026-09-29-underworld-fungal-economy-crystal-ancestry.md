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
