#!/usr/bin/env python3
"""Author all twenty-four owned Underworld armour pieces on the canonical Valheim player bone order.

The sources are genuine skinned wearable art. The production exporter serializes their canonical
player-bone weights into .model.json so ModelAssets can rebuild SkinnedMeshRenderer equipment under
Valheim's native attach_skin hierarchy without an AssetBundle-only parallel pipeline.
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

SETS=("sporeweave","palewater","emberiron","rimeward","stoneanchor","defiant")
SLOTS=("helmet","chest","legs","cape")
DETAIL_FLOOR={"helmet":16,"chest":26,"legs":22,"cape":18}

def material(name,color,metal=0.0,rough=.65,emission=None):
 m=bpy.data.materials.new(name);m.use_nodes=True
 bs=m.node_tree.nodes.get("Principled BSDF");bs.inputs["Base Color"].default_value=(*color,1)
 bs.inputs["Metallic"].default_value=metal;bs.inputs["Roughness"].default_value=rough
 if emission is not None and "Emission Color" in bs.inputs:
  bs.inputs["Emission Color"].default_value=(*emission,1);bs.inputs["Emission Strength"].default_value=.45
 bind_underworld_material(bpy,m,name)
 return m

def palette(kind):
 data={
 "sporeweave":((.25,.17,.08),(.34,.52,.28),(.44,.78,.38),0.05),
 "palewater":((.48,.52,.47),(.27,.38,.40),(.62,.84,.84),0.20),
 "emberiron":((.16,.08,.04),(.29,.15,.09),(.86,.26,.04),0.82),
 "rimeward":((.34,.44,.46),(.72,.78,.80),(.68,.90,.96),0.88),
 "stoneanchor":((.54,.49,.39),(.22,.20,.26),(.58,.40,.78),0.70),
 "defiant":((.18,.11,.05),(.34,.29,.22),(.78,.45,.10),0.55),
 }[kind]
 return (
  material("armour."+kind+".base",data[0],0,.72),
  material("armour."+kind+".structure",data[1],data[3],.34 if data[3]>.5 else .62),
  material("armour."+kind+".accent",data[2],.05,.18,data[2]),
 )

def add_bone(eb,name,head,tail,parent=None):
 b=eb.new(name);b.head=head;b.tail=tail;b.parent=parent;return b

def build_rig():
 bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0))
 arm=bpy.context.object;arm.name="RIG_ValheimPlayer_AttachSkin";eb=arm.data.edit_bones
 root=eb[0];root.name="Hips";root.head=(0,0,.90);root.tail=(0,0,1.05)
 bones={"Hips":root}
 def B(name,h,t,parent):
  bones[name]=add_bone(eb,name,h,t,bones[parent]);return bones[name]
 B("Spine",(0,0,1.03),(0,0,1.24),"Hips");B("Spine1",(0,0,1.22),(0,0,1.43),"Spine")
 B("Spine2",(0,0,1.41),(0,0,1.62),"Spine1");B("Neck",(0,0,1.60),(0,0,1.76),"Spine2")
 B("Head",(0,0,1.74),(0,0,2.02),"Neck");B("Jaw",(0,-.03,1.83),(0,-.10,1.73),"Head")
 for side in ("Left","Right"):
  s=-1 if side=="Left" else 1
  B(side+"Shoulder",(0,0,1.57),(s*.22,0,1.57),"Spine2")
  B(side+"Arm",(s*.20,0,1.57),(s*.60,0,1.50),side+"Shoulder")
  B(side+"ForeArm",(s*.58,0,1.50),(s*.91,0,1.40),side+"Arm")
  B(side+"Hand",(s*.89,0,1.40),(s*1.08,0,1.37),side+"ForeArm")
  finger_names=("Thumb","Index","Middle","Ring","Pinky")
  for fi,finger in enumerate(finger_names):
   parent=side+"Hand";base_y=-.045+fi*.022
   for seg in range(1,4):
    start=s*(1.04+.055*(seg-1));end=s*(1.04+.055*seg)
    B(side+"Hand"+finger+str(seg),(start,base_y,1.36),(end,base_y,1.35),parent);parent=side+"Hand"+finger+str(seg)
 for side in ("Left","Right"):
  s=-1 if side=="Left" else 1
  B(side+"UpLeg",(s*.14,0,.94),(s*.18,0,.55),"Hips")
  B(side+"Leg",(s*.18,0,.56),(s*.17,.01,.13),side+"UpLeg")
  B(side+"Foot",(s*.17,.01,.14),(s*.17,-.17,.055),side+"Leg")
  B(side+"ToeBase",(s*.17,-.16,.055),(s*.17,-.30,.045),side+"Foot")
 bpy.ops.object.mode_set(mode="OBJECT");arm.show_in_front=True
 if [b.name for b in arm.data.bones]!=BONE_ORDER:
  raise RuntimeError("Valheim player bone creation order drift")
 arm["magenheim_host_rig"]="VALHEIM-PLAYER-ATTACH-SKIN"
 arm["magenheim_bone_order"]=json.dumps(BONE_ORDER,separators=(",",":"))
 return arm

def finish(o,name,m,arm,bone):
 o.name=name;o["game_node_path"]=name;o["game_collision"]=False;o["game_crystal"]=json.dumps(None)
 o.data.materials.clear();o.data.materials.append(m)
 if o.data.uv_layers.get("ArmourUV") is None:o.data.uv_layers.new(name="ArmourUV")
 o.data.uv_layers.active_index=o.data.uv_layers.find("ArmourUV")
 bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode="EDIT");bpy.ops.mesh.select_all(action="SELECT")
 bpy.ops.uv.cube_project(cube_size=.32);bpy.ops.object.mode_set(mode="OBJECT");o.select_set(False)
 vg=o.vertex_groups.new(name=bone);vg.add(range(len(o.data.vertices)),1.0,"REPLACE")
 mod=o.modifiers.new("ValheimAttachSkin","ARMATURE");mod.object=arm;o.parent=arm
 return o

def cube(name,loc,scale,m,arm,bone,bevel=.025,rot=(0,0,0)):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc,rotation=rot);o=bpy.context.object;o.scale=scale
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  md=o.modifiers.new("worked-edge","BEVEL");md.width=bevel;md.segments=2
 return finish(o,name,m,arm,bone)

def sphere(name,loc,scale,m,arm,bone):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=10,radius=1,location=loc);o=bpy.context.object;o.scale=scale
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,m,arm,bone)

def tube(name,a,b,r,m,arm,bone,verts=10):
 a=Vector(a);b=Vector(b);d=b-a;bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d.length,location=(a+b)*.5)
 o=bpy.context.object;o.rotation_mode="QUATERNION";o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized())
 return finish(o,name,m,arm,bone)

def torus(name,loc,major,minor,m,arm,bone,rot=(0,0,0)):
 bpy.ops.mesh.primitive_torus_add(major_segments=20,minor_segments=7,major_radius=major,minor_radius=minor,location=loc,rotation=rot)
 return finish(bpy.context.object,name,m,arm,bone)

def accent_shape(kind,name,loc,scale,m,arm,bone,index):
 if kind in ("sporeweave","defiant"):
  return sphere(name,loc,scale,m,arm,bone)
 if kind in ("palewater","rimeward"):
  return cube(name,loc,scale,m,arm,bone,.018,(0,0,(index%3-1)*.12))
 return cube(name,loc,scale,m,arm,bone,.03,(0,(index%2)*.12,0))

def helmet(kind,m,arm):
 base,structure,accent=m
 torus("brow-band",(0,-.01,1.89),.25,.045,structure,arm,"Head",(math.pi/2,0,0))
 for i in range(8):
  q=i*math.tau/8;x=math.cos(q)*.22;y=math.sin(q)*.17
  tube("crown-rib-"+str(i),(x,y,1.88),(x*.45,y*.45,2.08),.032,base,arm,"Head",8)
 for i in range(5):
  x=-.18+i*.09;accent_shape(kind,"helm-accent-"+str(i),(x,-.19,1.94+.025*(i%2)),(.045,.025,.06),accent,arm,"Head",i)
 for side in (-1,1):
  cube("cheek-guard-"+str(side),(side*.24,-.04,1.82),(.055,.11,.14),structure,arm,"Head",.025)
  tube("jaw-tie-"+str(side),(side*.22,-.03,1.78),(side*.13,-.08,1.68),.022,base,arm,"Head",8)
 sphere("crown-cap",(0,.01,2.01),(.17,.15,.08),base,arm,"Head")

def chest(kind,m,arm):
 base,structure,accent=m
 for bone,z,w,h in (("Spine",1.16,.28,.16),("Spine1",1.36,.34,.18),("Spine2",1.55,.38,.17)):
  cube("torso-layer-"+bone,(0,-.025,z),(w,.17,h),base,arm,bone,.035)
  cube("back-layer-"+bone,(0,.14,z),(w*.92,.055,h*.88),structure,arm,bone,.025)
 for side,name in ((-1,"Left"),(1,"Right")):
  cube("shoulder-"+name,(side*.28,0,1.58),(.13,.18,.10),structure,arm,name+"Shoulder",.035)
  for n,(bone,x,z) in enumerate(((name+"Arm",side*.43,1.52),(name+"ForeArm",side*.74,1.44))):
   tube("arm-guard-"+name+"-"+str(n),(x,0,z+.10),(x,0,z-.10),.085,base,arm,bone,10)
 for i in range(7):
  x=-.24+i*.08;accent_shape(kind,"chest-accent-"+str(i),(x,-.205,1.48+(.04 if i%2 else 0)),(.035,.025,.055),accent,arm,"Spine2",i)
 for i in range(5):
  tube("rib-binding-"+str(i),(-.30,-.19,1.20+i*.075),(.30,-.19,1.20+i*.075),.018,structure,arm,"Spine1",8)
 for side in (-1,1):
  tube("waist-brace-"+str(side),(side*.29,-.05,1.15),(side*.23,-.04,1.43),.035,structure,arm,"Spine1",8)

def legs(kind,m,arm):
 base,structure,accent=m
 cube("waist-front",(0,-.08,1.00),(.34,.13,.10),base,arm,"Hips",.035);cube("waist-back",(0,.10,1.00),(.33,.08,.09),structure,arm,"Hips",.025)
 for side,name in ((-1,"Left"),(1,"Right")):
  for idx,(bone,z,length) in enumerate(((name+"UpLeg",.73,.28),(name+"Leg",.34,.25))):
   x=side*(.19 if idx==0 else .17)
   tube("leg-shell-"+name+"-"+str(idx),(x,0,z+length*.5),(x,0,z-length*.5),.105 if idx==0 else .085,base,arm,bone,10)
   cube("knee-shin-plate-"+name+"-"+str(idx),(x,-.105,z),(.10,.045,.14),structure,arm,bone,.025)
  cube("foot-guard-"+name,(side*.17,-.10,.10),(.11,.16,.055),structure,arm,name+"Foot",.025)
  for i in range(4):
   accent_shape(kind,"leg-accent-"+name+"-"+str(i),(side*.19,-.11,.76-i*.17),(.035,.025,.045),accent,arm,name+"UpLeg" if i<2 else name+"Leg",i)
 for i in range(4):
  tube("hip-binding-"+str(i),(-.28,-.13,.94+i*.025),(.28,-.13,.94+i*.025),.018,structure,arm,"Hips",8)

def cape(kind,m,arm):
 base,structure,accent=m
 torus("mantle-collar",(0,.04,1.62),.30,.045,structure,arm,"Spine2",(math.pi/2,0,0))
 for col,x in enumerate((-.24,-.12,0,.12,.24)):
  # Three rigidly weighted overlapping panels approximate cloth while retaining correct host bones.
  for row,(bone,z0,z1,w) in enumerate((("Spine2",1.58,1.35,.075),("Spine1",1.34,1.08,.080),("Spine",1.07,.78,.085))):
   cube("cape-panel-"+str(col)+"-"+str(row),(x,.16,(z0+z1)/2),(w,.025,(z0-z1)/2),base,arm,bone,.015,(0,0,(col-2)*.025))
 for i,x in enumerate((-.22,-.11,0,.11,.22)):
  accent_shape(kind,"cape-clasp-"+str(i),(x,-.02,1.61),(.035,.025,.045),accent,arm,"Spine2",i)
 for side in (-1,1):
  tube("shoulder-chain-"+str(side),(side*.28,.02,1.60),(side*.18,.12,1.48),.022,structure,arm,"Spine2",8)

BUILD={"helmet":helmet,"chest":chest,"legs":legs,"cape":cape}

def author(kind,slot):
 model_id="underworld-armor-"+kind+"-"+slot
 bpy.ops.wm.read_factory_settings(use_empty=True);arm=build_rig();m=palette(kind);BUILD[slot](kind,m,arm)
 meshes=[o for o in bpy.context.scene.objects if o.type=="MESH"]
 if len(meshes)<DETAIL_FLOOR[slot]:raise RuntimeError(f"{model_id}: detail regression {len(meshes)} < {DETAIL_FLOOR[slot]}")
 for o in meshes:
  if not o.data.uv_layers.get("ArmourUV"):raise RuntimeError(model_id+"/"+o.name+": ArmourUV missing")
  if not o.modifiers.get("ValheimAttachSkin"):raise RuntimeError(model_id+"/"+o.name+": armature modifier missing")
  weighted={g.name for g in o.vertex_groups if any(vg.group==g.index and vg.weight>0 for v in o.data.vertices for vg in v.groups)}
  if not weighted:raise RuntimeError(model_id+"/"+o.name+": no bone weights")
  if not weighted.issubset(set(BONE_ORDER)):raise RuntimeError(model_id+"/"+o.name+": unknown bone group")
 sc=bpy.context.scene;sc["model_id"]=model_id;sc["magenheim_family"]="underworld_biome_armour";sc["magenheim_armour_set"]=kind;sc["magenheim_armour_slot"]=slot
 sc["magenheim_fidelity"]="endgame-armour-r1";sc["magenheim_skinning"]="valheim-player-attach-skin";sc["magenheim_bone_order"]=json.dumps(BONE_ORDER,separators=(",",":"));sc["runtime_lights"]="[]"
 bpy.context.preferences.filepaths.save_version=0;out=SOURCE/(model_id+".blend");bpy.ops.wm.save_as_mainfile(filepath=str(out),compress=True)
 print("AUTHORED",model_id,len(meshes),"parts /",len(arm.data.bones),"bones",flush=True)

requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else [f"underworld-armor-{s}-{p}" for s in SETS for p in SLOTS]
for model_id in requested:
 bits=model_id.split("-")
 if len(bits)<4 or not model_id.startswith("underworld-armor-"):raise SystemExit("Invalid armour model id: "+model_id)
 kind="-".join(bits[2:-1]);slot=bits[-1]
 if kind not in SETS or slot not in SLOTS:raise SystemExit("Unknown armour model id: "+model_id)
 author(kind,slot)
