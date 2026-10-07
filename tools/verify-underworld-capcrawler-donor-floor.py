#!/usr/bin/env python3
"""Fail-closed quality floor against the actual runtime-exported Valheim Seeker donor.

Run under Blender with MAGENHEIM_VALHEIM_DONOR_EXPORT_DIR pointing to the
CinematicDonorExporter directory containing Seeker.obj, Seeker.mtl,
manifest.json and exported donor PNGs. This is a source quality gate,
not a substitute for silhouette renders or in-game acceptance.
"""
import json
import os
import struct
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source/underworld-creature-capcrawler.blend'
TEXTURES = ROOT / 'assets/textures/underworld/creatures/capcrawler'
STEMS = ('cap-carapace', 'underside-flesh', 'leg-plate', 'mandible', 'gill')

def png_size(path):
    with path.open('rb') as stream:
        head = stream.read(24)
    if len(head) != 24 or head[:8] != bytes((137,80,78,71,13,10,26,10)):
        raise RuntimeError(f'Missing or invalid PNG: {path}')
    return struct.unpack('>II', head[16:24])

def main():
    donor_dir = os.environ.get('MAGENHEIM_VALHEIM_DONOR_EXPORT_DIR')
    if not donor_dir:
        raise RuntimeError('Set MAGENHEIM_VALHEIM_DONOR_EXPORT_DIR to a fresh in-game Seeker export')
    donor_dir = Path(donor_dir)
    manifest = json.loads((donor_dir/'manifest.json').read_text(encoding='utf-8-sig'))
    matches = [r for r in manifest.get('models', [])
               if str(r.get('prefab','')).casefold() == 'seeker' and r.get('file')]
    if len(matches) != 1:
        raise RuntimeError('Expected exactly one measured Seeker donor')
    donor = matches[0]
    donor_triangles = int(donor.get('triangles', 0))
    if donor_triangles <= 0:
        raise RuntimeError('Seeker donor triangle count invalid')
    mtl = donor_dir / Path(donor['file']).with_suffix('.mtl')
    donor_maps = []
    for line in mtl.read_text(encoding='utf-8-sig').splitlines():
        if line.startswith(('map_Kd ', 'map_Bump ', 'map_Ke ')):
            donor_maps.append(png_size(donor_dir / line.split(maxsplit=1)[1].strip()))
    if not donor_maps:
        raise RuntimeError('Seeker donor texture resolution unmeasured')
    donor_min = (max(w for w,h in donor_maps), max(h for w,h in donor_maps))
    for stem in STEMS:
        for suffix in ('albedo', 'normal', 'roughness'):
            path = TEXTURES/f'{stem}-{suffix}.png'
            width,height = png_size(path)
            if width < donor_min[0] or height < donor_min[1]:
                raise RuntimeError(f'{path.name}: {width}x{height} below Seeker {donor_min}')
    if not SOURCE.is_file():
        raise RuntimeError(f'Missing authored source: {SOURCE}')
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
    scene = bpy.context.scene
    if scene.get('magenheim_model_id') != 'underworld-creature-capcrawler':
        raise RuntimeError('Wrong creature source')
    meshes = [o for o in scene.objects if o.type == 'MESH']
    triangles = sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
    if triangles < max(6500,donor_triangles):
        raise RuntimeError(f'Capcrawler {triangles} triangles below donor {donor_triangles} / 6500 floor')
    print(f'PASS: Capcrawler {triangles} triangles >= Seeker {donor_triangles}; maps >= {donor_min[0]}x{donor_min[1]}',flush=True)

if __name__ == '__main__':
    main()
