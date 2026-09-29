#!/usr/bin/env python3
"""Author the six progression-opening Underworld biome tools.

These are owned Magenheim forms, not recoloured vanilla tools. Their runtime verbs remain separate
gameplay work; this file establishes production source geometry, UVs and material language.
"""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/"assets"/"models"/"source"
DETAIL_FLOOR=18

SPECS={
 "underworld-tool-sporelight-lantern":"sporelight",
 "underworld-tool-diving-bell-hood":"diving-bell",
 "underworld-tool-slag-pick":"slag-pick",
 "underworld-tool-rime-chisel":"rime-chisel",
 "underworld-tool-anchor-spike":"anchor-spike",
 "underworld-tool-defiant-censer":"defiant-censer",
}

def mat(name,color,metal=0.0,rough=.65,emission=None):
 m=bpy.data.materials.new(name);m.use_nodes=True
 bs=m.node_tree.nodes.get("Principled BSDF")
 bs.inputs["Base Color"].default_value=(*color,1)
 bs.inputs["Metallic"].default_value=metal
 bs.inputs["Roughness"].default_value=rough
 if emission is not None and "Emission Color" in bs.inputs:
  bs.inputs["Emission Color"].default_value=(*emission,1)
  bs.inputs["Emission Strength"].default_value=.65
 return m

def finish(o,name,material):
 o.name=name;o["game_node_path"]=name;o["game_collision"]=False;o["game_crystal"]=json.dumps(None)
 o.data.materials.clear();o.data.materials.append(material)
 if not o.data.uv_layers:o.data.uv_layers.new(name="ToolUV")
 bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode="EDIT")
 bpy.ops.mesh.select_all(action="SELECT");bpy.ops.uv.cube_project(cube_size=.28)
 bpy.ops.object.mode_set(mode="OBJECT");o.select_set(False)
 return o

def cube(name,loc,scale,m,bevel=.025,rot=(0,0,0)):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc,rotation=rot);o=bpy.context.object;o.scale=scale
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new("worked-edge","BEVEL");mod.width=bevel;mod.segments=2
 return finish(o,name,m)

def cyl(name,loc,radius,depth,m,verts=14,rot=(0,0,0)):
 bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=radius,depth=depth,location=loc,rotation=rot)
 return finish(bpy.context.object,name,m)

def sphere(name,loc,radius,m,segments=14,rings=9):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=radius,location=loc)
 return finish(bpy.context.object,name,m)

def torus(name,loc,major,minor,m,rot=(0,0,0)):
 bpy.ops.mesh.primitive_torus_add(major_segments=20,minor_segments=7,major_radius=major,minor_radius=minor,location=loc,rotation=rot)
 return finish(bpy.context.object,name,m)

def tube(name,a,b,r,m,verts=10):
 a=Vector(a);b=Vector(b);d=b-a
 if d.length<=1e-5:raise ValueError(name+" degenerate")
 bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d.length,location=(a+b)*.5)
 o=bpy.context.object;o.rotation_mode="QUATERNION";o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized())
 return finish(o,name,m)

def palette(kind):
 if kind=="sporelight":
  return (mat("tool.sporelight.root",(.24,.15,.07),0,.80),mat("tool.sporelight.iron",(.12,.13,.12),.72,.38),mat("tool.sporelight.glowcap",(.38,.78,.42),0,.26,(.08,.36,.12)))
 if kind=="diving-bell":
  return (mat("tool.diving.flowstone",(.28,.38,.40),0,.82),mat("tool.diving.pearl-metal",(.38,.49,.51),.55,.28),mat("tool.diving.pearl",(.62,.84,.85),.08,.18,(.08,.24,.27)))
 if kind=="slag-pick":
  return (mat("tool.slag.charred-root",(.17,.085,.04),0,.84),mat("tool.slag.emberiron",(.28,.15,.09),.86,.28),mat("tool.slag.heat",(.90,.28,.05),.10,.20,(.55,.10,.01)))
 if kind=="rime-chisel":
  return (mat("tool.rime.rimewood",(.34,.43,.45),0,.72),mat("tool.rime.rimesilver",(.72,.78,.80),.92,.18),mat("tool.rime.clear-ice",(.65,.88,.96),0,.14,(.10,.28,.36)))
 if kind=="anchor-spike":
  return (mat("tool.anchor.titanbone",(.55,.50,.40),0,.70),mat("tool.anchor.dark-metal",(.15,.14,.17),.82,.32),mat("tool.anchor.fracture",(.57,.40,.78),0,.16,(.22,.09,.38)))
 return (mat("tool.censer.rotwood",(.18,.11,.05),0,.82),mat("tool.censer.binding",(.20,.14,.09),.72,.30),mat("tool.censer.amber",(.78,.45,.10),.05,.16,(.36,.14,.02)))

