#!/usr/bin/env python3
"""Fail closed on measured Valheim Tick donor mesh and texture minimums.

Run in Blender with MAGENHEIM_VALHEIM_DONOR_EXPORT_DIR pointing to a fresh
CinematicDonorExporter export of the installed Valheim Tick prefab.
"""
import json
import os
import shlex
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source/underworld-creature-ashmite.blend'
TEXTURES = ROOT / 'assets/textures/underworld/creatures/ashmite'
STEMS = ('heat-chitin', 'sulfur-crust', 'protected-joint', 'mouthpart', 'eye')
KINDS = ('albedo', 'normal', 'roughness')


def png_size(path):
    with Path(path).open('rb') as stream:
        head = stream.read(24)
    if (len(head) != 24 or head[:8] != b'\x89PNG\r\n\x1a\n'
            or head[12:16] != b'IHDR' or head[8:12] != b'\x00\x00\x00\x0d'):
        raise RuntimeError(f'Invalid or missing PNG IHDR: {path}')
    width, height = struct.unpack('>II', head[16:24])
    if width <= 0 or height <= 0:
        raise RuntimeError(f'Invalid PNG dimensions: {path}')
    return width, height


def obj_triangle_count(path):
    """Measure exported faces rather than accepting a manifest-only polygon count."""
    vertices = 0
    triangles = 0
    for line in Path(path).read_text(encoding='utf-8-sig').splitlines():
        fields = line.split()
        if not fields:
            continue
        if fields[0] == 'v':
            if len(fields) < 4:
                raise RuntimeError(f'Malformed donor OBJ vertex: {line}')
            vertices += 1
        elif fields[0] == 'f':
            if len(fields) < 4:
                raise RuntimeError(f'Malformed donor OBJ face: {line}')
            for corner in fields[1:]:
                raw = corner.split('/')[0]
                try:
                    index = int(raw)
                except ValueError as exc:
                    raise RuntimeError(f'Invalid donor OBJ face index: {corner}') from exc
                if index <= 0 or index > vertices:
                    raise RuntimeError(f'Donor OBJ face references missing vertex: {corner}')
            triangles += len(fields) - 3
    if vertices <= 0 or triangles <= 0:
        raise RuntimeError('Tick donor OBJ contains no measurable mesh geometry')
    return vertices, triangles


def measured_donor(donor_dir):
    donor_dir = Path(donor_dir).resolve()
    manifest = json.loads((donor_dir / 'manifest.json').read_text(encoding='utf-8-sig'))
    if manifest.get('schema') != 1 or manifest.get('source') != 'registered Valheim runtime prefabs':
        raise RuntimeError('Tick donor manifest does not match CinematicDonorExporter schema/source')
    matches = [r for r in manifest.get('models', ())
               if str(r.get('prefab', '')).casefold() == 'tick' and r.get('file')]
    if len(matches) != 1:
        raise RuntimeError('Expected exactly one exported Tick donor entry')
    donor = matches[0]
    triangles = int(donor.get('triangles', 0))
    if triangles <= 0:
        raise RuntimeError('Tick donor triangle count missing or invalid')
    donor_file = (donor_dir / donor['file']).resolve()
    if not donor_file.is_relative_to(donor_dir) or not donor_file.is_file():
        raise RuntimeError('Tick donor OBJ missing or outside export directory')
    vertices_actual, triangles_actual = obj_triangle_count(donor_file)
    if triangles_actual != triangles:
        raise RuntimeError(f'Tick donor manifest triangle count {triangles} differs from OBJ {triangles_actual}')
    if int(donor.get('vertices', 0)) != vertices_actual:
        raise RuntimeError(f'Tick donor manifest vertex count differs from OBJ {vertices_actual}')
    if int(donor.get('meshes', 0)) <= 0:
        raise RuntimeError('Tick donor manifest mesh count missing or invalid')
    mtl = donor_file.with_suffix('.mtl')
    if not mtl.is_file():
        raise RuntimeError('Tick donor MTL missing')
    sizes = []
    for line in mtl.read_text(encoding='utf-8-sig').splitlines():
        parts = shlex.split(line.strip())
        if parts and parts[0] in ('map_Kd', 'map_Bump', 'map_Ke'):
            if len(parts) != 2:
                raise RuntimeError(f'Unsupported Tick donor MTL texture reference: {line}')
            path = (mtl.parent / parts[1]).resolve()
            if not path.is_relative_to(donor_dir):
                raise RuntimeError('Tick donor texture escapes export directory')
            sizes.append(png_size(path))
    if not sizes:
        raise RuntimeError('Tick donor has no measured albedo/normal/emission maps')
    return triangles_actual, (max(w for w, h in sizes), max(h for w, h in sizes))


def validate_texture_floor(texture_dir, donor_size):
    texture_dir = Path(texture_dir)
    for stem in STEMS:
        for kind in KINDS:
            path = texture_dir / f'{stem}-{kind}.png'
            width, height = png_size(path)
            if width < donor_size[0] or height < donor_size[1]:
                raise RuntimeError(f'Ashmite {path.name} {width}x{height} below Tick donor {donor_size}')


def main():
    donor_dir = os.environ.get('MAGENHEIM_VALHEIM_DONOR_EXPORT_DIR')
    if not donor_dir:
        raise RuntimeError('Set MAGENHEIM_VALHEIM_DONOR_EXPORT_DIR to a fresh in-game Tick export')
    donor_triangles, donor_size = measured_donor(donor_dir)
    validate_texture_floor(TEXTURES, donor_size)
    if not SOURCE.is_file():
        raise RuntimeError(f'Missing Ashmite source: {SOURCE}')
    import bpy
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
    if bpy.context.scene.get('magenheim_model_id') != 'underworld-creature-ashmite':
        raise RuntimeError('Wrong creature source loaded')
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    triangles = sum(len(p.vertices) - 2 for o in meshes for p in o.data.polygons)
    if triangles < max(6500, donor_triangles):
        raise RuntimeError(f'Ashmite {triangles} triangles below Tick donor {donor_triangles} / 6500 floor')
    print(f'PASS: Ashmite {triangles} triangles >= Tick {donor_triangles}; '
          f'PBR maps >= {donor_size[0]}x{donor_size[1]}', flush=True)


if __name__ == '__main__':
    main()
