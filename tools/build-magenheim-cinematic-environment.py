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
    parser.add_argument("--render-motion-frames", type=Path)
    parser.add_argument("--valheim-donor-library", type=Path)
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
    """Articulated low-poly blocking rig.

    This is not final character art. Its job is to make body mechanics honest:
    hips, knees, ankles, shoulders and elbows have real pivots so walking,
    bracing, standing and weapon draws can be judged before final rigs replace it.
    """
    root=bpy.data.objects.new(name+"_ROOT",None)
    root.empty_display_type='ARROWS'
    root.empty_display_size=.65*scale
    root.location=location
    target.objects.link(root)

    def part(suffix, center, dims, pivot=None, parent=None, key=material_key):
        obj=add_box(name+"_"+suffix,
                    (location[0]+center[0],location[1]+center[1],location[2]+center[2]),
                    dims,mats[key],target)
        if pivot is not None:
            bpy.context.scene.cursor.location=(
                location[0]+pivot[0],location[1]+pivot[1],location[2]+pivot[2])
            bpy.context.view_layer.objects.active=obj
            obj.select_set(True)
            bpy.ops.object.origin_set(type='ORIGIN_CURSOR',center='MEDIAN')
            obj.select_set(False)
        parent=parent or root
        obj.parent=parent
        obj.matrix_parent_inverse=parent.matrix_world.inverted()
        return obj

    torso_w=(.95 if broad else .72)*scale
    part("Pelvis",(0,0,.92*scale),(.62*scale,.42*scale,.30*scale))
    part("Torso",(0,0,1.48*scale),(torso_w,.48*scale,1.00*scale))

    hip_z=.86*scale
    thigh_len=.48*scale
    knee_z=hip_z-thigh_len
    shin_len=.38*scale
    ankle_z=knee_z-shin_len
    shoulder_z=1.90*scale
    upper_len=.48*scale
    elbow_z=shoulder_z-upper_len
    fore_len=.43*scale
    wrist_z=elbow_z-fore_len

    for side in (-1,1):
        sx=side*.19*scale
        thigh=part(f"Leg_{side:+}",(sx,0,(hip_z+knee_z)/2),
                   (.26*scale,.30*scale,thigh_len),
                   pivot=(sx,0,hip_z))
        shin=part(f"Shin_{side:+}",(sx,0,(knee_z+ankle_z)/2),
                  (.22*scale,.26*scale,shin_len),
                  pivot=(sx,0,knee_z),parent=thigh)
        part(f"Foot_{side:+}",(sx,.12*scale,max(.075*scale,ankle_z+.055*scale)),
             (.25*scale,.46*scale,.14*scale),
             pivot=(sx,0,max(.07*scale,ankle_z)),parent=shin)

        ax=side*(torso_w*.62)
        upper=part(f"Arm_{side:+}",(ax,0,(shoulder_z+elbow_z)/2),
                   (.23*scale,.26*scale,upper_len),
                   pivot=(ax,0,shoulder_z))
        fore=part(f"Forearm_{side:+}",(ax,0,(elbow_z+wrist_z)/2),
                  (.20*scale,.23*scale,fore_len),
                  pivot=(ax,0,elbow_z),parent=upper)
        part(f"Hand_{side:+}",(ax,0,wrist_z-.07*scale),
             (.22*scale,.24*scale,.18*scale),
             pivot=(ax,0,wrist_z),parent=fore)

    bpy.ops.mesh.primitive_ico_sphere_add(
        subdivisions=2,radius=.30*scale,
        location=(location[0],location[1],location[2]+2.22*scale))
    head=bpy.context.object
    head.name=name+"_Head"
    head.data.materials.append(mats[material_key])
    relink(head,target)
    head.parent=root
    head.matrix_parent_inverse=root.matrix_world.inverted()
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
    left=mesh_obj(name+"_Wing_L",
                  [(x,y,z),(x-1.05*scale,y-.12*scale,z+.08*scale),(x-.30*scale,y,z-.08*scale)],
                  [(0,1,2)],mats["raven"],target)
    right=mesh_obj(name+"_Wing_R",
                   [(x,y,z),(x+1.05*scale,y-.12*scale,z+.08*scale),(x+.30*scale,y,z-.08*scale)],
                   [(0,1,2)],mats["raven"],target)
    for wing in (left,right):
        bpy.context.scene.cursor.location=location
        bpy.context.view_layer.objects.active=wing
        wing.select_set(True)
        bpy.ops.object.origin_set(type='ORIGIN_CURSOR',center='MEDIAN')
        wing.select_set(False)
        wing.parent=root
        wing.matrix_parent_inverse=root.matrix_world.inverted()
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

    # The newly forged dark-throne source is the complete Blackstone final-battle
    # site: throne, dais, attendant seats, banners, eight braziers, rune circuit,
    # terraces, parapets, pillars, arches, spires, bridge and abyss-edge geology.
    # Do not bury it under cinematic-only cube architecture or duplicate its props.
    append_blend(SOURCE/"dark-throne.blend", "AUTH_DarkThrone", hall,
                 location=(0,y0,0))

    # The source is authored game-Y-up and converted into Blender Z-up by wloc:
    # positive game Z points toward negative Blender Y.  The throne/rune circuit
    # therefore sits around y0-22, while the formal approach enters from y0+23.
    king_y = y0 - 19.5
    append_blend(SOURCE/"nowhere-king-sword-firmament.blend","AUTH_Firmament",hall,
                 location=(-1.15,king_y,2.55),scale=1.82,
                 rotation=(0,math.radians(-46),0))
    append_blend(SOURCE/"nowhere-king-sword-null-gate.blend","AUTH_NullGate",hall,
                 location=(1.15,king_y,2.55),scale=1.82,
                 rotation=(0,math.radians(46),0))

    # Replaceable rigging proxy until the real Dverger-derived King rig is
    # materialized for Blender.  It now occupies the actual home/throne region.
    add_king_proxy((0,king_y,.95),mats,hall)
    return hall

def append_valheim_donor_collections(path: Path):
    if not path.is_file():
        raise RuntimeError(f"Transient Valheim donor library missing: {path}")
    wanted_names=[]
    loaded_list=[]
    with bpy.data.libraries.load(str(path),link=False) as (src,dst):
        wanted_names=[name for name in src.collections if name.startswith("VALHEIM_")]
        dst.collections=wanted_names
        loaded_list=dst.collections
    loaded={col.name:col for col in loaded_list if col is not None}
    if not loaded:
        raise RuntimeError("Valheim donor library supplied no VALHEIM_* collections.")
    return loaded


