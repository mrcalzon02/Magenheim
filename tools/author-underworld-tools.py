#!/usr/bin/env python3
"""Author the six progression-opening Underworld biome tools.

These are owned Magenheim forms, not recoloured vanilla tools. Their runtime verbs remain separate
gameplay work; this file establishes production source geometry, UVs and material language.
"""
import json
import math
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from underworld_material_library import bind_underworld_material

import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/"assets"/"models"/"source"
DETAIL_FLOOR=18
BONE_ORDER=[
"Hips","Spine","Spine1","Spine2","Neck","Head","Jaw",
"LeftShoulder","LeftArm","LeftForeArm","LeftHand",
"LeftHandThumb1","LeftHandThumb2","LeftHandThumb3","LeftHandIndex1","LeftHandIndex2","LeftHandIndex3",
"LeftHandMiddle1","LeftHandMiddle2","LeftHandMiddle3","LeftHandRing1","LeftHandRing2","LeftHandRing3",
"LeftHandPinky1","LeftHandPinky2","LeftHandPinky3",
"RightShoulder","RightArm","RightForeArm","RightHand",
"RightHandThumb1","RightHandThumb2","RightHandThumb3","RightHandIndex1","RightHandIndex2","RightHandIndex3",
"RightHandMiddle1","RightHandMiddle2","RightHandMiddle3","RightHandRing1","RightHandRing2","RightHandRing3",
"RightHandPinky1","RightHandPinky2","RightHandPinky3",
"LeftUpLeg","LeftLeg","LeftFoot","LeftToeBase","RightUpLeg","RightLeg","RightFoot","RightToeBase",
]
ACTIVE_SKIN_ARMATURE=None

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
 bind_underworld_material(bpy,m,name)
 return m

def add_bone(eb,name,head,tail,parent=None):
 b=eb.new(name);b.head=head;b.tail=tail;b.parent=parent;return b

def build_player_rig():
 bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0))
 arm=bpy.context.object;arm.name="RIG_ValheimPlayer_AttachSkin";eb=arm.data.edit_bones
 root=eb[0];root.name="Hips";root.head=(0,0,.90);root.tail=(0,0,1.05);bones={"Hips":root}
 def B(name,h,t,parent):bones[name]=add_bone(eb,name,h,t,bones[parent]);return bones[name]
 B("Spine",(0,0,1.03),(0,0,1.24),"Hips");B("Spine1",(0,0,1.22),(0,0,1.43),"Spine")
 B("Spine2",(0,0,1.41),(0,0,1.62),"Spine1");B("Neck",(0,0,1.60),(0,0,1.76),"Spine2")
 B("Head",(0,0,1.74),(0,0,2.02),"Neck");B("Jaw",(0,-.03,1.83),(0,-.10,1.73),"Head")
 for side in ("Left","Right"):
  s=-1 if side=="Left" else 1
  B(side+"Shoulder",(0,0,1.57),(s*.22,0,1.57),"Spine2");B(side+"Arm",(s*.20,0,1.57),(s*.60,0,1.50),side+"Shoulder")
  B(side+"ForeArm",(s*.58,0,1.50),(s*.91,0,1.40),side+"Arm");B(side+"Hand",(s*.89,0,1.40),(s*1.08,0,1.37),side+"ForeArm")
  for fi,finger in enumerate(("Thumb","Index","Middle","Ring","Pinky")):
   parent=side+"Hand";base_y=-.045+fi*.022
   for seg in range(1,4):
    start=s*(1.04+.055*(seg-1));end=s*(1.04+.055*seg)
    B(side+"Hand"+finger+str(seg),(start,base_y,1.36),(end,base_y,1.35),parent);parent=side+"Hand"+finger+str(seg)
 for side in ("Left","Right"):
  s=-1 if side=="Left" else 1
  B(side+"UpLeg",(s*.14,0,.94),(s*.18,0,.55),"Hips");B(side+"Leg",(s*.18,0,.56),(s*.17,.01,.13),side+"UpLeg")
  B(side+"Foot",(s*.17,.01,.14),(s*.17,-.17,.055),side+"Leg");B(side+"ToeBase",(s*.17,-.16,.055),(s*.17,-.30,.045),side+"Foot")
 bpy.ops.object.mode_set(mode="OBJECT");arm.show_in_front=True
 if [b.name for b in arm.data.bones]!=BONE_ORDER:raise RuntimeError("Valheim player bone creation order drift")
 arm["magenheim_host_rig"]="VALHEIM-PLAYER-ATTACH-SKIN";arm["magenheim_bone_order"]=json.dumps(BONE_ORDER,separators=(",",":"))
 return arm

