#!/usr/bin/env python3
"""Report whether each model's material samples the UV map the exporter actually exports.

    tools/blender.ps1 report-uv-binding [model-id ...]

The exporter writes `mesh.uv_layers.active.data[li].uv` -- whichever UV map happens to be active in
the source. A material, though, samples whatever UV map its Image Texture node is wired to, which
may be a different one when a mesh carries more than one. When those disagree, Blender shows the
material sampling its own map and the game shows the texture sampled through an unrelated one. That
is a precise match for "perfect in Blender, a twisted mess in the world", so it is worth measuring
rather than assuming.

Reports, per mesh: how many UV layers exist, which is active, which the material samples, and
whether they agree.
"""
import sys
from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets" / "models" / "source"


def sampled_uv(material):
    """The UV map a material's image texture reads, or None when it uses the default."""
    if material is None or material.node_tree is None:
        return None
    for node in material.node_tree.nodes:
        if node.type != 'TEX_IMAGE' or not node.image:
            continue
        vector = node.inputs.get('Vector')
        if vector is None or not vector.is_linked:
            return None  # unlinked: Blender falls back to the active render UV map
        source = vector.links[0].from_node
        if source.type == 'UVMAP':
            return source.uv_map or None
        return f'<{source.type}>'   # attribute/mapping/generated -- not a plain UV map
    return None


def report(model_id):
    path = SOURCE / f"{model_id}.blend"
    if not path.exists():
        print(f"UVREPORT {model_id}: MISSING SOURCE", flush=True)
        return
    bpy.ops.wm.open_mainfile(filepath=str(path))
    if bpy.context.mode != 'OBJECT':
        try:
            bpy.ops.object.mode_set(mode='OBJECT')
        except RuntimeError:
            pass

    mismatched = 0
    multi = 0
    meshes = 0
    for obj in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
        meshes += 1
        layers = [layer.name for layer in obj.data.uv_layers]
        active = obj.data.uv_layers.active.name if obj.data.uv_layers.active else None
        material = obj.data.materials[0] if obj.data.materials else None
        wanted = sampled_uv(material)
        if len(layers) > 1:
            multi += 1
        disagree = wanted is not None and not wanted.startswith('<') and wanted != active
        if disagree:
            mismatched += 1
            print(f"UVREPORT {model_id}/{obj.name}: MISMATCH layers={layers} active={active!r} "
                  f"material samples {wanted!r}", flush=True)
    print(f"UVREPORT {model_id}: meshes={meshes} multi_uv={multi} mismatched={mismatched}", flush=True)


requested = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
if not requested:
    requested = sorted(p.stem for p in SOURCE.glob('*.blend'))
for model in requested:
    report(model)
print("UVREPORT complete", flush=True)
