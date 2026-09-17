# Socketing Moved to the Crystal Enchanting Dais — 2026-09-17

## Reconciled authority

`main` at `574f832`, matching `origin/main`. Raised by user directive: the weapon slotting
interface should live purely on the Crystal Enchanting Dais, because hosting it on the
Geologist's Workstation overwrites that station's standard crafting menu.

## Why the Workstation was the wrong host

The Geologist's Workstation carries the mod's recipe list — 14 recipe/piece configs resolve
to it, including the crystal tiers, the weapon family and all eight staff families. The
socket surface is hosted inside `InventoryGui` and draws over the active crafting panel, so
on a station with a full recipe list the two compete for the same space.

The Crystal Enchanting Dais already carried **no item recipes**. The only things referencing
it were two `PieceConfig` entries (Crystal Bed, Crystalline Ice Box), which gate *Hammer
placement* near the Dais rather than adding anything to its crafting menu. It is registered
with `m_showBasicRecipies = false` and `m_canRepair = false`. It was already a recipe-free
station, which makes it the correct host.

## A terminology correction worth recording

An earlier framing in this session treated "extraction" as though it were a distinct
system needing its own station gate. It is not. `SocketExtractionService` is **socket
removal** — the third socket operation beside `TryAddSlot` and `TryInstall`, surfaced in the
UI as `Extract 1. <Tier> <Element>`. The geode → rough → refinement chain never extracts
anything.

What made this confusing is a real defect in the code, and it is the coupling this change
removes: `SocketExtractionService.RequiredStationId` was set to
`Magenheim_StationUpgrade_FacetingWheel`. The Faceting Wheel is a Geologist's Workstation
upgrade whose actual job is gating **Crystal → Advanced refinement**
(`CrystalRefinementService.cs:103`). One shared constant made a socket operation look like a
refinement step and tied socketing to the wrong station.

## Material implementation

- `Magenheim.Core/Socketing/SocketExtractionService.cs`: `RequiredStationId` is now
  `Magenheim_CrystalEnchantingDais`. The rejection message and the type documentation were
  rewritten to say what the operation actually is. The Faceting Wheel keeps its refinement
  role untouched.
- `CrystalDaisSocketOverlay` (renamed from `SocketWorkstationOverlay`): resolves the Dais,
  and the Faceting Wheel gating on the removal buttons and in `TryExtract` is gone.
- `SocketOperationRpc`: the server-side `TryResolveNearbyMagenheimStation` and the
  client-side `TryGetCurrentMagenheimStation` both resolve the Dais, and the server's
  Faceting Wheel rejection for removal is gone. Client and server authority moved together;
  neither is a client-only trust change.
- Deleted `CrystalEnchantingDaisSocketPatch` and its `PatchAll` registration. It existed
  only to *add* the Dais alongside the Workstation through three Harmony postfixes over
  `TryGetMagenheimStation`, `TryGetCurrentMagenheimStation` and
  `TryResolveNearbyMagenheimStation`. With the Dais primary it was redundant, and the
  behaviour now lives in the authority rather than in a postfix over it. Harmony patch
  targets verified against installed assemblies drop from 25 to 22.

## Coverage

`SocketingTests` now asserts that `RequiredStationId` is the Dais, and loops all four
refinement stations — Geologist's Workstation, Fracturing Block, Faceting Wheel, Resonance
Frame — proving each is refused with `InvalidStation` and leaves per-item metadata
unchanged. The suite moved from 37,120 to 37,124 assertions.

## Gameplay authority consequence

`SocketExtractionService.RequiredStationId` participates in the gameplay authority
fingerprint, so `GameplayAuthorityFingerprint` changes with this build. Peers on an older
build will fail authority admission for persistent socket mutation rather than silently
diverging, which is the designed fail-closed behaviour. Host and clients must run 0.0.56.

## Verification boundary

`build.ps1 -Offline` passes: 37,124 deterministic assertions, 0 warnings, 0 errors, 281
model asset sets, 22 Harmony patch targets, 13 literal plus 42 helper-wrapped reflection
bindings.

**Static validation only. No runtime claim.** Specifically unverified: that the Dais opens
the socket surface in game, that the Workstation's crafting menu is now unobstructed, that
removal works without the Faceting Wheel, and the host/client socket matrix.

## Player-facing consequence

Existing worlds must build a Crystal Enchanting Dais to socket at all. It is built at the
Geologist's Workstation from Stone 20, Iron 8, Crystal 8 and Crystal Dust 4.

## Next actionable slice

The Dais now owns its whole panel, which is what makes the substantial socket-menu
improvements possible. `CrystalDaisSocketOverlay` still lays out a flat vertical list of
buttons built for a cramped shared surface; it can now use the space deliberately.
