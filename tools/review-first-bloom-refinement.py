"""Matched render and geometric/binding checks for the individualized pilot."""
import bpy,bmesh,json,math,hashlib,struct
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-refinement'
row=json.loads((OUT/'refinement-metrics.json').read_text());ID=row['id']
bpy.ops.wm.open_mainfile(filepath=str(ROOT/row['source']))
scene=bpy.context.scene;arm=next(o for o in scene.objects if o.type=='ARMATURE');o=next(o for o in scene.objects if o.type=='MESH')
arm.animation_data.action=None;arm.data.pose_position='REST'
assert len([x for x in scene.objects if x.type=='MESH'])==1
assert len(o.data.materials)==1 and len(o.data.uv_layers)==1
groups={g.index:g.name for g in o.vertex_groups}
for v in o.data.vertices:
 weights=[w for w in v.groups if w.weight>1e-7]
 assert 1<=len(weights)<=2 and abs(sum(w.weight for w in weights)-1)<1e-5
 assert all(groups[w.group] in arm.data.bones for w in weights)
 assert all(math.isfinite(c) for c in v.co)
bm=bmesh.new();bm.from_mesh(o.data)
assert all(e.is_manifold for e in bm.edges),'open/nonmanifold edge'
bm.free();o.data.calc_loop_triangles();assert len(o.data.loop_triangles)==row['triangles']<=row['soft_cap']
for t in o.data.loop_triangles:
 a,b,c=[o.data.vertices[i].co for i in t.vertices];assert (b-a).cross(c-a).length>1e-10
mat=o.data.materials[0];images=[n.image for n in mat.node_tree.nodes if n.type=='TEX_IMAGE'];assert len(images)==4
assert all(tuple(im.size)==(512,512) and im.packed_file for im in images)
actions=str(scene['magenheim_authored_actions']).split(',');assert all(bpy.data.actions.get(n) for n in actions)
points=[o.matrix_world@Vector(p) for p in o.bound_box];lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
assert abs(hi.z-lo.z-row.get('study_height_metres',4.65))<.005
center=(hi+lo)/2;size=max(hi-lo)
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
scene.world=bpy.data.worlds.new('Matched comparison');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.055,.064,.073,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.25
for label,direction,power in [('Key',(-1,1.4,2),120),('Fill',(1,1,.6),45),('Rim',(0,-1.8,1.3),140)]:
 data=bpy.data.lights.new(label,'AREA');data.energy=power*size*size;data.shape='DISK';data.size=size
 light=bpy.data.objects.new(label,data);scene.collection.objects.link(light);light.location=center+Vector(direction)*size;light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
cd=bpy.data.cameras.new('Matched camera');cd.type='ORTHO';cam=bpy.data.objects.new('Matched camera',cd);scene.collection.objects.link(cam);scene.camera=cam
clay=bpy.data.materials.new('Neutral anatomy clay');clay.use_nodes=True;bs=clay.node_tree.nodes['Principled BSDF'];bs.inputs['Base Color'].default_value=(.30,.32,.34,1);bs.inputs['Roughness'].default_value=.83
deformation={}
for view,direction in [('three-quarter',(1,2,.55)),('front',(0,2,.06)),('side',(2,0,.06)),('detail',(1,2,.35)),('clay',(1,2,.55)),('attack',(1,2,.55))]:
 target=center;span=size
 if view=='detail':target=Vector(scene['refinement_head_focus']);span=size*.43
 o.data.materials[0]=clay if view=='clay' else mat
 if view=='attack':
  arm.data.pose_position='POSE';names=[n for n in actions if any(t in n.lower() for t in ('slam','sweep','bite','cast','attack'))] or actions
  action=bpy.data.actions[names[0]];arm.animation_data.action=action
  if action.slots:arm.animation_data.action_slot=action.slots[0]
  start,end=action.curve_frame_range
  # Sample five frames; these checks catch broken bindings, not animation quality.
  for t in (0,.25,.5,.75,1):
   scene.frame_set(round(start+(end-start)*t));evaluated=o.evaluated_get(bpy.context.evaluated_depsgraph_get());me=evaluated.to_mesh()
   assert all(all(math.isfinite(c) for c in v.co) for v in me.vertices)
   pts=[evaluated.matrix_world@v.co for v in me.vertices]
   dims=[max(p[i] for p in pts)-min(p[i] for p in pts) for i in range(3)];assert max(dims)<size*3,('exploded pose',dims)
   deformation[str(t)]=dims;evaluated.to_mesh_clear()
  scene.frame_set(round(start+(end-start)*.4))
 cd.ortho_scale=span*1.18;cam.location=target+Vector(direction)*span*2;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
 scene.render.filepath=str(OUT/(ID+'--'+view+'.png'));bpy.ops.render.render(write_still=True)
 print('RENDERED First Bloom refinement '+view,flush=True)
data=(OUT/(ID+'.glb')).read_bytes();magic,version,length=struct.unpack_from('<4sII',data);assert magic==b'glTF' and version==2 and length==len(data)
size,kind=struct.unpack_from('<II',data,12);doc=json.loads(data[20:20+size]);assert len(doc['meshes'])==1 and len(doc['materials'])==1
assert len(doc['meshes'][0]['primitives'])==1 and doc.get('skins')
assert 'JOINTS_0' in doc['meshes'][0]['primitives'][0]['attributes'] and 'WEIGHTS_0' in doc['meshes'][0]['primitives'][0]['attributes']
assert all('bufferView' in im for im in doc['images'])
report=dict(result='PASS offline geometry, cap, atlas, weights, retained actions and portable skin export',triangles=row['triangles'],dimensions=list(hi-lo),vertices=len(o.data.vertices),material_slots=1,mesh_primitives=1,atlas_maps=4,atlas_pixels=512,uncompressed_rgba_mip_estimate_bytes=4*512*512*4*4/3,source_sha256=hashlib.sha256((ROOT/row['source']).read_bytes()).hexdigest(),sampled_pose_bounds=deformation,limitations='sampled finite bounds do not prove good deformation; runtime loader admission, GPU compression, LODs and live frame cost unverified')
(OUT/'verification.json').write_text(json.dumps(report,indent=2)+'\n')
print('VERIFIED individualized First Bloom pilot',flush=True)
