"""Give the Deep Fracture creature models an albedo map without touching their geometry.

    blender --background --factory-startup --python tools/texture-creature-models.py -- [model-id ...]

104 creature and creature-visual models carried 1,520 untextured parts, the largest remaining
block of the library texture gap. Their materials lost meaningful names in the model
migration - most are `export-source`, `export-source.001` or `export-source.002` shared
across hundreds of parts - so the material cannot say what a surface is. The part names
survived and can: `body`, `torso`, `leg-l`, `back-plate`, `hook-r`, `prong` against
`crystal`, `core`, `shard`, `spire`, `eye`.

So each object gets a copy of its own material carrying a role-appropriate map. Copying
rather than editing the shared material matters: one `export-source` is used by carapace and
crystal parts alike, and editing it in place would put a crystal facet map on a leg.

Base colour, roughness and metallic are preserved exactly. Those already encode the elemental
tint for all eight variants, and Unity multiplies albedo by base colour, so the map adds
surface without disturbing the tint.

Geometry, UVs, custom properties and object names are untouched; this is a material pass only.

Generated images are packed before saving. An unpacked generated image stores only its
settings in a .blend and exports as its default colour, which shipped every texture in one
authoring session pure black.
"""
import bpy
import math
import random
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source'
TEXTURE_SIZE = 256

CRYSTAL_HINTS = ('crystal', 'core', 'shard', 'spire', 'eye', 'orbit', 'prism',
                 'facet', 'gem', 'glow', 'vein', 'rune')


def role_of(name):
    lowered = name.lower()
    return 'crystal' if any(h in lowered for h in CRYSTAL_HINTS) else 'carapace'


def noise(seed, lattice=16):
    rnd = random.Random(seed)
    grid = [[rnd.random() for _ in range(lattice)] for _ in range(lattice)]

    def sample(u, v, frequency):
        fx, fy = u * frequency, v * frequency
        x0, y0 = int(math.floor(fx)) % lattice, int(math.floor(fy)) % lattice
        x1, y1 = (x0 + 1) % lattice, (y0 + 1) % lattice
        tx, ty = fx - math.floor(fx), fy - math.floor(fy)
        tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
        top = grid[x0][y0] * (1 - tx) + grid[x1][y0] * tx
        bottom = grid[x0][y1] * (1 - tx) + grid[x1][y1] * tx
        return top * (1 - ty) + bottom * ty
    return sample


def sampler(role):
    field = noise(6101 if role == 'crystal' else 6102)

    def sample(u, v):
        n = field(u, v, 7.0) * 0.6 + field(u, v, 19.0) * 0.4
        if role == 'crystal':
            facet = 0.5 + 0.5 * math.sin(u * math.tau * 6.0) * math.cos(v * math.tau * 4.0)
            return 0.55 + 0.32 * facet + 0.13 * n
        # Pitted, plated carapace: broader mottling with a harder speckle on top.
        plate = 0.5 + 0.5 * math.sin(u * math.tau * 3.0 + n * 4.0)
        return 0.42 + 0.30 * n + 0.16 * plate
    return sample


def build_image(role):
    name = 'creature-' + role
    existing = bpy.data.images.get(name)
    if existing:
        return existing
    image = bpy.data.images.new(name, TEXTURE_SIZE, TEXTURE_SIZE)
    take = sampler(role)
    pixels = [0.0] * (TEXTURE_SIZE * TEXTURE_SIZE * 4)
    for y in range(TEXTURE_SIZE):
        v = (y + 0.5) / TEXTURE_SIZE
        for x in range(TEXTURE_SIZE):
            value = min(1.0, max(0.0, take((x + 0.5) / TEXTURE_SIZE, v)))
            i = (y * TEXTURE_SIZE + x) * 4
            pixels[i] = pixels[i + 1] = pixels[i + 2] = value
            pixels[i + 3] = 1.0
    image.pixels = pixels
    image.update()
    image.pack()
    return image


def textured_material(source, role):
    """A copy of `source` carrying the role map, created once per material and role."""
    name = '%s.%s' % (source.name, role)
    existing = bpy.data.materials.get(name)
    if existing:
        return existing
    copy = source.copy()
    copy.name = name
    copy.use_nodes = True
    tree = copy.node_tree
    shader = tree.nodes.get('Principled BSDF')
    if shader is None:
        return source
    for node in list(tree.nodes):
        if node.type == 'TEX_IMAGE':
            return source            # already mapped; leave it alone
    node = tree.nodes.new('ShaderNodeTexImage')
    node.image = build_image(role)
    tree.links.new(node.outputs['Color'], shader.inputs['Base Color'])
    return copy


def process(model_id):
    path = SOURCE / (model_id + '.blend')
    if not path.exists():
        raise SystemExit('No such model source: ' + model_id)
    bpy.ops.wm.open_mainfile(filepath=str(path))
    touched = 0
    for obj in bpy.context.scene.objects:
        if obj.type != 'MESH' or not obj.data.materials:
            continue
        source = obj.data.materials[0]
        if source is None:
            continue
        replacement = textured_material(source, role_of(obj.name))
        if replacement is not source:
            obj.data.materials[0] = replacement
            touched += 1
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(path), compress=True)
    print('TEXTURED %-52s parts=%d' % (model_id, touched))


def main():
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    if args:
        targets = args
    else:
        targets = sorted(p.stem for p in SOURCE.glob('deep-fracture-*.blend')
                         if 'creature' in p.stem or 'visual' in p.stem)
    for model_id in targets:
        process(model_id)


main()
