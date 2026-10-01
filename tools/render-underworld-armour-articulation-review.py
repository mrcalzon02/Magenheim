#!/usr/bin/env python3
"""Render a stress-pose review of all 24 rigged Underworld armour sources.

This is a review-only pose. It deliberately exaggerates shoulder, forearm, hip, spine and head
articulation so rigid weights, detached plates and bad attach_skin assignments are easier to see
before the production branch is accepted.
"""
import math
from pathlib import Path

import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/"assets"/"models"/"source"
OUT=ROOT/"dist"/"underworld-production-review"/"armour-articulation"
OUT.mkdir(parents=True,exist_ok=True)
SETS=("sporeweave","palewater","emberiron","rimeward","stoneanchor","defiant")
SLOTS=("helmet","chest","legs","cape")
IDS=tuple(f"underworld-armor-{family}-{slot}" for family in SETS for slot in SLOTS)
SIZE=320

def pose_bone(arm,name,xyz):
    pb=arm.pose.bones.get(name)
    if pb is None: raise RuntimeError(f"{arm.name}: missing stress-pose bone {name}")
    pb.rotation_mode="XYZ"
    pb.rotation_euler=tuple(math.radians(value) for value in xyz)

def apply_stress_pose(arm):
    pose_bone(arm,"Spine",(5,0,-7))
    pose_bone(arm,"Spine1",(-4,0,11))
    pose_bone(arm,"Spine2",(6,0,-9))
    pose_bone(arm,"Neck",(0,8,0))
    pose_bone(arm,"Head",(0,-18,8))
    pose_bone(arm,"LeftShoulder",(0,0,12))
    pose_bone(arm,"RightShoulder",(0,0,-12))
    pose_bone(arm,"LeftArm",(0,-48,-34))
    pose_bone(arm,"RightArm",(0,42,31))
    pose_bone(arm,"LeftForeArm",(8,0,-58))
    pose_bone(arm,"RightForeArm",(-8,0,54))
    pose_bone(arm,"LeftUpLeg",(10,-23,9))
    pose_bone(arm,"RightUpLeg",(-8,20,-8))
    pose_bone(arm,"LeftLeg",(28,0,0))
    pose_bone(arm,"RightLeg",(12,0,0))

def bounds(meshes):
    depsgraph=bpy.context.evaluated_depsgraph_get()
    points=[]
    for obj in meshes:
        evaluated=obj.evaluated_get(depsgraph)
        points.extend(evaluated.matrix_world@Vector(corner) for corner in evaluated.bound_box)
    if not points: raise RuntimeError("stress-pose review has no mesh bounds")
    lo=Vector([min(p[i] for p in points) for i in range(3)])
    hi=Vector([max(p[i] for p in points) for i in range(3)])
    return lo,hi

def render(model_id):
    path=SOURCE/(model_id+".blend")
    if not path.is_file(): raise RuntimeError("missing armour source "+str(path))
    bpy.ops.wm.open_mainfile(filepath=str(path))
    scene=bpy.context.scene
    arms=[obj for obj in scene.objects if obj.type=="ARMATURE"]
    meshes=[obj for obj in scene.objects if obj.type=="MESH"]
    if len(arms)!=1 or not meshes:
        raise RuntimeError(f"{model_id}: expected one armature and visible mesh objects")
    arm=arms[0]
    apply_stress_pose(arm)
    scene.frame_set(scene.frame_current)
    bpy.context.view_layer.update()

    # Remove source cameras/lights so review composition is deterministic.
    for obj in list(scene.objects):
        if obj.type in {"CAMERA","LIGHT"}:
            bpy.data.objects.remove(obj,do_unlink=True)

    scene.render.engine="BLENDER_EEVEE"
    scene.render.resolution_x=SIZE
    scene.render.resolution_y=SIZE
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format="PNG"
    scene.render.image_settings.color_mode="RGBA"
    scene.render.film_transparent=False
    scene.view_settings.view_transform="AgX"
    scene.world=bpy.data.worlds.get("ArmourStressWorld") or bpy.data.worlds.new("ArmourStressWorld")
    scene.world.use_nodes=True
    bg=scene.world.node_tree.nodes["Background"]
    bg.inputs[0].default_value=(.115,.125,.145,1)
    bg.inputs[1].default_value=.52

    lo,hi=bounds(meshes)
    center=(lo+hi)*.5
    height=max((hi-lo).y,(hi-lo).z,.001)
    width=max((hi-lo).x,.001)
    extent=max(width,height)

    camera_data=bpy.data.cameras.new("StressCamera")
    camera_data.type="ORTHO"
    camera_data.ortho_scale=extent*1.22
    camera=bpy.data.objects.new("StressCamera",camera_data)
    scene.collection.objects.link(camera)
    camera.location=(center.x,center.y-6.0,center.z)
    # Point camera's -Z axis at the posed armour center.
    direction=center-camera.location
    camera.rotation_euler=direction.to_track_quat("-Z","Y").to_euler()
    scene.camera=camera

    for name,pos,energy,size in (
        ("StressKey",(center.x-2.7,center.y-2.8,center.z+3.6),1150,5.5),
        ("StressFill",(center.x+3.0,center.y-1.0,center.z+2.0),620,6.0),
        ("StressRim",(center.x,center.y+2.6,center.z+3.0),480,4.0),
    ):
        data=bpy.data.lights.new(name,"AREA")
        data.energy=energy;data.shape="DISK";data.size=size
        light=bpy.data.objects.new(name,data);scene.collection.objects.link(light);light.location=pos

    target=OUT/(model_id+".png")
    scene.render.filepath=str(target)
    bpy.ops.render.render(write_still=True)
    print("RENDERED",model_id,"articulation-stress",flush=True)

for model_id in IDS: render(model_id)
print("RENDERED",len(IDS),"Underworld armour articulation-stress reviews",flush=True)