def sporelight(a,b,c):
 tube("root-handle",(-.17,0,.56),(-.17,0,1.20),.07,a,12);tube("root-handle-r",( .17,0,.56),( .17,0,1.20),.07,a,12)
 tube("handle-crown",(-.17,0,1.20),(.17,0,1.20),.06,a,12)
 for z in (.38,.64,.90):
  torus("iron-cage-"+str(z),(0,0,z),.25,.025,b)
 for i in range(6):
  q=i*math.tau/6;x=math.cos(q)*.23;y=math.sin(q)*.23
  tube("cage-rib-"+str(i),(x,y,.30),(x,y,.98),.018,b,8)
 for i,(x,y,z,r) in enumerate(((0,0,.55,.18),(.10,.04,.73,.12),(-.11,-.02,.70,.11),(0,.08,.40,.10))):
  sphere("glowcap-"+str(i),(x,y,z),r,c,16,10)
 for i in range(4):
  tube("root-foot-"+str(i),(0,0,.28),(math.cos(i*math.pi/2)*.25,math.sin(i*math.pi/2)*.25,.10),.035,a,8)

def diving(a,b,c):
 # Bell is a wearable traversal tool, but this source is the owned item/display shell; attach_skin comes later.
 for z,r in ((.25,.48),(.42,.52),(.62,.50),(.82,.44),(.98,.35)):
  torus("flowstone-band-"+str(z),(0,0,z),r,.07,a)
 for i in range(8):
  q=i*math.tau/8
  tube("bell-rib-"+str(i),(math.cos(q)*.47,math.sin(q)*.47,.28),(math.cos(q)*.30,math.sin(q)*.30,1.12),.035,b,8)
 torus("neck-seal",(0,0,.16),.31,.055,b)
 for i in range(6):
  q=i*math.tau/6;sphere("pearl-gauge-"+str(i),(math.cos(q)*.48,math.sin(q)*.48,.64),.065,c,12,8)
 cube("face-window",(0,-.47,.67),(.27,.035,.19),c,.02)
 for side in (-1,1):
  tube("pale-fibre-harness-"+str(side),(side*.34,.20,.24),(side*.25,.18,1.03),.025,a,8)

def slag_pick(a,b,c):
 tube("charred-haft",(0,0,-.48),(0,0,.68),.075,a,12)
 for z in (-.25,.04,.33,.58):torus("grip-band-"+str(z),(0,0,z),.085,.017,b)
 cube("pick-eye",(0,0,.72),(.22,.16,.16),b,.035)
 tube("pick-left",(-.02,0,.76),(-.70,0,.96),.10,b,12)
 tube("pick-right",(.02,0,.76),(.58,0,.62),.10,b,12)
 for x in (-.55,-.30,.30,.48):sphere("heat-rivet-"+str(x),(x,0,.80 if x<0 else .70),.04,c,10,6)
 for side in (-1,1):
  tube("root-head-brace-"+str(side),(side*.08,0,.60),(side*.36,0,.79),.035,a,8)

