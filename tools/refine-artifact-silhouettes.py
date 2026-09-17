"""Saved-mesh art edits for animal effigies and cloth; no runtime generators."""
import bpy,bmesh,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]

def contour(obj, outline, depth=.035, front=0):
    n=len(outline);inv=obj.matrix_world.inverted()
    v=[inv@Vector((x,front+y,z)) for y in (-depth,depth) for x,z in outline]
    faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    m=bpy.data.meshes.new(obj.name+'-carved');m.from_pydata(v,[],faces)
    for mat in obj.data.materials:m.materials.append(mat)
    obj.data=m
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT');obj.select_set(False)

def piece(name,outline,mat,depth=.025,front=0):
    m=bpy.data.meshes.new(name);o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);m.materials.append(mat)
    o['game_node_path']=name;o['game_collision']=False;o['game_crystal']='null'
    contour(o,outline,depth,front)
    return o

for file in sorted((ROOT/'assets/models/source').glob('*.blend')):
    if not (file.stem.startswith('spirit-fetish-') or file.stem.startswith('crystal-banner-')):continue
    bpy.ops.wm.open_mainfile(filepath=str(file));scene=bpy.context.scene
    if scene.get('silhouette_finish')=='1':continue
    if file.stem.startswith('crystal-banner-'):
        for o in scene.objects:
            if o.type!='MESH' or not any('cloth' in m.name for m in o.data.materials):continue
            bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=5,use_grid_fill=True);bm.to_mesh(o.data);bm.free()
            inv=o.matrix_world.inverted();points=[o.matrix_world@v.co for v in o.data.vertices];low=min(p.z for p in points);high=max(p.z for p in points)
            for v,p in zip(o.data.vertices,points):
                fall=(high-p.z)/max(.001,high-low);p.y+=.03*fall*math.sin(p.x*15);v.co=inv@p
            # Fabric needs folds, not rounded solid-block edge strips.
            for mod in list(o.modifiers):o.modifiers.remove(mod)
    else:
        objects=scene.objects
        bone=next(m for m in bpy.data.materials if '.bone' in m.name)
        dark=objects['spine'].data.materials[0]
        if file.stem.endswith('raven'):
            wing=[(.015,.52),(.16,.61),(.36,.65),(.30,.51),(.25,.48),(.28,.56),(.20,.46),(.15,.43),(.18,.52),(.09,.44),(.02,.43)]
            for side,label in [(-1,'left'),(1,'right')]:contour(objects[label+'-wing'],[(x*side,z) for x,z in wing],.024)
            contour(objects['beak'],[(.03,.68),(.08,.73),(.23,.67),(.075,.64)],.026)
            piece('raven-skull',[(-.075,.61),(-.09,.70),(-.04,.77),(.045,.76),(.08,.70),(.035,.62)],bone,.048)
            piece('raven-eye',[(.012,.71),(.045,.71),(.048,.683),(.014,.683)],dark,.006,-.05)
        elif file.stem.endswith('wolf'):
            for side,label in [(-1,'left'),(1,'right')]:contour(objects[label+'-ear'],[(side*.035,.69),(side*.075,.89),(side*.145,.73),(side*.105,.65)],.027)
            piece('wolf-skull',[(-.13,.69),(-.11,.76),(-.04,.79),(.04,.79),(.11,.76),(.13,.69),(.07,.61),(.04,.54),(-.04,.54),(-.07,.61)],bone,.055)
            for side in (-1,1):
                piece('wolf-eye-'+str(side),[(side*.035,.70),(side*.10,.715),(side*.075,.66),(side*.03,.675)],dark,.008,-.06)
            piece('wolf-nose',[(-.035,.59),(.035,.59),(.018,.55),(-.018,.55)],dark,.012,-.065)
        else:
            for side,label in [(-1,'left'),(1,'right')]:contour(objects['helm-'+label],[(0,.72),(0,.88),(side*.065,.86),(side*.115,.79),(side*.10,.70),(side*.06,.74)],.045)
            piece('helmet-nasal',[(-.014,.84),(.014,.84),(.02,.71),(-.02,.71)],bone,.02,-.06)
            piece('shield-boss',[(-.055,.49),(-.03,.54),(.03,.54),(.055,.49),(.03,.44),(-.03,.44)],bone,.025,-.055)
            contour(objects['sword'],[(.19,.20),(.21,.20),(.225,.69),(.20,.80),(.175,.69)],.018,-.03)
            piece('sword-guard',[(.13,.29),(.27,.29),(.27,.32),(.13,.32)],bone,.025,-.03)
    scene['silhouette_finish']='1';bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(file),compress=True)
    print('REFINED',file.stem,flush=True)
