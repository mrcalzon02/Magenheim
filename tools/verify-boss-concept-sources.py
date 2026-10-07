"""Verify draft art integrity without conflating it with gameplay acceptance."""
import bpy, bmesh, json, hashlib, math, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
REVIEW=ROOT/'docs/art/creature-models/2026-10-04'
if '--detail-r2' in sys.argv:REVIEW=REVIEW/'detail-r2'
records=json.loads((REVIEW/'boss-construction-manifest.json').read_text())
report=[]
for row in records:
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/row['source']))
 scene=bpy.context.scene
 arms=[o for o in scene.objects if o.type=='ARMATURE'];assert len(arms)==1,row['id']
 arm=arms[0];arm.data.pose_position='REST';arm.animation_data.action=None
 assert 'Root' in arm.data.bones,(row['id'],'missing root')
 meshes=[o for o in scene.objects if o.type=='MESH'];assert meshes,row['id']
 points=[];boundaries=0;triangles=0
 for o in meshes:
  assert len(o.data.materials)==1,(row['id'],o.name,'multi-material part')
  assert o.data.uv_layers,(row['id'],o.name,'missing UV')
  groups={g.index:g.name for g in o.vertex_groups}
  for v in o.data.vertices:
   active=[groups[w.group] for w in v.groups if w.weight>1e-7]
   assert len(active)==1 and active[0] in arm.data.bones,(row['id'],o.name,'unbound anatomy')
   assert all(math.isfinite(c) for c in v.co),(row['id'],o.name,'nonfinite vertex')
  bm=bmesh.new();bm.from_mesh(o.data)
  open_edges=sum(e.is_boundary for e in bm.edges);boundaries+=open_edges
  if open_edges:assert row['id'].endswith('blackwater-maw') and o.name.startswith('Head__'),(row['id'],o.name,'unexpected open surface')
  bm.free()
  o.data.calc_loop_triangles();triangles+=len(o.data.loop_triangles)
  for t in o.data.loop_triangles:
   a,b,c=[o.data.vertices[i].co for i in t.vertices]
   assert (b-a).cross(c-a).length>1e-10,(row['id'],o.name,'zero-area triangle')
  points.extend(o.matrix_world@Vector(p) for p in o.bound_box)
  m=o.data.materials[0];root=ROOT/m['magenheim_material_source_root'];key=m['magenheim_material_source_key']
  for kind in ['albedo','normal','roughness','metallic-smoothness']:
   path=root/(key+'-'+kind+'.png');assert path.exists(),path
  images=[n.image for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image]
  assert len(images)>=3,(row['id'],o.name,'incomplete PBR nodes')
  assert all(tuple(im.size)==(2048,2048) and im.packed_file for im in images),(row['id'],o.name,'unpacked or undersized maps')
 dims=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
 axis=1 if 'blackwater-maw' in row['id'] else 2
 assert abs(dims[axis]-row['target_metres'])<.005,(row['id'],dims,'scale drift')
 actions=str(scene['magenheim_authored_actions']).split(',')
 for name in actions:assert bpy.data.actions.get(name),(row['id'],'lost inherited action',name)
 assert hashlib.sha256((ROOT/row['concept']).read_bytes()).hexdigest()==row['concept_sha256'],row['concept']
 for view in ['front','side','three-quarter']+([] if '--detail-r2' in sys.argv else ['attack']):
  render_id=row['id'] if '--detail-r2' in sys.argv else row['id'].removeprefix('underworld-boss-')
  assert (REVIEW/(render_id+'--'+view+'.png')).exists(),view
 report.append(dict(id=row['id'],dimensions=dims,triangles=triangles,bones=len(arm.data.bones),inherited_actions=len(actions),intentional_open_edges=boundaries,source_sha256=hashlib.sha256((ROOT/row['source']).read_bytes()).hexdigest(),result='PASS art integrity; animation retargeting and live checks pending'))
 print('VERIFIED '+row['id'],flush=True)
(REVIEW/'source-verification.json').write_text(json.dumps(report,indent=2)+'\n')