def style_valheim_donor_materials(donors):
    """Apply restrained cinematic values when the headless server cannot read textures.

    Geometry stays the actual Valheim donor mesh.  These are intentionally broad
    color fields, following the reference cinematic's low-noise material language.
    """
    palettes={
        "timber_wall":((.18,.105,.055),.86),
        "timber_floor":((.16,.09,.045),.88),
        "roof_26":((.16,.13,.075),.93),
        "roof_45":((.14,.115,.065),.94),
        "door":((.12,.065,.032),.88),
        "beam":((.095,.052,.028),.90),
        "pole":((.095,.052,.028),.90),
        "stone_floor":((.24,.25,.25),.94),
        "table":((.17,.09,.045),.88),
        "bench":((.16,.085,.04),.90),
        "chair":((.15,.08,.04),.90),
        "hearth":((.19,.18,.17),.95),
        "torch":((.12,.085,.05),.88),
        "fir_tree":((.055,.10,.075),.96),
        "rock":((.20,.22,.23),.96),
        "raven":((.012,.016,.025),.90),
        "player_body":((.17,.15,.14),.86),
        "nowhere_king_chassis":((.008,.010,.014),.78),
    }
    for key,col in donors.items():
        logical=key.removeprefix("VALHEIM_")
        base,rough=palettes.get(logical,((.18,.18,.18),.9))
        seen=set()
        for obj in col.all_objects:
            if obj.type!="MESH":
                continue
            for material in obj.data.materials:
                if material is None or material in seen:
                    continue
                seen.add(material)
                material.use_nodes=True
                bs=material.node_tree.nodes.get("Principled BSDF")
                if bs is None:
                    continue
                name=(material.name+" "+obj.name).lower()
                color=base
                if logical=="fir_tree":
                    color=(.055,.105,.073) if any(token in name for token in ("leaf","needle","branch","pine")) else (.105,.060,.032)
                elif logical in {"hearth","torch"} and any(token in name for token in ("fire","ember","flame","coal")):
                    color=(.42,.10,.018)
                    if "Emission Color" in bs.inputs:
                        bs.inputs["Emission Color"].default_value=(1.0,.12,.015,1)
                        bs.inputs["Emission Strength"].default_value=1.5
                elif logical=="player_body" and any(token in name for token in ("skin","body","face","head")):
                    color=(.31,.20,.145)
                if "Base Color" in bs.inputs:
                    bs.inputs["Base Color"].default_value=(*color,1)
                if "Roughness" in bs.inputs:
                    bs.inputs["Roughness"].default_value=rough


def hide_objects_with_prefixes(prefixes):
    for obj in bpy.data.objects:
        if any(obj.name.startswith(prefix) for prefix in prefixes):
            obj.hide_render=True


def add_king_cinematic_chassis(donors,target,location,mats):
    """Temporary real-humanoid chassis until an owned King body/client Dverger donor is available."""
    player=donor_instance("CIN_King_PlayerChassis","player_body",donors,target,location,scale=1.38)
    if player is None:
        return None
    x,y,z=location
    # Broad mantle: one quiet silhouette field rather than a cube mannequin.
    verts=[
        (x-.82,y+.08,z+2.18),(x+.82,y+.08,z+2.18),
        (x+1.05,y+.24,z+.45),(x-.1,y+.48,z+.12),(x-1.05,y+.24,z+.45),
        (x-.62,y-.20,z+2.05),(x+.62,y-.20,z+2.05),
    ]
    faces=[(0,1,2,3,4),(0,5,6,1),(0,4,3,2,1)]
    cloak=mesh_obj("CIN_King_Mantle",verts,faces,mats["king"],target)
    # Crown and eye markers are the Magenheim identity carried over the real donor body.
    for i,dx in enumerate((-.24,-.12,0,.12,.24)):
        add_box(f"CIN_King_Crown_{i}",(x+dx,y,z+2.75+abs(dx)*.18),(.055,.07,.46-abs(dx)*.5),mats["blackmetal"],target)
    for i,dx in enumerate((-.095,.095)):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, radius=.035, location=(x+dx,y-.12,z+2.32))
        eye=bpy.context.object
        eye.name=f"CIN_King_Eye_{i}"
        eye.data.materials.append(mats["eye"])
        relink(eye,target)
    return player


def donor_instance(name, logical, donors, target, location, rotation=(0,0,0), scale=1.0):
    key="VALHEIM_"+logical
    col=donors.get(key)
    if col is None:
        return None
    obj=bpy.data.objects.new(name,None)
    obj.instance_type='COLLECTION'
    obj.instance_collection=col
    obj.location=location
    obj.rotation_euler=rotation
    obj.scale=(scale,scale,scale)
    target.objects.link(obj)
    return obj


def remove_named_prefixes(collection_, prefixes):
    doomed=[o for o in tuple(collection_.objects) if any(o.name.startswith(prefix) for prefix in prefixes)]
    for obj in doomed:
        bpy.data.objects.remove(obj,do_unlink=True)


