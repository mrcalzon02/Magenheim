"""Promote the selected immutable art sources to production weighted boss payloads.

This is the export authority for the three *-refined identities; the ordinary
rigid creature exporter remains unchanged. No study or ordinary source is saved.
"""
import bpy,json,sys,hashlib,math
from pathlib import Path
from mathutils import Matrix,Vector
import numpy as np
ROOT=Path(__file__).resolve().parents[1];ASSETS=ROOT/'assets/models'
ROWS=[('first-bloom','first-bloom-arm-seams-r22',8846),('rift-titan','rift-titan-broken-strata-r18',1800),('carrion-crown','carrion-crown-anatomy-r3',2046)]
if '--' in sys.argv:
    selected=set(sys.argv[sys.argv.index('--')+1:]);ROWS=[r for r in ROWS if r[0] in selected]
OUT=ROOT/'docs/validation/2026-10-16-refined-boss-runtime';OUT.mkdir(parents=True,exist_ok=True)
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def texture(im,model,kind):
    stage=ASSETS/'textures'/('.staging-'+model+'-'+kind+'.png');im.filepath_raw=str(stage);im.file_format='PNG';im.save()
    name=sha(stage)[:16]+'.png';target=ASSETS/'textures'/name
    if target.exists():stage.unlink()
    else:stage.replace(target)
    return name
def encoded_pose(matrix):
    p,q,s=matrix.decompose();q.normalize()
    rebuilt=Matrix.LocRotScale(p,q,s);error=max(abs(matrix[i][j]-rebuilt[i][j]) for i in range(4) for j in range(4))
    assert error<3e-5,('Pose contains unrepresentable shear',error)
    assert all(math.isfinite(v) for v in (*p,*q,*s)) and min(s)>0
    return dict(position=list(p),rotation=[q.x,q.y,q.z,q.w],scale=list(s)),rebuilt
