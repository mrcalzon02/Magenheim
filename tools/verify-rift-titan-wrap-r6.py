from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/verify-rift-titan-cage-r4.py').read_text(encoding='utf-8').replace('rift-titan-strata-r4','rift-titan-wrap-r6').replace('Rift cage contacts','Rift wrapped torso contacts')
exec(compile(source,str(__file__),'exec'))
import json
records=json.loads(scene['tissue_attachment_contract']);band_records=[r for r in records if r['tissue']=='WrappedStoneBand']
assert len(band_records)==12
obj=next(o for o in scene.objects if o.type=='MESH');me=obj.data
markers=me.attributes['attachment_endpoint'];components=me.attributes['attachment_component']
marker_ids={r['id'] for r in band_records};indices={v.index for v in me.vertices if markers.data[v.index].value in marker_ids}
band_ids={components.data[f.index].value for f in me.polygons if indices.intersection(f.vertices)}
assert len(band_ids)==1
faces=[f for f in me.polygons if components.data[f.index].value in band_ids]
vertices={v for f in faces for v in f.vertices};edges={}
for f in faces:
 for a,b in zip(f.vertices,(*f.vertices[1:],f.vertices[0])):
  key=tuple(sorted((a,b)));edges[key]=edges.get(key,0)+1
assert len(faces)==72 and len(vertices)==36 and all(n==2 for n in edges.values())
assert len(vertices)-len(edges)+len(faces)==0
adjacency={v:set() for v in vertices}
for a,b in edges:adjacency[a].add(b);adjacency[b].add(a)
seen=set();pending=[next(iter(vertices))]
while pending:
 v=pending.pop()
 if v in seen:continue
 seen.add(v);pending.extend(adjacency[v]-seen)
assert seen==vertices
out=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-wrap-r6'
(out/'band-verification.json').write_text(json.dumps(dict(result='PASS',triangles=72,vertices=36,edges=len(edges),connected_components=1,all_edges_two_faces=True,euler_characteristic=0,band_rib_seatings=12,scope='saved mesh closed continuous ring topology and twelve supported inner-ridge contacts; full 48 contacts audited separately across retained actions'),indent=2)+'\n',encoding='utf-8')
print('VERIFIED closed wrapped band: one connected ring, 12 supported rib crossings')
collar_ids={r['support_component'] for r in records if r['support']=='StoneCollarbone'}
assert len(collar_ids)==1
faces=[f for f in me.polygons if components.data[f.index].value in collar_ids]
vertices={v for f in faces for v in f.vertices};edges={}
for f in faces:
 for a,b in zip(f.vertices,(*f.vertices[1:],f.vertices[0])):
  key=tuple(sorted((a,b)));edges[key]=edges.get(key,0)+1
assert len(faces)==48 and len(vertices)==24 and all(n==2 for n in edges.values())
assert len(vertices)-len(edges)+len(faces)==0
adjacency={v:set() for v in vertices}
for a,b in edges:adjacency[a].add(b);adjacency[b].add(a)
seen=set();pending=[next(iter(vertices))]
while pending:
 v=pending.pop()
 if v in seen:continue
 seen.add(v);pending.extend(adjacency[v]-seen)
assert seen==vertices
seatings=[r for r in records if r['tissue']=='StoneCollarbone']
assert len(seatings)==2 and {r['support'] for r in seatings}=={'ShoulderStratumL0','ShoulderStratumR0'}
(out/'collar-verification.json').write_text(json.dumps(dict(result='PASS',triangles=48,vertices=24,edges=len(edges),connected_components=1,all_edges_two_faces=True,euler_characteristic=0,upper_torso_seatings=2,scope='saved closed upper collar ring; full left and right sections seated into upper torso stones, checked in the 50-contact action audit'),indent=2)+'\n',encoding='utf-8')
print('VERIFIED upper collar: closed ring seated into both upper torso masses')
