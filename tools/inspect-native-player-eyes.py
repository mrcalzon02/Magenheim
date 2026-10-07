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
load(bundle)
for obj in env.objects:
 if obj.type.name!='GameObject': continue
 go=obj.read()
 if go.m_Name!='Player': continue
 print('Player',obj.path_id)
 for entry in go.m_Component:
  c=entry.component
  if c.type.name!='MonoBehaviour': continue
  data=c.read_typetree()
  fields=[field for field in ('m_eye','m_helmet') if field in data]
  for field in fields:
   ref=data[field]
   target=c.assetsfile.objects[ref['m_PathID']].read()
   print(field)
   chain=[]
   while True:
    chain.append(target)
    print(target.m_GameObject.read().m_Name, target.m_LocalPosition, target.m_LocalRotation, target.m_LocalScale)
    if not target.m_Father.path_id: break
    target=target.m_Father.read()
   point=[0,1.808,.11]
   for bone in reversed(chain[:-1]):
    q=bone.m_LocalRotation;pos=bone.m_LocalPosition;scale=bone.m_LocalScale
    x,y,z,w=-q.x,-q.y,-q.z,q.w
    v=[point[0]-pos.x,point[1]-pos.y,point[2]-pos.z]
    uv=[y*v[2]-z*v[1],z*v[0]-x*v[2],x*v[1]-y*v[0]]
    uuv=[y*uv[2]-z*uv[1],z*uv[0]-x*uv[2],x*uv[1]-y*uv[0]]
    point=[(v[i]+2*(w*uv[i]+uuv[i]))/[scale.x,scale.y,scale.z][i] for i in range(3)]
   print('Eye-height forward point in attachment space',point)
