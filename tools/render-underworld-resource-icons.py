"""Render one inventory icon per authored Underworld resource from its Blender source.

    tools/blender.ps1 render-underworld-resource-icons [model-id ...]

The resources shipped with their vanilla donors' icons (a Round Log, a Magecap, Flax, a Stone).
Their items now carry authored models, so their icons are rendered from the same `.blend` the
runtime mesh is exported from, the way the weapon and staff icons are. Lighting, size and margin
match render-weapon-icons.py so the inventory reads as one set; the view looks down on the object
at three-quarters because resources lie on the ground rather than being held.
"""
import bpy
import json
import math
import sys
from pathlib import Path

from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
MODELS = ROOT / 'assets/models'
OUT = ROOT / 'assets/earth'
SIZE = 256
SAMPLES = 96


def render(entry, out_path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = SAMPLES
    scene.cycles.use_denoising = True
    scene.render.resolution_x = SIZE
    scene.render.resolution_y = SIZE
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.view_settings.view_transform = 'AgX'
    scene.world = bpy.data.worlds.new('Studio')
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes['Background']
    background.inputs[0].default_value = (.5, .55, .62, 1)
    background.inputs[1].default_value = .55

    with bpy.data.libraries.load(str(MODELS / entry['source']), link=False) as (src, dst):
        dst.objects = src.objects
    objects = [o for o in dst.objects if o and o.type == 'MESH']
    if not objects:
        raise SystemExit(f"{entry['id']}: source .blend contains no mesh objects")
    for obj in objects:
        scene.collection.objects.link(obj)
    # Library-loaded world matrices are stale until the objects enter an evaluated scene.
    bpy.context.view_layer.update()

    view = Matrix.Rotation(math.radians(-50), 4, 'X') @ Matrix.Rotation(math.radians(-30), 4, 'Z')
    transforms = {obj: obj.matrix_world.copy() for obj in objects}
    for obj in objects:
        obj.matrix_world = view @ transforms[obj]
    bpy.context.view_layer.update()
    points = [o.matrix_world @ Vector(c) for o in objects for c in o.bound_box]
    minimum = Vector([min(p[i] for p in points) for i in range(3)])
    maximum = Vector([max(p[i] for p in points) for i in range(3)])
    extent = max((maximum - minimum).x, (maximum - minimum).y)
    if extent <= 0:
        raise SystemExit(f"{entry['id']}: degenerate bounding box")
    placement = Matrix.Scale(1.86 / extent, 4) @ Matrix.Translation(-(minimum + maximum) / 2)
    for obj in objects:
        obj.matrix_world = placement @ obj.matrix_world
    bpy.context.view_layer.update()

    camera_data = bpy.data.cameras.new('Camera')
    camera_data.type = 'ORTHO'
    camera_data.ortho_scale = 2.2
    camera = bpy.data.objects.new('Camera', camera_data)
    scene.collection.objects.link(camera)
    camera.location = (0, 0, 12)
    scene.camera = camera
    for name, pos, power, size in [('Key', (-2.2, -1.4, 6), 900, 6), ('Fill', (2.6, 1.8, 5), 520, 6)]:
        light_data = bpy.data.lights.new(name, 'AREA')
        light_data.energy = power
        light_data.shape = 'DISK'
        light_data.size = size
        light = bpy.data.objects.new(name, light_data)
        scene.collection.objects.link(light)
        light.location = pos
    scene.render.filepath = str(out_path)
    bpy.ops.render.render(write_still=True)


def main():
    catalog = json.loads((MODELS / 'catalog.json').read_text())
    resources = [e for e in catalog if e['id'].startswith('underworld-resource-')]
    if not resources:
        raise SystemExit('No authored Underworld resource models in the catalog.')
    requested = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    if requested:
        resources = [e for e in resources if e['id'] in requested]
    for entry in sorted(resources, key=lambda e: e['id']):
        render(entry, OUT / f"{entry['id']}.icon.png")
        print('RENDERED', entry['id'], flush=True)
    print(f'Rendered {len(resources)} Underworld resource icons at {SIZE}px into {OUT}', flush=True)


main()
