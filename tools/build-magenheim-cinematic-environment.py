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
    # Quiet broad walls; detail is limited to structurally meaningful beams.
    beam = 0.20 * scale
    for sx in (-1, 1):
        add_box(prefix + f"_CornerBeam_{sx:+}", (x + sx * (w / 2 - beam / 2), y, stone_h + h / 2),
                (beam, d + 0.05, h), mats["darkwood"], target)
    add_box(prefix + "_Door", (x, y - d / 2 - 0.03, stone_h + 1.0 * scale),
            (1.15 * scale, 0.15 * scale, 2.0 * scale), mats["darkwood"], target)
    return body


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
    # One great hall and a small number of readable houses, not decorative clutter.
    add_house("Village_GreatHall", 7.5, 8.0, 1.0, mats, terrain, great_hall=True)
    for idx, (x, y, s) in enumerate(((-12, 5, .86), (-18, -8, .78), (16, -6, .82), (22, 10, .72), (-3, 18, .72))):
        add_house(f"Village_House_{idx:02}", x, y, s, mats, terrain)
    path = add_box("Village_MainPath", (0, -5, 0.03), (6.5, 50, 0.08), mats["path"], terrain)
    path.rotation_euler[2] = math.radians(-4)
    # Foreground trees get more shape; background trees collapse to silhouettes.
    pines = ((-28,-18,1.1,True),(-24,18,1.0,True),(31,-10,.95,True),
             (-38,4,.95,False),(-45,18,.9,False),(39,24,.8,False),
             (45,-24,.85,False),(-50,-24,.82,False),(34,34,.78,False))
    for i,(x,y,s,fg) in enumerate(pines):
        add_pine(f"Village_Pine_{i:02}",x,y,0,s,mats,terrain,fg)

    # Blocking anchors are real scene objects, not painted-in figures.
    for i, loc in enumerate(((-2,-26,0.2),(0,-27,0.2),(2,-26.5,0.2))):
        e = bpy.data.objects.new(f"HERO_{chr(65+i)}_ROOT", None)
        e.empty_display_type = 'ARROWS'
        e.empty_display_size = 0.8
        e.location = loc
        terrain.objects.link(e)
    raven = bpy.data.objects.new("RAVEN_ROOT", None)
    raven.empty_display_type = 'SPHERE'
    raven.empty_display_size = 0.35
    raven.location = (-7, 8, 9)
    terrain.objects.link(raven)
    return terrain


def append_blend(path: Path, root_name: str, target, location=(0,0,0), scale=1.0, rotation=(0,0,0)):
    if not path.is_file():
        raise RuntimeError(f"Missing authoritative cinematic source: {path}")
    before = set(bpy.data.objects)
    with bpy.data.libraries.load(str(path), link=False) as (src, dst):
        dst.objects = [name for name in src.objects if not name.lower().startswith(("camera", "light"))]
    loaded = [o for o in dst.objects if o is not None and o not in before]
    root = bpy.data.objects.new(root_name, None)
    target.objects.link(root)
    root.location = location
    root.scale = (scale, scale, scale)
    root.rotation_euler = rotation
    for obj in loaded:
        if not obj.users_collection:
            target.objects.link(obj)
        else:
            for c in tuple(obj.users_collection):
                c.objects.unlink(obj)
            target.objects.link(obj)
    for obj in loaded:
        if obj.parent is None:
            world = obj.matrix_world.copy()
            obj.parent = root
            obj.matrix_world = world
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
    floor = add_box("ThroneHall_Floor", (0,0,-.55), (54,64,1.0), mats["basalt"], hall)
    # Keep the architectural envelope broad and quiet.
    for x in (-25.0, 25.0):
        add_box(f"ThroneHall_Wall_{x:+}", (x,3,7), (2.0,62,15), mats["basalt_dark"], hall)
    add_box("ThroneHall_RearWall", (0,29,7), (52,2.0,15), mats["basalt_dark"], hall)
    for i,x in enumerate((-19,-12,-5,5,12,19)):
        add_box(f"ThroneHall_Pier_{i}", (x,16,6), (1.4,1.4,12), mats["basalt"], hall)
    throne = append_blend(SOURCE / "dark-throne.blend", "AUTH_DarkThrone", hall, location=(0,18,0))
    # Existing Magenheim weapon art, not storyboard inventions.
    firm = append_blend(SOURCE / "nowhere-king-sword-firmament.blend", "AUTH_Firmament", hall,
                        location=(-1.2,13.7,2.2), scale=.78, rotation=(math.radians(90),0,math.radians(-16)))
    null = append_blend(SOURCE / "nowhere-king-sword-null-gate.blend", "AUTH_NullGate", hall,
                        location=(1.2,13.7,2.2), scale=.78, rotation=(math.radians(90),0,math.radians(16)))

    # Four banners and eight braziers correspond to the canonical throne presentation.
    for i, x in enumerate((-18,-8,8,18)):
        add_banner(f"Blackstone_Banner_{i}", x, 26.7, 7.0, 0, mats, hall)
    for i, (x,y) in enumerate(((-18,-2),(-9,-3),(9,-3),(18,-2),(-18,11),(-9,10),(9,10),(18,11))):
        add_brazier(f"Blackstone_Brazier_{i}", x,y,0,mats,hall)

    king = bpy.data.objects.new("NOWHERE_KING_ROOT", None)
    king.empty_display_type = 'ARROWS'
    king.empty_display_size = 1.4
    king.location = (0,14.0,0.25)
    hall.objects.link(king)
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
    specs = (
        ("SHOT_01_RETURN_CAM", (-13,-36,6.5), (0,-8,2.3), 44),
        ("SHOT_02_HOMECOMING_CAM", (-4,-4,3.4), (7.5,8,2.6), 48),
        ("SHOT_03_PEACE_CAM", (-34,-34,15), (2,7,3.2), 54),
        ("SHOT_04_RUMBLE_CAM", (4,-21,2.1), (3,12,5.0), 52),
        ("SHOT_05_OMEN_CAM", (-16,4,8), (-7,8,9), 68),
        ("SHOT_06_THRONE_REVEAL_CAM", (0,-25,5.0), (0,18,4.6), 42),
        ("SHOT_07_KING_AWAKENS_CAM", (-6,1,4.0), (0,14.5,4.2), 58),
        ("SHOT_08_LAST_ARGUMENT_CAM", (0,4.0,3.4), (0,14.0,3.3), 74),
    )
    created = {}
    for name, loc, tgt, lens in specs:
        created[name] = camera(name, loc, tgt, lens, cams)
    return created


