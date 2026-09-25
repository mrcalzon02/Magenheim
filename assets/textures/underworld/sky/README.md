# Shared cavern roof artwork

`lava-fungal-roof-v2.png`: 1774 x 887 RGB equirectangular PNG, generated using the built-in
image_gen tool and copied unchanged into this repository on 2026-09-23. No external runtime
URL or image service is used. This is a shared skybox for all biomes, not physical ceiling art.

SHA-256: `648997be0e4935d8d7b825815f5a1b1989c5ed6b30d0dd5328f3bc5b244147cb`.

## Generation prompt

Create a production game skybox environment texture for a subterranean fantasy realm, wide 2:1 equirectangular panorama (360 degrees horizontal by 180 degrees vertical), highest available resolution. This is the interior of an impossibly vast dark cavern, looking in every direction toward its distant overhead roof. Broad deeply shadowed charcoal basalt rock masses across the upper hemisphere; irregular thin branching veins of molten orange-red lava embedded in cracks, bright only in their tiny cores, not broad rivers. Tiny scattered turquoise, muted cyan and pale green bioluminescent fungal colonies dotted across the rock roof like stars, with a few loosely connected constellations of organic pinpricks. Make these read as distant luminous fungus attached to stone, not actual astronomical stars. Vast scale, restrained emission, detailed weathered angular rock, dark and atmospheric, legible subtle texture rather than featureless black. Lower hemisphere fades into nearly black bluish charcoal cavern haze; no terrain foreground, no horizon mountains, no buildings, no people. Absolutely no sun, no moon, no daylight sky, no blue daytime sky, no planetary bodies, no text, no labels, no borders. Horizontally wrap seamlessly, with matching left and right edges. Preserve correct equirectangular polar distortion. Upper pole is continuous rock with lava fractures and fungus, lower pole is smooth near-black haze. Texture only, not a rendered scene mockup or a cubemap cross layout. Painterly physically grounded low-fantasy art consistent with Valheim, avoid glossy sci-fi/space-nebula aesthetics.

## Validation

`python tools/verify-underworld-sky.py` checks aspect/resolution, useful tonal variation,
localized lava/fungal colour, limited bright coverage, dark lower hemisphere and horizontal wrap.
`tools/blender.ps1 render-underworld-sky-review` projects the packaged texture upward, toward the
horizon and around the wrap seam at day/night exposure. These are offline projection reviews,
not screenshots from Valheim. The first projection exposed zenith pinching; v2 corrects the polar band.
The initial `lava-fungal-roof.png` is retained as source history and is not packaged. No live appearance, shader availability or haze opacity is claimed.

## Targeted edit prompt (built-in image_gen)

Edit this 2:1 equirectangular cavern skybox texture to correct only its north-pole projection.
Preserve the dark basalt roof, thin orange lava fissures, small cyan/green fungal star colonies,
dark lower haze, overall brightness, panorama proportions and left/right wrap. The current upper
edge produces radial streaks when viewed straight upward in a spherical skybox. In the uppermost
12% of the image, deliberately stretch the rock detail horizontally in the correct equirectangular
polar manner, progressively reducing horizontal contrast toward the top edge. The entire topmost
row must converge to one uniform very dark charcoal colour, so it maps to one continuous pole.
Do not put detailed fissures, fungus specks, vertical streaks or a bright feature touching the
topmost edge. Blend the corrected upper band seamlessly into the unchanged detailed basalt below,
keeping a naturally dark rock recess overhead rather than a visible circle or stripe. Retain the
existing lower 88% as closely as possible. No sun, moon, stars in outer space, text or labels.
Output the corrected complete 360x180 equirectangular panorama, not a perspective render.

## Emission material data

`lava-fungal-roof-emission.png` is a pixel-aligned grayscale material mask extracted from the
final artwork's warm lava/cool fungal chroma and bright cores. The generator does not alter the
colour panorama. It is owned by the `underworld-sky-emission` freshness entry and rebuilt with
`python tools/verify-generated-freshness.py --update underworld-sky-emission`.

At load, runtime combines the colour and mask into one linear HDR panorama: base RGB plus
normalized RGB times mask times 4. This keeps roughly 1% of the roof emissive and leaves rock/haze
dark. Unity's native panoramic sky shader then applies the smooth clock-driven exposure. The
roof glow is appearance; existing dim directional/ambient environment light illuminates terrain.
No fake specular highlights, per-star lights or per-frame panorama recomposition are introduced.
Projection review uses the same composition and both day/night exposures.

## 0.0.105 live-test correction

0.0.104 failed in game: Skybox/Panoramic was listed by name but stripped from the player.
The HDR panorama is now mapped to a camera-centred background sphere with no collider, using
Valheim's actual Custom/Particle (Unlit) shader loaded through SoftReferenceableAssets. Its ID
and asset path are verified against the installed manifest. Fog, soft-particle fading and
sky masking are disabled for this background material; opaque blending and ordinary depth
comparison let terrain cover it. Vertex colour carries the same day/night exposure. The Surface
sky is restored on exit. The artwork and emission mask are unchanged. This adapter still needs
live rendering acceptance; the prior offline projection images do not prove the runtime repair.
