#!/usr/bin/env python3
"""Author the Nowhere King's paired Last Argument swords.

Firmament is a contained-galaxy blade. Null Gate is a framed absence/event-horizon blade.
They deliberately share the existing crystal sword's proven grip/guard ancestry and axis, but
replace its blade construction completely. They are not palette swaps.

Run through the project Blender wrapper:
  tools/blender.ps1 author-nowhere-king-swords
  tools/blender.ps1 export-model-assets nowhere-king-sword-firmament nowhere-king-sword-null-gate

Blender Z remains weapon length, +Z is the working end, and the grip remains around the origin.
The King's 1.82x body scale turns this ordinary authored envelope into the intended ~2.8m royal
weapons without inventing a second held-item convention.
"""
import bmesh
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets" / "models" / "source"
BASE_ID = "crystal-weapon-sword"
BASE = SOURCE / (BASE_ID + ".blend")
REVISION = "nowhere-king-last-argument-r1"
SPECS = {
    "nowhere-king-sword-firmament": "firmament",
    "nowhere-king-sword-null-gate": "null-gate",
}
REMOVE = {"blade", "blade-core", "rainbow-inlay"}


def material(name, colour, metallic=0.0, roughness=0.5, emission=(0.0, 0.0, 0.0), alpha=1.0, image=None):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bs = mat.node_tree.nodes.get("Principled BSDF")
    bs.inputs["Base Color"].default_value = (*colour, alpha)
    bs.inputs["Metallic"].default_value = metallic
    bs.inputs["Roughness"].default_value = roughness
    bs.inputs["Emission Color"].default_value = (*emission, 1.0)
    bs.inputs["Emission Strength"].default_value = 1.0
    bs.inputs["Alpha"].default_value = alpha
    if image is not None:
        tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
        tex.image = image
        tex.interpolation = "Linear"
        mat.node_tree.links.new(tex.outputs["Color"], bs.inputs["Base Color"])
        mat.node_tree.links.new(tex.outputs["Alpha"], bs.inputs["Alpha"])
    return mat


def texture(kind, model_id, size=512):
    image = bpy.data.images.new(model_id + "-" + kind, size, size, alpha=True)
    px = [0.0] * (size * size * 4)
    seed = 17.0 if kind == "galaxy" else 43.0
    for y in range(size):
        v = y / (size - 1)
        for x in range(size):
            u = x / (size - 1)
            nx = u - 0.5
            h = math.sin((x * 12.9898 + y * 78.233 + seed) * 0.01745329252) * 43758.5453
            h -= math.floor(h)
            i = (y * size + x) * 4
            if kind == "galaxy":
                ribbon = math.exp(-((nx - 0.13 * math.sin(v * 11.0)) ** 2) / 0.012)
                ribbon2 = math.exp(-((nx + 0.16 * math.sin(v * 7.0 + 1.4)) ** 2) / 0.022)
                r = 0.008 + ribbon * 0.14 + ribbon2 * 0.05
                g = 0.012 + ribbon * 0.045 + ribbon2 * 0.11
                b = 0.035 + ribbon * 0.24 + ribbon2 * 0.20
                if h > 0.996:
                    star = 0.55 + (h - 0.996) / 0.004 * 0.45
                    r, g, b = star, star * 0.94, min(1.0, star * 1.08)
                px[i:i+4] = (r, g, b, 1.0)
            else:
                core = abs(nx)
                horizon = math.exp(-((core - 0.34) ** 2) / 0.0018)
                ripple = (0.5 + 0.5 * math.sin(v * 42.0 + core * 31.0)) * math.exp(-core * 5.0)
                r = 0.0015 + horizon * 0.15 + ripple * 0.010
                g = 0.0008 + horizon * 0.015
                b = 0.0030 + horizon * 0.30 + ripple * 0.025
                px[i:i+4] = (r, g, b, 0.985)
    image.pixels.foreach_set(px)
    image.pack()
    return image


