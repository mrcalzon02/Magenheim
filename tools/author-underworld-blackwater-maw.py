#!/usr/bin/env python3
"""Author the Blackwater Maw boss from the accepted Deep Hunter rig and anatomy.

Deliberately preserve the existing seven-stage aquatic skeleton, thirteen authored
swim/combat animations, canonical creature PBR maps, and donor combat authority.
The Maw is a different *body*, not a larger copy of the Deep Hunter. Blender 5.
Run with: tools/blender.ps1 author-underworld-blackwater-maw
Then: tools/blender.ps1 export-model-assets underworld-boss-blackwater-maw
"""
from pathlib import Path
import json
import math
import bpy
import bmesh
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets/models/source/underworld-creature-deep-hunter.blend"
DEST = ROOT / "assets/models/source/underworld-boss-blackwater-maw.blend"
MODEL = "underworld-boss-blackwater-maw"

if not SOURCE.is_file():
    raise RuntimeError("Deep Hunter editable source missing: " + str(SOURCE))
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
armatures = [obj for obj in scene.objects if obj.type == "ARMATURE"]
if len(armatures) != 1:
    raise RuntimeError("Expected exactly one inherited Deep Hunter armature")
arm = armatures[0]
if arm.get("magenheim_asset") != "deep-hunter":
    raise RuntimeError("Refusing to derive Maw from an unrelated creature")
for bone in ("Head", "Jaw", "Spine_1", "Spine_2", "Spine_3", "Spine_4",
             "Spine_5", "Tail_1", "Tail_2", "Fin_L", "Fin_R"):
    if bone not in arm.data.bones:
        raise RuntimeError("Deep Hunter lost required animated segment: " + bone)
original_actions = str(scene.get("magenheim_authored_actions", "")).split(",")
if len([name for name in original_actions if name.strip()]) < 13:
    raise RuntimeError("Deep Hunter's 13-action production set is missing")
base = [obj for obj in scene.objects if obj.type == "MESH"]
base_tris = sum(len(p.vertices) - 2 for obj in base for p in obj.data.polygons)
if base_tris < 30000:
    raise RuntimeError("Deep Hunter's accepted hero-creature anatomy regressed")

DORSAL = bpy.data.materials["DeepHunter_DorsalHide"]
VENTRAL = bpy.data.materials["DeepHunter_VentralHide"]
FIN = bpy.data.materials["DeepHunter_FinTissue"]
MOUTH = bpy.data.materials["DeepHunter_OralTissue"]
TOOTH = bpy.data.materials["DeepHunter_Tooth"]
SCAR = bpy.data.materials["DeepHunter_ScarTissue"]
EYE = bpy.data.materials["DeepHunter_Eye"]

GLOW = bpy.data.materials.new("Maw_AbyssalPhotophore")
GLOW.diffuse_color = (.028, .23, .29, 1)
GLOW.use_nodes = True
principled = GLOW.node_tree.nodes.get("Principled BSDF")
principled.inputs["Base Color"].default_value = (.025, .10, .13, 1)
principled.inputs["Roughness"].default_value = .32
principled.inputs["Emission Color"].default_value = (.08, .46, .53, 1)
principled.inputs["Emission Strength"].default_value = .75

new_parts = []
def attach(obj, material, bone):
    if bone not in arm.data.bones:
        raise RuntimeError("Unknown Maw attachment bone: " + bone)
    obj.data.materials.clear()
    obj.data.materials.append(material)
    obj["game_node_path"] = "creaturebone:" + bone + "/" + obj.name
    obj["game_collision"] = False
    obj["game_crystal"] = json.dumps(None)
    if not obj.data.uv_layers:
        raise RuntimeError("Missing authored Maw UVs: " + obj.name)
    vg = obj.vertex_groups.new(name=bone)
    vg.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    modifier = obj.modifiers.new("MawSegmentArmature", "ARMATURE")
    modifier.object = arm
    obj.parent = arm
    new_parts.append(obj)
    return obj

