"""Author the three transient effect meshes missing from the editable library."""
import bpy,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]/'assets/models/source'
for kind in ('shard','ring','orb'):
    path=ROOT/('effect-'+kind+'.blend')
    if path.exists():raise RuntimeError('Refusing to overwrite edited effect: '+str(path))
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if kind=='ring':
        v=[];n=48
        # Unity cylinder footprint is diameter 1, height 2. Keep that envelope.
        for z,r in [(-1,.5),(1,.5),(-1,.39),(1,.39)]:
            for i in range(n):
                a=2*math.pi*i/n;v.append((r*math.cos(a),r*math.sin(a),z))
        faces=[]
        for i in range(n):
            j=(i+1)%n
            faces.extend([(i,j,n+j,n+i),(2*n+j,2*n+i,3*n+i,3*n+j),(n+i,n+j,3*n+j,3*n+i),(j,i,2*n+i,2*n+j)])
        mesh=bpy.data.meshes.new('hollow-impact-ring');mesh.from_pydata(v,[],faces);obj=bpy.data.objects.new('hollow-impact-ring',mesh);bpy.context.collection.objects.link(obj)
    else:
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2 if kind=='orb' else 1,radius=.5)
        obj=bpy.context.object;obj.name='energy-orb' if kind=='orb' else 'fractured-debris'
        if kind=='shard':
            for v in obj.data.vertices:
                v.co.x*=.8+.2*math.sin(v.index*2.4);v.co.y*=.8+.2*math.cos(v.index*1.8)
    mat=bpy.data.materials.new('effect-'+kind);mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=(.23,.20,.17,1) if kind=='shard' else (.4,.65,.8,1)
    bs.inputs['Roughness'].default_value=.8 if kind=='shard' else .28
    obj.data.materials.append(mat);obj['game_node_path']=obj.name;obj['game_collision']=False;obj['game_crystal']='null'
    bpy.context.view_layer.objects.active=obj;obj.select_set(True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.scene['model_id']='effect-'+kind;bpy.context.scene['runtime_lights']='[]';bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True)
