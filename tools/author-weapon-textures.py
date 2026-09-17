"""Author per-family 512px albedo maps for the crystal weapons and bind them by intent.

    blender --background --python tools/author-weapon-textures.py

refine-retro-textures.py ends by saying its fine grain "is a floor, not a finish: a proper
fix is a per-family unwrap and an authored map". This is that pass for the weapon family.

Two defects are repaired together.

1. The packed map contradicts the material. Weapon material names are spelled
   `magenheim.crystal-weapon.<model>.<intent>.<family>`, and the retro pass assigned the
   family essentially at random: 76 of 79 weapon materials disagreed with their own intent.
   A sword grip was textured as stone, its crystal as metal, its blackmetal as stone, and
   a greatsword's blackmetal as carapace. Five shared 256px maps served all ten weapons.

2. The name misleads the runtime classifier. GeneratedSurfaceTextures.Classify reads the
   material name for keywords and picks a generated surface from the first match, so a
   material ending `.stone` is classified Stone whatever it was meant to be. Rewriting the
   family token to match the intent fixes the fallback as well as the packed map.

Grain stays fine and low-contrast per the retro-texture lesson: these UVs are smart-projected
into many small islands, so any feature larger than an island reads as patchwork across the
seams. Family character comes from tone, tint and the direction of the grain, not from large
shapes.
"""
import bpy
import math
import random
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source'
SIZE = 512

# intent -> (image name, family token the classifier must read, base rgb, contrast, anisotropy)
# The family token is chosen so GeneratedSurfaceTextures.Classify lands on the right kind:
# 'leather' -> Leather, 'timber' -> Timber, 'metal'/'silver' -> Metal, 'crystal' -> Crystal.
FAMILIES = {
    'grip':           ('weapon-leather',  'leather', (0.29, 0.20, 0.14), 0.075, 2.6),
    'grip-timber':    ('weapon-timber',   'timber',  (0.35, 0.26, 0.17), 0.085, 5.5),
    'blackmetal':     ('weapon-blackmetal', 'metal',  (0.20, 0.21, 0.24), 0.060, 1.0),
    'silver':         ('weapon-silver',   'silver',  (0.70, 0.72, 0.76), 0.050, 1.0),
    'crystal':        ('weapon-crystal',  'crystal', (0.52, 0.60, 0.68), 0.070, 1.0),
    'crystal-bright': ('weapon-crystal-bright', 'crystal', (0.72, 0.80, 0.88), 0.060, 1.0),
    'rainbow':        ('weapon-prismatic', 'crystal', (0.62, 0.58, 0.72), 0.080, 1.0),
}


def build_pixels(seed, rgb, contrast, anisotropy):
    """Fine tooth with family character. No feature spans more than a few pixels, so a UV
    island boundary cannot expose a discontinuity."""
    rnd = random.Random(seed)
    pixels = [0.0] * (SIZE * SIZE * 4)
    for y in range(SIZE):
        # anisotropy stretches the grain along one axis: leather weave, wood run.
        fy = y / anisotropy
        for x in range(SIZE):
            fine = math.sin(x * 1.9 + fy * 2.7) * math.cos(x * 2.3 - fy * 1.7)
            speck = rnd.random() - 0.5
            shade = 1.0 + contrast * fine + (contrast * 0.8) * speck
            i = (y * SIZE + x) * 4
            pixels[i] = min(1.0, max(0.0, rgb[0] * shade))
            pixels[i + 1] = min(1.0, max(0.0, rgb[1] * shade))
            pixels[i + 2] = min(1.0, max(0.0, rgb[2] * shade))
            pixels[i + 3] = 1.0
    return pixels


def ensure_image(intent):
    name, _token, rgb, contrast, anisotropy = FAMILIES[intent]
    image = bpy.data.images.get(name)
    if image is None:
        image = bpy.data.images.new(name, width=SIZE, height=SIZE, alpha=False)
    if tuple(image.size) != (SIZE, SIZE):
        image.scale(SIZE, SIZE)
    image.pixels.foreach_set(build_pixels(abs(hash(name)) % (2 ** 31), rgb, contrast, anisotropy))
    image.pack()
    return image


def intent_of(material_name):
    """`magenheim.crystal-weapon.<model>.<intent>[.<n>].<family>` -> intent."""
    tokens = material_name.split('.')
    if len(tokens) < 5:
        return None
    intent = tokens[3]
    return intent if intent in FAMILIES or intent == 'grip' else None


def process(path):
    bpy.ops.wm.open_mainfile(filepath=str(path))
    # Polearms carry a timber haft; the bladed and ranged weapons carry a wrapped leather grip.
    timber_haft = path.stem in {
        'crystal-weapon-axe', 'crystal-weapon-battleaxe',
        'crystal-weapon-mace', 'crystal-weapon-spear', 'crystal-weapon-atgeir',
    }
    changed = []
    for material in bpy.data.materials:
        if not material.name.startswith('magenheim.crystal-weapon.'):
            continue
        intent = intent_of(material.name)
        if intent is None:
            raise SystemExit(f'{path.stem}: cannot read intent from material {material.name}')
        key = 'grip-timber' if (intent == 'grip' and timber_haft) else intent
        image = ensure_image(key)
        _name, token, rgb, _c, _a = FAMILIES[key]

        tokens = material.name.split('.')
        if tokens[-1] != token:
            tokens[-1] = token
            target = '.'.join(tokens)
            # Two materials on one model can share an intent (a greatsword carries two
            # blackmetal parts). Blender would resolve the collision by appending '.001',
            # which breaks the family token. Disambiguate the way the data already does it,
            # with an index between intent and family: blackmetal.2.metal.
            if target != material.name and bpy.data.materials.get(target) is not None:
                index = 2
                while bpy.data.materials.get('.'.join(tokens[:-1] + [str(index), token])) is not None:
                    index += 1
                target = '.'.join(tokens[:-1] + [str(index), token])
            material.name = target

        tree = material.node_tree
        bsdf = tree.nodes.get('Principled BSDF')
        if bsdf is None:
            raise SystemExit(f'{path.stem}: {material.name} has no Principled BSDF')
        node = next((n for n in tree.nodes if n.type == 'TEX_IMAGE'), None)
        if node is None:
            node = tree.nodes.new('ShaderNodeTexImage')
            node.location = (bsdf.location.x - 340, bsdf.location.y)
            tree.links.new(bsdf.inputs['Base Color'], node.outputs['Color'])
        node.image = image
        # Keep the authored base colour meaningful even where the map is not sampled.
        bsdf.inputs['Base Color'].default_value = (rgb[0], rgb[1], rgb[2], 1.0)
        changed.append((material.name, key))

    if not changed:
        raise SystemExit(f'{path.stem}: no Magenheim weapon materials found')
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(path), compress=True)
    print(f'AUTHORED {path.stem}: {len(changed)} materials', flush=True)


sources = sorted(SOURCE.glob('crystal-weapon-*.blend'))
if not sources:
    raise SystemExit('No weapon sources found.')
for source in sources:
    process(source)
print(f'Authored {len(FAMILIES)} weapon surface families at {SIZE}px across {len(sources)} weapons.', flush=True)
