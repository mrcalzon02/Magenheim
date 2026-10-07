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
for native in ('MineRock_Stone','Pickable_Mushroom_Magecap','MushroomMagecap'):
 match=re.search(r'bundle: (\w+)\n  path in bundle: [^\n]*/'+native+r'\.prefab',manifest)
 if match:load(match.group(1))
for obj in env.objects:
 if obj.type.name!='GameObject':continue
 go=obj.read()
 if go.m_Name not in ('MineRock_Stone','Pickable_Mushroom_Magecap','MushroomMagecap'):continue
 print('ROOT',go.m_Name,'layer',go.m_Layer)
 def visit(go):
  transform=None
  for entry in go.m_Component:
   c=entry.component
   if c.type.name=='Transform':transform=c.read()
   if 'Collider' in c.type.name:print(' COLLIDER',go.m_Name,go.m_Layer,c.type.name)
   if c.type.name=='MonoBehaviour':
    d=c.read_typetree()
    print(' SCRIPT',go.m_Name, {k:v for k,v in d.items() if k in ('m_autoPickup','m_itemData')})
  if transform:
   for child in transform.m_Children:visit(child.read().m_GameObject.read())
 visit(go)

