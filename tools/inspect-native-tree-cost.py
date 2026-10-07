"""Read actual installed player head/eye references without modifying assets."""
import sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'dist/toolchain/unitypy'))
import UnityPy
import re
soft=root.parent/'valheim_Data/StreamingAssets/SoftRef'
manifest=(soft/'manifest_extended').read_text()
bundle=re.search(r'bundle: (\w+)\n  path in bundle: Assets/Characters/Player/Player.prefab',manifest).group(1)
deps={m.group(1):re.findall(r'^  - (\w+)$',m.group(2),re.M) for m in re.finditer(r'^- bundle: (\w+)\n(.*?)(?=^- bundle:|^assets:|\Z)',manifest,re.M|re.S)}
env=UnityPy.Environment();loaded=set()
def load(name):
 if name in loaded:return
 loaded.add(name);env.load_file(str(soft/'Bundles'/name))
 for dep in deps.get(name,[]):load(dep)
import json
for native in ('Beech1','FirTree','PineTree'):
 match=re.search(r'bundle: (\w+)\n  path in bundle: [^\n]*/'+native+r'\.prefab',manifest)
 load(match.group(1))
for obj in env.objects:
 if obj.type.name!='GameObject':continue
 go=obj.read()
 if go.m_Name not in ('Beech1','FirTree','PineTree'):continue
 meshes=[]; lods=[]; renderers=0
 def visit(go):
  global renderers
  transform=None
  for entry in go.m_Component:
   c=entry.component
   if c.type.name=='Transform':transform=c.read()
   if c.type.name=='MeshRenderer':renderers+=1
   if c.type.name=='MeshFilter':
    mf=c.read()
    if mf.m_Mesh.path_id:
     mesh=mf.m_Mesh.read()
     meshes.append((go.m_Name,sum(s.indexCount for s in mesh.m_SubMeshes)//3,mesh.m_VertexData.m_VertexCount))
   if c.type.name=='LODGroup':
    d=c.read_typetree()
    lods.extend([(v.get('screenRelativeHeight'),len(v.get('renderers',[]))) for v in d.get('m_LODs',[])])
  if transform:
   for child in transform.m_Children:visit(child.read().m_GameObject.read())
 visit(go)
 print(json.dumps({'native':go.m_Name,'renderers':renderers,'lods':lods,'meshes':meshes}))
for path in sorted((root/'assets/models/runtime').glob('underworld-flora-fungal-*.model.json')):
 d=json.loads(path.read_text())
 parts=d['parts']
 print(json.dumps({'custom':path.stem,'parts':len(parts),'triangles':sum(len(p['triangles'])//3 for p in parts),'materials':len(set(json.dumps(p['material'],sort_keys=True) for p in parts))}))
