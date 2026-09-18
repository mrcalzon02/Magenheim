"""Render one inventory icon per crystal weapon from the authoritative Blender sources.

    tools/blender.ps1 render-weapon-icons [model-id ...]

The crystal weapons shipped with hand-drawn procedural pixel art from CrystalWeaponIcons.cs, which
described the weapons only as well as somebody could draw them in code and drifted from the models
the moment those were re-authored. The staff family already solved this: render the icon from the
same .blend the runtime mesh is exported from, so it cannot drift again. This is that, for weapons.

Output is 256x256 RGBA with a transparent background. EarthAssets.CreateIconSprite uses a square
texture's own width as pixels-per-unit, so 256 is presentation-neutral against the 128px icons.
"""
import bpy, json, math, sys
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
MODELS = ROOT / 'assets/models'
OUT = ROOT / 'assets/earth'
SIZE = 256
SAMPLES = 96

# A crystal weapon's catalog id is already the asset name its registrar passes to EarthAssets.Icon.
def asset_name(model_id: str) -> str:
    return model_id


def frame_and_render(entry, out_path: Path) -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = SAMPLES
    scene.cycles.use_denoising = True
    scene.render.resolution_x = SIZE
    scene.render.resolution_y = SIZE
    scene.render.resolution_percentage = 100
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

    # Lay the staff along the inventory diagonal the way Valheim presents long weapons,
    # then tip it toward the camera so the head reads as volume rather than silhouette.
    rot = (Matrix.Rotation(math.radians(-32), 4, 'Z')
           @ Matrix.Rotation(math.radians(-68), 4, 'X')
           @ Matrix.Rotation(math.radians(18), 4, 'Y'))
    transforms = {obj: obj.matrix_world.copy() for obj in objects}
    for obj in objects:
        obj.matrix_world = rot @ transforms[obj]
    bpy.context.view_layer.update()

    points = [o.matrix_world @ Vector(c) for o in objects for c in o.bound_box]
    minimum = Vector([min(p[i] for p in points) for i in range(3)])
    maximum = Vector([max(p[i] for p in points) for i in range(3)])
    center = (minimum + maximum) / 2
    extent = max((maximum - minimum).x, (maximum - minimum).y)
    if extent <= 0:
        raise SystemExit(f"{entry['id']}: degenerate bounding box")

    # Leave a small margin so the alpha edge never touches the icon border.
    placement = Matrix.Scale(1.86 / extent, 4) @ Matrix.Translation(-center)
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


def main() -> None:
    catalog = json.loads((MODELS / 'catalog.json').read_text())
    weapons = [e for e in catalog if e['id'].startswith('crystal-weapon-')]
    if len(weapons) != 10:
        raise SystemExit(f'Expected 10 crystal weapon models in the catalog, found {len(weapons)}.')

    requested = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    if requested:
        weapons = [e for e in weapons if e['id'] in requested]

    for entry in sorted(weapons, key=lambda e: e['id']):
        name = asset_name(entry['id'])
        frame_and_render(entry, OUT / f'{name}.icon.png')
        print('RENDERED', name, flush=True)

    print(f'Rendered {len(weapons)} crystal weapon icons at {SIZE}px into {OUT}', flush=True)


main()
