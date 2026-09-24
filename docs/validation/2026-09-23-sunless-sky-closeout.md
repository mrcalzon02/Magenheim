# Sunless sky and haze-line terrain - 0.0.104

Date: 2026-09-23. Testing closeout; no live world acceptance claimed.

## Delivered behavior

Shared basalt roof panorama with localized lava/fungal emission. The independent grayscale
mask covers 0.968% of the roof and is composed once into a linear HDR texture. Native panoramic
rendering removes the visible sun; dim native directional/ambient lighting illuminates the world
using Valheim's existing day fraction. The roof does not physically cast light from its pixels.
Native cloud geometry is reused for haze at native height 4800m; faceted spires/plateaus use
absolute heights up to 5100m. No new physical roof, flight cap or build-height cap was added.

Installed Character.InInterior(Vector3) inspection found the native engine-Y > 3000 dungeon
shortcut. The patch bypasses it only within the active admitted Underworld instance, whose engine
adapter offset is 12000m. Surface and ordinary dungeon classification remain unchanged.

## Verification

Full `closeout.ps1 -Offline` passed; log: artifacts/sky-emission-closeout-20260923.log.
45,220 Core assertions plus separately reported suites; runtime zero warnings/errors.
344 model sets, 142 icons, 368 generated files across 14 generators.
43 Harmony targets, 33 literal reflection bindings, 42 helper fields and 21 assembly references
verified against installed game libraries; 12 dynamic reflection cases are not statically checked.
Sky checks: 1774x887 panorama, 8329 lava pixels, 14911 fungal pixels, 0.059% bright pixels,
wrap difference 2.90/255; emission dimensions, localized coverage and dark lower hemisphere pass.
Six offline sky projections reviewed across upward/horizon/wrap and day/night exposures;
Core-derived spire/plateau detail and horizon renders also reviewed. Images are under
artifacts/review/sky and artifacts/review/terrain, not game screenshots.

## Installation

Central Fuckery profile: enabled Magenheim 0.0.104; installed file hashes and launcher catalog
verified. Launcher UI and game startup were not observed.
Runtime DLL SHA-256: 1E48D30F1C16C20A96A9E77D6906CF2680DDA8133E77DE654F45D324C3E8DD58.
Core DLL SHA-256: C02EEC2ECE98F39D3EE5CB72473799704FC1E1DCEC827ED3A9664AB7356623A7.
Initial backups: backups/Local-Magenheim-20260923-174015.zip and
backups/mods-20260923-174031-452.yml. A documentation-only package refresh follows in
artifacts/sky-closeout-doc-refresh-20260923.log; the tested binaries are unchanged.

Launcher description: "0.0.104: Sunless lava-and-fungal cavern sky with masked glow and dim day/night lighting. Overhead haze at 4.8 km; sharp terrain capped at 5.1 km. Fixes the native dungeon-height restriction for Underworld building."

## Developer testing and remaining acceptance

Command registration confirmed in UnderworldWorldSessionLifecycle.Configure; enter/return use
UnderworldGateTransitRuntime and native Player.TeleportTo. Launch with -console, load a world,
press F5, enable devcommands, then use magenheim_underworld enter, return or status. Entry skips
the boss unlock. Return uses the entry anchor, with bed/home fallback after relog. A live round
trip is still unverified. Wait for a transfer to finish before issuing another command.

Check native shader loading, haze opacity/position, night visibility, cliff silhouettes/collision,
terrain seams, landmark placement, Surface sky/cloud restoration, multiplayer and persistence.
No acceptance of these runtime behaviors is inferred from compilation or offline renders.
Six pre-existing Earth icons remain below the advisory 256px target; the existing build gate
reports them without enforcement. Online dependency audit was unavailable during offline build.

Artwork, exact image_gen generation/edit prompts and provenance are in
assets/textures/underworld/sky/README.md. Source v1 is retained; only v2 plus its mask are packaged.
Next ecology authoring slice remains Sulfurous Wastes after live terrain/sky review.
