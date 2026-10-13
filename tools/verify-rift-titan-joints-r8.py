from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/verify-rift-titan-silhouette-r7.py').read_text(encoding='utf-8').replace('rift-titan-silhouette-r7','rift-titan-joints-r8')
exec(compile(source,str(__file__),'exec'))
records=json.loads(scene['tissue_attachment_contract'])
roots=[r for r in records if r['end'] in ('proximal-stone-root','finger-palm-root')]
assert len(records)==58 and len(roots)==8
obj=next(o for o in scene.objects if o.type=='MESH');groups={g.index:g.name for g in obj.vertex_groups};markers=obj.data.attributes['attachment_endpoint']
for r in roots:
 vertices=[v for v in obj.data.vertices if markers.data[v.index].value==r['id']]
 assert len(vertices)==6
 expected='Body' if r['end']=='proximal-stone-root' else ('ForearmL' if 'StratumL' in r['tissue'] else 'ForearmR')
 assert all({groups[w.group] for w in v.groups if w.weight>1e-7}=={expected} for v in vertices)
blend_counts={}
for side in ('L','R'):
 part=parts['LongFractureArm'+side+'0'];ids={v for f in obj.data.polygons if obj.data.attributes['attachment_component'].data[f.index].value==part for v in f.vertices}
 mixed=[v for v in obj.data.vertices if v.index in ids and len([w for w in v.groups if w.weight>1e-7])==2]
 assert len(ids)==14 and len(mixed)==2
 for v in mixed:
  weights={groups[w.group]:w.weight for w in v.groups if w.weight>1e-7}
  assert set(weights)=={'Body','Arm'+side} and abs(weights['Body']-.45)<1e-6 and abs(weights['Arm'+side]-.55)<1e-6
 blend_counts[side]=len(mixed)
for name,scope in [('band-verification','saved closed middle ring and twelve rib crossings; full 58-contact audit over 35 action samples'),('collar-verification','saved closed upper collar ring and two upper torso seatings; full 58-contact audit over 35 action samples')]:
 p=out/(name+'.json');row=json.loads(p.read_text());row['scope']=scope;p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
(out/'joint-verification.json').write_text(json.dumps(dict(result='PASS',supported_arm_roots=2,supported_finger_roots=6,vertices_per_root=6,root_weights='Body at arm/shoulder seatings; corresponding Forearm at finger/palm seatings',blended_ridge_vertices_per_arm=blend_counts,blend='45% Body / 55% Arm at each proximal stone face ridge',contacts_across_actions=58,sampled_poses=35,scope='saved support vertices, bone ownership and sparse blend; native every-frame deformation unverified'),indent=2)+'\n',encoding='utf-8')
print('VERIFIED arm and finger roots: eight complete six-vertex seatings with matching support weights')
from mathutils.bvhtree import BVHTree
from maw_contact_validation import contains
import numpy as np
samples=[('REST',None,0)]
for name in str(scene['magenheim_authored_actions']).split(','):
 action=bpy.data.actions[name];start,end=action.curve_frame_range
 samples.extend((name,action,round(start+(end-start)*t)) for t in (0,.25,.5,.75,1))
bary=((1/3,1/3,1/3),(.5,.25,.25),(.25,.5,.25),(.25,.25,.5),(.5,.5,0),(0,.5,.5),(.5,0,.5))
cap_samples=0;worst=0;gaps=[]
for name,action,frame_index in samples:
 arm.data.pose_position='POSE' if action else 'REST';arm.animation_data.action=action
 if action and action.slots:arm.animation_data.action_slot=action.slots[0]
 scene.frame_set(frame_index);graph=bpy.context.evaluated_depsgraph_get();evaluated=obj.evaluated_get(graph);me=evaluated.to_mesh(preserve_all_data_layers=True,depsgraph=graph);me.calc_loop_triangles()
 points=[evaluated.matrix_world@v.co for v in me.vertices];markers=me.attributes['attachment_endpoint'];components=me.attributes['attachment_component'];cache={}
 for r in roots:
  component=r['support_component']
  if component not in cache:
   host_faces=[list(t.vertices) for t in me.loop_triangles if components.data[t.polygon_index].value==component]
   cache[component]=(BVHTree.FromPolygons(points,host_faces,all_triangles=True),np.array([[list(points[i]) for i in f] for f in host_faces]))
  bvh,triangles=cache[component];ids={i for i,a in enumerate(markers.data) if a.value==r['id']}
  caps=[t for t in me.loop_triangles if all(i in ids for i in t.vertices)];assert len(caps)==4,r
  for t in caps:
   a,b,c=[points[i] for i in t.vertices]
   for u,v,w in bary:
    point=a*u+b*v+c*w;cap_samples+=1
    if not contains(point,bvh,triangles):
     distance=bvh.find_nearest(point)[3];worst=max(worst,distance)
     if distance>.001:gaps.append(dict(action=name,frame=frame_index,tissue=r['tissue'],support=r['support'],distance_metres=distance))
 evaluated.to_mesh_clear()
(out/'closing-face-verification.json').write_text(json.dumps(dict(result='PASS' if not gaps else 'FAIL',root_groups=8,closing_triangles_per_group=4,sampled_points=cap_samples,poses=len(samples),max_outside_distance_metres=worst,tolerance_metres=.001,outside_samples=gaps,scope='saved evaluated closing-face centers and edges in rest plus five samples per retained action; native every-frame acceptance separate'),indent=2)+'\n',encoding='utf-8')
assert not gaps,('closing-face interiors leave their supports',gaps[:4])
print('VERIFIED closing-face interiors: 32 triangles,',cap_samples,'point samples across',len(samples),'poses')
