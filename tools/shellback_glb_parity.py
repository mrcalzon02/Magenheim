"""Read exported GLB scene and enforce Abyss Shellback triangle parity.

Passing the runtime JSON budget alone cannot prove that the other shipping
representation contains the same evaluated geometry.
"""
import json
import struct
from pathlib import Path

def scene_triangle_count(path):
    raw = Path(path).read_bytes()
    if len(raw) < 20:
        raise ValueError('Shellback GLB truncated header')
    magic, version, total_length = struct.unpack_from('<4sII', raw)
    if magic != b'glTF' or version != 2 or total_length != len(raw):
        raise ValueError('Shellback GLB invalid header/version/length')
    offset = 12
    document = None
    while offset < len(raw):
        if offset + 8 > len(raw):
            raise ValueError('Shellback GLB truncated chunk header')
        size, chunk_type = struct.unpack_from('<I4s', raw, offset)
        offset += 8
        if size % 4 or offset + size > len(raw):
            raise ValueError('Shellback GLB malformed chunk length')
        if chunk_type == b'JSON' and document is None:
            document = json.loads(raw[offset:offset + size].rstrip(b' \x00'))
        offset += size
    if document is None or not document.get('scenes'):
        raise ValueError('Shellback GLB missing scene JSON')
    meshes = document.get('meshes', [])
    accessors = document.get('accessors', [])
    nodes = document.get('nodes', [])

    def accessor_count(index):
        if not isinstance(index, int) or index < 0 or index >= len(accessors):
            raise ValueError('Shellback GLB invalid accessor reference')
        n = accessors[index].get('count')
        if not isinstance(n, int) or n < 0:
            raise ValueError('Shellback GLB invalid accessor count')
        return n

    def mesh_count(index):
        if not isinstance(index, int) or index < 0 or index >= len(meshes):
            raise ValueError('Shellback GLB invalid mesh reference')
        result = 0
        for primitive in meshes[index].get('primitives', []):
            if primitive.get('mode', 4) != 4:
                raise ValueError('Shellback GLB non-triangle primitive')
            if 'indices' in primitive:
                count = accessor_count(primitive['indices'])
            else:
                position = primitive.get('attributes', {}).get('POSITION')
                count = accessor_count(position)
            if count % 3:
                raise ValueError('Shellback GLB triangle index count not divisible by three')
            result += count // 3
        return result

    scene_index = document.get('scene', 0)
    scenes = document['scenes']
    if not isinstance(scene_index, int) or not 0 <= scene_index < len(scenes):
        raise ValueError('Shellback GLB invalid default scene')

    def visit(index, ancestors):
        if not isinstance(index, int) or index < 0 or index >= len(nodes):
            raise ValueError('Shellback GLB invalid scene node')
        if index in ancestors:
            raise ValueError('Shellback GLB cyclic scene hierarchy')
        node = nodes[index]
        total = mesh_count(node['mesh']) if 'mesh' in node else 0
        for child in node.get('children', []):
            total += visit(child, ancestors | {index})
        return total

    total = sum(visit(index, set()) for index in scenes[scene_index].get('nodes', []))
    if total == 0:
        raise ValueError('Shellback GLB scene contains no triangles')
    return total

def validate_parity(runtime, glb_path):
    runtime_count = sum(len(p['triangles']) // 3 for p in runtime['parts'])
    glb_count = scene_triangle_count(glb_path)
    if runtime_count != glb_count:
        raise ValueError(f'Shellback GLB/JSON triangle parity failure: JSON={runtime_count}, GLB={glb_count}')
    return glb_count
