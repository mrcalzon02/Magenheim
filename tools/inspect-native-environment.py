"""Read actual installed player head/eye references without modifying assets."""
import sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'dist/toolchain/unitypy'))
import UnityPy
import re
soft=root.parent/'valheim_Data/StreamingAssets/SoftRef'
manifest=(soft/'manifest_extended').read_text()
bundle=re.search(r'bundle: (\w+)\n  path in bundle: Assets/Systems/_GameMain.prefab',manifest).group(1)
deps={m.group(1):re.findall(r'^  - (\w+)$',m.group(2),re.M) for m in re.finditer(r'^- bundle: (\w+)\n(.*?)(?=^- bundle:|^assets:|\Z)',manifest,re.M|re.S)}
env=UnityPy.Environment();loaded=set()
def load(name):
 if name in loaded:return
 loaded.add(name);env.load_file(str(soft/'Bundles'/name))
 for dep in deps.get(name,[]):load(dep)
load(bundle)
for obj in env.objects:
 if obj.type.name != 'GameObject': continue
 go=obj.read()
 if any(s in go.m_Name.lower() for s in ('ygg','sky','sun','moon','cloud')):
  print(go.m_Name, [(c.component.type.name) for c in go.m_Component])