def build_donor_house(prefix,x,y,width,depth,wall_levels,donors,target,scale=1.0):
    # Valheim build pieces are approximately two-metre modules.  We preserve
    # their authored material/mesh identity and use placement logic only.
    step=2.0*scale
    h=2.0*wall_levels*scale
    nx=max(2,int(round(width/step)))
    ny=max(2,int(round(depth/step)))
    half_w=(nx-1)*step/2
    half_d=(ny-1)*step/2
    for level in range(wall_levels):
        z=(1.0+2.0*level)*scale
        for i in range(nx):
            dx=-half_w+i*step
            if not (level==0 and i==nx//2):
                donor_instance(f"{prefix}_Front_{level}_{i}","timber_wall",donors,target,(x+dx,y-half_d,z),scale=scale)
            donor_instance(f"{prefix}_Back_{level}_{i}","timber_wall",donors,target,
                           (x+dx,y+half_d,z),(0,0,math.pi),scale)
        for j in range(1,ny-1):
            dy=-half_d+j*step
            donor_instance(f"{prefix}_Left_{level}_{j}","timber_wall",donors,target,
                           (x-half_w-step/2,y+dy,z),(0,0,math.pi/2),scale)
            donor_instance(f"{prefix}_Right_{level}_{j}","timber_wall",donors,target,
                           (x+half_w+step/2,y+dy,z),(0,0,-math.pi/2),scale)
    donor_instance(prefix+"_Door","door",donors,target,(x,y-half_d-.03,1.0*scale),scale=scale)

    # Roof donor already carries the Valheim pitch; run two opposing slopes.
    for j in range(ny):
        dy=-half_d+j*step
        for side in (-1,1):
            rx=x+side*(half_w*.52)
            donor_instance(f"{prefix}_Roof_{side}_{j}","roof_26",donors,target,
                           (rx,y+dy,h+.55*scale),(0,0,0 if side<0 else math.pi),scale)
    for sx in (-1,1):
        for sy in (-1,1):
            donor_instance(f"{prefix}_Pole_{sx}_{sy}","pole",donors,target,
                           (x+sx*half_w,y+sy*half_d,h/2),(0,0,0),scale)


def add_valheim_donor_sets(mats,root,donor_library:Path):
    donors=append_valheim_donor_collections(donor_library)
    style_valheim_donor_materials(donors)
    surface=bpy.data.collections.get("CIN_SurfaceVillage")
    hall=bpy.data.collections.get("CIN_GreatHallInterior")
    if surface is None or hall is None:
        raise RuntimeError("Base cinematic surface collections do not exist.")

    # Retain terrain, road, atmosphere and rigging anchors, but replace the
    # synthetic architecture/tree stand-ins with real runtime-extracted Valheim
    # meshes.  These objects never become repository assets.
    remove_named_prefixes(surface,("Village_GreatHall_","Village_House_","Village_Pine_"))
    remove_named_prefixes(hall,("HallInterior_Wall_","HallInterior_Post_","HallInterior_Crossbeam_",
                                "HallInterior_Bench_","HallInterior_Hearth"))

    v=collection("CIN_ValheimVillage",root)
    build_donor_house("VH_GreatHall",7.5,8.0,8.0,12.0,2,donors,v,1.0)
    for idx,(x,y,s) in enumerate(((-12,5,.86),(-18,-8,.78),(16,-6,.82),(22,10,.72),(-3,18,.72))):
        build_donor_house(f"VH_House_{idx}",x,y,5.5,7.0,1,donors,v,s)

    for i,(x,y,s) in enumerate(((-28,-18,1.1),(-24,18,1.0),(31,-10,.95),(-38,4,.95),
                                (-45,18,.9),(39,24,.8),(45,-24,.85),(-50,-24,.82),(34,34,.78))):
        donor_instance(f"VH_Fir_{i}","fir_tree",donors,v,(x,y,0),scale=s)

    h=collection("CIN_ValheimHall",root)
    x0=HALL_X
    # Wall/pole shell: camera-facing end remains open enough for readable depth.
    for yi in (-11,-7,-3,1,5,9,13):
        for sx in (-1,1):
            donor_instance(f"VHI_Wall_{sx}_{yi}","timber_wall",donors,h,(x0+sx*7.0,yi,2.0),(0,0,math.pi/2 if sx<0 else -math.pi/2),1.0)
            donor_instance(f"VHI_Pole_{sx}_{yi}","pole",donors,h,(x0+sx*6.7,yi,3.0),scale=1.0)
    for xi in (-6,-4,-2,0,2,4,6):
        donor_instance(f"VHI_Rear_{xi}","timber_wall",donors,h,(x0+xi,14.0,2.0),(0,0,math.pi),1.0)
    donor_instance("VHI_Hearth","hearth",donors,h,(x0,3,.05),scale=1.0)
    for sx in (-1,1):
        for yi in (-5,0,5,10):
            donor_instance(f"VHI_Bench_{sx}_{yi}","bench",donors,h,(x0+sx*4.2,yi,.1),(0,0,0 if sx<0 else math.pi),1.0)
    donor_instance("VHI_Table","table",donors,h,(x0,7,.1),scale=1.0)

    # Replace box mannequins with the actual readable Valheim Player donor for
    # blocking.  Equipment and final character-specific meshes are a later rig pass.
    if "VALHEIM_player_body" in donors:
        hide_objects_with_prefixes(("HERO_A_","HERO_B_","HERO_C_","HALL_HERO_A_","HALL_HERO_B_","HALL_HERO_C_","HALL_VILLAGER_"))
        for name,loc,rot,scale in (
            ("VH_Hero_A",(-2.2,-26.0,.15),0.06,.96),
            ("VH_Hero_B",(0.0,-27.0,.15),-.04,.88),
            ("VH_Hero_C",(2.1,-26.4,.15),.03,.92),
        ):
            donor_instance(name,"player_body",donors,v,loc,(0,0,rot),scale)
        for name,dx,dy,rot,scale in (
            ("VHI_Hero_A",-1.6,-7,.02,.96),("VHI_Hero_B",0,-7.5,-.03,.88),("VHI_Hero_C",1.6,-7,.04,.92),
            ("VHI_Villager_0",-5,-1,.10,.82),("VHI_Villager_1",5,-1,-.10,.82),
            ("VHI_Villager_2",-5,5,.08,.80),("VHI_Villager_3",5,5,-.08,.80),
            ("VHI_Villager_4",-4,10,.05,.78),("VHI_Villager_5",4,10,-.05,.78),
        ):
            donor_instance(name,"player_body",donors,h,(x0+dx,dy,.05),(0,0,rot),scale)

    raven=bpy.data.objects.get("RAVEN_ROOT")
    if raven is not None and "VALHEIM_raven" in donors:
        hide_objects_with_prefixes(("RAVEN_",))
        donor_instance("VH_Raven","raven",donors,v,(-7,8,9),(0,0,-.35),.82)

    # Dedicated-server Dverger geometry is non-readable.  Until the client donor
    # or owned final King body is admitted, use the readable Valheim player mesh
    # under Magenheim's mantle/crown rather than the old block mannequin.
    if "VALHEIM_player_body" in donors:
        hide_objects_with_prefixes(("NOWHERE_KING_",))
        actors=collection("CIN_ValheimThroneActors",root)
        add_king_cinematic_chassis(donors,actors,(0,THRONE_Y-19.5,.10),mats)

    bpy.context.scene["valheim_cinematic_donors"]=len(donors)
    return donors


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
        # Blackstone approach is at +Y; throne/King are at -Y after the
        # Magenheim game-space -> Blender-space conversion.
        ("SHOT_06_THRONE_REVEAL_CAM", (19,y0+30,6.4), (0,y0-17.5,3.2), 44),
        ("SHOT_07_KING_AWAKENS_CAM", (-9.5,y0-3.0,4.4), (0,y0-19.5,2.7), 58),
        ("SHOT_08_LAST_ARGUMENT_CAM", (0,y0-3.0,4.35), (0,y0-19.5,2.8), 58),
    )
    created = {}
    for name, loc, tgt, lens in specs:
        created[name] = camera(name, loc, tgt, lens, cams)
    return created

