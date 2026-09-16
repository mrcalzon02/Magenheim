"""Validate committed model assets and write the reviewable catalog."""
from pathlib import Path
import csv,json,math,struct,hashlib
root=Path(__file__).resolve().parents[1];assets=root/'assets/models'
rows=[]
for file in sorted((assets/'runtime').glob('*.model.json')):
 id=file.name.removesuffix('.model.json');source=assets/'source'/(id+'.blend');glb=assets/'glb'/(id+'.glb')
 assert source.exists() and glb.exists(),f'Missing editable/interchange model: {id}'
 # Compressed Blender files use Zstandard; uncompressed files begin with BLENDER.
 head=source.read_bytes()[:7];assert head.startswith(b'BLENDER') or head.startswith(bytes.fromhex('28b52ffd')),f'Invalid Blender file: {id}'
 doc=json.loads(file.read_text());assert doc['parts'],id
 triangles=0;materials=set();names=set()
 for part in doc['parts']:
  vertices=part['vertices'];indices=part['triangles'];uv=part['uv'];normals=part['normals']
  assert vertices and len(indices)%3==0 and len(vertices)==len(uv)==len(normals),id
  assert part['name'] not in names,('Duplicate object',id,part['name']);names.add(part['name'])
  assert all(0<=i<len(vertices) for i in indices),id
  assert all(math.isfinite(x) for table in (vertices,uv,normals) for v in table for x in v),id
  assert all(abs(sum(x*x for x in n)-1)<.01 for n in normals),('Bad normal',id)
  for i in range(0,len(indices),3):
   a,b,c=[vertices[indices[i+j]] for j in range(3)];u=[b[j]-a[j] for j in range(3)];v=[c[j]-a[j] for j in range(3)];cross=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]
   assert sum(x*x for x in cross)>1e-22,('Degenerate face',id,part['name'])
  texture=part['material'].get('texture')
  if texture:assert (assets/'textures'/texture).read_bytes().startswith(b'\x89PNG\r\n\x1a\n'),('Missing PNG',id,texture)
  triangles+=len(indices)//3;materials.add(part['material']['name'])
 data=glb.read_bytes();magic,version,length=struct.unpack_from('<III',data);assert magic==0x46546c67 and version==2 and length==len(data),id
 size,kind=struct.unpack_from('<II',data,12);assert kind==0x4e4f534a,id
 gltf=json.loads(data[20:20+size]);assert gltf['meshes'],id
 for m in gltf['meshes']:
  for primitive in m['primitives']:
   for key in ('POSITION','NORMAL','TEXCOORD_0'):assert key in primitive['attributes'],(id,key)
 rows.append(dict(id=id,parts=len(doc['parts']),triangles=triangles,materials=len(materials),source='source/'+source.name,glb='glb/'+glb.name,runtime='runtime/'+file.name,source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),runtime_sha256=hashlib.sha256(file.read_bytes()).hexdigest(),glb_sha256=hashlib.sha256(data).hexdigest()))
assert len(rows)==276,('Unexpected asset coverage',len(rows))
for file in (root/'src/Magenheim.Runtime').glob('*.cs'):
 if file.name in ('ModelAssets.cs','EarthAssets.cs','ModelExportRuntime.cs'):continue
 text=file.read_text();assert 'CreatePrimitive(' not in text and 'new Mesh' not in text,('Runtime shape builder remains',file.name)
(assets/'catalog.json').write_text(json.dumps(rows,indent=2))
with (assets/'catalog.tsv').open('w',newline='') as f:
 writer=csv.DictWriter(f,fieldnames=rows[0].keys(),delimiter='\t');writer.writeheader();writer.writerows(rows)
print(f'PASS: {len(rows)} Blender/GLB/runtime sets; {sum(r["triangles"] for r in rows):,} triangles; UVs, normals, PNGs, topology, hashes, and no active shape generators.')
