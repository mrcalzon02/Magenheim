#!/usr/bin/env python3
"""Render Drowned Vault room/passage context and top-down acceptance views."""
import bpy,math
from pathlib import Path
from mathutils import Matrix,Vector
ROOT=Path(__file__).resolve().parents[1];SOURCE=ROOT/"assets/models/source"
OUT=ROOT/"dist/underworld-production-review/drowned-vault-renders";OUT.mkdir(parents=True,exist_ok=True)
PREFIX="underworld-dungeon-blackwater-drowned-vaults-"
SUFFIXES=["drowned-sinkhole","tide-gallery","dry-ledger","collapsed-dock","siphon-hall","bell-chamber","split-cistern","drowned-shaft","pearl-vault","high-water-archive","lamprey-run","deep-hunter-lair","sunken-quay","broken-causeway","undertow-sluice","abyssal-sanctum","passage"]
def setup():
 sc=bpy.context.scene;sc.render.engine="BLENDER_EEVEE_NEXT";sc.render.resolution_x=480;sc.render.resolution_y=360;sc.render.resolution_percentage=100
 sc.render.image_settings.file_format="PNG";sc.render.film_transparent=False;sc.view_settings.view_transform="AgX"
 sc.world=bpy.data.worlds.new("DrownedVaultReview");sc.world.use_nodes=True;bg=sc.world.node_tree.nodes["Background"];bg.inputs[0].default_value=(.018,.045,.065,1);bg.inputs[1].default_value=.30
 for name,pos,power,size,color in (("BlackwaterKey",(-4,-4,8),1150,6,(.28,.68,.82)),("PearlFill",(5,2,5),540,7,(.52,.82,.86)),("ColdRim",(0,5,3),320,5,(.18,.30,.46))):
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
def camera(scale):
 d=bpy.data.cameras.new("Camera");d.type="ORTHO";d.ortho_scale=scale;o=bpy.data.objects.new("Camera",d);bpy.context.scene.collection.objects.link(o);o.location=(0,0,12);bpy.context.scene.camera=o
def render(mid,mode):
 bpy.ops.wm.read_factory_settings(use_empty=True);setup();objs,meshes=load(mid)
 if mode=="context":
  rot=Matrix.Rotation(math.radians(-30),4,"Z")@Matrix.Rotation(math.radians(-58),4,"X")@Matrix.Rotation(math.radians(8),4,"Y")
  for o in objs:o.matrix_world=rot@o.matrix_world
 lo,hi=bounds(meshes);center=(lo+hi)*.5;extent=max((hi-lo).x,(hi-lo).y,(hi-lo).z*.78,.001);place=Matrix.Scale(2.0/extent,4)@Matrix.Translation(-center)
 for o in objs:o.matrix_world=place@o.matrix_world
 camera(2.35);target=OUT/f"{mid}--{mode}.png";bpy.context.scene.render.filepath=str(target);bpy.ops.render.render(write_still=True);print("RENDERED",mid,mode,flush=True)
for suffix in SUFFIXES:
 mid=PREFIX+suffix
 for mode in ("context","top"):render(mid,mode)
print("RENDERED Drowned Vault review",len(SUFFIXES),"models x 2 views")
