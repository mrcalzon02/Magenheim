"""Verify the unwired Blackwater source contract; this is not visual/game acceptance."""
import bpy
import importlib.util
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('blackwater', ROOT/'tools/author-underworld-blackwater-deep.py')
author = importlib.util.module_from_spec(spec)
spec.loader.exec_module(author)
results = []
for index, (model_id, (_, size)) in enumerate(author.MODELS.items()):
    bpy.ops.wm.open_mainfile(filepath=str(author.SOURCE/(model_id+'.blend')))
    scene = bpy.context.scene
    assert scene['model_id'] == model_id, model_id
    assert scene['underworld_authoring'] == author.REVISION, f'{model_id}: stale revision'
    assert json.loads(scene['runtime_lights']) == [], model_id
    meshes = [o for o in scene.objects if o.type == 'MESH']
    assert meshes and not any(o.type == 'LIGHT' for o in scene.objects), model_id
    points, triangles = [], 0
    for obj in meshes:
        mesh = obj.data
        mesh.calc_loop_triangles()
        triangles += len(mesh.loop_triangles)
        assert mesh.uv_layers.active and mesh.polygons, f'{model_id}/{obj.name}: missing mesh/UVs'
        assert all(math.isfinite(c) for v in mesh.vertices for c in v.co), model_id
        assert all(math.isfinite(c) and -.001 <= c <= 1.001
                   for uv in mesh.uv_layers.active.data for c in uv.uv), f'{model_id}: invalid UV'
        assert obj['game_node_path'].startswith(f'magenheim.underworld.{model_id}/'), model_id
        for mat in mesh.materials:
            image = mat.node_tree.nodes['Atlas'].image
            assert image and tuple(image.size) == (size, size), f'{model_id}: atlas dimensions'
            assert image.packed_file, f'{model_id}: unpacked image'
            if model_id == 'underworld-resource-blackwater-pearl':
                import numpy
                pixels = numpy.array(image.pixels[:]).reshape(-1, 4)
                dark = float(numpy.mean(numpy.max(pixels[:, :3], axis=1) < .2))
                print(f'Pearl atlas dark fraction={dark:.8f}', flush=True)
                # A few raster-boundary texels can survive; an unpadded atlas leaves large
                # black gutters. Require at least 99% of this pale atlas to remain pale;
                # this is a regression guard, not a substitute for the rendered review.
                assert dark < .01, 'pearl: black gutter/seam regression'
        points.extend(obj.matrix_world @ v.co for v in mesh.vertices)
    assert any(o.get('game_collision', False) for o in meshes) == (index < 4), f'{model_id}: collision policy'
    bounds = [max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
    assert min(bounds) > .01 and max(bounds) < 10, f'{model_id}: scale {bounds}'
    assert 50 <= triangles <= 10000, f'{model_id}: triangle budget {triangles}'
    if model_id.endswith(('wet-stone', 'fingerstone-rubble', 'lakebed-shelf')):
        assert 600 <= triangles <= 1500, f'{model_id}: cover budget {triangles}'
    result = dict(id=model_id, triangles=triangles, bounds_m=[round(v, 3) for v in bounds])
    results.append(result)
    print(f'VERIFIED {model_id}: {triangles} triangles, {result["bounds_m"]}m', flush=True)
out = ROOT/'artifacts/review/blackwater'
out.mkdir(parents=True, exist_ok=True)
(out/'source-validation.json').write_text(json.dumps(results, indent=2)+'\n')
print('Verified 16 Blackwater sources; visual acceptance and runtime export remain separate.')
