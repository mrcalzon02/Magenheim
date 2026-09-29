#!/usr/bin/env python3
"""Derive Underworld weapons from the existing Crystal weapon Blender sources.

This tool deliberately opens the authored crystal .blend as the parent, preserves every original
part, grip origin and weapon envelope, then adds biome material around that chassis. It never
reconstructs the Crystal weapon from scratch.

Current admitted slice:
  underworld-weapon-worldroot-club <- crystal-weapon-mace
  underworld-weapon-worldroot-bow  <- crystal-weapon-bow

Run through tools/blender.ps1:
  tools/blender.ps1 author-underworld-weapons [model-id ...]
"""
import json
import math
import sys
from pathlib import Path
from underworld_material_library import bind_underworld_material

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets" / "models" / "source"

SPECS = {
    "underworld-weapon-worldroot-club": ("crystal-weapon-mace", "club"),
    "underworld-weapon-worldroot-bow": ("crystal-weapon-bow", "bow"),
}


def material(name, colour, metallic=0.0, roughness=0.72, emission=None):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*colour, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission is not None:
        bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
        bsdf.inputs["Emission Strength"].default_value = 0.22
    bind_underworld_material(bpy, mat, name)
    return mat


def finish_object(obj, path, mat):
    obj.name = path
    obj["game_node_path"] = path
    obj["game_collision"] = False
    obj["game_crystal"] = json.dumps(None)
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    if not obj.data.uv_layers:
        obj.data.uv_layers.new(name="UVMap")
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        # Purposefully not smart-project: the project forbids packed smart projection for
        # generated surface maps. Box projection is stable for these root collars/ribs.
        bpy.ops.uv.cube_project(cube_size=0.22)
        bpy.ops.object.mode_set(mode="OBJECT")
        obj.select_set(False)
    return obj


def torus(path, z, major, minor, mat, x=0.0, y=0.0, tilt=0.0):
    bpy.ops.mesh.primitive_torus_add(
        major_segments=20, minor_segments=8,
        location=(x, y, z),
        major_radius=major, minor_radius=minor)
    obj = bpy.context.object
    obj.rotation_euler[1] = tilt
    return finish_object(obj, path, mat)


def sphere(path, location, radius, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=12, ring_count=8, radius=radius, location=location)
    return finish_object(bpy.context.object, path, mat)


def tube(path, start, end, radius, mat):
    a, b = Vector(start), Vector(end)
    direction = b - a
    length = direction.length
    if length <= 1e-5:
        raise ValueError(path + ": degenerate tube")
    midpoint = (a + b) * 0.5
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=10, radius=radius, depth=length, location=midpoint)
    obj = bpy.context.object
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(direction.normalized())
    return finish_object(obj, path, mat)


def build_club(root_mat, glow_mat):
    # The mace stays unmistakably present. Worldroot is grown around its haft and cages the
    # existing faceted head rather than replacing the head with another unrelated bludgeon.
    torus("worldroot/grip-collar-low", -0.39, 0.036, 0.009, root_mat, tilt=0.08)
    torus("worldroot/grip-collar-mid", -0.16, 0.035, 0.008, root_mat, tilt=-0.06)
    torus("worldroot/head-collar", 0.34, 0.043, 0.010, root_mat, tilt=0.10)
    braces = [
        ((0.025, 0.000, 0.31), (0.105, 0.035, 0.55)),
        ((-0.025, 0.000, 0.31), (-0.105, -0.035, 0.55)),
        ((0.000, 0.025, 0.33), (0.070, -0.085, 0.60)),
        ((0.000, -0.025, 0.33), (-0.070, 0.085, 0.60)),
    ]
    for i, (a, b) in enumerate(braces):
        tube("worldroot/head-brace-%d" % i, a, b, 0.012, root_mat)
    sphere("worldroot/spore-node-0", (0.105, 0.030, 0.56), 0.026, glow_mat)
    sphere("worldroot/spore-node-1", (-0.095, -0.035, 0.59), 0.023, glow_mat)
    sphere("worldroot/spore-node-2", (0.040, -0.090, 0.53), 0.019, glow_mat)


def build_bow(root_mat, glow_mat):
    # Keep the segmented crystal limbs exposed. Root growth reinforces the riser and continues
    # as two narrow organic ribs along the inner limb line; Spire Fibre is represented by the
    # repeated binding collars rather than a new unrelated bow body.
    for i, (z, tilt) in enumerate(((-0.18, 0.08), (-0.06, -0.06), (0.07, 0.05), (0.19, -0.08))):
        torus("worldroot/riser-binding-%d" % i, z, 0.034, 0.0065, root_mat, tilt=tilt)
    ribs = [
        ((-0.020, 0.018, 0.17), (-0.045, 0.020, 0.48)),
        ((-0.045, 0.020, 0.48), (-0.025, 0.012, 0.76)),
        ((0.020, -0.018, -0.17), (0.045, -0.020, -0.48)),
        ((0.045, -0.020, -0.48), (0.025, -0.012, -0.76)),
    ]
    for i, (a, b) in enumerate(ribs):
        tube("worldroot/limb-rib-%d" % i, a, b, 0.009, root_mat)
    sphere("worldroot/spore-node-upper", (-0.040, 0.020, 0.30), 0.021, glow_mat)
    sphere("worldroot/spore-node-lower", (0.040, -0.020, -0.30), 0.021, glow_mat)


def author(model_id):
    base_id, kind = SPECS[model_id]
    base = SOURCE / (base_id + ".blend")
    if not base.is_file():
        raise FileNotFoundError(base)

    bpy.ops.wm.open_mainfile(filepath=str(base))
    scene = bpy.context.scene
    scene["model_id"] = model_id
    scene["derived_from"] = base_id
    scene["underworld_weapon_derivation"] = "crystal-chassis-plus-biome-accent"

    root_mat = material(
        "magenheim.underworld-weapon.%s.worldroot.timber" % model_id,
        (0.30, 0.38, 0.20), roughness=0.78)
    glow_mat = material(
        "magenheim.underworld-weapon.%s.spore-crystal.crystal" % model_id,
        (0.46, 0.82, 0.52), roughness=0.28, emission=(0.12, 0.34, 0.16))

    if kind == "club":
        build_club(root_mat, glow_mat)
    elif kind == "bow":
        build_bow(root_mat, glow_mat)
    else:
        raise ValueError(kind)

    scene["runtime_lights"] = scene.get("runtime_lights", "[]")
    bpy.context.preferences.filepaths.save_version = 0
    target = SOURCE / (model_id + ".blend")
    bpy.ops.wm.save_as_mainfile(filepath=str(target), compress=True)
    print("AUTHORED", model_id, "from", base_id, flush=True)


requested = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else list(SPECS)
unknown = [value for value in requested if value not in SPECS]
if unknown:
    raise SystemExit("Unknown Underworld weapon model(s): " + ", ".join(unknown))
for model_id in requested:
    author(model_id)
