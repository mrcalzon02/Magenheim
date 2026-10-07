#!/usr/bin/env python3
"""Author the six owned Underworld crafting stations.

These are deliberately independent silhouettes. Vanilla donors may supply runtime CraftingStation
behavior, but no station here is authored by opening, recolouring or reshaping a vanilla workbench.
Every object has explicit UVs; no smart projection is used.
"""
import json
import math
import hashlib
import random
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from underworld_material_library import bind_underworld_material
from worked_surface_authoring import bind_worked_surface
from station_process_authoring import build_process_station

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets" / "models" / "source"
DETAIL_FLOORS = {
    "mycelial": 52,
    "tidal": 54,
    "furnace": 58,
    "silence": 52,
    "anchor": 58,
    "crown": 56,
}
DETAIL_REVISION = 7

SPECS = {
    "underworld-station-mycelial-bench": "mycelial",
    "underworld-station-tidal-basin": "tidal",
    "underworld-station-furnace-heart-forge": "furnace",
    "underworld-station-silence-table": "silence",
    "underworld-station-anchor-forge": "anchor",
    "underworld-station-crown-reliquary": "crown",
}


def material(name, colour, metallic=0.0, roughness=0.72, emission=None, alpha=1.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*colour, alpha)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission is not None and "Emission Color" in bsdf.inputs:
        bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
        bsdf.inputs["Emission Strength"].default_value = 0.7
    bind_underworld_material(bpy, mat, name)
    if emission is None:
        bind_worked_surface(bpy, mat, wood=any(token in name for token in ('worldroot', 'rimewood', 'rotwood', 'charred')), weathered=True)
    return mat


def finish(obj, path, mat):
    obj.name = path
    obj["game_node_path"] = path
    obj["game_collision"] = False
    obj["game_crystal"] = json.dumps(None)
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    if obj.data.uv_layers.get("StationUV") is None:
        obj.data.uv_layers.new(name="StationUV")
    obj.data.uv_layers.active_index = obj.data.uv_layers.find("StationUV")
    bpy.ops.object.select_all(action="DESELECT")
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    # Explicit per-face dimensional projection also handles chipped Boolean faces.
    uv = obj.data.uv_layers.active.data
    for face in obj.data.polygons:
        axis = max(range(3), key=lambda index: abs(face.normal[index]))
        axes = ((1, 2), (0, 2), (0, 1))[axis]
        for loop_index in face.loop_indices:
            point = obj.data.vertices[obj.data.loops[loop_index].vertex_index].co
            uv[loop_index].uv = (point[axes[0]] / 1.5, point[axes[1]] / 1.5)
    obj.select_set(False)
    return obj


