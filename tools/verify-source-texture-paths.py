"""Check the appended-source image dependencies used by offline icon rendering."""
import bpy, json, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
catalog = json.loads((ROOT / 'assets/models/catalog.json').read_text())
requested = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
open_source = '--open' in requested
requested = [name for name in requested if name != '--open']
known = {entry['id'] for entry in catalog}
if not requested or set(requested) - known:
    raise RuntimeError('Provide existing catalog model IDs: ' + str(set(requested) - known))
for entry in catalog:
    if entry['id'] not in requested:
        continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    source = ROOT / 'assets/models' / entry['source']
    if open_source:
        bpy.ops.wm.open_mainfile(filepath=str(source))
    else:
        with bpy.data.libraries.load(str(source), link=False) as (src, dst):
            dst.objects = src.objects
    for material in bpy.data.materials:
        if not material.use_nodes:
            continue
        bsdf = next((node for node in material.node_tree.nodes if node.type == 'BSDF_PRINCIPLED'), None)
        if bsdf:
            print('MATERIAL', material.name, 'base=', tuple(bsdf.inputs['Base Color'].default_value), flush=True)
        for node in material.node_tree.nodes:
            if node.type == 'TEX_IMAGE' and node.image is None:
                raise RuntimeError(f'{entry["id"]}: unbound texture node in {material.name}: {node.name}')
    for image in bpy.data.images:
        path = Path(bpy.path.abspath(image.filepath))
        print('IMAGE', entry['id'], image.name, 'packed=', bool(image.packed_file),
              'size=', tuple(image.size), 'exists=', path.exists(), 'path=', path, flush=True)
        if image.source == 'FILE' and not image.packed_file and not path.is_file():
            raise RuntimeError(f'{entry["id"]}: missing appended texture {image.name}: {path}')
        if image.source == 'FILE' and min(image.size) <= 0:
            raise RuntimeError(f'{entry["id"]}: unreadable image {image.name}')
    print('VERIFIED appended source textures', entry['id'], flush=True)