reports=[]
for boss,revision,expected in ROWS:
    model='underworld-boss-'+boss+'-refined';source=ASSETS/'studies'/('underworld-boss-'+revision+'.blend');source_hash=sha(source)
    bpy.ops.wm.open_mainfile(filepath=str(source));scene=bpy.context.scene
    arm=next(o for o in scene.objects if o.type=='ARMATURE');o=next(o for o in scene.objects if o.type=='MESH')
    assert len([p for p in scene.objects if p.type=='MESH'])==len(o.data.materials)==1
    assert all(abs(obj.matrix_world[i][j]-(1 if i==j else 0))<1e-6 for obj in (o,arm) for i in range(4) for j in range(4))
    assert not any(m.type=='ARMATURE' and m.use_deform_preserve_volume for m in o.modifiers)
    arm.animation_data_create();arm.animation_data.action=None;arm.data.pose_position='REST';scene.frame_set(0)
    original=o.data;original.calc_loop_triangles();assert len(original.loop_triangles)==expected
    # Freeze Blender's exact tessellation in the unsaved production copy. Its
    # original nonplanar quad caps need separate normals per real triangle.
    original_triangles=list(original.loop_triangles);old_uv=original.uv_layers.active.data
    original_weights=[[(g.group,g.weight) for g in v.groups] for v in original.vertices];original_group_names=[g.name for g in o.vertex_groups]
    me=bpy.data.meshes.new(model+' production triangles');me.from_pydata([v.co for v in original.vertices],[],[t.vertices for t in original_triangles]);me.materials.append(original.materials[0]);o.data=me
    for group in list(o.vertex_groups):o.vertex_groups.remove(group)
    for name in original_group_names:o.vertex_groups.new(name=name)
    new_uv=me.uv_layers.new(name='RefinedBossAtlas');original_normals=[]
    for poly,tri in zip(me.polygons,original_triangles):
        for new_li,old_li in zip(poly.loop_indices,tri.loops):new_uv.data[new_li].uv=old_uv[old_li].uv;original_normals.append(original.corner_normals[old_li].vector.copy())
    for index,row in enumerate(original_weights):
        for group,weight in row:o.vertex_groups[group].add([index],weight,'REPLACE')
    me.normals_split_custom_set(original_normals);me.update();me.calc_loop_triangles()
    # Each study explicitly faces Blender +Y. Rotate it into the character's
    # Unity +Z, preserving handedness, and put its authored sole plane at zero.
    C=Matrix(((-1,0,0,0),(0,0,1,0),(0,1,0,0),(0,0,0,1)));C[1][3]=-min(v.co.z for v in me.vertices)
    assert C.to_3x3().determinant()>0
    bones=list(arm.data.bones);bone_index={b.name:i for i,b in enumerate(bones)}
    rest={b.name:C@arm.matrix_world@b.matrix_local for b in bones};rig_bones=[]
    for b in bones:
        m=rest[b.name];assert max(abs(v-1) for v in m.to_scale())<1e-4,(b.name,list(m.to_scale()))
        y=m.to_3x3().col[1].normalized();z=m.to_3x3().col[2];z=(z-y*y.dot(z)).normalized();x=y.cross(z)
        canonical=Matrix((x,y,z)).transposed().to_4x4();canonical.translation=m.translation;rest[b.name]=canonical
        rig_bones.append(dict(name=b.name,parent=b.parent.name if b.parent else None,head=list(m.translation),yAxis=list(y),zAxis=list(z)))
    weights=[]
    for v in me.vertices:
        pairs=sorted([(bone_index[o.vertex_groups[g.group].name],float(g.weight)) for g in v.groups if g.weight>1e-7],key=lambda p:-p[1]);assert 1<=len(pairs)<=2
        total=sum(w for _,w in pairs);assert abs(total-1)<2e-5
        weights.append([[i,w/total] for i,w in pairs])
    indices=np.array([[r[0][0],r[-1][0]] for r in weights]);mix=np.array([[r[0][1],r[1][1] if len(r)>1 else 0] for r in weights])
    points=np.array([list(C@v.co)+[1.] for v in me.vertices]);inverse=np.array([np.array(rest[b.name].inverted()) for b in bones]);maximum_error=0.;checked=0;pose_count=0
    low=points[:,:3].min(axis=0);high=points[:,:3].max(axis=0);actions=[]
    for name in str(scene['magenheim_authored_actions']).split(','):
        action=bpy.data.actions[name];arm.data.pose_position='POSE';arm.animation_data.action=action
        if action.slots:arm.animation_data.action_slot=action.slots[0]
        start,end=[round(v) for v in action.curve_frame_range];frames=[];sample_frames={round(start+(end-start)*t) for t in (0,.25,.5,.75,1)}
        for frame in range(start,end+1):
            scene.frame_set(frame);pose_count+=1;world={p.name:C@arm.matrix_world@p.matrix for p in arm.pose.bones};sample={};reconstructed={}
            pending=list(bones)
            while pending:
                ready=[b for b in pending if not b.parent or b.parent.name in reconstructed];assert ready,'Cyclic bone hierarchy'
                for b in ready:
                    local=world[b.parent.name].inverted()@world[b.name] if b.parent else world[b.name]
                    sample[b.name],rebuilt=encoded_pose(local);reconstructed[b.name]=reconstructed[b.parent.name]@rebuilt if b.parent else rebuilt;pending.remove(b)
            matrices=np.array([np.array(reconstructed[b.name]) for b in bones])@inverse
            skin=np.einsum('nvab,nb->nva',matrices[indices],points)
            positions=(skin*mix[:,:,None]).sum(axis=1)[:,:3];assert np.isfinite(positions).all();low=np.minimum(low,positions.min(axis=0));high=np.maximum(high,positions.max(axis=0))
            if frame in sample_frames:
                graph=bpy.context.evaluated_depsgraph_get();ev=o.evaluated_get(graph);posed=ev.to_mesh();actual=np.array([list(C@(ev.matrix_world@v.co)) for v in posed.vertices]);assert actual.shape==positions.shape
                error=float(np.max(np.linalg.norm(actual-positions,axis=1)));maximum_error=max(maximum_error,error);checked+=len(actual);ev.to_mesh_clear();assert error<.00015,(boss,name,frame,error)
            frames.append(sample)
        actions.append(dict(name=name,start=start,end=end,samples=frames))
    arm.animation_data.action=None;arm.data.pose_position='REST';scene.frame_set(0)
    images=[n.image for n in o.data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE'];assert len(images)==4 and all(tuple(im.size)==(512,512) and im.packed_file for im in images)
    maps={k:next(im for im in images if k in im.name.lower()) for k in ('albedo','normal','roughness','emission')}
    names={k:texture(im,model,k) for k,im in maps.items() if k!='roughness'}
    rough=np.array(maps['roughness'].pixels[:],dtype=np.float32).reshape((-1,4));pixels=np.zeros_like(rough);pixels[:,3]=1-rough[:,0]
    packed=bpy.data.images.new(model+' metallic smoothness',width=512,height=512,alpha=True,is_data=True);packed.pixels.foreach_set(pixels.ravel());packed.update();names['metallicGloss']=texture(packed,model,'metallic-smoothness')
    material=dict(doubleSided=False,name=model+' atlas',color=[1,1,1,1],metallic=0,roughness=.8,emission=[1,1,1],texture=names['albedo'],normalTexture=names['normal'],normalScale=1.,metallicGlossTexture=names['metallicGloss'],emissionTexture=names['emission'])
    part=dict(name=model+' skin',path='creatureskin',vertices=[],normals=[],uv=[],triangles=[],skinWeights=[],material=material,collider=False,crystal=None)
    normal_matrix=C.to_3x3();uv=me.uv_layers.active.data;corrected_normals=[n.vector.copy() for n in me.corner_normals];normal_repairs=0
    for tri in me.loop_triangles:
        a,b,c=[me.vertices[i].co for i in tri.vertices];geometric=(b-a).cross(c-a).normalized();average=sum((corrected_normals[li] for li in tri.loops),Vector())
        if average.length>1e-8 and geometric.dot(average.normalized())<-.05:
            # Some inherited smooth cap normals point behind their triangles.
            # Preserve geometry/winding, use the real face normal at that cap.
            assert len(me.polygons[tri.polygon_index].vertices)==3
            for li in tri.loops:corrected_normals[li]=geometric.copy()
            normal_repairs+=1
        for li in tri.loops:
            vi=me.loops[li].vertex_index;part['vertices'].append(list(C@me.vertices[vi].co));part['normals'].append(list((normal_matrix@corrected_normals[li]).normalized()));part['uv'].append(list(uv[li].uv));part['skinWeights'].append(weights[vi]);part['triangles'].append(len(part['vertices'])-1)
    rig=dict(kind='weighted-creature-v1',poseSpace='parent-local-absolute',fps=scene.render.fps/scene.render.fps_base,bones=rig_bones,actions=actions,animatedBounds=dict(center=((low+high)/2).tolist(),size=(high-low+.3).tolist()))
    payload=dict(source='studies/'+source.name,source_sha256=source_hash,creatureRig=rig,parts=[part],lights=[])
    runtime=ASSETS/'runtime'/(model+'.model.json');runtime.write_text(json.dumps(payload,separators=(',',':')))
    # Only the unsaved interchange copy is rotated/grounded. Studies stay exact.
    me.normals_split_custom_set(corrected_normals)
    rotate=Matrix.Diagonal(Vector((-1.,-1.,1.,1.)));rotate[2][3]=C[1][3];me.transform(rotate);me.update();arm.data.transform(rotate)
    glb=ASSETS/'glb'/(model+'.glb');bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',export_animations=False,export_vertex_color='NONE',export_yup=True)
    assert sha(source)==source_hash
    report=dict(model=model,study=source.relative_to(ROOT).as_posix(),source_sha256=source_hash,runtime_sha256=sha(runtime),glb_sha256=sha(glb),triangles=expected,bones=len(bones),actions=len(actions),animation_frames=pose_count,compared_posed_vertices=checked,max_skin_error_metres=maximum_error,backward_smooth_cap_normals_repaired=normal_repairs,renderers=1,materials=1,atlas_maps=4,atlas_pixels=512,front='Blender +Y -> Unity +Z',ground_offset_metres=C[1][3],source_unchanged=True,scope='exported parent-local transforms and linear skin compared against Blender posed vertices; native AI/hitbox/live acceptance separate')
    (OUT/(boss+'-export.json')).write_text(json.dumps(report,indent=2)+'\n');reports.append(report);print('EXPORTED refined boss',boss,expected,'triangles',len(actions),'actions; max skin error',maximum_error,flush=True)
(OUT/'export-summary.json').write_text(json.dumps(reports,indent=2)+'\n')
