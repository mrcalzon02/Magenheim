# Staff Icon Integrity, Registration Chain and Durability — 2026-09-17

## Reconciled authority

`main` was at `5aa8ab0`, identical to `origin/main`; 67 commits had landed since
`PROJECT_STATE.md` was last written and the source version had moved 0.0.49 -> 0.0.53.
The first action was to run `build.ps1 -Offline`, which P0.1 requires before push.

## Defect 1 — `main` did not compile

Eight compile errors were on `main`, none of which had ever been built:

| File | Error |
|---|---|
| `Magenheim.Core/Underworld/UnderworldVesselCatalog.cs` | `Math.Clamp` is not in netstandard2.0 |
| `FurnitureIcons.cs` | `string.Contains(string, StringComparison)` is not in net462 |
| `UnderworldGeothermalHazardVolume.cs` | CS8604 passing a possibly-null `ZNetView` |
| `UnderworldPhysicalWorldSwitchDriver.cs` (x2) | `UnderworldWorldIdentity` has no `Fingerprint` |
| `UnderworldVesselRegistrar.cs` (x2) | wrong `CustomPrefab` overload; `AddPrefab` returns void |
| `UnderworldWorldSessionLifecycle.cs` | CS8604; net462 does not annotate `IsNullOrWhiteSpace` |

Each was repaired against the idiom already established elsewhere in the codebase rather
than a new one. Committed as `72b0652`.

## Defect 2 — a missing icon silently removed four staff families

`EarthStaffRegistrar` requested `EarthAssets.Icon("staff-earth-<tier>")`. No
`assets/earth/staff-earth-*.icon.png` has ever existed in the repository history.
`EarthAssets.Texture` resolves icons through `File.ReadAllBytes`, so the Earth staff
family threw `FileNotFoundException` during registration.

All eight staff registrars subscribe to `PrefabManager.OnVanillaPrefabsAvailable`. That
is a multicast delegate: the first handler to throw stops every handler subscribed after
it. Bootstrap order is Fire, Frost, Storm, **Earth**, Venom, Radiance, Seidr, Spirit, so
the missing Earth icon also prevented **Venom, Radiance, Seidr and Spirit** from
registering at all — 20 items removed with no log line naming them.

This is the mechanism behind the P0.0 live report "Crystal Staff of Venom does not
attack". The registrar that builds the Venom projectile payloads and binds them to the
staff never ran.

## Defect 3 — three committed staff icons were corrupt

`staff-frost-simple`, `staff-venom-advanced` and `staff-venom-master` carry bad IDAT CRCs
or truncated IDAT chunks in the committed blobs, not merely in the worktree. Frost is
second in the bootstrap order, so `staff-frost-simple.icon.png` would fail decode and
throw `InvalidDataException` even before Earth was reached.

The repository had no `.gitattributes`, so nothing declared these assets binary.

## Material implementation

- `tools/render-staff-icons.py` renders one icon per staff model from the same `.blend`
  the runtime mesh is exported from, so an icon can no longer drift from its model.
  Output is 256x256 RGBA on a transparent film. `EarthAssets.CreateIconSprite` uses a
  square texture's own width as pixels-per-unit, so 256 is presentation-neutral against
  the historical 128px icons.
- All 32 staff icons regenerated: 12 that never existed (Earth, Fire, Storm), 3 corrupt,
  and 17 that predated the C1 staff visual rebuild.
- All eight registrars now resolve `m_icons` from their own asset name. Fire and Storm
  previously set no icon at all and inherited the `StaffIceShards` donor icon; Radiance,
  Seidr and Spirit previously used a tinted generic `crystal` icon rather than their own
  models. `assetName` was added to the Fire and Storm definitions to match the other six.
- `Magenheim.Core/CrystalStaffDurability.cs` adds the pure tier -> service-life rule
  (Simple 150, +75 per tier), with fail-closed tier resolution from canonical staff
  identities. Registered as `CrystalStaffDurabilityTests`, 18 assertions.
- All eight registrars now set `m_useDurability`, `m_maxDurability`,
  `m_durabilityPerLevel`, `m_durabilityDrain` and `m_useDurabilityDrain`. The field set
  was read off the installed `ItemDrop+ItemData+SharedData` by reflection rather than
  assumed. Staves clone `StaffIceShards`, which spends Eitr instead of durability, so the
  clones previously inherited no durability and never degraded.
- `tools/verify-icon-assets.py` is a new build gate that walks every icon's PNG chunk
  stream verifying CRCs and decompressing the pixel data, requires every staff model in
  the catalog to have a matching icon, and resolves every literal `EarthAssets.Icon("x")`
  reference against disk. Wired into `build.ps1` after the model asset gate.
- `.gitattributes` declares `*.png`, `*.blend`, `*.glb`, `*.zip` and `*.dll` binary.

## Gate proof

Run against the pre-repair tree, `verify-icon-assets.py` fails with exactly the three
defects and nothing else:

```
FAIL: icon asset verification
  - staff-frost-simple.icon.png: undecodable (bad CRC on IDAT chunk)
  - staff-venom-master.icon.png: undecodable (truncated IDAT chunk)
  - Magenheim_Staff_Earth_Simple: staff model has no icon at assets/earth/staff-earth-simple.icon.png
```

Against the repaired tree it passes: 44 icons decode as square RGBA >=128px, 32 staff
models carry a matching icon, all literal references resolve.

## Verification boundary

`build.ps1 -Offline` passes end to end: 37,120 deterministic assertions, 0 warnings,
0 errors, 281 model asset sets, 25 Harmony patch targets, and 13 literal plus 42
helper-wrapped reflection bindings resolved against the installed assemblies.

Static validation only. **No runtime claim is made.** The game has not been launched
against this build. Specifically unverified:

- that the Earth, Venom, Radiance, Seidr and Spirit staff families now register;
- that the Venom staff attacks;
- that the new icons read correctly at inventory scale under game lighting;
- that staves lose durability per swing and repair at the workstation;
- held-model orientation, which P0.0 still records as open.

## Residual risk

`m_durabilityDrain` and `m_useDurabilityDrain` are pinned to 0 so a staff loses durability
per attack rather than passively. Vanilla staff donors were not measured for their own
drain values; if live testing shows staves never losing durability, the per-attack path is
the next thing to inspect.

## Next actionable slice

The registration-chain coupling is now a recorded backlog item: one content registrar
throwing still removes every registrar subscribed after it, and the build gate only
prevents the icon trigger. The disposable-world acceptance session (P0.1) is unchanged as
the controlling gate, and should specifically confirm the five previously-dead staff
families.
