# Meadows Earth Geode — Asset Target

## Purpose

`Magenheim_Geode_Meadows_Earth` is the first intact world geode and inventory resource for the Meadows/Earth vertical slice. Runtime 0.0.10 temporarily clones Valheim's vanilla `Stone` item so the prefab identity can exist before a custom asset bundle is admitted. The clone is a placeholder, not the final visual.

## Modeling target

Build a hand-sized irregular stone nodule that reads as natural geology rather than a cut gemstone. Target approximately 0.35–0.45 m on its longest axis for the dropped world object. Keep the silhouette asymmetric with 7–12 broad fractured planes and several smaller chips rather than a high-frequency sculpt.

A shallow broken seam should cross roughly one third of the visible surface. The seam is not an open cavity: the geode is still intact. Expose only two or three narrow mineral flashes through the seam so the object communicates that something crystalline is enclosed without revealing a finished crystal.

Recommended production target: 300–700 triangles for the inventory/world-drop mesh, one material, one 512×512 texture set. A lower-detail collider should use a simple convex approximation rather than the render mesh.

## Meadows / Earth visual language

Outer shell: weathered grey-brown stone with muted moss staining and soil lodged in depressions. Avoid saturated green; the item must remain readable against Meadows grass.

Earth alignment: internal seam material should use warm ochre, smoky amber, iron-brown, and faint mineral-gold values. Do not make it glow like an enchanted object. At this stage magic should still look geological. A restrained emissive contribution may be added later if runtime tests show the seam disappears under Valheim lighting.

Surface breakup should favor broad Valheim-like painted value groups rather than photorealistic noise. Edge highlights can be hand-painted into the albedo to match the game's readable low-detail material language.

## Texture channels

- Albedo: painted stone, moss traces, dark seam recesses, warm mineral flashes.
- Normal: shallow fracture planes and mineral seam only; do not use dense rock noise.
- Metallic: zero for shell; mineral seam may use a very small value if the selected Valheim shader benefits from it.
- Smoothness: low shell smoothness, moderately higher seam smoothness.
- Emission: default zero or nearly zero; any later emission must remain subtle and Earth-toned.

## Runtime identity

Prefab: `Magenheim_Geode_Meadows_Earth`

Definition id: `magenheim.geode.meadows.earth`

Inventory display name currently generated from authority as `Meadows Earth Geode`.

The custom asset must replace only the placeholder visual source. It must not change the prefab identity, definition id, geode probabilities, elemental weights, inventory transaction rules, or worldgen registration key.

## Acceptance

The final mesh should be recognizable at ordinary ground-loot distance, distinguishable from vanilla Stone without relying on emission, visually plausible as a Meadows geological find, and sufficiently restrained that Rough/Simple/Crystal/Advanced/Master crystals can become progressively more visually magical later.
