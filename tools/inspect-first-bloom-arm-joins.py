"""Inspect saved arm-root rings and compare trimmed/untrimmed shoulder views."""
import bpy,json,math,numpy as np
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-arm-seams-r22';OUT.mkdir(parents=True,exist_ok=True)
for label,revision in [('before','first-bloom-bundles-r21'),('untrimmed','first-bloom-bundles-r21-raw')]:
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies'/('underworld-boss-'+revision+'.blend')))
 scene=bpy.context.scene;o=next(o for o in scene.objects if o.type=='MESH');arm=next(o for o in scene.objects if o.type=='ARMATURE');arm.animation_data.action=None;arm.data.pose_position='REST'
 for b in arm.pose.bones:b.matrix_basis=Matrix.Identity(4)
 records=json.loads(scene['tissue_attachment_contract']);me=o.data;me.calc_loop_triangles()
 if label=='untrimmed':
  report=[]
  for item in [r for r in records if r['end']=='compressed_shoulder_root']:
   ids=[i for i,a in enumerate(me.attributes['attachment_endpoint'].data) if a.value==item['id']]
   points=np.array([list(me.vertices[i].co) for i in ids]);center=points.mean(axis=0);u,s,v=np.linalg.svd(points-center);coords=(points-center)@v[:2].T
   crossings=[]
   def cross(a,b):return float(a[0]*b[1]-a[1]*b[0])
   for i in range(10):
    a,b=coords[i],coords[(i+1)%10]
    for j in range(i+2,10):
     if i==0 and j==9:continue
     c,d=coords[j],coords[(j+1)%10]
     if cross(b-a,c-a)*cross(b-a,d-a)<0 and cross(d-c,a-c)*cross(d-c,b-c)<0:crossings.append([i,j])
   report.append(dict(tissue=item['tissue'],root_vertices=ids,root_positions=points.tolist(),best_plane_ring_crossings=crossings,normal_axis=v[-1].tolist(),off_plane_extent=float(s[-1])))
  (OUT/'arm-root-diagnostic.json').write_text(json.dumps(report,indent=2)+'\n')
  print('VERIFIED arm root diagnostic '+json.dumps([(r['tissue'],r['best_plane_ring_crossings']) for r in report]),flush=True)
 scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.render.resolution_x=scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
 scene.world=bpy.data.worlds.new('Arm join inspection');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.055,.064,.073,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.25
 scale=scene['refinement_head_focus'][2]/2.22;target=Vector((-.66,.02,2.20))*scale
 for name,direction,energy in [('Key',(-1,2,2),500),('Fill',(1,2,.5),200),('Rim',(0,-2,1),650)]:
  data=bpy.data.lights.new(name,'AREA');data.energy=energy;data.size=3;light=bpy.data.objects.new(name,data);scene.collection.objects.link(light);light.location=target+Vector(direction)*3;light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler()
 mat=bpy.data.materials.new('Arm join clay');mat.use_nodes=True;mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.30,.32,.34,1);mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.8;o.data.materials[0]=mat
 data=bpy.data.cameras.new('Arm join camera');data.type='ORTHO';data.ortho_scale=1.25;cam=bpy.data.objects.new('Arm join camera',data);scene.collection.objects.link(cam);scene.camera=cam
 for view,direction in [('front',(0,2,.10)),('angle',(-1,2,.15))]:
  cam.location=target+Vector(direction)*3;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/(label+'--left-join--'+view+'.png'));bpy.ops.render.render(write_still=True)
  print('RENDERED arm join '+label+' '+view,flush=True)