def finish(o,name,material):
 o.name=name;o["game_node_path"]=name;o["game_collision"]=False;o["game_crystal"]=json.dumps(None)
 o.data.materials.clear();o.data.materials.append(material)
 if not o.data.uv_layers:o.data.uv_layers.new(name="ToolUV")
 bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode="EDIT")
 bpy.ops.mesh.select_all(action="SELECT");bpy.ops.uv.cube_project(cube_size=.28)
 bpy.ops.object.mode_set(mode="OBJECT");o.select_set(False)
 if ACTIVE_SKIN_ARMATURE is not None:
  bone="Neck" if ("neck" in name or "harness" in name) else "Head"
  vg=o.vertex_groups.new(name=bone);vg.add(range(len(o.data.vertices)),1.0,"REPLACE")
  mod=o.modifiers.new("ValheimAttachSkin","ARMATURE");mod.object=ACTIVE_SKIN_ARMATURE;o.parent=ACTIVE_SKIN_ARMATURE
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
 global ACTIVE_SKIN_ARMATURE
 kind=SPECS[model_id];bpy.ops.wm.read_factory_settings(use_empty=True)
 ACTIVE_SKIN_ARMATURE=build_player_rig() if kind=="diving-bell" else None
 m=palette(kind);BUILD[kind](*m)
 meshes=[o for o in bpy.context.scene.objects if o.type=="MESH"]
 if len(meshes)<DETAIL_FLOOR:raise RuntimeError(f"{model_id}: tool detail regression {len(meshes)} < {DETAIL_FLOOR}")
 if any(not o.data.uv_layers.get("ToolUV") for o in meshes):raise RuntimeError(model_id+": missing ToolUV")
 if ACTIVE_SKIN_ARMATURE is not None:
  for o in meshes:
   if not o.modifiers.get("ValheimAttachSkin"):raise RuntimeError(model_id+"/"+o.name+": armature modifier missing")
   weighted={g.name for g in o.vertex_groups if any(vg.group==g.index and vg.weight>0 for v in o.data.vertices for vg in v.groups)}
   if not weighted or not weighted.issubset(set(BONE_ORDER)):raise RuntimeError(model_id+"/"+o.name+": invalid player bone weights")
 sc=bpy.context.scene;sc["model_id"]=model_id;sc["magenheim_family"]="underworld_biome_tool";sc["magenheim_tool_kind"]=kind
 sc["magenheim_fidelity"]="endgame-tool-r1";sc["magenheim_detail_parts"]=len(meshes)
 lights=[] if kind!="sporelight" else [{"path":"sporelight-core","position":[0,.62,0],"color":[.34,1.0,.48],"range":7.5,"intensity":2.4}]
 sc["runtime_lights"]=json.dumps(lights,separators=(",",":"))
 if ACTIVE_SKIN_ARMATURE is not None:
  sc["magenheim_skinning"]="valheim-player-attach-skin";sc["magenheim_bone_order"]=json.dumps(BONE_ORDER,separators=(",",":"))
 bpy.context.preferences.filepaths.save_version=0
 out=SOURCE/(model_id+".blend");bpy.ops.wm.save_as_mainfile(filepath=str(out),compress=True)
 print("AUTHORED",model_id,kind,len(meshes),"parts",("/ 53 bones" if ACTIVE_SKIN_ARMATURE is not None else ""),flush=True)
 ACTIVE_SKIN_ARMATURE=None

requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else list(SPECS)
unknown=[x for x in requested if x not in SPECS]
if unknown:raise SystemExit("Unknown Underworld tool model(s): "+", ".join(unknown))
for model_id in requested:author(model_id)
