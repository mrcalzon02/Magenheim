"""Matched diagnostic views of actual saved torso components, with arms hidden."""
import bpy,bmesh,json,hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-wrap-r6'
report={}
for revision in ('rift-titan-torso-r5','rift-titan-wrap-r6'):
 folder=ROOT/'docs/art/creature-models/2026-10-13'/revision
 row=json.loads((folder/'refinement-metrics.json').read_text());source=ROOT/row['source']
 bpy.ops.wm.open_mainfile(filepath=str(source));scene=bpy.context.scene
 arm=next(o for o in scene.objects if o.type=='ARMATURE');arm.animation_data.action=None;arm.data.pose_position='REST'
 o=next(o for o in scene.objects if o.type=='MESH');records=json.loads(scene['tissue_attachment_contract'])
 records=[r for r in records if 'NeckMetalRib' not in r['tissue'] and not r['support'].startswith('ShoulderStratum')]
 components=o.data.attributes['attachment_component'];markers=o.data.attributes['attachment_endpoint']
 keep={r['support_component'] for r in records}
 for r in records:
  ids={v.index for v in o.data.vertices if markers.data[v.index].value==r['id']}
  keep.update(components.data[f.index].value for f in o.data.polygons if ids.intersection(f.vertices))
 isolated=o.copy();isolated.data=o.data.copy();scene.collection.objects.link(isolated);o.hide_render=True
 bm=bmesh.new();bm.from_mesh(isolated.data);layer=bm.faces.layers.int['attachment_component']
 bmesh.ops.delete(bm,geom=[f for f in bm.faces if f[layer] not in keep],context='FACES');bm.to_mesh(isolated.data);bm.free()
 isolated.data.calc_loop_triangles();report[revision]=dict(source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),visible_component_ids=sorted(keep),visible_triangles=len(isolated.data.loop_triangles),scope='actual saved torso ribs, band, collar and lower keel; neck, head, shoulders, limbs and crystal cluster hidden for diagnostic visibility only')
 scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
 scene.render.resolution_x=scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
 scene.world=bpy.data.worlds.new('Torso comparison');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.055,.064,.073,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.25
 target=Vector(scene['refinement_head_focus']);span=7*.43
 for label,direction,power in [('Key',(-1,1.4,2),120),('Fill',(1,1,.6),45),('Rim',(0,-1.8,1.3),140)]:
  data=bpy.data.lights.new(label,'AREA');data.energy=power*span*span;data.shape='DISK';data.size=span
  light=bpy.data.objects.new(label,data);scene.collection.objects.link(light);light.location=target+Vector(direction)*span;light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler()
 data=bpy.data.cameras.new('Torso camera');data.type='ORTHO';data.ortho_scale=span*1.18;camera=bpy.data.objects.new('Torso camera',data);scene.collection.objects.link(camera);scene.camera=camera
 for view,direction in [('front',(0,2,.08)),('side',(-2,0,.08)),('rear',(0,-2,.08))]:
  camera.location=target+Vector(direction)*span*2;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
  if view=='side':assert camera.rotation_euler.to_matrix().col[0].dot(Vector((0,1,0)))<0, 'front +Y must project to screen-left'
  scene.render.filepath=str(OUT/(revision+'--isolated-'+view+'.png'));bpy.ops.render.render(write_still=True);print('RENDERED isolated torso '+revision+' '+view,flush=True)
(OUT/'isolated-review.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
