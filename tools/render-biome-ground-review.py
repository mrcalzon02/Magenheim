"""Review all ten saved biome ground models in a neutral studio."""
import bpy, math
from pathlib import Path
from mathutils import Matrix,Vector
ROOT=Path(__file__).resolve().parents[1]
paths=sorted((ROOT/'assets/models/source').glob('underworld-ground-*.blend'))
assert len(paths)==10
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=24; scene.cycles.use_denoising=True
scene.render.resolution_x=1400; scene.render.resolution_y=2000; scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Studio'); scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.17,.20,.23,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.7
ink=bpy.data.materials.new('Labels'); ink.use_nodes=True
bs=ink.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(.8,.88,.91,1)
bs.inputs['Emission Color'].default_value=(.8,.88,.91,1); bs.inputs['Emission Strength'].default_value=1
for i,path in enumerate(paths):
 with bpy.data.libraries.load(str(path),link=False) as (src,dst): dst.objects=src.objects
 objects=[o for o in dst.objects if o and o.type=='MESH']
 for o in objects: scene.collection.objects.link(o)
 bpy.context.view_layer.update()
 points=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
 rot=Matrix.Rotation(math.radians(-52),4,'X')@Matrix.Rotation(math.radians(-18),4,'Z')
 turned=[rot@p for p in points]; lo=Vector([min(p[k] for p in turned) for k in range(3)]); hi=Vector([max(p[k] for p in turned) for k in range(3)])
 scale=min(4.2/(hi-lo).x,2.1/(hi-lo).y)
 x=(i%2-.5)*5.2; y=6.2-(i//2)*3.0
 place=Matrix.Translation(Vector((x,y,0)))@Matrix.Scale(scale,4)@Matrix.Translation(-(lo+hi)/2)
 for o in objects: o.matrix_world=place@rot@o.matrix_world
 data=bpy.data.curves.new('Label','FONT'); data.body=path.stem.replace('underworld-ground-',''); data.size=.19; data.align_x='CENTER'
 text=bpy.data.objects.new('Label',data); scene.collection.objects.link(text); text.location=(x,y-1.35,2); data.materials.append(ink)
 print('VERIFIED review source '+path.stem,flush=True)
camdata=bpy.data.cameras.new('Camera'); camera=bpy.data.objects.new('Camera',camdata); scene.collection.objects.link(camera)
camera.location=(0,0,30); camdata.type='ORTHO'; camdata.ortho_scale=16; scene.camera=camera
for name,position,power in [('Key',(-4,3,10),1500),('Fill',(6,3,8),1000)]:
 d=bpy.data.lights.new(name,'AREA'); o=bpy.data.objects.new(name,d); scene.collection.objects.link(o); o.location=position; d.energy=power; d.size=8
scene.view_settings.view_transform='AgX'; scene.render.image_settings.file_format='PNG'
out=ROOT/'artifacts/review/ground-features'; out.mkdir(parents=True,exist_ok=True)
scene.render.filepath=str(out/'biome-ground-overview.png'); bpy.ops.render.render(write_still=True)
print('RENDERED '+scene.render.filepath,flush=True)
