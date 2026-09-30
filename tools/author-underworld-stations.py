#!/usr/bin/env python3
"""Author the six owned Underworld crafting stations.

These are deliberately independent silhouettes. Vanilla donors may supply runtime CraftingStation
behavior, but no station here is authored by opening, recolouring or reshaping a vanilla workbench.
Every object has explicit UVs; no smart projection is used.
"""
import json
import math
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from underworld_material_library import bind_underworld_material

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
DETAIL_REVISION = 2

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
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.cube_project(cube_size=0.45)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.select_set(False)
    return obj


def cube(path, location, scale, mat, bevel=0.04, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new("Worked edges", "BEVEL")
        mod.width = bevel
        mod.segments = 2
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


def add_station_finish(kind, m):
    if kind == "mycelial":
        # Carved understone sockets, pegged joinery, tool drawers and consumable spools.
        for side in (-1,1):
            for face in (-1,1):
                cube(f"stone-socket-{side}-{face}",(side*1.15,face*.48,.34),(.26,.08,.10),m["stone"],.025)
                worked_rivet(f"stone-pin-{side}-{face}",(side*1.15,face*.57,.34),m["metal"])
        for x in (-.86,-.43,0,.43,.86):
            cube(f"bench-drawer-{x}",(x,-.77,1.08),(.17,.09,.12),m["body"],.018)
            drawer_handle(f"bench-drawer-handle-{x}",(x,-.87,1.08),.14,m["metal"])
        for i,x in enumerate((-.78,-.26,.26,.78)):
            cyl(f"fibre-spool-{i}",(x,.66,1.52),.10,.20,m["body"],12,(math.pi/2,0,0))
            torus(f"spool-band-{i}",(x,.66,1.52),.105,.018,m["metal"],(math.pi/2,0,0))
        clamp_band("worktop-collar",0,1.38,.90,1.22,m["metal"])

    elif kind == "tidal":
        # Drainage and measuring hardware make the basin read as a curing machine, not a fountain.
        for i in range(10):
            a=i*math.tau/10
            tube(f"drain-channel-{i}",(math.cos(a)*.78,math.sin(a)*.78,.84),
                 (math.cos(a)*1.28,math.sin(a)*1.28,.46),.025,m["metal"],7)
        for i,a in enumerate((0,math.pi/2,math.pi,math.pi*1.5)):
            x,y=math.cos(a)*1.42,math.sin(a)*1.42
            torus(f"pearl-caliper-{i}",(x,y,1.64),.18,.025,m["metal"],(math.pi/2,a,0))
            tube(f"caliper-pointer-{i}",(x,y,1.64),(x+math.cos(a)*.24,y+math.sin(a)*.24,1.64),.018,m["metal"],7)
        for i in range(6):
            cube(f"salt-drawer-{i}",(-1.05+i*.42,-1.18,.56),(.16,.08,.10),m["stone"],.015)
            drawer_handle(f"salt-drawer-handle-{i}",(-1.05+i*.42,-1.27,.56),.12,m["metal"])
        for i in range(8):
            a=i*math.tau/8
            worked_rivet(f"basin-rivet-{i}",(math.cos(a)*1.48,math.sin(a)*1.48,.62),m["metal"],.05)

    elif kind == "furnace":
        # Bellows, quench trough, tuyere pipework and service hardware explain how the forge works.
        cube("quench-trough",(1.24,1.12,.58),(.45,.52,.28),m["stone"],.055)
        cube("quench-lip",(1.24,1.12,.88),(.50,.57,.04),m["metal"],.018)
        for side in (-1,1):
            cube(f"bellows-body-{side}",(side*.78,1.02,1.26),(.42,.42,.22),m["body"],.045)
            tube(f"bellows-nozzle-{side}",(side*.62,.78,1.16),(side*.34,.35,.94),.055,m["metal"],10)
            tube(f"bellows-lever-{side}",(side*.90,1.26,1.32),(side*1.15,1.52,1.70),.035,m["metal"],8)
        for i,x in enumerate((-.58,-.20,.20,.58)):
            hanging_chain(f"tool-chain-{i}",(x,-1.19,1.58),4,.10,m["metal"])
            tube(f"forge-tool-{i}",(x,-1.19,1.18),(x+(.12 if i%2 else -.12),-1.19,.72),.022,m["metal"],7)
        for i,x in enumerate((-.80,0,.80)):
            torus(f"pressure-gauge-{i}",(x,.92,1.72),.13,.025,m["metal"],(math.pi/2,0,0))
            tube(f"gauge-needle-{i}",(x,.80,1.72),(x+.07,.80,1.78),.012,m["accent"],6)
        for i in range(8):
            worked_rivet(f"anvil-rivet-{i}",(-.70+i*.20,1.51,1.03),m["metal"],.04)

    elif kind == "silence":
        # Damped precision work: isolation feet, vice jaws, indexed micrometer and tuning forks.
        for side in (-1,1):
            for depth in (-1,1):
                cyl(f"damping-foot-{side}-{depth}",(side*1.18,depth*.52,.10),.14,.10,m["body"],14)
                torus(f"damping-collar-{side}-{depth}",(side*1.18,depth*.52,.17),.15,.025,m["metal"])
        cube("ice-vice-left",(-.34,-.40,1.42),(.18,.18,.12),m["metal"],.025)
        cube("ice-vice-right",(.34,-.40,1.42),(.18,.18,.12),m["metal"],.025)
        tube("vice-screw",(-.18,-.40,1.42),(.18,-.40,1.42),.035,m["metal"],10)
        torus("micrometer-dial",(0,.48,1.62),.19,.028,m["metal"],(math.pi/2,0,0))
        for i in range(12):
            a=i*math.tau/12
            tube(f"dial-index-{i}",(math.cos(a)*.17,.47,1.62+math.sin(a)*.17),
                 (math.cos(a)*.22,.47,1.62+math.sin(a)*.22),.010,m["accent"],6)
        for i,x in enumerate((-.78,-.39,0,.39,.78)):
            tube(f"tuning-fork-left-{i}",(x,.74,1.42),(x-.04,.74,1.82),.017,m["metal"],7)
            tube(f"tuning-fork-right-{i}",(x+.08,.74,1.42),(x+.12,.74,1.82),.017,m["metal"],7)
            tube(f"tuning-fork-bridge-{i}",(x-.04,.74,1.82),(x+.12,.74,1.82),.017,m["metal"],7)
        for i,x in enumerate((-.92,-.46,0,.46,.92)):
            drawer_handle(f"tool-drawer-handle-{i}",(x,-.87,.88),.14,m["metal"])

    elif kind == "anchor":
        # Mechanical load path: bearing plates, chain hoist, brace locks, force gauges and pawls.
        for side in (-1,1):
            cube(f"gantry-bearing-{side}",(side*1.35,.62,2.72),(.18,.18,.14),m["metal"],.03)
            for r in range(4):
                worked_rivet(f"gantry-bearing-rivet-{side}-{r}",
                             (side*1.35+(-.10 if r<2 else .10),.49+(r%2)*.26,2.72),m["metal"],.035)
        hanging_chain("hammer-hoist",(0,.62,2.58),7,.11,m["metal"])
        torus("hoist-wheel",(0,.62,2.82),.24,.04,m["metal"],(math.pi/2,0,0))
        for side in (-1,1):
            for depth in (-1,1):
                cube(f"brace-lock-{side}-{depth}",(side*1.22,depth*.72,1.18),(.18,.18,.14),m["metal"],.03)
                torus(f"brace-lock-ring-{side}-{depth}",(side*1.22,depth*.84,1.18),.11,.025,m["accent"],(math.pi/2,0,0))
        for i,x in enumerate((-.75,-.25,.25,.75)):
            torus(f"force-gauge-{i}",(x,-.88,1.96),.12,.024,m["metal"],(math.pi/2,0,0))
            tube(f"force-needle-{i}",(x,-.90,1.96),(x+.06,-.90,2.01),.012,m["accent"],6)
        for i in range(6):
            cube(f"anvil-tooth-{i}",(-.75+i*.30,0,1.84),(.10,.20,.05),m["metal"],.015)

    elif kind == "crown":
        # Reliquary hardware: sealed drawers, ritual clamps, hanging seals and carved crown ribs.
        for i,x in enumerate((-1.15,-.69,-.23,.23,.69,1.15)):
            drawer_handle(f"reliquary-handle-{i}",(x,-1.05,.78),.13,m["metal"])
        for i,a in enumerate((0,math.pi/2,math.pi,math.pi*1.5)):
            x,y=math.cos(a)*.58,math.sin(a)*.58
            cube(f"amber-clamp-{i}",(x,y,1.86),(.14,.10,.10),m["metal"],.025,(0,0,a))
            hanging_chain(f"amber-seal-chain-{i}",(x,y,1.54),3,.09,m["metal"])
            sphere(f"amber-seal-{i}",(x,y,1.22),.07,m["accent"],10,7)
        for i in range(9):
            a=i*math.tau/9
            tube(f"crown-rib-{i}",(math.cos(a)*.52,0,2.92+math.sin(a)*.52),
                 (math.cos(a)*.74,0,2.92+math.sin(a)*.74),.022,m["body"],7)
            worked_rivet(f"crown-rivet-{i}",(math.cos(a)*.72,-.04,2.92+math.sin(a)*.72),m["metal"],.035)
        for side in (-1,1):
            cube(f"ritual-tool-rack-{side}",(side*1.30,.62,1.36),(.08,.42,.38),m["body"],.025)
            for j in range(3):
                tube(f"ritual-tool-{side}-{j}",(side*1.30,.40+j*.16,1.62),(side*1.30,.40+j*.16,1.08),.018,m["metal"],7)


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
    for i, x in enumerate((-.88, 0, .88)):
        sphere(f"glowcap-task-pod-{i}", (x, -.58, 1.58 + .10 * (i % 2)), .20, m["accent"])
        tube(f"pod-stem-{i}", (x, -.48, 1.34), (x, -.58, 1.52), .045, m["body"], 9)
    for i in range(6):
        y = -.60 + i * .24
        tube(f"under-brace-{i}", (-1.15, y, .66), (1.15, -y, .72), .045, m["body"], 9)
    cube("tool-rack", (0, .78, 1.62), (1.20, .07, .12), m["stone"], .025)
    for i in range(6):
        tube(f"hanging-tool-{i}", (-.95 + i * .38, .76, 1.56), (-.95 + i * .38, .76, 1.20), .025, m["metal"], 8)
    for i in range(4):
        sphere(f"spore-node-{i}", (-1.20 + i * .80, .64, 1.42 + .08 * (i % 2)), .08, m["accent"], 12, 8)


def build_tidal(m):
    cube("flowstone-plinth", (0, 0, .18), (1.65, 1.38, .18), m["stone"], .08)
    for ring, (r, z) in enumerate(((1.34, .42), (1.18, .66), (1.02, .84))):
        torus(f"basin-ring-{ring}", (0, 0, z), r, .15, m["stone"])
    cyl("blackwater-bowl", (0, 0, .82), .94, .10, m["accent"], 32)
    for i in range(8):
        a = i * math.tau / 8
        x, y = math.cos(a) * 1.35, math.sin(a) * 1.35
        tube(f"pale-fibre-stanchion-{i}", (x, y, .35), (x * .92, y * .92, 1.34), .055, m["body"], 10)
        sphere(f"pearl-gauge-{i}", (x * .90, y * .90, 1.42), .09, m["accent"], 12, 8)
    for i in range(4):
        a = i * math.pi / 2
        x, y = math.cos(a) * 1.72, math.sin(a) * 1.72
        cube(f"salt-rack-{i}", (x, y, 1.05), (.34, .08, .62), m["body"], .025, (0, 0, a))
        for j in range(3):
            cube(f"salt-tray-{i}-{j}", (x, y, .75 + j * .30), (.42, .20, .035), m["stone"], .015, (0, 0, a))
    for i in range(8):
        a = i * math.tau / 8 + math.pi / 8
        tube(f"basin-binding-{i}", (math.cos(a) * 1.48, math.sin(a) * 1.48, .38),
             (math.cos(a) * 1.18, math.sin(a) * 1.18, 1.02), .035, m["metal"], 8)


def build_furnace(m):
    cube("slag-foundation", (0, 0, .22), (1.82, 1.30, .22), m["stone"], .08)
    cube("hearth-body", (0, 0, .82), (1.40, 1.05, .60), m["stone"], .10)
    cube("fire-mouth", (0, -1.06, .82), (.72, .18, .44), m["metal"], .04)
    sphere("furnace-heart", (0, -.83, .88), .34, m["accent"], 20, 12)
    for side in (-1, 1):
        cube(f"emberiron-cheek-{side}", (side * 1.45, 0, 1.05), (.18, 1.08, .82), m["metal"], .04)
        for j in range(4):
            sphere(f"cheek-rivet-{side}-{j}", (side * 1.64, -.72 + j * .48, .80 + .15 * (j % 2)), .055, m["metal"], 10, 6)
    for i, x in enumerate((-.72, 0, .72)):
        cyl(f"chimney-{i}", (x, .62, 2.08), .23 if i else .30, 1.95, m["stone"], 16)
        torus(f"chimney-band-{i}", (x, .62, 2.32), .27 if i else .35, .045, m["metal"])
    cube("anvil-bed", (0, 1.16, .78), (.90, .48, .22), m["metal"], .05)
    cube("anvil-face", (0, 1.16, 1.05), (.70, .34, .08), m["metal"], .025)
    for i in range(6):
        x = -1.35 + i * .54
        tube(f"heat-cage-{i}", (x, -.86, .46), (x * .75, -.86, 1.58), .04, m["metal"], 8)
    for i in range(4):
        tube(f"charred-root-fuel-{i}", (-.82 + i * .55, .25, .46), (-.58 + i * .40, -.28, .72), .08, m["body"], 10)
    for i in range(5):
        torus(f"forge-collar-{i}", (0, 0, .46 + i * .30), 1.12 - i * .08, .035, m["metal"])


def build_silence(m):
    for side in (-1, 1):
        cube(f"rimewood-foot-{side}", (side * 1.18, 0, .20), (.42, .70, .20), m["stone"], .045)
        tube(f"rimewood-leg-front-{side}", (side * 1.15, -.52, .28), (side * .92, -.45, 1.12), .09, m["stone"], 10)
        tube(f"rimewood-leg-back-{side}", (side * 1.15, .52, .28), (side * .92, .45, 1.12), .09, m["stone"], 10)
    cube("precision-slab", (0, 0, 1.18), (1.55, .88, .12), m["stone"], .05)
    for i in range(7):
        x = -1.20 + i * .40
        cube(f"rimesilver-inlay-{i}", (x, 0, 1.32), (.025, .70, .025), m["metal"], .01)
    for i, x in enumerate((-.95, -.47, 0, .47, .95)):
        tube(f"instrument-arm-{i}", (x, .56, 1.30), (x * .76, .22, 1.92 + .10 * (i % 2)), .035, m["metal"], 8)
        sphere(f"ice-lens-{i}", (x * .76, .22, 1.95 + .10 * (i % 2)), .12, m["accent"], 16, 10)
    torus("silence-focus-ring", (0, -.15, 1.74), .62, .045, m["metal"], (math.pi / 2, 0, 0))
    for i in range(8):
        a = i * math.tau / 8
        sphere(f"clear-ice-index-{i}", (math.cos(a) * .62, -.15, 1.74 + math.sin(a) * .62), .065, m["body"], 12, 8)
    for i in range(5):
        cube(f"tool-drawer-{i}", (-.92 + i * .46, -.76, .88), (.18, .10, .15), m["stone"], .02)


def build_anchor(m):
    cube("shardstone-bed", (0, 0, .25), (1.90, 1.28, .25), m["stone"], .09)
    cube("anvil-monolith", (0, 0, 1.05), (1.18, .66, .56), m["stone"], .08)
    cube("anvil-cap", (0, 0, 1.64), (1.55, .82, .16), m["metal"], .04)
    for side in (-1, 1):
        for depth in (-1, 1):
            tube(f"titanbone-brace-{side}-{depth}", (side * 1.56, depth * .92, .28), (side * .96, depth * .56, 1.78), .11, m["body"], 12)
            sphere(f"fracture-anchor-{side}-{depth}", (side * 1.62, depth * .98, .34), .16, m["accent"], 14, 8)
    for i in range(6):
        a = i * math.tau / 6
        tube(f"anchor-spike-{i}", (math.cos(a) * 1.25, math.sin(a) * .86, .22),
             (math.cos(a) * 1.75, math.sin(a) * 1.24, .05), .055, m["metal"], 9)
    tube("hammer-gantry-left", (-1.35, .62, .32), (-1.35, .62, 2.72), .09, m["metal"], 10)
    tube("hammer-gantry-right", (1.35, .62, .32), (1.35, .62, 2.72), .09, m["metal"], 10)
    tube("hammer-gantry-top", (-1.35, .62, 2.72), (1.35, .62, 2.72), .09, m["metal"], 10)
    tube("suspended-hammer-shaft", (0, .62, 2.68), (0, .62, 1.95), .075, m["body"], 10)
    cube("suspended-hammer-head", (0, .62, 1.82), (.42, .22, .16), m["metal"], .04)
    for i in range(8):
        x = -1.18 + i * .34
        sphere(f"fracture-gauge-{i}", (x, -.78, 1.58 + .06 * (i % 2)), .065, m["accent"], 10, 7)
    for i in range(4):
        cube(f"bone-tool-rest-{i}", (-1.05 + i * .70, -1.02, .92), (.25, .08, .16), m["body"], .025)


def build_crown(m):
    cube("bone-gravel-dais", (0, 0, .22), (1.80, 1.42, .22), m["stone"], .08)
    for side in (-1, 1):
        tube(f"rotwood-arch-leg-{side}", (side * 1.42, 0, .30), (side * .86, 0, 2.70), .13, m["body"], 12)
        tube(f"rotwood-arch-crown-{side}", (side * .86, 0, 2.70), (0, 0, 3.28), .11, m["body"], 12)
    cube("reliquary-table", (0, 0, 1.10), (1.34, .92, .13), m["body"], .05)
    for i in range(8):
        a = i * math.tau / 8
        tube(f"amber-cage-{i}", (math.cos(a) * .54, math.sin(a) * .54, 1.18),
             (math.cos(a) * .32, math.sin(a) * .32, 2.30), .032, m["metal"], 8)
    sphere("carrion-amber-heart", (0, 0, 1.88), .38, m["accent"], 20, 12)
    for z, r in ((1.32, .58), (1.84, .46), (2.30, .34)):
        torus(f"reliquary-ring-{z}", (0, 0, z), r, .045, m["metal"])
    torus("crown-halo", (0, 0, 2.92), .72, .07, m["accent"], (math.pi / 2, 0, 0))
    for i in range(9):
        a = i * math.tau / 9
        tube(f"crown-ray-{i}", (math.cos(a) * .62, 0, 2.92 + math.sin(a) * .62),
             (math.cos(a) * .91, 0, 2.92 + math.sin(a) * .91), .035, m["metal"], 8)
        sphere(f"crown-amber-{i}", (math.cos(a) * .93, 0, 2.92 + math.sin(a) * .93), .075, m["accent"], 10, 7)
    for i in range(6):
        x = -1.15 + i * .46
        cube(f"bone-reliquary-drawer-{i}", (x, -.92, .78), (.18, .12, .16), m["stone"], .02)


BUILDERS = {
    "mycelial": build_mycelial,
    "tidal": build_tidal,
    "furnace": build_furnace,
    "silence": build_silence,
    "anchor": build_anchor,
    "crown": build_crown,
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
    scene["magenheim_fidelity"] = "endgame-station-r2"
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
