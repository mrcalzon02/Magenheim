#!/usr/bin/env python3
"""Verify Underworld armour source rigs before any wearable runtime admission."""
import json,sys
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parents[1];SOURCE=ROOT/"assets"/"models"/"source"
SETS=("sporeweave","palewater","emberiron","rimeward","stoneanchor","defiant");SLOTS=("helmet","chest","legs","cape")
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
ids=[f"underworld-armor-{s}-{p}" for s in SETS for p in SLOTS]
requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else ids
for model_id in requested:
 path=SOURCE/(model_id+".blend")
 if not path.exists():raise RuntimeError("Missing armour source: "+str(path))
 bpy.ops.wm.open_mainfile(filepath=str(path));sc=bpy.context.scene
 if sc.get("model_id")!=model_id:raise RuntimeError(model_id+": scene identity drift")
 if sc.get("magenheim_skinning")!="valheim-player-attach-skin":raise RuntimeError(model_id+": attach_skin contract missing")
 arms=[o for o in sc.objects if o.type=="ARMATURE"]
 if len(arms)!=1:raise RuntimeError(model_id+": expected exactly one armature")
 arm=arms[0];actual=[b.name for b in arm.data.bones]
 if actual!=BONE_ORDER:raise RuntimeError(model_id+": player bone order mismatch")
 meshes=[o for o in sc.objects if o.type=="MESH"]
 if not meshes:raise RuntimeError(model_id+": no armour geometry")
 for o in meshes:
  mod=o.modifiers.get("ValheimAttachSkin")
  if mod is None or mod.object!=arm:raise RuntimeError(model_id+"/"+o.name+": invalid armature binding")
  if not o.data.uv_layers.get("ArmourUV"):raise RuntimeError(model_id+"/"+o.name+": ArmourUV missing")
  groups={g.name for g in o.vertex_groups}
  if not groups or not groups.issubset(set(BONE_ORDER)):raise RuntimeError(model_id+"/"+o.name+": invalid bone groups")
  if any(not v.groups for v in o.data.vertices):raise RuntimeError(model_id+"/"+o.name+": unweighted vertex")
 print("PASS",model_id,len(meshes),"parts / 53-bone attach_skin source",flush=True)
print("PASS Underworld armour source-rig gate:",len(requested),"piece(s)",flush=True)
