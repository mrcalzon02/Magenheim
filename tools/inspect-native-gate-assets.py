"""Read installed gate assembly and boss defeat flags; no game/save writes."""
import sys, json, re
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'dist/toolchain/unitypy'))
import UnityPy
BUNDLES = ROOT.parent / 'valheim_Data/StreamingAssets/SoftRef/Bundles'
env = UnityPy.load(str(BUNDLES / 'e06fccc7'))
manifest = (BUNDLES.parent / 'manifest').read_text()
for name in re.findall(r'^  - (\w+)$', manifest.split('- bundle: e06fccc7\n')[1].split('- bundle:')[0], re.M):
    env.load_file(str(BUNDLES / name))
gate = next(v for k,v in env.container.items() if k.lower().endswith('/lastbossgate.prefab'))
def walk(go, depth=0):
    data = go.read()
    print('  '*depth + data.m_Name)
    for c in data.m_Component:
        obj = c.component
        if obj.type.name == 'Transform':
            for child in obj.read().m_Children:
                walk(child.read().m_GameObject, depth+1)
        elif obj.type.name == 'MonoBehaviour':
            tree = obj.read_typetree()
            script = obj.read().m_Script.read()
            print('  '*depth + script.m_ClassName + ' ' + json.dumps(tree, default=str))
        else:
            print('  '*depth + obj.type.name)
walk(gate)
for path, obj in env.container.items():
    if any(word in path.lower() for word in ('locationlist_deepnorth', 'dungeonlocation', 'bossdoor', 'bossgate', 'prison')):
        print('LOCATION_ASSET', path)
        if obj.type.name == 'GameObject':
            for component in obj.read().m_Component:
                pointer = component.component
                if pointer.type.name == 'MonoBehaviour':
                    tree = pointer.read_typetree()
                    if 'm_locations' in tree:
                        print('NORTH_LOCATION_LIST', json.dumps(tree['m_locations'], default=str))
for path, obj in env.container.items():
    if '/frozenking/' not in path.lower() or not path.lower().endswith('.prefab'): continue
    if obj.type.name != 'GameObject': continue
    for component in obj.read().m_Component:
        pointer = component.component
        if pointer.type.name != 'MonoBehaviour': continue
        tree = pointer.read_typetree()
        if 'm_defeatSetGlobalKey' in tree:
            print('BOSS', path, tree.get('m_name'), tree['m_defeatSetGlobalKey'])
env.load_file(str(ROOT.parent / 'valheim_Data/resources.assets'))
for obj in env.objects:
    if obj.type.name != 'MonoBehaviour': continue
    try:
        tree = obj.read_typetree()
    except (ValueError, FileNotFoundError):
        continue
    if 'm_locationScenes' in tree:
        print('ZONE_CONFIGURATION', json.dumps(tree, default=str))
