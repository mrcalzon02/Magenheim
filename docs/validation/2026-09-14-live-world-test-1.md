# Live world test 1 - 2026-09-14

## Test source

This record captures direct in-game observations supplied from a disposable Valheim
world using the locally installed Magenheim 0.0.16 package. Screenshots were supplied
for the workstation UI, spawned world geode, inventory/crafting UI, and console.
These observations outrank prior source-only assumptions for the behavior exercised.

## Confirmed working

- The Geologist's Workstation is present in-world and can be used to open its station UI.
- Earth content prefab names are registered sufficiently for direct `spawn` commands.
- Rough, Simple, Crystal, Advanced, and Master Earth crystals and Earth Crystal Shards
  spawned correctly in the observed test.
- The dedicated Meadows Earth world-geode prefab can be spawned and uses the custom
  Magenheim geometry rather than silently falling back to the vanilla source visual.

## Confirmed defects and implementation gaps

### Empty workstation Craft panel

The Geologist's Workstation Craft panel contains no Magenheim mineral recipes or
operations. Source inspection confirms this is not a discovery/material problem:
0.0.16 registers the station and items but does not yet bind geode opening or crystal
refinement inventory transactions to the UI. Static vanilla recipes must not be used
as a fake repair because the authoritative refinement contract includes probabilistic
failure, source destruction, matching shard returns, skill effects, station progression,
and server-authoritative atomic inventory mutation.

### No adaptive socket operation

No workstation action exists for adding sockets to equipment. This matches current
source state: adaptive per-item socket metadata, eligibility discovery, transaction
handling, and persistence are still P2 work. Shared prefab mutation is explicitly not
an acceptable shortcut.

### World geode renders translucent

The custom world geode is visibly translucent. The checked-in geode texture is an
opaque RGB atlas, while `EarthAssets.ReplaceVisual` cloned the source prefab material
without normalizing inherited blend/depth render state. The source repair now forces
Magenheim-owned cloned materials to opaque RenderType, opaque blend factors, ZWrite,
non-alpha keywords, and geometry render queue. This is source-level repaired but not
runtime-accepted until a rebuilt package is observed in Valheim.

### Workstation iron-band surface fighting

Visible fighting/overlap occurs around the iron tabletop banding. Source review of
`tools/generate-workshop-assets.py` shows the current iron-band cuboids physically
intersect the tabletop boards and share outer surface planes in places. This is a real
asset-geometry defect, not merely a lighting issue. It remains open until the generated
workstation mesh and its generator are changed together and the new asset is tested in-game.

### Crystal Shaping console diagnostic

`raiseskill Crystal Shaping 1` was rejected and Valheim printed the normal
`raiseskill [skill] [amount]` syntax. Jotunn registers the permanent custom identifier
`magenheim.crystal_shaping`; the display name contains a space and is split by the
vanilla command parser before Jotunn's custom-skill name resolution. The corrected
diagnostic is:

```text
raiseskill magenheim.crystal_shaping 1
```

This command still requires live confirmation. It is only a diagnostic for the
registered skill and is not evidence that gameplay XP awarding is implemented.

## Acceptance status

The test materially advances the validation boundary: station presence, station UI
opening, direct Earth-item spawn registration, and direct world-geode custom geometry
are now observed. Mineral operations, socketing, geode opacity after repair, workstation
band geometry, skill diagnostic by permanent identifier, natural worldgen/mining,
save/reload, and multiplayer transactions remain unaccepted until separately observed.
