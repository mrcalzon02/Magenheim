"""Read installed native construction settings; development evidence only."""
import json,re,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'dist/toolchain/unitypy'))
import UnityPy
soft=root.parent/'valheim_Data/StreamingAssets/SoftRef'
entries=re.findall(r'- asset ID: (\w+)\s+bundle: (\w+)\s+path in bundle: ([^\r\n]+)',(soft/'manifest_extended').read_text())
selected=[(bundle,path) for _,bundle,path in entries if Path(path).stem.lower() in {'wood_pole','stone_floor_2x2','wood_beam','wood_pole_log'} and path.endswith('.prefab')]
deps={}
for section in (soft/'manifest').read_text().split('- bundle: ')[1:]:
 deps[section.splitlines()[0]]=re.findall(r'^  - (\w+)$',section,re.M)
required=set()
def include(bundle):
 if bundle in required:return
 required.add(bundle)
 for child in deps.get(bundle,[]):include(child)
for bundle,_ in selected:include(bundle)
env=UnityPy.load(*(str(soft/'Bundles'/b) for b in sorted(required)))
for _,path in selected:
 print('\nDONOR',path)
 def walk(pointer):
  go=pointer.read()
  for entry in go.m_Component:
   p=entry.component
   if p.type.name in ('BoxCollider','MeshCollider','CapsuleCollider','MonoBehaviour'):
    try:
     tree=p.read_typetree()
     if p.type.name=='MonoBehaviour':
      script=p.read().m_Script.read().m_ClassName
      if script not in ('WearNTear','LodFadeInOut','Piece'):continue
      print(go.m_Name,'layer',go.m_Layer,script,json.dumps(tree,default=str))
     else:print(go.m_Name,'layer',go.m_Layer,p.type.name,json.dumps(tree,default=str))
    except Exception as e:print('READ FAILED',go.m_Name,p.type.name,str(e))
   if p.type.name=='Transform':
    for child in p.read().m_Children:walk(child.read().m_GameObject)
 walk(next(obj for key,obj in env.container.items() if key.lower()==path.lower()))
for file in sorted((root/'assets/models/runtime').glob('rootforged-*.model.json')):
 doc=json.loads(file.read_text());parts=[p for p in doc['parts'] if p.get('collider')]
 print('\nOWNED',file.stem,'colliders',len(parts))
 for p in parts:
  lo=[min(v[i] for v in p['vertices']) for i in range(3)];hi=[max(v[i] for v in p['vertices']) for i in range(3)]
  print(p['name'],[round(v,3) for v in lo],[round(v,3) for v in hi])
