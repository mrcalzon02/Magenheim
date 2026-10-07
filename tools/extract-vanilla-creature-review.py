"""Development-only native creature evidence. Never shipped or used at runtime."""
import json,re,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'dist/toolchain/unitypy'))
import UnityPy
OUT=ROOT/'dist/vanilla-creature-comparison/native';OUT.mkdir(parents=True,exist_ok=True)
SOFT=ROOT.parent/'valheim_Data/StreamingAssets/SoftRef'
entries=re.findall(r'- asset ID: (\w+)\s+bundle: (\w+)\s+path in bundle: ([^\r\n]+)',(SOFT/'manifest_extended').read_text())
names={v.lower() for v in sys.argv[1:]} or {'wolf','troll','lox','seeker','tick','bat','stonegolem','serpent','greydwarf_shaman'}
selected=[(bundle,path) for _,bundle,path in entries if Path(path).stem.lower() in names and path.endswith('.prefab')]
deps={}
for section in (SOFT/'manifest').read_text().split('- bundle: ')[1:]:
 bundle=section.splitlines()[0];deps[bundle]=re.findall(r'^  - (\w+)$',section,re.M)
required=set()
def include(bundle):
 if bundle in required:return
 required.add(bundle)
 for child in deps.get(bundle,[]):include(child)
for bundle,_ in selected:include(bundle)
print('LOADING',len(required),'bundles for',selected,flush=True)
env=UnityPy.load(*(str(SOFT/'Bundles'/b) for b in sorted(required)))
scenes=[]
for _,path in selected:
 name=Path(path).stem;directory=OUT/name;directory.mkdir(exist_ok=True)
 root=next(obj for key,obj in env.container.items() if key.lower()==path.lower())
 nodes=[];materials={};textures={};mesh_cache={};hierarchy=[];lod_groups=[]
 def material(pointer):
  mat=pointer.read();key=str(pointer.path_id)
  if key in materials:return key
  tree=pointer.read_typetree();props=tree.get('m_SavedProperties',{})
  record={'name':mat.m_Name,'properties':props,'textures':{}}
  for pair in props.get('m_TexEnvs',[]):
   slot,value=pair if isinstance(pair,(tuple,list)) else (pair.get('first'),pair.get('second',{}))
   tex=value.get('m_Texture',{})
   if not tex.get('m_PathID'):continue
   from UnityPy.classes import PPtr
   target=PPtr(**tex,assetsfile=pointer.deref().assets_file).deref()
   if target.type.name!='Texture2D':continue
   texture=target.read();filename=str(target.path_id)+'.png'
   if filename not in textures:
    texture.image.save(directory/filename)
    textures[filename]={'name':texture.m_Name,'width':texture.m_Width,'height':texture.m_Height}
   record['textures'][slot]={'file':filename,'scale':value.get('m_Scale'),'offset':value.get('m_Offset')}
  materials[key]=record;return key
 def xyz(v):return [v.x,v.y,v.z]
 def walk(pointer,parent=None,inherited=True,depth=0):
  go=pointer.read();active=inherited and bool(go.m_IsActive)
  components=[entry.component for entry in go.m_Component]
  for component in components:
   if component.type.name=='LODGroup':
    tree=component.read_typetree()
    levels=[[(p.get('renderer',p)).get('m_PathID',0) for p in level.get('renderers',level.get('m_Renderers',[]))] for level in tree.get('m_LODs',[])]
    lod_groups.append({'name':go.m_Name,'levels':levels})
  tr=next((p.read() for p in components if p.type.name=='Transform'),None)
  if tr is None:return
  index=len(nodes);rotation=tr.m_LocalRotation
  node={'name':go.m_Name,'parent':parent,'active':active,'position':xyz(tr.m_LocalPosition),'rotation':[rotation.x,rotation.y,rotation.z,rotation.w],'scale':xyz(tr.m_LocalScale)}
  nodes.append(node);hierarchy.append(' '*depth+go.m_Name+(' [inactive]' if not active else ''))
  mf=next((p.read() for p in components if p.type.name=='MeshFilter'),None)
  renderer_pointer=next((p for p in components if p.type.name in ('MeshRenderer','SkinnedMeshRenderer')),None)
  renderer=renderer_pointer.read() if renderer_pointer else None
  mesh_pointer=mf.m_Mesh if mf else (getattr(renderer,'m_Mesh',None) if renderer else None)
  if mesh_pointer and renderer and active and renderer.m_Enabled and mesh_pointer.path_id:
   mesh=mesh_pointer.read();filename=str(mesh_pointer.path_id)+'.obj'
   if filename not in mesh_cache:
    obj=mesh.export();(directory/filename).write_text(obj)
    mesh_cache[filename]={'name':mesh.m_Name,'vertices':sum(line.startswith('v ') for line in obj.splitlines()),'triangles':sum(line.startswith('f ') for line in obj.splitlines()),'submeshes':len(mesh.m_SubMeshes)}
   node['mesh']=filename;node['materials']=[material(p) for p in renderer.m_Materials if p.path_id];node['rendererId']=renderer_pointer.path_id;node['rendererType']=renderer_pointer.type.name
  for child in tr.m_Children:walk(child.read().m_GameObject,index,active,depth+1)
 walk(root)
 all_lod={p for g in lod_groups for level in g['levels'] for p in level}
 high_lod={p for g in lod_groups for p in (g['levels'][0] if g['levels'] else [])}
 for node in nodes:
  node['highDetailVisible']='mesh' in node and (node['rendererId'] not in all_lod or node['rendererId'] in high_lod)
 doc={'name':name,'source':path,'nodes':nodes,'materials':materials,'textures':textures,'meshes':mesh_cache,'lodGroups':lod_groups}
 (directory/'scene.json').write_text(json.dumps(doc,indent=2,default=str))
 (directory/'hierarchy.txt').write_text('\n'.join(hierarchy))
 scenes.append({'name':name,'source':path,'activeMeshNodes':sum('mesh' in n for n in nodes),'highDetailMeshNodes':sum(n['highDetailVisible'] for n in nodes),'highDetailTriangles':sum(mesh_cache[n['mesh']]['triangles'] for n in nodes if n['highDetailVisible']),'uniqueMeshes':mesh_cache,'textures':textures,'lodGroups':lod_groups})
 print('EXTRACTED',name,'meshes',sum('mesh' in n for n in nodes),flush=True)
metrics_path=OUT.parent/'native-metrics.json'
previous=json.loads(metrics_path.read_text()) if metrics_path.exists() else []
selected_names={s['name'] for s in scenes}
metrics_path.write_text(json.dumps([s for s in previous if s['name'] not in selected_names]+scenes,indent=2))

