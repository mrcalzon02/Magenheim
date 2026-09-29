#!/usr/bin/env python3
"""Render Rootwarren room/passage acceptance views after the Blender forge."""
import bpy
import math
from pathlib import Path
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets/models/source"
OUT = ROOT / "dist/underworld-production-review/rootwarren-renders"
OUT.mkdir(parents=True, exist_ok=True)

PREFIX = "underworld-dungeon-fungal-rootwarren-"
SUFFIXES = [
    "fracture-mouth","mycelial-gallery","glowcap-vault","spore-basin","root-bridge",
    "sunken-nursery","tangle-junction","shelf-drop","amber-grotto","worldroot-hollow",
    "crawler-nest","stalker-den","puffback-graze","buried-archway","root-squeeze",
    "heartcap-sanctum","passage",
]


def load(model_id):
    path = SOURCE / (model_id + ".blend")
    if not path.is_file():
        raise RuntimeError("Missing Rootwarren Blender source: " + str(path))
    with bpy.data.libraries.load(str(path), link=False) as (src, dst):
        dst.objects = src.objects
    objects = [obj for obj in dst.objects if obj]
    for obj in objects:
        bpy.context.scene.collection.objects.link(obj)
    meshes = [obj for obj in objects if obj.type == "MESH"]
    if not meshes:
        raise RuntimeError(model_id + ": no meshes for review")
    return objects, meshes


def bounds(meshes):
    bpy.context.view_layer.update()
    points = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
    lo = Vector([min(point[i] for point in points) for i in range(3)])
    hi = Vector([max(point[i] for point in points) for i in range(3)])
    return lo, hi


def light(name, position, energy, size, color):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    data.color = color
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = position


def prepare_scene():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 480
    scene.render.resolution_y = 360
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "AgX"
    scene.world = bpy.data.worlds.new("RootwarrenReview")
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes["Background"]
    background.inputs[0].default_value = (.028, .055, .045, 1)
    background.inputs[1].default_value = .34
    light("FungalKey", (-3.8, -4.2, 8.0), 1250, 6.0, (.38, .86, .58))
    light("FungalFill", (4.8, 2.4, 5.2), 620, 7.5, (.40, .62, .92))
    light("AmberRim", (0.0, 5.0, 3.5), 360, 5.0, (.95, .55, .16))


def camera(ortho_scale, location=(0,0,12)):
    data = bpy.data.cameras.new("Camera")
    data.type = "ORTHO"
    data.ortho_scale = ortho_scale
    obj = bpy.data.objects.new("Camera", data)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = location
    bpy.context.scene.camera = obj
    return obj


def render_view(model_id, mode):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    prepare_scene()
    objects, meshes = load(model_id)

    if mode == "context":
        rotation = (
            Matrix.Rotation(math.radians(-30), 4, "Z")
            @ Matrix.Rotation(math.radians(-58), 4, "X")
            @ Matrix.Rotation(math.radians(8), 4, "Y")
        )
        for obj in objects:
            obj.matrix_world = rotation @ obj.matrix_world
        lo, hi = bounds(meshes)
        center = (lo + hi) * .5
        horizontal = max((hi-lo).x, (hi-lo).y)
        vertical = (hi-lo).z
        extent = max(horizontal, vertical * .78, .001)
        scale = 2.0 / extent
        placement = Matrix.Scale(scale, 4) @ Matrix.Translation(-center)
        for obj in objects:
            obj.matrix_world = placement @ obj.matrix_world
        camera(2.35)
    elif mode == "top":
        lo, hi = bounds(meshes)
        center = (lo + hi) * .5
        extent = max((hi-lo).x, (hi-lo).y, .001)
        scale = 2.0 / extent
        placement = Matrix.Scale(scale, 4) @ Matrix.Translation(-center)
        for obj in objects:
            obj.matrix_world = placement @ obj.matrix_world
        cam = camera(2.35, (0,0,12))
        cam.rotation_euler = (0,0,0)
    else:
        raise RuntimeError("Unknown Rootwarren review mode " + mode)

    target = OUT / f"{model_id}--{mode}.png"
    bpy.context.scene.render.filepath = str(target)
    bpy.ops.render.render(write_still=True)
    print("RENDERED", model_id, mode, flush=True)


for suffix in SUFFIXES:
    model_id = PREFIX + suffix
    for mode in ("context", "top"):
        render_view(model_id, mode)

print("RENDERED Rootwarren review", len(SUFFIXES), "models x 2 views", flush=True)
