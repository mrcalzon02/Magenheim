"""Editable replacements for the remaining world-center box/cylinder meshes."""
import bpy,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]/'assets/models/source'
for kind in ('standing-stone','dais'):
    file=ROOT/('underworld-'+kind+'.blend')
    if file.exists():raise RuntimeError('Refusing to overwrite '+str(file))
    bpy.ops.wm.read_factory_settings(use_empty=True)
    vertices=[];faces=[]
    if kind=='standing-stone':
        cross=[(-.40,-.5),(.40,-.5),(.5,-.4),(.5,.4),(.4,.5),(-.4,.5),(-.5,.4),(-.5,-.4)]
        rings=[(-.5,1),(-.34,1),(.32,.94),(.46,.82),(.5,.68)]
        for z,scale in rings:
            vertices.extend((x*scale,y*scale,z) for x,y in cross)
        n=8
    else:
        n=48;rings=[(-.5,.5),(-.3,.5),(-.26,.475),(.22,.475),(.26,.45),(.5,.45)]
        for z,r in rings:
            vertices.extend((r*math.cos(i*math.tau/n),r*math.sin(i*math.tau/n),z) for i in range(n))
    faces.append(tuple(range(n-1,-1,-1)))
    for layer in range(len(rings)-1):
        for i in range(n):faces.append((layer*n+i,layer*n+(i+1)%n,(layer+1)*n+(i+1)%n,(layer+1)*n+i))
    faces.append(tuple((len(rings)-1)*n+i for i in range(n)))
    mesh=bpy.data.meshes.new(kind);mesh.from_pydata(vertices,[],faces);obj=bpy.data.objects.new(kind,mesh);bpy.context.collection.objects.link(obj)
    mat=bpy.data.materials.new('deepstone');mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.075,.082,.095,1);bs.inputs['Roughness'].default_value=.76;bs.inputs['Metallic'].default_value=.12;mesh.materials.append(mat)
    obj['game_node_path']=kind;obj['game_collision']=False;obj['game_crystal']='null'
    bpy.context.view_layer.objects.active=obj;obj.select_set(True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.scene['model_id']='underworld-'+kind;bpy.context.scene['runtime_lights']='[]';bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(file),compress=True)