def mesh_part(name, points, faces, uvs, material, bone):
    data = bpy.data.meshes.new(name + "_mesh")
    data.from_pydata(points, [], faces)
    data.update()
    uv_layer = data.uv_layers.new(name="BlackwaterMawUV")
    if len(uvs) != len(faces):
        raise RuntimeError("Invalid authored face UV count for " + name)
    for polygon, face_uv in zip(data.polygons, uvs):
        if len(face_uv) != len(polygon.loop_indices):
            raise RuntimeError("Invalid per-corner UV for " + name)
        for corner, texcoord in zip(polygon.loop_indices, face_uv):
            uv_layer.data[corner].uv = texcoord
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    # Recalculate through the mesh only; preserve the original Deep Hunter source.
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(data)
    bm.free()
    return attach(obj, material, bone)

def tube(name, path, material, bone, sectors=10):
    """Closed tapered anatomical extrusion with longitudinal/radial UVs."""
    if len(path) < 2:
        raise RuntimeError("Maw tube needs two or more rings")
    verts = []
    rings = []
    for i, (x, y, z, rx, rz) in enumerate(path):
        center = Vector((x, y, z))
        direction = Vector(path[min(i + 1, len(path)-1)][:3]) - Vector(path[max(i - 1, 0)][:3])
        if direction.length < 1e-5:
            raise RuntimeError("Degenerate Maw extrusion: " + name)
        tangent = direction.normalized()
        across = tangent.cross(Vector((0, 0, 1)))
        if across.length < .005:
            across = Vector((1, 0, 0))
        across.normalize()
        up = across.cross(tangent).normalized()
        ring = []
        for j in range(sectors):
            angle = j * math.tau / sectors
            point = center + across * (math.cos(angle) * rx) + up * (math.sin(angle) * rz)
            ring.append(len(verts))
            verts.append(tuple(point))
        rings.append(ring)
    faces, uv_faces = [], []
    for k in range(len(rings)-1):
        for j in range(sectors):
            next_j = (j + 1) % sectors
            faces.append((rings[k][j], rings[k][next_j], rings[k+1][next_j], rings[k+1][j]))
            uv_faces.append(((j/sectors, k/(len(rings)-1)), (next_j/sectors, k/(len(rings)-1)),
                             (next_j/sectors, (k+1)/(len(rings)-1)), (j/sectors, (k+1)/(len(rings)-1))))
    # Physical end caps: no visually hollow loose-ended horns, barbs or tentacles.
    faces.extend((tuple(reversed(rings[0])), tuple(rings[-1])))
    uv_faces.extend((tuple((.5+.48*math.cos(j*math.tau/sectors), .5+.48*math.sin(j*math.tau/sectors))
                          for j in reversed(range(sectors))),
                     tuple((.5+.48*math.cos(j*math.tau/sectors), .5+.48*math.sin(j*math.tau/sectors))
                           for j in range(sectors))))
    return mesh_part(name, verts, faces, uv_faces, material, bone)

def armour(name, polygon, thickness, material, bone):
    """Substantial thin-shell armour or membrane, not a zero-thickness billboard."""
    n = len(polygon)
    front = [(x, y, z + thickness*.5) for x, y, z in polygon]
    back = [(x, y, z - thickness*.5) for x, y, z in polygon]
    vertices = front + back
    faces = [tuple(range(n)), tuple(reversed(range(n, 2*n)))]
    for i in range(n):
        j = (i+1)%n
        faces.append((i, i+n, j+n, j))
    xs = [p[0] for p in polygon]
    ys = [p[1] for p in polygon]
    mnx, mx = min(xs), max(xs)
    mny, my = min(ys), max(ys)
    def uv(index):
        x,y,_ = vertices[index]
        return (.02+.96*(x-mnx)/max(.001,mx-mnx), .02+.96*(y-mny)/max(.001,my-mny))
    return mesh_part(name, vertices, faces, [tuple(uv(v) for v in face) for face in faces],
                     material, bone)

