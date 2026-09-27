# Loading backdrops

Ten individual, lossless crops of the user's supplied `ChatGPT Image Sep 27, 2026, 12_59_31 PM.png` (1672 x 941). Divider lines are excluded. No repainting, stretching or upscaling is applied.

Reading order, left to right then top to bottom: root caverns, burning roots, sulfurous wastes, frozen caverns, fungal forest, great decay, ancient ruins, fracture zones, blackwater deep, titanbone arches. Names describe the artwork; they do not add new gameplay biomes.

Each PNG is shipped separately under `Magenheim/assets/loading/underworld`, the directory used by the remote Underworld loading presenter. It rotates panels every eight seconds while shown. Aspect ratio is preserved, so the panoramic artwork is letterboxed on narrower displays. Live rendering remains to be confirmed in game.

To reproduce the exact crops, run `tools/split-loading-backdrops.ps1 -Source <original image path>`.