def add_lighting(root, mats):
    lights = collection("CIN_Lights", root)
    sun_data = bpy.data.lights.new("Surface_Sun_Data", type='SUN')
    sun_data.energy = 2.2
    sun_data.color = (0.64,0.70,0.78)
    sun = bpy.data.objects.new("Surface_Sun", sun_data)
    sun.rotation_euler = (math.radians(31), math.radians(-18), math.radians(-34))
    lights.objects.link(sun)

    key_data = bpy.data.lights.new("Throne_Key_Data", type='AREA')
    key_data.energy = 1500
    key_data.shape = 'DISK'
    key_data.size = 12
    key_data.color = (0.22,0.25,0.34)
    key = bpy.data.objects.new("Throne_Key", key_data)
    key.location = (0,4,11)
    key.rotation_euler = (math.radians(24),0,0)
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
    bg.inputs["Color"].default_value = (0.075,0.095,0.13,1)
    bg.inputs["Strength"].default_value = .48
    bpy.context.scene.world = world


def materials():
    return {
        "ground": mat("CIN_Ground", (.17,.19,.18), .95),
        "path": mat("CIN_Path", (.22,.20,.17), .97),
        "wood": mat("CIN_Timber", (.22,.15,.10), .82),
        "darkwood": mat("CIN_DarkTimber", (.09,.065,.05), .88),
        "roof": mat("CIN_Roof", (.14,.12,.11), .96),
        "stone": mat("CIN_VillageStone", (.28,.30,.31), .93),
        "pine": mat("CIN_Pine", (.075,.12,.10), .97),
        "mountain_far": mat("CIN_MountainFar", (.20,.25,.31), 1.0),
        "mountain_mid": mat("CIN_MountainMid", (.17,.21,.25), 1.0),
        "mountain_near": mat("CIN_MountainNear", (.12,.15,.17), 1.0),
        "basalt": mat("CIN_Basalt", (.055,.060,.070), .92),
        "basalt_dark": mat("CIN_BasaltDark", (.025,.028,.035), .96),
        "blackmetal": mat("CIN_BlackMetal", (.025,.025,.030), .34, .78),
        "cloth": mat("CIN_BlackCloth", (.018,.019,.024), .88),
        "eclipse": mat("CIN_Eclipse", (.23,.23,.24), .75),
        "eye": mat("CIN_EyeGlow", (.20,.005,.003), .25, emission=(1.0,.01,.005), emission_strength=7.0),
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
        "NOWHERE_KING_ROOT", "HERO_A_ROOT", "HERO_B_ROOT", "HERO_C_ROOT",
        "SHOT_01_RETURN_CAM", "SHOT_06_THRONE_REVEAL_CAM", "SHOT_08_LAST_ARGUMENT_CAM",
        "Village_GreatHall_TimberBody", "ThroneHall_Floor",
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