def add_lighting(root, mats):
    surface_lights = collection("CIN_SurfaceLights", root)
    throne_lights = collection("CIN_ThroneLights", root)

    sun_data = bpy.data.lights.new("Surface_Sun_Data", type='SUN')
    sun_data.energy = 3.0
    sun_data.color = (0.74,0.80,0.92)
    sun = bpy.data.objects.new("Surface_Sun", sun_data)
    sun.rotation_euler = (math.radians(31), math.radians(-18), math.radians(-34))
    surface_lights.objects.link(sun)

    # Cold overhead key to expose broad Blackstone forms without flattening them.
    key_data = bpy.data.lights.new("Throne_Key_Data", type='AREA')
    key_data.energy = 1850
    key_data.shape = 'DISK'
    key_data.size = 16
    key_data.color = (0.18,0.27,0.48)
    key = bpy.data.objects.new("Throne_Key", key_data)
    key.location = (0,THRONE_Y-2,15)
    direction=Vector((0,THRONE_Y-20,3.5))-key.location
    key.rotation_euler=direction.to_track_quat('-Z','Y').to_euler()
    throne_lights.objects.link(key)

    # Materialized equivalents of the authored runtime lights.  The Blackstone
    # .blend intentionally stores runtime light metadata rather than Blender
    # lamps, so the cinematic scene must instantiate those sources explicitly.
    brazier_game = (
        (-11,3.4,-12),(11,3.4,-12),(-19,3.4,-1),(19,3.4,-1),
        (-15,3.6,11),(15,3.6,11),(-8,4.6,18),(8,4.6,18),
    )
    for i,(x,up,z) in enumerate(brazier_game,1):
        data=bpy.data.lights.new(f"Throne_Brazier_{i}_Data",type='POINT')
        data.color=(1.0,.19,.035)
        data.energy=1050
        data.shadow_soft_size=1.35
        obj=bpy.data.objects.new(f"Throne_Brazier_{i}",data)
        obj.location=(x,THRONE_Y-z,up)
        throne_lights.objects.link(obj)

    for i in range(8):
        ang=i*math.tau/8
        x=math.sin(ang)*7.4
        game_z=22+math.cos(ang)*3.2
        data=bpy.data.lights.new(f"Throne_Rune_{i+1}_Data",type='POINT')
        data.color=(.34,.055,.62)
        data.energy=240
        data.shadow_soft_size=.55
        obj=bpy.data.objects.new(f"Throne_Rune_{i+1}",data)
        obj.location=(x,THRONE_Y-game_z,2.6)
        throne_lights.objects.link(obj)

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



def _key(obj, frame, data_path, value=None, index=-1):
    if value is not None:
        if data_path == "location":
            obj.location = value
        elif data_path == "rotation_euler":
            obj.rotation_euler = value
        elif data_path == "scale":
            obj.scale = value
        elif data_path == "lens":
            obj.data.lens = value
        else:
            raise ValueError("Unsupported key path: " + data_path)
    owner = obj.data if data_path == "lens" else obj
    owner.keyframe_insert(data_path=data_path, frame=frame, index=index)


def _set_action_interpolation(idblock, interpolation="BEZIER"):
    """Best-effort interpolation tuning across Blender animation APIs.

    Blender 5 migrated Actions away from the legacy action.fcurves collection.
    Keyframes remain valid without this cosmetic tuning, so an unavailable curve
    iterator must never invalidate the authored motion itself.
    """
    data=getattr(idblock,"animation_data",None)
    action=getattr(data,"action",None) if data is not None else None
    if action is None:
        return
    curves=[]
    legacy=getattr(action,"fcurves",None)
    if legacy is not None:
        curves.extend(list(legacy))
    # Blender 5 layered Action API.  Introspect rather than depending on a
    # private/deprecated attribute; if no public channel bag is exposed, leave
    # Blender's default interpolation intact.
    slot=getattr(data,"action_slot",None)
    for layer in getattr(action,"layers",()):
        for strip in getattr(layer,"strips",()):
            bag=None
            channelbag=getattr(strip,"channelbag",None)
            if callable(channelbag) and slot is not None:
                try:
                    bag=channelbag(slot)
                except Exception:
                    bag=None
            if bag is not None:
                curves.extend(list(getattr(bag,"fcurves",())))
    for curve in curves:
        for point in getattr(curve,"keyframe_points",()):
            point.interpolation = interpolation
            if interpolation == "BEZIER":
                point.handle_left_type = "AUTO_CLAMPED"
                point.handle_right_type = "AUTO_CLAMPED"


def _look_at(cam, frame, location, target, lens=None):
    cam.location = location
    direction = Vector(target) - Vector(location)
    cam.rotation_euler = direction.to_track_quat('-Z','Y').to_euler()
    cam.keyframe_insert(data_path="location", frame=frame)
    cam.keyframe_insert(data_path="rotation_euler", frame=frame)
    if lens is not None:
        cam.data.lens = lens
        cam.data.keyframe_insert(data_path="lens", frame=frame)


