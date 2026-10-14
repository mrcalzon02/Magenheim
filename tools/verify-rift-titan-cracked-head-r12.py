"""Verify one connected recessed head, contained crack floor and retained body."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
_r12_base=(ROOT/'tools/verify-rift-titan-notched-r11.py').read_text(encoding='utf-8').replace('rift-titan-notched-r11','rift-titan-cracked-head-r12')
_r12_base=_r12_base[:_r12_base.index("author=json.loads((out/'notch-authoring.json')")]
_r12_base=_r12_base.replace("exec(compile(_r11_proportions,str(__file__),'exec'))","_r11_proportions=_r11_proportions.replace(\"n.startswith('SplitCrown')\",\"n == 'UnifiedCrystalHead'\")\nexec(compile(_r11_proportions,str(__file__),'exec'))")
_r12_base=_r12_base.replace("exec(compile(_r11_crystals,str(__file__),'exec'))","_r11_crystals=_r11_crystals.replace(\"counts['binding-crystals']==172\",\"counts['binding-crystals']==152\")\nexec(compile(_r11_crystals,str(__file__),'exec'))")
exec(compile(_r12_base,str(__file__),'exec'))
head=parts['UnifiedCrystalHead'];assert not any(n in parts for n in ('SplitCrown0','SplitCrown1','SplitCrown2','SplitCrown3','FloatingPrism0','FloatingPrism1'))
me=obj.data;components=me.attributes['attachment_component'];faces=[f for f in me.polygons if components.data[f.index].value==head];ids={i for f in faces for i in f.vertices};assert len(faces)==108 and len(ids)==56
edges={};adjacency={i:set() for i in ids}
for f in faces:
 for a,b in zip(f.vertices,(*f.vertices[1:],f.vertices[0])):
  key=tuple(sorted((a,b)));edges[key]=edges.get(key,0)+1;adjacency[a].add(b);adjacency[b].add(a)
assert all(n==2 for n in edges.values()) and len(ids)-len(edges)+len(faces)==2
seen=set();pending=[next(iter(ids))]
while pending:
 i=pending.pop()
 if i not in seen:seen.add(i);pending.extend(adjacency[i]-seen)
assert seen==ids,'head contains disconnected shells'
roles=me.attributes['head_surface_role'];role_counts={r:sum(roles.data[f.index].value==r for f in faces) for r in (1,2,3)};assert role_counts=={1:76,2:24,3:8}
stations=me.attributes['head_station'];sections=me.attributes['head_section'];depths=[];back_wall=[]
for j in range(1,8):
 ring={sections.data[i].value:obj.matrix_world@me.vertices[i].co for i in ids if stations.data[i].value==j};assert set(ring)==set(range(1,9))
 lip=(ring[2].y+ring[5].y)/2;floor=(ring[3].y+ring[4].y)/2;rear=(ring[7].y+ring[8].y)/2
 assert lip-floor>.001 and floor-rear>.001,(j,lip,floor,rear)
 depths.append(lip-floor);back_wall.append(floor-rear)
groups={g.index:g.name for g in obj.vertex_groups}
assert all({groups[w.group]:round(w.weight,6) for w in me.vertices[i].groups if w.weight>1e-7}=={'Body':1.0} for i in ids)
roots=[r for r in records if r['support']=='UnifiedCrystalHead'];assert len(roots)==6 and all(r['end']=='head-neck-root' for r in roots)
for r in roots:
 vertices=[v for v in me.vertices if me.attributes['attachment_endpoint'].data[v.index].value==r['id']]
 assert len(vertices)==3 and all({groups[w.group]:round(w.weight,6) for w in v.groups if w.weight>1e-7}=={'Body':1.0} for v in vertices)
_r12_caps=_r11_joint[_r11_joint.index('from mathutils.bvhtree import BVHTree'):].replace("'closing-face-verification.json'","'neck-head-face-verification.json'").replace('assert len(caps)==4,r','assert len(caps)==1,r').replace('root_groups=8,closing_triangles_per_group=4','root_groups=6,closing_triangles_per_group=1').replace("'VERIFIED closing-face interiors: 32 triangles,'","'VERIFIED neck/head closing faces: 6 triangles,'")
exec(compile(_r12_caps,str(__file__),'exec'))
neck=json.loads((out/'neck-head-face-verification.json').read_text());assert neck['result']=='PASS' and neck['sampled_points']==1512 and neck['poses']==36
metrics=json.loads((out/'refinement-metrics.json').read_text());assert metrics['triangles']==1792 and metrics['category_triangles']['head-crystal']==108 and metrics['category_triangles']['strata']==984
(out/'head-verification.json').write_text(json.dumps(dict(result='PASS',connected_components=1,closed=True,vertices=56,triangles=108,euler_characteristic=2,surface_role_triangles=role_counts,recess_depth_metres=depths,solid_rear_wall_metres=back_wall,neck_supports=6,neck_face_samples=1512,total_actor_triangles=1792,scope='saved connected shell, shared bevel/floor edges, recessed purple floor and continuous solid rear wall; user visual acceptance remains separate'),indent=2)+'\n',encoding='utf-8')
# Compare all retained body components against the actual r11 saved mesh. Head
# objects are replaced; only six neck endpoint sections are allowed to move.
me=obj.data;components=me.attributes['attachment_component']
with bpy.data.libraries.load(str(ROOT/'assets/models/studies/underworld-boss-rift-titan-notched-r11.blend'),link=False) as (old_data,loaded):
 assert 'RiftTitan_CoherentSurface' in old_data.objects
 loaded.objects=['RiftTitan_CoherentSurface']
old=loaded.objects[0];old_components=old.data.attributes['attachment_component'];checked={};worst=0
for name,component in parts.items():
 if name=='UnifiedCrystalHead' or name.startswith('NeckMetalRib'):continue
 current_faces=[f for f in me.polygons if components.data[f.index].value==component];old_faces=[f for f in old.data.polygons if old_components.data[f.index].value==component]
 current_ids={i for f in current_faces for i in f.vertices};old_ids={i for f in old_faces for i in f.vertices}
 assert len(current_faces)==len(old_faces) and len(current_ids)==len(old_ids),name
 a=sorted(tuple(me.vertices[i].co) for i in current_ids);b=sorted(tuple(old.data.vertices[i].co) for i in old_ids)
 difference=max(abs(x-y) for p,q in zip(a,b) for x,y in zip(p,q));assert difference<2e-6,(name,difference)
 worst=max(worst,difference);checked[name]=dict(triangles=len(current_faces),vertices=len(current_ids),max_coordinate_difference_metres=difference)
(out/'body-preservation.json').write_text(json.dumps(dict(result='PASS',previous_source='assets/models/studies/underworld-boss-rift-titan-notched-r11.blend',components=checked,max_coordinate_difference_metres=worst,scope='all retained body/limb/core geometry unchanged from saved r11; replaced head and six reseated neck sections excluded'),indent=2)+'\n',encoding='utf-8')
p=out/'limb-verification.json';report=json.loads(p.read_text());report['budget']['r9_historical_total']=report['budget']['total'];report['budget']['total']=1792;report['scope']='retained r9 limb topology/root ownership, with current actor cost after unified head correction';p.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('VERIFIED single closed recessed crystal head, six neck caps, 1792 triangles and preserved body geometry')
