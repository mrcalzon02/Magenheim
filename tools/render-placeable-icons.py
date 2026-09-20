"""Render one Hammer icon per Magenheim placeable from the authoritative Blender sources.

    tools/blender.ps1 render-placeable-icons [model-id ...]

The placeables shipped with procedural pixel art drawn in C# across nine *Icons.cs classes -- the
same defect 0.0.72 fixed for the ten crystal weapons and the staves before them. Live play against
0.0.85 reported the Hammer build menu as rows of grey diamonds that describe nothing, which is the
honest outcome of drawing a crystal foundation with a rectangle primitive. Rendering each icon from
the same .blend the runtime mesh is exported from means the icon cannot drift from the model again.

Output is 256x256 RGBA with a transparent background, identical framing and lighting to the weapon
and staff families. A placeable's catalog id is the asset name its registrar passes to
EarthAssets.Icon, so no name mapping is needed.
"""
import bpy, json, math, sys
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
MODELS = ROOT / 'assets/models'
OUT = ROOT / 'assets/earth'
SIZE = 256
SAMPLES = 96

# Every family whose registrar asks EarthAssets for an icon by model id. The count is asserted so a
# newly authored placeable cannot quietly ship without an icon, and so a renamed one fails loudly.
# crystal-brazier, crystal-lantern and crystal-wardstone are deliberately absent: WorldArtifactVisuals
# names them but nothing registers them as pieces, so nothing ever asks for their icon.
PREFIXES = (
    'architecture-',
    'decor-',
    'furniture-',
    'crystal-banner-',
    'crystal-bed-',
    'crystal-enchanting-dais',
    'crystal-sentinel',
    'crystalline-ice-box',
)
EXPECTED = 62

# A placeable's catalog id is already the asset name its registrar passes to EarthAssets.Icon.
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
    placeables = [e for e in catalog if any(e['id'].startswith(p) for p in PREFIXES)]
    if len(placeables) != EXPECTED:
        raise SystemExit(
            f'Expected {EXPECTED} placeable models in the catalog, found {len(placeables)}. '
            'Add the new model to PREFIXES/EXPECTED deliberately rather than letting the set drift.')

    requested = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    if requested:
        placeables = [e for e in placeables if e['id'] in requested]

    for entry in sorted(placeables, key=lambda e: e['id']):
        name = asset_name(entry['id'])
        frame_and_render(entry, OUT / f'{name}.icon.png')
        print('RENDERED', name, flush=True)

    print(f'Rendered {len(placeables)} placeable icons at {SIZE}px into {OUT}', flush=True)


main()
