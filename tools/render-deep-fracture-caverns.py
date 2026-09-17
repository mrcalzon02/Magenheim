"""Render player-eye interior views of the Deep Fracture districts.

    blender --background --factory-startup --python tools/render-deep-fracture-caverns.py -- [DF-01 ...]

The catalog contact sheets frame each model from outside, which is useless for a space the
player stands inside: an enclosed cavern just reads as a grey lump. This puts the camera on
the cavern floor at eye height and looks across the chamber, so the vault, the terraced
floor and the formations can actually be judged.

Writes assets/models/previews/cavern-<id>.png.
"""
import bpy
import math
import sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source'
PREVIEW = ROOT / 'assets/models/previews'
PREVIEW.mkdir(exist_ok=True)

EYE_HEIGHT = 1.8


def floor_height_at(floor, x, y):
    """Nearest floor sample, so the camera stands on the ground rather than in it."""
    best, best_distance = 0.0, 1e18
    for vertex in floor.data.vertices:
        point = floor.matrix_world @ vertex.co
        distance = (point.x - x) ** 2 + (point.y - y) ** 2
        if distance < best_distance:
            best, best_distance = point.z, distance
    return best


def render(district_id):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene

    for engine in ('BLENDER_EEVEE_NEXT', 'BLENDER_EEVEE', 'CYCLES'):
        try:
            scene.render.engine = engine
            break
        except TypeError:
            continue
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 900
    scene.render.film_transparent = False

    scene.world = bpy.data.worlds.new('Cave')
    scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value = (0.02, 0.02, 0.028, 1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value = 0.35

    with bpy.data.libraries.load(str(SOURCE / (district_id + '.blend')), link=False) as (src, dst):
        dst.objects = src.objects
    meshes = [o for o in dst.objects if o and o.type == 'MESH']
    for obj in meshes:
        scene.collection.objects.link(obj)
    bpy.context.view_layer.update()

    floor = next((o for o in meshes if o.name == 'CavernFloor'), None)
    if floor is None:
        raise SystemExit('No CavernFloor in ' + district_id)

    # Stand on the floor near one edge and look across the chamber.
    samples = [floor.matrix_world @ v.co for v in floor.data.vertices]
    near = min(samples, key=lambda p: (p.x ** 2 + (p.y + 34.0) ** 2))
    eye = Vector((near.x, near.y, near.z + EYE_HEIGHT))

    # The district's own fissure lights come across with the library load. Aim at one so
    # the shaft is what the preview actually shows, and keep the added lighting to a weak
    # headlamp so the beam is not washed out. Placement must settle before the camera is
    # built, because the camera is oriented from the final eye and aim positions.
    fissures = [o for o in dst.objects if o and o.type == 'LIGHT']
    if fissures:
        target = fissures[0].location
        stand_x, stand_y = target.x, target.y - 40.0
        ground = floor_height_at(floor, stand_x, stand_y)
        eye = Vector((stand_x, stand_y, ground + EYE_HEIGHT))
        # Aim between the floor under the fissure and the opening, so the preview shows
        # the chamber and the shaft together instead of a ceiling study.
        under = floor_height_at(floor, target.x, target.y)
        camera_aim = Vector((target.x, target.y, (under + target.z) * 0.5))
    else:
        camera_aim = Vector((eye.x, eye.y + 40.0, eye.z + 6.0))

    camera_data = bpy.data.cameras.new('Eye')
    camera_data.lens = 20.0          # wide, to convey the volume
    camera_data.clip_end = 400.0
    camera = bpy.data.objects.new('Eye', camera_data)
    camera.location = eye
    camera.rotation_euler = (camera_aim - eye).to_track_quat('-Z', 'Y').to_euler()
    scene.collection.objects.link(camera)
    scene.camera = camera

    lamp_data = bpy.data.lights.new('Headlamp', type='POINT')
    lamp_data.energy = 24000.0
    lamp_data.shadow_soft_size = 3.0
    lamp = bpy.data.objects.new('Headlamp', lamp_data)
    lamp.location = eye + Vector((0.0, 5.0, 1.0))
    scene.collection.objects.link(lamp)

    scene.render.filepath = str(PREVIEW / ('cavern-%s.png' % district_id))
    bpy.ops.render.render(write_still=True)
    print('RENDERED %s -> %s' % (district_id, scene.render.filepath))


def main():
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    for district_id in (args or ['DF-01']):
        render(district_id)


main()
