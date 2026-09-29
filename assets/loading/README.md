# Loading backdrops

The Underworld loading artwork is an **environment promise**, not detached concept art. A backdrop
may stylize composition, atmosphere and the player's viewpoint, but the geography, landmark
families and environmental motifs it depicts must be achievable by the shipped Underworld
generator. If a scene cannot be encountered in play, either world generation must be advanced
until it can or the artwork must be reduced to the implemented visual envelope.

The current ten scene identities are: root caverns, burning roots, sulfurous wastes, frozen
caverns, fungal forest, great decay, ancient ruins, fracture zones, blackwater deep and titanbone
arches. These are scene names, not additional gameplay biomes. Their biome/worldgen mappings and
acceptance boundaries are defined in
`docs/UNDERWORLD_LOADING_SCREEN_WORLDGEN_CONTRACT.md`.

Artwork rules:

- Match vanilla Valheim's restrained loading-art language: a painted vignette that falls into black,
  not a full-screen wallpaper.
- Do not bake the Valheim/Magenheim logo, loading text, tips, spinner, progress bar or other UI into
  the PNG. Runtime owns UI.
- Keep the desaturated charcoal / blue-grey value structure with only restrained local colour.
- Keep at least one Viking as a readable dark foreground or midground silhouette when the scene
  composition permits it.
- Every biome-focused vignette must also show at least one inhabitant or enemy actually associated
  with that environment. It may be distant, obscured or silhouetted, but its body plan must match
  a runtime creature the player can encounter. Do not advertise a finished bespoke monster while
  the game still presents only a tinted/scaled donor body.
- Prefer broad painted masses and low-frequency texture. Do not prompt for "ultra detailed",
  "high-detail" or similar microtexture language that produces crinkled/noisy surfaces.
- Do not depict unsupported volumetric terrain. Heightfield ground may form basins, cliffs, ridges,
  plateaus and spires; free-standing bridges, roots, arches, ceilings and overhangs require actual
  landmark/prop geometry in the runtime.
- A loading scene is not accepted merely because the PNG exists. Its required terrain and landmark
  ingredients need source implementation and then live visual confirmation.

Each PNG is shipped separately under `Magenheim/assets/loading/underworld`, the directory used by
the Underworld loading presenter. It rotates panels every eight seconds while shown and preserves
image aspect ratio. The presenter is Magenheim-owned UI used while the mod constructs its native
Underworld instance and during local Deep Gate transfers; it does not replace Valheim's ordinary
global loading screens.

The original crop workflow remains available for archival/source recovery through
`tools/split-loading-backdrops.ps1 -Source <original image path>`, but those crops are no longer
the visual authority for future replacements.
