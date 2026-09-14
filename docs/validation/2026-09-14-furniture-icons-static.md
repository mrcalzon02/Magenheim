# Furniture Collection Static Validation — 2026-09-14

## Scope

This record covers the geology/crystal furniture slice introduced on `main` and the follow-up Hammer-menu icon repair.

Furniture identities:

- `Magenheim_Furniture_GeodeTable`
- `Magenheim_Furniture_GeodeChair`
- `Magenheim_Furniture_CrystalBench`
- `Magenheim_Furniture_CrystalBed`
- `Magenheim_Furniture_MineralShelf`
- `Magenheim_Furniture_LapidaryCabinet`
- `Magenheim_Furniture_GeoDesk`
- `Magenheim_Furniture_GeodePedestal`
- `Magenheim_Furniture_CrystalDivider`
- `Magenheim_Furniture_CrystalThrone`

## Authoritative source

- `src/Magenheim.Runtime/FurnitureVisuals.cs` owns the original procedural furniture geometry and geology/crystal material language.
- `src/Magenheim.Runtime/FurnitureRegistrar.cs` owns Hammer registration, recipes, fallback source-prefab selection, wear variants, and replacement colliders.
- `src/Magenheim.Runtime/FurnitureIcons.cs` owns ten generated Magenheim-specific build icons. The furniture no longer reuses unrelated vanilla clone-source icons in the Hammer menu.
- `src/Magenheim.Runtime/MagenheimPlugin.cs` registers `FurnitureRegistrar` and advertises source identity `0.0.32`.
- `src/Magenheim.Runtime/Magenheim.Runtime.csproj` matches runtime source identity `0.0.32`.

## Static checks

Observed on remote `main` after commit `a5614353683951ca81d43dff8389dbe020234a92`:

1. all ten furniture definitions have stable Magenheim-owned prefab names;
2. every definition is placed in `Hammer > Furniture` and requires the vanilla workbench;
3. every piece receives original Magenheim geometry instead of retaining the clone-source renderer;
4. non-trigger source colliders are disabled and replaced by model-specific Magenheim colliders while interaction triggers remain available;
5. wear-capable clone sources receive new/worn/weathered variants from the Magenheim visual;
6. every furniture definition requests a unique generated icon through `FurnitureIcons.Icon(definition.ModelId)`;
7. the icon generator has a dedicated case for all ten furniture model IDs and fails closed on an unknown model ID;
8. plugin and project version identities both read `0.0.32`.

## Admission boundary

Status: **static source admitted; runtime validation deferred**.

This cycle did not execute a local Valheim/Jötunn build or disposable-world test. Therefore it does not claim compilation, plugin startup, Hammer-menu rendering, placement, interaction, wear-state, storage, comfort, or multiplayer acceptance.

## Next exact action

Build/install `0.0.32` in the normal Valheim development profile, open a disposable world, verify all ten pieces appear with distinct Magenheim icons under Hammer > Furniture, place every piece, and check representative inherited interactions: chair/throne seating, bed use, cabinet storage, wear variants, collision, and refund behavior. Repair any source-prefab/API/interaction defect at `FurnitureRegistrar.cs` or the corresponding visual authority before expanding the furniture family.
