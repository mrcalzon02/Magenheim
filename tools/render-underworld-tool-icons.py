#!/usr/bin/env python3
"""Render inventory icons for the six owned Underworld progression tools."""
import bpy,json,math,sys
from pathlib import Path
from mathutils import Vector,Matrix

ROOT=Path(__file__).resolve().parents[1];MODELS=ROOT/"assets"/"models";OUT=ROOT/"assets"/"earth"
IDS=(
 "underworld-tool-sporelight-lantern",
 "underworld-tool-diving-bell-hood",
 "underworld-tool-slag-pick",
 "underworld-tool-rime-chisel",
 "underworld-tool-anchor-spike",
 "underworld-tool-defiant-censer",
)

def render(entry):
 bpy.ops.wm.read_factory_settings(use_empty=True);sc=bpy.context.scene;sc.render.engine="CYCLES";sc.cycles.samples=96;sc.cycles.use_denoising=True
 sc.render.resolution_x=sc.render.resolution_y=256;sc.render.resolution_percentage=100;sc.render.film_transparent=True
 sc.render.image_settings.file_format="PNG";sc.render.image_settings.color_mode="RGBA";sc.view_settings.view_transform="AgX"
 sc.world=bpy.data.worlds.new("ToolStudio");sc.world.use_nodes=True;bg=sc.world.node_tree.nodes["Background"];bg.inputs[0].default_value=(.48,.52,.58,1);bg.inputs[1].default_value=.52
 with bpy.data.libraries.load(str(MODELS/entry["source"]),link=False) as (src,dst):dst.objects=src.objects
 objs=[o for o in dst.objects if o and o.type=="MESH"]
 for o in objs:sc.collection.objects.link(o)
 bpy.context.view_layer.update()
 rot=Matrix.Rotation(math.radians(-32),4,"Z")@Matrix.Rotation(math.radians(-68),4,"X")@Matrix.Rotation(math.radians(18),4,"Y")
 for o in objs:o.matrix_world=rot@o.matrix_world
 bpy.context.view_layer.update();pts=[o.matrix_world@Vector(c) for o in objs for c in o.bound_box]
 lo=Vector([min(p[i] for p in pts) for i in range(3)]);hi=Vector([max(p[i] for p in pts) for i in range(3)]);center=(lo+hi)/2;extent=max((hi-lo).x,(hi-lo).y)
 place=Matrix.Scale(1.84/max(extent,.001),4)@Matrix.Translation(-center)
 for o in objs:o.matrix_world=place@o.matrix_world
 cd=bpy.data.cameras.new("Camera");cd.type="ORTHO";cd.ortho_scale=2.2;cam=bpy.data.objects.new("Camera",cd);sc.collection.objects.link(cam);cam.location=(0,0,12);sc.camera=cam
 for name,pos,power,size in (("Key",(-2.2,-1.4,6),900,6),("Fill",(2.6,1.8,5),520,6)):
  ld=bpy.data.lights.new(name,"AREA");ld.energy=power;ld.shape="DISK";ld.size=size;l=bpy.data.objects.new(name,ld);sc.collection.objects.link(l);l.location=pos
 sc.render.filepath=str(OUT/(entry["id"]+".icon.png"));bpy.ops.render.render(write_still=True);print("RENDERED",entry["id"],flush=True)

requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else list(IDS)
for model_id in requested:
 if model_id not in IDS:raise SystemExit("Unknown Underworld tool icon: "+model_id)
 source=MODELS/"source"/(model_id+".blend")
 if not source.is_file():raise RuntimeError(model_id+": authored Blender source is missing")
 render({"id":model_id,"source":"source/"+source.name})
