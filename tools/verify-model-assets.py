"""Validate committed model assets and write the reviewable catalog."""
from pathlib import Path

import csv,json,math,struct,hashlib,zlib

_texture_range={}

def _png_luminance_range(png):
 """Smallest and largest sampled channel value in an 8-bit PNG."""
 pos=8;idat=b'';width=height=colour=0
 while pos<len(png):
  length=struct.unpack('>I',png[pos:pos+4])[0];kind=png[pos+4:pos+8];body=png[pos+8:pos+8+length]
  if kind==b'IHDR':width,height,_depth,colour=struct.unpack('>IIBB',body[:10])
  elif kind==b'IDAT':idat+=body
  elif kind==b'IEND':break
  pos+=12+length
 channels={0:1,2:3,4:2,6:4}.get(colour)
 if channels is None or not idat:return (0,255)
 raw=zlib.decompress(idat);stride=width*channels;values=[];previous=bytearray(stride);offset=0
 for y in range(height):
  filter_type=raw[offset];offset+=1;line=bytearray(raw[offset:offset+stride]);offset+=stride
  for x in range(stride):
   left=line[x-channels] if x>=channels else 0;up=previous[x];corner=previous[x-channels] if x>=channels else 0
   if filter_type==1:line[x]=(line[x]+left)&255
   elif filter_type==2:line[x]=(line[x]+up)&255
   elif filter_type==3:line[x]=(line[x]+((left+up)>>1))&255
   elif filter_type==4:
    predictor=left+up-corner;da,db,dc=abs(predictor-left),abs(predictor-up),abs(predictor-corner)
    line[x]=(line[x]+(left if (da<=db and da<=dc) else (up if db<=dc else corner)))&255
  if y%16==0:values.extend(line[0:stride:channels*4])
  previous=line
 return (min(values),max(values)) if values else (0,255)

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
  # Winding gate: exported vertices are intentionally unwelded, so edge-pairing cannot
  # establish a reliable inside/outside. Instead require each geometric face normal to
  # agree with its authored vertex normals. This catches the silent back-face defect that
  # previously shipped in prisms, cylinders and the geode while remaining valid for
  # concave/open meshes where a model-centroid heuristic can misclassify legitimate faces.
  inverted=[]
  for i in range(0,len(indices),3):
   face=[indices[i+j] for j in range(3)];a,b,c=[vertices[n] for n in face];u=[b[j]-a[j] for j in range(3)];v=[c[j]-a[j] for j in range(3)];cross=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]
   magnitude=math.sqrt(sum(x*x for x in cross));assert magnitude>1e-11,('Degenerate face',id,part['name'])
   geometric=[x/magnitude for x in cross];authored=[sum(normals[n][axis] for n in face) for axis in range(3)]
   authored_magnitude=math.sqrt(sum(x*x for x in authored))
   if authored_magnitude>1e-8:
    authored=[x/authored_magnitude for x in authored]
    if sum(geometric[j]*authored[j] for j in range(3)) < -0.05: inverted.append(i//3)
  assert not inverted,('Inverted face winding',id,part['name'],len(inverted),len(indices)//3,'first faces',inverted[:12])
  texture=part['material'].get('texture')
  if texture:
   texture_path=assets/'textures'/texture;png=texture_path.read_bytes()
   assert png.startswith(b'\x89PNG\r\n\x1a\n'),('Missing PNG',id,texture)
   assert png[12:16]==b'IHDR' and len(png)>=24,('Invalid PNG header',id,texture)
   width,height=struct.unpack('>II',png[16:24])
   assert width>=256 and height>=256,('Texture below 256px fidelity floor',id,texture,width,height)
   # A texture can be the right size, valid PNG, correctly hashed and still carry no image.
   # Generated Blender images store only their settings in a .blend unless packed, so an
   # unpacked one exports as its default generated colour: every map produced in one
   # authoring session shipped pure black, multiplying each albedo to nothing. Size and
   # hash checks cannot see that, so decode and require actual tonal range.
   if texture not in _texture_range:
    _texture_range[texture]=_png_luminance_range(png)
   low,high=_texture_range[texture]
   assert high>low,('Texture carries no tonal range; it is a flat fill',id,texture,low,high)
   assert high>=16,('Texture is effectively black',id,texture,low,high)
  triangles+=len(indices)//3;materials.add(part['material']['name'])
 data=glb.read_bytes();magic,version,length=struct.unpack_from('<III',data);assert magic==0x46546c67 and version==2 and length==len(data),id
 size,kind=struct.unpack_from('<II',data,12);assert kind==0x4e4f534a,id
 gltf=json.loads(data[20:20+size]);assert gltf['meshes'],id
 for m in gltf['meshes']:
  for primitive in m['primitives']:
   for key in ('POSITION','NORMAL','TEXCOORD_0'):assert key in primitive['attributes'],(id,key)
 rows.append(dict(id=id,parts=len(doc['parts']),triangles=triangles,materials=len(materials),source='source/'+source.name,glb='glb/'+glb.name,runtime='runtime/'+file.name,source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),runtime_sha256=hashlib.sha256(file.read_bytes()).hexdigest(),glb_sha256=hashlib.sha256(data).hexdigest()))
assert len(rows)==294,('Unexpected asset coverage',len(rows))
# Magenheim imports authored geometry; it does not generate stand-in shapes at runtime. The rule
# is about *art*: a cube or cylinder built in C# is a model that was never authored. It is not a
# ban on constructing a UnityEngine.Mesh, which is also how authored data and generated terrain
# reach the renderer. Each exemption therefore carries the reason it is not art substitution, so
# adding one is a deliberate act rather than appending a name to a tuple.
MESH_BUILDERS={
 'ModelAssets.cs':'builds the Mesh that an authored .model.json payload is imported into',
 'EarthAssets.cs':'builds the Mesh that an authored .mesh.json payload is imported into',
 'ModelExportRuntime.cs':'reads meshes back out of the running game for export',
 'UnderworldInstanceChunkMaterializer.cs':'builds Underworld terrain from Core-authoritative '
  'heightfield samples. Terrain is generated per chunk from a seed and cannot be authored in '
  'Blender, and this file owns presentation only: heights, biomes and admission all arrive from '
  'Magenheim.Core.Underworld.',
}
for name in MESH_BUILDERS:
 assert (root/'src/Magenheim.Runtime'/name).exists(),('Exempted mesh builder no longer exists',name)
for file in (root/'src/Magenheim.Runtime').glob('*.cs'):
 if file.name in MESH_BUILDERS:continue
 text=file.read_text(encoding="utf-8-sig");assert 'CreatePrimitive(' not in text and 'new Mesh' not in text,('Runtime shape builder remains',file.name)
(assets/'catalog.json').write_text(json.dumps(rows,indent=2))
with (assets/'catalog.tsv').open('w',newline='') as f:
 writer=csv.DictWriter(f,fieldnames=rows[0].keys(),delimiter='\t');writer.writeheader();writer.writerows(rows)
print(f'PASS: {len(rows)} Blender/GLB/runtime sets; {sum(r["triangles"] for r in rows):,} triangles; UVs, normals, >=256px PNGs with real tonal range, topology/winding, hashes, and no active shape generators.')