def _walk_cycle(root_name, start, end, start_loc, end_loc, phase=0, stride=24, swing_deg=22):
    """Articulated walk with root motion, knee flexion and approximate foot leveling.

    Stride is a complete left-to-left gait cycle. At the default 24 frames this
    is one second at picture rate: a natural two-step cadence rather than the
    old half-second pinwheel gait.
    """
    root=bpy.data.objects.get(root_name)
    if root is None:
        return
    prefix=root_name.removesuffix("_ROOT")
    thigh_l=bpy.data.objects.get(prefix+"_Leg_-1")
    thigh_r=bpy.data.objects.get(prefix+"_Leg_+1")
    shin_l=bpy.data.objects.get(prefix+"_Shin_-1")
    shin_r=bpy.data.objects.get(prefix+"_Shin_+1")
    foot_l=bpy.data.objects.get(prefix+"_Foot_-1")
    foot_r=bpy.data.objects.get(prefix+"_Foot_+1")
    arm_l=bpy.data.objects.get(prefix+"_Arm_-1")
    arm_r=bpy.data.objects.get(prefix+"_Arm_+1")
    fore_l=bpy.data.objects.get(prefix+"_Forearm_-1")
    fore_r=bpy.data.objects.get(prefix+"_Forearm_+1")

    amp=math.radians(swing_deg)
    quarter=max(1,stride//4)
    frames=list(range(start,end+1,quarter))
    if frames[-1]!=end:
        frames.append(end)

    for frame in frames:
        t=(frame-start)/max(1,end-start)
        gait=math.tau*((frame-start+phase)/stride)
        left_thigh=amp*math.cos(gait)
        right_thigh=-left_thigh
        left_knee=math.radians(4+31*max(0.0,-math.sin(gait)))
        right_knee=math.radians(4+31*max(0.0, math.sin(gait)))
        left_arm=-.62*left_thigh
        right_arm=-.62*right_thigh

        poses=(
            (thigh_l,left_thigh),(thigh_r,right_thigh),
            (shin_l,-left_knee),(shin_r,-right_knee),
            (arm_l,left_arm),(arm_r,right_arm),
            (fore_l,math.radians(-10)-.18*left_arm),
            (fore_r,math.radians(-10)-.18*right_arm),
            (foot_l,-.70*(left_thigh-left_knee)),
            (foot_r,-.70*(right_thigh-right_knee)),
        )
        for obj,angle in poses:
            if obj is None:
                continue
            obj.rotation_mode='XYZ'
            obj.rotation_euler=(angle,0,0)
            obj.keyframe_insert(data_path="rotation_euler",frame=frame)

        x=start_loc[0]+(end_loc[0]-start_loc[0])*t
        y=start_loc[1]+(end_loc[1]-start_loc[1])*t
        base_z=start_loc[2]+(end_loc[2]-start_loc[2])*t
        bob=.028*(1-math.cos(2*gait))
        root.location=(x,y,base_z+bob)
        root.keyframe_insert(data_path="location",frame=frame)

    root.location=end_loc
    root.keyframe_insert(data_path="location",frame=end)

def _pose_limb(name, frame, rotation):
    obj=bpy.data.objects.get(name)
    if obj is None:
        return
    obj.rotation_mode='XYZ'
    obj.rotation_euler=rotation
    obj.keyframe_insert(data_path="rotation_euler",frame=frame)


def _camera_shake(cam, center_frame, base_location, target, offsets):
    # Damped whole-camera impulse caused by the world tremor.  All offsets are
    # in metres and return to the same physical camera position.
    for delta,off in offsets:
        frame=center_frame+delta
        loc=(base_location[0]+off[0],base_location[1]+off[1],base_location[2]+off[2])
        _look_at(cam,frame,loc,target)


def _animate_light_energy(obj_name, keys):
    obj=bpy.data.objects.get(obj_name)
    if obj is None or obj.type!="LIGHT":
        return
    for frame,value in keys:
        obj.data.energy=value
        obj.data.keyframe_insert(data_path="energy",frame=frame)


def bind_camera_cuts(scene,cameras):
    marker_by_name={m.name:m for m in scene.timeline_markers}
    for shot,start,_ in SHOT_SPECS:
        marker=marker_by_name.get(shot)
        cam=cameras.get(shot+"_CAM")
        if marker is None or cam is None:
            raise RuntimeError(f"Cannot bind cinematic camera cut {shot}")
        marker.camera=cam
    scene.camera=cameras["SHOT_01_RETURN_CAM"]


def animate_scene(scene,cameras):
    """Author object-space motion for the eight-shot cinematic.

    Every motion is attached to a scene object, limb, camera, light or weapon.
    There is no image warping and no arbitrary moving screen region.
    """
    # Shot 1 — three heroes walk into the village on actual world trajectories.
    _walk_cycle("HERO_A_ROOT",1,72,(-2.2,-29.4,.15),(-2.0,-24.7,.15),phase=0,stride=24)
    _walk_cycle("HERO_B_ROOT",1,72,(0,-30.2,.15),(.1,-25.5,.15),phase=6,stride=24)
    _walk_cycle("HERO_C_ROOT",1,72,(2.1,-29.6,.15),(2.2,-24.9,.15),phase=12,stride=24)
    _look_at(cameras["SHOT_01_RETURN_CAM"],1,(0,-42,5.5),(0,-22,2.1),50)
    _look_at(cameras["SHOT_01_RETURN_CAM"],72,(1.0,-35.4,5.0),(.2,-16.5,2.3),48)

    # Shot 2 — short homecoming advance, then weight settles; villagers turn
    # toward the entrants instead of twitching in place.
    for idx,x in enumerate((-1.6,0,1.6)):
        root=bpy.data.objects.get(f"HALL_HERO_{chr(65+idx)}_ROOT")
        if root:
            start=(HALL_X+x,-9.0-(.35 if idx==1 else 0),.05)
            end=(HALL_X+x,-6.3-(.25 if idx==1 else 0),.05)
            _walk_cycle(root.name,73,112,start,end,phase=idx*5,stride=20,swing_deg=19)
            root.location=end
            root.keyframe_insert(data_path="location",frame=144)
    for i in range(6):
        root=bpy.data.objects.get(f"HALL_VILLAGER_{i}_ROOT")
        if root:
            root.rotation_mode='XYZ'
            root.rotation_euler=(0,0,0)
            root.keyframe_insert(data_path="rotation_euler",frame=73)
            turn=math.radians(14 if i%2==0 else -14)
            root.rotation_euler=(0,0,turn)
            root.keyframe_insert(data_path="rotation_euler",frame=116)
            root.keyframe_insert(data_path="rotation_euler",frame=144)
    _look_at(cameras["SHOT_02_HOMECOMING_CAM"],73,(HALL_X,-12.5,3.1),(HALL_X,2.5,2.4),46)
    _look_at(cameras["SHOT_02_HOMECOMING_CAM"],144,(HALL_X+.4,-10.2,3.0),(HALL_X,5.0,2.5),49)

    # Shot 3 — stillness is intentional.  Only the camera eases across the
    # landscape; characters do not perform fake idle motion.
    _look_at(cameras["SHOT_03_PEACE_CAM"],145,(-31,-37,13.5),(2,6,3.2),48)
    _look_at(cameras["SHOT_03_PEACE_CAM"],216,(-27.8,-34.5,12.9),(3.5,7.0,3.1),52)

    # Shot 4 — physical tremor followed by human reaction.
    cam4=cameras["SHOT_04_RUMBLE_CAM"]
    base=(8,-30,3.0); target=(0,-17,2.5)
    _look_at(cam4,217,base,target,58)
    _camera_shake(cam4,242,base,target,(
        (-8,(0,0,0)),(-5,(.16,0,.075)),(-2,(-.22,.015,-.095)),
        (1,(.18,-.012,.080)),(4,(-.12,.010,-.055)),(7,(.065,0,.028)),(11,(0,0,0)),
    ))
    _look_at(cam4,288,(7.4,-28.7,3.15),(0,-16,2.6),60)
    for i,name in enumerate(("HERO_A_ROOT","HERO_B_ROOT","HERO_C_ROOT")):
        root=bpy.data.objects.get(name)
        if root:
            root.rotation_mode='XYZ'
            base_rot=(0,0,0)
            root.rotation_euler=base_rot
            root.keyframe_insert(data_path="rotation_euler",frame=230)
            # Brace into the impulse before turning toward its source.
            root.rotation_euler=(math.radians((3,-2,4)[i]),math.radians((-2,3,-3)[i]),0)
            root.keyframe_insert(data_path="rotation_euler",frame=244)
            root.rotation_euler=(0,0,math.radians((-18,7,22)[i]))
            root.keyframe_insert(data_path="rotation_euler",frame=270)
    for prefix in ("HERO_A","HERO_B","HERO_C"):
        _pose_limb(prefix+"_Leg_-1",238,(math.radians(3),0,0))
        _pose_limb(prefix+"_Leg_+1",238,(math.radians(-2),0,0))
        _pose_limb(prefix+"_Shin_-1",244,(math.radians(-18),0,0))
        _pose_limb(prefix+"_Shin_+1",244,(math.radians(-16),0,0))
        _pose_limb(prefix+"_Shin_-1",260,(math.radians(-4),0,0))
        _pose_limb(prefix+"_Shin_+1",260,(math.radians(-4),0,0))
    # Lift the heads slightly toward the source of the disturbance.
    for prefix in ("HERO_A","HERO_B","HERO_C"):
        head=bpy.data.objects.get(prefix+"_Head")
        if head:
            head.rotation_mode='XYZ'
            head.rotation_euler=(0,0,0); head.keyframe_insert(data_path="rotation_euler",frame=230)
            head.rotation_euler=(math.radians(-10),0,0); head.keyframe_insert(data_path="rotation_euler",frame=268)

    # Shot 5 — raven crosses actual 3D space. Left/right wings flap around
    # real body pivots until the final raven rig replaces the blocking proxy.
    raven=bpy.data.objects.get("RAVEN_ROOT")
    if raven:
        raven.location=(-13,1,7.0); raven.keyframe_insert(data_path="location",frame=289)
        raven.location=(-8,7,9.4); raven.keyframe_insert(data_path="location",frame=325)
        raven.location=(-3,13,10.0); raven.keyframe_insert(data_path="location",frame=360)
        raven.rotation_mode='XYZ'
        raven.rotation_euler=(0,0,math.radians(-20)); raven.keyframe_insert(data_path="rotation_euler",frame=289)
        raven.rotation_euler=(0,0,math.radians(8)); raven.keyframe_insert(data_path="rotation_euler",frame=360)
    wing_l=bpy.data.objects.get("RAVEN_Wing_L")
    wing_r=bpy.data.objects.get("RAVEN_Wing_R")
    if wing_l and wing_r:
        for frame in range(289,361,6):
            angle=math.radians(34 if ((frame-289)//6)%2==0 else -24)
            wing_l.rotation_mode='XYZ'; wing_r.rotation_mode='XYZ'
            wing_l.rotation_euler=(0,angle,0)
            wing_r.rotation_euler=(0,-angle,0)
            wing_l.keyframe_insert(data_path="rotation_euler",frame=frame)
            wing_r.keyframe_insert(data_path="rotation_euler",frame=frame)
    _look_at(cameras["SHOT_05_OMEN_CAM"],289,(-13,1,10.8),(-8,7,9.0),76)
    _look_at(cameras["SHOT_05_OMEN_CAM"],360,(-7,5,11.3),(-3,13,10.0),82)

    # Shot 6 — a real dolly through the Blackstone approach.  The monumental
    # set supplies parallax; the camera does not zoom a still image.
    _look_at(cameras["SHOT_06_THRONE_REVEAL_CAM"],361,(19,THRONE_Y+30,6.4),(0,THRONE_Y-17.5,3.2),44)
    _look_at(cameras["SHOT_06_THRONE_REVEAL_CAM"],444,(10.5,THRONE_Y+13.5,5.3),(0,THRONE_Y-19.0,3.0),49)

    # Shot 7 — King rises on a coherent body trajectory, torso straightens,
    # head lifts, and arms separate with delayed follow-through.
    king=bpy.data.objects.get("NOWHERE_KING_ROOT")
    if king:
        king.location=(0,THRONE_Y-19.5,-.38); king.keyframe_insert(data_path="location",frame=445)
        king.location=(0,THRONE_Y-19.5,.18); king.keyframe_insert(data_path="location",frame=480)
        king.location=(0,THRONE_Y-19.5,.95); king.keyframe_insert(data_path="location",frame=522)
        king.keyframe_insert(data_path="location",frame=540)
    torso=bpy.data.objects.get("NOWHERE_KING_Torso")
    if torso:
        torso.rotation_mode='XYZ'
        torso.rotation_euler=(math.radians(34),0,0); torso.keyframe_insert(data_path="rotation_euler",frame=445)
        torso.rotation_euler=(0,0,0); torso.keyframe_insert(data_path="rotation_euler",frame=520)
    head=bpy.data.objects.get("NOWHERE_KING_Head")
    if head:
        head.rotation_mode='XYZ'
        head.rotation_euler=(math.radians(32),0,0); head.keyframe_insert(data_path="rotation_euler",frame=445)
        head.rotation_euler=(math.radians(-3),0,0); head.keyframe_insert(data_path="rotation_euler",frame=528)
    _pose_limb("NOWHERE_KING_Arm_-1",445,(math.radians(12),0,math.radians(-8)))
    _pose_limb("NOWHERE_KING_Arm_+1",445,(math.radians(12),0,math.radians(8)))
    _pose_limb("NOWHERE_KING_Forearm_-1",445,(math.radians(-38),0,0))
    _pose_limb("NOWHERE_KING_Forearm_+1",445,(math.radians(-38),0,0))
    _pose_limb("NOWHERE_KING_Arm_-1",530,(math.radians(-8),0,math.radians(-18)))
    _pose_limb("NOWHERE_KING_Arm_+1",530,(math.radians(-8),0,math.radians(18)))
    _pose_limb("NOWHERE_KING_Forearm_-1",530,(math.radians(-18),0,0))
    _pose_limb("NOWHERE_KING_Forearm_+1",530,(math.radians(-18),0,0))
    _look_at(cameras["SHOT_07_KING_AWAKENS_CAM"],445,(-9.5,THRONE_Y-3.0,4.4),(0,THRONE_Y-19.5,2.2),58)
    _look_at(cameras["SHOT_07_KING_AWAKENS_CAM"],540,(-7.4,THRONE_Y-6.0,4.35),(0,THRONE_Y-19.5,2.9),62)

    # Shot 8 — both Last Argument blades start physically inside the mantle
    # volume and are drawn outward by the King's arms.  Nothing pops on.
    sword_specs=(
        ("AUTH_Firmament",-1,math.radians(-48)),
        ("AUTH_NullGate",1,math.radians(48)),
    )
    for name,side,final_y in sword_specs:
        sword=bpy.data.objects.get(name)
        if sword is None:
            continue
        sword.rotation_mode='XYZ'
        sword.scale=(1.82,1.82,1.82)
        sword.keyframe_insert(data_path="scale",frame=541)
        sword.keyframe_insert(data_path="scale",frame=672)
        # Grip begins beneath the mantle with the blade nearly vertical.
        sword.location=(side*.18,THRONE_Y-19.34,2.32)
        sword.rotation_euler=(0,side*math.radians(8),0)
        sword.keyframe_insert(data_path="location",frame=541)
        sword.keyframe_insert(data_path="rotation_euler",frame=541)
        # Hand clears the cloak before the blade fans outward.
        sword.location=(side*.52,THRONE_Y-19.42,2.48)
        sword.rotation_euler=(0,side*math.radians(22),0)
        sword.keyframe_insert(data_path="location",frame=578)
        sword.keyframe_insert(data_path="rotation_euler",frame=578)
        sword.location=(side*1.18,THRONE_Y-19.50,2.62)
        sword.rotation_euler=(0,final_y,0)
        sword.keyframe_insert(data_path="location",frame=624)
        sword.keyframe_insert(data_path="rotation_euler",frame=624)
        sword.location=(side*1.30,THRONE_Y-19.54,2.68)
        sword.rotation_euler=(0,side*math.radians(52),0)
        sword.keyframe_insert(data_path="location",frame=672)
        sword.keyframe_insert(data_path="rotation_euler",frame=672)
    _pose_limb("NOWHERE_KING_Arm_-1",541,(math.radians(-8),0,math.radians(-18)))
    _pose_limb("NOWHERE_KING_Arm_+1",541,(math.radians(-8),0,math.radians(18)))
    _pose_limb("NOWHERE_KING_Forearm_-1",541,(math.radians(-42),0,0))
    _pose_limb("NOWHERE_KING_Forearm_+1",541,(math.radians(-42),0,0))
    _pose_limb("NOWHERE_KING_Arm_-1",622,(math.radians(-42),math.radians(-10),math.radians(-48)))
    _pose_limb("NOWHERE_KING_Arm_+1",622,(math.radians(-42),math.radians(10),math.radians(48)))
    _pose_limb("NOWHERE_KING_Forearm_-1",622,(math.radians(-14),0,math.radians(-8)))
    _pose_limb("NOWHERE_KING_Forearm_+1",622,(math.radians(-14),0,math.radians(8)))
    _pose_limb("NOWHERE_KING_Arm_-1",672,(math.radians(-36),math.radians(-8),math.radians(-42)))
    _pose_limb("NOWHERE_KING_Arm_+1",672,(math.radians(-36),math.radians(8),math.radians(42)))
    _pose_limb("NOWHERE_KING_Forearm_-1",672,(math.radians(-10),0,math.radians(-6)))
    _pose_limb("NOWHERE_KING_Forearm_+1",672,(math.radians(-10),0,math.radians(6)))
    _look_at(cameras["SHOT_08_LAST_ARGUMENT_CAM"],541,(0,THRONE_Y-3.0,4.35),(0,THRONE_Y-19.5,2.8),58)
    _look_at(cameras["SHOT_08_LAST_ARGUMENT_CAM"],672,(0,THRONE_Y-6.5,4.15),(0,THRONE_Y-19.5,2.9),62)

    # Coherent light transition: surface sun owns the upper-world shots;
    # Blackstone sources own the underworld shots.
    _animate_light_energy("Surface_Sun",((1,3.0),(360,3.0),(361,0.0),(672,0.0)))
    _animate_light_energy("Throne_Key",((1,0.0),(360,0.0),(361,1850.0),(672,1850.0)))
    for i in range(1,9):
        _animate_light_energy(f"Throne_Brazier_{i}",((1,0.0),(360,0.0),(361,1050.0),(672,1050.0)))
        _animate_light_energy(f"Throne_Rune_{i}",((1,0.0),(360,0.0),(361,240.0),(672,240.0)))

    # World background transitions on the cut, not continuously across shots.
    bg=scene.world.node_tree.nodes.get("Background") if scene.world and scene.world.use_nodes else None
    if bg:
        bg.inputs["Color"].default_value=(.16,.22,.31,1)
        bg.inputs["Color"].keyframe_insert("default_value",frame=1)
        bg.inputs["Color"].keyframe_insert("default_value",frame=360)
        bg.inputs["Strength"].default_value=.72
        bg.inputs["Strength"].keyframe_insert("default_value",frame=1)
        bg.inputs["Strength"].keyframe_insert("default_value",frame=360)
        bg.inputs["Color"].default_value=(.004,.006,.014,1)
        bg.inputs["Color"].keyframe_insert("default_value",frame=361)
        bg.inputs["Color"].keyframe_insert("default_value",frame=672)
        bg.inputs["Strength"].default_value=.11
        bg.inputs["Strength"].keyframe_insert("default_value",frame=361)
        bg.inputs["Strength"].keyframe_insert("default_value",frame=672)

    # Apply motion interpolation deliberately.  Translation/camera moves ease;
    # the short tremor remains linear to preserve its impulse.
    for obj in bpy.data.objects:
        _set_action_interpolation(obj,"BEZIER")
        if obj.type=="CAMERA":
            _set_action_interpolation(obj.data,"BEZIER")
        if obj.type=="LIGHT":
            _set_action_interpolation(obj.data,"CONSTANT")
    # Camera-shake keys are deliberately dense and damped; on Blender versions
    # where layered Action F-curves are public the helper above adjusts them,
    # otherwise the authored samples remain the source of truth.
    bind_camera_cuts(scene,cameras)
    scene["motion_contract"]="object-space-keyframed-v1"
    scene["motion_no_image_warp"]=True

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
    s.view_settings.exposure = .35
    s["magenheim_cinematic_id"] = "peace-was-only-the-beginning"
    s["style_negative_space"] = "simple broad fields; detail only where story requires"
    frame_markers(s)
    return s


def set_shot_visibility(shot_name):
    groups={
        "CIN_SurfaceVillage": shot_name in {"SHOT_01_RETURN","SHOT_03_PEACE","SHOT_04_RUMBLE","SHOT_05_OMEN"},
        "CIN_GreatHallInterior": shot_name=="SHOT_02_HOMECOMING",
        "CIN_DarkThrone": shot_name in {"SHOT_06_THRONE_REVEAL","SHOT_07_KING_AWAKENS","SHOT_08_LAST_ARGUMENT"},
        "CIN_SurfaceLights": shot_name in {"SHOT_01_RETURN","SHOT_03_PEACE","SHOT_04_RUMBLE","SHOT_05_OMEN"},
        "CIN_ThroneLights": shot_name in {"SHOT_06_THRONE_REVEAL","SHOT_07_KING_AWAKENS","SHOT_08_LAST_ARGUMENT"},
        "CIN_ValheimVillage": shot_name in {"SHOT_01_RETURN","SHOT_03_PEACE","SHOT_04_RUMBLE","SHOT_05_OMEN"},
        "CIN_ValheimHall": shot_name=="SHOT_02_HOMECOMING",
        "CIN_ValheimThroneActors": shot_name in {"SHOT_06_THRONE_REVEAL","SHOT_07_KING_AWAKENS","SHOT_08_LAST_ARGUMENT"},
    }
    for name,visible in groups.items():
        col=bpy.data.collections.get(name)
        if col is not None:
            col.hide_render=not visible
    bg=bpy.context.scene.world.node_tree.nodes.get("Background") if bpy.context.scene.world and bpy.context.scene.world.use_nodes else None
    if bg is not None:
        if shot_name in {"SHOT_06_THRONE_REVEAL","SHOT_07_KING_AWAKENS","SHOT_08_LAST_ARGUMENT"}:
            bg.inputs["Color"].default_value=(.004,.006,.014,1)
            bg.inputs["Strength"].default_value=.11
        else:
            bg.inputs["Color"].default_value=(.16,.22,.31,1)
            bg.inputs["Strength"].default_value=.72


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
        set_shot_visibility(shot)
        scene.camera = cameras[cam_name]
        scene.frame_set(frame_by_shot[shot])
        scene.render.filepath = str(preview_dir / f"{shot.lower()}.png")
        bpy.ops.render.render(write_still=True)



def render_motion_frames(scene, output_dir: Path):
    """Render a lightweight silent 28-second motion proof as PNG frames.

    Blender 5 no longer exposes FFMPEG as an image output format on this build,
    so Actions encodes these frames with a pinned Python ffmpeg wheel afterward.
    Sampling every second 24-fps frame at 12 output fps preserves the 28-second
    duration while keeping the proof inexpensive.
    """
    output_dir.mkdir(parents=True, exist_ok=True)
    for stale in output_dir.glob("*.png"):
        stale.unlink()
    scene.render.engine='CYCLES'
    scene.cycles.samples=2
    scene.cycles.use_denoising=False
    scene.render.resolution_x=480
    scene.render.resolution_y=270
    scene.render.resolution_percentage=100
    scene.render.fps=12
    scene.frame_step=2
    scene.render.image_settings.file_format='PNG'
    scene.render.filepath=str(output_dir/"motion_")
    scene.camera=bpy.data.objects["SHOT_01_RETURN_CAM"]
    print(f"CINEMATIC MOTION FRAMES render -> {output_dir}",flush=True)
    bpy.ops.render.render(animation=True)
    frames=sorted(output_dir.glob("*.png"))
    if len(frames) < 300:
        raise RuntimeError(f"Motion proof rendered too few frames: {len(frames)}")
    print(f"CINEMATIC MOTION FRAMES wrote {len(frames)} frames",flush=True)


def verify():
    scene = bpy.context.scene
    required_objects = {
        "AUTH_DarkThrone", "AUTH_Firmament", "AUTH_NullGate",
        "NOWHERE_KING_ROOT", "HERO_A_ROOT", "HERO_B_ROOT", "HERO_C_ROOT", "RAVEN_ROOT",
        "SHOT_01_RETURN_CAM", "SHOT_02_HOMECOMING_CAM", "SHOT_06_THRONE_REVEAL_CAM", "SHOT_08_LAST_ARGUMENT_CAM",
        "HallInterior_Floor", "AUTH_DarkThrone",
    }
    missing = sorted(name for name in required_objects if name not in bpy.data.objects)
    if scene.get("valheim_cinematic_donors",0):
        if bpy.data.collections.get("CIN_ValheimVillage") is None or bpy.data.collections.get("CIN_ValheimHall") is None:
            missing.append("CIN_ValheimVillage/CIN_ValheimHall")
    elif "Village_GreatHall_TimberBody" not in bpy.data.objects:
        missing.append("Village_GreatHall_TimberBody")
    if missing:
        raise RuntimeError("Cinematic environment missing required objects: " + ", ".join(missing))
    marker_names = {m.name for m in scene.timeline_markers}
    missing_markers = sorted(name for name,_,_ in SHOT_SPECS if name not in marker_names)
    if missing_markers:
        raise RuntimeError("Cinematic environment missing shot markers: " + ", ".join(missing_markers))
    if scene.get("magenheim_cinematic_id") != "peace-was-only-the-beginning":
        raise RuntimeError("Cinematic identity metadata missing.")
    if scene.get("motion_contract") != "object-space-keyframed-v1" or not scene.get("motion_no_image_warp"):
        raise RuntimeError("Cinematic motion contract missing.")
    animated_required=("HERO_A_ROOT","HERO_B_ROOT","HERO_C_ROOT","RAVEN_ROOT",
                       "NOWHERE_KING_ROOT","AUTH_Firmament","AUTH_NullGate",
                       "SHOT_01_RETURN_CAM","SHOT_06_THRONE_REVEAL_CAM","SHOT_08_LAST_ARGUMENT_CAM")
    unanimated=[name for name in animated_required
                if bpy.data.objects.get(name) is None
                or bpy.data.objects[name].animation_data is None
                or bpy.data.objects[name].animation_data.action is None]
    if unanimated:
        raise RuntimeError("Required cinematic object motion missing: "+", ".join(unanimated))
    cut_markers=[m for m in scene.timeline_markers if m.camera is not None]
    if len(cut_markers) != len(SHOT_SPECS):
        raise RuntimeError(f"Expected {len(SHOT_SPECS)} camera-cut markers; got {len(cut_markers)}.")
    report = {
        "scene": scene.get("magenheim_cinematic_id"),
        "objects": len(bpy.data.objects),
        "meshes": len(bpy.data.meshes),
        "materials": len(bpy.data.materials),
        "shots": len(SHOT_SPECS),
        "frame_end": scene.frame_end,
        "animated_objects": sum(1 for o in bpy.data.objects if o.animation_data and o.animation_data.action),
        "camera_cuts": sum(1 for m in scene.timeline_markers if m.camera is not None),
    }
    print("CINEMATIC VERIFIED " + json.dumps(report, sort_keys=True))


def build(output: Path, preview_dir: Path | None, donor_library: Path | None):
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
    if donor_library:
        donor_library = donor_library if donor_library.is_absolute() else ROOT / donor_library
        print("CINEMATIC BUILD Valheim donors", flush=True)
        add_valheim_donor_sets(mats,root,donor_library)
    print("CINEMATIC BUILD cameras", flush=True)
    cameras = add_cameras(root)
    add_lighting(root, mats)
    print("CINEMATIC BUILD motion", flush=True)
    animate_scene(scene,cameras)
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
    elif ns.render_motion_frames:
        verify()
        output_dir = ns.render_motion_frames if ns.render_motion_frames.is_absolute() else ROOT / ns.render_motion_frames
        render_motion_frames(bpy.context.scene, output_dir)
    else:
        build(ns.output, ns.preview_dir, ns.valheim_donor_library)


if __name__ == "__main__":
    main()
