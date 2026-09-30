#!/usr/bin/env python3
"""Render Hammer icons for the six owned Underworld biome stations."""
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
MODELS = ROOT / "assets" / "models"
OUT = ROOT / "assets" / "earth"
IDS = (
    "underworld-station-mycelial-bench",
    "underworld-station-tidal-basin",
    "underworld-station-furnace-heart-forge",
    "underworld-station-silence-table",
    "underworld-station-anchor-forge",
    "underworld-station-crown-reliquary",
)

def render(entry):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 96
    scene.cycles.use_denoising = True
    scene.render.resolution_x = 256
    scene.render.resolution_y = 256
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.view_transform = "AgX"

    scene.world = bpy.data.worlds.new("StationStudio")
    scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (.42, .46, .52, 1)
    bg.inputs[1].default_value = .48

    with bpy.data.libraries.load(str(MODELS / entry["source"]), link=False) as (src, dst):
        dst.objects = src.objects
    objects = [obj for obj in dst.objects if obj and obj.type == "MESH"]
    if not objects:
        raise RuntimeError(entry["id"] + ": no mesh objects")
    for obj in objects:
        scene.collection.objects.link(obj)
    bpy.context.view_layer.update()

    rot = (Matrix.Rotation(math.radians(-35), 4, "Z")
           @ Matrix.Rotation(math.radians(-62), 4, "X")
           @ Matrix.Rotation(math.radians(22), 4, "Y"))
    for obj in objects:
        obj.matrix_world = rot @ obj.matrix_world
    bpy.context.view_layer.update()

    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    low = Vector([min(point[i] for point in points) for i in range(3)])
    high = Vector([max(point[i] for point in points) for i in range(3)])
    center = (low + high) * .5
    extent = max((high - low).x, (high - low).y)
    scale = 1.82 / max(extent, .001)
    placement = Matrix.Scale(scale, 4) @ Matrix.Translation(-center)
    for obj in objects:
        obj.matrix_world = placement @ obj.matrix_world

    camera_data = bpy.data.cameras.new("Camera")
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = 2.2
    camera = bpy.data.objects.new("Camera", camera_data)
    scene.collection.objects.link(camera)
    camera.location = (0, 0, 12)
    scene.camera = camera

    for name, position, energy, size in (
        ("Key", (-2.5, -1.6, 6.5), 1050, 5.5),
        ("Fill", (2.8, 1.6, 5.2), 540, 6.0),
        ("Rim", (0, 3.2, 4.8), 420, 4.0),
    ):
        data = bpy.data.lights.new(name, "AREA")
        data.energy = energy
        data.shape = "DISK"
        data.size = size
        light = bpy.data.objects.new(name, data)
        scene.collection.objects.link(light)
        light.location = position

    target = OUT / (entry["id"] + ".icon.png")
    scene.render.filepath = str(target)
    bpy.ops.render.render(write_still=True)
    print("RENDERED", entry["id"], flush=True)

requested = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else list(IDS)
unknown = [model_id for model_id in requested if model_id not in IDS]
if unknown:
    raise SystemExit("Unknown Underworld station icon model(s): " + ", ".join(unknown))
for model_id in requested:
    source = MODELS / "source" / (model_id + ".blend")
    if not source.is_file():
        raise RuntimeError(model_id + ": authored Blender source is missing")
    render({"id": model_id, "source": "source/" + source.name})
