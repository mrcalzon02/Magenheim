"""Render extracted local native stations under the owned-model review lighting.

Static bind pose and approximate Principled materials only; Valheim vertex noise,
rain, animated liquids, particles and gameplay light effects are not simulated.
"""
import json,math
from pathlib import Path
import bpy
from mathutils import Matrix,Quaternion,Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'dist/vanilla-station-comparison';NATIVE=OUT/'native'
RENDERS=OUT/'renders';RENDERS.mkdir(exist_ok=True)

def pairs(values):
    return {p[0]:p[1] for p in values}

def make_material(record,directory):
    mat=bpy.data.materials.new(record['name']);mat.use_nodes=True
    tree=mat.node_tree;bsdf=tree.nodes.get('Principled BSDF')
    floats=pairs(record['properties'].get('m_Floats',[]));colors=pairs(record['properties'].get('m_Colors',[]))
    color=colors.get('_Color',{'r':1,'g':1,'b':1,'a':1});tint=tuple(color[k] for k in ('r','g','b'))
    bsdf.inputs['Base Color'].default_value=(*tint,1)
    bsdf.inputs['Roughness'].default_value=1-floats.get('_Glossiness',.25)
    bsdf.inputs['Metallic'].default_value=floats.get('_Metallic',0)
    def texture(slot,noncolor=False):
        item=record['textures'].get(slot)
        if not item:return None
        node=tree.nodes.new('ShaderNodeTexImage');node.image=bpy.data.images.load(str(directory/item['file']),check_existing=False)
        node.interpolation='Closest'
        if noncolor:node.image.colorspace_settings.name='Non-Color'
        return node
    main=texture('_MainTex')
    if main:
        mul=tree.nodes.new('ShaderNodeMixRGB');mul.blend_type='MULTIPLY';mul.inputs[0].default_value=1;mul.inputs[2].default_value=(*tint,1)
        tree.links.new(main.outputs['Color'],mul.inputs[1]);tree.links.new(mul.outputs[0],bsdf.inputs['Base Color'])
    metal=texture('_MetallicGlossMap',True) or texture('_MetallicTex',True)
    if metal:
        sep=tree.nodes.new('ShaderNodeSeparateColor');tree.links.new(metal.outputs['Color'],sep.inputs['Color'])
        tree.links.new(sep.outputs['Red'],bsdf.inputs['Metallic'])
        smooth=tree.nodes.new('ShaderNodeMath');smooth.operation='MULTIPLY';smooth.inputs[1].default_value=floats.get('_MetallicAlphaGloss',floats.get('_GlossMapScale',1))
        tree.links.new(metal.outputs['Alpha'],smooth.inputs[0])
        rough=tree.nodes.new('ShaderNodeMath');rough.operation='SUBTRACT';rough.inputs[0].default_value=1
        tree.links.new(smooth.outputs[0],rough.inputs[1]);tree.links.new(rough.outputs[0],bsdf.inputs['Roughness'])
    emission=texture('_EmissionMap')
    em=colors.get('_EmissionColor',{'r':0,'g':0,'b':0})
    if emission:
        mul=tree.nodes.new('ShaderNodeMixRGB');mul.blend_type='MULTIPLY';mul.inputs[0].default_value=1;mul.inputs[2].default_value=(em['r'],em['g'],em['b'],1)
        tree.links.new(emission.outputs['Color'],mul.inputs[1]);tree.links.new(mul.outputs[0],bsdf.inputs['Emission Color']);bsdf.inputs['Emission Strength'].default_value=.7
    elif any(em[k] for k in ('r','g','b')):
        bsdf.inputs['Emission Color'].default_value=(em['r'],em['g'],em['b'],1);bsdf.inputs['Emission Strength'].default_value=.7
    # Native normal maps use DXT5nm's alpha/green storage, decoded explicitly.
    normal=texture('_BumpMap',True)
    if normal:
        image=normal.image;pixels=list(image.pixels);decoded=[]
        for i in range(0,len(pixels),4):
            x=pixels[i+3]*2-1;y=pixels[i+1]*2-1
            z=math.sqrt(max(0,1-x*x-y*y));decoded.extend((x*.5+.5,y*.5+.5,z*.5+.5,1))
        decoded_image=bpy.data.images.new(record['name']+'.decoded-normal',width=image.size[0],height=image.size[1]);decoded_image.colorspace_settings.name='Non-Color';decoded_image.pixels.foreach_set(decoded)
        normal.image=decoded_image;nmap=tree.nodes.new('ShaderNodeNormalMap');nmap.inputs['Strength'].default_value=.42*floats.get('_BumpScale',1)
        tree.links.new(normal.outputs['Color'],nmap.inputs['Color']);tree.links.new(nmap.outputs['Normal'],bsdf.inputs['Normal'])
    return mat