def cube(path, location, scale, mat, bevel=0.04, rotation=(0, 0, 0)):
    # The assembly coordinates and dimensions below use half extents. size=1
    # previously halved every board/block and disconnected the intended joints.
    bpy.ops.mesh.primitive_cube_add(size=2, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if any(token in path for token in ('worktop-slat', 'precision-slab', 'reliquary-table', 'hearth-body', 'anvil-monolith', 'anvil-cap')):
        # Work the existing surfaces rather than decorating an immaculate cube.
        # Subdivision gives long boards a shallow bow and unequal hewn edges.
        import bmesh
        rng = random.Random(int(hashlib.sha256(path.encode()).hexdigest()[:8], 16))
        bm = bmesh.new(); bm.from_mesh(obj.data)
        bmesh.ops.subdivide_edges(bm, edges=list(bm.edges), cuts=3, use_grid_fill=True)
        for vertex in bm.verts:
            x, y, z = vertex.co
            vertex.co.z += .014*math.sin(x*2.3+y*1.7)+rng.uniform(-.007, .007)
            if abs(y) > scale[1]*.9:
                vertex.co.y += .013*math.sin(x*4.1)+rng.uniform(-.008, .008)
                if z > scale[2]*.9 and rng.random() < .45:
                    vertex.co.z -= rng.uniform(.020, .045)
                    vertex.co.y += .018
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        bmesh.ops.triangulate(bm, faces=list(bm.faces))
        bm.to_mesh(obj.data); bm.free()
        # Warped, chipped edge geometry already supplies the worked silhouette.
        bevel = 0
    if bevel:
        mod = obj.modifiers.new("Worked edges", "BEVEL")
        mod.width = bevel
        mod.segments = 1
    return finish(obj, path, mat)


def cyl(path, location, radius, depth, mat, vertices=16, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    return finish(bpy.context.object, path, mat)


def sphere(path, location, radius, mat, segments=16, rings=10):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=radius, location=location)
    return finish(bpy.context.object, path, mat)


def torus(path, location, major, minor, mat, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(
        major_segments=24, minor_segments=8,
        major_radius=major, minor_radius=minor,
        location=location, rotation=rotation)
    return finish(bpy.context.object, path, mat)


def tube(path, start, end, radius, mat, vertices=10):
    a = Vector(start)
    b = Vector(end)
    direction = b - a
    if direction.length <= 1e-5:
        raise ValueError(path + ": degenerate tube")
    mid = (a + b) * 0.5
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=direction.length, location=mid)
    obj = bpy.context.object
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(direction.normalized())
    return finish(obj, path, mat)


def worked_rivet(path, location, mat, radius=.045):
    sphere(path, location, radius, mat, 10, 6)


def clamp_band(path, center, half_width, half_depth, z, mat):
    cube(path + "-front", (center, -half_depth, z), (half_width, .035, .035), mat, .015)
    cube(path + "-back", (center, half_depth, z), (half_width, .035, .035), mat, .015)
    cube(path + "-left", (center-half_width, 0, z), (.035, half_depth, .035), mat, .015)
    cube(path + "-right", (center+half_width, 0, z), (.035, half_depth, .035), mat, .015)


def hanging_chain(path, start, count, spacing, mat):
    x, y, z = start
    for i in range(count):
        torus(f"{path}-{i}", (x, y, z-i*spacing), .055, .014, mat, (math.pi/2 if i%2 else 0, 0, 0))


def drawer_handle(path, location, width, mat):
    x,y,z=location
    tube(path+"-left",(x-width*.5,y,z),(x-width*.5,y-.08,z),.018,mat,7)
    tube(path+"-right",(x+width*.5,y,z),(x+width*.5,y-.08,z),.018,mat,7)
    tube(path+"-grip",(x-width*.5,y-.08,z),(x+width*.5,y-.08,z),.018,mat,7)


def drawer_bank(path, width, front, z, count, m):
    # One joined carcass, recessed thin drawer fronts and real reveals.
    depth, height = .40, .15
    cube(path+'-back', (0, front+depth*2, z), (width, .035, height), m['body'], .012)
    for side in (-1, 1):
        cube(path+f'-side-{side}', (side*width, front+depth, z), (.045, depth, height+.04), m['body'], .018)
    for level in (-1, 1):
        cube(path+f'-rail-{level}', (0, front+depth, z+level*(height+.035)), (width, depth, .035), m['body'], .012)
    pitch = 2*width/count
    for i in range(count):
        x = -width+pitch*(i+.5)
        cube(path+f'-front-{i}', (x, front+.035, z), (pitch*.5-.022, .025, height-.02), m['body'], .012)
        drawer_handle(path+f'-handle-{i}', (x, front, z), min(.16, pitch*.38), m['metal'])
        for side in (-1, 1):
            worked_rivet(path+f'-peg-{i}-{side}', (x+side*(pitch*.5-.055), front-.002, z), m['metal'], .015)


def add_station_finish(kind, m):
    if kind == "mycelial":
        # Carved understone sockets, pegged joinery, tool drawers and consumable spools.
        for side in (-1,1):
            for face in (-1,1):
                cube(f"stone-socket-{side}-{face}",(side*1.15,face*.48,.34),(.26,.08,.10),m["stone"],.025)
                worked_rivet(f"stone-pin-{side}-{face}",(side*1.15,face*.57,.34),m["metal"])
        drawer_bank('bench-drawers', 1.08, -.78, 1.03, 3, m)
        clamp_band("worktop-collar",0,1.38,.90,1.22,m["metal"])

    elif kind == "tidal":
        drawer_bank('salt-drawers', 1.15, -1.16, .29, 3, m)


def palette(kind):
    if kind == "mycelial":
        return {
            "stone": material("underworld.station.mycelial.understone", (.20, .23, .19)),
            "body": material("underworld.station.mycelial.worldroot", (.25, .16, .075)),
            "metal": material("underworld.station.mycelial.dark-iron", (.10, .11, .11), .7, .38),
            "accent": material("underworld.station.mycelial.glowcap", (.30, .62, .34), 0, .30, (.08, .28, .10)),
        }
    if kind == "tidal":
        return {
            "stone": material("underworld.station.tidal.flowstone", (.25, .34, .36)),
            "body": material("underworld.station.tidal.pale-fibre", (.52, .55, .50)),
            "metal": material("underworld.station.tidal.pearl-metal", (.38, .48, .50), .35, .32),
            "accent": material("underworld.station.tidal.blackwater-pearl", (.54, .78, .80), .1, .20, (.08, .22, .26)),
        }
    if kind == "furnace":
        return {
            "stone": material("underworld.station.furnace.slagstone", (.18, .14, .12)),
            "body": material("underworld.station.furnace.charred-root", (.16, .08, .045)),
            "metal": material("underworld.station.furnace.emberiron", (.24, .14, .09), .8, .30),
            "accent": material("underworld.station.furnace.heart", (.82, .24, .045), .05, .18, (.72, .10, .015)),
        }
    if kind == "silence":
        return {
            "stone": material("underworld.station.silence.rimewood", (.32, .40, .42)),
            "body": material("underworld.station.silence.clear-ice", (.48, .70, .78), 0, .18, (.06, .18, .24)),
            "metal": material("underworld.station.silence.rimesilver", (.68, .74, .76), .9, .20),
            "accent": material("underworld.station.silence.focus-ice", (.68, .88, .94), 0, .12, (.14, .34, .42)),
        }
    if kind == "anchor":
        return {
            "stone": material("underworld.station.anchor.shardstone", (.24, .21, .27)),
            "body": material("underworld.station.anchor.titanbone", (.50, .46, .37)),
            "metal": material("underworld.station.anchor.forged-brace", (.15, .14, .16), .8, .34),
            "accent": material("underworld.station.anchor.fracture-crystal", (.52, .36, .72), 0, .16, (.20, .08, .34)),
        }
    return {
        "stone": material("underworld.station.crown.bone-gravel", (.34, .30, .23)),
        "body": material("underworld.station.crown.rotwood", (.19, .12, .055)),
        "metal": material("underworld.station.crown.dark-binding", (.16, .12, .09), .7, .30),
        "accent": material("underworld.station.crown.carrion-amber", (.72, .42, .10), .05, .16, (.35, .12, .015)),
    }


def build_mycelial(m):
    for side in (-1, 1):
        cube(f"understone-foot-{side}", (side * 1.15, 0, .18), (.46, .62, .18), m["stone"], .05)
        for leg in (-1, 1):
            tube(f"root-leg-{side}-{leg}", (side * .95, leg * .42, .28), (side * .78, leg * .34, 1.22), .12, m["body"], 12)
    for i in range(7):
        x = -1.28 + i * .43
        cube(f"worktop-slat-{i}", (x, 0, 1.30 + .025 * math.sin(i)), (.19, .86, .10), m["body"], .035)
    for i, x in enumerate((-1.12, -.56, 0, .56, 1.12)):
        torus(f"fibre-lashing-{i}", (x, 0, 1.30), .20, .025, m["metal"], (math.pi / 2, 0, 0))
    for i in range(6):
        y = -.60 + i * .24
        tube(f"under-brace-{i}", (-1.15, y, .66), (1.15, -y, .72), .045, m["body"], 9)
    # A clamped Worldroot billet and drawknife explain plank/weapon-handle work.
    cube('planing-billet',(-.45,-.22,1.47),(.68,.14,.08),m['body'],.018)
    for side in (-1,1):
        cube(f'bench-dog-{side}',(-.45+side*.75,-.22,1.46),(.055,.18,.08),m['body'],.012)
    cube('drawknife-blade',(-.45,-.49,1.53),(.32,.025,.025),m['metal'],.008)
    for side in (-1,1):
        tube(f'drawknife-handle-{side}',(-.45+side*.34,-.49,1.53),(-.45+side*.42,-.65,1.49),.038,m['body'],9)
    # A supported cord-making spindle, with a visible winding of Spire fibre.
    for side in (-1,1):
        cube(f'spindle-bearing-{side}',(.87+side*.25,-.15,1.57),(.045,.10,.18),m['body'],.012)
    tube('cord-spindle',(.55,-.15,1.73),(1.22,-.15,1.73),.035,m['metal'],10)
    for i in range(9):
        torus(f'spire-cord-winding-{i}',(.72+i*.033,-.15,1.73),.080,.018,m['body'],(0,math.pi/2,0))
    tube('spindle-crank',(1.22,-.15,1.73),(1.22,-.15,1.57),.025,m['metal'],8)
    tube('spindle-crank-grip',(1.22,-.15,1.57),(1.32,-.15,1.57),.034,m['body'],8)
    # Sliced caps rest on a slatted drying rack rather than glowing balls on stems.
    for side in (-1,1):
        cube(f'drying-rack-upright-{side}',(side*1.12,.68,1.69),(.045,.05,.30),m['body'],.012)
    for i in range(8):
        tube(f'drying-rack-slat-{i}',(-1.12,.42+i*.052,1.91),(1.12,.42+i*.052,1.91),.024,m['body'],8)
    for i in range(5):
        cap=cyl(f'glowcap-drying-slice-{i}',(-.85+i*.41,.59,1.957),.13,.035,m['accent'],10)
        cap.scale.y=.65


def build_tidal(m):
    # A table built around an actual opening. No board covers the recessed bowl.
    for side in (-1,1):
        for end in (-1,1):
            cube(f'basin-leg-{side}-{end}',(side*1.27,end*.99,.48),(.13,.13,.48),m['body'],.025)
        cube(f'basin-side-apron-{side}',(side*1.30,0,.76),(.09,1.19,.14),m['body'],.018)
        cube(f'basin-end-apron-{side}',(0,side*1.10,.76),(1.33,.09,.14),m['body'],.018)
        cube(f'worktop-slat-basin-side-{side}',(side*1.22,0,.96),(.30,1.27,.07),m['body'],.018)
        for i in range(2):
            cube(f'worktop-slat-basin-end-{side}-{i}',(0,side*(.86+i*.25),.96),(.92,.115,.07),m['body'],.018)
    # Closed flowstone shell: outer walls, broad lip, sloping inner walls and floor.
    profile=((.90,.70,.43),(.90,.70,1.045),(.78,.58,1.045),(.58,.38,.50))
    vertices=[];faces=[]
    for x,y,z in profile:
        vertices.extend((sx*x,sy*y,z) for sx,sy in ((-1,-1),(1,-1),(1,1),(-1,1)))
    for ring in range(3):
        for i in range(4):
            j=(i+1)%4
            faces.append((ring*4+i,ring*4+j,(ring+1)*4+j,(ring+1)*4+i))
    faces.extend(((3,2,1,0),(12,13,14,15)))
    mesh=bpy.data.meshes.new('InsetFlowstoneBasin');mesh.from_pydata(vertices,[],faces);mesh.update()
    bowl=bpy.data.objects.new('inset-flowstone-basin',mesh);bpy.context.scene.collection.objects.link(bowl)
    import bmesh
    bm=bmesh.new();bm.from_mesh(bowl.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bmesh.ops.triangulate(bm,faces=list(bm.faces))
    bm.to_mesh(bowl.data);bm.free();bowl.data.update()
    finish(bowl,'inset-flowstone-basin',m['stone'])
    brine=material('underworld.station.tidal.flowstone-brine',(.07,.14,.16),.05,.30)
    brine_albedo=next(node.image for node in brine.node_tree.nodes if node.type=='TEX_IMAGE' and 'Albedo' in node.name)
    values=list(brine_albedo.pixels)
    for index in range(0,len(values),4):
        values[index]*=.18; values[index+1]*=.30; values[index+2]*=.34
    brine_albedo.pixels.foreach_set(values);brine_albedo.pack()
    cube('recessed-brine-surface',(0,0,.60),(.61,.41,.012),brine,0)
    tube('basin-drain-spout',(0,-.68,.52),(0,-1.38,.52),.065,m['metal'],12)
    tube('drain-valve-stem',(0,-1.02,.52),(0,-1.02,.73),.025,m['metal'],8)
    tube('drain-valve-handle',(-.12,-1.02,.73),(.12,-1.02,.73),.027,m['metal'],8)
    # A draining basket with contained pearl samples, resting beside the bath.
    for i in range(7):
        cube(f'pearl-basket-slat-{i}',(-1.23,-.40+i*.11,1.06),(.23,.023,.025),m['body'],.008)
    for side in (-1,1):
        cube(f'pearl-basket-side-{side}',(-1.23+side*.25,-.07,1.15),(.025,.38,.10),m['body'],.008)
        cube(f'pearl-basket-end-{side}',(-1.23,-.07+side*.38,1.15),(.25,.025,.10),m['body'],.008)
    for i in range(3):
        sphere(f'brined-pearl-sample-{i}',(-1.23,-.24+i*.17,1.15),.065,m['accent'],12,8)
    # Pale cords drip over the back of the bowl; their supports are visible.
    for side in (-1,1):
        cube(f'fibre-drying-post-{side}',(side*1.12,.99,1.36),(.045,.045,.36),m['body'],.012)
    tube('fibre-drying-crossbar',(-1.16,.99,1.71),(1.16,.99,1.71),.045,m['body'],10)
    for i in range(7):
        x=-.80+i*.26
        tube(f'pale-cord-drying-{i}',(x,.99,1.69),(x+.03,.85,.92),.018,m['body'],7)
    cube('deep-salt-tray-base',(1.22,-.65,1.07),(.23,.30,.025),m['stone'],.012)
    for side in (-1,1):
        cube(f'deep-salt-tray-side-{side}',(1.22+side*.23,-.65,1.13),(.025,.30,.07),m['stone'],.012)
        cube(f'deep-salt-tray-end-{side}',(1.22,-.65+side*.30,1.13),(.23,.025,.07),m['stone'],.012)
    for i in range(5):
        cube(f'deep-salt-crystal-{i}',(1.16+(i%2)*.12,-.84+i*.09,1.13),(.042,.038,.031),m['body'],0,(0,0,i*.7))


BUILDERS = {
    "mycelial": build_mycelial,
    "tidal": build_tidal,
    "furnace": lambda m: build_process_station("furnace", globals(), m),
    "silence": lambda m: build_process_station("silence", globals(), m),
    "anchor": lambda m: build_process_station("anchor", globals(), m),
    "crown": lambda m: build_process_station("crown", globals(), m),
}


def author(model_id):
    kind = SPECS[model_id]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mats = palette(kind)
    BUILDERS[kind](mats)
    add_station_finish(kind, mats)

    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    floor = DETAIL_FLOORS[kind]
    if len(meshes) < floor:
        raise RuntimeError(f"{model_id}: station detail regression: {len(meshes)} parts < {floor}")
    for obj in meshes:
        uv = obj.data.uv_layers.get("StationUV")
        if uv is None or not uv.data:
            raise RuntimeError(f"{model_id}/{obj.name}: explicit StationUV missing")

    scene = bpy.context.scene
    scene["model_id"] = model_id
    scene["magenheim_family"] = "underworld_biome_station"
    scene["magenheim_biome_station_kind"] = kind
    scene["magenheim_fidelity"] = "endgame-station-r7"
    scene["magenheim_detail_revision"] = DETAIL_REVISION
    scene["magenheim_detail_parts"] = len(meshes)
    scene["magenheim_craft_language"] = "functional-joinery+service-hardware+biome-tooling"
    scene["runtime_lights"] = "[]"
    bpy.context.preferences.filepaths.save_version = 0
    target = SOURCE / (model_id + ".blend")
    bpy.ops.wm.save_as_mainfile(filepath=str(target), compress=True)
    print("AUTHORED", model_id, kind, len(meshes), "parts", flush=True)


requested = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else list(SPECS)
unknown = [value for value in requested if value not in SPECS]
if unknown:
    raise SystemExit("Unknown Underworld station model(s): " + ", ".join(unknown))
for model_id in requested:
    author(model_id)
