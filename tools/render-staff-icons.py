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

Staffs are unusually thin inventory subjects. A restrained Freestyle silhouette is
therefore rendered around the authored mesh. This is icon composition, not replacement
geometry: runtime mesh, proportions, UVs and materials stay untouched, while thin shafts
retain a readable edge after inventory downsampling. Only silhouette/border lines are
selected; internal triangulation is deliberately excluded so texture detail stays clean.

The renderer now measures its own finished alpha silhouette before accepting an icon.
This is intentionally stricter than the general icon compatibility gate: the treatment
exists to improve the staff family's known 6.4%-18.8% bounding-box ink-density debt, so
a render that still lands below 8% is a failed composition rather than a successful PNG.
"""
import bpy, json, math, sys
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
MODELS = ROOT / 'assets/models'
OUT = ROOT / 'assets/earth'
SIZE = 256
SAMPLES = 96
OUTLINE_PX = 1.6
MIN_STAFF_INK_DENSITY = .08
MIN_EDGE_CLEARANCE = .012

# Catalog ids are spelled two ways across the staff families. Icons are always named by
# the asset name the registrars pass to EarthAssets.Icon.
def asset_name(model_id: str) -> str:
    if model_id.startswith('Magenheim_Staff_'):
        _, _, family, tier = model_id.split('_', 3)
        return f'staff-{family.lower()}-{tier.lower()}'
    return model_id


def configure_readability_outline(scene):
    """Configure an icon-only silhouette without modifying loaded source geometry."""
    scene.render.use_freestyle = True
    scene.render.line_thickness = OUTLINE_PX
    settings = bpy.context.view_layer.freestyle_settings
    # A fresh --factory-startup view layer has no lineset until one is added, and even a lineset
    # carried over from the source .blend can have no linestyle datablock attached (Freestyle keeps
    # them as separate IDs) -- either gap surfaces as an AttributeError on style.color below rather
    # than anything self-explanatory, so both are created explicitly instead of assumed present.
    if len(settings.linesets) == 0:
        settings.linesets.new('MagenheimIconSilhouette')
    lineset = settings.linesets[0]
    if lineset.linestyle is None:
        lineset.linestyle = bpy.data.linestyles.new('MagenheimIconSilhouetteStyle')
    lineset.select_silhouette = True
    lineset.select_border = True
    lineset.select_crease = False
    lineset.select_edge_mark = False
    lineset.select_external_contour = True
    lineset.select_material_boundary = False
    lineset.select_ridge_valley = False
    lineset.select_suggestive_contour = False
    style = lineset.linestyle
    style.color = (.012, .016, .022)
    style.alpha = .92
    style.thickness = OUTLINE_PX


def verify_rendered_readability(name: str, path: Path) -> tuple[float, float, float]:
    """Measure the actual saved render, including Freestyle, rather than source bounds.

    Returns frame coverage, ink density inside the visible bounding box, and minimum edge
    clearance. Alpha >= 24/255 matches verify-icon-assets.py so authoring and build gates
    judge the same visible pixels.
    """
    image = bpy.data.images.load(str(path), check_existing=False)
    try:
        width, height = image.size[:]
        if (width, height) != (SIZE, SIZE):
            raise RuntimeError(f'{name}: rendered {width}x{height}, expected {SIZE}x{SIZE}')
        rgba = list(image.pixels)
        threshold = 24.0 / 255.0
        xs=[]; ys=[]
        for y in range(height):
            row=y*width*4
            for x in range(width):
                if rgba[row+x*4+3] >= threshold:
                    xs.append(x); ys.append(y)
        if not xs:
            raise RuntimeError(f'{name}: render is transparent')
        visible=len(xs); box=(max(xs)-min(xs)+1)*(max(ys)-min(ys)+1)
        coverage=visible/(width*height); ink=visible/box
        clearance=min(min(xs)/width,(width-1-max(xs))/width,min(ys)/height,(height-1-max(ys))/height)
        if ink < MIN_STAFF_INK_DENSITY:
            raise RuntimeError(f'{name}: staff ink density {ink:.1%} remains below {MIN_STAFF_INK_DENSITY:.0%}; silhouette treatment is not doing its job')
        if clearance < MIN_EDGE_CLEARANCE:
            raise RuntimeError(f'{name}: edge clearance {clearance:.1%} is below {MIN_EDGE_CLEARANCE:.1%}; outline is cropped')
        return coverage, ink, clearance
    finally:
        bpy.data.images.remove(image)


def frame_and_render(entry, out_path: Path) -> tuple[float, float, float]:
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
    configure_readability_outline(scene)

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

    bpy.context.view_layer.update()
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
    return verify_rendered_readability(asset_name(entry['id']), out_path)


def main() -> None:
    catalog = json.loads((MODELS / 'catalog.json').read_text())
    staff = [e for e in catalog if 'staff' in e['id'].lower()]
    if len(staff) != 32:
        raise SystemExit(f'Expected 32 staff models in the catalog, found {len(staff)}.')

    requested = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    if requested:
        staff = [e for e in staff if asset_name(e['id']) in requested or e['id'] in requested]

    results=[]
    for entry in sorted(staff, key=lambda e: asset_name(e['id'])):
        name = asset_name(entry['id'])
        target = OUT / f'{name}.icon.png'
        coverage,ink,clearance=frame_and_render(entry,target)
        results.append((name,coverage,ink,clearance))
        print(f'RENDERED {name}: coverage={coverage:.1%} ink={ink:.1%} edge={clearance:.1%}', flush=True)

    worst=min(results,key=lambda r:r[2]) if results else None
    if worst:
        print(f'READABILITY FLOOR {worst[0]}: ink={worst[2]:.1%}, edge={worst[3]:.1%}',flush=True)
    print(f'Rendered and readability-gated {len(staff)} staff icons at {SIZE}px into {OUT}', flush=True)


main()
