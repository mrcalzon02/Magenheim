"""Import extracted local native donors without changing the game files."""
import bpy, json, math
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'dist/boss-donor-quality/native-source';OUT.mkdir(parents=True,exist_ok=True)
NATIVE=ROOT/'dist/vanilla-creature-comparison/native'
# Reuse the verified native material decoder, including DXT5nm decoding.
namespace={'__file__':str(Path(__file__).with_name('render-vanilla-station-comparison.py'))}
text=Path(namespace['__file__']).read_text().split('measurements=[]')[0]
exec(compile(text,namespace['__file__'],'exec'),namespace)
make_material=namespace['make_material']

def mesh_from_obj(path,name):
 vs=[];uvs=[];ns=[];faces=[];faceuv=[];normals=[];slots=[];group=0
 for line in path.read_text().splitlines():
  f=line.split()
  if not f:continue
  if f[0]=='v':vs.append(tuple(map(float,f[1:4])))
  elif f[0]=='vt':uvs.append(tuple(map(float,f[1:3])))
  elif f[0]=='vn':ns.append(tuple(map(float,f[1:4])))
  elif f[0]=='g' and f[1].rsplit('_',1)[-1].isdigit():group=int(f[1].rsplit('_',1)[-1])
  elif f[0]=='f':
   indices=[[int(x) if x else 0 for x in token.split('/')] for token in f[1:]]
   faces.append(tuple(p[0]-1 for p in indices));slots.append(group)
   faceuv.append([uvs[p[1]-1] if len(p)>1 and 0<p[1]<=len(uvs) else (0,0) for p in indices])
   normals.extend(ns[p[2]-1] if len(p)>2 and 0<p[2]<=len(ns) else (0,0,1) for p in indices)
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(vs,[],faces);mesh.update();uv=mesh.uv_layers.new(name='NativeUV')
 for poly,coords,slot in zip(mesh.polygons,faceuv,slots):
  poly.material_index=slot;poly.use_smooth=True
  for li,co in zip(poly.loop_indices,coords):uv.data[li].uv=co
 mesh.normals_split_custom_set(normals);return mesh

records=[]
for name in ['Troll','Serpent','StoneGolem','Greydwarf_Shaman']:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 directory=NATIVE/name;doc=json.loads((directory/'scene.json').read_text())
 materials={key:make_material(record,directory) for key,record in doc['materials'].items()}
 transforms=[];meshes=[];reflect=Matrix.Diagonal((-1,1,1,1));convert=Matrix.Rotation(math.pi/2,4,'X');focus=None
 for node in doc['nodes']:
  x,y,z,w=node['rotation'];local=Matrix.Translation(Vector(node['position']))@Quaternion((w,x,y,z)).to_matrix().to_4x4()@Matrix.Diagonal((*node['scale'],1))
  world=transforms[node['parent']]@local if node['parent'] is not None else local;transforms.append(world)
  if node['name'].lower()=='head':focus=list((convert@reflect@world@reflect).translation)
  if not node['highDetailVisible']:continue
  mesh=mesh_from_obj(directory/node['mesh'],node['name']);o=bpy.data.objects.new(node['name'],mesh);bpy.context.collection.objects.link(o)
  o.matrix_world=convert@reflect@world@reflect
  for key in node['materials']:mesh.materials.append(materials[key])
  meshes.append(o)
 for im in bpy.data.images:
  if im.source=='GENERATED':im.pack()
  elif im.filepath:im.pack()
 for o in meshes:o.data.calc_loop_triangles()
 count=sum(len(o.data.loop_triangles) for o in meshes)
 maps=[dict(name=im.name,dimensions=list(im.size)) for im in bpy.data.images]
 if focus:bpy.context.scene['native_head_focus']=focus
 path=OUT/(name+'.blend');bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True)
 records.append(dict(donor=name,source=path.relative_to(ROOT).as_posix(),triangles=count,renderers=len(meshes),materials=len(materials),maps=maps))
 print(f'AUTHORED donor evidence {name}: {count} triangles / {len(meshes)} renderers',flush=True)
(OUT/'metrics.json').write_text(json.dumps(records,indent=2)+'\n')
