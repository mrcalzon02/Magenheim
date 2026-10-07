"""Restore missing staff source images from their existing owned runtime exports.

Geometry and authored palette stay unchanged. No foreign asset or replacement
texture is introduced; the exported material identifies the exact existing image.
"""
import bpy, json, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
MODELS = ROOT / 'assets/models'
catalog = json.loads((MODELS / 'catalog.json').read_text())
requested = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
entries = [e for e in catalog if e['id'] in requested] if requested else [e for e in catalog if e['id'].startswith(('staff-', 'Magenheim_Staff_'))]
if requested and {e['id'] for e in entries} != set(requested):
    raise RuntimeError('Unknown requested source model')
if not requested and len(entries) != 32:
    raise RuntimeError('Expected 32 staff sources')
for entry in entries:
    source = MODELS / entry['source']
    bpy.ops.wm.open_mainfile(filepath=str(source))
    payload = json.loads((MODELS / 'runtime' / (entry['id'] + '.model.json')).read_text())
    materials = {}
    for part in payload['parts']:
        row = part['material']
        name = row['name']
        if name in materials and materials[name]['texture'] != row['texture']:
            raise RuntimeError(f'{entry["id"]}: ambiguous source texture for {name}')
        materials[name] = row
    repaired = 0
    for material in bpy.data.materials:
        if not material.use_nodes:
            continue
        nodes = [n for n in material.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image is None]
        if not nodes:
            continue
        row = materials.get(material.get('magenheim_material_name', material.name))
        if row is None or len(nodes) != 1 or not row.get('texture'):
            raise RuntimeError(f'{entry["id"]}: missing or ambiguous image recovery for {material.name}')
        texture = MODELS / 'textures' / row['texture']
        if not texture.is_file():
            raise RuntimeError('Owned runtime texture missing: ' + str(texture))
        image = bpy.data.images.load(str(texture), check_existing=True)
        image.pack()
        node = nodes[0]
        node.image = image
        tree = material.node_tree
        bsdf = tree.nodes.get('Principled BSDF')
        base = bsdf.inputs['Base Color']
        # Unity multiplies the map by this authored material tint. Use the same
        # multiplication in Blender so icons describe the held runtime material.
        if any(link.from_node == node and link.to_socket == base for link in tree.links):
            tint = tree.nodes.new('ShaderNodeMixRGB')
            tint.name = 'Magenheim Source Material Tint'
            tint.blend_type = 'MULTIPLY'
            tint.inputs[0].default_value = 1.0
            tint.inputs[2].default_value = tuple(base.default_value)
            tree.links.new(node.outputs['Color'], tint.inputs[1])
            tree.links.new(tint.outputs[0], base)
        repaired += 1
    bpy.context.preferences.filepaths.save_version = 0
    if repaired:
        bpy.ops.wm.save_as_mainfile(filepath=str(source), compress=True)
    print('REPAIRED', entry['id'], 'missing images=', repaired, flush=True)
