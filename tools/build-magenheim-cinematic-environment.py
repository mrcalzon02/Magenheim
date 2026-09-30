#!/usr/bin/env python3
"""
Build the Magenheim cinematic environment for "Peace Was Only the Beginning".

This is a Blender authoring tool, not an image-warping animatic.  It creates two
real 3D sets:
  * a restrained Alpine/Nordic village assembled from simple buildable forms;
  * the Dark Throne hall, importing Magenheim's authoritative dark-throne and
    Last Argument Blender sources.

The scene deliberately follows the cinematic reference rule that negative space
may remain simple.  Broad sky, terrain, walls, roofs and shadow masses are kept
quiet; detail is concentrated on silhouettes, the throne, braziers, banners,
eyes and weapons.

Run:
  blender --background --factory-startup \
    --python tools/build-magenheim-cinematic-environment.py -- \
    --output assets/cinematics/peace-was-only-the-beginning/source/magenheim-peace-cinematic.blend \
    --preview-dir build/cinematic-previews

Verification:
  blender --background <blend> \
    --python tools/build-magenheim-cinematic-environment.py -- --verify
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets" / "models" / "source"
DEFAULT_OUTPUT = ROOT / "assets" / "cinematics" / "peace-was-only-the-beginning" / "source" / "magenheim-peace-cinematic.blend"

FPS = 24
SHOT_SPECS = (
    ("SHOT_01_RETURN", 1, 72),
    ("SHOT_02_HOMECOMING", 73, 144),
    ("SHOT_03_PEACE", 145, 216),
    ("SHOT_04_RUMBLE", 217, 288),
    ("SHOT_05_OMEN", 289, 360),
    ("SHOT_06_THRONE_REVEAL", 361, 444),
    ("SHOT_07_KING_AWAKENS", 445, 540),
    ("SHOT_08_LAST_ARGUMENT", 541, 672),
)
REQUIRED_SOURCES = (
    "dark-throne.blend",
    "nowhere-king-sword-firmament.blend",
    "nowhere-king-sword-null-gate.blend",
)

# Keep the three cinematic sets physically separated so preview cameras never see
# geometry from another shot family.  This also makes the .blend pleasant to
# inspect and animate by hand.
SURFACE_Y = 0.0
HALL_X = 110.0
THRONE_Y = 240.0


def args():
    raw = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--preview-dir", type=Path)
    parser.add_argument("--render-previews", type=Path)
    parser.add_argument("--verify", action="store_true")
    return parser.parse_args(raw)


def collection(name: str, parent=None):
    c = bpy.data.collections.get(name) or bpy.data.collections.new(name)
    if parent is None:
        if c.name not in bpy.context.scene.collection.children:
            try:
                bpy.context.scene.collection.children.link(c)
            except RuntimeError:
                pass
    else:
        if c.name not in parent.children:
            try:
                parent.children.link(c)
            except RuntimeError:
                pass
    return c


def relink(obj, target):
    for c in tuple(obj.users_collection):
        c.objects.unlink(obj)
    target.objects.link(obj)


def mat(name, color, roughness=0.75, metallic=0.0, emission=None, emission_strength=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        if "Base Color" in bsdf.inputs:
            bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = roughness
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = metallic
        if emission:
            key = "Emission Color" if "Emission Color" in bsdf.inputs else "Emission"
            if key in bsdf.inputs:
                bsdf.inputs[key].default_value = (*emission, 1.0)
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = emission_strength
    m.diffuse_color = (*color, 1.0)
    return m


def add_box(name, location, scale, material, target):
    bpy.ops.mesh.primitive_cube_add(location=location)
    o = bpy.context.object
    o.name = name
    o.scale = (scale[0] / 2.0, scale[1] / 2.0, scale[2] / 2.0)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(material)
    relink(o, target)
    return o


def mesh_obj(name, verts, faces, material, target):
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    o = bpy.data.objects.new(name, mesh)
    target.objects.link(o)
    if material:
        mesh.materials.append(material)
    return o


def add_roof(name, location, width, depth, wall_height, rise, material, target):
    w, d, z = width / 2, depth / 2, wall_height / 2
    verts = [
        (-w, -d, -z), (w, -d, -z), (-w, d, -z), (w, d, -z),
        (0, -d, rise - z), (0, d, rise - z),
    ]
    faces = [(0, 2, 5, 4), (1, 4, 5, 3), (0, 4, 1), (2, 3, 5)]
    o = mesh_obj(name, verts, faces, material, target)
    o.location = location
    return o


def add_house(prefix, x, y, scale, mats, target, great_hall=False):
    w = (8.0 if great_hall else 5.2) * scale
    d = (12.0 if great_hall else 7.0) * scale
    h = (4.4 if great_hall else 3.0) * scale
    stone_h = 0.55 * scale
    add_box(prefix + "_StoneFoot", (x, y, stone_h / 2), (w, d, stone_h), mats["stone"], target)
    body = add_box(prefix + "_TimberBody", (x, y, stone_h + h / 2), (w, d, h), mats["wood"], target)
    add_roof(prefix + "_Roof", (x, y, stone_h + h), w * 1.15, d * 1.08, 0.2, h * 0.62, mats["roof"], target)

    # Construction detail only: corner posts, one wall band and a ridge.  Do not
    # turn the broad wall surfaces into decorative noise.
    beam = 0.20 * scale
    for sx in (-1, 1):
        add_box(prefix + f"_CornerBeam_{sx:+}", (x + sx * (w / 2 - beam / 2), y, stone_h + h / 2),
                (beam, d + 0.05, h), mats["darkwood"], target)
    add_box(prefix + "_WallBand", (x, y - d / 2 - .04, stone_h + h * .67),
            (w * .92, .16 * scale, .18 * scale), mats["darkwood"], target)
    add_box(prefix + "_Ridge", (x, y, stone_h + h + h * .61),
            (.18 * scale, d * 1.10, .18 * scale), mats["darkwood"], target)

    front_y = y - d / 2 - 0.05
    add_box(prefix + "_Door", (x, front_y, stone_h + 1.0 * scale),
            (1.15 * scale, 0.14 * scale, 2.0 * scale), mats["darkwood"], target)
    window_count = 2 if great_hall else 1
    for wi in range(window_count):
        wx = x + ((wi * 2 - (window_count - 1)) * 1.65 * scale)
        if window_count == 1:
            wx = x + 1.45 * scale
        add_box(prefix + f"_Window_{wi}", (wx, front_y - .015, stone_h + h * .62),
                (.72 * scale, .08 * scale, .62 * scale), mats["window"], target)
    return body

def add_humanoid_proxy(name, location, scale, mats, target, material_key="hero", broad=False):
    """Low-poly rigging proxy. It is a replaceable body, not final character art."""
    root = bpy.data.objects.new(name + "_ROOT", None)
    root.empty_display_type = 'ARROWS'
    root.empty_display_size = .65 * scale
    root.location = location
    target.objects.link(root)

    def child_box(suffix, local, dims, key=material_key):
        obj = add_box(name + "_" + suffix,
                      (location[0] + local[0], location[1] + local[1], location[2] + local[2]),
                      dims, mats[key], target)
        obj.parent = root
        obj.matrix_parent_inverse = root.matrix_world.inverted()
        return obj

    torso_w = (.95 if broad else .72) * scale
    child_box("Torso", (0,0,1.42*scale), (torso_w,.48*scale,1.28*scale))
    child_box("Pelvis", (0,0,.78*scale), (.62*scale,.42*scale,.34*scale))
    for side in (-1,1):
        child_box(f"Leg_{side:+}", (side*.19*scale,0,.34*scale), (.24*scale,.28*scale,.82*scale))
        child_box(f"Arm_{side:+}", (side*(torso_w*.62),0,1.40*scale), (.22*scale,.25*scale,1.05*scale))
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=.30*scale,
                                         location=(location[0],location[1],location[2]+2.28*scale))
    head = bpy.context.object
    head.name = name + "_Head"
    head.data.materials.append(mats[material_key])
    relink(head,target)
    head.parent = root
    head.matrix_parent_inverse = root.matrix_world.inverted()
    return root


def add_raven_proxy(name, location, scale, mats, target):
    root = bpy.data.objects.new(name + "_ROOT", None)
    root.empty_display_type = 'ARROWS'
    root.empty_display_size = .3 * scale
    root.location = location
    target.objects.link(root)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=.34*scale, location=location)
    body=bpy.context.object
    body.name=name+"_Body"
    body.scale=(1.15,.55,.55)
    body.data.materials.append(mats["raven"])
    relink(body,target)
    body.parent=root
    body.matrix_parent_inverse=root.matrix_world.inverted()
    x,y,z=location
    verts=[(x-.1*scale,y,z),(x-1.05*scale,y-.12*scale,z+.08*scale),(x-.3*scale,y,z-.08*scale),
           (x+.1*scale,y,z),(x+1.05*scale,y-.12*scale,z+.08*scale),(x+.3*scale,y,z-.08*scale)]
    mesh_obj(name+"_Wings",verts,[(0,1,2),(3,4,5)],mats["raven"],target)
    return root


def add_king_proxy(location, mats, target):
    root = add_humanoid_proxy("NOWHERE_KING", location, 1.75, mats, target, "king", broad=True)
    # Tall narrow crown and two luminous eyes are focal read markers.  The
    # runtime King remains the Dverger-derived authored chassis; this proxy is
    # only for camera/blocking until that rig is imported.
    x,y,z=location
    for i,dx in enumerate((-.24,-.12,0,.12,.24)):
        spike=add_box(f"NOWHERE_KING_CrownSpike_{i}",(x+dx,y,z+4.57+abs(dx)*.6),
                      (.07,.09,.62-abs(dx)*.7),mats["blackmetal"],target)
        spike.parent=root
        spike.matrix_parent_inverse=root.matrix_world.inverted()
    for i,dx in enumerate((-.11,.11)):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, radius=.045,
                                            location=(x+dx,y-.27,z+4.0))
        eye=bpy.context.object
        eye.name=f"NOWHERE_KING_Eye_{i}"
        eye.data.materials.append(mats["eye"])
        relink(eye,target)
        eye.parent=root
        eye.matrix_parent_inverse=root.matrix_world.inverted()
    return root


def add_pine(name, x, y, z, scale, mats, target, foreground=False):
    bpy.ops.mesh.primitive_cylinder_add(vertices=7, radius=0.18 * scale, depth=2.8 * scale, location=(x, y, z + 1.4 * scale))
    trunk = bpy.context.object
    trunk.name = name + "_Trunk"
    trunk.data.materials.append(mats["darkwood"])
    relink(trunk, target)
    tiers = 3 if foreground else 2
    for i in range(tiers):
        radius = (1.35 - 0.27 * i) * scale
        depth = (2.1 - 0.2 * i) * scale
        zz = z + (2.3 + i * 0.8) * scale
        bpy.ops.mesh.primitive_cone_add(vertices=7, radius1=radius, radius2=0.08 * scale, depth=depth, location=(x, y, zz))
        crown = bpy.context.object
        crown.name = f"{name}_Crown_{i}"
        crown.data.materials.append(mats["pine"])
        relink(crown, target)


def add_terrain(mats, target):
    n = 20
    size = 120.0
    verts = []
    faces = []
    for j in range(n + 1):
        y = -size / 2 + size * j / n
        for i in range(n + 1):
            x = -size / 2 + size * i / n
            z = -0.35 + 0.65 * math.sin(x * 0.045) * math.cos(y * 0.035)
            if abs(x) < 7 and -36 < y < 28:
                z *= 0.25
            verts.append((x, y, z))
    for j in range(n):
        for i in range(n):
            a = j * (n + 1) + i
            faces.append((a, a + 1, a + n + 2, a + n + 1))
    return mesh_obj("Village_Terrain", verts, faces, mats["ground"], target)


def add_mountain_backdrop(mats, target):
    # Three simple silhouette ridges: broad shapes, intentionally low detail.
    for layer, (y, height, color_key) in enumerate(((45, 18, "mountain_far"), (34, 12, "mountain_mid"), (26, 8, "mountain_near"))):
        verts = [(-70, y, -1)]
        samples = 14
        for i in range(samples + 1):
            x = -70 + 140 * i / samples
            z = 5 + height * (0.30 + 0.70 * abs(math.sin(i * 1.41 + layer * 0.6)))
            verts.append((x, y, z))
        verts.append((70, y, -1))
        faces = [tuple(range(len(verts)))]
        mesh_obj(f"Village_MountainLayer_{layer}", verts, faces, mats[color_key], target)


def add_village(mats, root):
    terrain = collection("CIN_SurfaceVillage", root)
    add_terrain(mats, terrain)
    add_mountain_backdrop(mats, terrain)

    # A small, buildable settlement: one hall, five houses, a road and tree
    # silhouettes.  Nothing here exists only to decorate the storyboard.
    add_house("Village_GreatHall", 7.5, 8.0, 1.0, mats, terrain, great_hall=True)
    for idx, (x, y, s) in enumerate(((-12, 5, .86), (-18, -8, .78), (16, -6, .82),
                                      (22, 10, .72), (-3, 18, .72))):
        add_house(f"Village_House_{idx:02}", x, y, s, mats, terrain)
    path = add_box("Village_MainPath", (0, -5, 0.03), (6.5, 50, 0.08), mats["path"], terrain)
    path.rotation_euler[2] = math.radians(-4)

    pines = ((-28,-18,1.1,True),(-24,18,1.0,True),(31,-10,.95,True),
             (-38,4,.95,False),(-45,18,.9,False),(39,24,.8,False),
             (45,-24,.85,False),(-50,-24,.82,False),(34,34,.78,False))
    for i,(x,y,s,fg) in enumerate(pines):
        add_pine(f"Village_Pine_{i:02}",x,y,0,s,mats,terrain,fg)

    # Renderable rigging proxies.  These are intentionally simple and are
    # replaced by character rigs in the animation pass.
    add_humanoid_proxy("HERO_A",(-2.2,-26.0,.15),.92,mats,terrain,"hero",True)
    add_humanoid_proxy("HERO_B",(0.0,-27.0,.15),.82,mats,terrain,"hero")
    add_humanoid_proxy("HERO_C",(2.1,-26.4,.15),.88,mats,terrain,"hero")
    add_raven_proxy("RAVEN",(-7,8,9),.75,mats,terrain)
    return terrain


def add_hall_interior(mats, root):
    """Separate practical great-hall interior for Shot 2."""
    hall = collection("CIN_GreatHallInterior", root)
    x0 = HALL_X
    add_box("HallInterior_Floor",(x0,0,-.12),(18,30,.24),mats["hall_floor"],hall)
    # Broad side walls stop below the rafters; posts and crossbeams explain the
    # structure without filling every plank with trim.
    for sx in (-1,1):
        add_box(f"HallInterior_Wall_{sx:+}",(x0+sx*8.6,0,3.3),(.45,30,6.6),mats["wood"],hall)
    add_box("HallInterior_RearWall",(x0,14.7,3.3),(17.6,.45,6.6),mats["wood"],hall)
    for yi in (-11,-5,1,7,13):
        for sx in (-1,1):
            add_box(f"HallInterior_Post_{sx:+}_{yi:+}",(x0+sx*6.7,yi,3.3),(.38,.38,6.6),mats["darkwood"],hall)
        add_box(f"HallInterior_Crossbeam_{yi:+}",(x0,yi,6.1),(13.8,.34,.34),mats["darkwood"],hall)
    # Central hearth, benches and warm light.
    add_box("HallInterior_Hearth",(x0,3,.18),(3.8,2.2,.35),mats["stone"],hall)
    for sx in (-1,1):
        add_box(f"HallInterior_Bench_{sx:+}",(x0+sx*4.4,1,.55),(1.0,13,.55),mats["darkwood"],hall)
    light_data=bpy.data.lights.new("HallInterior_Fire_Data",type='POINT')
    light_data.color=(1.0,.28,.06); light_data.energy=1300; light_data.shadow_soft_size=2.3
    light=bpy.data.objects.new("HallInterior_Fire",light_data)
    light.location=(x0,3,1.3); hall.objects.link(light)
    # Hero and villager proxies give the camera real readable silhouettes.
    add_humanoid_proxy("HALL_HERO_A",(x0-1.6,-7,.05),.92,mats,hall,"hero",True)
    add_humanoid_proxy("HALL_HERO_B",(x0,-7.5,.05),.82,mats,hall,"hero")
    add_humanoid_proxy("HALL_HERO_C",(x0+1.6,-7,.05),.88,mats,hall,"hero")
    for i,(dx,dy) in enumerate(((-5,-1),(5,-1),(-5,5),(5,5),(-4,10),(4,10))):
        add_humanoid_proxy(f"HALL_VILLAGER_{i}",(x0+dx,dy,.05),.76,mats,hall,"villager")
    return hall

def append_blend(path: Path, root_name: str, target, location=(0,0,0), scale=1.0, rotation=(0,0,0)):
    if not path.is_file():
        raise RuntimeError(f"Missing authoritative cinematic source: {path}")
    before = set(bpy.data.objects)
    with bpy.data.libraries.load(str(path), link=False) as (src, dst):
        dst.objects = [name for name in src.objects if not name.lower().startswith(("camera", "light"))]
    loaded = [o for o in dst.objects if o is not None and o not in before]

    # Parent first at identity, then move the root.  The old version assigned
    # the root transform before preserving child world matrices, cancelling the
    # intended placement and leaving imported throne assets at the village.
    root = bpy.data.objects.new(root_name, None)
    target.objects.link(root)
    for obj in loaded:
        for collection_ in tuple(obj.users_collection):
            collection_.objects.unlink(obj)
        target.objects.link(obj)
    for obj in loaded:
        if obj.parent is None:
            world = obj.matrix_world.copy()
            obj.parent = root
            obj.matrix_world = world
    root.location = location
    root.scale = (scale, scale, scale)
    root.rotation_euler = rotation
    return root

def add_banner(name, x, y, z, facing, mats, target):
    pole = add_box(name + "_Pole", (x, y, z), (.18, .18, 7.2), mats["blackmetal"], target)
    banner = add_box(name + "_Cloth", (x, y, z + .7), (3.2, .12, 5.1), mats["cloth"], target)
    pole.rotation_euler[2] = facing
    banner.rotation_euler[2] = facing
    # The eclipse is a single restrained focal symbol, not surface noise.
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=.62, depth=.04, location=(x, y - .08, z + 1.2), rotation=(math.radians(90),0,facing))
    disk = bpy.context.object
    disk.name = name + "_Eclipse"
    disk.data.materials.append(mats["eclipse"])
    relink(disk, target)
    return banner


def add_brazier(name, x, y, z, mats, target):
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=.75, depth=.24, location=(x,y,z+.9))
    bowl = bpy.context.object
    bowl.name = name + "_Bowl"
    bowl.data.materials.append(mats["blackmetal"])
    relink(bowl, target)
    add_box(name + "_Stand", (x,y,z+.38), (.22,.22,.76), mats["blackmetal"], target)
    light_data = bpy.data.lights.new(name + "_Light", type='POINT')
    light_data.color = (1.0, .30, .08)
    light_data.energy = 850
    light_data.shadow_soft_size = 1.1
    light = bpy.data.objects.new(name + "_Light", light_data)
    light.location = (x,y,z+1.45)
    target.objects.link(light)


def add_throne_environment(mats, root):
    hall = collection("CIN_DarkThrone", root)
    y0 = THRONE_Y

    # The committed Dark Throne source is the arena authority.  These broad
    # envelope pieces only give cinematic negative space and vertical scale.
    add_box("ThroneHall_Floor", (0,y0,-.70), (58,68,.5), mats["basalt"], hall)
    for x in (-27.0,27.0):
        add_box(f"ThroneHall_Wall_{x:+}",(x,y0+2,8),(1.6,66,16),mats["basalt_dark"],hall)
    add_box("ThroneHall_RearWall",(0,y0+31,8),(56,1.6,16),mats["basalt_dark"],hall)
    for i,x in enumerate((-20,-13,-6,6,13,20)):
        add_box(f"ThroneHall_Pier_{i}",(x,y0+17,7),(1.2,1.2,14),mats["basalt"],hall)

    append_blend(SOURCE/"dark-throne.blend","AUTH_DarkThrone",hall,location=(0,y0,0))

    # Existing Magenheim weapon art is parked at the King's staging position.
    # Animation will bind these to the hands and perform the actual cloak draw.
    append_blend(SOURCE/"nowhere-king-sword-firmament.blend","AUTH_Firmament",hall,
                 location=(-1.25,y0+14.0,2.0),scale=.78,
                 rotation=(math.radians(90),0,math.radians(-16)))
    append_blend(SOURCE/"nowhere-king-sword-null-gate.blend","AUTH_NullGate",hall,
                 location=(1.25,y0+14.0,2.0),scale=.78,
                 rotation=(math.radians(90),0,math.radians(16)))

    for i,x in enumerate((-18,-8,8,18)):
        add_banner(f"Blackstone_Banner_{i}",x,y0+28.5,7.0,0,mats,hall)
    for i,(x,dy) in enumerate(((-18,-12),(-9,-10),(9,-10),(18,-12),
                               (-18,8),(-9,10),(9,10),(18,8))):
        add_brazier(f"Blackstone_Brazier_{i}",x,y0+dy,0,mats,hall)

    add_king_proxy((0,y0+14.0,.1),mats,hall)
    return hall

def camera(name, location, target_point, lens, target_collection):
    data = bpy.data.cameras.new(name + "_Data")
    data.lens = lens
    data.sensor_width = 36
    o = bpy.data.objects.new(name, data)
    o.location = location
    direction = Vector(target_point) - o.location
    o.rotation_euler = direction.to_track_quat('-Z','Y').to_euler()
    target_collection.objects.link(o)
    return o


def add_cameras(root):
    cams = collection("CIN_Cameras", root)
    y0 = THRONE_Y
    specs = (
        # Surface: heroes foreground, village/mountains progressively opened.
        ("SHOT_01_RETURN_CAM", (0,-42,5.5), (0,-6,2.8), 50),
        ("SHOT_02_HOMECOMING_CAM", (HALL_X,-12.5,3.1), (HALL_X,4.5,2.6), 46),
        ("SHOT_03_PEACE_CAM", (-31,-37,13.5), (2,6,3.2), 48),
        ("SHOT_04_RUMBLE_CAM", (8,-30,3.0), (0,-17,2.5), 58),
        ("SHOT_05_OMEN_CAM", (-13,1,10.8), (-7,8,9.0), 76),
        # Underworld: physically isolated set, so no village geometry can leak.
        ("SHOT_06_THRONE_REVEAL_CAM", (0,y0-37,6.4), (0,y0+18,4.2), 44),
        ("SHOT_07_KING_AWAKENS_CAM", (-7,y0+1,4.4), (0,y0+14.2,3.6), 60),
        ("SHOT_08_LAST_ARGUMENT_CAM", (0,y0+5.5,3.5), (0,y0+14.2,3.35), 78),
    )
    created = {}
    for name, loc, tgt, lens in specs:
        created[name] = camera(name, loc, tgt, lens, cams)
    return created

def add_lighting(root, mats):
    lights = collection("CIN_Lights", root)

    sun_data = bpy.data.lights.new("Surface_Sun_Data", type='SUN')
    sun_data.energy = 3.0
    sun_data.color = (0.74,0.80,0.92)
    sun = bpy.data.objects.new("Surface_Sun", sun_data)
    sun.rotation_euler = (math.radians(31), math.radians(-18), math.radians(-34))
    lights.objects.link(sun)

    # Cool top light for the distant throne; braziers remain the warm focal
    # sources and prevent the dark set from becoming uniformly orange.
    key_data = bpy.data.lights.new("Throne_Key_Data", type='AREA')
    key_data.energy = 1100
    key_data.shape = 'DISK'
    key_data.size = 14
    key_data.color = (0.20,0.28,0.44)
    key = bpy.data.objects.new("Throne_Key", key_data)
    key.location = (0,THRONE_Y+5,14)
    direction=Vector((0,THRONE_Y+18,3.5))-key.location
    key.rotation_euler=direction.to_track_quat('-Z','Y').to_euler()
    lights.objects.link(key)

def remap_and_pack_images():
    """Resolve workstation-authored texture paths back into the repository and pack them."""
    texture_dir = ROOT / "assets" / "models" / "textures"
    unresolved = []
    for image in bpy.data.images:
        if image.source != 'FILE' or not image.filepath:
            continue
        filename = Path(bpy.path.abspath(image.filepath)).name
        candidate = texture_dir / filename
        if candidate.is_file():
            image.filepath = str(candidate)
            try:
                image.reload()
            except RuntimeError:
                unresolved.append(image.name)
        elif not Path(bpy.path.abspath(image.filepath)).is_file():
            unresolved.append(image.name)
    if unresolved:
        print("CINEMATIC TEXTURE WARNING unresolved=" + ",".join(sorted(set(unresolved))), flush=True)
    try:
        bpy.ops.file.pack_all()
    except RuntimeError as exc:
        print("CINEMATIC PACK WARNING " + str(exc), flush=True)


def set_world():
    world = bpy.data.worlds.new("Magenheim_Cinematic_World")
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs["Color"].default_value = (0.16,0.22,0.31,1)
    bg.inputs["Strength"].default_value = .72
    bpy.context.scene.world = world


def materials():
    return {
        "ground": mat("CIN_Ground", (.15,.22,.16), .95),
        "path": mat("CIN_Path", (.24,.21,.17), .97),
        "wood": mat("CIN_Timber", (.25,.16,.09), .82),
        "darkwood": mat("CIN_DarkTimber", (.09,.065,.05), .88),
        "roof": mat("CIN_Roof", (.14,.12,.11), .96),
        "stone": mat("CIN_VillageStone", (.28,.30,.31), .93),
        "pine": mat("CIN_Pine", (.075,.12,.10), .97),
        "mountain_far": mat("CIN_MountainFar", (.25,.32,.42), 1.0, emission=(.25,.32,.42), emission_strength=.12),
        "mountain_mid": mat("CIN_MountainMid", (.19,.25,.32), 1.0, emission=(.19,.25,.32), emission_strength=.10),
        "mountain_near": mat("CIN_MountainNear", (.12,.17,.20), 1.0, emission=(.12,.17,.20), emission_strength=.08),
        "basalt": mat("CIN_Basalt", (.055,.060,.070), .92),
        "basalt_dark": mat("CIN_BasaltDark", (.025,.028,.035), .96),
        "blackmetal": mat("CIN_BlackMetal", (.025,.025,.030), .34, .78),
        "cloth": mat("CIN_BlackCloth", (.018,.019,.024), .88),
        "eclipse": mat("CIN_Eclipse", (.23,.23,.24), .75),
        "eye": mat("CIN_EyeGlow", (.20,.005,.003), .25, emission=(1.0,.01,.005), emission_strength=7.0),
        "window": mat("CIN_WindowWarm", (.42,.14,.035), .65, emission=(1.0,.24,.045), emission_strength=2.0),
        "hero": mat("CIN_HeroProxy", (.045,.055,.065), .82),
        "villager": mat("CIN_VillagerProxy", (.20,.10,.065), .90),
        "raven": mat("CIN_RavenProxy", (.012,.015,.020), .94),
        "king": mat("CIN_KingProxy", (.008,.010,.014), .68, .35),
        "hall_floor": mat("CIN_HallFloor", (.11,.075,.045), .90),
    }


def frame_markers(scene):
    for name, start, end in SHOT_SPECS:
        scene.timeline_markers.new(name, frame=start)
        scene[f"{name}_END"] = end


def configure_scene():
    s = bpy.context.scene
    s.render.engine = 'CYCLES'
    s.cycles.samples = 16
    s.cycles.use_denoising = True
    s.render.resolution_x = 1920
    s.render.resolution_y = 1080
    s.render.resolution_percentage = 50
    s.render.fps = FPS
    s.frame_start = SHOT_SPECS[0][1]
    s.frame_end = SHOT_SPECS[-1][2]
    s.render.image_settings.file_format = 'PNG'
    s.render.film_transparent = False
    s.view_settings.look = 'AgX - Medium High Contrast'
    s["magenheim_cinematic_id"] = "peace-was-only-the-beginning"
    s["style_negative_space"] = "simple broad fields; detail only where story requires"
    frame_markers(s)
    return s


def render_previews(scene, cameras, preview_dir: Path):
    preview_dir.mkdir(parents=True, exist_ok=True)
    frame_by_shot = {name: start for name, start, _ in SHOT_SPECS}
    mapping = {
        "SHOT_01_RETURN":"SHOT_01_RETURN_CAM",
        "SHOT_02_HOMECOMING":"SHOT_02_HOMECOMING_CAM",
        "SHOT_03_PEACE":"SHOT_03_PEACE_CAM",
        "SHOT_04_RUMBLE":"SHOT_04_RUMBLE_CAM",
        "SHOT_05_OMEN":"SHOT_05_OMEN_CAM",
        "SHOT_06_THRONE_REVEAL":"SHOT_06_THRONE_REVEAL_CAM",
        "SHOT_07_KING_AWAKENS":"SHOT_07_KING_AWAKENS_CAM",
        "SHOT_08_LAST_ARGUMENT":"SHOT_08_LAST_ARGUMENT_CAM",
    }
    scene.render.resolution_percentage = 35
    for shot, cam_name in mapping.items():
        scene.camera = cameras[cam_name]
        scene.frame_set(frame_by_shot[shot])
        scene.render.filepath = str(preview_dir / f"{shot.lower()}.png")
        bpy.ops.render.render(write_still=True)


def verify():
    scene = bpy.context.scene
    required_objects = {
        "AUTH_DarkThrone", "AUTH_Firmament", "AUTH_NullGate",
        "NOWHERE_KING_ROOT", "HERO_A_ROOT", "HERO_B_ROOT", "HERO_C_ROOT", "RAVEN_ROOT",
        "SHOT_01_RETURN_CAM", "SHOT_02_HOMECOMING_CAM", "SHOT_06_THRONE_REVEAL_CAM", "SHOT_08_LAST_ARGUMENT_CAM",
        "Village_GreatHall_TimberBody", "HallInterior_Floor", "ThroneHall_Floor",
    }
    missing = sorted(name for name in required_objects if name not in bpy.data.objects)
    if missing:
        raise RuntimeError("Cinematic environment missing required objects: " + ", ".join(missing))
    marker_names = {m.name for m in scene.timeline_markers}
    missing_markers = sorted(name for name,_,_ in SHOT_SPECS if name not in marker_names)
    if missing_markers:
        raise RuntimeError("Cinematic environment missing shot markers: " + ", ".join(missing_markers))
    if scene.get("magenheim_cinematic_id") != "peace-was-only-the-beginning":
        raise RuntimeError("Cinematic identity metadata missing.")
    report = {
        "scene": scene.get("magenheim_cinematic_id"),
        "objects": len(bpy.data.objects),
        "meshes": len(bpy.data.meshes),
        "materials": len(bpy.data.materials),
        "shots": len(SHOT_SPECS),
        "frame_end": scene.frame_end,
    }
    print("CINEMATIC VERIFIED " + json.dumps(report, sort_keys=True))


def build(output: Path, preview_dir: Path | None):
    print("CINEMATIC BUILD preflight", flush=True)
    for name in REQUIRED_SOURCES:
        if not (SOURCE / name).is_file():
            raise RuntimeError(f"Required authoritative source is missing: {SOURCE/name}")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    print("CINEMATIC BUILD factory", flush=True)
    root = collection("Magenheim_Cinematic")
    mats = materials()
    scene = configure_scene()
    set_world()
    print("CINEMATIC BUILD village", flush=True)
    add_village(mats, root)
    print("CINEMATIC BUILD hall interior", flush=True)
    add_hall_interior(mats, root)
    print("CINEMATIC BUILD throne", flush=True)
    add_throne_environment(mats, root)
    print("CINEMATIC BUILD cameras", flush=True)
    cameras = add_cameras(root)
    add_lighting(root, mats)
    print("CINEMATIC BUILD textures", flush=True)
    remap_and_pack_images()

    output = output if output.is_absolute() else ROOT / output
    output.parent.mkdir(parents=True, exist_ok=True)
    scene.camera = cameras["SHOT_01_RETURN_CAM"]
    print("CINEMATIC BUILD save", flush=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(output), compress=True)
    verify()
    if preview_dir:
        preview_dir = preview_dir if preview_dir.is_absolute() else ROOT / preview_dir
        print("CINEMATIC BUILD previews", flush=True)
        render_previews(scene, cameras, preview_dir)
        bpy.ops.wm.save_as_mainfile(filepath=str(output), compress=True)
    print(f"WROTE {output}", flush=True)


def main():
    ns = args()
    if ns.verify:
        verify()
    elif ns.render_previews:
        verify()
        preview_dir = ns.render_previews if ns.render_previews.is_absolute() else ROOT / ns.render_previews
        cameras = {o.name:o for o in bpy.data.objects if o.type == 'CAMERA'}
        print("CINEMATIC RENDER previews", flush=True)
        render_previews(bpy.context.scene, cameras, preview_dir)
    else:
        build(ns.output, ns.preview_dir)


if __name__ == "__main__":
    main()
