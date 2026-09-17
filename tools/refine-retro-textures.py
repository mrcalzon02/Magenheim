"""Replace the retro-fitted albedo maps with fine grain that island UVs cannot break up.

    blender --background --factory-startup --python tools/refine-retro-textures.py

Closing the texture gap applied generated maps to 155 models that had none. It regressed the
crystal buildables badly: reported from play as "texturally broken", and the screenshot shows
blotchy camouflage patchwork across the Crystal Beds.

The cause is UV layout, not the map. These models were unwrapped with smart projection, so
their UVs are many small packed islands rather than one continuous surface. A map with
large-scale structure - plate bands, brushed streaks, broad mottling - is sampled across
island boundaries, and neighbouring surface patches land on unrelated parts of the image. The
result reads as dirt blotches. Models with purpose-authored UVs (geode, crystal tiers,
caverns, passages) are unaffected, because their UVs were built for their maps.

The tonal-range gate cannot catch this: the map does have range. Coverage percentage was the
wrong measure of success.

So the retro maps become fine, low-contrast grain. High-frequency detail below the scale of an
island reads as surface tooth wherever it is sampled, and carries no large feature that can
land discontinuously. It is a floor, not a finish: a proper fix is a per-family unwrap and an
authored map, which is a much larger job.

Only images created by the retro pass are touched, identified by name. Geometry, UVs,
materials and object names are untouched.
"""
import bpy
import math
import random
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source'
PREFIX = 'creature-'          # the retro pass names every image it makes this way


def grain(seed, size):
    """Fine, low-contrast tooth. No feature is larger than a few pixels, so no UV island
    boundary can expose a discontinuity."""
    rnd = random.Random(seed)
    pixels = [0.0] * (size * size * 4)
    for y in range(size):
        for x in range(size):
            # Two interleaved high-frequency terms plus a little noise, centred near mid grey.
            fine = 0.5 + 0.5 * math.sin(x * 1.9 + y * 2.7) * math.cos(x * 2.3 - y * 1.7)
            value = 0.58 + 0.055 * (fine - 0.5) * 2.0 + 0.045 * (rnd.random() - 0.5) * 2.0
            value = min(1.0, max(0.0, value))
            i = (y * size + x) * 4
            pixels[i] = pixels[i + 1] = pixels[i + 2] = value
            pixels[i + 3] = 1.0
    return pixels


def process(path):
    bpy.ops.wm.open_mainfile(filepath=str(path))
    touched = []
    for image in bpy.data.images:
        if not image.name.startswith(PREFIX):
            continue
        width, height = image.size
        if width != height or width == 0:
            continue
        image.pixels = grain(abs(hash(image.name)) % 99991, width)
        image.update()
        image.pack()
        touched.append(image.name)
    if not touched:
        return False
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(path), compress=True)
    print('REFINED %-54s maps=%d' % (path.stem, len(touched)))
    return True


def main():
    count = 0
    for source in sorted(SOURCE.glob('*.blend')):
        if process(source):
            count += 1
    print('REFINED_TOTAL %d' % count)


main()