def rime_chisel(a,b,c):
 tube("rimewood-grip",(0,0,-.42),(0,0,.38),.07,a,12)
 for z in (-.30,-.08,.15,.34):torus("silver-grip-"+str(z),(0,0,z),.078,.014,b)
 tube("silver-spine",(0,0,.32),(0,0,.86),.055,b,12)
 # faceted ice cutting nose
 for i in range(6):
  q=i*math.tau/6
  tube("ice-fin-"+str(i),(0,0,.74),(math.cos(q)*.16,math.sin(q)*.16,1.04),.028,c,8)
 sphere("ice-core",(0,0,.86),.13,c,12,8)
 for i in range(4):
  q=i*math.pi/2;tube("precision-prong-"+str(i),(math.cos(q)*.08,math.sin(q)*.08,.52),(math.cos(q)*.13,math.sin(q)*.13,.80),.018,b,8)

def anchor_spike(a,b,c):
 # Massive deployable ground-stabilizer, not a handheld knife.
 tube("titanbone-core",(0,0,-.72),(0,0,.70),.13,a,14)
 cyl("driving-cap",(0,0,.78),.28,.18,b,16)
 for z in (-.40,.02,.42):torus("anchor-collar-"+str(z),(0,0,z),.18,.035,b)
 for i in range(6):
  q=i*math.tau/6
  tube("ground-fin-"+str(i),(math.cos(q)*.10,math.sin(q)*.10,-.52),(math.cos(q)*.48,math.sin(q)*.48,-.94),.055,b,9)
  sphere("fracture-node-"+str(i),(math.cos(q)*.20,math.sin(q)*.20,-.34),.055,c,10,6)
 for i in range(4):
  q=i*math.pi/2;tube("locking-brace-"+str(i),(math.cos(q)*.15,math.sin(q)*.15,.15),(math.cos(q)*.34,math.sin(q)*.34,.52),.035,a,8)

def censer(a,b,c):
 tube("rotwood-grip",(0,0,.72),(0,0,1.15),.055,a,10)
 torus("carry-ring",(0,0,1.28),.25,.035,b)
 for i in range(4):
  q=i*math.pi/2;tube("chain-"+str(i),(math.cos(q)*.18,math.sin(q)*.18,1.10),(math.cos(q)*.28,math.sin(q)*.28,.46),.018,b,7)
 torus("bowl-rim",(0,0,.42),.38,.055,b)
 cyl("bowl",(0,0,.30),.34,.22,a,18)
 for i in range(8):
  q=i*math.tau/8
  sphere("amber-vent-"+str(i),(math.cos(q)*.32,math.sin(q)*.32,.34),.045,c,10,6)
  tube("bone-ray-"+str(i),(math.cos(q)*.25,math.sin(q)*.25,.25),(math.cos(q)*.46,math.sin(q)*.46,.08),.025,b,8)
 sphere("amber-heart",(0,0,.34),.13,c,14,9)

BUILD={"sporelight":sporelight,"diving-bell":diving,"slag-pick":slag_pick,"rime-chisel":rime_chisel,"anchor-spike":anchor_spike,"defiant-censer":censer}

def author(model_id):
 kind=SPECS[model_id];bpy.ops.wm.read_factory_settings(use_empty=True);m=palette(kind);BUILD[kind](*m)
 meshes=[o for o in bpy.context.scene.objects if o.type=="MESH"]
 if len(meshes)<DETAIL_FLOOR:raise RuntimeError(f"{model_id}: tool detail regression {len(meshes)} < {DETAIL_FLOOR}")
 if any(not o.data.uv_layers.get("ToolUV") for o in meshes):raise RuntimeError(model_id+": missing ToolUV")
 sc=bpy.context.scene;sc["model_id"]=model_id;sc["magenheim_family"]="underworld_biome_tool";sc["magenheim_tool_kind"]=kind
 sc["magenheim_fidelity"]="endgame-tool-r1";sc["magenheim_detail_parts"]=len(meshes);sc["runtime_lights"]="[]"
 bpy.context.preferences.filepaths.save_version=0
 out=SOURCE/(model_id+".blend");bpy.ops.wm.save_as_mainfile(filepath=str(out),compress=True)
 print("AUTHORED",model_id,kind,len(meshes),"parts",flush=True)

requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else list(SPECS)
unknown=[x for x in requested if x not in SPECS]
if unknown:raise SystemExit("Unknown Underworld tool model(s): "+", ".join(unknown))
for model_id in requested:author(model_id)
