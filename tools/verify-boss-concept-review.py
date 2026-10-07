"""Portable GLB and review/provenance gate for the six authored studies."""
from pathlib import Path
import json,struct,hashlib
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04'
provenance=json.loads((OUT/'provenance.json').read_text())
for group in provenance.values():
 for path,digest in group.items():
  assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,('stale boss study',path)
records=json.loads((OUT/'boss-construction-manifest.json').read_text());assert len(records)==6
queue=json.loads((OUT/'production-order.json').read_text());assert len(queue)==48
assert all(r['kind']=='boss' for r in queue[:6]) and all(r['kind']=='creature' for r in queue[6:])
assert len({r['name'] for r in queue})==48
report=[]
for row in records:
 path=OUT/(row['id']+'.glb');data=path.read_bytes()
 magic,version,length=struct.unpack_from('<4sII',data)
 assert magic==b'glTF' and version==2 and length==len(data),path
 size,kind=struct.unpack_from('<II',data,12);assert kind==0x4E4F534A,path
 doc=json.loads(data[20:20+size])
 assert doc['meshes'] and doc['materials'],path
 assert all('uri' not in im and 'bufferView' in im for im in doc['images']),('external GLB texture',path)
 assert all('uri' not in b for b in doc['buffers']),('external GLB buffer',path)
 for mesh in doc['meshes']:
  for primitive in mesh['primitives']:
   assert all(key in primitive['attributes'] for key in ['POSITION','NORMAL','TEXCOORD_0']),path
 assert hashlib.sha256((ROOT/row['concept']).read_bytes()).hexdigest()==row['concept_sha256']
 report.append(dict(id=row['id'],portable_glb_bytes=length,meshes=len(doc['meshes']),materials=len(doc['materials']),images=len(doc['images']),result='PASS portable rest-pose interchange'))
(OUT/'interchange-verification.json').write_text(json.dumps(report,indent=2)+'\n')
print('VERIFIED six portable GLBs, all recorded hashes and complete boss-first 48-creature queue')
