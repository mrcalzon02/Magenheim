# True Blacksmithing

True Blacksmithing is an optional Magenheim subcomponent that de-abstracts Valheim equipment production into physical workshop processes without replacing the finished vanilla equipment ecosystem.

This directory is the authoritative implementation workspace for the subsystem. Runtime code lives under `src/Magenheim.Runtime/TrueBlacksmithing/`; subsystem-specific planning, data specifications, and future authored assets live here.

## Hard rules

- The subsystem is optional and controlled by `Modules.TrueBlacksmithing.Enabled`.
- The switch is startup-scoped. Changing it requires a restart.
- The fail-safe is **not** configurable.
- Disabled means zero True Blacksmithing recipe mutations.
- A failed activation must leave or restore baseline Valheim/Magenheim crafting.
- Recipe replacement must be reversible and go through the subsystem mutation boundary.
- Additive prefabs may remain registered and inert after a fault; live-session unregistering must not corrupt player inventories or world saves.
- Never delete player items, world pieces, or save data as part of fallback.
- No C# tuple syntax or `System.ValueTuple`.
- No GitHub Actions are required for this subsystem.
- Do not turn development tooling into a permanent factory line. Generate the required production artifacts, commit them, and ship them.

## Scope

The intended final scope covers primitive crafting, woodworking, knapping, leatherworking, textiles, casting, forging, armor forming, heat treatment, grinding, fitting, final assembly, vanilla upgrades, current endgame equipment, and Magenheim-specific compatibility.

The finished object should normally remain the actual vanilla or Magenheim prefab. True Blacksmithing changes **how it is made**, not its combat identity.

See [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) and [FAIL_SAFE_CONTRACT.md](FAIL_SAFE_CONTRACT.md).
