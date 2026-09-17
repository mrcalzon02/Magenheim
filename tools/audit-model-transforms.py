"""Blender-side regression guard: saved evaluated meshes must match game bounds.
Also checks material texture fidelity and reports every asset, not just samples.
"""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
ASSETS=ROOT/'assets/models'
results=[]
def bounds(points):
    return [[f(p[i] for p in points) for i in range(3)] for f in (min,max)]
for file in sorted((ASSETS/'source').glob('*.blend')):
    bpy.ops.wm.open_mainfile(filepath=str(file))
    scene=bpy.context.scene
    bpy.context.view_layer.update()
    data=json.loads((ASSETS/'runtime'/(file.stem+'.model.json')).read_text())
    parts={p['name']:p for p in data['parts']}
    objects=[o for o in scene.objects if o.type=='MESH']
    assert len(objects)==len(parts),('Part coverage',file.stem)
    error=0
    for obj in objects:
        part=parts[obj.name]
        evaluated=obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
        points=[obj.matrix_world@v.co for v in mesh.vertices]
        source=bounds([(p.x,p.z,-p.y) for p in points])
        target=bounds(part['vertices'])
        delta=max(abs(source[j][i]-target[j][i]) for j in range(2) for i in range(3))
        error=max(error,delta)
        assert delta<.0001,('Transform mismatch',file.stem,obj.name,delta)
        assert len(mesh.loop_triangles)*3==len(part['triangles']),('Triangle loss',file.stem,obj.name)
        assert obj.get('game_node_path',obj.name)==part['path']
        assert bool(obj.get('game_collision',False))==part['collider']
        assert json.loads(obj.get('game_crystal','null'))==part['crystal']
        for mat in mesh.materials:
            for n in mat.node_tree.nodes:
                if n.type=='TEX_IMAGE' and n.image:
                    assert min(n.image.size)>=256,('Packed texture below floor',file.stem,n.image.name)
        evaluated.to_mesh_clear()
    results.append({'id':file.stem,'parts':len(parts),'maximum_bound_error_m':error,'source_runtime_match':True})
assert len(results)==281
(ASSETS/'transform-audit.json').write_text(json.dumps(results,indent=2))
print('PASS: 281 evaluated Blender sources match runtime part counts, triangles, bounds, bindings and packed texture floor.')
