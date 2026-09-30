#!/usr/bin/env python3
"""Render actual Blackstone Throne Blender sources for visual acceptance."""
from pathlib import Path
import json
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/"assets"/"models"/"source"/"dark-throne.blend"
OUT=ROOT/"dist"/"blackstone-throne-review"
OUT.mkdir(parents=True,exist_ok=True)

if not SOURCE.is_file():
    raise SystemExit("dark-throne.blend missing; run Blackstone authoring before review render")

bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene=bpy.context.scene
scene.render.engine="BLENDER_EEVEE_NEXT"
scene.render.resolution_x=1440
scene.render.resolution_y=900
scene.render.resolution_percentage=100
scene.render.image_settings.file_format="PNG"
scene.render.film_transparent=False
scene.world.color=(.008,.012,.025)

def bpos(world):
    x,y,z=world
    return Vector((x,-z,y))

def look(camera,target):
    direction=bpos(target)-camera.location
    camera.rotation_euler=direction.to_track_quat("-Z","Y").to_euler()

def point(name,world,color,energy=1100,soft=1.8):
    data=bpy.data.lights.new(name,"POINT")
    data.color=color; data.energy=energy; data.shadow_soft_size=soft
    obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj); obj.location=bpos(world)
    return obj

# Runtime-authored flame positions become real review lights.
for i,light in enumerate(json.loads(scene.get("runtime_lights","[]"))):
    color=tuple(light["color"])
    energy=float(light["intensity"])*520.0
    obj=point("ReviewRuntimeLight_%02d"%i,light["position"],color,energy,1.6)
    obj.data.cutoff_distance=float(light["range"])

# Cold vertical hall fill establishes the source-reference contrast without changing materials.
for name,world,color,energy,size in (
    ("ColdVaultKey",(0,28,2),(.20,.33,.70),2400,12.0),
    ("ColdRearFill",(0,24,30),(.18,.22,.52),1800,10.0),
    ("WarmThroneFill",(0,19,22),(1.0,.24,.06),1200,7.0),
):
    data=bpy.data.lights.new(name,"AREA"); data.color=color; data.energy=energy; data.shape="DISK"; data.size=size
    obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj); obj.location=bpos(world)
    direction=bpos((0,10,10))-obj.location; obj.rotation_euler=direction.to_track_quat("-Z","Y").to_euler()

cam_data=bpy.data.cameras.new("BlackstoneReviewCamera")
cam=bpy.data.objects.new("BlackstoneReviewCamera",cam_data)
scene.collection.objects.link(cam); scene.camera=cam
cam_data.lens=30

views=(
    ("01-approach",(0,10,-42),(0,11,15),31),
    ("02-three-quarter",(37,24,-28),(0,10,10),34),
    ("03-side-elevation",(-43,18,4),(0,10,7),38),
    ("04-throne-close",(18,18,7),(0,16,25),42),
)
for name,position,target,lens in views:
    cam.location=bpos(position); cam_data.lens=lens; look(cam,target)
    scene.render.filepath=str(OUT/(name+".png"))
    bpy.ops.render.render(write_still=True)
    print("RENDERED",name,flush=True)

print("BLACKSTONE REVIEW",len(views),"actual Blender renders ->",OUT,flush=True)
