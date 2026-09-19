"""Render one inventory icon per staff model from the authoritative Blender sources.

Run with Blender:  blender --background --python tools/render-staff-icons.py

The staff icons previously shipped were authored by hand before the C1 staff visual
rebuild, so they no longer described the models they label; four families had no icon
at all and three of the committed PNGs were corrupt. This renders every staff icon from
the same .blend the runtime mesh is exported from, so the icon cannot drift from the
model again.

Output is 256x256 RGBA with a transparent background. EarthAssets.CreateIconSprite uses
a square texture's own width as pixels-per-unit, so 256 is presentation-neutral against
the historical 128px icons rather than twice the size in the UI.

Staffs are unusually thin inventory subjects. The renderer therefore uses a modest
icon-only silhouette support pass: a back-facing duplicate of each mesh is expanded
along vertex normals and rendered dark behind the authored surface. This is an outline,
not replacement geometry; the source model, runtime mesh, UVs and materials remain
untouched. It gives sub-pixel shafts enough weight to survive inventory downsampling
without lying about the staff's proportions or cropping it to satisfy an alpha metric.
"""
import bpy, json, math, sys
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
MODELS = ROOT / 'assets/models'
OUT = ROOT / 'assets/earth'
SIZE = 256
SAMPLES = 96
OUTLINE_WORLD = 0.012

# Catalog ids are spelled two ways across the staff families. Icons are always named by
# the asset name the registrars pass to EarthAssets.Icon.
def asset_name(model_id: str) -> str:
    if model_id.startswith('Magenheim_Staff_'):
        _, _, family, tier = model_id.split('_', 3)
        return f'staff-{family.lower()}-{tier.lower()}'
    return model_id


def add_readability_outline(scene, objects):
    """Add non-destructive icon-only back shells around the authored staff geometry."""
    mat = bpy.data.materials.new('InventoryReadabilityOutline')
    mat.diffuse_color = (.012, .016, .022, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    if bsdf:
        bsdf.inputs['Base Color'].default_value = (.012, .016, .022, 1)
        bsdf.inputs['Roughness'].default_value = .88

    shells = []
    for source in objects:
        shell = source.copy()
        shell.data = source.data.copy()
        shell.name = source.name + '_IconOutline'
        scene.collection.objects.link(shell)
        # Solidify expands around the true authored surface rather than scaling about an
        # arbitrary object origin. Flip normals + front-face culling leaves only the dark
        # rim outside the original mesh, so it cannot cover authored texture detail.
        solid = shell.modifiers.new('InventoryOutlineWidth', 'SOLIDIFY')
        solid.thickness = OUTLINE_WORLD
        solid.offset = 1.0
        for slot in shell.material_slots:
            slot.material = mat
        shell.data.materials.clear()
        shell.data.materials.append(mat)
        shells.append(shell)
    return shells


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

    # Leave a small margin so the alpha edge never touches the icon border. Frame from
    # authored geometry first; the support rim consumes margin rather than shrinking the staff.
    placement = Matrix.Scale(1.86 / extent, 4) @ Matrix.Translation(-center)
    for obj in objects:
        obj.matrix_world = placement @ obj.matrix_world
    bpy.context.view_layer.update()
    add_readability_outline(scene, objects)
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
    staff = [e for e in catalog if 'staff' in e['id'].lower()]
    if len(staff) != 32:
        raise SystemExit(f'Expected 32 staff models in the catalog, found {len(staff)}.')

    requested = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    if requested:
        staff = [e for e in staff if asset_name(e['id']) in requested or e['id'] in requested]

    for entry in sorted(staff, key=lambda e: asset_name(e['id'])):
        name = asset_name(entry['id'])
        target = OUT / f'{name}.icon.png'
        frame_and_render(entry, target)
        print('RENDERED', name, flush=True)

    print(f'Rendered {len(staff)} staff icons at {SIZE}px into {OUT}', flush=True)


main()
