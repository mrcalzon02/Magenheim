#!/usr/bin/env python3
"""Render Rime Sepulcher context and top-down acceptance views."""
import bpy,math
from pathlib import Path
from mathutils import Matrix,Vector
ROOT=Path(__file__).resolve().parents[1];SOURCE=ROOT/"assets/models/source"
OUT=ROOT/"dist/underworld-production-review/rime-sepulcher-renders";OUT.mkdir(parents=True,exist_ok=True)
PREFIX="underworld-dungeon-frozen-rime-sepulcher-"
SUFFIXES=["rime-mouth","long-glass-gallery","burial-colonnade","silent-crossing","icewell-shaft","whiteout-narthex","needle-pass","rimesilver-ossuary","clear-ice-lens-vault","still-air-crypt","frost-tick-niche","iceblind-hunt","cryolith-guard","frozen-archive","shelter-chapel","white-silence-antechamber","passage"]
def setup():
 sc=bpy.context.scene;sc.render.engine="BLENDER_EEVEE_NEXT";sc.render.resolution_x=480;sc.render.resolution_y=360;sc.render.resolution_percentage=100;sc.render.image_settings.file_format="PNG";sc.render.film_transparent=False;sc.view_settings.view_transform="AgX"
 sc.world=bpy.data.worlds.new("RimeSepulcherReview");sc.world.use_nodes=True;bg=sc.world.node_tree.nodes["Background"];bg.inputs[0].default_value=(.025,.055,.075,1);bg.inputs[1].default_value=.32
 for name,pos,power,size,color in (("IceKey",(-4,-4,8),1200,6,(.50,.78,1.0)),("RimeFill",(5,2,5),520,7,(.78,.92,1.0)),("SilverRim",(0,5,3),330,5,(.34,.54,.72))):
  d=bpy.data.lights.new(name,"AREA");d.energy=power;d.shape="DISK";d.size=size;d.color=color;o=bpy.data.objects.new(name,d);sc.collection.objects.link(o);o.location=pos
def load(mid):
 with bpy.data.libraries.load(str(SOURCE/(mid+".blend")),link=False) as (src,dst):dst.objects=src.objects
 objs=[o for o in dst.objects if o]
 for o in objs:bpy.context.scene.collection.objects.link(o)
 meshes=[o for o in objs if o.type=="MESH"]
 if not meshes:raise RuntimeError(mid+": no meshes")
 return objs,meshes
def bounds(meshes):
 bpy.context.view_layer.update();pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
 return Vector([min(p[i] for p in pts) for i in range(3)]),Vector([max(p[i] for p in pts) for i in range(3)])
def cam(scale):
 d=bpy.data.cameras.new("Camera");d.type="ORTHO";d.ortho_scale=scale;o=bpy.data.objects.new("Camera",d);bpy.context.scene.collection.objects.link(o);o.location=(0,0,12);bpy.context.scene.camera=o
def render(mid,mode):
 bpy.ops.wm.read_factory_settings(use_empty=True);setup();objs,meshes=load(mid)
 if mode=="context":
  rot=Matrix.Rotation(math.radians(-30),4,"Z")@Matrix.Rotation(math.radians(-58),4,"X")@Matrix.Rotation(math.radians(8),4,"Y")
  for o in objs:o.matrix_world=rot@o.matrix_world
 lo,hi=bounds(meshes);center=(lo+hi)*.5;extent=max((hi-lo).x,(hi-lo).y,(hi-lo).z*.78,.001);place=Matrix.Scale(2.0/extent,4)@Matrix.Translation(-center)
 for o in objs:o.matrix_world=place@o.matrix_world
 cam(2.35);bpy.context.scene.render.filepath=str(OUT/f"{mid}--{mode}.png");bpy.ops.render.render(write_still=True);print("RENDERED",mid,mode,flush=True)
for suffix in SUFFIXES:
 for mode in ("context","top"):render(PREFIX+suffix,mode)
print("RENDERED Rime Sepulcher review",len(SUFFIXES),"models x 2 views")