def orb(name, at, size, material, bone):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=at)
    obj = bpy.context.object
    obj.name = name
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    data = obj.data
    layer = data.uv_layers.new(name="BlackwaterMawUV")
    for polygon in data.polygons:
        for li in polygon.loop_indices:
            v = data.vertices[data.loops[li].vertex_index].co
            # Explicit spherical UVs for the small low-poly luminous nodes.
            unit = v.normalized()
            layer.data[li].uv = ((math.atan2(unit.y, unit.x)/math.tau+.5)%1,
                                 .5+math.asin(max(-1., min(1., unit.z)))/math.pi)
    return attach(obj, material, bone)

# Widened flanged mandibles project in front of the Deep Hunter's ordinary fish jaw.
# Off-axis tusks are attached to the original Jaw bone so actual bite clips move them.
for side, sign in (("L", -1), ("R", 1)):
    tube("Maw_MandibleRail_" + side,
         [(sign*.56, 2.16, -.30, .19, .16),
          (sign*.79, 2.66, -.40, .22, .14),
          (sign*.76, 3.11, -.39, .17, .12),
          (sign*.48, 3.62, -.37, .035, .028)], MOUTH, "Jaw", 14)
    # Forward pointing recurved tusks frame a much wider open-mouth profile.
    for index, y in enumerate((2.49, 2.80, 3.07, 3.31)):
        x = sign * (.73 - index*.075)
        tube("Maw_UpperTusk_%s_%02d" % (side,index+1),
             [(x, y, -.10, .070, .072),
              (x*1.07, y+.09, -.26, .060, .058),
              (x*.98, y+.18, -.47, .028, .029),
              (x*.82, y+.23, -.55, .006, .008)], TOOTH, "Head", 10)
        tube("Maw_LowerTusk_%s_%02d" % (side,index+1),
             [(x*.88, y+.04, -.47, .066, .066),
              (x*.89, y+.10, -.31, .050, .047),
              (x*.80, y+.15, -.12, .012, .013)], TOOTH, "Jaw", 10)
    # Throat armour is joined in overlapping, independently animated slabs.
    for index in range(5):
        y = 1.44-index*.32
        tube("Maw_PressureGill_%s_%02d" % (side,index+1),
             [(sign*.77,y+.13,-.12,.09,.13),
              (sign*.94,y-.02,-.19,.15,.16),
              (sign*.90,y-.22,-.33,.07,.09)], VENTRAL,
             "Spine_%d" % min(index+1, 5), 10)
    # Deep-sea sensory barbels are thick segmented organs, not paper strips.
    for index in range(3):
        y = 2.79 + index*.22
        tube("Maw_Barbel_%s_%d" % (side,index+1),
             [(sign*.65,y,-.26,.08,.08),
              (sign*(1.08+index*.14),y+.20,-.36,.060,.062),
              (sign*(1.42+index*.14),y+.05,-.54,.040,.044),
              (sign*(1.60+index*.14),y-.18,-.70,.008,.009)], SCAR, "Head", 10)
    # Shielded luminous lateral pressure-sensing organs.
    for index in range(7):
        y = 1.90-index*.43
        x = sign*(.85-max(0,index-3)*.075)
        orb("Maw_Photophore_%s_%02d" % (side,index+1),
            (x,y,.32-index*.028), (.075,.10,.062), GLOW,
            "Head" if index == 0 else "Spine_%d" % min(5, (index+1)//2))

# A crown-shaped pressure-shearing head crest visually distinguishes the boss
# even while its mouth is shut or it is viewed above a blackwater surface.
armour("Maw_SplinteredCranialCrest",
       [(-.48,2.13,.66),(-.78,2.50,.76),(-.46,3.28,1.22),
        (0,3.54,1.36),(.46,3.28,1.22),(.78,2.50,.76),
        (.48,2.13,.66),(0,2.00,.70)], .13, DORSAL, "Head")
# Swept dorsal blade keels are not the Deep Hunter's unchanged triangular fin.
for index, (y,z,h,bone) in enumerate([
    (1.30,.67,1.10,"Spine_1"),
    (.30,.67,1.40,"Spine_2"),
    (-.74,.56,1.19,"Spine_3"),
    (-1.60,.43,.91,"Spine_4"),
    (-2.34,.30,.66,"Spine_5")], 1):
    armour("Maw_ArmouredDorsalBlade_%02d" % index,
           [(-.22,y+.38,z),(-.08,y+.11,z+h*.80),
            (0,y-.32,z+h),(.13,y-.58,z+h*.27),
            (.26,y-.35,z)], .13, DORSAL, bone)

# Wide asymmetric shoulder vanes suggest an abyssal ambush predator,
# retaining the Deep Hunter's native fin motion on the Fin_L/Fin_R bones.
for side, sign in (("L",-1),("R",1)):
    armour("Maw_MantleWing_" + side,
           [(sign*.65,1.23,.11),
            (sign*1.48,.90,.36),
            (sign*2.38,.34,.23),
            (sign*2.52,-.26,-.01),
            (sign*1.58,-.55,-.19),
            (sign*.82,.22,-.21)], .075, FIN, "Fin_"+side)
    for index in range(3):
        y = .95-index*.30
        tube("Maw_MantleRib_%s_%d" % (side,index+1),
             [(sign*.84,y,.06,.046,.050),
              (sign*1.40,y-.12,.11,.037,.034),
              (sign*(2.15-index*.09),y-.37,.04,.012,.012)], SCAR, "Fin_"+side, 8)

# Mineralized terminal tail hooks follow the inherited Tail_2 control.
for side, sign in (("L",-1),("R",1)):
    tube("Maw_TailBarb_"+side,
         [(sign*.08,-3.38,.30,.10,.10),
          (sign*.25,-3.62,.65,.071,.069),
          (sign*.38,-3.89,.83,.018,.018)], TOOTH, "Tail_2", 10)

# Keep all inherited primary sculpted anatomy and authored actions intact.
# New parts reference the SAME skeleton: no second donor, action system, AI or ZDO.
for item in new_parts:
    if not item.get("game_node_path", "").startswith("creaturebone:"):
        raise RuntimeError("Unbound Blackwater Maw body part: " + item.name)
    if not item.data.uv_layers:
        raise RuntimeError("Untextured Maw body part: " + item.name)
extra_tris = sum(len(p.vertices)-2 for obj in new_parts for p in obj.data.polygons)
if len(new_parts) < 58 or extra_tris < 3000 or extra_tris > 16000:
    raise RuntimeError("Maw silhouette/regression budget: %d parts, %d tris" %
                       (len(new_parts), extra_tris))
if base_tris + extra_tris > 48000:
    raise RuntimeError("Boss geometry exceeds 48k triangles")
arm["magenheim_asset"] = "blackwater-maw"
arm["production_contract"] = "blackwater-maw-hero-boss-r1"
arm["host_rig"] = "HOST-AQUATIC-FISH-APEX"
arm["boss_identity"] = "BlackwaterMaw"
arm["anatomy"] = "hinged-mandibles;crown-crest;pressure-gills;barbels;mantle-wings;tail-barbs"
arm["rig_reused_from"] = "underworld-creature-deep-hunter"
arm["added_parts"] = len(new_parts)
arm["added_triangles"] = extra_tris
scene["magenheim_model_id"] = MODEL
scene["magenheim_skinning"] = "rigid-segment-weighted"
scene["magenheim_authored_actions"] = ",".join(original_actions)
scene["blackwater_maw_source"] = "author-underworld-blackwater-maw.py"
bpy.context.preferences.filepaths.save_version = 0
DEST.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(DEST), compress=True)
print("AUTHORED Blackwater Maw: source=%s, added_parts=%d, added_tris=%d, total_tris=%d, inherited_actions=%d" %
      (DEST, len(new_parts), extra_tris, base_tris+extra_tris, len(original_actions)), flush=True)
