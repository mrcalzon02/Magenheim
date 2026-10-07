"""Read the installed Frost Cave exterior and its native environment prefab."""
import sys, re, json
from pathlib import Path
root = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(root / 'dist/toolchain/unitypy'))
import UnityPy
soft = root.parent / 'valheim_Data/StreamingAssets/SoftRef'
manifest = (soft / 'manifest').read_text()
bundle = re.search(r'bundle: (\w+)\n  path in bundle: Assets/world/Locations/Mountains/MountainCave02.prefab', manifest).group(1)
deps = {m.group(1): re.findall(r'^  - (\w+)$', m.group(2), re.M)
        for m in re.finditer(r'^- bundle: (\w+)\n(.*?)(?=^- bundle:|^assets:|\Z)', manifest, re.M | re.S)}
env = UnityPy.Environment()
loaded = set()
def load(name):
    if name in loaded: return
    loaded.add(name)
    env.load_file(str(soft / 'Bundles' / name))
    for dep in deps.get(name, []): load(dep)
load(bundle)
go = next(v for k,v in env.container.items() if k.lower().endswith('/mountaincave02.prefab'))
def walk(ptr, depth=0):
    data = ptr.read()
    details = []
    transform = None
    for entry in data.m_Component:
        component = entry.component
        if component.type.name == 'Transform': transform = component.read()
        elif component.type.name == 'MonoBehaviour':
            script = component.read().m_Script.read().m_ClassName
            details.append(script)
        else: details.append(component.type.name)
    print('  '*depth + data.m_Name, transform.m_LocalPosition if transform else '', details)
    if transform:
        for child in transform.m_Children: walk(child.read().m_GameObject, depth+1)
walk(go)
