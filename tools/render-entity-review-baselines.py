"""Render actual current runtime meshes/maps for the campaign; no geometry authoring."""
import bpy,json,hashlib,math,sys
from pathlib import Path
from mathutils import Matrix,Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'docs/art/entity-review/2026-10-25';DEST=OUT/'baselines';DEST.mkdir(exist_ok=True)
RECIPE=hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
rows=json.loads((OUT/'entities.json').read_text())['entities'];selected=set(sys.argv[sys.argv.index('--')+1:]) if '--' in sys.argv else set()
rows=[r for r in rows if r['status']!='paused-by-user' and (not selected or r['name'].lower().replace(' ','-') in selected)]
records={r['name']:r for r in json.loads((OUT/'baseline-rendering.json').read_text())} if (OUT/'baseline-rendering.json').exists() else {}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
for row in rows:
    source=ROOT/row['runtime'];original=sha(source);slug=row['name'].lower().replace(' ','-');doc=json.loads(source.read_text())
    kind=doc['creatureRig']['kind']
    # Invert the actual exporter's basis. Both source families author faces
    # toward Blender +Y, but their stored Unity forward signs are opposite.
    if kind=='rigid-segment-creature-v1':C=Matrix(((1,0,0),(0,0,-1),(0,1,0)));forward='-Z';exporter='tools/export-model-assets.py: [x,z,-y]'
    elif kind=='weighted-creature-v1':C=Matrix(((-1,0,0),(0,0,1),(0,1,0)));forward='+Z';exporter='tools/export-refined-boss-runtime.py: [-x,z,y]'
    else:raise ValueError('Unverified creature coordinate convention: '+kind)
    existing=records.get(row['name']);paths=[DEST/(slug+'--'+view+'.png') for view in ('front','side','three-quarter','rear')]
    if existing and existing.get('recipe_sha256')==RECIPE and existing['runtime_sha256']==original and all(p.exists() and sha(p)==existing['renders'].get(p.name) for p in paths):continue
    bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;materials={};textures={};all_points=[];meshes=[]
    def tex(name,linear=False):
        key=(name,linear)
        if key not in textures:
            p=ROOT/'assets/models/textures'/name;im=bpy.data.images.load(str(p),check_existing=False);im.colorspace_settings.name='Non-Color' if linear else 'sRGB';textures[key]=im
        return textures[key]
    for part in doc['parts']:
        md=part['material'];key=json.dumps(md,sort_keys=True)
        if key not in materials:
            mat=bpy.data.materials.new(md['name']);mat.use_nodes=True;n=mat.node_tree.nodes;l=mat.node_tree.links;bs=n.get('Principled BSDF');bs.inputs['Base Color'].default_value=md.get('color',(1,1,1,1));bs.inputs['Roughness'].default_value=md.get('roughness',.8);bs.inputs['Metallic'].default_value=md.get('metallic',0)
            def image_node(field,linear=False):
                if not md.get(field):return None
                t=n.new('ShaderNodeTexImage');t.image=tex(md[field],linear);return t
            color=image_node('texture')
            if color:
                multiply=n.new('ShaderNodeMixRGB');multiply.blend_type='MULTIPLY';multiply.inputs[0].default_value=1;multiply.inputs[2].default_value=md.get('color',(1,1,1,1));l.new(color.outputs['Color'],multiply.inputs[1]);l.new(multiply.outputs[0],bs.inputs['Base Color'])
            normal=image_node('normalTexture',True)
            if normal:
                nm=n.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=md.get('normalScale') or .42;l.new(normal.outputs['Color'],nm.inputs['Color']);l.new(nm.outputs[0],bs.inputs['Normal'])
            packed=image_node('metallicGlossTexture',True)
            if packed:
                inv=n.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1;l.new(packed.outputs['Alpha'],inv.inputs[1]);l.new(inv.outputs[0],bs.inputs['Roughness'])
                channels=n.new('ShaderNodeSeparateColor');l.new(packed.outputs['Color'],channels.inputs['Color']);l.new(channels.outputs['Red'],bs.inputs['Metallic'])
            glow=image_node('emissionTexture');bs.inputs['Emission Color'].default_value=(*md.get('emission',(0,0,0)),1);bs.inputs['Emission Strength'].default_value=1
            if glow:
                tint=n.new('ShaderNodeMixRGB');tint.blend_type='MULTIPLY';tint.inputs[0].default_value=1;tint.inputs[2].default_value=(*md.get('emission',(0,0,0)),1);l.new(glow.outputs['Color'],tint.inputs[1]);l.new(tint.outputs[0],bs.inputs['Emission Color'])
            materials[key]=mat
        points=[C@Vector(p) for p in part['vertices']];faces=[part['triangles'][i:i+3] for i in range(0,len(part['triangles']),3)];me=bpy.data.meshes.new(part['name']);me.from_pydata(points,[],faces);me.update();uv=me.uv_layers.new(name='Runtime UV')
        for loop in me.loops:uv.data[loop.index].uv=part['uv'][loop.vertex_index]
        me.normals_split_custom_set([C@Vector(part['normals'][loop.vertex_index]) for loop in me.loops]);obj=bpy.data.objects.new(part['name'],me);scene.collection.objects.link(obj);me.materials.append(materials[key]);all_points.extend(points);meshes.append(obj)
    lo=Vector(tuple(min(p[k] for p in all_points) for k in range(3)));hi=Vector(tuple(max(p[k] for p in all_points) for k in range(3)));center=(lo+hi)/2;size=max(hi-lo)
    scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True;scene.render.resolution_x=scene.render.resolution_y=600;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
    scene.world=bpy.data.worlds.new('Entity baseline');scene.world.use_nodes=True;bg=scene.world.node_tree.nodes['Background'];bg.inputs[0].default_value=(.055,.064,.073,1);bg.inputs[1].default_value=.25
    for name,direction,power in [('Key',(-1,1.4,2),120),('Fill',(1,1,.6),45),('Rim',(0,-1.8,1.3),140)]:
        light=bpy.data.lights.new(name,'AREA');light.energy=power*size*size;light.shape='DISK';light.size=size;ob=bpy.data.objects.new(name,light);scene.collection.objects.link(ob);ob.location=center+Vector(direction)*size;ob.rotation_euler=(center-ob.location).to_track_quat('-Z','Y').to_euler()
    cd=bpy.data.cameras.new('Baseline camera');cd.type='ORTHO';cd.ortho_scale=size*1.18;cam=bpy.data.objects.new('Baseline camera',cd);scene.collection.objects.link(cam);scene.camera=cam
    for view,direction in [('front',(0,2,.06)),('side',(-2,0,.06)),('three-quarter',(1,2,.15)),('rear',(0,-2,.06))]:
        cam.location=center+Vector(direction)*size*2;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(DEST/(slug+'--'+view+'.png'));bpy.ops.render.render(write_still=True)
    assert sha(source)==original
    records[row['name']]=dict(name=row['name'],runtime=row['runtime'],runtime_sha256=original,geometry='actual current runtime JSON rest vertices, triangles, normals, UVs and owned material maps; no anatomy generation',lighting='same normalized three-area-light recipe and AgX; framing normalized to rest envelope',renders={p.name:sha(p) for p in paths},runtime_unchanged=True)
    records[row['name']]['recipe_sha256']=RECIPE
    records[row['name']]['orientation']=dict(anatomical_forward_in_stored_runtime=forward,exporter=exporter,review_frame='Blender +Y anatomical forward / +Z up; front camera +Y; rear camera -Y',runtime_to_review_basis=[list(r) for r in C],scope='anatomical views; no source or runtime model rotation written')
    (OUT/'baseline-rendering.json').write_text(json.dumps(list(records.values()),indent=2)+'\n');print('RENDERED entity baseline '+row['name'],flush=True)
print('VERIFIED actual runtime baselines; no model or source modified',flush=True)