def mesh_part(name, verts, faces, mat, path):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([tuple(v) for v in verts], [], faces)
    mesh.update()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.append(mat)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    obj["game_node_path"] = path
    obj["game_collision"] = False
    obj["game_crystal"] = json.dumps(None)
    unwrap(obj)
    return obj


def unwrap(obj):
    if obj.type != "MESH":
        return
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.cube_project(cube_size=0.28)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.select_set(False)


def extruded_polygon(name, points, half_thickness, mat, path):
    n = len(points)
    verts = [Vector((x, -half_thickness, z)) for x, z in points]
    verts += [Vector((x, half_thickness, z)) for x, z in points]
    faces = [tuple(reversed(range(n))), tuple(range(n, n * 2))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
    return mesh_part(name, verts, faces, mat, path)


def rail(name, a, b, half_width, half_thickness, mat, path):
    a = Vector(a)
    b = Vector(b)
    direction = b - a
    if direction.length <= 1e-5:
        raise ValueError(name + ": degenerate rail")
    d = direction.normalized()
    perpendicular = Vector((-d.z, 0.0, d.x)) * half_width
    p0, p1, p2, p3 = a + perpendicular, b + perpendicular, b - perpendicular, a - perpendicular
    points = [(p0.x, p0.z), (p1.x, p1.z), (p2.x, p2.z), (p3.x, p3.z)]
    return extruded_polygon(name, points, half_thickness, mat, path)


def sphere(name, location, radius, mat, path, subdivisions=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=radius, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    obj["game_node_path"] = path
    obj["game_collision"] = False
    obj["game_crystal"] = json.dumps(None)
    return obj


def torus(name, z, major, minor, mat, path):
    bpy.ops.mesh.primitive_torus_add(
        major_segments=24, minor_segments=8, major_radius=major, minor_radius=minor,
        location=(0.0, 0.0, z))
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    obj["game_node_path"] = path
    obj["game_collision"] = False
    obj["game_crystal"] = json.dumps(None)
    return obj


def ribbon(name, points, radius, mat, path):
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 2
    curve.bevel_depth = radius
    curve.bevel_resolution = 2
    spline = curve.splines.new("BEZIER")
    spline.bezier_points.add(len(points) - 1)
    for bp, point in zip(spline.bezier_points, points):
        bp.co = point
        bp.handle_left_type = "AUTO"
        bp.handle_right_type = "AUTO"
    obj = bpy.data.objects.new(name, curve)
    bpy.context.scene.collection.objects.link(obj)
    obj.data.materials.append(mat)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    obj = bpy.context.object
    obj["game_node_path"] = path
    obj["game_collision"] = False
    obj["game_crystal"] = json.dumps(None)
    unwrap(obj)
    return obj


def repaint_hilt(model_id):
    black = material(
        "magenheim.nowhere-king-sword.%s.hilt.metal" % model_id,
        (0.014, 0.016, 0.023), metallic=0.86, roughness=0.30)
    leather = material(
        "magenheim.nowhere-king-sword.%s.grip.leather" % model_id,
        (0.035, 0.022, 0.030), metallic=0.0, roughness=0.78)
    old_gold = material(
        "magenheim.nowhere-king-sword.%s.royal-gold.metal" % model_id,
        (0.28, 0.20, 0.075), metallic=0.76, roughness=0.36)
    for obj in list(bpy.context.scene.objects):
        if obj.type != "MESH":
            continue
        simple = obj.name.lower().split(".")[0]
        if simple in REMOVE:
            bpy.data.objects.remove(obj, do_unlink=True)
            continue
        obj["game_node_path"] = "attach/magenheim.%s.visual/hilt/%s" % (model_id, obj.name)
        obj["game_collision"] = False
        obj["game_crystal"] = json.dumps(None)
        obj.data.materials.clear()
        if "grip" in simple:
            obj.data.materials.append(leather)
        elif "guard" in simple:
            obj.data.materials.append(old_gold)
        else:
            obj.data.materials.append(black)


def build_firmament(model_id):
    image = texture("galaxy", model_id)
    body = material(
        "magenheim.nowhere-king-sword.%s.blade-frame.metal" % model_id,
        (0.012, 0.014, 0.024), metallic=0.82, roughness=0.24)
    cosmos = material(
        "magenheim.nowhere-king-sword.%s.cosmos.crystal" % model_id,
        (0.04, 0.06, 0.16), roughness=0.12, emission=(0.055, 0.075, 0.22), image=image)
    star = material(
        "magenheim.nowhere-king-sword.%s.starlight.crystal" % model_id,
        (0.86, 0.90, 1.0), roughness=0.08, emission=(1.0, 0.88, 1.25))
    cyan = material(
        "magenheim.nowhere-king-sword.%s.nebula-blue.crystal" % model_id,
        (0.10, 0.42, 0.72), roughness=0.14, emission=(0.18, 0.55, 0.92))
    magenta = material(
        "magenheim.nowhere-king-sword.%s.nebula-violet.crystal" % model_id,
        (0.44, 0.10, 0.60), roughness=0.14, emission=(0.62, 0.16, 0.84))
    outline = [(-0.050, -0.060), (-0.092, 0.060), (-0.108, 0.520), (-0.086, 0.790),
               (-0.040, 0.930), (0.0, 1.010), (0.040, 0.930), (0.086, 0.790),
               (0.108, 0.520), (0.092, 0.060), (0.050, -0.060)]
    inset = [(-0.031, 0.012), (-0.068, 0.095), (-0.080, 0.510), (-0.063, 0.755),
             (-0.028, 0.900), (0.0, 0.958), (0.028, 0.900), (0.063, 0.755),
             (0.080, 0.510), (0.068, 0.095), (0.031, 0.012)]
    extruded_polygon("firmament-frame", outline, 0.020, body,
                      "attach/magenheim.%s.visual/blade/frame" % model_id)
    extruded_polygon("firmament-cosmos", inset, 0.0225, cosmos,
                      "attach/magenheim.%s.visual/blade/cosmos" % model_id)
    stars = [
        (-0.041,0.145,0.008), (0.027,0.205,0.006), (0.050,0.286,0.010),
        (-0.022,0.338,0.006), (0.012,0.414,0.008), (-0.055,0.492,0.005),
        (0.058,0.563,0.007), (-0.010,0.622,0.010), (0.035,0.696,0.005),
        (-0.041,0.755,0.007), (0.018,0.814,0.006), (-0.008,0.884,0.009),
    ]
    for i, (x, z, r) in enumerate(stars):
        sphere("firmament-star-%02d" % i, (x, 0.0, z), r, star,
               "attach/magenheim.%s.visual/blade/star-%02d" % (model_id, i))
    points_a = [Vector((0.040 * math.sin(i * 0.65), 0.026, 0.10 + i * 0.072)) for i in range(12)]
    points_b = [Vector((0.050 * math.sin(i * 0.58 + 1.8), -0.026, 0.12 + i * 0.068)) for i in range(12)]
    ribbon("firmament-nebula-a", points_a, 0.0045, cyan,
           "attach/magenheim.%s.visual/blade/nebula-a" % model_id)
    ribbon("firmament-nebula-b", points_b, 0.0040, magenta,
           "attach/magenheim.%s.visual/blade/nebula-b" % model_id)
    sphere("firmament-heart", (0.0, 0.0, -0.035), 0.026, star,
           "attach/magenheim.%s.visual/blade/heart" % model_id, subdivisions=2)
    add_light("firmament-light", (0.0, 0.0, 0.52), (0.30, 0.42, 1.0), 85.0, 1.8)


def build_null_gate(model_id):
    image = texture("void", model_id)
    frame = material(
        "magenheim.nowhere-king-sword.%s.frame.metal" % model_id,
        (0.010, 0.008, 0.014), metallic=0.90, roughness=0.22)
    void = material(
        "magenheim.nowhere-king-sword.%s.void.crystal" % model_id,
        (0.001, 0.0005, 0.003), roughness=0.05, emission=(0.0, 0.0, 0.004),
        alpha=0.985, image=image)
    horizon = material(
        "magenheim.nowhere-king-sword.%s.event-horizon.crystal" % model_id,
        (0.18, 0.015, 0.34), roughness=0.12, emission=(0.55, 0.06, 1.10))
    panel = [(-0.040, 0.015), (-0.070, 0.110), (-0.074, 0.650), (-0.050, 0.840),
             (0.0, 0.980), (0.050, 0.840), (0.074, 0.650), (0.070, 0.110), (0.040, 0.015)]
    extruded_polygon("null-gate-absence", panel, 0.009, void,
                      "attach/magenheim.%s.visual/blade/absence" % model_id)
    left = [Vector((-0.055, 0.0, -0.050)), Vector((-0.092, 0.0, 0.110)),
            Vector((-0.094, 0.0, 0.660)), Vector((-0.058, 0.0, 0.855)), Vector((0.0, 0.0, 1.010))]
    right = [Vector((0.055, 0.0, -0.050)), Vector((0.092, 0.0, 0.110)),
             Vector((0.094, 0.0, 0.660)), Vector((0.058, 0.0, 0.855)), Vector((0.0, 0.0, 1.010))]
    for side_name, points in (("left", left), ("right", right)):
        for i in range(len(points) - 1):
            rail("null-gate-%s-frame-%d" % (side_name, i), points[i], points[i+1], 0.014, 0.021,
                 frame, "attach/magenheim.%s.visual/blade/frame-%s-%d" % (model_id, side_name, i))
            rail("null-gate-%s-horizon-%d" % (side_name, i), points[i], points[i+1], 0.0042, 0.024,
                 horizon, "attach/magenheim.%s.visual/blade/horizon-%s-%d" % (model_id, side_name, i))
    for i, (z, major) in enumerate(((0.25, 0.050), (0.50, 0.061), (0.74, 0.052))):
        torus("null-gate-ring-%d" % i, z, major, 0.0045, horizon,
              "attach/magenheim.%s.visual/blade/ring-%d" % (model_id, i))
    spiral = [Vector((0.037 * math.sin(i * 0.72), 0.037 * math.cos(i * 0.72), 0.12 + i * 0.068))
              for i in range(12)]
    ribbon("null-gate-filament", spiral, 0.0035, horizon,
           "attach/magenheim.%s.visual/blade/filament" % model_id)
    add_light("null-gate-light", (0.0, 0.0, 0.53), (0.42, 0.04, 0.86), 92.0, 1.9)


def add_light(name, location, colour, energy, distance):
    data = bpy.data.lights.new(name, "POINT")
    data.color = colour
    data.energy = energy
    data.cutoff_distance = distance
    obj = bpy.data.objects.new(name, data)
    obj.location = location
    bpy.context.scene.collection.objects.link(obj)


def author(model_id):
    if not BASE.is_file():
        raise FileNotFoundError(BASE)
    kind = SPECS[model_id]
    bpy.ops.wm.open_mainfile(filepath=str(BASE))
    repaint_hilt(model_id)
    if kind == "firmament":
        build_firmament(model_id)
    elif kind == "null-gate":
        build_null_gate(model_id)
    else:
        raise ValueError(kind)

    scene = bpy.context.scene
    scene["model_id"] = model_id
    scene["derived_from"] = BASE_ID
    scene["magenheim_family"] = "nowhere-king-last-argument"
    scene["nowhere_king_sword_kind"] = kind
    scene["nowhere_king_sword_authoring"] = REVISION
    scene["runtime_lights"] = "[]"
    scene["surface_finish"] = "2"
    bpy.context.preferences.filepaths.save_version = 0
    target = SOURCE / (model_id + ".blend")
    bpy.ops.wm.save_as_mainfile(filepath=str(target), compress=True)
    meshes = [o for o in scene.objects if o.type == "MESH"]
    triangles = sum(len(o.data.polygons) for o in meshes)
    print("AUTHORED", model_id, kind, len(meshes), "mesh parts", triangles, "faces", flush=True)


requested = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else list(SPECS)
unknown = [value for value in requested if value not in SPECS]
if unknown:
    raise SystemExit("Unknown Nowhere King sword model(s): " + ", ".join(unknown))
for model_id in requested:
    author(model_id)