def obj_mesh(path,name):
    vertices=[];uv=[];normals=[];faces=[];faceuv=[];facenormals=[];slots=[];group=0
    for line in path.read_text().splitlines():
        fields=line.split()
        if not fields:continue
        if fields[0]=='v':vertices.append(tuple(map(float,fields[1:4])))
        elif fields[0]=='vt':uv.append(tuple(map(float,fields[1:3])))
        elif fields[0]=='vn':normals.append(tuple(map(float,fields[1:4])))
        elif fields[0]=='g' and fields[1].rsplit('_',1)[-1].isdigit():group=int(fields[1].rsplit('_',1)[-1])
        elif fields[0]=='f':
            items=[list(map(int,p.split('/'))) for p in fields[1:]]
            faces.append(tuple(p[0]-1 for p in items));faceuv.append([uv[p[1]-1] for p in items]);facenormals.extend(normals[p[2]-1] for p in items);slots.append(group)
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    layer=mesh.uv_layers.new(name='NativeUV')
    for face,coords,slot in zip(mesh.polygons,faceuv,slots):
        face.material_index=slot;face.use_smooth=True
        for loop,point in zip(face.loop_indices,coords):layer.data[loop].uv=point
    mesh.normals_split_custom_set(facenormals)
    return mesh

measurements=[]
for directory in sorted(NATIVE.iterdir()):
    if not (directory/'scene.json').is_file():continue
    doc=json.loads((directory/'scene.json').read_text());bpy.ops.wm.read_factory_settings(use_empty=True)
    scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=scene.render.resolution_y=768;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX';scene.world=bpy.data.worlds.new('Comparison');scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.15,.17,.20,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.48
    materials={key:make_material(record,directory) for key,record in doc['materials'].items()};transforms=[];meshes=[]
    reflect=Matrix.Diagonal((-1,1,1,1));convert=Matrix.Rotation(math.pi/2,4,'X')
    for node in doc['nodes']:
        x,y,z,w=node['rotation'];local=Matrix.Translation(Vector(node['position']))@Quaternion((w,x,y,z)).to_matrix().to_4x4()@Matrix.Diagonal((*node['scale'],1))
        world=transforms[node['parent']]@local if node['parent'] is not None else local;transforms.append(world)
        if not node['highDetailVisible']:continue
        mesh=obj_mesh(directory/node['mesh'],node['name']);obj=bpy.data.objects.new(node['name'],mesh);scene.collection.objects.link(obj);obj.matrix_world=convert@reflect@world@reflect
        for key in node['materials']:mesh.materials.append(materials[key])
        meshes.append(obj)
    bpy.context.view_layer.update();points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
    lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
    measurements.append({'name':doc['name'],'dimensionsMeters':list(hi-lo),'renderers':len(meshes),'triangles':sum(len(o.data.polygons) for o in meshes),'materialSlots':sum(len(o.data.materials) for o in meshes)})
    view=Matrix.Rotation(math.radians(-28),4,'Z')@Matrix.Rotation(math.radians(-58),4,'X')@Matrix.Rotation(math.radians(8),4,'Y')
    for obj in meshes:obj.matrix_world=view@obj.matrix_world
    bpy.context.view_layer.update();points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
    lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)]);center=(lo+hi)/2
    placement=Matrix.Scale(1.82/max((hi-lo).x,(hi-lo).y),4)@Matrix.Translation(-center)
    for obj in meshes:obj.matrix_world=placement@obj.matrix_world
    cd=bpy.data.cameras.new('Camera');cd.type='ORTHO';cd.ortho_scale=2.2;camera=bpy.data.objects.new('Camera',cd);scene.collection.objects.link(camera);camera.location=(0,0,12);scene.camera=camera
    for name,pos,power,size,color in (('Key',(-2.4,-1.6,6.2),1050,5.5,(.90,.92,.95)),('Fill',(2.8,1.8,5.0),520,6.0,(.36,.40,.46))):
        data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color;obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);obj.location=pos
    scene.render.filepath=str(RENDERS/(doc['name']+'--neutral.png'));bpy.ops.render.render(write_still=True)
    print('RENDERED',doc['name'],flush=True)
(OUT/'native-render-metrics.json').write_text(json.dumps(measurements,indent=2))
